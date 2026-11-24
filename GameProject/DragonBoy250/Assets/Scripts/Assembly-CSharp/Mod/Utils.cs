using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Mod.Constants;
using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mod
{
	internal static class Utils
	{
		internal static readonly sbyte ID_SKILL_BUFF = 7;
		internal static readonly short ID_ICON_ITEM_TDLT = 4387;
		internal static readonly short ID_NPC_MOD_FACE = 7333; // Doraemon

		internal static string status = "Đã kết nối";

		internal static readonly int myCharSpeed = 8;
		internal static readonly float timeScale = 2;

		internal static Waypoint waypointLeft;
		internal static Waypoint waypointMiddle;
		internal static Waypoint waypointRight;
		static int _cachedWaypointMapId = -1;
		static int _cachedWaypointVGoCount = -1;
		static long _changingMapSinceMs;

		internal static string username = "";
		internal static string password = "";
		internal static JObject server = null;
		internal static JObject sizeData = null;

		internal static int channelSyncKey = -1;

		static bool isOpenedByExternalAccountManager;
		internal static bool IsOpenedByExternalAccountManager { get; set; }

		static readonly GUIContent _sharedGuiContent = new GUIContent();
		static readonly Dictionary<(string, int, int), string> _trimCache = new Dictionary<(string, int, int), string>();


		/// <summary>
		///     Kiểm tra xem game đang chạy trên Android hay không.
		/// </summary>
		/// <returns>Trả về true nếu đang chạy trên Android, ngược lại trả về false.</returns>
		internal static bool IsMobile()
		{
			return Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
		}

		/// <summary>
		///     Kiểm tra xem game đang chạy trên Linux hay không.
		/// </summary>
		/// <returns>Trả về true nếu đang chạy trên Linux, ngược lại trả về false.</returns>
		internal static bool IsLinuxBuild()
		{
			return Application.platform == RuntimePlatform.LinuxPlayer;
		}

		/// <summary>
		///     Kiểm tra xem game đang chạy trên Windows hay không.
		/// </summary>
		/// <returns>Trả về true nếu đang chạy trên Windows, ngược lại trả về false.</returns>
		internal static bool IsWindowsBuild()
		{
			return Application.platform == RuntimePlatform.WindowsPlayer;
		}

		internal static bool IsMacBuild()
		{
			return Application.platform == RuntimePlatform.OSXPlayer;
		}

		/// <summary>
		///     Kiểm tra xem game có đang chạy trên Unity Editor hay không.
		/// </summary>
		/// <returns>Trả về true nếu game đang chạy trên Editor, ngược lại trả về false.</returns>
		internal static bool IsEditor()
		{
			return Application.isEditor;
		}

		/// <summary>
		///     Kiểm tra xem game đang chạy trên PC hay không.
		/// </summary>
		/// <returns>Trả về true nếu đang chạy trên PC, ngược lại trả về false.</returns>
		internal static bool IsPC()
		{
			return !IsMobile();
		}

		internal static MyVector getMyVectorMe()
		{
			MyVector vMe = new MyVector();
			vMe.addElement(Char.myCharz());
			return vMe;
		}

		/// <summary>
		///     Kiểm tra khả năng sử dụng skill Trị thương vào bản thân.
		/// </summary>
		/// <param name="skillBuff">Skill trị thương.</param>
		/// <returns>true nếu có thể sử dụng skill trị thương vào bản thân.</returns>
		internal static bool canBuffMe(out Skill skillBuff)
		{
			skillBuff = Char.myCharz().getSkill(new SkillTemplate
			{
				id = ID_SKILL_BUFF
			});

			if (skillBuff == null)
			{
				return false;
			}

			return true;
		}

		internal static string getTextPopup(PopUp popUp)
		{
			if (popUp == null || popUp.says == null)
				return string.Empty;
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < popUp.says.Length; i++)
			{
				stringBuilder.Append(popUp.says[i]);
				stringBuilder.Append(" ");
			}
			return stringBuilder.ToString().Trim();
		}

		private static string GetMapNameSafe(int mapId)
		{
			if (TileMap.mapNames == null || mapId < 0 || mapId >= TileMap.mapNames.Length)
				return null;
			return TileMap.mapNames[mapId];
		}

		/// <summary>
		///     Kiểm tra trạng thái sử dụng TĐLT.
		/// </summary>
		/// <returns>true nếu đang sử dụng tự động luyện tập</returns>
		internal static bool isUsingTDLT()
		{
			return ItemTime.isExistItem(ID_ICON_ITEM_TDLT);
		}

		/// <summary>
		///     Sử dụng một item có id là một trong số các id truyền vào.
		/// </summary>
		/// <param name="templatesId">Mảng chứa các id của các item muốn sử dụng.</param>
		/// <returns>true nếu có vật phẩm được sử dụng.</returns>
		internal static sbyte getIndexItemBag(params short[] templatesId)
		{
			Item[] arr = Char.myCharz()?.arrItemBag;
			if (arr == null || templatesId == null)
				return -1;
			int length = System.Math.Min(arr.Length, sbyte.MaxValue + 1);
			for (int i = 0; i < length; i++)
			{
				Item item = arr[i];
				if (item?.template != null && templatesId.Contains(item.template.id))
				{
					return (sbyte)i;
				}
			}

			return -1;
		}

		static void teleToNpc(Npc npc)
		{
			TeleportMyChar(npc.cx, npc.ySd - npc.ySd % 24);
			Char.myCharz().npcFocus = npc;
		}

		internal static Npc findNpc(int npcId)
		{
			for (int i = 0; i < GameScr.vNpc.size(); i++)
			{
				Npc npc = (Npc)GameScr.vNpc.elementAt(i);
				if (npc.template.npcTemplateId == npcId)
				{
					return npc;
				}
			}
			return null;
		}

		internal static void teleToNpc(int npcId)
		{
			for (int i = 0; i < GameScr.vNpc.size(); i++)
			{
				Npc npc = (Npc)GameScr.vNpc.elementAt(i);
				if (npc.template.npcTemplateId == npcId)
				{
					teleToNpc(npc);
					return;
				}
			}
		}

		static bool PopupMatchesMap(string textPopup, int mapId)
		{
			string mapName = GetMapNameSafe(mapId);
			return !string.IsNullOrEmpty(textPopup) && !string.IsNullOrEmpty(mapName) && string.Equals(textPopup.Trim(), mapName.Trim(), System.StringComparison.OrdinalIgnoreCase);
		}

		internal static void requestChangeMap(Waypoint waypoint)
		{
			if (waypoint == null)
				return;
			_changingMapSinceMs = mSystem.currentTimeMillis();
			if (waypoint.isOffline || TileMap.isTrainingMap())
			{
				Service.gI().getMapOffline();
				Char.ischangingMap = true;
				return;
			}
			Service.gI().requestChangeMap();
			Char.ischangingMap = true;
		}

		static void setWaypointChangeMap(Waypoint waypoint)
		{
			if (waypoint == null)
				return;
			int cMapID = TileMap.mapID;
			string textPopup = getTextPopup(waypoint.popup);

			if (cMapID == 27 && PopupMatchesMap(textPopup, 53))
				return;

			if (cMapID == 70 && PopupMatchesMap(textPopup, 69) ||
			    cMapID == 73 && PopupMatchesMap(textPopup, 67) ||
			    cMapID == 110 && PopupMatchesMap(textPopup, 106))
			{
				waypointLeft = waypoint;
				return;
			}

			if ((cMapID == 106 || cMapID == 107) && PopupMatchesMap(textPopup, 110) ||
			    (cMapID == 105 || cMapID == 108) && PopupMatchesMap(textPopup, 109) ||
			    cMapID == 109 && PopupMatchesMap(textPopup, 105))
			{
				waypointMiddle = waypoint;
				return;
			}

			if (cMapID == 70 && PopupMatchesMap(textPopup, 71))
			{
				waypointRight = waypoint;
				return;
			}

			if (cMapID >= 54 && cMapID <= 57)
			{
				if (waypoint.minX <= 168)
					waypointLeft = waypoint;
				else
					waypointRight = waypoint;
				return;
			}

			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * TileMap.size;
			if (waypoint.maxX <= 60 || (waypoint.minX <= 96 && waypoint.maxX <= 168 && !waypoint.isOffline && (mapW <= 0 || waypoint.maxX <= mapW / 3)))
			{
				waypointLeft = waypoint;
				return;
			}

			if ((!waypoint.isEnter && waypoint.minX >= 400) ||
			    (mapW > 0 && (waypoint.minX >= mapW - 120 || (waypoint.maxX >= mapW - 84 && !waypoint.isOffline && waypoint.minX >= mapW / 2))))
			{
				waypointRight = waypoint;
				return;
			}

			waypointMiddle = waypoint;
		}

		internal static void EnsureWaypointsLoaded()
		{
			int vGoSize = TileMap.vGo != null ? TileMap.vGo.size() : 0;
			if (_cachedWaypointMapId != TileMap.mapID || _cachedWaypointVGoCount != vGoSize || (waypointLeft == null && waypointMiddle == null && waypointRight == null && (vGoSize > 0 || TileMap.mapID == 45 || TileMap.mapID == 46)))
			{
				UpdateWaypointChangeMap();
			}
		}

		internal static void UpdateWaypointChangeMap()
		{
			waypointLeft = waypointMiddle = waypointRight = null;
			int vGoSize = TileMap.vGo != null ? TileMap.vGo.size() : 0;
			_cachedWaypointMapId = TileMap.mapID;
			_cachedWaypointVGoCount = vGoSize;

			if (TileMap.mapID == 46)
				waypointRight = new Waypoint(570, 576, 570, 576, true, false, GetMapNameSafe(47) ?? "Map 47");

			if (vGoSize == 0)
			{
				if (TileMap.mapID == 45)
					waypointMiddle = new Waypoint(570, 576, 570, 576, true, false, GetMapNameSafe(46) ?? "Map 46");
			}
			for (int i = 0; i < vGoSize; i++)
			{
				Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
				setWaypointChangeMap(waypoint);
			}
			EnsureCharAboveVoidOnMapLoad();
		}

		/// <summary>
		///     Sử dụng skill Trị thương của namec vào bản thân.
		/// </summary>
		[ChatCommand("hsme"), ChatCommand("buffme"), HotkeyCommand('b')]
		internal static void buffMe()
		{
			if (!canBuffMe(out Skill skillBuff))
			{
				GameScr.info1.addInfo("Không tìm thấy kỹ năng Trị thương", 0);
				return;
			}

			// Đổi sang skill hồi sinh
			Service.gI().selectSkill(ID_SKILL_BUFF);

			// Tự tấn công vào bản thân
			Service.gI().sendPlayerAttack(new MyVector(), getMyVectorMe(), -1);

			// Trả về skill cũ
			Service.gI().selectSkill(Char.myCharz().myskill.template.id);

			// Đặt thời gian hồi cho skill
			skillBuff.lastTimeUseThisSkill = mSystem.currentTimeMillis();
		}

		/// <summary>
		///     Dịch chuyển tới một toạ độ cụ thể trong map.
		/// </summary>
		/// <param name="x">Toạ độ x.</param>
		/// <param name="y">Toạ độ y.</param>
		internal static void TeleportMyChar(int x, int y)
		{
			Char me = Char.myCharz();
			if (me == null)
				return;
			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * TileMap.size;
			int mapH = TileMap.pxh > 0 ? TileMap.pxh : TileMap.tmh * TileMap.size;
			if (mapW > 24)
				x = Mathf.Clamp(x, 12, mapW - 12);
			else
				x = System.Math.Max(12, x);
			if (mapH > 24)
				y = Mathf.Clamp(y, 12, mapH - 24);
			else
				y = System.Math.Max(12, y);

			me.currentMovePoint = null;
			me.vMovePoints?.removeAllElements();
			me.skillPaint = null;
			me.isTeleport = false;
			me.telePortSkill = false;
			me.isFlyUp = false;
			Char.ischangingMap = false;
			Char.isLockKey = false;
			me.cvx = 0;
			me.cvy = 0;
			me.cp1 = 0;
			me.cp2 = 0;
			me.cp3 = 0;
			me.delayFall = 0;
			if (!TileMap.isInAirMap())
				me.statusMe = 1;

			me.cxSend = x - 1;
			me.cySend = y - 1;
			me.cx = x;
			me.cy = y;
			GameEvents.SyncTeleportPose(x, y);
			Service.gI().charMove();

			if (isUsingTDLT())
				return;

			int nudgeY = (mapH > 36 && y >= mapH - 24) ? (y - 1) : (y + 1);
			me.cx = x;
			me.cy = nudgeY;
			Service.gI().charMove();
			me.cx = x;
			me.cy = y;
			GameEvents.SyncTeleportPose(x, y);
			Service.gI().charMove();
		}

		internal static void TeleportToNPC(int npcId)
		{
			for (int i = 0; i < GameScr.vNpc.size(); i++)
			{
				Npc npc = (Npc)GameScr.vNpc.elementAt(i);
				if (npc.template.npcTemplateId == npcId)
				{
					TeleportMyChar(npc.cx - 24, npc.ySd - npc.ySd % 24);
					Char.myCharz().npcFocus = npc;
					return;
				}
			}
		}

		internal static bool useItem(short itemTemplateId)
		{
			sbyte index = getIndexItemBag(itemTemplateId);
			if (index == -1)
			{
				return false;
			}

			Service.gI().useItem(0, 1, index, -1);
			return true;
		}

		[CanBeNull]
		internal static Item getItemInBag(short itemTemplateId)
		{
			Item[] arr = Char.myCharz()?.arrItemBag;
			if (arr == null)
				return null;
			for (int i = 0; i < arr.Length; i++)
			{
				Item item = arr[i];
				if (item?.template != null && item.template.id == itemTemplateId)
				{
					return item;
				}
			}

			return null;
		}

		[CanBeNull]
		internal static Item getItemInBox(short itemTemplateId)
		{
			Item[] arr = Char.myCharz()?.arrItemBox;
			if (arr == null)
				return null;
			for (int i = 0; i < arr.Length; i++)
			{
				Item item = arr[i];
				if (item?.template != null && item.template.id == itemTemplateId)
				{
					return item;
				}
			}

			return null;
		}

		internal static void menuZone()
		{
			GameEvents.MarkExplicitZoneOpen();
			Service.gI().openUIZone();
			GameCanvas.panel.setTypeZone();
			GameCanvas.panel.show();
		}

		internal static bool TryFindGroundAtColumn(int x, int preferredY, out int groundY)
		{
			groundY = 0;
			int tileSize = TileMap.size > 0 ? TileMap.size : 24;
			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * tileSize;
			int mapH = TileMap.pxh > 0 ? TileMap.pxh : TileMap.tmh * tileSize;
			if (mapW <= 0 || mapH <= 0 || x < 0 || x >= mapW)
				return false;

			int bestDist = int.MaxValue;
			bool found = false;
			for (int y = tileSize; y <= mapH - tileSize; y += tileSize)
			{
				if (TileMap.tileTypeAt(x, y, 2))
				{
					if (preferredY <= 0)
					{
						groundY = y;
						return true;
					}
					int dist = System.Math.Abs(y - preferredY);
					if (dist < bestDist)
					{
						bestDist = dist;
						groundY = y;
						found = true;
					}
				}
			}
			return found;
		}

		internal static void ResolveWaypointTarget(Waypoint waypoint, out int targetX, out int targetY)
		{
			int tileSize = TileMap.size > 0 ? TileMap.size : 24;
			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * tileSize;
			int mapH = TileMap.pxh > 0 ? TileMap.pxh : TileMap.tmh * tileSize;
			if (waypoint == null)
			{
				targetX = 60;
				targetY = GetYGround(60);
				return;
			}

			if ((TileMap.mapID == 45 || TileMap.mapID == 46) && waypoint.minX == 570 && waypoint.maxY >= 500)
			{
				targetX = 570;
				targetY = mapH > tileSize ? System.Math.Min(576, mapH - tileSize) : 552;
				return;
			}

			if ((TileMap.mapID == 46 || TileMap.mapID == 47) && waypoint.minY <= 120 && waypoint.maxY <= 150)
			{
				targetX = waypoint.minX + (waypoint.maxX - waypoint.minX) / 2;
				targetY = System.Math.Max(24, (waypoint.minY + waypoint.maxY) / 2);
				return;
			}

			int refY = waypoint.maxY > 0 ? waypoint.maxY : System.Math.Max(24, (int)waypoint.minY);
			if (mapH > tileSize && refY > mapH - tileSize)
				refY = mapH - tileSize;

			bool isLeftEdge = waypoint.maxX <= 60 || (waypoint.minX <= 96 && waypoint.maxX <= 168 && !waypoint.isOffline && (mapW <= 0 || waypoint.maxX <= mapW / 3));
			bool isRightEdge = !isLeftEdge && ((!waypoint.isEnter && waypoint.minX >= 400) || (mapW > 0 && waypoint.minX >= mapW - 144));

			int midX = waypoint.minX + (waypoint.maxX - waypoint.minX) / 2;
			if (mapW > 36)
				midX = Mathf.Clamp(midX, 18, mapW - 18);

			if (TryFindGroundAtColumn(midX, refY, out int midGroundY) && System.Math.Abs(midGroundY - refY) <= tileSize * 3)
			{
				targetX = midX;
				targetY = midGroundY;
				return;
			}

			if (isLeftEdge)
			{
				for (int probeX = System.Math.Max(tileSize, (int)waypoint.maxX); probeX <= System.Math.Min(mapW - tileSize, (int)waypoint.maxX + tileSize * 4); probeX += tileSize)
				{
					if (TryFindGroundAtColumn(probeX, refY, out int gy) && System.Math.Abs(gy - refY) <= tileSize * 4)
					{
						targetX = probeX;
						targetY = gy;
						return;
					}
				}
			}
			else if (isRightEdge && mapW > tileSize * 2)
			{
				int startProbe = System.Math.Min(mapW - tileSize, (int)waypoint.minX);
				for (int probeX = startProbe; probeX >= System.Math.Max(tileSize, startProbe - tileSize * 4); probeX -= tileSize)
				{
					if (TryFindGroundAtColumn(probeX, refY, out int gy) && System.Math.Abs(gy - refY) <= tileSize * 4)
					{
						targetX = probeX;
						targetY = gy;
						return;
					}
				}
			}

			targetX = midX;
			targetY = refY;
		}

		internal static void TeleportToSafeEdge(bool leftSide)
		{
			int tileSize = TileMap.size > 0 ? TileMap.size : 24;
			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * tileSize;
			int prefY = Char.myCharz() != null && Char.myCharz().cy > 0 ? Char.myCharz().cy : 0;
			if (mapW <= tileSize * 4)
			{
				int mid = System.Math.Max(24, mapW / 2);
				TeleportMyChar(mid, GetYGround(mid));
				return;
			}
			if (leftSide)
			{
				for (int x = 60; x <= mapW / 2; x += tileSize)
				{
					if (TryFindGroundAtColumn(x, prefY, out int gy))
					{
						TeleportMyChar(x, gy);
						return;
					}
				}
				TeleportMyChar(60, GetYGround(60));
			}
			else
			{
				for (int x = mapW - 60; x >= mapW / 2; x -= tileSize)
				{
					if (TryFindGroundAtColumn(x, prefY, out int gy))
					{
						TeleportMyChar(x, gy);
						return;
					}
				}
				TeleportMyChar(mapW - 60, GetYGround(mapW - 60));
			}
		}

		internal static void EnsureCharAboveVoidOnMapLoad()
		{
			Char me = Char.myCharz();
			if (me == null || TileMap.isInAirMap())
				return;
			int tileSize = TileMap.size > 0 ? TileMap.size : 24;
			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * tileSize;
			int mapH = TileMap.pxh > 0 ? TileMap.pxh : TileMap.tmh * tileSize;
			if (mapH <= tileSize)
				return;
			bool corrected = false;
			if (me.isFlyUp)
			{
				me.isFlyUp = false;
				Char.isLockKey = false;
				if (me.cxFocus > 0 && (mapW <= 0 || me.cxFocus < mapW))
				{
					me.cx = me.cxFocus;
					corrected = true;
				}
			}
			if (mapW > tileSize * 2 && (me.cx < 12 || me.cx > mapW - 12))
			{
				me.cx = Mathf.Clamp(me.cx, tileSize, mapW - tileSize);
				corrected = true;
			}
			bool hasGroundAtOrBelow = false;
			int startRowY = System.Math.Max(tileSize, TileMap.tileYofPixel(me.cy));
			for (int y = startRowY; y <= mapH - tileSize; y += tileSize)
			{
				if (TileMap.tileTypeAt(me.cx, y, 2))
				{
					hasGroundAtOrBelow = true;
					break;
				}
			}
			if (!hasGroundAtOrBelow)
			{
				int safeY = GetYGround(me.cx);
				if (safeY > 0 && safeY < mapH)
				{
					me.cy = safeY;
					me.cvy = 0;
					me.statusMe = 1;
					corrected = true;
				}
			}
			if (corrected)
			{
				me.cxSend = me.cx - 1;
				me.cySend = me.cy - 1;
				GameEvents.SyncTeleportPose(me.cx, me.cy);
				Service.gI().charMove();
			}
		}

		internal static void ChangeMap(Waypoint waypoint)
		{
			if (waypoint != null)
			{
				ResolveWaypointTarget(waypoint, out int targetX, out int targetY);
				TeleportMyChar(targetX, targetY);
				requestChangeMap(waypoint);
			}
		}

		internal static bool IsMeInNRDMap()
		{
			return TileMap.mapID >= 85 && TileMap.mapID <= 91;
		}

		/// <summary>
		///     Dịch chuyển đến đối tượng trong map
		/// </summary>
		/// <param name="obj">Đối tượng cần dịch chuyển tới</param>
		internal static void TeleportMyChar(IMapObject obj)
		{
			TeleportMyChar(obj.getX(), obj.getY());
		}

		/// <summary>
		///     Dịch chuyển đến vị trí trên mặt đất có hoành độ x
		/// </summary>
		/// <param name="x">Hoành độ</param>
		internal static void TeleportMyChar(int x)
		{
			TeleportMyChar(x, GetYGround(x));
		}

		internal static int getWidth(GUIStyle gUIStyle, string s)
		{
			_sharedGuiContent.text = s;
			return (int)(gUIStyle.CalcSize(_sharedGuiContent).x * 1.025f / mGraphics.zoomLevel);
		}

		internal static int getHeight(GUIStyle gUIStyle, string content)
		{
			_sharedGuiContent.text = content;
			return (int)gUIStyle.CalcSize(_sharedGuiContent).y / mGraphics.zoomLevel;
		}

		/// <summary>
		///     Lấy tung độ mặt đất từ hoành độ
		/// </summary>
		/// <param name="x">Hoành độ x</param>
		/// <returns>Tung độ y thỏa mãn (x, y) là mặt đất</returns>
		internal static int GetYGround(int x)
		{
			int tileSize = TileMap.size > 0 ? TileMap.size : 24;
			int mapW = TileMap.pxw > 0 ? TileMap.pxw : TileMap.tmw * tileSize;
			int mapH = TileMap.pxh > 0 ? TileMap.pxh : TileMap.tmh * tileSize;
			int prefY = Char.myCharz() != null && Char.myCharz().cy > 0 ? Char.myCharz().cy : 0;

			if (mapW > tileSize * 2)
				x = Mathf.Clamp(x, tileSize, mapW - tileSize);

			if (TryFindGroundAtColumn(x, 0, out int directY))
				return directY;

			for (int offset = tileSize; offset <= tileSize * 8; offset += tileSize)
			{
				if (x + offset < mapW && TryFindGroundAtColumn(x + offset, prefY, out int rightY))
					return rightY;
				if (x - offset >= tileSize && TryFindGroundAtColumn(x - offset, prefY, out int leftY))
					return leftY;
			}

			int fallback = prefY > 0 ? prefY : 336;
			if (mapH > tileSize * 2)
				return Mathf.Clamp(fallback, tileSize, mapH - tileSize);
			return System.Math.Max(tileSize, fallback);
		}

		internal static int Distance(IMapObject mapObject1, IMapObject mapObject2)
		{
			return Res.distance(mapObject1.getX(), mapObject1.getY(), mapObject2.getX(), mapObject2.getY());
		}

		internal static short getNRSDId()
		{
			if (IsMeInNRDMap()) return (short)(2400 - TileMap.mapID);
			return 0;
		}

		internal static bool isMeWearingActivationSet(int idSet)
		{
			Item[] body = Char.myCharz()?.arrItemBody;
			if (body == null || body.Length < 5) return false;
			int activateCount = 0;
			for (int i = 0; i < 5; i++)
			{
				Item item = body[i];
				if (item?.itemOption == null) return false;
				for (int j = 0; j < item.itemOption.Length; j++)
				{
					if (item.itemOption[j]?.optionTemplate?.id == idSet)
					{
						activateCount++;
						break;
					}
				}
			}
			return activateCount == 5;
		}

		internal static bool isMeWearingTXHSet()
		{
			return Char.myCharz().cgender == 0 && isMeWearingActivationSet(127);
		}

		internal static bool isMeWearingPikkoroDaimaoSet()
		{
			return Char.myCharz().cgender == 1 && isMeWearingActivationSet(132);
		}

		internal static bool isMeWearingCadicSet()
		{
			return Char.myCharz().cgender == 2 && isMeWearingActivationSet(134);
		}

		internal static void DoDoubleClickToObj(IMapObject mapObject)
		{
			GameScr.gI().doDoubleClickToObj(mapObject);
		}

		internal static bool CanNextMap()
		{
			if (Char.ischangingMap && !Char.isLoadingMap && _changingMapSinceMs > 0 && mSystem.currentTimeMillis() - _changingMapSinceMs > 2000L)
			{
				Char.ischangingMap = false;
				Char.isLockKey = false;
				_changingMapSinceMs = 0L;
			}
			return !Char.isLoadingMap && !Char.ischangingMap && !Controller.isStopReadMessage && GameCanvas.currentScreen is not TransportScr;
		}

		internal static bool HasStarOption(Item item, out uint star, out uint starE)
		{
			star = 0;
			starE = 0;
			bool result = false;
			if (item == null || item.template == null || item.itemOption == null)
				return result;
			if ((item.template.type < ItemTemplateType.Shirt || item.template.type > ItemTemplateType.Radar) && item.template.type != ItemTemplateType.TrainingSuite)
				return result;
			for (int i = 0; i < item.itemOption.Length; i++)
			{
				if (item.itemOption[i] == null || item.itemOption[i].optionTemplate == null)
					continue;
				if (item.itemOption[i].optionTemplate.id == 102)
					star = starE = (uint)item.itemOption[i].param;
				if (item.itemOption[i].optionTemplate.id == 107)
					starE = (uint)item.itemOption[i].param;
			}
			if (starE != 0)
				result = true;
			starE -= star;
			return result;
		}

		internal static long GetLastTimePress()
		{
			return GameCanvas.lastTimePress;
		}

		/// <summary>
		///     Lấy hệ của đệ tử bằng cách kiểm tra skill 1
		/// </summary>
		/// <returns></returns>
		internal static int GetPetGender()
		{
			return Char.myPetz()?.cgender ?? -1;
		}

		internal static string TrimUntilFit(string str, GUIStyle style, int width)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			(string, int, int) key = (str, width, mGraphics.zoomLevel);
			if (_trimCache.TryGetValue(key, out string cached))
				return cached;
			int originalWidth = (int)(getWidth(style, str) / 1.025f);
			string result = str;
			if (originalWidth > width)
			{
				int lo = 0;
				int hi = str.Length;
				while (lo < hi)
				{
					int mid = (lo + hi + 1) >> 1;
					if (getWidth(style, str.Substring(0, mid) + "...") <= width)
						lo = mid;
					else
						hi = mid - 1;
				}
				result = str.Substring(0, lo).Trim() + "...";
			}
			if (_trimCache.Count > 4096)
				_trimCache.Clear();
			_trimCache[key] = result;
			return result;
		}

		internal static bool HasActivateOption(Item item)
		{
			if (item == null || item.itemOption == null)
				return false;
			for (int i = 0; i < item.itemOption.Length; i++)
			{
				if (item.itemOption[i] == null || item.itemOption[i].optionTemplate == null)
					continue;
				if (item.itemOption[i].optionTemplate.id >= 127 && item.itemOption[i].optionTemplate.id <= 144)
					return true;
			}
			return false;
		}

		internal static Char FindCharInMap(string name)
		{
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char ch = (Char)GameScr.vCharInMap.elementAt(i);
				if (ch.GetNameWithoutClanTag() == name)
					return ch;
			}
			return null;
		}

		internal static bool IsMyCharHome()
		{
			return TileMap.mapID == Char.myCharz().cgender + 21;
		}

		internal static string FormatWithSIPrefix(double number)
		{
			if (number == 0)
				return "0";
			string[] prefix =
			{
				"", "k", "M", "B", "T"
			};
			int degree = Math.max(0, Math.min((int)System.Math.Floor(System.Math.Log10(System.Math.Abs(number)) / 3), prefix.Length - 1));
			double scaled = number * System.Math.Pow(1000, -degree);
			return $"{scaled:0.##}{prefix[degree]}";
		}

		internal static void ResetTextField(ChatTextField chatTextField)
		{
			if (chatTextField == null)
				return;
			chatTextField.left = new Command(mResources.OK, chatTextField, 8000, null, 1, GameCanvas.h - mScreen.cmdH + 1);
			chatTextField.right = new Command(mResources.DELETE, chatTextField, 8001, null, GameCanvas.w - 70, GameCanvas.h - mScreen.cmdH + 1);
			chatTextField.center = null;
			chatTextField.w = chatTextField.tfChat.width + 20;
			chatTextField.h = chatTextField.tfChat.height + 26;
			chatTextField.x = GameCanvas.w / 2 - chatTextField.w / 2;
			chatTextField.tfChat.y = GameCanvas.h - 40 - chatTextField.tfChat.height;
			chatTextField.y = chatTextField.tfChat.y - 18;
			if (Main.isPC && chatTextField.w > 320)
				chatTextField.w = 320;
			chatTextField.left.x = chatTextField.x;
			chatTextField.right.x = chatTextField.x + chatTextField.w - 68;
			if (GameCanvas.isTouch)
			{
				//tfChat.y -= 5;
				chatTextField.y -= 15;
				chatTextField.h += 30;
				chatTextField.left.x = GameCanvas.w / 2 - 68 - 5;
				chatTextField.right.x = GameCanvas.w / 2 + 5;
				chatTextField.left.y = GameCanvas.h - 30;
				chatTextField.right.y = GameCanvas.h - 30;
			}
			chatTextField.yBegin = chatTextField.tfChat.y;
			chatTextField.yUp = GameCanvas.h / 2 - 2 * chatTextField.tfChat.height;
			if (Main.isWindowsPhone)
				chatTextField.tfChat.showSubTextField = false;
			if (Main.isIPhone)
				chatTextField.tfChat.isPaintMouse = false;
			chatTextField.tfChat.name = "chat";
			if (Main.isWindowsPhone)
				chatTextField.tfChat.strInfo = chatTextField.tfChat.name;
			chatTextField.tfChat.width = GameCanvas.w - 6;
			if (Main.isPC && chatTextField.tfChat.width > 250)
				chatTextField.tfChat.width = 250;
			chatTextField.tfChat.height = mScreen.ITEM_HEIGHT + 2;
			chatTextField.tfChat.x = GameCanvas.w / 2 - chatTextField.tfChat.width / 2;
			chatTextField.tfChat.isFocus = true;
			chatTextField.tfChat.setMaxTextLenght(80);
		}

		internal static double Distance(double x1, double y1, double x2, double y2)
		{
			return System.Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
		}

		internal static string RemoveVietnameseDiacritics(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			string normalized = text.Normalize(NormalizationForm.FormD);
			StringBuilder result = new StringBuilder(normalized.Length);

			foreach (char c in normalized)
			{
				UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

				if (category == UnicodeCategory.NonSpacingMark)
					continue;

				switch (c)
				{
				case 'Đ':
					result.Append('D');
					break;

				case 'đ':
					result.Append('d');
					break;

				default:
					result.Append(c);
					break;
				}
			}

			return result.ToString().Normalize(NormalizationForm.FormC);
		}
	}
}
