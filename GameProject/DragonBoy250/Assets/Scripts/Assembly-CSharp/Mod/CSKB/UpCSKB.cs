using System.Collections.Generic;
using Mod.CustomPanel;
using Mod.ModMenu;
using UnityEngine;

namespace Mod.CSKB
{
	public static class UpCSKB
	{
		const string RMS_ACTION_ON_FULL_BAG = "upcskb_action_on_full_bag";
		const string RMS_MONEY_TO_DEPOSIT = "upcskb_price_for_deposit";

		public static ActionOnFullBag actionOnFullBag = ActionOnFullBag.Sell;
		public static int moneyToDeposit = 40_000_000;

		static ModMenuItemValues[] modMenuItemValues;
		static ModMenuItemFunction[] modMenuItemFunctions;

		static readonly GUIStyle style = new GUIStyle
		{
			font = Resources.Load<Font>($"FontSys/x{mGraphics.zoomLevel}/chelthm")
		};

		static UpCSKB()
		{
			LoadRMS();
		}

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
			List<ModMenuItemValues> values = new List<ModMenuItemValues>();
			List<ModMenuItemFunction> functions = new List<ModMenuItemFunction>();

			// Action on full bag - cycle through options
			values.Add(new ModMenuItemValues(new ModMenuItemValuesConfig
			{
				ID = "UpCSKB_ActionOnFullBag",
				Title = "Khi x999 cskb",
				Description = "Action to take when CSKB reaches x999",
				Values = new[]
				{
					"Chuyển vào rương", "Kí gửi", "Bán vào cửa hàng"
				},
				GetValueFunc = () => (int)actionOnFullBag,
				SetValueAction = v => ApplyActionOnFullBag((ActionOnFullBag)(int)v, true, true),
				RMSName = RMS_ACTION_ON_FULL_BAG
			}));

			// Deposit amount - only show when action is Deposit
			if (actionOnFullBag == ActionOnFullBag.Deposit)
			{
				functions.Add(new ModMenuItemFunction(new ModMenuItemFunctionConfig
				{
					ID = "UpCSKB_MoneyToDeposit",
					Title = "Số tiền kí gửi khi x99 cskb",
					Description = mSystem.numberTostring(moneyToDeposit),
					Action = OpenTFInputMoneyToDeposit
				}));
			}

			modMenuItemValues = values.ToArray();
			modMenuItemFunctions = functions.ToArray();
		}

