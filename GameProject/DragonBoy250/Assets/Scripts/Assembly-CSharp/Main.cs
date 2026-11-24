using System.Net.NetworkInformation;
using System.Threading;
using Mod;
using UnityEngine;

public class Main : MonoBehaviour
{
	public static Main main;

	public static mGraphics g;

	public static GameMidlet midlet;

	public static string res = "res";

	public static string mainThreadName;

	public static volatile int mainThreadId;

	public static volatile int WatchdogTickMs;

	public static volatile int WatchdogLastCmd;

	public static volatile string WatchdogPhase = "Boot";

	private static bool watchdogStarted;

	private static void StartStallWatchdog()
	{
		if (watchdogStarted)
		{
			return;
		}
		watchdogStarted = true;
		WatchdogTickMs = System.Environment.TickCount;
		Thread wd = new Thread(() =>
		{
			int reportedTick = 0;
			while (!isQuitApp)
			{
				Thread.Sleep(500);
				if (!started)
				{
					continue;
				}
				int last = WatchdogTickMs;
				int elapsed = unchecked(System.Environment.TickCount - last);
				if (elapsed >= 1500 && reportedTick != last)
				{
					reportedTick = last;
					Debug.LogError("[WATCHDOG] Main thread stalled for " + elapsed + "ms | phase=" + WatchdogPhase + " | lastCmd=" + WatchdogLastCmd);
				}
			}
		});
		wd.IsBackground = true;
		wd.Name = "MainStallWatchdog";
		wd.Start();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void CaptureMainThreadId()
	{
		mainThreadId = Thread.CurrentThread.ManagedThreadId;
	}

	public static bool IsMainThread()
	{
		int id = mainThreadId;
		return id != 0 && Thread.CurrentThread.ManagedThreadId == id;
	}

	public static bool started;

	public static bool isIpod;

	public static bool isIphone4;

	public static bool isPC;

	public static bool isWindowsPhone;

	public static bool isIPhone;

	public static bool IphoneVersionApp;

	public static string IMEI;

	public static int versionIp;

	public static int numberQuit = 1;

	private int level;

	internal int updateCount;

	internal int paintCount;

	private int count;

	private int fps;

	private int max;

	private int up;

	private int upmax;

	private long timefps;

	private long timeup;

	private bool isRun;

	public static int waitTick;

	public static int f;

	public static bool isResume;

	public static bool isMiniApp = true;

	public static bool isQuitApp;

	private Vector2 lastMousePos = default(Vector2);

	public static int a = 1;

	public static bool isCompactDevice = true;

	private void Awake()
	{
		mainThreadId = Thread.CurrentThread.ManagedThreadId;
		GameEvents.OnAwake();
	}

	private void Start()
	{
		mainThreadId = Thread.CurrentThread.ManagedThreadId;
		if (started)
		{
			return;
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
		mainThreadName = Thread.CurrentThread.Name;
		isPC = true;
		#if UNITY_IOS
		isIPhone = true;
		#endif
		started = true;
		StartStallWatchdog();
		GameEvents.OnGameStart();
		GameEvents.OnMainStart();
	}

	private void OnGUI()
	{
		if (count >= 10)
		{
			if (fps == 0)
			{
				timefps = mSystem.currentTimeMillis();
			}
			else if (mSystem.currentTimeMillis() - timefps > 1000)
			{
				max = fps;
				fps = 0;
				timefps = mSystem.currentTimeMillis();
			}
			fps++;
			checkInput();
			if (Event.current != null && Event.current.type.Equals(EventType.Repaint) && paintCount <= updateCount)
			{
				WatchdogTickMs = System.Environment.TickCount;
				WatchdogPhase = "OnGUI.Paint";
				GameMidlet.gameCanvas.paint(g);
				paintCount++;
				g.reset();
			}
		}
	}

	public void setsizeChange()
	{
		if (!isRun)
		{
			Screen.orientation = ScreenOrientation.LandscapeLeft;
			Application.runInBackground = true;
			base.useGUILayout = false;
			isCompactDevice = detectCompactDevice();
			if (main == null)
			{
				main = this;
			}
			isRun = true;
			ScaleGUI.initScaleGUI();
			if (isPC)
			{
				IMEI = SystemInfo.deviceUniqueIdentifier;
			}
			else
			{
				IMEI = GetMacAddress();
			}
			isPC = true;
			if (isPC)
			{
				Screen.fullScreen = false;
			}
			if (iPhoneSettings.generation == iPhoneGeneration.iPodTouch4Gen)
			{
				isIpod = true;
			}
			if (iPhoneSettings.generation == iPhoneGeneration.iPhone4)
			{
				isIphone4 = true;
			}
			g = new mGraphics();
			midlet = new GameMidlet();
			TileMap.loadBg();
			Paint.loadbg();
			PopUp.loadBg();
			GameScr.loadBg();
			InfoMe.gI().loadCharId();
			Panel.loadBg();
			Menu.loadBg();
			Key.mapKeyPC();
			g.CreateLineMaterial();
		}
	}

	public static void setBackupIcloud(string path)
	{
	}

	public string GetMacAddress()
	{
		string empty = string.Empty;
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		for (int i = 0; i < allNetworkInterfaces.Length; i++)
		{
			PhysicalAddress physicalAddress = allNetworkInterfaces[i].GetPhysicalAddress();
			if (physicalAddress.ToString() != string.Empty)
			{
				return physicalAddress.ToString();
			}
		}
		return string.Empty;
	}

	public void doClearRMS()
	{
		if (isPC)
		{
			sbyte[] zoomBytes = Rms.loadRMS(Rms.RMS_lastZoomlevel);
			if (zoomBytes == null || zoomBytes.Length == 0)
			{
				Rms.saveRMSInt(Rms.RMS_lastZoomlevel, mGraphics.zoomLevel);
				Rms.saveRMSInt("levelScreenKN", level);
				return;
			}
			int num = zoomBytes[0];
			if (num != mGraphics.zoomLevel)
			{
				Rms.clearAll();
				Rms.saveRMSInt(Rms.RMS_lastZoomlevel, mGraphics.zoomLevel);
				Rms.saveRMSInt("levelScreenKN", level);
			}
		}
	}

	public static void closeKeyBoard()
	{
		if (TouchScreenKeyboard.visible && TField.kb != null)
		{
			TField.kb.active = false;
			TField.kb = null;
		}
	}

	private void FixedUpdate()
	{
		WatchdogTickMs = System.Environment.TickCount;
		WatchdogPhase = "FixedUpdate";
		Rms.update();
		Sound.update();
		count++;
		if (count >= 10)
		{
			if (up == 0)
			{
				timeup = mSystem.currentTimeMillis();
			}
			else if (mSystem.currentTimeMillis() - timeup > 1000)
			{
				upmax = up;
				up = 0;
				timeup = mSystem.currentTimeMillis();
			}
			up++;
			setsizeChange();
			updateCount++;
			ipKeyboard.update();
			GameMidlet.gameCanvas.update();
			Image.update();
			DataInputStream.update();
			SMS.update();
			Net.update();
			f++;
			if (f > 8)
			{
				f = 0;
			}
			if (!isPC)
			{
				int num = 1 / a;
			}
		}
		GameEvents.OnFixedUpdateMain();
	}

	private void Update()
	{
		if (count >= 10)
		{
			if (GameMidlet.gameCanvas != null)
			{
				checkPointerInput();
			}
			WatchdogTickMs = System.Environment.TickCount;
			WatchdogPhase = "Update.PreProcessQueue";
			GameEvents.PreProcessIncomingNetworkQueue();
			WatchdogPhase = "Update.SessionME";
			Session_ME.update();
			Session_ME2.update();
			WatchdogPhase = "Update.Done";
		}
		GameEvents.OnUpdateMain();
	}

	private void checkPointerInput()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Vector3 mousePosition = Input.mousePosition;
			GameMidlet.gameCanvas.pointerPressed((int)(mousePosition.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
			lastMousePos.x = mousePosition.x / (float)mGraphics.zoomLevel;
			lastMousePos.y = mousePosition.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
		}
		if (Input.GetMouseButton(0))
		{
			Vector3 mousePosition2 = Input.mousePosition;
			GameMidlet.gameCanvas.pointerDragged((int)(mousePosition2.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition2.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
			lastMousePos.x = mousePosition2.x / (float)mGraphics.zoomLevel;
			lastMousePos.y = mousePosition2.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
		}
		if (Input.GetMouseButtonUp(0))
		{
			Vector3 mousePosition3 = Input.mousePosition;
			lastMousePos.x = mousePosition3.x / (float)mGraphics.zoomLevel;
			lastMousePos.y = mousePosition3.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
			GameMidlet.gameCanvas.pointerReleased((int)(mousePosition3.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition3.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
		}
		if (isPC)
		{
			GameMidlet.gameCanvas.scrollMouse((int)(Input.GetAxis("Mouse ScrollWheel") * 10f));
			float x = Input.mousePosition.x;
			float y = Input.mousePosition.y;
			int x2 = (int)x / mGraphics.zoomLevel;
			int y2 = (Screen.height - (int)y) / mGraphics.zoomLevel;
			GameMidlet.gameCanvas.pointerMouse(x2, y2);
		}
	}

	private void checkInput()
	{
		if (Event.current == null || GameMidlet.gameCanvas == null)
		{
			return;
		}
		if (Input.anyKeyDown && Event.current.type == EventType.KeyDown)
		{
			int num = MyKeyMap.map(Event.current.keyCode);
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				switch (Event.current.keyCode)
				{
				case KeyCode.Alpha2:
					num = 64;
					break;
				case KeyCode.Minus:
					num = 95;
					break;
				}
			}
			if (num != 0)
			{
				GameMidlet.gameCanvas.keyPressedz(num);
			}
		}
		if (Event.current.type == EventType.KeyUp)
		{
			int num2 = MyKeyMap.map(Event.current.keyCode);
			if (num2 != 0)
			{
				GameMidlet.gameCanvas.keyReleasedz(num2);
			}
		}
	}

	private void OnApplicationQuit()
	{
		GameEvents.OnGameClosing();
		Debug.LogWarning("APP QUIT");
		GameCanvas.bRun = false;
		Session_ME.gI().close();
		Session_ME2.gI().close();
		if (isPC)
		{
			Application.Quit();
		}
	}

	private void OnApplicationPause(bool paused)
	{
		isResume = false;
		if (paused)
		{
			if (GameCanvas.isWaiting())
			{
				isQuitApp = true;
			}
		}
		else
		{
			isResume = true;
		}
		if (TouchScreenKeyboard.visible && TField.kb != null)
		{
			TField.kb.active = false;
			TField.kb = null;
		}
		if (isQuitApp)
		{
			Application.Quit();
		}
		GameEvents.OnGamePause(paused);
	}

	public static void exit()
	{
		if (isPC)
		{
			main.OnApplicationQuit();
		}
		else
		{
			a = 0;
		}
	}

	public static bool detectCompactDevice()
	{
		if (iPhoneSettings.generation == iPhoneGeneration.iPhone || iPhoneSettings.generation == iPhoneGeneration.iPhone3G || iPhoneSettings.generation == iPhoneGeneration.iPodTouch1Gen || iPhoneSettings.generation == iPhoneGeneration.iPodTouch2Gen)
		{
			return false;
		}
		return true;
	}

	public static bool checkCanSendSMS()
	{
		if (iPhoneSettings.generation == iPhoneGeneration.iPhone3GS || iPhoneSettings.generation == iPhoneGeneration.iPhone4 || iPhoneSettings.generation > iPhoneGeneration.iPodTouch4Gen)
		{
			return true;
		}
		return false;
	}
}
