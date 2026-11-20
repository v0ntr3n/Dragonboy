using System.Reflection;
using UnityEngine;

namespace Mod
{
	[DefaultExecutionOrder(-1000)]
	public sealed class SmoothFrame180Hz : MonoBehaviour
	{
		static SmoothFrame180Hz _instance;
		static FieldInfo _paintCountField;
		static FieldInfo _updateCountField;
		static bool _reflectionReady;
		static readonly object ZeroBox = 0;
		static readonly object MaxBox = int.MaxValue / 2;

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
			if (!_reflectionReady)
			{
				_reflectionReady = true;
				_paintCountField = typeof(Main).GetField("paintCount", BindingFlags.Instance | BindingFlags.NonPublic);
				_updateCountField = typeof(Main).GetField("updateCount", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			if (Main.main != null)
			{
				_paintCountField?.SetValue(Main.main, ZeroBox);
				_updateCountField?.SetValue(Main.main, MaxBox);
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
			GameEvents.PreProcessIncomingNetworkQueue();
			GameEvents.ApplySubTickPoseForRender();
		}

		void FixedUpdate()
		{
			UnlockMainPaintGate();
			GameEvents.PreProcessIncomingNetworkQueue();
			GameEvents.BeginFixedTick();
		}

		void Update()
		{
			UnlockMainPaintGate();
			GameEvents.PreProcessIncomingNetworkQueue();
			GameEvents.RestoreAuthoritativePose();
		}

		void LateUpdate()
		{
			GameEvents.RestoreAuthoritativePose();
		}
	}
}
