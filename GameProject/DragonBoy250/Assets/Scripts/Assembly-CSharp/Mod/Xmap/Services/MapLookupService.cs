using System;
using JetBrains.Annotations;
using Mod.R;

namespace Mod.Xmap
{
	internal sealed class MapLookupService
	{
		internal const short IdItemCapsuleVip = 194;
		internal const short IdItemCapsuleNormal = 193;
		public const int IdMapHomeBase = 21;
		public const int IdMapLangBase = 7;
		internal const int IdMapTTVTBase = 24;

		internal int GetGateX(sbyte type)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint.maxX < 60 && type == 0)
				{
					return 15;
				}

				if (waypoint.minX > TileMap.pxw - 60 && type == 2)
				{
					return TileMap.pxw - 15;
				}
			}

			return 0;
		}

		internal int GetGateY(sbyte type)
		{
			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint.maxX < 60 && type == 0)
				{
					return waypoint.maxY;
				}

				if (waypoint.minX > TileMap.pxw - 60 && type == 2)
				{
					return waypoint.maxY;
				}
			}

			return 0;
		}

		[CanBeNull]
		internal Waypoint FindWaypoint(int mapId)
		{
			if (TileMap.vGo == null || TileMap.mapNames == null || mapId < 0 || mapId >= TileMap.mapNames.Length)
			{
				return null;
			}

			for (int i = 0; i < TileMap.vGo.size(); i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				if (waypoint?.popup?.says == null)
				{
					continue;
				}

				string textPopup = Utils.getTextPopup(waypoint.popup);
				if (textPopup != null && textPopup.Equals(TileMap.mapNames[mapId]))
				{
					return waypoint;
				}
			}

			return null;
		}

		internal int ResolveMapIdFromName(string mapName)
		{
			if (string.IsNullOrEmpty(mapName))
			{
				return -1;
			}

			int offset = Char.myCharz().cgender;
			if (mapName.Contains(LocalizedString.goHome))
			{
				return IdMapHomeBase + offset;
			}

			if (mapName.Contains(LocalizedString.spaceshipStation))
			{
				return IdMapTTVTBase + offset;
			}

			string[] mapNames = TileMap.mapNames;
			if (mapNames == null)
			{
				return -1;
			}

			string trimmedName = mapName.Trim();
			for (int i = 0; i < mapNames.Length; i++)
			{
				string candidate = mapNames[i];
				if (!string.IsNullOrEmpty(candidate) && (mapName.Equals(candidate, StringComparison.Ordinal) || trimmedName.Equals(candidate, StringComparison.Ordinal)))
				{
					return i;
				}
			}

			int bestMapId = -1;
			int bestLength = 0;
			for (int i = 0; i < mapNames.Length; i++)
			{
				string candidate = mapNames[i];
				if (!string.IsNullOrEmpty(candidate) && candidate.Length > bestLength && mapName.Contains(candidate))
				{
					bestLength = candidate.Length;
					bestMapId = i;
				}
			}

			return bestMapId;
		}

		internal int GetHomeMapId(int characterGender)
		{
			return IdMapHomeBase + characterGender;
		}

		internal int GetVillageMapId(int characterGender)
		{
			return IdMapLangBase * characterGender;
		}

		internal bool HasCapsuleVipInBag()
		{
			Item[] items = Char.myCharz()?.arrItemBag;
			if (items == null)
			{
				return false;
			}

			for (int i = 0; i < items.Length; i++)
			{
				Item item = items[i];
				if (item?.template != null && item.template.id == IdItemCapsuleVip)
				{
					return true;
				}
			}

			return false;
		}

		internal bool HasCapsuleNormalInBag()
		{
			Item[] items = Char.myCharz()?.arrItemBag;
			if (items == null)
			{
				return false;
			}

			for (int i = 0; i < items.Length; i++)
			{
				Item item = items[i];
				if (item?.template != null && item.template.id == IdItemCapsuleNormal && item.quantity > 10)
				{
					return true;
				}
			}

			return false;
		}
	}

}
