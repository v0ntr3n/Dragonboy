using System.Collections;
using System.Collections.Generic;
using Mod.Auto;
using Mod.ModHelper;
using Mod.PickMob;
using Mod.Xmap;

namespace Mod.Yardat
{
	public class AutoYardat : CoroutineMainThreadAction<AutoYardat>
	{
		const long USE_TDLT_COOLDOWN_MS = 2000L;

		static readonly List<Char> ValidTargets = new List<Char>(16);
		static readonly MyVector EmptyMobVector = new MyVector();
		static readonly MyVector AttackCharVector = new MyVector();

		long lastPorataTime;
		long lastTimeChangeFocus;
		long _lastUseTdltMs;
		int targetIndex;

		protected override float Interval => 0.1f;

		protected override void OnStart()
		{
			YardatSettings.Load();
			AutoGoback.gI.Toggle(true);
			lastPorataTime = 0;
			lastTimeChangeFocus = 0;
			_lastUseTdltMs = 0;
			targetIndex = 0;
			GameScr.info1.addInfo("[Auto Yardat] BẮT ĐẦU", 0);
		}

		protected override void OnStop()
		{
			AutoGoback.gI.Toggle(false);

			if (!PickMobConfig.IsAutoPickItems)
			{
				PickMobConfig.IsAutoPickItems = true;
			}
			GameScr.info1.addInfo("[Auto Yardat] ĐÃ DỪNG", 0);
		}

		protected override IEnumerator OnUpdate()
		{

			if (Char.myCharz().IsCharDead() || XmapController.gI.IsActing)
			{
				yield break;
			}

			if (TileMap.mapID == Char.myCharz().cgender + 21 && GameScr.vItemMap.size() > 0)
			{
				Service.gI().pickItem(((ItemMap)GameScr.vItemMap.elementAt(0)).itemMapID);
				yield break;
			}

			UpdateAutoPorata();
			UpdatePetGoHome();
			UpdateAutoChangeFocus();
			yield return null;
		}

		void UpdateAutoPorata()
		{
			if (!YardatSettings.AutoPorata)
			{
				return;
			}

			if (Char.myCharz().IsCharDead())
			{
				return;
			}

			sbyte index = Utils.getIndexItemBag(921, 454);
			if (index == -1)
			{
				GameScr.info1.addInfo("[Yardat] Không có Bông tai Porata!", 0);
				YardatSettings.AutoPorata = false;
				YardatSettings.Save();
				return;
			}

			long now = mSystem.currentTimeMillis();
			int delay = Char.myCharz().isNhapThe ? 11000 : 2000;
			if (now - lastPorataTime >= delay)
			{
				Service.gI().useItem(0, 1, index, -1);
				lastPorataTime = now;
			}
		}

		static void UpdatePetGoHome()
		{
			if (!YardatSettings.PetGoHome || !Char.myCharz().havePet)
			{
				return;
			}

			if (Char.myPetz() != null && Char.myPetz().petStatus != 3 && IsPetInMap())
			{
				Service.gI().petStatus(3);
				Char.myPetz().petStatus = 3;
			}
		}

		static bool IsPetInMap()
		{
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char c = (Char)GameScr.vCharInMap.elementAt(i);
				if (c != null && !string.IsNullOrEmpty(c.cName) && c.charID == Char.myCharz().charID * -1)
				{
					return true;
				}
			}
			return false;
		}

		static long GetCooldownAutoChangeFocus(Skill skill)
		{
			if (skill == null || skill.coolDown <= 500)
			{
				return 1000L;
			}
			return (long)(skill.coolDown * 1.2 + 200.0);
		}

		void UpdateAutoChangeFocus()
		{
			if (!YardatSettings.AutoChangeFocus || Char.myCharz().isCharge)
			{
				return;
			}

			if (Char.myCharz().IsCharDead() || Char.myCharz().statusMe == 14 || Char.myCharz().statusMe == 5)
			{
				return;
			}

			if (!Utils.isUsingTDLT())
			{
				long tdltNow = mSystem.currentTimeMillis();
				if (tdltNow - _lastUseTdltMs >= USE_TDLT_COOLDOWN_MS)
				{
					_lastUseTdltMs = tdltNow;
					Utils.useItem(521);
				}
			}

			if (PickMobConfig.IsAutoPickItems)
			{
				PickMobConfig.IsAutoPickItems = false;
			}

			Skill skill = SkillPicker.GetSkillAttack();

			GameScr.gI().doSelectSkill(skill, true);

			if (skill == null)
			{
				return;
			}

			long cooldown = GetCooldownAutoChangeFocus(skill);
			long now = mSystem.currentTimeMillis();
			if (now - lastTimeChangeFocus < cooldown)
			{
				return;
			}

			List<Char> targets = GetValidTargets();
			if (targets.Count == 0)
			{
				return;
			}

			lastTimeChangeFocus = now;

			if (targetIndex >= targets.Count)
			{
				targetIndex = 0;
			}
			Char target = targets[targetIndex];
			targetIndex = (targetIndex + 1) % targets.Count;

			if (target != null)
			{
				Char.myCharz().charFocus = target;

				int dist = System.Math.Abs(target.cx - Char.myCharz().cx);
				int maxRange = (int)(skill.dx * 1.5f);
				if (dist <= maxRange)
				{
					skill.lastTimeUseThisSkill = now;
					EmptyMobVector.removeAllElements();
					AttackCharVector.removeAllElements();
					AttackCharVector.addElement(target);
					Service.gI().sendPlayerAttack(EmptyMobVector, AttackCharVector, 2);
				}
				else if (YardatSettings.AutoMoveToTarget)
				{
					Utils.TeleportMyChar(target.cx, target.cy);
				}
			}
		}

		static List<Char> GetValidTargets()
		{
			ValidTargets.Clear();
			if (GameScr.vCharInMap == null)
			{
				return ValidTargets;
			}

			int myCharId = Char.myCharz().charID;
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char c = (Char)GameScr.vCharInMap.elementAt(i);
				if (c == null || c.charID == myCharId || c.isPet || c.isMiniPet || c.statusMe == 14 || c.statusMe == 5 || string.IsNullOrEmpty(c.cName))
				{
					continue;
				}

				if (YardatSettings.AutoSelectAllYardat)
				{
					if (c.charID <= 0)
					{
						ValidTargets.Add(c);
					}
				}
				else if (YardatSettings.CustomTargetIds.Contains(c.charID))
				{
					ValidTargets.Add(c);
				}
			}
			return ValidTargets;
		}
	}
}
