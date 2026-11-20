using System.Collections;
using Mod.ModHelper;
using Mod.PickMob;
using Mod.Xmap;

namespace Mod.Auto
{
	public class AutoKillAll : CoroutineMainThreadAction<AutoKillAll>
	{
		static readonly MyVector EmptyMobVector = new MyVector();
		static readonly MyVector AttackCharVector = new MyVector();

		Char currentTarget;
		bool _prevAutoPickItems;
		bool _hasSavedAutoPickItems;

		protected override float Interval => 0.2f;

		static bool IsEnemyCandidate(Char target)
		{
			return target != null
			       && !string.IsNullOrEmpty(target.cName)
			       && !char.IsUpper(target.cName[0])
			       && !target.isPet
			       && !target.isMiniPet
			       && !target.cName.StartsWith("#")
			       && !target.cName.StartsWith("$")
			       && target.cName != "Trọng tài"
			       && target.cFlag != 0
			       && target.cHP > 0;
		}

		static bool IsTargetValidInMap(Char target)
		{
			return IsEnemyCandidate(target)
			       && !target.meDead
			       && target.charID > 0
			       && target.cx >= 0
			       && target.cy >= 0
			       && target.cx <= TileMap.pxw
			       && target.cy <= TileMap.pxh;
		}

		static bool IsTargetValid(Char target)
		{
			return IsTargetValidInMap(target)
			       && GameScr.findCharInMap(target.charID) == target;
		}

		static void ClearFocus()
		{
			Char.myCharz().mobFocus = null;
			Char.myCharz().npcFocus = null;
			Char.myCharz().itemFocus = null;
		}

		static void AttackTarget(Char target)
		{
			ClearFocus();
			Char.myCharz().charFocus = target;

			Skill skill = SkillPicker.GetSkillAttack();

			if (skill == null || skill.paintCanNotUseSkill)
			{
				return;
			}

			GameScr.gI().doSelectSkill(skill, true);

			bool inRange = Utils.Distance(Char.myCharz(), target) <= 50 || System.Math.Abs(Char.myCharz().cx - target.cx) <= 70;

			if (!inRange)
			{
				Utils.TeleportMyChar(target.cx - 30, Utils.GetYGround(target.cx));
				return;
			}

			skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
			EmptyMobVector.removeAllElements();
			AttackCharVector.removeAllElements();
			AttackCharVector.addElement(target);
			Service.gI().sendPlayerAttack(EmptyMobVector, AttackCharVector, 2);
		}

		protected override IEnumerator OnUpdate()
		{
			if (XmapController.gI.IsActing)
			{
				yield break;
			}

			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
				yield return null;
			}

			if (Char.myCharz().meDead || Char.myCharz().stone || Char.myCharz().isCharge)
			{
				yield break;
			}

			if (currentTarget != null)
			{
				if (!IsTargetValid(currentTarget))
				{
					currentTarget = null;
				}
				else
				{
					AttackTarget(currentTarget);
					yield break;
				}
			}

			if (GameScr.vCharInMap == null)
			{
				yield break;
			}

			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char obj = (Char)GameScr.vCharInMap.elementAt(i);
				if (IsTargetValidInMap(obj))
				{
					currentTarget = obj;
					AttackTarget(obj);
					break;
				}
			}
		}

		protected override void OnStart()
		{
			_prevAutoPickItems = PickMobConfig.IsAutoPickItems;
			_hasSavedAutoPickItems = true;
			PickMobConfig.IsAutoPickItems = false;
			currentTarget = null;
			base.OnStart();
		}

		protected override void OnStop()
		{
			if (_hasSavedAutoPickItems)
			{
				PickMobConfig.IsAutoPickItems = _prevAutoPickItems;
				_hasSavedAutoPickItems = false;
			}
			currentTarget = null;
			base.OnStop();
		}
	}
}
