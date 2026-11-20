using System.Collections.Generic;
using Mod.CustomPanel;
using Mod.ModMenu;
using Mod.R;
using UnityEngine;

namespace Mod.PickMob
{
	internal static class PickMobMenu
	{
		static ModMenuItemBoolean[] modMenuItemBools;
		static ModMenuItemFunction[] modMenuItemFunctions;
		static readonly GUIStyle style = new GUIStyle
		{
			font = Resources.Load<Font>($"FontSys/x{mGraphics.zoomLevel}/chelthm")
		};

		internal static void ShowMenu()
		{
			BuildMenuItems();
			CustomPanelMenu.Show(new CustomPanelMenuConfig
			{
				SetTabAction = SetTab,
				DoFireItemAction = DoFire,
				PaintTabHeaderAction = PaintTabHeader,
				PaintAction = Paint
			});
		}

		static void BuildMenuItems()
		{
			Mob mobFocus = Char.myCharz().mobFocus;
			ItemMap itemFocus = Char.myCharz().itemFocus;
			SkillTemplate mySkillTemplate = Char.myCharz().myskill.template;

			List<ModMenuItemBoolean> bools = new List<ModMenuItemBoolean>();
			List<ModMenuItemFunction> functions = new List<ModMenuItemFunction>();

			// --- Context-aware toggles (only when focused on mob/item) ---
			if (mobFocus != null || itemFocus != null)
			{
				// Toggle mob ID / item ID in pick list
				string toggleIdTitle;
				string toggleIdDesc;
				if (mobFocus != null)
				{
					bool isInList = PickMobConfig.IdMobsTanSat.Contains(mobFocus.mobId);
					toggleIdTitle = isInList
						? string.Format(Strings.pickMobRemoveMobIdFromList, mobFocus.mobId)
						: string.Format(Strings.pickMobAddMobIdToList, mobFocus.mobId);
					toggleIdDesc = isInList
						? $"Remove mob ID {mobFocus.mobId} from slaughter list"
						: $"Add mob ID {mobFocus.mobId} to slaughter list";
				}
				else
				{
					bool isInList = PickMobConfig.IdItemPicks.Contains(itemFocus.template.id);
					toggleIdTitle = isInList
						? string.Format(Strings.pickMobRemoveFromList, itemFocus.template.name)
						: string.Format(Strings.pickMobAddToList, itemFocus.template.name);
					toggleIdDesc = isInList
						? $"Remove \"{itemFocus.template.name}\" from auto pick list"
						: $"Add \"{itemFocus.template.name}\" to auto pick list";
				}
				bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "PickMob_ToggleFocusedId",
					Title = toggleIdTitle,
					Description = toggleIdDesc,
					GetValueFunc = () => false,
					SetValueAction = _ => PickMobCommands.ToggleSelectedMob()
				}));

