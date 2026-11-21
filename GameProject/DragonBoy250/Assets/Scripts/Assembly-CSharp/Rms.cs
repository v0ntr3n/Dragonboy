using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Mod;
using UnityEngine;

public class Rms
{
	private sealed class RmsRequest
	{
		public string Filename;

		public sbyte[] Data;

		public bool IsSave;

		public ManualResetEventSlim Done;

		private int _pendingOwners;

		public RmsRequest(string filename, sbyte[] data, bool isSave)
		{
			Filename = filename;
			Data = data;
			IsSave = isSave;
			Done = new ManualResetEventSlim(false);
			_pendingOwners = 2;
		}

		public void Signal()
		{
			try
			{
				Done.Set();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		public void Release()
		{
			if (Interlocked.Decrement(ref _pendingOwners) == 0)
			{
				Done.Dispose();
			}
		}
	}

	const int INTERVAL = 5;

	const int MAXTIME = 500;

	private static readonly ConcurrentQueue<RmsRequest> _requestQueue = new ConcurrentQueue<RmsRequest>();

	public static volatile int status;

	public static sbyte[] data;

	public static string filename;

	public static string RMS_ResVersion = "ResVersion";

	public static string RMS_clienttype = "clienttype";

	public static string RMS_lastZoomlevel = "lastZoomlevel";

	public static string RMS_userAo = "userAo";

	public static string RMS_acc = "acc";

	public static string RMS_pass = "pass";

	public static string RMS_check = "check";

	public static void saveRMS(string filename, sbyte[] data)
	{
		if (Main.IsMainThread())
		{
			__saveRMS(filename, data);
		}
		else
		{
			_saveRMS(filename, data);
		}
	}

	public static sbyte[] loadRMS(string filename)
	{
		if (Main.IsMainThread())
		{
			return __loadRMS(filename);
		}
		return _loadRMS(filename);
	}

	public static string loadRMSString(string fileName)
	{
		sbyte[] array = loadRMS(fileName);
		if (array == null)
		{
			return null;
		}
		DataInputStream dataInputStream = new DataInputStream(array);
		try
		{
			string result = dataInputStream.readUTF();
			dataInputStream.close();
			return result;
		}
		catch (Exception ex)
		{
			Cout.println(ex.StackTrace);
		}
		return null;
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

	public static void saveRMSString(string filename, string data)
	{
		DataOutputStream dataOutputStream = new DataOutputStream();
		try
		{
			dataOutputStream.writeUTF(data);
			saveRMS(filename, dataOutputStream.toByteArray());
			dataOutputStream.close();
		}
		catch (Exception ex)
		{
			Cout.println(ex.StackTrace);
		}
		GameEvents.OnSaveRMSString(ref filename, ref data);
	}

	static void _saveRMS(string filename, sbyte[] data)
	{
		RmsRequest req = new RmsRequest(filename, data, true);
		_requestQueue.Enqueue(req);
		try
		{
			if (!req.Done.Wait(INTERVAL * MAXTIME))
			{
				Debug.LogError("TOO LONG TO SAVE RMS " + filename);
			}
		}
		finally
		{
			req.Release();
		}
	}

	static sbyte[] _loadRMS(string filename)
	{
		RmsRequest req = new RmsRequest(filename, null, false);
		_requestQueue.Enqueue(req);
		try
		{
			if (!req.Done.Wait(INTERVAL * MAXTIME))
			{
				Debug.LogError("TOO LONG TO LOAD RMS " + filename);
				return null;
			}
			return req.Data;
		}
		finally
		{
			req.Release();
		}
	}

	public static void update()
	{
		while (_requestQueue.TryDequeue(out RmsRequest req))
		{
			if (req == null)
			{
				continue;
			}
			try
			{
				if (req.IsSave)
				{
					__saveRMS(req.Filename, req.Data);
				}
				else
				{
					req.Data = __loadRMS(req.Filename);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				req.Signal();
				req.Release();
			}
		}
	}

	public static int loadRMSInt(string file)
	{
		sbyte[] array = loadRMS(file);
		return (array != null && array.Length > 0) ? array[0] : (-1);
	}

	public static void saveRMSInt(string file, int x)
	{
		try
		{
			saveRMS(file, new sbyte[1]
			{
				(sbyte)x
			});
			if (file == ServerListScreen.RMS_svselect)
			{
			}
		}
		catch (Exception)
		{
		}
	}

	public static string GetiPhoneDocumentsPath()
	{
		if (GameEvents.OnGetRMSPath(out string result))
			return result;
		return Application.persistentDataPath;
	}

	static void __saveRMS(string filename, sbyte[] data)
	{
		if (data == null)
		{
			return;
		}
		GameEvents.OnRmsFileIo(() =>
		{
			string text = GetiPhoneDocumentsPath() + "/" + filename;
			string tmpPath = text + ".tmp";
			try
			{
				string dir = Path.GetDirectoryName(text);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				byte[] bytes = ArrayCast.cast(data);
				using (FileStream fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					fileStream.Write(bytes, 0, bytes.Length);
					fileStream.Flush(false);
				}
				if (File.Exists(text))
				{
					try
					{
						File.Replace(tmpPath, text, null);
					}
					catch (PlatformNotSupportedException)
					{
						File.Delete(text);
						File.Move(tmpPath, text);
					}
				}
				else
				{
					File.Move(tmpPath, text);
				}
				Main.setBackupIcloud(text);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				try
				{
					if (File.Exists(tmpPath))
					{
						File.Delete(tmpPath);
					}
				}
				catch (Exception)
				{
				}
			}
		});
	}

	static sbyte[] __loadRMS(string filename)
	{
		sbyte[] loaded = null;
		GameEvents.OnRmsFileIo(() =>
		{
			try
			{
				string path = GetiPhoneDocumentsPath() + "/" + filename;
				if (!File.Exists(path))
				{
					loaded = null;
					return;
				}
				byte[] array = File.ReadAllBytes(path);
				loaded = ArrayCast.cast(array);
			}
			catch (Exception)
			{
				loaded = null;
			}
		});
		return loaded;
	}

	public static void clearAll()
	{
		GameEvents.OnRmsFileIo(() =>
		{
			if (GameEvents.OnClearAllRMS())
				return;
			FileInfo[] files = new DirectoryInfo(GetiPhoneDocumentsPath() + "/").GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				fileInfo.Delete();
			}
		});
	}

	public static void DeleteStorage(string path)
	{
		try
		{
			GameEvents.OnRmsFileIo(() =>
			{
				File.Delete(GetiPhoneDocumentsPath() + "/" + path);
			});
		}
		catch (Exception)
		{
		}
	}

	public static string ByteArrayToString(byte[] ba)
	{
		string text = BitConverter.ToString(ba);
		return text.Replace("-", string.Empty);
	}

	public static byte[] StringToByteArray(string hex)
	{
		int length = hex.Length;
		byte[] array = new byte[length / 2];
		for (int i = 0; i < length; i += 2)
		{
			array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
		}
		return array;
	}

	public static void deleteRecord(string name)
	{
		try
		{
			PlayerPrefs.DeleteKey(name);
		}
		catch (Exception ex)
		{
			Cout.println("loi xoa RMS --------------------------" + ex);
		}
	}

	public static void clearRMS()
	{
		deleteRecord("data");
		deleteRecord("dataVersion");
		deleteRecord("map");
		deleteRecord("mapVersion");
		deleteRecord("skill");
		deleteRecord("killVersion");
		deleteRecord("item");
		deleteRecord("itemVersion");
	}

	public static void saveIP(string strID)
	{
		saveRMSString("NRIPlink", strID);
	}

	public static string loadIP()
	{
		string text = loadRMSString("NRIPlink");
		if (text == null)
		{
			return null;
		}
		return text;
	}
}
