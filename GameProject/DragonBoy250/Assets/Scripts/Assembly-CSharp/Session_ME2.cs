using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

public class Session_ME2 : ISession
{

	protected static Session_ME2 instance = new Session_ME2();

	static NetworkStream dataStream;

	static BinaryReader dis;

	static BinaryWriter dos;

	public static IMessageHandler messageHandler;

	public static bool isMainSession = true;

	static TcpClient sc;

	private static int _connectionId;

	public static volatile bool connected;

	public static volatile bool connecting;

	static readonly Sender sender = new Sender();

	public static Thread initThread;

	public static Thread collectorThread;

	public static Thread sendThread;

	public static int sendByteCount;

	public static int recvByteCount;

	static volatile bool getKeyComplete;

	public static sbyte[] key;

	static int curR;

	static int curW;

	static int timeConnected;

	public static string strRecvByteCount = string.Empty;

	public static bool isCancel;

	public static MyVector recieveMsg = new MyVector();

	string host;

	long lastTimeConn;

	int port;

	long timeWaitConnect;

	public bool isConnected()
	{
		return connected && sc != null && dis != null;
	}

	public void setHandler(IMessageHandler msgHandler)
	{
		messageHandler = msgHandler;
	}

	public void connect(string host, int port)
	{
		if (!connected && !connecting && mSystem.currentTimeMillis() >= timeWaitConnect)
		{
			timeWaitConnect = mSystem.currentTimeMillis() + 50;
			this.host = host;
			this.port = port;
			getKeyComplete = false;
			close();
			int connId = Interlocked.Increment(ref _connectionId);
			connecting = true;
			Debug.Log("connecting...!");
			Debug.Log("host: " + host);
			Debug.Log("port: " + port);
			initThread = new Thread(() => NetworkInit(connId));
			initThread.IsBackground = true;
			initThread.Start();
		}
	}

	public void sendMessage(Message message)
	{
		Res.outz("SEND MSG: " + message.command);
		sender.AddMessage(message);
	}

	public void close()
	{
		cleanNetwork();
	}

	public void clearSendingMessage()
	{
		sender.Clear();
	}

	public static Session_ME2 gI()
	{
		if (instance == null)
		{
			instance = new Session_ME2();
		}
		return instance;
	}

	void NetworkInit(int connId)
	{
		if (connId != Volatile.Read(ref _connectionId))
		{
			return;
		}
		isCancel = false;
		connecting = true;
		Thread.CurrentThread.Priority = ThreadPriority.Highest;
		try
		{
			doConnect(host, port, connId);
			if (connId != Volatile.Read(ref _connectionId))
			{
				return;
			}
			if (messageHandler != null)
			{
				messageHandler.onConnectOK(isMainSession);
			}
		}
		catch (Exception)
		{
			if (connId != Volatile.Read(ref _connectionId))
			{
				return;
			}
			close();
			if (messageHandler != null)
			{
				messageHandler.onConnectionFail(isMainSession);
			}
		}
	}

	public void doConnect(string host, int port)
	{
		doConnect(host, port, Volatile.Read(ref _connectionId));
	}

	void doConnect(string host, int port, int connId)
	{
		if (connId != Volatile.Read(ref _connectionId))
		{
			return;
		}
		TcpClient localSc = new TcpClient();
		if (!localSc.ConnectAsync(host, port).Wait(10000))
		{
			localSc.Close();
			throw new SocketException((int)SocketError.TimedOut);
		}
		if (connId != Volatile.Read(ref _connectionId))
		{
			try { localSc.Close(); } catch (Exception) { }
			return;
		}
		sc = localSc;
		dataStream = sc.GetStream();
		dis = new BinaryReader(dataStream, new UTF8Encoding());
		dos = new BinaryWriter(dataStream, new UTF8Encoding());
		key = null;
		connected = true;
		timeConnected = currentTimeMillis();
		connecting = false;
		doSendMessage(new Message(-27), connId);
		if (connId != Volatile.Read(ref _connectionId))
		{
			return;
		}
		sendThread = new Thread(() => sender.run(connId));
		sendThread.IsBackground = true;
		sendThread.Start();
		MessageCollector messageCollector = new MessageCollector();
		Cout.LogError("new -----");
		collectorThread = new Thread(() => messageCollector.run(connId));
		collectorThread.IsBackground = true;
		collectorThread.Start();
	}