				// Toggle mob type / item type in pick list
				string toggleTypeTitle;
				string toggleTypeDesc;
				if (mobFocus != null)
				{
					bool isInList = PickMobConfig.TypeMobsTanSat.Contains(mobFocus.getTemplate().mobTemplateId);
					toggleTypeTitle = isInList
						? string.Format(Strings.pickMobRemoveFromList, mobFocus.getTemplate().name)
						: string.Format(Strings.pickMobAddToList, mobFocus.getTemplate().name);
					toggleTypeDesc = isInList
						? $"Remove \"{mobFocus.getTemplate().name}\" type from slaughter list"
						: $"Add \"{mobFocus.getTemplate().name}\" type to slaughter list";
				}
				else
				{
					bool isInList = PickMobConfig.TypeItemPicks.Contains(itemFocus.template.type);
					toggleTypeTitle = isInList
						? string.Format(Strings.pickMobRemoveItemTypeFromList, itemFocus.template.type)
						: string.Format(Strings.pickMobAddItemTypeToList, itemFocus.template.type);
					toggleTypeDesc = isInList
						? $"Remove item type {itemFocus.template.type} from auto pick list"
						: $"Add item type {itemFocus.template.type} to auto pick list";
				}
				bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
				{
					ID = "PickMob_ToggleFocusedType",
					Title = toggleTypeTitle,
					Description = toggleTypeDesc,
					GetValueFunc = () => false,
					SetValueAction = _ => PickMobCommands.ToggleItemOrMobType()
				}));

				// Item-only: block item ID
				if (itemFocus != null)
				{
					bool isBlocked = PickMobConfig.IdItemBlocks.Contains(itemFocus.template.id);
					bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
					{
						ID = "PickMob_BlockItemId",
						Title = isBlocked
							? string.Format(Strings.pickMobRemoveFromDontPickList, itemFocus.template.name)
							: string.Format(Strings.pickMobAddToDontPickList, itemFocus.template.name),
						Description = isBlocked
							? $"Allow picking \"{itemFocus.template.name}\""
							: $"Block picking \"{itemFocus.template.name}\"",
						GetValueFunc = () => false,
						SetValueAction = _ => PickMobCommands.BlockItem(itemFocus.template.id)
					}));

					// Item-only: block item type
					bool isTypeBlocked = PickMobConfig.TypeItemBlocks.Contains(itemFocus.template.type);
					bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
					{
						ID = "PickMob_BlockItemType",
						Title = isTypeBlocked
							? string.Format(Strings.pickMobRemoveItemTypeFromDontPickList, itemFocus.template.type)
							: string.Format(Strings.pickMobAddItemTypeToDontPickList, itemFocus.template.type),
						Description = isTypeBlocked
							? $"Allow picking item type {itemFocus.template.type}"
							: $"Block picking item type {itemFocus.template.type}",
						GetValueFunc = () => false,
						SetValueAction = _ => PickMobCommands.BlockItemType(itemFocus.template.type)
					}));
				}
			}

			// --- Clear monster list (only when list is not empty) ---
			if (PickMobConfig.IdMobsTanSat.Count + PickMobConfig.TypeMobsTanSat.Count > 0)
			{
				functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "PickMob_ClearMonsterList",
					Title = Strings.pickMobClearMonsterList,
					Description = "Clear all monsters from the slaughter list",
					Action = PickMobCommands.ClearMonsterToFightList
				}));
			}

			// --- Toggle current skill for slaughter ---
			bool skillInList = PickMobConfig.IdSkillsTanSat.Contains(mySkillTemplate.id);
			bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
			{
				ID = "PickMob_ToggleSkill",
				Title = skillInList
					? string.Format(Strings.pickMobRemoveFromSkillList, mySkillTemplate.name)
					: string.Format(Strings.pickMobAddToSkillList, mySkillTemplate.name),
				Description = skillInList
					? $"Remove \"{mySkillTemplate.name}\" from slaughter skill list"
					: $"Add \"{mySkillTemplate.name}\" to slaughter skill list",
				GetValueFunc = () => false,
				SetValueAction = _ => PickMobCommands.ToggleSelectedSkillForSlaughter()
			}));

			// --- Reset skill list to default ---
			functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
			{
				ID = "PickMob_ResetSkillList",
				Title = Strings.pickMobResetSkillListToDefault,
				Description = "Reset the slaughter skill list to default values",
				Action = PickMobCommands.SaveCurrentSkillListAsDefault
			}));

			// --- Reset item filter to default ---
			functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
			{
				ID = "PickMob_ResetItemList",
				Title = Strings.pickMobResetItemListToDefault,
				Description = "Reset the item pick/block filter to default values",
				Action = PickMobCommands.ResetItemFilterToDefault
			}));

			// --- View lists ---
			if (PickMobConfig.IdMobsTanSat.Count + PickMobConfig.TypeMobsTanSat.Count > 0)
			{
				functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "PickMob_ViewMonsterList",
					Title = Strings.pickMobViewMonsterList,
					Description = "View the current monster slaughter list",
					Action = () =>
					{
						string infoMob = Strings.pickMobMonsterIdList + ": ";
						if (PickMobConfig.IdMobsTanSat.Count > 0)
						{
							foreach (int mobId in PickMobConfig.IdMobsTanSat)
								infoMob += mobId + ", ";
							infoMob = infoMob.Remove(infoMob.LastIndexOf(','), 2);
						}
						else
							infoMob += Strings.empty;
						infoMob += $"\n{Strings.pickMobMonsterTypeList}: ";
						if (PickMobConfig.TypeMobsTanSat.Count > 0)
						{
							foreach (int mobType in PickMobConfig.TypeMobsTanSat)
								infoMob += $"{Mob.arrMobTemplate[mobType].name} [{mobType}], ";
							infoMob = infoMob.Remove(infoMob.LastIndexOf(','), 2);
						}
						else
							infoMob += Strings.empty;
						GameCanvas.startOKDlg(infoMob);
					}
				}));
			}

			if (PickMobConfig.IdItemPicks.Count + PickMobConfig.TypeItemPicks.Count
			                                    + PickMobConfig.IdItemBlocks.Count + PickMobConfig.TypeItemBlocks.Count > 0)
			{
				functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "PickMob_ViewItemList",
					Title = Strings.pickMobViewItemList,
					Description = "View the current item pick/block lists",
					Action = () =>
					{
						string infoItem = Strings.pickMobAutoPickItemList + ": ";
						if (PickMobConfig.IdItemPicks.Count > 0)
						{
							foreach (short itemIdPick in PickMobConfig.IdItemPicks)
								infoItem += $"{ItemTemplates.get(itemIdPick).name} [{itemIdPick}], ";
							infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
						}
						else
							infoItem += Strings.empty;
						infoItem += $"\n{Strings.pickMobAutoPickItemTypeList}: ";
						if (PickMobConfig.TypeItemPicks.Count > 0)
						{
							foreach (sbyte itemTypePick in PickMobConfig.TypeItemPicks)
								infoItem += itemTypePick + ", ";
							infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
						}
						else
							infoItem += Strings.empty;
						infoItem += $"\n{Strings.pickMobDontPickItemList}: ";
						if (PickMobConfig.IdItemBlocks.Count > 0)
						{
							foreach (short itemIdBlock in PickMobConfig.IdItemBlocks)
								infoItem += $"{ItemTemplates.get(itemIdBlock).name} [{itemIdBlock}], ";
							infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
						}
						else
							infoItem += Strings.empty;
						infoItem += $"\n{Strings.pickMobDontPickItemTypeList}: ";
						if (PickMobConfig.TypeItemBlocks.Count > 0)
						{
							foreach (sbyte itemTypeBlock in PickMobConfig.TypeItemBlocks)
								infoItem += itemTypeBlock + ", ";
							infoItem = infoItem.Remove(infoItem.LastIndexOf(','), 2);
						}
						else
							infoItem += Strings.empty;
						GameCanvas.startOKDlg(infoItem);
					}
				}));
			}

			// --- View skill list ---
			functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
			{
				ID = "PickMob_ViewSkillList",
				Title = Strings.pickMobViewSkillList,
				Description = "View the current slaughter skill list",
				Action = () =>
				{
					string infoskill = Strings.pickMobSkillList + ": ";
					foreach (sbyte skillid in PickMobConfig.IdSkillsTanSat)
					{
						for (int i = 0; i < 8; i++)
						{
							SkillTemplate template = Char.myCharz().nClass.skillTemplates[i];
							if (template.id == skillid)
							{
								infoskill += $"{template.name} [{skillid}]";
								goto here;
							}
						}
					}
					here:
					GameCanvas.startOKDlg(infoskill);
				}
			}));

			// --- Auto change zone ---
			bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
			{
				ID = "PickMob_AutoChangeZone",
				Title = "Auto đổi khu",
				Description = "Automatically change zone when no monsters are available",
				GetValueFunc = () => PickMobConfig.IsAutoChangeZone,
				SetValueAction = PickMobConfig.SetAutoChangeZone,
				RMSName = "pickmob_auto_change_zone"
			}));

			// --- Priority zone ---
			bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
			{
				ID = "PickMob_PriorityZone",
				Title = "Priority zone",
				Description = "Prioritize certain zones when auto changing",
				GetValueFunc = () => PickMobConfig.IsPriorityZone,
				SetValueAction = PickMobConfig.SetPriorityZone,
				RMSName = "pickmob_priority_zone"
			}));

			// --- Auto fusion ---
			bools.Add(new ModMenuItemBoolean(new ModMenuItemBooleanConfig
			{
				ID = "PickMob_AutoFusion",
				Title = "Auto hợp thể",
				Description = "Automatically fuse when conditions are met",
				GetValueFunc = () => PickMobConfig.IsAutoFusion,
				SetValueAction = PickMobConfig.SetAutoFusion,
				RMSName = "pickmob_auto_fusion"
			}));

			modMenuItemBools = bools.ToArray();
			modMenuItemFunctions = functions.ToArray();
		}

		static void SetTab(Panel p)
		{
			SetTabPanelTemplates.setTabListTemplate(p, modMenuItemBools, modMenuItemFunctions);
			p.tabName[CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU] = new[]
			{
				new[]
				{
					"Toggles"
				},
				new[]
				{
					"Actions"
				}
			};
		}

		static void DoFire(Panel p)
		{
			if (p.currentTabIndex == 0)
				DoFireBool(p);
			else if (p.currentTabIndex == 1)
				DoFireFunction(p);
		}

		static void DoFireBool(Panel p)
		{
			if (p.selected < 0 || p.selected >= modMenuItemBools.Length)
				return;
			modMenuItemBools[p.selected].SwitchSelection();
			GameScr.info1.addInfo(modMenuItemBools[p.selected].Title + ": done!", 0);
		}

		static void DoFireFunction(Panel p)
		{
			if (p.selected < 0 || p.selected >= modMenuItemFunctions.Length)
				return;
			p.hide();
			modMenuItemFunctions[p.selected].Action?.Invoke();
		}

		static void PaintTabHeader(Panel p, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(p, g, "PickMob Settings");
		}

		static void Paint(Panel p, mGraphics g)
		{
			if (p.currentTabIndex == 0)
				PaintBools(p, g);
			else if (p.currentTabIndex == 1)
				PaintFunctions(p, g);
		}

		static void PaintBools(Panel p, mGraphics g)
		{
			if (modMenuItemBools == null || modMenuItemBools.Length != p.currentListLength)
				return;
			int offset = System.Math.Max(p.cmy / p.ITEM_HEIGHT, 0);
			for (int i = offset; i < Mathf.Clamp(offset + p.hScroll / p.ITEM_HEIGHT + 2, 0, p.currentListLength); i++)
			{
				int xScroll = p.xScroll;
				int yScroll = p.yScroll + i * p.ITEM_HEIGHT;
				int wScroll = p.wScroll;
				int itemHeight = p.ITEM_HEIGHT - 1;
				ModMenuItemBoolean item = modMenuItemBools[i];
				g.setColor(i != p.selected ? 0xE7DFD2 : 0xF9FF4A);
				g.fillRect(xScroll, yScroll, wScroll, itemHeight);
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + item.Title, xScroll + 7, yScroll, 0);
				mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(item.Description, style, p.wScroll - 5), xScroll + 7, yScroll + 11, 0);
			}
			p.paintScrollArrow(g);
		}

		static void PaintFunctions(Panel p, mGraphics g)
		{
			if (modMenuItemFunctions == null || modMenuItemFunctions.Length != p.currentListLength)
				return;
			int offset = System.Math.Max(p.cmy / p.ITEM_HEIGHT, 0);
			for (int i = offset; i < Mathf.Clamp(offset + p.hScroll / p.ITEM_HEIGHT + 2, 0, p.currentListLength); i++)
			{
				int xScroll = p.xScroll;
				int yScroll = p.yScroll + i * p.ITEM_HEIGHT;
				int wScroll = p.wScroll;
				int itemHeight = p.ITEM_HEIGHT - 1;
				ModMenuItemFunction item = modMenuItemFunctions[i];
				g.setColor(i != p.selected ? 0xE7DFD2 : 0xF9FF4A);
				g.fillRect(xScroll, yScroll, wScroll, itemHeight);
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + item.Title, xScroll + 7, yScroll, 0);
				mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(item.Description, style, p.wScroll - 5), xScroll + 7, yScroll + 11, 0);
			}
			p.paintScrollArrow(g);
		}
	}
}
