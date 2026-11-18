using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mod.ModHelper
{
	public class GameLauncherClient
	{
		static readonly string pathLog = ModStorage.GetCommonDataPath("log_launcher_client.txt");

		static GameLauncherClient _instance;
		readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
		bool _autoLoginDone;
		bool _hasCliPassword;
		volatile bool _credentialsReady;
		CancellationTokenSource _cts;
		Thread _receiveThread;

		ClientWebSocket _ws;
		public static GameLauncherClient Instance => _instance ??= new GameLauncherClient();

		public bool IsConnected { get; private set; }

		public string Username { get; private set; } = "";
		public string Password { get; private set; } = "";
		public string Server { get; private set; } = "";
		public string IpcToken { get; private set; } = "";
		public int WsPort { get; private set; } = -1;

		public event Action<string, JObject> OnLauncherEvent;

		public void ParseStartupArgs()
		{
			try
			{
				string[] args = Environment.GetCommandLineArgs();
				for (int i = 0; i < args.Length; i++)
				{
					switch (args[i])
					{
					case "-port":
						if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedPort))
							WsPort = parsedPort;
						break;
					case "-username":
						if (i + 1 < args.Length) Username = args[++i];
						break;
					case "-password":
						if (i + 1 < args.Length)
						{
							Password = args[++i];
							_hasCliPassword = true;
							_credentialsReady = true;
						}
						break;
					case "-ipcToken":
						if (i + 1 < args.Length) IpcToken = args[++i];
						break;
					case "-server":
						if (i + 1 < args.Length) Server = args[++i];
						break;
					}
				}
			}
			catch (Exception ex)
			{
				WriteLog("Error parsing startup args: " + ex);
			}
		}

		public bool IsLaunchedByLauncher()
		{
			return WsPort > 0;
		}

		static bool TryGetToken(CancellationTokenSource cts, out CancellationToken token)
		{
			if (cts == null)
			{
				token = CancellationToken.None;
				return false;
			}
			try
			{
				token = cts.Token;
				return !token.IsCancellationRequested;
			}
			catch (ObjectDisposedException)
			{
				token = CancellationToken.None;
				return false;
			}
		}

		public void Connect()
		{
			if (WsPort <= 0)
			{
				WriteLog("Cannot connect: wsPort not set.");
				return;
			}

			CancellationTokenSource newCts = new CancellationTokenSource();
			CancellationTokenSource oldCts = Interlocked.Exchange(ref _cts, newCts);
			if (oldCts != null)
			{
				try { oldCts.Cancel(); } catch (ObjectDisposedException) { }
			}
			_receiveThread = new Thread(() => ConnectAndListen(newCts))
			{
				Name = "LauncherWSClient",
				IsBackground = true
			};
			_receiveThread.Start();
		}

		void ConnectAndListen(CancellationTokenSource cts)
		{
			if (!TryGetToken(cts, out CancellationToken token))
			{
				try { cts?.Dispose(); } catch (ObjectDisposedException) { }
				return;
			}

			ClientWebSocket ws = new ClientWebSocket();
			_ws = ws;

			try
			{
				Uri uri = new Uri($"ws://127.0.0.1:{WsPort}");

				Stopwatch sw = Stopwatch.StartNew();

				const int timeoutMs = 10_000;
				System.Threading.Tasks.Task connectTask = ws.ConnectAsync(uri, token);
				bool completed = connectTask.Wait(timeoutMs);
				sw.Stop();

				if (!completed)
				{
					WriteLog($"Connection timed out after {sw.ElapsedMilliseconds}ms. Launcher may not be running or not completing WebSocket handshake on port {WsPort}.");
					try { ws.Abort(); }
					catch
					{
						// Ignore abort errors
					}
					try { ws.Dispose(); }
					catch
					{
						// Ignore dispose errors
					}
					Interlocked.CompareExchange(ref _ws, null, ws);
					MainThreadDispatcher.Dispatch(() =>
					{
						GameCanvas.startOKDlg($"Không thể kết nối tới Launcher (timeout sau {sw.ElapsedMilliseconds / 1000}s).\nKiểm tra Launcher đang chạy WebSocket server trên port {WsPort}.");
					});
					return;
				}

				if (connectTask.IsFaulted)
				{
					throw connectTask.Exception?.InnerException ?? connectTask.Exception ?? new Exception("Unknown connection error");
				}

				IsConnected = true;

				if (string.IsNullOrEmpty(IpcToken))
				{
					SendMessage(new
					{
						action = "connected",
						id = Process.GetCurrentProcess().Id,
						username = Username
					});
				}
				else
				{
					SendMessage(new
					{
						action = "connected",
						id = Process.GetCurrentProcess().Id,
						username = Username,
						token = IpcToken
					});
				}

				if (!_hasCliPassword)
				{
					_credentialsReady = false;
					SendMessage(new
					{
						action = "get_credentials",
						token = IpcToken
					});
				}
				else
				{
					_credentialsReady = true;
				}

				MainThreadDispatcher.Dispatch(() =>
				{
					Utils.IsOpenedByExternalAccountManager = true;
					Utils.username = Username;
					if (_hasCliPassword)
					{
						Utils.password = Password;
					}
					ApplyServerSelection(Server);
				});

				// Wait 5 seconds for the game to fully boot before auto-login (separate thread to not block receive loop)
				new Thread(() =>
				{
					Thread.Sleep(5000);
					MainThreadDispatcher.Dispatch(TryAutoLogin);
				})
				{
					IsBackground = true
				}.Start();

				byte[] buffer = new byte[4096];
				using (MemoryStream ms = new MemoryStream(4096))
				{
					while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
					{
						try
						{
							ms.SetLength(0);
							WebSocketReceiveResult result;
							do
							{
								result = ws.ReceiveAsync(new ArraySegment<byte>(buffer), token).GetAwaiter().GetResult();
								if (result.MessageType == WebSocketMessageType.Close)
									break;
								ms.Write(buffer, 0, result.Count);
							} while (!result.EndOfMessage);

							if (result.MessageType == WebSocketMessageType.Close)
							{
								WriteLog("Launcher closed the WebSocket connection.");
								IsConnected = false;
								break;
							}

							string message = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);

							if (message == "2" || (message.Length > 1 && message[0] == '2' && !char.IsDigit(message[1])))
							{
								string pong = message.Length == 1 ? "3" : "3" + message.Substring(1);
								SendRawText(pong);
								continue;
							}

							if (message == "0" || message.StartsWith("0{") || message == "40" || message.StartsWith("40{") || message == "3")
							{
								continue;
							}

							if (message.StartsWith("42"))
							{
								string json = message.Substring(2);
								JArray arr = JArray.Parse(json);
								if (arr.Count >= 1)
								{
									string eventName = arr[0]?.ToString() ?? "";
									JObject data = arr.Count >= 2 ? arr[1] as JObject : null;
									if (data == null)
									{
										data = new JObject();
									}
									if (data["action"] == null && !string.IsNullOrEmpty(eventName))
									{
										data["action"] = eventName;
									}

									WriteLog($"Event: {eventName}");
									MainThreadDispatcher.Dispatch(() => HandleMessage(data));
								}
							}
							else if (message.StartsWith("{"))
							{
								JObject data = JObject.Parse(message);
								MainThreadDispatcher.Dispatch(() => HandleMessage(data));
							}
							else
							{
								WriteLog("Received non-42 message: " + message);
							}
						}
						catch (OperationCanceledException)
						{
							break;
						}
						catch (ObjectDisposedException)
						{
							break;
						}
						catch (Exception ex)
						{
							if (token.IsCancellationRequested || _ws == null)
								break;
							WriteLog("Receive error: " + ex);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected on shutdown
			}
			catch (ObjectDisposedException)
			{
				// Expected on shutdown
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested && _ws != null)
				{
					WriteLog("Connection error: " + ex);
					MainThreadDispatcher.Dispatch(() =>
					{
						GameCanvas.startOKDlg("Mất kết nối với Launcher");
					});
				}
			}
			finally
			{
				IsConnected = false;
				Interlocked.CompareExchange(ref _cts, null, cts);
				try { cts?.Dispose(); } catch (ObjectDisposedException) { }
			}
		}

		void HandleMessage(JObject msg)
		{
			string action = (string)msg["action"];
			if (string.IsNullOrEmpty(action))
			{
				WriteLog("Received message with no action.");
				return;
			}

			WriteLog($"Received action: {action}");

			switch (action)
			{
			case "credentials":
			{
				string receivedUser = (string)msg["username"];
				string receivedPass = (string)msg["password"];
				string receivedServer = (string)msg["server"];

				if (!string.IsNullOrEmpty(receivedUser))
				{
					Username = receivedUser;
					Utils.username = receivedUser;
				}
				if (receivedPass != null)
				{
					Password = receivedPass;
					Utils.password = receivedPass;
				}
				if (!string.IsNullOrEmpty(receivedServer))
				{
					Server = receivedServer;
				}

				ApplyServerSelection(Server);
				_credentialsReady = true;
				TryAutoLogin();
				break;
			}
			case "stop":
			{
				WriteLog("Launcher requested stop game.");
				Close();
				Application.Quit();
				break;
			}
			default:
			{
				WriteLog($"Unknown action: {action}");
				break;
			}
			}
			OnLauncherEvent?.Invoke(action, msg);
		}

		static void ApplyServerSelection(string serverSpec)
		{
			if (string.IsNullOrWhiteSpace(serverSpec))
			{
				Utils.server = new JObject
				{
					["ip"] = GameEvents.TEAMOBI2026_HOST,
					["port"] = (int)GameEvents.TEAMOBI2026_PORT
				};
				return;
			}

			string trimmed = serverSpec.Trim();
			string[] colonParts = trimmed.Split(':');
			if ((colonParts.Length == 2 || colonParts.Length == 3) &&
			    ushort.TryParse(colonParts[colonParts.Length - 1], out ushort customPort))
			{
				string host = colonParts[colonParts.Length - 2].Trim();
				if (!string.IsNullOrEmpty(host))
				{
					Utils.server = new JObject
					{
						["ip"] = host,
						["port"] = (int)customPort
					};
					return;
				}
			}

			if (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0)
			{
				for (int i = 0; i < ServerListScreen.nameServer.Length; i++)
				{
					string candidate = ServerListScreen.nameServer[i];
					if (!string.IsNullOrEmpty(candidate) &&
					    string.Equals(candidate.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
					{
						Utils.server = null;
						ServerListScreen.ipSelect = i;
						Rms.saveRMSInt("svselect", i);
						return;
					}
				}

				if (int.TryParse(trimmed, out int index) && index >= 0 && index < ServerListScreen.nameServer.Length)
				{
					Utils.server = null;
					ServerListScreen.ipSelect = index;
					Rms.saveRMSInt("svselect", index);
					return;
				}
			}

			Utils.server = null;
			ServerListScreen.ipSelect = 0;
			Rms.saveRMSInt("svselect", 0);
		}

		void SendCharacterInfo()
		{
			try
			{
				Char myChar = Char.myCharz();
				Char myPet = Char.myPetz();

				if (myChar == null || string.IsNullOrEmpty(myChar.cName))
				{
					return;
				}

				CharacterInfoMessage message = CharacterInfoMessage.Create(myChar, myPet);
				if (message != null)
				{
					SendMessage(message);
				}
			}
			catch (Exception ex)
			{
				WriteLog("Error sending char info: " + ex);
			}
		}

		/// <summary>
		///     If the current screen is ServerListScreen, auto-trigger login with the launcher's credentials.
		/// </summary>
		internal void TryAutoLogin()
		{
			if (_autoLoginDone || !_credentialsReady)
				return;

			if (GameCanvas.currentScreen is ServerListScreen && GameCanvas.serverScreen != null)
			{
				_autoLoginDone = true;
				ApplyServerSelection(Server);
				Rms.saveRMSString("acc", Utils.username);
				if (!string.IsNullOrEmpty(Utils.password))
				{
					Rms.saveRMSString("pass", Utils.password);
				}
				GameCanvas.serverScreen.perform(3, null);
				DelayedAction.ScheduleRepeating(30f, SendCharacterInfo);
			}
		}

		public void SendMessage(object obj)
		{
			if (obj == null)
				return;

			ClientWebSocket ws = _ws;
			if (ws == null || ws.State != WebSocketState.Open)
				return;

			try
			{
				string json = JsonConvert.SerializeObject(obj);
				SendRawText(json);
			}
			catch (Exception ex)
			{
				WriteLog("Send error: " + ex);
			}
		}

		void SendRawText(string text)
		{
			ClientWebSocket ws = _ws;
			if (ws == null || ws.State != WebSocketState.Open)
				return;

			CancellationTokenSource cts = _cts;
			if (!TryGetToken(cts, out CancellationToken token))
				return;

			byte[] bytes = Encoding.UTF8.GetBytes(text);
			_ = SendBytesAsync(ws, bytes, token);
		}

		async System.Threading.Tasks.Task SendBytesAsync(ClientWebSocket ws, byte[] bytes, CancellationToken cancellationToken)
		{
			bool acquired = false;
			try
			{
				await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
				acquired = true;

				if (ws == null || ws.State != WebSocketState.Open)
					return;

				await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Expected on shutdown
			}
			catch (ObjectDisposedException)
			{
				// Expected on shutdown
			}
			catch (Exception ex)
			{
				WriteLog("Send error: " + ex);
			}
			finally
			{
				if (acquired)
				{
					_sendLock.Release();
				}
			}
		}

		public void Close()
		{
			ClientWebSocket ws = Interlocked.Exchange(ref _ws, null);
			CancellationTokenSource cts = Interlocked.Exchange(ref _cts, null);
			IsConnected = false;

			if (ws == null)
			{
				try { cts?.Cancel(); } catch (ObjectDisposedException) { } catch { }
				return;
			}

			string closeJson = JsonConvert.SerializeObject(
				string.IsNullOrEmpty(IpcToken)
					? (object)new { action = "close-socket" }
					: new { action = "close-socket", token = IpcToken }
			);
			byte[] closeBytes = Encoding.UTF8.GetBytes(closeJson);

			_ = System.Threading.Tasks.Task.Run(async () =>
			{
				bool acquired = false;
				try
				{
					using (CancellationTokenSource closeTimeout = new CancellationTokenSource(1500))
					{
						await _sendLock.WaitAsync(closeTimeout.Token).ConfigureAwait(false);
						acquired = true;

						if (ws.State == WebSocketState.Open)
						{
							await ws.SendAsync(new ArraySegment<byte>(closeBytes), WebSocketMessageType.Text, true, closeTimeout.Token)
								.ConfigureAwait(false);
							await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Client closing", closeTimeout.Token)
								.ConfigureAwait(false);
						}
					}
				}
				catch (Exception ex)
				{
					WriteLog("Error closing: " + ex);
				}
				finally
				{
					if (acquired)
					{
						_sendLock.Release();
					}
					try { cts?.Cancel(); } catch (ObjectDisposedException) { } catch { }
					try { ws.Dispose(); } catch { }
					WriteLog("WebSocket connection closed.");
				}
			});
		}

		void WriteLog(string log)
		{
			try
			{
				// File.AppendAllText(pathLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {log}\n");
			}
			catch
			{
				// Ignore logging errors
			}
		}
	}
}
