using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mod.PickMob.Services
{
	public static class ZoneService
	{
		const float ZONE_CHANGE_TIMEOUT = 8f;
		const long NO_MOB_WAIT_MS = 1500;
		const long ZONE_CHANGE_COOLDOWN_MS = 2000;
		const long PRIORITY_ZONE_TRACK_MS = 30000; // 30 seconds

		static readonly List<int> _candidateZones = new List<int>(32);

		static long _noMobSinceMs;
		static long _lastZoneChangeMs;
		static long _priorityZoneTrackSinceMs;
		static int? _cachedPriorityEmptyZoneId;

		public static void ResetNoMobTimer()
		{
			_noMobSinceMs = 0;
		}

		public static void ResetPriorityZoneTimer()
		{
			_priorityZoneTrackSinceMs = 0;
			_cachedPriorityEmptyZoneId = null;
		}

		public static bool ShouldChangeZone()
		{
			if (!PickMobConfig.IsAutoChangeZone || !MobPicker.AreAllTanSatMobsRespawning())
			{
				_noMobSinceMs = 0;
				return false;
			}

			long now = mSystem.currentTimeMillis();

			if (_noMobSinceMs == 0)
			{
				_noMobSinceMs = now;
			}

			return now - _noMobSinceMs >= NO_MOB_WAIT_MS && now - _lastZoneChangeMs >= ZONE_CHANGE_COOLDOWN_MS;
		}

		public static bool TryGetPriorityEmptyZone(out int emptyZoneId)
		{
			emptyZoneId = -1;
			if (!PickMobConfig.IsPriorityZone)
			{
				ResetPriorityZoneTimer();
				return false;
			}

			int currentPlayerCount = GetCurrentZonePlayerCount();
			if (currentPlayerCount <= 1)
			{
				ResetPriorityZoneTimer();
				return false;
			}

			long now = mSystem.currentTimeMillis();

			if (_priorityZoneTrackSinceMs == 0)
			{
				_priorityZoneTrackSinceMs = now;
				return false;
			}

			// After 30 seconds, check if still crowded
			if (now - _priorityZoneTrackSinceMs >= PRIORITY_ZONE_TRACK_MS)
			{
				if (now - _lastZoneChangeMs < ZONE_CHANGE_COOLDOWN_MS)
				{
					return false;
				}

				int? emptyZone = FindEmptyZoneId();
				ResetPriorityZoneTimer();
				if (emptyZone.HasValue)
				{
					_cachedPriorityEmptyZoneId = emptyZone.Value;
					emptyZoneId = emptyZone.Value;
					return true;
				}
			}

			return false;
		}

		public static bool ShouldChangeZoneByPriority()
		{
			return TryGetPriorityEmptyZone(out _);
		}

		static int GetCurrentZonePlayerCount()
		{
			GameScr gameScr = GameScr.gI();
			if (gameScr == null)
			{
				return 0;
			}

			int[] zones = gameScr.zones;
			int[] numPlayer = gameScr.numPlayer;
			int currentZone = TileMap.zoneID;

			if (zones == null || numPlayer == null)
			{
				return 0;
			}

			int count = System.Math.Min(zones.Length, numPlayer.Length);
			for (int i = 0; i < count; i++)
			{
				if (i < zones.Length && zones[i] == currentZone)
				{
					return numPlayer[i];
				}
			}

			return 0;
		}

		static int? FindEmptyZoneId()
		{
			GameScr gameScr = GameScr.gI();
			if (gameScr == null)
			{
				return null;
			}

			int[] zones = gameScr.zones;
			int[] numPlayer = gameScr.numPlayer;

			if (zones == null || numPlayer == null)
			{
				return null;
			}

			int count = System.Math.Min(zones.Length, numPlayer.Length);
			_candidateZones.Clear();

			for (int i = 0; i < count; i++)
			{
				if (i < zones.Length && numPlayer[i] == 0 && zones[i] != TileMap.zoneID)
				{
					_candidateZones.Add(zones[i]);
				}
			}

			if (_candidateZones.Count == 0)
			{
				return null;
			}

			int randomIndex = Random.Range(0, _candidateZones.Count);
			return _candidateZones[randomIndex];
		}

		public static IEnumerator ChangeToLowestPlayerZone()
		{
			long now = mSystem.currentTimeMillis();
			if (now - _lastZoneChangeMs < ZONE_CHANGE_COOLDOWN_MS)
			{
				yield break;
			}

			_lastZoneChangeMs = now;
			_noMobSinceMs = 0;

			int? targetZone = FindLowestPlayerZoneId();
			if (!targetZone.HasValue)
			{
				yield break;
			}

			int startMapId = TileMap.mapID;
			Service.gI().requestChangeZone(targetZone.Value, 0);

			float deadline = Time.realtimeSinceStartup + ZONE_CHANGE_TIMEOUT;
			while (Time.realtimeSinceStartup < deadline)
			{
				if (TileMap.zoneID == targetZone.Value || TileMap.mapID != startMapId)
				{
					yield break;
				}

				if (MobPicker.HasAttackableMob())
				{
					yield break;
				}

				yield return null;
			}
		}

		public static IEnumerator ChangeToEmptyZone()
		{
			int? targetZone = _cachedPriorityEmptyZoneId ?? FindEmptyZoneId();
			_cachedPriorityEmptyZoneId = null;
			if (!targetZone.HasValue)
			{
				_lastZoneChangeMs = mSystem.currentTimeMillis();
				ResetPriorityZoneTimer();
				yield break;
			}

			yield return ChangeToEmptyZone(targetZone.Value);
		}

		public static IEnumerator ChangeToEmptyZone(int targetZone)
		{
			_cachedPriorityEmptyZoneId = null;
			long now = mSystem.currentTimeMillis();
			if (now - _lastZoneChangeMs < ZONE_CHANGE_COOLDOWN_MS)
			{
				yield break;
			}

			int startMapId = TileMap.mapID;
			_lastZoneChangeMs = now;
			_noMobSinceMs = 0;
			ResetPriorityZoneTimer();

			Service.gI().requestChangeZone(targetZone, 0);

			float deadline = Time.realtimeSinceStartup + ZONE_CHANGE_TIMEOUT;
			while (Time.realtimeSinceStartup < deadline)
			{
				if (TileMap.zoneID == targetZone || TileMap.mapID != startMapId)
				{
					yield break;
				}

				if (MobPicker.HasAttackableMob())
				{
					yield break;
				}

				yield return null;
			}
		}

		static int? FindLowestPlayerZoneId()
		{
			GameScr gameScr = GameScr.gI();
			if (gameScr == null)
			{
				return null;
			}

			int[] zones = gameScr.zones;
			int[] numPlayer = gameScr.numPlayer;

			if (zones == null || numPlayer == null)
			{
				return null;
			}

			int count = System.Math.Min(zones.Length, numPlayer.Length);
			_candidateZones.Clear();
			int minPlayer = int.MaxValue;

			for (int i = 0; i < count; i++)
			{
				if (i >= zones.Length)
				{
					break;
				}

				int zoneId = zones[i];
				int playerCount = numPlayer[i];
				if (playerCount < minPlayer)
				{
					minPlayer = playerCount;
					_candidateZones.Clear();
					if (zoneId != TileMap.zoneID)
					{
						_candidateZones.Add(zoneId);
					}
				}
				else if (playerCount == minPlayer && zoneId != TileMap.zoneID)
				{
					_candidateZones.Add(zoneId);
				}
			}

			if (_candidateZones.Count == 0)
			{
				return null;
			}

			int randomIndex = Random.Range(0, _candidateZones.Count);
			return _candidateZones[randomIndex];
		}
	}
}