	private static readonly object _writeLock = new object();

	static void doSendMessage(Message m)
	{
		doSendMessage(m, Volatile.Read(ref _connectionId));
	}

	static void doSendMessage(Message m, int connId)
	{
		sbyte[] data = m.getData();
		bool shouldClean = false;
		lock (_writeLock)
		{
			try
			{
				if (dos == null || connId != Volatile.Read(ref _connectionId))
				{
					return;
				}
				int dataLen = (data != null) ? data.Length : 0;
				int len = 3 + dataLen;
				byte[] buf = new byte[len];
				int pos = 0;
				bool encrypted = getKeyComplete;
				if (encrypted)
				{
					buf[pos++] = (byte)writeKey(m.command);
				}
				else
				{
					buf[pos++] = (byte)m.command;
				}
				if (data != null)
				{
					if (encrypted)
					{
						buf[pos++] = (byte)writeKey((sbyte)(dataLen >> 8));
						buf[pos++] = (byte)writeKey((sbyte)(dataLen & 0xFF));
						for (int i = 0; i < dataLen; i++)
						{
							buf[pos++] = (byte)writeKey(data[i]);
						}
					}
					else
					{
						buf[pos++] = (byte)(dataLen >> 8);
						buf[pos++] = (byte)(dataLen & 0xFF);
						Buffer.BlockCopy(data, 0, buf, pos, dataLen);
						pos += dataLen;
					}
					sendByteCount += 5 + dataLen;
				}
				else
				{
					if (encrypted)
					{
						buf[pos++] = (byte)writeKey(0);
						buf[pos++] = (byte)writeKey(0);
					}
					else
					{
						buf[pos++] = 0;
						buf[pos++] = 0;
					}
					sendByteCount += 5;
				}
				dos.Write(buf, 0, len);
				dos.Flush();
			}
			catch (IOException ex)
			{
				Debug.Log(ex.StackTrace);
				if (connId == Volatile.Read(ref _connectionId))
				{
					connected = false;
					shouldClean = true;
				}
			}
			catch (SocketException ex)
			{
				Debug.Log(ex.StackTrace);
				if (connId == Volatile.Read(ref _connectionId))
				{
					connected = false;
					shouldClean = true;
				}
			}
			catch (Exception ex)
			{
				Debug.Log(ex.StackTrace);
			}
		}
		if (shouldClean && connId == Volatile.Read(ref _connectionId))
		{
			bool wasEstablished = currentTimeMillis() - timeConnected > 500;
			cleanNetwork();
			if (messageHandler != null)
			{
				if (wasEstablished)
				{
					messageHandler.onDisconnected(isMainSession);
				}
				else
				{
					messageHandler.onConnectionFail(isMainSession);
				}
			}
		}
	}

	public static sbyte readKey(sbyte b)
	{
		sbyte[] localKey = key;
		if (localKey == null || localKey.Length == 0)
		{
			return b;
		}
		int num = curR % localKey.Length;
		curR = (num + 1) % localKey.Length;
		return (sbyte)((localKey[num] & 0xFF) ^ (b & 0xFF));
	}

	public static sbyte writeKey(sbyte b)
	{
		sbyte[] localKey = key;
		if (localKey == null || localKey.Length == 0)
		{
			return b;
		}
		int num = curW % localKey.Length;
		curW = (num + 1) % localKey.Length;
		return (sbyte)((localKey[num] & 0xFF) ^ (b & 0xFF));
	}

	internal static readonly object RecieveLock = new object();

