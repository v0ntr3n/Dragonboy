using System.Collections.Generic;
using Mod.Constants;
using Mod.PickMob;
using Mod.Xmap;
using UnityEngine;

namespace Mod.Auto
{
	internal enum AutoTrainPetMode
	{
		Disabled,
		Normal,
		AvoidSuperMob,
		Kaioken
	}

	internal enum AutoTrainPetAttackMode
	{
		AttackClosestMob,
		AttackMyPet,
		AttackMyself
	}

	internal static class AutoTrainPet
	{
		static long lastTimePick;
		static int lastX;
		static bool isAssignedLastX;
		static int lastXPet;
		static bool isAssignedLastXPet;
		static bool isMyPetDied;

		static bool isPicking;
		static long lastTimePetFollow = mSystem.currentTimeMillis();
		static bool isGoHomeGetMorePean;
		static bool isMagicTreeUpgrading;
		static bool isMagicTreeOutOfPean;
		static long lastTimeCheckMagicTree;
		static KeyValuePair<int, int> mapZoneGoBack;
		static long delayCheckPet;

		internal static bool isFirstTimeCheckPet = true;
		static bool isTTNL;

		internal static bool saoMayLuoiThe;
		internal static AutoTrainPetMode Mode { get; set; }
		internal static bool isEnabled => Mode != AutoTrainPetMode.Disabled;
		internal static AutoTrainPetAttackMode ModeAttackWhenNeeded { get; set; }

		internal static void SetState(int value)
		{
			Mode = (AutoTrainPetMode)value;
		}

		internal static void SetAttackState(int value)
		{
			ModeAttackWhenNeeded = (AutoTrainPetAttackMode)value;
		}

		internal static void Update()
		{
			if (Mode == AutoTrainPetMode.Disabled || Char.myCharz() == null || !Char.myCharz().havePet)
				return;
			if (isFirstTimeCheckPet)
			{
				delayCheckPet = mSystem.currentTimeMillis();
				isFirstTimeCheckPet = false;
			}

			if (mSystem.currentTimeMillis() - delayCheckPet < 3000)
				return;
			int tick30 = System.Math.Max(1, Mathf.RoundToInt(30f * Time.timeScale));
			int tick60 = System.Math.Max(2, Mathf.RoundToInt(60f * Time.timeScale));
			int tick120 = System.Math.Max(4, Mathf.RoundToInt(120f * Time.timeScale));
			if (GameCanvas.gameTick % tick30 != 0 || XmapController.gI.IsActing)
				return;
			// AutoPick();
			if (Char.myPetz().cStamina < 5 && GameScr.hpPotion > 0 && !(Char.myPetz().cHP <= 0 || Char.myPetz().isDie))
				GameScr.gI().doUseHP();
			if (isMagicTreeOutOfPean && mSystem.currentTimeMillis() - lastTimeCheckMagicTree >= 600000)
			{
				isMagicTreeOutOfPean = false;
				lastTimeCheckMagicTree = mSystem.currentTimeMillis();
			}

			if (!isMagicTreeUpgrading)
				if (GameScr.hpPotion == 0 && !isGoHomeGetMorePean && !isMagicTreeOutOfPean)
				{
					isGoHomeGetMorePean = true;
					lastX = Char.myCharz().cx;
					mapZoneGoBack = new KeyValuePair<int, int>(TileMap.mapID, TileMap.zoneID);
				}

			if (isGoHomeGetMorePean && !isMagicTreeOutOfPean && GameCanvas.gameTick % tick60 == 0)
			{
				if (TileMap.mapID != Char.myCharz().cgender + 21)
				{
					if (!XmapController.gI.IsActing && GameScr.hpPotion == 0)
						XmapController.start(Char.myCharz().cgender + 21);
				}
				else
				{
					if (GameCanvas.menu.showMenu)
						GameCanvas.menu.doCloseMenu();
					MagicTree magicTree = GameScr.gI().magicTree;
					if (magicTree == null)
						return;
					if (magicTree.currPeas > 0)
					{
						Service.gI().openMenu(4);
						Service.gI().confirmMenu(4, 0);
						return;
					}

					isMagicTreeOutOfPean = true;
					if (magicTree.isUpdateTree)
					{
						lastTimeCheckMagicTree = mSystem.currentTimeMillis();
						isMagicTreeUpgrading = true;
					}

					if ((magicTree.isPeasEffect || magicTree.isUpdateTree ||
					     magicTree.currPeas == 0) && !XmapController.gI.IsActing)
						XmapController.start(mapZoneGoBack.Key);
				}

				if (TileMap.mapID == mapZoneGoBack.Key && !XmapController.gI.IsActing)
				{
					if (TileMap.zoneID == mapZoneGoBack.Value)
					{
						if (lastX == 0 || Res.abs(Char.myCharz().cx - lastX) <= 24)
							isGoHomeGetMorePean = false;
					}
					else if (GameCanvas.gameTick % tick120 == 0)
					{
						Service.gI().requestChangeZone(mapZoneGoBack.Value, 0);
					}
				}
			}

			//TeleToSafePos();
			AutoSkill();
			if (isMyPetDied)
				return;
			if (isAssignedLastXPet)
			{
				isAssignedLastXPet = false;
				Utils.TeleportMyChar(lastXPet);
			}

			if (mSystem.currentTimeMillis() - lastTimePetFollow > 600000 ||
			    GameScr.findCharInMap(-Char.myCharz().charID) != null &&
			    Utils.Distance(Char.myCharz(), GameScr.findCharInMap(-Char.myCharz().charID)) > 400)
			{
				lastTimePetFollow = mSystem.currentTimeMillis();
				Service.gI().petStatus(0);
				Utils.TeleportMyChar(Char.myCharz().cx);
				return;
			}

			if (Mode == AutoTrainPetMode.Normal)
			{
				//up đệ thường
				if (Char.myPetz().petStatus != 2)
					Service.gI().petStatus(2); //tấn công
			}
			else
			{
				if (isTTNL)
					return;
				if (Mode == AutoTrainPetMode.AvoidSuperMob)
				{
					//up đệ né siêu quái
					if (Char.myPetz().petStatus != 1)
						Service.gI().petStatus(1); //bảo vệ
					Mob closestMob = ClosestMob();
					if (closestMob != null && closestMob.x > 50 && closestMob.y > 50)
						Char.myCharz().currentMovePoint = new MovePoint(closestMob.x + Res.random(-5, 5), closestMob.y);
				}
				else if (Mode == AutoTrainPetMode.Kaioken)
				{
					//up đệ kaioken
					if (Char.myPetz().petStatus != 2)
						Service.gI().petStatus(2); //tấn công
					if (GameCanvas.gameTick % tick60 == 0)
					{
						Char.myCharz().cy--;
						Service.gI().charMove();
					}

					if (GameCanvas.gameTick % tick60 == tick30)
					{
						Char.myCharz().cy++;
						Service.gI().charMove();
					}
				}
			}
		}

