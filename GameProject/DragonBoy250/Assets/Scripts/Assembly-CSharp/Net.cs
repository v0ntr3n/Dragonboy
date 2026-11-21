using UnityEngine;
using UnityEngine.Networking;

internal class Net
{
	public static UnityWebRequest www;

	public static Command h;

	public static void update()
	{
		if (www != null && www.isDone)
		{
			string str = (www.result == UnityWebRequest.Result.Success && www.downloadHandler != null) ? www.downloadHandler.text : string.Empty;
			www.Dispose();
			www = null;
			if (h != null)
			{
				h.perform(str);
			}
		}
	}

	public static void connectHTTP(string link, Command h)
	{
		if (www != null)
		{
			Cout.LogError("GET HTTP BUSY");
			www.Dispose();
			www = null;
		}
		Net.h = h;
		if (string.IsNullOrEmpty(link))
		{
			if (h != null)
			{
				h.perform(string.Empty);
			}
			return;
		}
		www = UnityWebRequest.Get(link);
		www.SendWebRequest();
	}

	public static void connectHTTP2(string link, Command h)
	{
		if (www != null)
		{
			www.Dispose();
			www = null;
		}
		Net.h = h;
		if (link != null && h != null)
		{
			h.perform(link);
		}
	}
}
