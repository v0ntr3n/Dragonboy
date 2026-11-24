using UnityEngine;

public class MyAudioClip
{
	public string name;

	public AudioClip clip;

	public long timeStart;

	public MyAudioClip(string filename)
	{
		clip = null;
		name = filename;
	}

	public void Play()
	{
		timeStart = mSystem.currentTimeMillis();
	}

	public bool isPlaying()
	{
		return false;
	}
}
