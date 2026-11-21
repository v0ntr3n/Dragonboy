using UnityEngine;

namespace Mod
{
	[DefaultExecutionOrder(-1000)]
	public sealed class SmoothFrame180Hz : MonoBehaviour
	{
		static SmoothFrame180Hz _instance;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 180;
			GameEvents.EnsureMainThreadRegistered();
			GameEvents.ApplyStaticServerDefaultsOnly();
			UnlockMainPaintGate();

			if (_instance == null)
			{
				GameObject host = new GameObject("Mod_SmoothFrame180Hz");
				UnityEngine.Object.DontDestroyOnLoad(host);
				host.hideFlags = HideFlags.HideAndDontSave;
				_instance = host.AddComponent<SmoothFrame180Hz>();
			}
		}

		void Awake()
		{
			useGUILayout = false;
		}

		internal static void UnlockMainPaintGate()
		{
			if (Main.main != null)
			{
				Main.main.paintCount = 0;
				Main.main.updateCount = 1;
			}
		}

		void OnGUI()
		{
			if (UnityEngine.Event.current == null || UnityEngine.Event.current.type != UnityEngine.EventType.Repaint)
			{
				GameEvents.RestoreAuthoritativePose();
				return;
			}
			UnlockMainPaintGate();
			GameEvents.ApplySubTickPoseForRender();
		}

		void FixedUpdate()
		{
			UnlockMainPaintGate();
			GameEvents.BeginFixedTick();
		}

		void Update()
		{
			UnlockMainPaintGate();
			GameEvents.RestoreAuthoritativePose();
		}

		void LateUpdate()
		{
			GameEvents.RestoreAuthoritativePose();
		}
	}
}