		static void TeleToSafePos()
		{
			if (!isPicking &&
			    (Char.myCharz().arrItemBody[5] == null || Char.myCharz().arrItemBody[5].template.id != 449))
			{
				Char myPet = GameScr.findCharInMap(-Char.myCharz().charID);
				if (myPet == null)
					myPet = Char.myPetz();
				if (myPet != null)
				{
					if (myPet.cHP <= 0 || myPet.isDie)
					{
						if (!isMyPetDied)
						{
							isMyPetDied = true;
							if (!isAssignedLastXPet)
							{
								isAssignedLastXPet = true;
								lastXPet = Char.myCharz().cx;
							}

							Utils.TeleportMyChar(50);
						}
					}
					else if (isMyPetDied)
					{
						isMyPetDied = false;
					}
				}
				else if (isMyPetDied)
				{
					isMyPetDied = false;
				}
			}
		}

		static void AutoSkill()
		{
			Char myChar = Char.myCharz();
			SkillTemplate[] skillTemplates = myChar?.nClass?.skillTemplates;
			if (skillTemplates == null || skillTemplates.Length <= 2)
				return;
			Skill skill3 = myChar.getSkill(skillTemplates[2]);
			Skill skill1 = myChar.getSkill(skillTemplates[0]);
			Char myPet = Char.myPetz();
			if (skill3 != null)
			{
				if (skill3.point > 0 && mSystem.currentTimeMillis() - skill3.lastTimeUseThisSkill > skill3.coolDown)
				{
					bool petLowHp = myPet != null && myPet.cHPFull > 0 && myPet.cHP * 100 / myPet.cHPFull < 10;
					bool petLowMp = myPet != null && myPet.cMPFull > 0 && myPet.cMP * 100 / myPet.cMPFull < 10;
					bool myLowHp = myChar.cHPFull > 0 && myChar.cHP * 100 / myChar.cHPFull < 10;
					bool myLowMp = myChar.cMPFull > 0 && myChar.cMP * 100 / myChar.cMPFull < 10;
					if ((petLowHp || petLowMp || myLowHp || myLowMp) && myChar.cgender == 1 &&
					    skill3.manaUse < myChar.cMP)
					{
						if (skill3.point > 1)
						{
							Utils.buffMe();
							skill3.lastTimeUseThisSkill = mSystem.currentTimeMillis();
						}
						else
						{
							Char petTarget = GameScr.findCharInMap(-myChar.charID);
							if (petTarget != null)
							{
								MyVector vecPet = new MyVector();
								vecPet.addElement(petTarget);
								Service.gI().selectSkill(skill3.template.id);
								Service.gI().sendPlayerAttack(new MyVector(), vecPet, 2);
								if (skill1 != null && skill1.template != null)
									Service.gI().selectSkill(skill1.template.id);
								skill3.lastTimeUseThisSkill = mSystem.currentTimeMillis();
							}
						}

						return;
					}

					if ((myLowHp || myLowMp) && myChar.cgender == 2)
					{
						GameScr.gI().doUseSkillNotFocus(skill3);
						isTTNL = true;
						skill3.lastTimeUseThisSkill = mSystem.currentTimeMillis();
						return;
					}
				}

				if (myChar.cgender == 2 && isTTNL &&
				    mSystem.currentTimeMillis() - skill3.lastTimeUseThisSkill > 10000)
				{
					if (skill1 != null)
						myChar.myskill = skill1;
					isTTNL = false;
				}
			}

			//đánh khi đệ cần
			if (saoMayLuoiThe)
			{
				saoMayLuoiThe = false;
				if (skill1 == null || skill1.template == null)
					return;
				Service.gI().selectSkill(skill1.template.id);
				switch (ModeAttackWhenNeeded)
				{
				case AutoTrainPetAttackMode.AttackClosestMob:
					Mob closestMob = ClosestMob();
					if (closestMob != null)
					{
						MyVector vecMob = new MyVector();
						vecMob.addElement(closestMob);
						Service.gI().sendPlayerAttack(vecMob, new MyVector(), 1);
					}
					break;
				case AutoTrainPetAttackMode.AttackMyPet:
					Char petInMap = GameScr.findCharInMap(-myChar.charID);
					if (petInMap != null)
					{
						if (myChar.cFlag != 8)
							Service.gI().getFlag(1, 8);
						MyVector vecPet = new MyVector();
						vecPet.addElement(petInMap);
						Service.gI().sendPlayerAttack(new MyVector(), vecPet, 2);
					}
					break;
				case AutoTrainPetAttackMode.AttackMyself:
					if (myChar.cFlag != 8)
						Service.gI().getFlag(1, 8);
					Service.gI().sendPlayerAttack(new MyVector(), Utils.getMyVectorMe(), 2);
					break;
				}
			}
		}

