using System;
using System.Reflection;
using UnityEngine;

namespace Mod
{
	[DefaultExecutionOrder(-1000)]
	internal sealed class SmoothFrame180Hz : MonoBehaviour
	{
		private static FieldInfo _paintCountField;
		private static FieldInfo _updateCountField;
		private static bool _reflectionReady;
		private static bool _hasTickSnapshot;
		private static bool _isInterpolatingPose;
		private static int _prevCx, _prevCy, _authCx, _authCy, _interpCx, _interpCy;
		private static int _prevCmx, _prevCmy, _authCmx, _authCmy, _interpCmx, _interpCmy;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 180;
			Time.fixedDeltaTime = 0.02f;

			GameObject host = (Main.main != null) ? Main.main.gameObject : new GameObject("SmoothFrame180HzHost");
			if (Main.main == null)
			{
				DontDestroyOnLoad(host);
			}
			if (host.GetComponent<SmoothFrame180Hz>() == null)
			{
				host.AddComponent<SmoothFrame180Hz>();
			}
			if (host.GetComponent<SmoothFrame180HzLate>() == null)
			{
				host.AddComponent<SmoothFrame180HzLate>();
			}
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
				_paintCountField?.SetValue(Main.main, 0);
				_updateCountField?.SetValue(Main.main, 1000000000);
			}
		}

		internal static void RestoreAuthoritativePoseBeforeTick()
		{
			UnlockMainPaintGate();
			if (!_hasTickSnapshot)
			{
				return;
			}
			Char me = Char.myCharz();
			if (me != null)
			{
				if (_isInterpolatingPose && me.cx == _interpCx && me.cy == _interpCy)
				{
					me.cx = _authCx;
					me.cy = _authCy;
				}
				else
				{
					_authCx = me.cx;
					_authCy = me.cy;
				}
				_prevCx = _authCx;
				_prevCy = _authCy;
			}
			if (_isInterpolatingPose && GameScr.cmx == _interpCmx && GameScr.cmy == _interpCmy)
			{
				GameScr.cmx = _authCmx;
				GameScr.cmy = _authCmy;
			}
			else
			{
				_authCmx = GameScr.cmx;
				_authCmy = GameScr.cmy;
			}
			_prevCmx = _authCmx;
			_prevCmy = _authCmy;
			_isInterpolatingPose = false;
		}

		internal static void RecordAuthoritativePoseAfterTick()
		{
			UnlockMainPaintGate();
			if (GameCanvas.currentScreen != GameScr.instance || Char.isLoadingMap || Char.ischangingMap)
			{
				_hasTickSnapshot = false;
				_isInterpolatingPose = false;
				return;
			}
			Char me = Char.myCharz();
			if (me == null)
			{
				return;
			}
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
			if (Application.targetFrameRate != 180)
			{
				QualitySettings.vSyncCount = 0;
				Application.targetFrameRate = 180;
			}
			UnlockMainPaintGate();
			if (!_hasTickSnapshot || GameCanvas.currentScreen != GameScr.instance || Char.isLoadingMap || Char.ischangingMap)
			{
				_isInterpolatingPose = false;
				return;
			}
			Char me = Char.myCharz();
			if (me == null)
			{
				return;
			}
			if (_isInterpolatingPose && (me.cx != _interpCx || me.cy != _interpCy))
			{
				_prevCx = _authCx = me.cx;
				_prevCy = _authCy = me.cy;
			}
			if (_isInterpolatingPose && (GameScr.cmx != _interpCmx || GameScr.cmy != _interpCmy))
			{
				_prevCmx = _authCmx = GameScr.cmx;
				_prevCmy = _authCmy = GameScr.cmy;
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

		private void FixedUpdate()
		{
			RestoreAuthoritativePoseBeforeTick();
		}
	}

	[DefaultExecutionOrder(1000)]
	internal sealed class SmoothFrame180HzLate : MonoBehaviour
	{
		private void FixedUpdate()
		{
			SmoothFrame180Hz.RecordAuthoritativePoseAfterTick();
		}

		private void Update()
		{
			SmoothFrame180Hz.ApplySubTickPoseForRender();
		}
	}
}
