using System.Collections;
using Mod.ModHelper;
using Mod.PickMob;
using Mod.Xmap;
using UnityEngine;

namespace Mod.Auto
{
	public class AutoKillSelfAndPickGold : CoroutineMainThreadAction<AutoKillSelfAndPickGold>
	{
		static readonly int[] SelfAttackSkillIds = { 0, 2, 4, 17 };
		static readonly MyVector EmptyMobVector = new MyVector();
		static readonly MyVector SelfTargetVector = new MyVector();

		int lastItemId = -1;
		float pickStartTime = -1f;
		bool _prevAutoPickItems;
		bool _hasSavedAutoPickItems;

		protected override float Interval => 0.5f;

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().IsCharDead() || TileMap.mapID != XmapContext.MapLookup.GetVillageMapId(Char.myCharz().cgender) || AutoGoback.IsGoingBack)
			{
				yield break;
			}

			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
				yield break;
			}

			int itemMapId = -1;

			for (int i = 0; i < GameScr.vItemMap.size(); i++)
			{
				ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (itemMap.template.id == 190 && itemMap.playerId == Char.myCharz().charID)
				{
					itemMapId = itemMap.itemMapID;
					break;
				}
			}

			if (itemMapId != -1)
			{
				if (itemMapId != lastItemId)
				{
					lastItemId = itemMapId;
					pickStartTime = Time.realtimeSinceStartup;
				}

				if (Time.realtimeSinceStartup - pickStartTime < 5f)
				{
					Service.gI().pickItem(itemMapId);
				}
				else
				{
					AttackSelf();
				}
			}
			else
			{
				AttackSelf();
			}
		}

		static bool IsSelfAttackSkill(int skillId)
		{
			for (int i = 0; i < SelfAttackSkillIds.Length; i++)
			{
				if (SelfAttackSkillIds[i] == skillId)
				{
					return true;
				}
			}

			return false;
		}

		static void AttackSelf()
		{
			Skill selectedSkill = null;
			Skill[] keySkill = GameScr.keySkill;
			if (keySkill != null)
			{
				for (int i = 0; i < keySkill.Length; i++)
				{
					Skill s = keySkill[i];
					if (s != null && IsSelfAttackSkill(s.template.id))
					{
						if (s.template.id == 17)
						{
							selectedSkill = s;
							break;
						}
						if (selectedSkill == null)
						{
							selectedSkill = s;
						}
					}
				}
			}

			if (selectedSkill != null && selectedSkill != Char.myCharz().myskill)
			{
				GameScr.gI().doSelectSkill(selectedSkill, true);
			}

			EmptyMobVector.removeAllElements();
			SelfTargetVector.removeAllElements();
			SelfTargetVector.addElement(Char.myCharz());
			Service.gI().sendPlayerAttack(EmptyMobVector, SelfTargetVector, -1);
		}

		protected override void OnStart()
		{
			if (TileMap.mapID != XmapContext.MapLookup.GetVillageMapId(Char.myCharz().cgender))
			{
				GameScr.info1.addInfo("Chỉ hoạt động ở map Làng", 0);
				Toggle(false);
				return;
			}
			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
			}
			AutoGoback.gI.Toggle(true);
			_prevAutoPickItems = PickMobConfig.IsAutoPickItems;
			_hasSavedAutoPickItems = true;
			PickMobConfig.IsAutoPickItems = false;
			base.OnStart();
		}

		protected override void OnStop()
		{
			AutoGoback.gI.Toggle(false);
			if (_hasSavedAutoPickItems)
			{
				PickMobConfig.IsAutoPickItems = _prevAutoPickItems;
				_hasSavedAutoPickItems = false;
			}
			GameScr.info1.addInfo("Đã tắt AutoKillSelfAndPickGold", 0);
			base.OnStop();
		}
	}
}