		static Mob ClosestMob()
		{
			Mob result = null;
			int minDistance = int.MaxValue;
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (mob.status != 0 && mob.status != 1 && mob.hp > 0 && !mob.isMobMe && mob.levelBoss == 0 && mob.getTemplate().type != MonsterType.Fly)
				{
					int distance = Res.distance(mob.x, mob.y, Char.myCharz().cx, Char.myCharz().cy);
					if (minDistance > distance)
					{
						minDistance = distance;
						result = mob;
					}
				}
			}

			return result;
		}

		static void AutoPick()
		{
			if (!PickMobConfig.IsAutoPickItems) return;
			if (mSystem.currentTimeMillis() - lastTimePick > 550)
			{
				bool hasPickableItem = false;
				if (GameScr.vItemMap.size() == 0)
					isPicking = false;
				for (int i = GameScr.vItemMap.size() - 1; i >= 0; i--)
				{
					ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
					if (itemMap.template.id >= 828 && itemMap.template.id <= 842 || itemMap.template.id == 859 ||
					    itemMap.template.id == 362 || itemMap.template.id >= 353 && itemMap.template.id <= 360)
					{
						GameScr.vItemMap.removeElementAt(i);
						continue;
					}

					if (itemMap.template.id == 74)
					{
						Service.gI().pickItem(itemMap.itemMapID);
						lastTimePick = mSystem.currentTimeMillis();
						return;
					}

					int distance = Utils.Distance(itemMap, Char.myCharz());
					if (itemMap.playerId == Char.myCharz().charID || itemMap.playerId == -1)
					{
						hasPickableItem = true;
						if (!isAssignedLastX)
						{
							isAssignedLastX = true;
							lastX = Char.myCharz().cx;
						}

						if (distance > 60)
							Utils.TeleportMyChar(itemMap.x);
						else
							Char.myCharz().currentMovePoint = new MovePoint(itemMap.x, itemMap.y);
						Service.gI().pickItem(itemMap.itemMapID);
						isPicking = true;
						lastTimePick = mSystem.currentTimeMillis();
						break;
					}
				}

				if (isAssignedLastX && !hasPickableItem)
				{
					if (lastX <= 50 && lastXPet > 50)
						lastX = lastXPet;
					isPicking = false;
					if (Res.distance(Char.myCharz().cx, Char.myCharz().cy, lastX, Utils.GetYGround(lastX)) > 60)
						Utils.TeleportMyChar(lastX);
					else
						Char.myCharz().currentMovePoint = new MovePoint(lastX, Utils.GetYGround(lastX));
					isAssignedLastX = false;
				}
			}
		}
	}
}
