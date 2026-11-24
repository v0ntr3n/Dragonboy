using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Assembly_CSharp.Mod.BoMong;
using Mod.AccountManager;
using Mod.Auto;
using Mod.CharEffect;
using Mod.CSKB;
using Mod.CustomPanel;
using Mod.Graphics;
using Mod.ListChar;
using Mod.ModHelper;
using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.ModMenu;
using Mod.PickMob;
using Mod.R;
using Mod.TeleportMenu;
using Mod.Xmap;
using Mod.Yardat;
using Newtonsoft.Json.Linq;
using UnityEngine;
using CharacterInfo = Mod.AccountManager.CharacterInfo;

namespace Mod
{
	internal static class GameEvents
	{
		static float _previousWidth = -1f;
		static float _previousHeight = -1f;
		static bool isHaveSelectSkill_old;
		static long lastTimeGamePause;
		static long lastTimeRequestPetInfo;
		static long delayRequestPetInfo = 1000;
		static long lastTimeRequestZoneInfo;
		static long delayRequestZoneInfo = 100;
		static bool isFirstPause = true;
		static bool isOpenZoneUI;
		static bool _isBackgroundZonePoll;
		internal static bool IsBackgroundPetInfoPoll;
		static bool _hasSavedSelectSkill;
		static GUIStyle _hpStyle;
		static GUIStyle _mpStyle;
		static int _hudStyleZoomLevel = -1;
		static float _lastFpsSampleTime = -1f;
		static string _cachedFpsStr = "0.0";
		static bool _rmsBootstrapResetDone;
		internal const string TEAMOBI2026_SERVER_NAME = "Ngọc Rồng Teamobi2026";
		internal const string TEAMOBI2026_HOST = "127.0.0.1";
		internal const int TEAMOBI2026_PORT = 14445;
		internal const string TEAMOBI2026_SERVER_LIST = "Ngọc Rồng Teamobi2026:127.0.0.1:14445:0:0:0,0,0";

		static string nameCustomServer = "";
		static string currentHost = TEAMOBI2026_HOST;
		static ushort currentPort = (ushort)TEAMOBI2026_PORT;

		static FieldInfo _midletIpField;
		static FieldInfo _midletPortField;
		static bool _endpointReflectionInitialized;
		static int _splashTicksObserved;
		static bool _splashLoadIpTriggered;
		static bool _splashCompleted;
		static long _lastHasRequiredResourcesCheckMs;

		internal static void EnsureMainThreadRegistered()
		{
			if (Main.mainThreadId == 0)
			{
				Main.mainThreadId = Thread.CurrentThread.ManagedThreadId;
			}
			if (Thread.CurrentThread.Name != "Main")
			{
				try
				{
					Thread.CurrentThread.Name = "Main";
				}
				catch
				{
				}
			}
			if (string.IsNullOrEmpty(Main.mainThreadName))
			{
				Main.mainThreadName = Thread.CurrentThread.Name;
			}
			if (!_rmsBootstrapResetDone)
			{
				_rmsBootstrapResetDone = true;
				if (Rms.status != 0)
				{
					Rms.status = 0;
				}
			}
		}

		static void EnsureEndpointReflection()
		{
			if (_endpointReflectionInitialized)
				return;
			_endpointReflectionInitialized = true;
			const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			_midletIpField = typeof(GameMidlet).GetField("IP", staticFlags);
			_midletPortField = typeof(GameMidlet).GetField("PORT", staticFlags);
		}

		static bool IsLegacyOrWildcardHost(string candidate)
		{
			if (string.IsNullOrEmpty(candidate))
				return true;
			string trimmed = candidate.Trim();
			return trimmed == "0.0.0.0"
				|| trimmed.IndexOf("teamobi.com", StringComparison.OrdinalIgnoreCase) >= 0
				|| trimmed.IndexOf("indonaga.com", StringComparison.OrdinalIgnoreCase) >= 0
				|| trimmed.StartsWith("112.213.", StringComparison.Ordinal)
				|| trimmed.StartsWith("210.211.", StringComparison.Ordinal)
				|| trimmed.StartsWith("27.0.", StringComparison.Ordinal)
				|| trimmed.StartsWith("52.74.", StringComparison.Ordinal);
		}

		internal static void SyncGameMidletEndpoints(string host, int port)
		{
			EnsureEndpointReflection();
			string safeHost = IsLegacyOrWildcardHost(host) ? TEAMOBI2026_HOST : host;
			int safePort = port > 0 ? port : TEAMOBI2026_PORT;
			try
			{
				_midletIpField?.SetValue(null, safeHost);
				if (_midletPortField != null)
				{
					_midletPortField.SetValue(null, Convert.ChangeType(safePort, _midletPortField.FieldType));
				}
			}
			catch
			{
			}
			if (IsLegacyOrWildcardHost(GameMidlet.IP2))
			{
				GameMidlet.IP2 = safeHost;
			}
			if (GameMidlet.PORT2 <= 0)
			{
				GameMidlet.PORT2 = safePort;
			}
		}

		static bool _suboiReflectionScanned;
		static FieldInfo[] _websiteStringFields;

		internal static void EnforceSuboiLabel(bool rescan = false)
		{
			ServerListScreen.linkweb = "suboi";
			if (rescan || !_suboiReflectionScanned)
			{
				_suboiReflectionScanned = true;
				try
				{
					var found = new System.Collections.Generic.List<FieldInfo>();
					const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
					Type[] targets = { typeof(ServerListScreen), typeof(mResources), typeof(LoginScr) };
					for (int t = 0; t < targets.Length; t++)
					{
						FieldInfo[] fields = targets[t].GetFields(flags);
						for (int i = 0; i < fields.Length; i++)
						{
							FieldInfo f = fields[i];
							if (f.FieldType == typeof(string) && !f.IsLiteral && !f.IsInitOnly)
							{
								string val = f.GetValue(null) as string;
								if (!string.IsNullOrEmpty(val) && (val.IndexOf("ngocrongonline", StringComparison.OrdinalIgnoreCase) >= 0 || val.IndexOf("dragonbabe", StringComparison.OrdinalIgnoreCase) >= 0))
								{
									found.Add(f);
								}
							}
						}
					}
					_websiteStringFields = found.ToArray();
				}
				catch
				{
					_websiteStringFields = Array.Empty<FieldInfo>();
				}
			}
			if (_websiteStringFields != null)
			{
				for (int i = 0; i < _websiteStringFields.Length; i++)
				{
					try
					{
						if (!string.Equals(_websiteStringFields[i].GetValue(null) as string, "suboi", StringComparison.Ordinal))
						{
							_websiteStringFields[i].SetValue(null, "suboi");
						}
					}
					catch
					{
					}
				}
			}
		}

		internal static void ApplyStaticServerDefaultsOnly()
		{
			ServerListScreen.smartPhoneVN = TEAMOBI2026_SERVER_LIST;
			ServerListScreen.javaVN = TEAMOBI2026_SERVER_LIST;
			ServerListScreen.linkDefault = TEAMOBI2026_SERVER_LIST;
			EnforceSuboiLabel(true);
			try
			{
				foreach (FieldInfo field in typeof(ServerListScreen).GetFields(BindingFlags.Public | BindingFlags.Static))
				{
					if (field.FieldType == typeof(string) && !field.IsLiteral && !field.IsInitOnly)
					{
						string val = field.GetValue(null) as string;
						if (!string.IsNullOrEmpty(val) && (val.Contains(":14445:") || val.Contains("teamobi.com") || val.Contains("112.213.") || val.Contains("indonaga.com")))
						{
							field.SetValue(null, TEAMOBI2026_SERVER_LIST);
						}
					}
				}
			}
			catch
			{
			}
			SyncGameMidletEndpoints(TEAMOBI2026_HOST, TEAMOBI2026_PORT);
		}

