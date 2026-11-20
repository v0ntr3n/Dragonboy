using JetBrains.Annotations;

namespace Mod.PickMob
{
	public static class SkillPicker
	{
		static readonly sbyte[] IdSkillsMelee =
		{
			9, 2, 17, 4, 0
		};
		static readonly sbyte[] IdSkillsCanNotAttack =
		{
			10, 11, 14, 23, 7
		};
		static readonly SkillTemplate LookupTemplate = new SkillTemplate();

		[CanBeNull]
		public static Skill GetSkillAttack()
		{
			Skill best = null;
			Char myChar = Char.myCharz();
			for (int i = 0; i < PickMobConfig.IdSkillsTanSat.Count; i++)
			{
				LookupTemplate.id = PickMobConfig.IdSkillsTanSat[i];
				Skill candidate = myChar.getSkill(LookupTemplate);
				if (IsSkillBetter(candidate, best))
				{
					best = candidate;
				}
			}

			return best;
		}

		static bool IsSkillBetter(Skill candidate, Skill current)
		{
			if (candidate == null || !CanUseSkill(candidate))
			{
				return false;
			}

			if (current == null)
			{
				return true;
			}

			bool isPrioritize = candidate.template.id == 17 && current.template.id == 2 ||
			                    candidate.template.id == 9 && current.template.id == 0;

			return current.coolDown < candidate.coolDown || isPrioritize;
		}

		static bool CanUseSkill(Skill skill)
		{
			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown)
			{
				skill.paintCanNotUseSkill = false;
			}

			if (skill.paintCanNotUseSkill && !ContainsSkillId(IdSkillsMelee, skill.template.id))
			{
				return false;
			}

			if (ContainsSkillId(IdSkillsCanNotAttack, skill.template.id))
			{
				return false;
			}

			Char myChar = Char.myCharz();
			if (skill.template.id == 9 && (myChar.cHP < myChar.cHPFull / 3 || myChar.cMP < myChar.cMPFull / 3))
			{
				return false;
			}

			if (myChar.cMP < GetManaUseSkill(skill))
			{
				return false;
			}

			return true;
		}

		static bool ContainsSkillId(sbyte[] skillIds, sbyte id)
		{
			for (int i = 0; i < skillIds.Length; i++)
			{
				if (skillIds[i] == id)
				{
					return true;
				}
			}

			return false;
		}

		static long GetManaUseSkill(Skill skill)
		{
			if (skill.template.manaUseType == 2)
				return 1L;
			if (skill.template.manaUseType == 1)
				return (long)skill.manaUse * Char.myCharz().cMPFull / 100L;
			return skill.manaUse;
		}
	}
}
