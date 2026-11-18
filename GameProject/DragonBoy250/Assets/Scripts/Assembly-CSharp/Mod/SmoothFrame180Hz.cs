using System.Reflection;
using UnityEngine;

namespace Mod
{
	internal static class SmoothFrame180Hz
	{
		static FieldInfo _paintCountField;
		static FieldInfo _updateCountField;
		static bool _reflectionReady;

		static bool _hasSnapshot;
		static bool _isInterpolating;
		static int _prevCx, _prevCy, _authCx, _authCy, _interpCx, _interpCy;
		static int _prevCmx, _prevCmy, _authCmx, _authCmy, _interpCmx, _interpCmy;
		static int _lastMapId = -1;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 180;
			GameEvents.ApplyTeamobi2026ServerConfig();

			GameObject host = new GameObject("Mod_SmoothFrame180Hz");
			Object.DontDestroyOnLoad(host);
			host.hideFlags = HideFlags.HideAndDontSave;
			host.AddComponent<SmoothFrame180HzBefore>();
			host.AddComponent<SmoothFrame180HzAfter>();
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

		internal static void OnBeforeFixedTick()
		{
			UnlockMainPaintGate();
			if (!_hasSnapshot)
				return;

			Char me = Char.myCharz();
			if (me != null)
			{
				if (_isInterpolating && me.cx == _interpCx && me.cy == _interpCy)
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

			if (_isInterpolating && GameScr.cmx == _interpCmx && GameScr.cmy == _interpCmy)
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
			_isInterpolating = false;
		}

		internal static void OnAfterFixedTick()
		{
			UnlockMainPaintGate();
			if (GameCanvas.currentScreen != GameScr.instance || Char.isLoadingMap || Char.ischangingMap)
			{
				_hasSnapshot = false;
				_isInterpolating = false;
				return;
			}

			if (_lastMapId != TileMap.mapID)
			{
				_lastMapId = TileMap.mapID;
				_hasSnapshot = false;
				_isInterpolating = false;
			}

			Char me = Char.myCharz();
			if (me == null)
				return;

			_authCx = me.cx;
			_authCy = me.cy;
			_authCmx = GameScr.cmx;
			_authCmy = GameScr.cmy;

			if (!_hasSnapshot || Mathf.Abs(_authCx - _prevCx) > 48 || Mathf.Abs(_authCy - _prevCy) > 48)
			{
				_prevCx = _authCx;
				_prevCy = _authCy;
			}
			if (!_hasSnapshot || Mathf.Abs(_authCmx - _prevCmx) > 96 || Mathf.Abs(_authCmy - _prevCmy) > 96)
			{
				_prevCmx = _authCmx;
				_prevCmy = _authCmy;
			}

			_hasSnapshot = true;
			_isInterpolating = false;
		}

		internal static void OnRenderFrame()
		{
			UnlockMainPaintGate();
			if (!_hasSnapshot || GameCanvas.currentScreen != GameScr.instance || Char.isLoadingMap || Char.ischangingMap)
			{
				_isInterpolating = false;
				return;
			}

			Char me = Char.myCharz();
			if (me == null)
				return;

			if (_isInterpolating && (me.cx != _interpCx || me.cy != _interpCy))
			{
				_prevCx = _authCx = me.cx;
				_prevCy = _authCy = me.cy;
			}
			if (_isInterpolating && (GameScr.cmx != _interpCmx || GameScr.cmy != _interpCmy))
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
			_isInterpolating = true;
		}
	}

	[DefaultExecutionOrder(-1000)]
	internal sealed class SmoothFrame180HzBefore : MonoBehaviour
	{
		void FixedUpdate()
		{
			SmoothFrame180Hz.OnBeforeFixedTick();
		}
	}

	[DefaultExecutionOrder(1000)]
	internal sealed class SmoothFrame180HzAfter : MonoBehaviour
	{
		void FixedUpdate()
		{
			SmoothFrame180Hz.OnAfterFixedTick();
		}

		void Update()
		{
			SmoothFrame180Hz.OnRenderFrame();
		}
	}
}