		internal static bool HasRequiredResources()
		{
			try
			{
				int zoom = mGraphics.zoomLevel > 0 ? mGraphics.zoomLevel : 2;
				string basePath = Rms.GetiPhoneDocumentsPath();
				for (int i = 0; i <= 5; i++)
				{
					string path = Path.Combine(basePath, "x" + zoom + "Big" + i);
					if (!File.Exists(path) || new FileInfo(path).Length <= 0)
						return false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static void EnsureResVersionReady()
		{
			try
			{
				if (!HasRequiredResources())
				{
					Rms.DeleteStorage(Rms.RMS_ResVersion);
				}
			}
			catch
			{
			}
		}

		internal static void ApplyTeamobi2026ServerConfig()
		{
			EnsureMainThreadRegistered();
			ApplyStaticServerDefaultsOnly();
			EnsureResVersionReady();
			bool alreadyConfigured = ServerListScreen.address != null
				&& ServerListScreen.address.Length > 0
				&& ServerListScreen.ipSelect >= 0
				&& ServerListScreen.ipSelect < ServerListScreen.address.Length
				&& ServerListScreen.address[ServerListScreen.ipSelect] == TEAMOBI2026_HOST
				&& ServerListScreen.port != null
				&& ServerListScreen.ipSelect < ServerListScreen.port.Length
				&& ServerListScreen.port[ServerListScreen.ipSelect] == TEAMOBI2026_PORT;
			if (!alreadyConfigured)
			{
				ServerListScreen.getServerList(TEAMOBI2026_SERVER_LIST);
				ServerListScreen.ipSelect = 0;
				Rms.saveRMSInt("svselect", 0);
			}
			LoginScr.serverName = TEAMOBI2026_SERVER_NAME;
			SyncGameMidletEndpoints(TEAMOBI2026_HOST, TEAMOBI2026_PORT);
		}

		static void CompleteSplashAndOpenServerList()
		{
			if (GameCanvas.instance == null || GameCanvas.serverScreen == null)
				return;
			_splashCompleted = true;
			try
			{
				ApplyTeamobi2026ServerConfig();
				SplashScr.imgLogo = null;
				bool hasRes = HasRequiredResources();
				ServerListScreen.bigOk = hasRes;
				ServerListScreen.loadScreen = hasRes;
				ServerListScreen.isGetData = !hasRes;
				ServerListScreen.waitToLogin = false;
				if (hasRes)
				{
					GameCanvas.loadBG(0);
					GameCanvas.instance.perform(9000, null);
					if (GameCanvas.currentScreen != GameCanvas.serverScreen)
					{
						GameCanvas.serverScreen.switchToMe();
					}
				}
				else
				{
					GameCanvas.endDlg();
					GameCanvas.serverScreen.show2();
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			if (GameCanvas.currentScreen is SplashScr || GameCanvas.currentScreen == null)
			{
				try
				{
					bool hasRes = HasRequiredResources();
					ServerListScreen.loadScreen = hasRes;
					ServerListScreen.isGetData = !hasRes;
					GameCanvas.loadBG(0);
					GameCanvas.clearKeyPressed();
					GameCanvas.clearKeyHold();
					GameCanvas.serverScreen.initCommand();
					GameCanvas.currentScreen = GameCanvas.serverScreen;
					if (hasRes)
					{
						OnServerListScreenLoaded(GameCanvas.serverScreen);
					}
					else
					{
						GameCanvas.endDlg();
						GameCanvas.serverScreen.show2();
					}
				}
				catch (Exception ex2)
				{
					Debug.LogException(ex2);
				}
			}
		}

		static string NormalizeServerListPayload(string rawList)
		{
			if (string.IsNullOrEmpty(rawList))
				return TEAMOBI2026_SERVER_LIST;
			string[] tokens = rawList.Split(',');
			bool changed = false;
			for (int i = 0; i < tokens.Length; i++)
			{
				if (tokens[i].IndexOf(':') < 0)
					continue;
				string[] cols = tokens[i].Split(':');
				if (cols.Length >= 3 && IsLegacyOrWildcardHost(cols[1]))
				{
					cols[1] = TEAMOBI2026_HOST;
					changed = true;
				}
				if (cols.Length == 4)
				{
					tokens[i] = string.Join(":", cols) + ":0:0";
					changed = true;
				}
				else if (cols.Length == 5)
				{
					tokens[i] = string.Join(":", cols) + ":0";
					changed = true;
				}
				else if (changed)
				{
					tokens[i] = string.Join(":", cols);
				}
			}
			return changed ? string.Join(",", tokens) : rawList;
		}

		static sbyte[] ExtractIncomingMessageBytes(Message msg)
		{
			if (msg == null)
				return null;
			try
			{
				myReader r = msg.reader();
				if (r != null)
				{
					return r.buffer;
				}
			}
			catch
			{
			}
			return null;
		}

		static void NormalizeQueueServerListMessages(MyVector queue, object queueLock)
		{
			if (queue == null || queueLock == null)
				return;
			try
			{
				lock (queueLock)
				{
					int count = queue.size();
					if (count <= 0)
						return;
					for (int i = 0; i < count; i++)
					{
						Message msg = queue.elementAt(i) as Message;
						if (msg == null || msg.command != -29)
							continue;
						sbyte[] data = ExtractIncomingMessageBytes(msg);
						if (data == null || data.Length < 4 || data[0] != 2)
							continue;
						int utfLen = ((data[1] & 0xFF) << 8) | (data[2] & 0xFF);
						if (utfLen <= 0 || 3 + utfLen > data.Length)
							continue;
						byte[] utfBytes = new byte[utfLen];
						for (int k = 0; k < utfLen; k++)
						{
							utfBytes[k] = unchecked((byte)data[3 + k]);
						}
						string decoded = System.Text.Encoding.UTF8.GetString(utfBytes);
						string normalized = NormalizeServerListPayload(decoded);
						if (normalized == decoded)
							continue;
						byte[] newUtf = System.Text.Encoding.UTF8.GetBytes(normalized);
						if (newUtf.Length > 65535)
							continue;
						int tailLen = data.Length - (3 + utfLen);
						sbyte[] rebuilt = new sbyte[3 + newUtf.Length + tailLen];
						rebuilt[0] = 2;
						rebuilt[1] = unchecked((sbyte)((newUtf.Length >> 8) & 0xFF));
						rebuilt[2] = unchecked((sbyte)(newUtf.Length & 0xFF));
						for (int k = 0; k < newUtf.Length; k++)
						{
							rebuilt[3 + k] = unchecked((sbyte)newUtf[k]);
						}
						for (int k = 0; k < tailLen; k++)
						{
							rebuilt[3 + newUtf.Length + k] = data[3 + utfLen + k];
						}
						queue.setElementAt(new Message(-29, rebuilt), i);
					}
				}
			}
			catch
			{
			}
		}

		internal static void PreProcessIncomingNetworkQueue()
		{
			NormalizeQueueServerListMessages(Session_ME.recieveMsg, Session_ME.RecieveLock);
			NormalizeQueueServerListMessages(Session_ME2.recieveMsg, Session_ME2.RecieveLock);
			if (IsLegacyOrWildcardHost(GameMidlet.IP2))
			{
				GameMidlet.IP2 = currentHost;
			}
			if (GameMidlet.PORT2 <= 0)
			{
				GameMidlet.PORT2 = currentPort > 0 ? currentPort : TEAMOBI2026_PORT;
			}
		}
		static bool _hasTickSnapshot;
		static bool _isInterpolatingPose;
		static int _prevCx, _prevCy, _authCx, _authCy, _interpCx, _interpCy;
		static int _prevCmx, _prevCmy, _authCmx, _authCmy, _interpCmx, _interpCmy;

		static void UnlockMainPaintGate()
		{
			SmoothFrame180Hz.UnlockMainPaintGate();
		}

		internal static void RestoreAuthoritativePose()
		{
			if (!_hasTickSnapshot || !_isInterpolatingPose)
				return;
			Char me = Char.myCharz();
			if (me != null && _isInterpolatingPose)
			{
				if (me.cx == _interpCx && me.cy == _interpCy)
				{
					me.cx = _authCx;
					me.cy = _authCy;
				}
				else
				{
					_prevCx = _authCx = me.cx;
					_prevCy = _authCy = me.cy;
				}
			}
			if (_isInterpolatingPose)
			{
				if (GameScr.cmx == _interpCmx && GameScr.cmy == _interpCmy)
				{
					GameScr.cmx = _authCmx;
					GameScr.cmy = _authCmy;
				}
				else
				{
					_prevCmx = _authCmx = GameScr.cmx;
					_prevCmy = _authCmy = GameScr.cmy;
				}
			}
			_isInterpolatingPose = false;
		}

		internal static void BeginFixedTick()
		{
			RestoreAuthoritativePose();
			if (!_hasTickSnapshot)
				return;
			Char me = Char.myCharz();
			if (me != null)
			{
				_prevCx = _authCx = me.cx;
				_prevCy = _authCy = me.cy;
			}
			_prevCmx = _authCmx = GameScr.cmx;
			_prevCmy = _authCmy = GameScr.cmy;
		}

		internal static void RecordAuthoritativePoseAfterTick()
		{
			Char me = Char.myCharz();
			if (me == null)
				return;
			_authCx = me.cx;
			_authCy = me.cy;
			_authCmx = GameScr.cmx;
			_authCmy = GameScr.cmy;
			if (!_hasTickSnapshot || Mathf.Abs(_authCx - _prevCx) > 48 || Mathf.Abs(_authCy - _prevCy) > 48)
			{
				_prevCx = _authCx;
				_prevCy = _authCy;
			}
			if (!_hasTickSnapshot || Mathf.Abs(_authCmx - _prevCmx) > 96 || Mathf.Abs(_authCmy - _prevCmy) > 96)
			{
				_prevCmx = _authCmx;
				_prevCmy = _authCmy;
			}
			_hasTickSnapshot = true;
			_isInterpolatingPose = false;
		}

		internal static void ApplySubTickPoseForRender()
		{
			UnlockMainPaintGate();
			if (!_hasTickSnapshot || !(GameCanvas.currentScreen is GameScr) || Char.isLoadingMap || Char.ischangingMap)
			{
				_isInterpolatingPose = false;
				return;
			}
			Char me = Char.myCharz();
			if (me == null)
				return;
			if (!_isInterpolatingPose)
			{
				if (me.cx != _authCx || me.cy != _authCy)
				{
					_prevCx = _authCx = me.cx;
					_prevCy = _authCy = me.cy;
				}
				if (GameScr.cmx != _authCmx || GameScr.cmy != _authCmy)
				{
					_prevCmx = _authCmx = GameScr.cmx;
					_prevCmy = _authCmy = GameScr.cmy;
				}
			}
			else
			{
				if (me.cx != _interpCx || me.cy != _interpCy)
				{
					_prevCx = _authCx = me.cx;
					_prevCy = _authCy = me.cy;
				}
				if (GameScr.cmx != _interpCmx || GameScr.cmy != _interpCmy)
				{
					_prevCmx = _authCmx = GameScr.cmx;
					_prevCmy = _authCmy = GameScr.cmy;
				}
			}
			float fixedDt = (Time.fixedDeltaTime > 0.0001f) ? Time.fixedDeltaTime : 0.02f;
			float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / fixedDt);
			_interpCx = Mathf.RoundToInt(Mathf.Lerp(_prevCx, _authCx, alpha));
			_interpCy = Mathf.RoundToInt(Mathf.Lerp(_prevCy, _authCy, alpha));
			_interpCmx = Mathf.RoundToInt(Mathf.Lerp(_prevCmx, _authCmx, alpha));
			_interpCmy = Mathf.RoundToInt(Mathf.Lerp(_prevCmy, _authCmy, alpha));
			me.cx = _interpCx;
			me.cy = _interpCy;
			GameScr.cmx = _interpCmx;
			GameScr.cmy = _interpCmy;
			_isInterpolatingPose = true;
		}
		static readonly string[] QwertySkillHotkeys =
		{
			"1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
		};
		static readonly string[] NonQwertySkillHotkeys =
		{
			"1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
		};

		private static bool _awakeInitialized;
		private static bool _gameStartInitialized;

		internal static void SyncTeleportPose(int x, int y)
		{
			_prevCx = _authCx = _interpCx = x;
			_prevCy = _authCy = _interpCy = y;
			_isInterpolatingPose = false;
		}

		static void MuteAllGameAudio()
		{
			AudioListener.volume = 0f;
			AudioListener.pause = true;
			GameCanvas.isPlaySound = false;
			Sound.isSound = false;
			Sound.isNotPlay = true;
			Sound.stopAll = true;
			SoundMn.isSound = false;
			SoundMn.volume = 0f;
		}

		internal static void OnAwake()
		{
			if (_awakeInitialized)
				return;
			_awakeInitialized = true;
			EnsureMainThreadRegistered();
			MuteAllGameAudio();
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
			Application.runInBackground = true;
			Time.timeScale = 2f;
			Time.fixedDeltaTime = 0.02f;
			OnSetResolution();
		}

		internal static void OnGameStart()
		{
			if (_gameStartInitialized)
				return;
			_gameStartInitialized = true;
			EnsureMainThreadRegistered();
			MuteAllGameAudio();
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
			if (Utils.IsMobile())
			{
				QualitySettings.vSyncCount = 1;
				Screen.sleepTimeout = SleepTimeout.NeverSleep;
				Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
				Screen.orientation = ScreenOrientation.AutoRotation;
				Screen.autorotateToLandscapeLeft = true;
				Screen.autorotateToLandscapeRight = true;
				Screen.autorotateToPortrait = false;
				Screen.autorotateToPortraitUpsideDown = false;
			}
			OnSetResolution();
			OnCheckZoomLevel(Screen.width, Screen.height);
			ModStorage.EnsureCommonDataDirectory();
			ApplyTeamobi2026ServerConfig();
			YardatSettings.Load();
			AutoPean.LoadSettings();
			CharEffectMain.Init();
			ChatCommandHandler.loadDefault();
			HotkeyCommandHandler.loadDefault();
			GraphicsReducer.InitializeTileMap(true);
			PickMobConfig.Init();
			SpaceshipSkip.isEnabled = true;
			InGameAccountManager.OnStart();
			AutoSkill.gI.Toggle(true);
			AutoPean.gI.Toggle(true);

			UIReportersManager.AddReporter(Boss.Paint);
			UIReportersManager.AddReporter(ListCharsInMap.Paint);

			GameLauncherClient launcher = GameLauncherClient.Instance;
			launcher.ParseStartupArgs();
			if (launcher.IsLaunchedByLauncher())
			{
				launcher.Connect();
			}
		}

		internal static void OnMainStart()
		{
			ApplyTeamobi2026ServerConfig();
		}

		internal static void OnGamePause(bool paused)
		{
			if (mSystem.currentTimeMillis() - lastTimeGamePause > 1000 && !isFirstPause)
			{
				if (!Utils.IsOpenedByExternalAccountManager)
				{
					InGameAccountManager.OnCloseAndPause();
				}
			}

			lastTimeGamePause = mSystem.currentTimeMillis();
			if (isFirstPause)
			{
				isFirstPause = false;
			}
		}

		internal static void OnGameClosing()
		{
			TeleportMenuMain.SaveData();

			if (GameLauncherClient.Instance.IsConnected)
			{
				GameLauncherClient.Instance.Close();
			}

			if (!Utils.IsOpenedByExternalAccountManager)
			{
				InGameAccountManager.OnCloseAndPause();
			}

			UIReportersManager.ClearReporters();

			Session_ME.gI().close();
			Session_ME2.gI().close();
		}

		internal static void OnFixedUpdateMain()
		{
			EnsureMainThreadRegistered();
			UnlockMainPaintGate();
			if (!string.Equals(ServerListScreen.linkweb, "suboi", StringComparison.Ordinal))
			{
				EnforceSuboiLabel(false);
			}
			if (ServerListScreen.address == null || ServerListScreen.address.Length == 0 || string.IsNullOrEmpty(ServerListScreen.address[0]) || (InGameAccountManager.SelectedAccount == null && IsLegacyOrWildcardHost(ServerListScreen.address[0])))
			{
				ApplyTeamobi2026ServerConfig();
			}
			if (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0 && (ServerListScreen.ipSelect < 0 || ServerListScreen.ipSelect >= ServerListScreen.nameServer.Length))
			{
				ServerListScreen.ipSelect = 0;
			}
			if (!_splashCompleted && GameCanvas.currentScreen is SplashScr)
			{
				_splashTicksObserved++;
				if ((_splashLoadIpTriggered && _splashTicksObserved >= 32) || _splashTicksObserved >= 36)
				{
					CompleteSplashAndOpenServerList();
				}
			}
			else
			{
				if (!(GameCanvas.currentScreen is SplashScr))
				{
					_splashTicksObserved = 0;
				}
				if (!_splashCompleted && GameCanvas.currentScreen == null && GameCanvas.serverScreen != null && GameCanvas.gameTick > 40)
				{
					CompleteSplashAndOpenServerList();
				}
			}
			if (GameCanvas.currentScreen is ServerListScreen && !ServerListScreen.loadScreen && !ServerListScreen.isGetData)
			{
				long nowMs = mSystem.currentTimeMillis();
				if (nowMs - _lastHasRequiredResourcesCheckMs >= 500)
				{
					_lastHasRequiredResourcesCheckMs = nowMs;
					if (HasRequiredResources())
					{
						EnsureResVersionReady();
						ServerListScreen.loadScreen = true;
						GameCanvas.loadBG(0);
						if (GameCanvas.serverScreen != null && (GameCanvas.serverScreen.cmd == null || GameCanvas.serverScreen.cmd.Length == 0))
						{
							GameCanvas.serverScreen.initCommand();
						}
					}
				}
			}
			if (GameCanvas.currentScreen != null)
			{
				if (!GameCanvas.panel.isShow && GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
				{
					GameCanvas.isFocusPanel2 = true;
					GameCanvas.panel2?.update();
					if (GameCanvas.panel2?.chatTField != null && GameCanvas.panel2.chatTField.isShow)
						GameCanvas.panel2?.chatTFUpdateKey();
					else
						GameCanvas.panel2?.updateKey();
				}

				if (!GameCanvas.panel.isShow && GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
				{
					int panel2DrawX = GameCanvas.panel2.X + GameCanvas.panel2.X - GameCanvas.panel2.cmx;
					if (!GameCanvas.isPointer(panel2DrawX, GameCanvas.panel2.Y, GameCanvas.panel2.W, GameCanvas.panel2.H) &&
					    GameCanvas.isPointerJustRelease && GameCanvas.panel2.isDoneCombine &&
					    !GameCanvas.panel2.pointerIsDowning)
					{
						GameCanvas.panel2.hide();
						GameCanvas.clearAllPointerEvent();
					}
				}
			}
		}

		internal static void OnUpdateMain()
		{
			if (!Main.started)
				return;
			UnlockMainPaintGate();
			if (GameCanvas.instance == null || MotherCanvas.instance == null)
				return;
			if (!_previousWidth.Equals(Screen.width) || !_previousHeight.Equals(Screen.height))
			{
				_previousWidth = Screen.width;
				_previousHeight = Screen.height;
				ScaleGUI.initScaleGUI();
				GameCanvas.instance.ResetSize();
				if (GameCanvas.currentScreen == GameCanvas.serverScreen)
				{
					GameCanvas.serverScreen?.initCommand();
				}
				Utils.ResetTextField(ChatTextField.gI());
				GameScr.gamePad?.SetGamePadZone();
				GameScr.loadCamera(false, -1, -1);
				if (GameCanvas.panel2 != null)
				{
					GameCanvas.panel2.EmulateSetTypePanel(1);
				}
				ModMenuMain.UpdatePosition();
				InGameAccountManager.UpdateSizeAndPos();
			}
		}

		internal static void OnSaveRMSString(ref string filename, ref string data)
		{
			if (filename.StartsWith("userAo") && string.IsNullOrEmpty(data))
				filename = "";
		}

		internal static void OnRmsFileIo(Action action)
		{
			ModStorage.ExecuteWithCrossProcessDataLock(action);
		}

		internal static void OnLoadLanguage(sbyte newLanguage)
		{
			Strings.LoadLanguage(newLanguage);
			ModMenuMain.UpdateLanguage(newLanguage);
			EnforceSuboiLabel(true);
		}

		internal static void OnSetResolution()
		{
			if (Utils.IsMobile())
				return;
			int width = 1024;
			int height = 600;
			bool fullScreen = false;
			if (Utils.sizeData != null)
			{
				try
				{
					if (Utils.sizeData.TryGetValue("width", out JToken widthToken) && widthToken != null && widthToken.Type != JTokenType.Null)
					{
						int parsedWidth = widthToken.Value<int>();
						if (parsedWidth > 0)
							width = parsedWidth;
					}
					if (Utils.sizeData.TryGetValue("height", out JToken heightToken) && heightToken != null && heightToken.Type != JTokenType.Null)
					{
						int parsedHeight = heightToken.Value<int>();
						if (parsedHeight > 0)
							height = parsedHeight;
					}
					if (Utils.sizeData.TryGetValue("fullScreen", out JToken fsToken) && fsToken != null && fsToken.Type != JTokenType.Null)
					{
						fullScreen = fsToken.Value<bool>();
					}
				}
				catch
				{
				}
			}
			FullScreenMode mode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
			if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != mode || Screen.fullScreen != fullScreen)
			{
				Screen.SetResolution(width, height, mode);
			}
		}

		internal static void OnGameScrPressHotkeysUnassigned()
		{
			if (HotkeyCommandHandler.handleHotkey(GameCanvas.keyAsciiPress))
			{
				GameCanvas.keyAsciiPress = 0;
			}
		}

		internal static void OnGameScrPressHotkeys()
		{

		}

		internal static bool OnSendChat(string text)
		{
			return ChatCommandHandler.handleChatText(text);
		}

		internal static void OnPaintChatTextField(ChatTextField instance, mGraphics g)
		{

		}

		internal static bool OnStartChatTextField(ChatTextField sender, IChatable parentScreen)
		{
			sender.parentScreen = parentScreen;
			return false;
		}

		internal static bool OnGetRMSPath(out string result)
		{
			string subFolder = "TeaMobi";
			result = ModStorage.RootDataPath;
			result = Path.Combine(result, subFolder);
			if (!Directory.Exists(result))
				Directory.CreateDirectory(result);
			return true;
		}

		internal static bool OnTeleportUpdate(Teleport teleport)
		{
			if (SpaceshipSkip.isEnabled)
			{
				SpaceshipSkip.Update(teleport);
				return true;
			}

			return false;
		}

		internal static void OnUpdateChatTextField(ChatTextField sender)
		{
			if (!string.IsNullOrEmpty(sender.tfChat.getText()))
				GameCanvas.keyPressed[14] = false;
		}

		internal static bool OnClearAllRMS()
		{
			foreach (FileInfo file in new DirectoryInfo(Rms.GetiPhoneDocumentsPath() + "/").GetFiles()
				         .Where(f => f.Extension != ".log"))
				try
				{
					if (file.Name != "isPlaySound")
						file.Delete();
				}
				catch
				{
				}

			EnsureResVersionReady();
			Rms.saveRMSInt("svselect", 0);
			return true;
		}

		internal static void OnUpdateGameScr()
		{
			int syncInterval = Mathf.Max(1, Mathf.RoundToInt(60f * Time.timeScale));
			Char myChar = Char.myCharz();
			if (!Utils.IsOpenedByExternalAccountManager && myChar != null && GameCanvas.gameTick % syncInterval == 0)
			{
				Account account = InGameAccountManager.SelectedAccount;
				if (account != null)
				{
					account.Gold = myChar.xu;
					account.Gem = myChar.luong;
					account.Ruby = myChar.luongKhoa;

					if (account.Info == null)
					{
						account.Info = new CharacterInfo();
					}
					account.Info.Name = myChar.cName;
					account.Info.CharID = myChar.charID;
					account.Info.Gender = (sbyte)myChar.cgender;
					account.Info.EXP = myChar.cPower;
					account.Info.MaxHP = myChar.cHPFull;
					account.Info.MaxMP = myChar.cMPFull;
					account.Info.Icon = myChar.avatarz();
					if (myChar.havePet && Char.myPet != null && !string.IsNullOrEmpty(Char.myPet.cName) && Char.myPet.cHPFull > 0)
					{
						Char myPet = Char.myPet;
						if (account.PetInfo == null)
						{
							account.PetInfo = new CharacterInfo();
						}
						account.PetInfo.Name = myPet.cName;
						account.PetInfo.CharID = myPet.charID;
						account.PetInfo.Gender = (sbyte)Utils.GetPetGender();
						account.PetInfo.EXP = myPet.cPower;
						account.PetInfo.MaxHP = myPet.cHPFull;
						account.PetInfo.MaxMP = myPet.cMPFull;
						account.PetInfo.Icon = myPet.avatarz();
					}
				}
			}

			if (myChar != null && myChar.havePet && mSystem.currentTimeMillis() - lastTimeRequestPetInfo > delayRequestPetInfo)
			{
				delayRequestPetInfo = Res.random(750, 1000);
				lastTimeRequestPetInfo = mSystem.currentTimeMillis();
				if (!Panel.pendingUserPetInfoRequest)
					IsBackgroundPetInfoPoll = true;
				Service.gI().petInfo();
			}

			if (!isOpenZoneUI && mSystem.currentTimeMillis() - lastTimeRequestZoneInfo > delayRequestZoneInfo)
			{
				delayRequestZoneInfo = Res.random(2000, 3000);
				lastTimeRequestZoneInfo = mSystem.currentTimeMillis();
				_isBackgroundZonePoll = true;
				Service.gI().openUIZone();
			}

			if (myChar != null)
			{
				myChar.cspeed = Utils.myCharSpeed;
			}
			Time.timeScale = Utils.timeScale;
			Boss.Update();
			ListCharsInMap.Update();
			CharEffectMain.Update();
			TeleportMenuMain.Update();
			AutoTrainPet.Update();
			AutoSellTrashItems.Update();
			DelayedAction.Tick();
			AutoUseItem.Update();
			RecordAuthoritativePoseAfterTick();
		}

		internal static void OnRegisterSuccess(string username, string password, string upgradedGuest)
		{
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				InGameAccountDAO.UpsertAndSelectRegisteredAccount(username, password, upgradedGuest, ServerListScreen.ipSelect);
			}
		}

		internal static void OnLogin(ref string username, ref string pass, ref sbyte type)
		{
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				if (type == 1)
				{
					InGameAccountManager.ResetSelectedAccountIndex();
					Rms.DeleteStorage("acc");
					Rms.DeleteStorage("pass");
					return;
				}

				Account acc = InGameAccountManager.SelectedAccount;
				if (acc == null)
					return;
				type = (sbyte)acc.Type;
				username = acc.Username;
				if (acc.Type == AccountType.Registered)
				{
					pass = acc.Password;
				}
				else
				{
					pass = string.Empty;
					int serverIndex = acc.Server != null ? acc.Server.index : ServerListScreen.ipSelect;
					Rms.DeleteStorage("userAo" + serverIndex);
				}

				acc.LastTimeLogin = DateTime.Now;
			}
			else
			{
				username = string.IsNullOrEmpty(Utils.username) ? username : Utils.username;
				if (!string.IsNullOrEmpty(username) && username.StartsWith("User", StringComparison.Ordinal))
				{
					pass = string.Empty;
					type = 1;
				}
				else
				{
					pass = string.IsNullOrEmpty(Utils.password) ? pass : Utils.password;
				}
			}
			if (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0)
			{
				ServerListScreen.ipSelect = Mathf.Clamp(ServerListScreen.ipSelect, 0, ServerListScreen.nameServer.Length - 1);
			}
			else if (ServerListScreen.ipSelect < 0)
			{
				ServerListScreen.ipSelect = 0;
			}
			AutoLogin.SetServer(ServerListScreen.ipSelect);
		}

		internal static void OnServerListScreenLoaded(ServerListScreen serverListScreen)
		{
			EnforceSuboiLabel(true);
			ModMenuMain.Initialize();
			TeleportMenuMain.LoadData();
			AutoTrainPet.isFirstTimeCheckPet = true;
			if (Utils.IsOpenedByExternalAccountManager)
			{
				if (GameLauncherClient.Instance.IsConnected)
				{
					GameLauncherClient.Instance.TryAutoLogin();
				}
				return;
			}
			if (serverListScreen == null || serverListScreen.cmd == null)
			{
				return;
			}
			if (string.IsNullOrEmpty(Strings.accounts))
			{
				Strings.LoadLanguage(mResources.language);
			}
			int accountCmdIdx = 1 + serverListScreen.nCmdPlay;
			if (accountCmdIdx >= 0 && accountCmdIdx < serverListScreen.cmd.Length && serverListScreen.cmd[accountCmdIdx] != null)
			{
				serverListScreen.cmd[accountCmdIdx].caption = Strings.accounts;
			}
			if (string.IsNullOrEmpty(nameCustomServer))
			{
				return;
			}
			int serverCmdIdx = 2 + serverListScreen.nCmdPlay;
			if (serverCmdIdx >= 0 && serverCmdIdx < serverListScreen.cmd.Length && serverListScreen.cmd[serverCmdIdx] != null)
			{
				serverListScreen.cmd[serverCmdIdx].caption =
					mResources.server + ": [custom] " + nameCustomServer;
			}
		}

		internal static void OnSessionConnecting(ref string host, ref int port)
		{
			if (!Utils.IsOpenedByExternalAccountManager)
			{
				nameCustomServer = "";
				if (InGameAccountManager.SelectedAccount == null)
				{
					host = currentHost = TEAMOBI2026_HOST;
					port = currentPort = (ushort)TEAMOBI2026_PORT;
					SyncGameMidletEndpoints(host, port);
					return;
				}
				Server server = InGameAccountManager.SelectedServer ?? InGameAccountManager.SelectedAccount.Server;
				InGameAccountManager.SelectedServer = null;
				if (server != null && server.IsCustomIP())
				{
					host = currentHost = IsLegacyOrWildcardHost(server.hostnameOrIPAddress) ? TEAMOBI2026_HOST : server.hostnameOrIPAddress;
					port = currentPort = server.port > 0 ? server.port : (ushort)TEAMOBI2026_PORT;
					nameCustomServer = server.name;
				}
				else
				{
					if (ServerListScreen.address == null || ServerListScreen.address.Length == 0 || IsLegacyOrWildcardHost(ServerListScreen.address[0]))
					{
						ApplyTeamobi2026ServerConfig();
					}
					int idx = (server != null && ServerListScreen.address != null && server.index >= 0 && server.index < ServerListScreen.address.Length)
						? server.index
						: 0;
					string resolvedHost = (ServerListScreen.address != null && idx >= 0 && idx < ServerListScreen.address.Length && !IsLegacyOrWildcardHost(ServerListScreen.address[idx]))
						? ServerListScreen.address[idx]
						: TEAMOBI2026_HOST;
					ushort resolvedPort = (ServerListScreen.port != null && idx >= 0 && idx < ServerListScreen.port.Length && ServerListScreen.port[idx] > 0)
						? (ushort)ServerListScreen.port[idx]
						: (ushort)TEAMOBI2026_PORT;
					host = currentHost = resolvedHost;
					port = currentPort = resolvedPort;
					ServerListScreen.ipSelect = idx;
				}
			}
			else
			{
				if (Utils.server != null)
				{
					string extHost = (string)Utils.server["ip"];
					host = (extHost == "0.0.0.0" || string.IsNullOrEmpty(extHost)) ? TEAMOBI2026_HOST : extHost;
					port = (int)Utils.server["port"];
				}
				else
				{
					host = currentHost = TEAMOBI2026_HOST;
					port = currentPort = (ushort)TEAMOBI2026_PORT;
				}
			}
			SyncGameMidletEndpoints(host, port);
		}

		internal static void OnScreenDownloadDataShow()
		{
			bool alreadyConfigured = ServerListScreen.address != null
				&& ServerListScreen.address.Length > 0
				&& ServerListScreen.ipSelect >= 0
				&& ServerListScreen.ipSelect < ServerListScreen.address.Length
				&& ServerListScreen.address[ServerListScreen.ipSelect] == TEAMOBI2026_HOST
				&& ServerListScreen.port != null
				&& ServerListScreen.ipSelect < ServerListScreen.port.Length
				&& ServerListScreen.port[ServerListScreen.ipSelect] == TEAMOBI2026_PORT;
			if (!alreadyConfigured)
			{
				ApplyTeamobi2026ServerConfig();
			}
			if (HasRequiredResources())
			{
				ServerListScreen.isGetData = false;
				ServerListScreen.loadScreen = true;
				GameCanvas.loadBG(0);
				GameCanvas.serverScreen?.switchToMe();
			}
			else
			{
				ServerListScreen.stopDownload = false;
				ServerListScreen.loadScreen = false;
				Rms.DeleteStorage(Rms.RMS_ResVersion);
				if (Session_ME.gI().isConnected())
				{
					ServerListScreen.isGetData = true;
					Service.gI().setClientType();
					Service.gI().getResource(1, null);
				}
				else
				{
					ServerListScreen.isGetData = false;
					ServerListScreen.loadScreen = false;
				}
			}
		}

		internal static bool OnCheckZoomLevel(int w, int h)
		{
			if (Utils.IsMobile())
			{
				if (w * h >= 2073600)
					mGraphics.zoomLevel = 4;
				else if (w * h >= 691200)
					mGraphics.zoomLevel = 3;
				else if (w * h > 153600)
					mGraphics.zoomLevel = 2;
				else
					mGraphics.zoomLevel = 1;
			}
			else
			{
				mGraphics.zoomLevel = 2;
				if (w * h < 480000)
					mGraphics.zoomLevel = 1;
			}

			return true;
		}

		internal static bool OnKeyPressed(int keyCode, bool isFromSync)
		{
			if (Utils.channelSyncKey != -1 && !isFromSync)
			{
				//SocketClient.gI.sendMessage(new
				//{
				//    action = "syncKeyPressed",
				//    keyCode,
				//    Utils.channelSyncKey
				//});
			}

			return false;
		}

		internal static bool OnKeyReleased(int keyCode, bool isFromSync)
		{
			if (Utils.channelSyncKey != -1 && !isFromSync)
			{
				//SocketClient.gI.sendMessage(new
				//{
				//    action = "syncKeyReleased",
				//    keyCode,
				//    Utils.channelSyncKey
				//});
			}

			return false;
		}

		internal static bool OnChatPopupMultiLine(string chat)
		{
			GameScr.info1.addInfo(chat, 0);
			BoMongMessageEvent.Process(chat);
			return true;
		}

		internal static bool OnAddBigMessage(string chat, Npc npc)
		{
			if (!AutoTrainPet.isEnabled && !UpCSKBController.IsRunning)
				return false;
			return true;
		}

		internal static void OnInfoMapLoaded()
		{
			_hasTickSnapshot = false;
			_isInterpolatingPose = false;
			_isBackgroundZonePoll = false;
			IsBackgroundPetInfoPoll = false;
			Utils.UpdateWaypointChangeMap();
			GameScr.gI().pts = null;
		}

		internal static void OnPaintGameScr(mGraphics g)
		{
			try
			{
				ModMenuMain.Paint(g);
				CharEffectMain.Paint(g);
				UIReportersManager.handlePaintGameScr(g);
				g.setColor(Color.red);
				if (GameScr.vCharInMap != null)
				{
					for (int i = 0; i < GameScr.vCharInMap.size(); i++)
					{
						Char @char = (Char)GameScr.vCharInMap.elementAt(i);
						if (@char != null && @char.cTypePk == 5)
						{
							g.drawLineToBoss(Char.myCharz().cx - GameScr.cmx, Char.myCharz().cy - GameScr.cmy, @char.cx - GameScr.cmx, @char.cy - GameScr.cmy);
						}
					}
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}

		}

		internal static bool OnUseSkill(Char ch)
		{
			if (ch.me)
			{
				CharEffectMain.AddEffectCreatedByMe(ch.myskill);
			}
			return false;
		}

		internal static void OnAddInfoMe(string str)
		{
			EdwardXmap.Info(str);
			AutoUseGrape.Info(str);
			BoMongMessageEvent.Process(str);
		}

		internal static bool OnUpdateTouchGameScr(GameScr instance)
		{
			if (!GameCanvas.panel.isShow && GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
			{
				int panel2DrawX = GameCanvas.panel2.X + GameCanvas.panel2.X - GameCanvas.panel2.cmx;
				if (GameCanvas.isPointerJustRelease &&
				    !GameCanvas.isPointer(panel2DrawX, GameCanvas.panel2.Y, GameCanvas.panel2.W, GameCanvas.panel2.H) &&
				    GameCanvas.panel2.isDoneCombine && !GameCanvas.panel2.pointerIsDowning)
				{
					GameCanvas.panel2.hide();
					GameCanvas.clearAllPointerEvent();
					return true;
				}

				if (GameCanvas.panel2.pointerIsDowning ||
				    GameCanvas.isPointer(panel2DrawX, GameCanvas.panel2.Y, GameCanvas.panel2.W,
					    GameCanvas.panel2.H))
				{
					instance.isPointerDowning = false;
					if (Char.myCharz() != null)
						Char.myCharz().currentMovePoint = null;
					return true;
				}
			}

			Boss.UpdateTouch();
			ListCharsInMap.updateTouch();
			ModMenuMain.UpdateTouch();
			if (GameCanvas.isTouchControl)
			{
				if (TileMap.isOfflineMap())
				{
					if (GameCanvas.isMouseFocus(GameScr.xC, GameScr.yC, 34, 34))
					{
						mScreen.keyMouse = 15;
					}
					else if (GameCanvas.isMouseFocus(GameScr.xHP, GameScr.yHP, 40, 40))
					{
						if (Char.myCharz().statusMe != 14)
							mScreen.keyMouse = 10;
					}
					else if (GameCanvas.isMouseFocus(GameScr.xF, GameScr.yF, 40, 40))
					{
						if (Char.myCharz().statusMe != 14)
							mScreen.keyMouse = 5;
					}
					else if (instance.cmdMenu != null && GameCanvas.isMouseFocus(instance.cmdMenu.x, instance.cmdMenu.y,
						         instance.cmdMenu.w / 2, instance.cmdMenu.h))
					{
						mScreen.keyMouse = 1;
					}
					else
					{
						mScreen.keyMouse = -1;
					}

					if (GameCanvas.isPointerHoldIn(GameScr.xC, GameScr.yC, 34, 34))
					{
						mScreen.keyTouch = 15;
						GameCanvas.isPointerJustDown = false;
						instance.isPointerDowning = false;
						if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
						{
							ChatTextField.gI().startChat(instance, string.Empty);
							SoundMn.gI().buttonClick();
							Char.myCharz().currentMovePoint = null;
							GameCanvas.clearAllPointerEvent();
						}

						return true;
					}
				}
			}

			if (instance.mobCapcha != null)
			{
				;
			}
			else if (GameScr.isHaveSelectSkill)
			{
				if (Char.myCharz().IsCharDead())
					return false;
				if (!instance.isCharging())
				{
					Skill[] skills = Main.isPC ? GameScr.keySkill : GameScr.onScreenSkill;
					if (skills == null || GameScr.xS == null || GameScr.yS == null)
						return false;
					int xSMax = int.MinValue;
					int xSMin = int.MaxValue;
					int ySMax = int.MinValue;
					int ySMin = int.MaxValue;
					bool hasAnySkill = false;
					int count = System.Math.Min(skills.Length, System.Math.Min(GameScr.xS.Length, GameScr.yS.Length));
					for (int i = count - 1; i >= 0; i--)
						if (skills[i] != null)
						{
							hasAnySkill = true;
							xSMax = Math.max(GameScr.xS[i], xSMax);
							xSMin = Math.min(GameScr.xS[i], xSMin);
							ySMax = Math.max(GameScr.yS[i], ySMax);
							ySMin = Math.min(GameScr.yS[i], ySMin);
						}

					if (hasAnySkill && GameCanvas.isPointerHoldIn(GameScr.xSkill + xSMin - 5, ySMin - 5,
						    xSMax - xSMin + GameScr.wSkill + 10, ySMax - ySMin + GameScr.wSkill + 10))
					{
						for (int i = 0; i < count; i++)
							if (GameCanvas.isPointerHoldIn(GameScr.xSkill + GameScr.xS[i], GameScr.yS[i],
								    GameScr.wSkill, GameScr.wSkill))
							{
								GameCanvas.isPointerJustDown = false;
								instance.isPointerDowning = false;
								instance.keyTouchSkill = i;
								if (GameCanvas.isPointerClick && GameCanvas.isPointerJustRelease)
								{
									GameCanvas.isPointerClick = GameCanvas.isPointerJustDown =
										GameCanvas.isPointerJustRelease = false;
									instance.selectedIndexSkill = i;
									if (GameScr.indexSelect < 0)
										GameScr.indexSelect = 0;
									if (instance.selectedIndexSkill > skills.Length - 1)
										instance.selectedIndexSkill = skills.Length - 1;

									Skill skill = skills[instance.selectedIndexSkill];
									if (skill != null)
										instance.doSelectSkill(skill, true);
									break;
								}
							}

						return true;
					}
				}
			}
			return false;
		}

		internal static void OnUpdateTouchPanel(Panel instance)
		{
			if (instance.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
				instance.updateKeyScrollView();
		}

		internal static void OnSetPointItemMap(int xEnd, int yEnd)
		{

		}

		internal static bool OnMenuStartAt(MyVector menuItems)
		{
			return false;
		}

		internal static void OnAddInfoChar(Char c, string info)
		{
			if (AutoTrainPet.Mode <= AutoTrainPetMode.Disabled || c == null || string.IsNullOrEmpty(info))
				return;
			if (LocalizedString.saoMayLuoiThe.ContainsReversed(info.ToLower()) && c.charID == -Char.myCharz().charID)
			{
				AutoTrainPet.saoMayLuoiThe = true;
			}
		}

		internal static void OnMobStartDie(Mob mob)
		{
			if (mob.status != 1 && mob.status != 0)
			{
				mob.lastTimeDie = mSystem.currentTimeMillis();
				mob.countDie = (mob.countDie + 1) > 10 ? 0 : mob.countDie + 1;
			}
		}

		internal static void OnUpdateMob(Mob mob)
		{
			if (mob.levelBoss != 0)
			{
				mob.countDie = 0;
			}
		}

		internal static bool OnCreateImage(string filename, out Image image)
		{
			string streamingAssetsPath = Application.streamingAssetsPath;
			if (Utils.IsMobile())
				streamingAssetsPath = Path.Combine(ModStorage.PersistentDataPath, "StreamingAssets");
			string customAssetsPath = Path.Combine(streamingAssetsPath, "CustomAssets");
			image = new Image();
			Texture2D texture2D;
			bool fromResources = false;
			if (!Utils.IsEditor() && !Directory.Exists(customAssetsPath))
				Directory.CreateDirectory(customAssetsPath);
			string filePath = Path.Combine(customAssetsPath, filename.Replace('/', Path.DirectorySeparatorChar) + ".png");
			if (File.Exists(filePath))
			{
				byte[] bytes = File.ReadAllBytes(filePath);
				texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				bool loaded = false;
				try
				{
					loaded = texture2D.LoadImage(bytes);
				}
				catch
				{
					UnityEngine.Object.Destroy(texture2D);
					throw;
				}
				if (!loaded)
				{
					UnityEngine.Object.Destroy(texture2D);
					texture2D = Resources.Load<Texture2D>(filename);
					fromResources = true;
				}
			}
			else
			{
				texture2D = Resources.Load<Texture2D>(filename);
				fromResources = true;
			}

			if (texture2D == null)
				throw new NullReferenceException(nameof(texture2D));
			image.texture = texture2D;
			image.isResourceAsset = fromResources;
			image.w = image.texture.width;
			image.h = image.texture.height;
			image.texture.anisoLevel = 0;
			image.texture.filterMode = FilterMode.Point;
			image.texture.mipMapBias = 0f;
			image.texture.wrapMode = TextureWrapMode.Clamp;
			return true;
		}

		internal static void OnChatVip(string chatVip)
		{
			Boss.AddBoss(chatVip);
		}

		internal static void OnUpdateChar(Char ch)
		{
			CharEffectMain.UpdateChar(ch);
		}

		internal static void OnCharRemoveHoldEff(Char ch)
		{
			CharEffectMain.RemoveHold(ch);
		}

		internal static void OnCharSetHoldChar(Char ch, Char r)
		{
			CharEffectMain.AddCharHoldChar(ch, r);
		}

		internal static void OnCharSetHoldMob(Char ch)
		{
			CharEffectMain.AddCharHoldMob(ch);
		}

		internal static bool OnPaintTouchControl(GameScr instance, mGraphics g)
		{
			if (instance.isNotPaintTouchControl())
				return false;
			GameScr.resetTranslate(g);
			if (mScreen.keyTouch == 15 || Utils.IsPC() && mScreen.keyMouse == 15)
				g.drawImage(Utils.IsPC() ? GameScr.imgChatsPC2 : GameScr.imgChat2, GameScr.xC + 17,
					GameScr.yC + 17 + mGraphics.addYWhenOpenKeyBoard, mGraphics.HCENTER | mGraphics.VCENTER);
			else
				g.drawImage(Utils.IsPC() ? GameScr.imgChatPC : GameScr.imgChat, GameScr.xC + 17,
					GameScr.yC + 17 + mGraphics.addYWhenOpenKeyBoard, mGraphics.HCENTER | mGraphics.VCENTER);
			return true;
		}

		internal static bool OnGameScrPaintGamePad(mGraphics g)
		{
			if (_hasSavedSelectSkill)
				GameScr.isHaveSelectSkill = isHaveSelectSkill_old;
			if (GameScr.isAnalog != 0 && Char.myCharz().statusMe != 14)
			{
				g.drawImage(mScreen.keyTouch == 5 ? GameScr.imgFire1 : GameScr.imgFire0, GameScr.xF + 20,
					GameScr.yF + 20, mGraphics.HCENTER | mGraphics.VCENTER);
				GameScr.gamePad.paint(g);
				g.drawImage(mScreen.keyTouch != 13 ? GameScr.imgFocus : GameScr.imgFocus2, GameScr.xTG + 20,
					GameScr.yTG + 20, mGraphics.HCENTER | mGraphics.VCENTER);
			}

			return true;
		}

		internal static bool OnPanelFireOption(Panel panel)
		{
			if (panel.selected >= 0)
				switch (panel.selected)
				{
				case 0:
					SoundMn.gI().AuraToolOption();
					break;
				case 1:
					SoundMn.gI().AuraToolOption2();
					break;
				case 2:
					SoundMn.gI().chatVipToolOption();
					break;
				case 3:
					SoundMn.gI().soundToolOption();
					break;
				case 4:
					SoundMn.gI().analogToolOption();
					break;
				case 5:
					SoundMn.gI().CaseSizeScr();
					break;
				case 6:
					GameCanvas.startYesNoDlg(mResources.changeSizeScreen,
						new Command(mResources.YES, panel, 170391, null),
						new Command(mResources.NO, panel, 4005, null));
					break;
				}

			return true;
		}

		internal static bool OnSoundMnGetStrOption()
		{
			Panel.strCauhinh = new[]
			{
				mResources.aura_off?.Trim() + ": " + Strings.OnOffStatus(Char.isPaintAura), mResources.aura_off_2?.Trim() + ": " + Strings.OnOffStatus(Char.isPaintAura2), mResources.serverchat_off?.Trim() + ": " + Strings.OnOffStatus(GameScr.isPaintChatVip), mResources.turnOffSound?.Trim() + ": " + Strings.OnOffStatus(GameCanvas.isPlaySound), mResources.analog?.Trim() + ": " + Strings.OnOffStatus(GameScr.isAnalog != 0), (GameCanvas.lowGraphic ? mResources.cauhinhcao : mResources.cauhinhthap)?.Trim(), mGraphics.zoomLevel <= 1 ? mResources.x2Screen : mResources.x1Screen
			};
			return true;
		}

		internal static bool OnSetSkillBarPosition()
		{
			Skill[] skills = Main.isPC ? GameScr.keySkill : GameScr.onScreenSkill;
			GameScr.xS = new int[skills.Length];
			GameScr.yS = new int[skills.Length];
			if (GameCanvas.isTouchControlSmallScreen && GameScr.isUseTouch)
			{
				GameScr.padSkill = 5;
			}
			else
			{
				GameScr.wSkill = 30;
				if (GameCanvas.w <= 320)
					GameScr.ySkill = GameScr.gH - GameScr.wSkill - 6;
				else
					GameScr.wSkill = 40;
			}

			GameScr.xSkill = 17;
			GameScr.ySkill = GameCanvas.h - 40;
			if (GameScr.gamePad.isSmallGamePad && GameScr.isAnalog == 1)
			{
				GameScr.xHP = Math.min(skills.Length, 5) * GameScr.wSkill;
				GameScr.yHP = GameScr.ySkill;
			}
			else
			{
				GameScr.xHP = GameCanvas.w - 45;
				GameScr.yHP = GameCanvas.h - 45;
			}

			//GameScr.setTouchBtn();
			if (GameScr.isAnalog != 0)
			{
				GameScr.xTG = GameScr.xF = GameCanvas.w - 45;
				if (GameScr.gamePad.isLargeGamePad)
				{
					//GameScr.xSkill = GameScr.gamePad.wZone + 20;
					int skillsCountNotNull = skills.Length;
					for (int i = skills.Length - 1; i >= 0; i--)
						if (skills[i] == null)
							skillsCountNotNull--;
						else
							break;
					GameScr.wSkill = 35;
					GameScr.xSkill = Math.max(GameScr.gamePad.wZone + 20,
						GameCanvas.hw - skillsCountNotNull * GameScr.wSkill / 2);
					GameScr.xHP = GameScr.xF - 45;
				}
				else if (GameScr.gamePad.isMediumGamePad)
				{
					GameScr.xHP = GameScr.xF - 45;
				}

				GameScr.yF = GameCanvas.h - 45;
				GameScr.yTG = GameScr.yF - 45;
			}

			if (GameCanvas.isTouchControlSmallScreen && GameScr.isUseTouch ||
			    !GameScr.gamePad.isLargeGamePad && GameScr.isAnalog == 1)
			{
				for (int i = 0; i < GameScr.xS.Length; i++)
				{
					GameScr.xS[i] = i * GameScr.wSkill;
					GameScr.yS[i] = GameScr.ySkill;
					if (GameScr.xS.Length > 5 && i >= GameScr.xS.Length / 2)
					{
						GameScr.xS[i] = (i - GameScr.xS.Length / 2) * GameScr.wSkill;
						GameScr.yS[i] = GameScr.ySkill - 32;
					}
				}
			}
			else
			{
				int lastJ = 0;
				for (int i = 0; i < GameScr.xS.Length; i++)
				{
					GameScr.xS[i] = i * GameScr.wSkill;
					GameScr.yS[i] = GameScr.ySkill;
					if (lastJ == 0 && GameScr.xSkill + i * GameScr.wSkill > GameScr.xHP - 30)
						lastJ = i;
					if (GameScr.xS.Length > 5 && lastJ > 0 && i >= lastJ)
					{
						GameScr.xS[i] = (i - lastJ) * GameScr.wSkill;
						GameScr.yS[i] = GameScr.ySkill - 32;
					}
				}
			}

			return true;
		}

		internal static bool OnGamepadPaint(GamePad instance, mGraphics g)
		{
			if (GameScr.isAnalog != 0)
			{
				return false;
			}

			return false;
		}

		internal static void OnGameScrPaintSelectedSkill(GameScr instance, mGraphics g)
		{
			isHaveSelectSkill_old = GameScr.isHaveSelectSkill;
			_hasSavedSelectSkill = true;
			if (!GameScr.isHaveSelectSkill)
				return;
			try
			{
				GameScr.isHaveSelectSkill = false;
				if (HideGameUI.isEnabled)
					return;
				Skill[] array;
				if (Main.isPC)
					array = GameScr.keySkill;
				else if (GameCanvas.isTouch)
					array = GameScr.onScreenSkill;
				else
					array = GameScr.keySkill;
				if (array == null)
					return;
				if (!GameCanvas.isTouch)
				{
					g.setColor(0xAA2C11);
					g.fillRect(GameScr.xSkill + GameScr.xHP + 2, GameScr.yHP - 10 + 6, 20, 10);
					mFont.tahoma_7_white.drawString(g, "*", GameScr.xSkill + GameScr.xHP + 12, GameScr.yHP - 8 + 6,
						mFont.CENTER);
				}

				int num = instance.nSkill;
				if (Main.isPC || !GameCanvas.isTouch)
					num = array.Length;
				if (GameScr.xS == null || GameScr.yS == null || GameScr.xS.Length < num || GameScr.yS.Length < num)
					OnSetSkillBarPosition();
				num = System.Math.Min(num, System.Math.Min(array.Length, System.Math.Min(GameScr.xS.Length, GameScr.yS.Length)));
				string[] array2 = TField.isQwerty ? QwertySkillHotkeys : NonQwertySkillHotkeys;
				bool hasSkillsInTopRow = false;
				bool isStartHasSkill = false;
				for (int i = num - 1; i >= 0; i--)
				{
					if (array[i] != null)
						isStartHasSkill = true;
					if (isStartHasSkill)
						if (GameScr.yS[i] == GameScr.ySkill - 32)
						{
							hasSkillsInTopRow = true;
							break;
						}
				}

				isStartHasSkill = false;

				bool anySlotPopulated = false;
				for (int k = 0; k < num; k++) { if (array[k] != null) { anySlotPopulated = true; break; } }
				if (!anySlotPopulated && Char.myCharz().vSkillFight.size() > 0)
				{
					instance.loadDefaultKeySkill();
					instance.loadDefaultonScreenSkill();
				}
				for (int i = num - 1; i >= 0; i--)
				{
					Skill skill = array[i];
					if (skill != null)
					{
						isStartHasSkill = true;
						if (skill != Char.myCharz().myskill)
							g.drawImage(GameScr.imgSkill, GameScr.xSkill + GameScr.xS[i] - 1, GameScr.yS[i] - 1, 0);
						else
							g.drawImage(GameScr.imgSkill2, GameScr.xSkill + GameScr.xS[i] - 1, GameScr.yS[i] - 1, 0);
					}
					else
					{
						if (isStartHasSkill || i < 5)
							g.drawImage(GameScr.imgSkill, GameScr.xSkill + GameScr.xS[i] - 1, GameScr.yS[i] - 1, 0);
						continue;
					}

					if (Utils.IsPC() && i < array2.Length)
					{
						int num2 = 27;
						if (hasSkillsInTopRow)
						{
							if (GameScr.yS[i] == GameScr.ySkill - 32)
								num2 = -13;
						}
						else
						{
							num2 = -13;
						}

						mFont.tahoma_7b_white.drawString(g, array2[i], GameScr.xSkill + GameScr.xS[i] + 14,
							GameScr.yS[i] + num2 + 1, mFont.CENTER, mFont.tahoma_7b_dark);
					}

					skill.paint(GameScr.xSkill + GameScr.xS[i] + 13, GameScr.yS[i] + 13, g);
					if (i == instance.selectedIndexSkill && !instance.isPaintUI() && GameCanvas.gameTick % 10 > 5 ||
					    i == instance.keyTouchSkill)
						g.drawImage(ItemMap.imageFlare, GameScr.xSkill + GameScr.xS[i] + 13, GameScr.yS[i] + 14, 3);
				}
			}
			catch (Exception)
			{
				GameScr.isHaveSelectSkill = isHaveSelectSkill_old;
			}
		}

		internal static void AfterGameScrPaintSelectedSkill(GameScr instance, mGraphics g)
		{
			if (_hasSavedSelectSkill)
			{
				GameScr.isHaveSelectSkill = isHaveSelectSkill_old;
				_hasSavedSelectSkill = false;
			}
		}

		internal static bool OnPanelPaintToolInfo(mGraphics g)
		{
			mFont.tahoma_7b_white.drawString(g, Strings.communityMod, 60, 4, mFont.LEFT, mFont.tahoma_7b_dark);
			mFont.tahoma_7_yellow.drawString(g, Strings.gameVersion + ": v" + GameMidlet.VERSION, 60, 16, mFont.LEFT,
				mFont.tahoma_7_grey);
			string charName = Char.myCharz()?.cName ?? string.Empty;
			mFont.tahoma_7_yellow.drawString(g, mResources.character + ": " + charName, 60, 27, mFont.LEFT,
				mFont.tahoma_7_grey);
			Account account = InGameAccountManager.SelectedAccount;
			string serverName = account == null || account.Server == null
				? TEAMOBI2026_SERVER_NAME
				: account.Server.IsCustomIP()
					? account.Server.name
					: (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0
						? ServerListScreen.nameServer[Mathf.Clamp(account.Server.index, 0, ServerListScreen.nameServer.Length - 1)]
						: TEAMOBI2026_SERVER_NAME);
			mFont.tahoma_7_yellow.drawString(g,
				mResources.account + " " + mResources.account_server.ToLower() + " " + serverName, 60, 39, mFont.LEFT,
				mFont.tahoma_7_grey);
			return true;
		}

		internal static bool OnSkillPaint(Skill skill, int x, int y, mGraphics g)
		{
			if (!HideGameUI.isEnabled)
				SmallImage.drawSmallImage(g, skill.template.iconId, x, y, 0, StaticObj.VCENTER_HCENTER);
			long coolingDown = mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill;
			if (coolingDown < skill.coolDown)
			{
				float opacity = .6f;
				int realX = x - 11;
				int realY = y - 11;
				Color color = new Color(0, 0, 0, opacity);
				Color color2 = new Color(0, 0, 0, opacity / 2);
				g.setColor(color2);
				g.fillRect(realX, realY, 22, 22);
				float coolDownRatio = 1 - coolingDown / (float)skill.coolDown;
				CustomGraphics.drawCooldownRect(x, y, 22, 22, coolDownRatio, color);
				string cooldownStr = $"{(skill.coolDown - coolingDown) / 1000f:#.0}".Replace(',', '.');
				if (cooldownStr.Length > 4)
					cooldownStr = cooldownStr.Substring(0, cooldownStr.IndexOf('.'));
				mFont.tahoma_7_yellow.drawString(g, cooldownStr, x + 1, y - 12 + mFont.tahoma_7.getHeight() / 2,
					mFont.CENTER);
			}
			else
			{
				skill.paintCanNotUseSkill = false;
			}

			return true;
		}

		internal static bool OnGotoPlayer(int id, bool isAutoUseYardrat = true)
		{
			// if (isAutoUseYardrat)
			// {
			// 	new Thread(delegate()
			// 	{
			// 		int previousDisguiseId = -1;
			// 		if (Char.myCharz().arrItemBody[5] == null || Char.myCharz().arrItemBody[5] != null &&
			// 		    (Char.myCharz().arrItemBody[5].template.id < 592 ||
			// 		     Char.myCharz().arrItemBody[5].template.id > 594))
			// 		{
			// 			if (Char.myCharz().arrItemBody[5] != null)
			// 				previousDisguiseId = Char.myCharz().arrItemBody[5].template.id;
			// 			for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
			// 			{
			// 				Item item = Char.myCharz().arrItemBag[i];
			// 				if (item != null && item.template.id >= 592 && item.template.id <= 594)
			// 				{
			// 					do
			// 					{
			// 						Service.gI().getItem(4, (sbyte)i);
			// 						Thread.Sleep(250);
			// 					} while (Char.myCharz().arrItemBody[5].template.id < 592 ||
			// 					         Char.myCharz().arrItemBody[5].template.id > 594);
			//
			// 					break;
			// 				}
			// 			}
			// 		}
			//
			// 		Service.gI().gotoPlayer(id);
			// 		if (previousDisguiseId != -1)
			// 		{
			// 			Thread.Sleep(500);
			// 			for (int j = 0; j < Char.myCharz().arrItemBag.Length; j++)
			// 			{
			// 				Item item = Char.myCharz().arrItemBag[j];
			// 				if (item != null && item.template.id == previousDisguiseId)
			// 				{
			// 					do
			// 					{
			// 						Service.gI().getItem(4, (sbyte)j);
			// 						Thread.Sleep(250);
			// 					} while (Char.myCharz().arrItemBody[5].template.id != previousDisguiseId);
			//
			// 					break;
			// 				}
			// 			}
			// 		}
			// 	}).Start();
			// 	return true;
			// }

			return false;
		}

		internal static bool OnPaintPanel(Panel panel, mGraphics g)
		{
			if (panel.type != CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
				return false;
			g.translate(-g.getTranslateX(), -g.getTranslateY());
			g.translate(-panel.cmx, 0);
			g.translate(panel.X, panel.Y);
			GameCanvas.paintz.paintFrameSimple(panel.X, panel.Y, panel.W, panel.H, g);
			g.setClip(panel.X + 1, panel.Y, panel.W - 2, panel.yScroll - 2);
			g.setColor(0x987B55);
			g.fillRect(panel.X, panel.Y, panel.W - 2, 50);
			//panel.paintCharInfo(g, Char.myCharz());
			CustomPanelMenu.PaintTopInfo(panel, g);
			panel.paintBottomMoneyInfo(g);
			if (!CustomPanelMenu.PaintTabHeader(panel, g))
				panel.paintTab(g);
			CustomPanelMenu.Paint(panel, g);
			GameScr.resetTranslate(g);
			panel.paintDetail(g);
			if (panel.cmx == panel.cmtoX)
				panel.cmdClose.paint(g);
			if (panel.tabIcon != null && panel.tabIcon.isShow)
				panel.tabIcon.paint(g);
			return true;
		}

		static bool EnsureHudStyles()
		{
			if (GUI.skin == null)
				return false;
			int zoom = mGraphics.zoomLevel;
			if (_hpStyle == null || _mpStyle == null || _hudStyleZoomLevel != zoom)
			{
				_hudStyleZoomLevel = zoom;
				_hpStyle = new GUIStyle(GUI.skin.label)
				{
					fontStyle = FontStyle.Bold,
					fontSize = (int)(8.5 * zoom)
				};
				_hpStyle.normal.textColor = _hpStyle.hover.textColor = Color.yellow;

				_mpStyle = new GUIStyle(GUI.skin.label)
				{
					fontStyle = FontStyle.Bold,
					fontSize = 5 * zoom
				};
				_mpStyle.normal.textColor = _mpStyle.hover.textColor = Color.yellow;
			}
			return true;
		}

		internal static void OnPaintGameCanvas(GameCanvas instance, mGraphics g)
		{
			EnsureHudStyles();

			if (!GameCanvas.panel.isShow)
			{
				if (GameCanvas.panel2 != null)
				{
					g.translate(-g.getTranslateX(), -g.getTranslateY());
					g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
					if (GameCanvas.panel2.isShow)
					{
						GameCanvas.panel2.paint(g);
					}
					if (GameCanvas.panel2.chatTField != null && GameCanvas.panel2.chatTField.isShow)
					{
						GameCanvas.panel2.chatTField.paint(g);
					}
				}
			}

			UnlockMainPaintGate();
			g.setColor(new Color(0.2f, 0.2f, 0.2f, 0.6f));
			float now = Time.realtimeSinceStartup;
			if (_lastFpsSampleTime < 0f || now - _lastFpsSampleTime >= 0.25f)
			{
				_lastFpsSampleTime = now;
				double fps = System.Math.Round(1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 1);
				_cachedFpsStr = fps.ToString("F1", CultureInfo.InvariantCulture);
			}
			g.fillRect(0, 0, mFont.tahoma_7b_red.getWidth(_cachedFpsStr) + 2, 12);
			mFont.tahoma_7b_red.drawString(g, _cachedFpsStr, 2, 0, 0);
			RestoreAuthoritativePose();
		}

		internal static bool OnUpdatePanel(Panel instance)
		{
			if (instance == GameCanvas.panel)
			{
				Panel panel = ModMenuMain.currentPanel;
				int instanceDrawX = instance.X + instance.X - instance.cmx;
				if (panel != null && panel.isShow && panel.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU &&
				    GameCanvas.isPointerJustRelease &&
				    !GameCanvas.isPointer(instanceDrawX, instance.Y, instance.W, instance.H) &&
				    !GameCanvas.isPointer(panel.X + panel.X - panel.cmx, panel.Y, panel.W, panel.H) &&
				    !instance.pointerIsDowning &&
				    (instance.chatTField == null || !instance.chatTField.isShow))
				{
					instance.hide();
					return false;
				}
			}

			if (instance.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
				if ((instance.chatTField == null || !instance.chatTField.isShow) && !instance.isKiguiXu &&
				    !instance.isKiguiLuong && (instance.tabIcon == null || !instance.tabIcon.isShow) &&
				    instance.waitToPerform > 0)
					if (instance.waitToPerform - 1 == 0)
					{
						instance.waitToPerform--;
						instance.lastSelect[instance.currentTabIndex] = instance.selected;
						CustomPanelMenu.DoFire(instance);
					}

			return false;
		}

		internal static bool OnPanelUpdateKeyInTabBar(Panel instance)
		{
			if (instance.type == CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU)
			{
				if (instance.scroll != null && instance.scroll.pointerIsDowning || instance.pointerIsDowning)
					return true;
				int num = instance.currentTabIndex;
				if (instance.isTabInven() && instance.isnewInventory)
				{
					if (instance.selected == -1)
					{
						if (GameCanvas.keyPressed[6])
						{
							instance.currentTabIndex++;
							if (instance.currentTabIndex >= instance.currentTabName.Length)
							{
								if (GameCanvas.panel2 != null)
								{
									instance.currentTabIndex = instance.currentTabName.Length - 1;
									GameCanvas.isFocusPanel2 = true;
								}
								else
								{
									instance.currentTabIndex = 0;
								}
							}

							instance.selected = instance.lastSelect[instance.currentTabIndex];
							instance.lastTabIndex[instance.type] = instance.currentTabIndex;
						}

						if (GameCanvas.keyPressed[4])
						{
							instance.currentTabIndex--;
							if (instance.currentTabIndex < 0)
								instance.currentTabIndex = instance.currentTabName.Length - 1;
							if (GameCanvas.isFocusPanel2)
								GameCanvas.isFocusPanel2 = false;
							instance.selected = instance.lastSelect[instance.currentTabIndex];
							instance.lastTabIndex[instance.type] = instance.currentTabIndex;
						}
					}
					// sellectInventory is now synced with selected in updateKeyScrollView
					if (instance.sellectInventory < 0)
						instance.sellectInventory = 0;
					if (instance.sellectInventory >= instance.nTableItem)
						instance.sellectInventory = instance.nTableItem - 1;
				}
				else if (!instance.IsTabOption())
				{
					if (GameCanvas.keyPressed[!Main.isPC ? 6 : 24])
					{
						if (instance.isTabInven())
						{
							if (instance.selected >= 0)
							{
								instance.updateKeyInvenTab();
							}
							else
							{
								instance.currentTabIndex++;
								if (instance.currentTabIndex >= instance.currentTabName.Length)
								{
									if (GameCanvas.panel2 != null)
									{
										instance.currentTabIndex = instance.currentTabName.Length - 1;
										GameCanvas.isFocusPanel2 = true;
									}
									else
									{
										instance.currentTabIndex = 0;
									}
								}

								instance.selected = instance.lastSelect[instance.currentTabIndex];
								instance.lastTabIndex[instance.type] = instance.currentTabIndex;
							}
						}
						else
						{
							instance.currentTabIndex++;
							if (instance.currentTabIndex >= instance.currentTabName.Length)
							{
								if (GameCanvas.panel2 != null)
								{
									instance.currentTabIndex = instance.currentTabName.Length - 1;
									GameCanvas.isFocusPanel2 = true;
								}
								else
								{
									instance.currentTabIndex = 0;
								}
							}

							instance.selected = instance.lastSelect[instance.currentTabIndex];
							instance.lastTabIndex[instance.type] = instance.currentTabIndex;
						}
					}

					if (GameCanvas.keyPressed[!Main.isPC ? 4 : 23])
					{
						instance.currentTabIndex--;
						if (instance.currentTabIndex < 0)
							instance.currentTabIndex = instance.currentTabName.Length - 1;
						if (GameCanvas.isFocusPanel2)
							GameCanvas.isFocusPanel2 = false;
						instance.selected = instance.lastSelect[instance.currentTabIndex];
						instance.lastTabIndex[instance.type] = instance.currentTabIndex;
					}
				}

				instance.keyTouchTab = -1;
				for (int i = 0; i < instance.currentTabName.Length; i++)
				{
					if (!GameCanvas.isPointer(instance.startTabPos + i * instance.TAB_W, 52, instance.TAB_W - 1, 25))
						continue;
					instance.keyTouchTab = i;
					if (GameCanvas.isPointerJustRelease)
					{
						instance.currentTabIndex = i;
						instance.lastTabIndex[instance.type] = i;
						GameCanvas.isPointerJustRelease = false;
						instance.selected = instance.lastSelect[instance.currentTabIndex];
						if (num == instance.currentTabIndex && instance.cmRun == 0)
						{
							instance.cmtoY = 0;
							instance.selected = GameCanvas.isTouch ? -1 : 0;
						}

						break;
					}
				}

				if (num == instance.currentTabIndex)
					return true;
				instance.size_tab = 0;
				SoundMn.gI().panelClick();
				CustomPanelMenu.SetTab(instance);
				instance.selected = instance.lastSelect[instance.currentTabIndex];

				return true;
			}

			return false;
		}

		internal static void OnPaintImageBar(mGraphics g, bool isLeft, Char c)
		{
			if (!isLeft)
				return;
			if (c != Char.myCharz())
				return;
			int xHP = 85;
			int xMP = xHP;
			int yHP = 4;
			int yMP = 19;
			string cHP = Utils.FormatWithSIPrefix(Char.myCharz().cHP);
			string cMP = Utils.FormatWithSIPrefix(Char.myCharz().cMP);
			g.setColor(new Color(0.2f, 0.2f, 0.2f, 0.6f));
			if (mGraphics.zoomLevel > 1 && EnsureHudStyles())
			{
				g.fillRect(xHP, yHP + 1, Utils.getWidth(_hpStyle, cHP) + 1, Utils.getHeight(_hpStyle, cHP) - 2);
				g.drawString(cHP, xHP, yHP, _hpStyle);
				g.fillRect(xMP - 1, yMP + 1, Utils.getWidth(_mpStyle, cMP) + 1, Utils.getHeight(_mpStyle, cMP) - 2);
				g.drawString(cMP, xMP, yMP, _mpStyle);
			}
			else
			{
				g.fillRect(xHP - 1, yHP + 1, mFont.tahoma_7b_yellow.getWidth(cHP),
					mFont.tahoma_7b_yellow.getHeight() - 2);
				mFont.tahoma_7b_yellow.drawString(g, cHP, xHP, yHP, mFont.LEFT);
				g.fillRect(xMP - 1, yMP + 1, mFont.tahoma_7_yellow.getWidth(cMP),
					mFont.tahoma_7_yellow.getHeight() - 2);
				mFont.tahoma_7_yellow.drawString(g, cMP, xMP, yMP, mFont.LEFT);
			}
		}

		internal static void OnLoadIP()
		{
			_splashLoadIpTriggered = true;
			ApplyTeamobi2026ServerConfig();
		}

		internal static void OnAfterPaintPanel(Panel panel, mGraphics g)
		{

			sbyte GetColor_Item_Upgrade(int lv)
			{
				if (lv < 8)
					return 0;
				if (lv == 9)
					return 4;
				if (lv == 10)
					return 1;
				if (lv == 11)
					return 5;
				if (lv == 12)
					return 3;
				if (lv == 13)
					return 2;
				return 6;
			}

			int GetColor_ItemBg(int id)
			{
				switch (id)
				{
				case 4:
					return 1269146;
				case 1:
					return 2786816;
				case 5:
					return 13279744;
				case 3:
					return 12537346;
				case 2:
					return 7078041;
				case 6:
					return 11599872;
				default:
					return -1;
				}
			}

			if (GameCanvas.panel.combineSuccess != -1)
			{
				return;
			}
			g.translate(-(panel.cmx - panel.cmtoX), -panel.cmy);
			if (panel.type == 13) //trade
			{
				bool? isMe = null;
				if (panel.currentTabIndex == 0 && panel != GameCanvas.panel)
					isMe = false;
				if (panel.currentTabIndex == 2)
					isMe = false;
				if (panel.currentTabIndex == 1)
					isMe = true;
				if (isMe != null)
				{
					MyVector myVector = isMe.Value ? panel.vMyGD : panel.vFriendGD;
					if (myVector.size() <= 0)
						return;
					int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT, 0);
					for (int i = offset;
					     i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, myVector.size());
					     i++)
					{
						Item item = (Item)myVector.elementAt(i);
						if (item == null)
							continue;
						int y = panel.yScroll + i * panel.ITEM_HEIGHT;
						if (item.itemOption != null)
						{
							ItemOption itemOption = item.GetBestItemOption();
							if (itemOption?.optionTemplate == null)
								goto Label;
							int param = itemOption.param;
							int id = itemOption.optionTemplate.id;
							if (param > 7 || id >= 127 && id <= 135)
								param = 7;
							if (id == 107)
							{
								if (param > 1)
									param = (int)System.Math.Ceiling((double)param / 2);
								else if (param == 1)
									goto Label;
							}

							if (param <= 0)
								goto Label;
							g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
							for (int j = 0; j < item.itemOption.Length; j++)
								if (item.itemOption[j]?.optionTemplate != null && item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
								{
									byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
									if (GetColor_ItemBg(id_) != -1)
										g.setColor(GetColor_ItemBg(id_));
								}

							g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
							CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 11, 34, panel.ITEM_HEIGHT - 1, item);
							if (item?.template != null)
								SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2, panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
						}

						Label: ;
						CustomGraphics.PaintItemOptions(g, panel, item, y);
					}
				}
			}
			else if (panel.type == 1 || panel.type == 17) //shop
			{
				if (panel.type == 1 && panel.currentTabIndex == panel.currentTabName.Length - 1 &&
				    GameCanvas.panel2 == null && panel.typeShop != 2)
					return;
				Item[][] arrItemShop = Char.myCharz()?.arrItemShop;
				if (arrItemShop == null || panel.currentTabIndex < 0 || panel.currentTabIndex >= arrItemShop.Length || arrItemShop[panel.currentTabIndex] == null)
					return;
				if (panel.typeShop == 2 && panel == GameCanvas.panel)
					if (arrItemShop[panel.currentTabIndex].Length == 0 && panel.type != 17)
						return;
				Item[] array = arrItemShop[panel.currentTabIndex];
				if (panel.typeShop == 2 && (panel.currentTabIndex == 4 || panel.type == 17))
				{
					if (arrItemShop.Length <= 4 || arrItemShop[4] == null)
						return;
					array = arrItemShop[4];
					if (array.Length == 0)
						return;
				}

				for (int i = 0; i < array.Length; i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					Item item = array[i];
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption?.optionTemplate == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j]?.optionTemplate != null && item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 24, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 12, y + 11, 24, panel.ITEM_HEIGHT - 1,
							item);
						if (item?.template != null)
							SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 24 / 2,
								panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					if (panel.type == Panel.TYPE_KIGUI)
					{
						CustomGraphics.PaintItemOptions(g, panel, item, y + mFont.tahoma_7b_blue.getHeight() + 2);
					}
					else if (panel.type == Panel.TYPE_SHOP)
					{
						if (!string.IsNullOrEmpty(item.nameNguoiKyGui))
						{
							if (GameCanvas.gameTick % 120 > 60 && (Utils.HasStarOption(item, out _, out _) ||
							                                       Utils.HasActivateOption(item)))
							{
								int w = mFont.tahoma_7b_green.getWidth(item.nameNguoiKyGui) + 5;
								g.setColor(i != panel.selected ? 0xE7DFD2 : 0xF9FF4A);
								g.fillRect(panel.X + Panel.WIDTH_PANEL - 2 - w,
									y + mFont.tahoma_7b_blue.getHeight() + 2, w, mFont.tahoma_7b_green.getHeight());
								CustomGraphics.PaintItemOptions(g, panel, item,
									y + mFont.tahoma_7b_blue.getHeight() + 2);
							}
						}
						else
						{
							CustomGraphics.PaintItemOptions(g, panel, item, y + mFont.tahoma_7b_blue.getHeight() + 2);
						}
					}
					else
					{
						CustomGraphics.PaintItemOptions(g, panel, item, y);
					}
				}
			}
			else if (panel.type == 21 && panel.currentTabIndex == 0) //pet inventory
			{
				Item[] arrItemBody = Char.myPetz()?.arrItemBody;
				if (arrItemBody == null)
					return;
				for (int i = 0; i < arrItemBody.Length; i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					Item item = arrItemBody[i];
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption?.optionTemplate == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j]?.optionTemplate != null && item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 14, 34, panel.ITEM_HEIGHT - 1, item);
						if (item?.template != null)
							SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2, panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
			else if (panel.type == 2 && panel.currentTabIndex == 0) //box
			{
				Item[] arrItemBox = Char.myCharz()?.arrItemBox;
				if (arrItemBox == null)
					return;
				int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT - 1, 0);
				for (int i = offset;
				     i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, arrItemBox.Length);
				     i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
					{
						continue;
					}
					if (i == 0)
					{
						continue;
					}
					Item item = arrItemBox[i];
					if (item == null)
					{
						continue;
					}
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption?.optionTemplate == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						foreach (ItemOption option in item.itemOption)
						{
							if (option?.optionTemplate != null && option.optionTemplate.id == 72 && option.param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(option.param);
								if (GetColor_ItemBg(id_) != -1)
								{
									g.setColor(GetColor_ItemBg(id_));
								}
							}
						}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 11, 34, panel.ITEM_HEIGHT - 1,
							item);
						if (item?.template != null)
							SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2,
								y + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
			else if (panel.type == 12 && panel.currentTabIndex == 0) //combine
			{
				if (panel.vItemCombine.size() == 0)
					return;
				int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT, 0);
				for (int i = offset;
				     i < Mathf.Clamp(offset + panel.hScroll / panel.ITEM_HEIGHT + 2, 0, panel.vItemCombine.size() + 1);
				     i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll ||
					    y - panel.cmy < panel.yScroll - panel.ITEM_HEIGHT)
						continue;
					if (i == panel.vItemCombine.size())
						continue;
					Item item = (Item)panel.vItemCombine.elementAt(i);
					if (item == null)
						continue;
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption?.optionTemplate == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
								param = (int)System.Math.Ceiling((double)param / 2);
							else if (param == 1)
								goto Label;
						}

						if (param <= 0)
							goto Label;
						g.setColor(i == panel.selected ? 0x919600 : 0x987B55);
						for (int j = 0; j < item.itemOption.Length; j++)
							if (item.itemOption[j]?.optionTemplate != null && item.itemOption[j].optionTemplate.id == 72 && item.itemOption[j].param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(item.itemOption[j].param);
								if (GetColor_ItemBg(id_) != -1)
									g.setColor(GetColor_ItemBg(id_));
							}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);
						CustomGraphics.PaintItemEffectInPanel(g, panel.xScroll + 17, y + 11, 34, panel.ITEM_HEIGHT - 1, item);
						if (item?.template != null)
							SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2, panel.yScroll + i * panel.ITEM_HEIGHT + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
			else if (panel.type == 21 && panel.currentTabIndex == 2 ||
			         panel.type == 0 && panel.currentTabIndex == 1 ||
			         panel.type == 2 && panel.currentTabIndex == 1 ||
			         panel.type == 7 ||
			         panel.type == 12 && panel.currentTabIndex == 1 ||
			         panel.type == 13 && panel.currentTabIndex == 0 && panel == GameCanvas.panel ||
			         panel.type == 1 && panel.currentTabIndex == panel.currentTabName.Length - 1 &&
			         GameCanvas.panel2 == null && panel.typeShop != 2) //my inventory
			{
				Item[] arrItemBody = Char.myCharz()?.arrItemBody;
				Item[] arrItemBag = Char.myCharz()?.arrItemBag;
				if (arrItemBody == null || arrItemBag == null)
					return;
				int totalItems = arrItemBody.Length + arrItemBag.Length;
				int offset = Math.max(panel.cmy / panel.ITEM_HEIGHT, 0);

				int clamp = Mathf.Clamp(offset + (panel.hScroll - 21) / panel.ITEM_HEIGHT + 2, 0, totalItems);

				for (int i = offset; i < clamp; i++)
				{
					int y = panel.yScroll + i * panel.ITEM_HEIGHT;
					if (y - panel.cmy > panel.yScroll + panel.hScroll || y - panel.cmy < panel.yScroll)
					{
						continue;
					}
					bool isBodyItem = i < arrItemBody.Length;
					int bagIndex = i - arrItemBody.Length;
					Item item = isBodyItem ? arrItemBody[i] : bagIndex < arrItemBag.Length ? arrItemBag[bagIndex] : null;
					if (item == null)
					{
						continue;
					}
					if (item.itemOption != null)
					{
						ItemOption itemOption = item.GetBestItemOption();
						if (itemOption?.optionTemplate == null)
							goto Label;
						int param = itemOption.param;
						int id = itemOption.optionTemplate.id;
						if (param > 7 || id >= 127 && id <= 135)
							param = 7;
						if (id == 107)
						{
							if (param > 1)
							{
								param = (int)System.Math.Ceiling((double)param / 2);
							}
							else if (param == 1)
							{
								goto Label;
							}
						}

						if (param <= 0)
						{
							goto Label;
						}
						if (i == panel.selected)
						{
							g.setColor(0x919600);
						}
						else if (isBodyItem)
						{
							g.setColor(0x987B55);
						}
						else
						{
							g.setColor(0xB49F84);
						}

						foreach (ItemOption option in item.itemOption)
						{
							if (option?.optionTemplate != null && option.optionTemplate.id == 72 && option.param > 0)
							{
								byte id_ = (byte)GetColor_Item_Upgrade(option.param);
								if (GetColor_ItemBg(id_) != -1)
								{
									g.setColor(GetColor_ItemBg(id_));
								}
							}
						}

						g.fillRect(panel.xScroll, y, 34, panel.ITEM_HEIGHT - 1);

						CustomGraphics.PaintItemEffectInPanel(g,
							panel.xScroll + 17 + (panel == GameCanvas.panel2 ? 2 : 0), y + 11, 34,
							panel.ITEM_HEIGHT - 1, item);
						if (item?.template != null)
							SmallImage.drawSmallImage(g, item.template.iconID, panel.xScroll + 34 / 2,
								y + (panel.ITEM_HEIGHT - 1) / 2, 0, 3);
					}

					Label: ;
					CustomGraphics.PaintItemOptions(g, panel, item, y);
				}
			}
		}

		internal static bool OnServerListScreenInitCommand(ServerListScreen screen)
		{
			EnsureMainThreadRegistered();
			EnforceSuboiLabel(true);
			EnsureResVersionReady();
			if (ServerListScreen.address == null || ServerListScreen.address.Length == 0 || IsLegacyOrWildcardHost(ServerListScreen.address[0]))
			{
				ApplyTeamobi2026ServerConfig();
			}
			if (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0 && (ServerListScreen.ipSelect < 0 || ServerListScreen.ipSelect >= ServerListScreen.nameServer.Length))
			{
				ServerListScreen.ipSelect = 0;
			}
			if (string.IsNullOrEmpty(Strings.accounts))
			{
				Strings.LoadLanguage(mResources.language);
			}
			if (HasRequiredResources())
			{
				ServerListScreen.loadScreen = true;
				ServerListScreen.isGetData = false;
			}
			else
			{
				ServerListScreen.loadScreen = false;
				ServerListScreen.isGetData = true;
			}
			screen.nCmdPlay = 0;
			string text = Rms.loadRMSString("acc");
			sbyte[] userAo = Rms.loadRMS("userAo" + ServerListScreen.ipSelect);
			if (text == null)
			{
				if (userAo != null)
					screen.nCmdPlay = 1;
			}
			else if (text.Equals(string.Empty))
			{
				if (userAo != null)
					screen.nCmdPlay = 1;
			}
			else
			{
				screen.nCmdPlay = 1;
			}

			screen.cmd = new Command[5 + screen.nCmdPlay];
			int num = GameCanvas.hh - 15 * screen.cmd.Length + 28;
			for (int i = 0; i < screen.cmd.Length; i++)
			{
				switch (i)
				{
				case 0:
					screen.cmd[0] = new Command(string.Empty, screen, 3, null);
					if (string.IsNullOrEmpty(text))
					{
						screen.cmd[0].caption = mResources.playNew + "";
						if (Rms.loadRMS("userAo" + ServerListScreen.ipSelect) != null)
							screen.cmd[0].caption = mResources.choitiep + "";
					}
					else if (!Utils.IsOpenedByExternalAccountManager)
					{
						Account acc = InGameAccountManager.SelectedAccount;
						string displayName = acc?.Info?.Name;
						if (string.IsNullOrEmpty(displayName))
							screen.cmd[0].caption = mResources.playAcc + ": " + new string('*', text.Length);
						else
							screen.cmd[0].caption = mResources.playAcc + ": " + displayName;
					}
					else
					{
						screen.cmd[0].caption = mResources.playAcc + ": " + new string('*', text.Length);
					}

					if (screen.cmd[0].caption.Length > 23)
					{
						screen.cmd[0].caption = screen.cmd[0].caption.Substring(0, 23);
						screen.cmd[0].caption += "...";
					}

					break;
				case 1:
					if (screen.nCmdPlay == 1)
					{
						screen.cmd[1] = new Command(string.Empty, screen, 10100, null);
						screen.cmd[1].caption = mResources.playNew;
					}
					else
					{
						if (!Utils.IsOpenedByExternalAccountManager)
						{
							screen.cmd[1] = new Command(Strings.accounts, new InGameAccountManager.ActionListener(), 7, null);
						}
						else
						{
							screen.cmd[1] = new Command(mResources.change_account, screen, 7, null);
						}
					}

					break;
				case 2:
					if (screen.nCmdPlay == 1)
					{
						if (!Utils.IsOpenedByExternalAccountManager)
							screen.cmd[2] = new Command(Strings.accounts, new InGameAccountManager.ActionListener(), 7, null);
						else
							screen.cmd[2] = new Command(mResources.change_account, screen, 7, null);
					}
					else
					{
						string svName = (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0)
							? ServerListScreen.nameServer[Mathf.Clamp(ServerListScreen.ipSelect, 0, ServerListScreen.nameServer.Length - 1)]
							: TEAMOBI2026_SERVER_NAME;
						screen.cmd[2] = new Command(mResources.server + ": " + svName, screen, 17, null);
					}

					break;
				case 3:
					if (screen.nCmdPlay == 1)
					{
						string svName = (ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0)
							? ServerListScreen.nameServer[Mathf.Clamp(ServerListScreen.ipSelect, 0, ServerListScreen.nameServer.Length - 1)]
							: TEAMOBI2026_SERVER_NAME;
						screen.cmd[3] = new Command(mResources.server + ": " + svName, screen, 17, null);
					}
					else
						screen.cmd[3] = new Command(mResources.option, screen, 8, null);
					break;
				case 4:
					if (screen.nCmdPlay == 1)
						screen.cmd[4] = new Command(mResources.option, screen, 8, null);
					else
						screen.cmd[4] = new Command(mResources.register, screen, 10101, null);
					break;
				case 5:
					screen.cmd[5] = new Command(mResources.register, screen, 10101, null);
					break;
				}

				screen.cmd[i].y = num;
				screen.cmd[i].setType();
				screen.cmd[i].x = (GameCanvas.w - screen.cmd[i].w) / 2;
				num += 30;
			}

			return true;
		}

		internal static bool OnPanelFireTool(Panel panel)
		{
			if (panel.selected < 0)
				return false;
			if (SoundMn.IsDelAcc && panel.selected == Panel.strTool.Length - 1)
				return false;
			bool menuHasPet = Panel.strTool != null && Panel.strTool.Length > 3 && Panel.strTool[3] == mResources.pet;
			if (!menuHasPet)
				switch (panel.selected)
				{
				case 4:
					if (GameScr.gI().pts == null)
					{
						_isBackgroundZonePoll = false;
						isOpenZoneUI = true;
						Service.gI().openUIZone();
						InfoDlg.showWait();
						return true;
					}
					_isBackgroundZonePoll = false;
					Utils.menuZone();
					isOpenZoneUI = true;
					return true;
				case 8:
					GameCanvas.timeBreakLoading = mSystem.currentTimeMillis() + 30000;
					ServerListScreen.countDieConnect = 0;
					GameCanvas.instance.resetToLoginScr = false;
					GameCanvas.instance.doResetToLoginScr(GameCanvas.serverScreen);
					return true;
				}
			else
				switch (panel.selected)
				{
				case 5:
					if (GameScr.gI().pts == null)
					{
						_isBackgroundZonePoll = false;
						isOpenZoneUI = true;
						Service.gI().openUIZone();
						InfoDlg.showWait();
						return true;
					}
					_isBackgroundZonePoll = false;
					Utils.menuZone();
					isOpenZoneUI = true;
					return true;
				case 9:
					GameCanvas.timeBreakLoading = mSystem.currentTimeMillis() + 30000;
					ServerListScreen.countDieConnect = 0;
					GameCanvas.instance.resetToLoginScr = false;
					GameCanvas.instance.doResetToLoginScr(GameCanvas.serverScreen);
					return true;
				}

			return false;
		}

		internal static bool CanCurrentSessionRegisterGuest()
		{
			bool hasRmsGuest = !string.IsNullOrEmpty(Rms.loadRMSString(Rms.RMS_userAo + ServerListScreen.ipSelect));
			bool isLogin2Guest = (GameCanvas.loginScr != null && GameCanvas.loginScr.isLogin2) || (hasRmsGuest && string.IsNullOrEmpty(Rms.loadRMSString(Rms.RMS_acc)));
			bool isAccMgrGuest = !Utils.IsOpenedByExternalAccountManager &&
				InGameAccountManager.SelectedAccount != null &&
				InGameAccountManager.SelectedAccount.Type == AccountType.Unregistered;
			return isLogin2Guest || isAccMgrGuest;
		}

		internal static bool OnGetSoundOption()
		{
			if (CanCurrentSessionRegisterGuest())
			{
				Panel.strTool = new[]
				{
					mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout, mResources.REGISTOPROTECT
				};
				if (Char.myCharz().havePet)
					Panel.strTool = new[]
					{
						mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.pet, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout, mResources.REGISTOPROTECT
					};
			}
			else
			{
				Panel.strTool = new[]
				{
					mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout
				};
				if (Char.myCharz().havePet)
					Panel.strTool = new[]
					{
						mResources.radaCard, mResources.quayso, mResources.gameInfo, mResources.pet, mResources.change_flag, mResources.change_zone, mResources.chat_world, mResources.account, mResources.option, Utils.IsOpenedByExternalAccountManager ? mResources.change_account : Strings.logout
					};
			}

			if (SoundMn.IsDelAcc)
			{
				string[] array = new string[Panel.strTool.Length + 1];
				for (int i = 0; i < Panel.strTool.Length; i++) array[i] = Panel.strTool[i];
				array[Panel.strTool.Length] = mResources.delacc;
				Panel.strTool = array;
			}

			return true;
		}

		internal static void MarkExplicitZoneOpen()
		{
			_isBackgroundZonePoll = false;
		}

		internal static bool OnOpenUIZone(GameScr instance, Message message)
		{
			bool bg = _isBackgroundZonePoll && !isOpenZoneUI;
			_isBackgroundZonePoll = false;
			isOpenZoneUI = false;
			if (!bg)
			{
				InfoDlg.hide();
			}
			try
			{
				instance.zones = new int[message.reader().readByte()];
				instance.pts = new int[instance.zones.Length];
				instance.numPlayer = new int[instance.zones.Length];
				instance.maxPlayer = new int[instance.zones.Length];
				instance.rank1 = new int[instance.zones.Length];
				instance.rankName1 = new string[instance.zones.Length];
				instance.rank2 = new int[instance.zones.Length];
				instance.rankName2 = new string[instance.zones.Length];
				for (int i = 0; i < instance.zones.Length; i++)
				{
					instance.zones[i] = message.reader().readByte();
					instance.pts[i] = message.reader().readByte();
					instance.numPlayer[i] = message.reader().readByte();
					instance.maxPlayer[i] = message.reader().readByte();
					if (message.reader().readByte() == 1)
					{
						instance.rankName1[i] = message.reader().readUTF();
						instance.rank1[i] = message.reader().readInt();
						instance.rankName2[i] = message.reader().readUTF();
						instance.rank2[i] = message.reader().readInt();
					}
				}
			}
			catch (Exception ex)
			{
				Cout.LogError("Loi ham OPEN UIZONE " + ex);
			}

			if (!bg)
			{
				GameCanvas.panel.setTypeZone();
				GameCanvas.panel.show();
			}

			return true;
		}

		internal static bool OnStartOKDlg(string info)
		{
			if (info == LocalizedString.cantChangeZoneInThisMap)
			{
				_isBackgroundZonePoll = false;
				if (isOpenZoneUI)
				{
					isOpenZoneUI = false;
				}

				return true;
			}

			return false;
		}

		internal static bool OnRequestChangeMap()
		{
			isOpenZoneUI = false;
			_isBackgroundZonePoll = false;
			IsBackgroundPetInfoPoll = false;
			return false;
		}

		internal static bool OnGetMapOffline()
		{
			isOpenZoneUI = false;
			_isBackgroundZonePoll = false;
			IsBackgroundPetInfoPoll = false;
			return false;
		}

		internal static bool OnMGraphicsDrawImage(Image image, int x, int y, int anchor)
		{
			if (HideGameUI.isEnabled && !HideGameUI.ShouldDrawImage(image))
				return true;
			if (GraphicsReducer.IsEnabled && !GraphicsReducer.ShouldDrawImage(image))
				return true;
			return false;
		}

		internal static void AfterMGraphicsDrawImage(Image image, int x, int y, int anchor)
		{
		}
	}
}