	public static void onRecieveMsg(Message msg)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			messageHandler.onMessage(msg);
		}
		else
		{
			lock (RecieveLock)
			{
				recieveMsg.addElement(msg);
			}
		}
	}

	public static void update()
	{
		const int maxMessagesPerUpdate = 64;
		int processed = 0;
		while (processed < maxMessagesPerUpdate)
		{
			if (Controller.isStopReadMessage)
			{
				break;
			}
			Message message;
			lock (RecieveLock)
			{
				if (recieveMsg.size() <= 0)
				{
					break;
				}
				message = (Message)recieveMsg.elementAt(0);
				recieveMsg.removeElementAt(0);
			}
			if (message == null)
			{
				break;
			}
			messageHandler.onMessage(message);
			processed++;
		}
	}

	static void cleanNetwork()
	{
		Interlocked.Increment(ref _connectionId);
		connected = false;
		connecting = false;
		getKeyComplete = false;
		key = null;
		curR = 0;
		curW = 0;
		try
		{
			try { if (sc != null) sc.Close(); } catch (Exception) { }
			try { if (dataStream != null) dataStream.Close(); } catch (Exception) { }
			lock (_writeLock)
			{
				try { if (dos != null) dos.Close(); } catch (Exception) { }
				dos = null;
			}
			try { if (dis != null) dis.Close(); } catch (Exception) { }
		}
		catch (Exception)
		{
		}
		finally
		{
			connected = false;
			connecting = false;
			getKeyComplete = false;
			key = null;
			curR = 0;
			curW = 0;
			sc = null;
			dataStream = null;
			lock (_writeLock)
			{
				dos = null;
			}
			dis = null;
			sendThread = null;
			initThread = null;
			collectorThread = null;
			lock (RecieveLock)
			{
				recieveMsg.removeAllElements();
			}
			if (sender != null)
			{
				sender.Clear();
			}
		}
	}

	public static int currentTimeMillis()
	{
		return Environment.TickCount;
	}

	public static byte convertSbyteToByte(sbyte var)
	{
		if (var > 0)
		{
			return (byte)var;
		}
		return (byte)(var + 256);
	}

	public static byte[] convertSbyteToByte(sbyte[] var)
	{
		byte[] array = new byte[var.Length];
		for (int i = 0; i < var.Length; i++)
		{
			if (var[i] > 0)
			{
				array[i] = (byte)var[i];
			}
			else
			{
				array[i] = (byte)(var[i] + 256);
			}
		}
		return array;
	}
	public class Sender
	{
		private readonly object _sendLock = new object();
		public List<Message> sendingMessage;

		public Sender()
		{
			sendingMessage = new List<Message>();
		}

		public void AddMessage(Message message)
		{
			lock (_sendLock)
			{
				sendingMessage.Add(message);
			}
		}

		public void Clear()
		{
			lock (_sendLock)
			{
				sendingMessage?.Clear();
			}
		}

		public void run()
		{
			run(Volatile.Read(ref _connectionId));
		}

		public void run(int connId)
		{
			while (connected && connId == Volatile.Read(ref _connectionId))
			{
				try
				{
					if (getKeyComplete)
					{
						while (connected && connId == Volatile.Read(ref _connectionId))
						{
							Message m;
							lock (_sendLock)
							{
								if (sendingMessage.Count <= 0)
								{
									break;
								}
								m = sendingMessage[0];
								sendingMessage.RemoveAt(0);
							}
							if (connId != Volatile.Read(ref _connectionId))
							{
								return;
							}
							doSendMessage(m, connId);
						}
					}
					if (connId != Volatile.Read(ref _connectionId))
					{
						return;
					}
					try
					{
						Thread.Sleep(5);
					}
					catch (Exception ex)
					{
						Cout.LogError(ex.ToString());
					}
				}
				catch (Exception)
				{
					if (connId != Volatile.Read(ref _connectionId))
					{
						return;
					}
					Res.outz("error send message! ");
				}
			}
		}
	}

	class MessageCollector
	{
		public void run()
		{
			run(Volatile.Read(ref _connectionId));
		}

		public void run(int connId)
		{
			try
			{
				while (connected && connId == Volatile.Read(ref _connectionId))
				{
					Message message = readMessage();
					if (message == null || connId != Volatile.Read(ref _connectionId))
					{
						break;
					}
					try
					{
						if (message.command == -27)
						{
							getKey(message);
						}
						else
						{
							onRecieveMsg(message);
						}
					}
					catch (Exception)
					{
						Cout.println("LOI NHAN  MESS THU 1");
					}
				}
			}
			catch (Exception ex3)
			{
				if (connId != Volatile.Read(ref _connectionId))
				{
					return;
				}
				Debug.Log("error read message!");
				Debug.Log(ex3.Message);
			}
			if (connId != Volatile.Read(ref _connectionId))
			{
				return;
			}
			if (!connected)
			{
				return;
			}
			if (messageHandler != null)
			{
				if (currentTimeMillis() - timeConnected > 500)
				{
					messageHandler.onDisconnected(isMainSession);
				}
				else
				{
					messageHandler.onConnectionFail(isMainSession);
				}
			}
			if (connId != Volatile.Read(ref _connectionId))
			{
				return;
			}
			if (sc != null)
			{
				cleanNetwork();
			}
		}

		void getKey(Message message)
		{
			try
			{
				sbyte b = message.reader().readSByte();
				if (b <= 0)
				{
					return;
				}
				sbyte[] newKey = new sbyte[b];
				for (int i = 0; i < b; i++)
				{
					newKey[i] = message.reader().readSByte();
				}
				for (int j = 0; j < newKey.Length - 1; j++)
				{
					newKey[j + 1] ^= newKey[j];
				}
				key = newKey;
				getKeyComplete = true;
				GameMidlet.IP2 = message.reader().readUTF();
				GameMidlet.PORT2 = message.reader().readInt();
				GameMidlet.isConnect2 = message.reader().readByte() != 0 ? true : false;
				if (isMainSession && GameMidlet.isConnect2)
				{
					GameCanvas.connect2();
				}
			}
			catch (Exception)
			{
			}
		}

		private const int MAX_PACKET_SIZE = 4 * 1024 * 1024;

		Message readMessage2(sbyte cmd)
		{
			int num = readKey(dis.ReadSByte()) + 128;
			int num2 = readKey(dis.ReadSByte()) + 128;
			int num3 = readKey(dis.ReadSByte()) + 128;
			int num4 = (num3 * 256 + num2) * 256 + num;
			if (num4 < 0 || num4 > MAX_PACKET_SIZE)
			{
				Debug.LogError("Invalid packet length: " + num4);
				return null;
			}
			Cout.LogError("SIZE = " + num4);
			sbyte[] array = new sbyte[num4];
			if (num4 > 0)
			{
				byte[] buf = new byte[System.Math.Min(num4, 8192)];
				int offset = 0;
				while (offset < num4)
				{
					int toRead = System.Math.Min(buf.Length, num4 - offset);
					int read = dis.Read(buf, 0, toRead);
					if (read <= 0)
					{
						return null;
					}
					Buffer.BlockCopy(buf, 0, array, offset, read);
					offset += read;
				}
			}
			recvByteCount += 5 + num4;
			int num6 = recvByteCount + sendByteCount;
			strRecvByteCount = num6 / 1024 + "." + num6 % 1024 / 102 + "Kb";
			if (getKeyComplete)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = readKey(array[i]);
				}
			}
			return new Message(cmd, array);
		}

		Message readMessage()
		{
			try
			{
				sbyte b = dis.ReadSByte();
				if (getKeyComplete)
				{
					b = readKey(b);
				}
				if (b == -32 || b == -66 || b == 11 || b == -67 || b == -74 || b == -87 || b == 66 || b == 12)
				{
					return readMessage2(b);
				}
				int num;
				if (getKeyComplete)
				{
					sbyte b2 = dis.ReadSByte();
					sbyte b3 = dis.ReadSByte();
					num = (readKey(b2) & 0xFF) << 8 | readKey(b3) & 0xFF;
				}
				else
				{
					sbyte b4 = dis.ReadSByte();
					sbyte b5 = dis.ReadSByte();
					num = ((b4 & 0xFF) << 8) | (b5 & 0xFF);
				}
				sbyte[] array = new sbyte[num];
				int num2 = 0;
				int num3 = 0;
				byte[] src = dis.ReadBytes(num);
				if (src == null || src.Length < num)
				{
					return null;
				}
				Buffer.BlockCopy(src, 0, array, 0, num);
				recvByteCount += 5 + num;
				int num4 = recvByteCount + sendByteCount;
				strRecvByteCount = num4 / 1024 + "." + num4 % 1024 / 102 + "Kb";
				if (getKeyComplete)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = readKey(array[i]);
					}
				}
				return new Message(b, array);
			}
			catch (Exception ex)
			{
				Debug.Log(ex.StackTrace);
			}
			return null;
		}
	}
}