		static void SetTab(Panel p)
		{
			SetTabPanelTemplates.setTabListTemplate(p, modMenuItemValues, modMenuItemFunctions);
			p.tabName[CustomPanelMenu.TYPE_CUSTOM_PANEL_MENU] = new[]
			{
				new[]
				{
					"Settings"
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
			{
				DoFireValues(p);
			}
			else if (p.currentTabIndex == 1)
			{
				DoFireFunction(p);
			}
		}

		static void DoFireValues(Panel p)
		{
			if (p.selected < 0 || p.selected >= modMenuItemValues.Length)
			{
				return;
			}
			modMenuItemValues[p.selected].SwitchSelection();
			GameScr.info1.addInfo(modMenuItemValues[p.selected].Title + ": " + modMenuItemValues[p.selected].getSelectedValue() + "!", 0);
		}

		static void DoFireFunction(Panel p)
		{
			if (p.selected < 0 || p.selected >= modMenuItemFunctions.Length)
			{
				return;
			}
			p.hide();
			modMenuItemFunctions[p.selected].Action?.Invoke();
		}

		static void PaintTabHeader(Panel p, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(p, g, "Up CSKB Settings");
		}

		static void Paint(Panel p, mGraphics g)
		{
			if (p.currentTabIndex == 0)
			{
				PaintValues(p, g);
			}
			else if (p.currentTabIndex == 1)
			{
				PaintFunctions(p, g);
			}
		}

		static void PaintValues(Panel p, mGraphics g)
		{
			if (modMenuItemValues == null || modMenuItemValues.Length != p.currentListLength)
				return;
			int offset = System.Math.Max(p.cmy / p.ITEM_HEIGHT, 0);
			for (int i = offset; i < Mathf.Clamp(offset + p.hScroll / p.ITEM_HEIGHT + 2, 0, p.currentListLength); i++)
			{
				int xScroll = p.xScroll;
				int yScroll = p.yScroll + i * p.ITEM_HEIGHT;
				int wScroll = p.wScroll;
				int itemHeight = p.ITEM_HEIGHT - 1;
				ModMenuItemValues item = modMenuItemValues[i];
				g.setColor(i != p.selected ? 0xE7DFD2 : 0xF9FF4A);
				g.fillRect(xScroll, yScroll, wScroll, itemHeight);
				mFont.tahoma_7_green2.drawString(g, i + 1 + ". " + item.Title, xScroll + 7, yScroll, 0);
				string displayValue = item.Values != null ? item.getSelectedValue() : item.Description;
				mFont.tahoma_7_blue.drawString(g, Utils.TrimUntilFit(displayValue, style, p.wScroll - 5), xScroll + 7, yScroll + 11, 0);
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

		static void ApplyActionOnFullBag(ActionOnFullBag action, bool saveRms, bool showInfo)
		{
			actionOnFullBag = action;
			switch (action)
			{
			case ActionOnFullBag.PutToBox:
				if (showInfo)
					GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động chuyển vào rương", 0);
				break;
			case ActionOnFullBag.Deposit:
				if (showInfo)
					GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động kí gửi", 0);
				break;
			case ActionOnFullBag.Sell:
				if (showInfo)
					GameScr.info1.addInfo("[Up CSKB] Khi túi đầy sẽ tự động bán vào cửa hàng", 0);
				break;
			}

			if (saveRms)
				ModStorage.WriteInt(RMS_ACTION_ON_FULL_BAG, (int)actionOnFullBag);
		}

		static void OpenTFInputMoneyToDeposit()
		{
			ChatTextField.gI().strChat = "Nhập giá kí gửi";
			ChatTextField.gI().tfChat.name = "Nhập giá kí gửi";
			ChatTextField.gI().tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
			GameCanvas.panel.isShow = false;
			ChatTextField.gI().startChat2(new UpCSKBChatHandler(), string.Empty);
		}

		internal static void ApplyMoneyToDeposit(int money, bool saveRms, bool showInfo)
		{
			moneyToDeposit = money;
			if (showInfo)
			{
				GameScr.info1.addInfo($"[Up CSKB] Số tiền kí gửi khi túi đầy: {mSystem.numberTostring(moneyToDeposit)}", 0);
			}
			if (saveRms)
			{
				ModStorage.WriteInt(RMS_MONEY_TO_DEPOSIT, moneyToDeposit);
			}
		}

		static void LoadRMS()
		{
			int savedAction = ModStorage.ReadInt(RMS_ACTION_ON_FULL_BAG, (int)ActionOnFullBag.Deposit);
			if (savedAction == (int)ActionOnFullBag.PutToBox || savedAction == (int)ActionOnFullBag.Deposit || savedAction == (int)ActionOnFullBag.Sell)
			{
				ApplyActionOnFullBag((ActionOnFullBag)savedAction, false, false);
			}
			else
			{
				ApplyActionOnFullBag(actionOnFullBag, true, false);
			}

			int savedMoney = ModStorage.ReadInt(RMS_MONEY_TO_DEPOSIT, moneyToDeposit);
			if (savedMoney > 0)
			{
				ApplyMoneyToDeposit(savedMoney, false, false);
			}
			else
			{
				ApplyMoneyToDeposit(savedAction, true, false);
			}
		}
	}

	internal class UpCSKBChatHandler : IChatable
	{
		public void onChatFromMe(string text, string to)
		{
			if (ChatTextField.gI().tfChat.getText() == null || ChatTextField.gI().tfChat.getText().Equals(string.Empty) || text.Equals(string.Empty))
			{
				ChatTextField.gI().isShow = false;
			}
			else if (ChatTextField.gI().strChat.Equals("Nhập giá kí gửi"))
			{
				try
				{
					int price = int.Parse(ChatTextField.gI().tfChat.getText());
					ApplyMoneyToDeposit(price, true, true);
				}
				catch
				{
					GameScr.info1.addInfo("Giá Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ChatTextField.gI().ResetTF();
			}
		}

		public void onCancelChat()
		{
		}

		static void ApplyMoneyToDeposit(int money, bool saveRms, bool showInfo)
		{
			UpCSKB.ApplyMoneyToDeposit(money, saveRms, showInfo);
		}
	}
}
