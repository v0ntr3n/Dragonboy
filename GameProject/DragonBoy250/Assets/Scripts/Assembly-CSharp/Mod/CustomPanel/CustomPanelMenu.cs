using System;
using Mod.ModMenu;
using Mod.R;
using UnityEngine;

namespace Mod.CustomPanel
{
	internal class CustomPanelMenu
	{
		internal static readonly int TYPE_CUSTOM_PANEL_MENU = 27;

		static readonly CustomPanelMenu customPanel = new CustomPanelMenu
		{
			panel = GameCanvas.panel
		};
		static readonly CustomPanelMenu customPanel2 = new CustomPanelMenu
		{
			panel = GameCanvas.panel2
		};
		CustomPanelMenuConfig config;

		Panel panel;

		internal static void Show(CustomPanelMenuConfig config, Panel panel = null)
		{
			Panel targetPanel = panel ?? GameCanvas.panel;
			if (targetPanel == null)
				return;
			GetCustomPanel(targetPanel).ConfigAndShow(config);
		}

		static CustomPanelMenu GetCustomPanel(Panel panel)
		{
			if (panel == GameCanvas.panel)
			{
				customPanel.panel = panel;
				return customPanel;
			}
			customPanel2.panel = panel;
			return customPanel2;
		}

		void ConfigAndShow(CustomPanelMenuConfig customPanelMenuConfig)
		{
			panel.type = TYPE_CUSTOM_PANEL_MENU;
			config = customPanelMenuConfig;
			SetType();
			panel.show();
		}

		void SetType()
		{
			SoundMn.gI().getSoundOption();
			if (config.SetTabAction.Method == new Action<Panel>(ModMenuMain.SetTabModMenu).Method) //Mod menu main
			{
				panel.mainTabName = panel.tabName[TYPE_CUSTOM_PANEL_MENU] = Strings.modMenuPanelTabName;
				panel.currentTabIndex = Mathf.Clamp(panel.currentTabIndex, 0, Strings.modMenuPanelTabName.Length - 1);
			}
			else
			{
				panel.tabName[TYPE_CUSTOM_PANEL_MENU] = new[]
				{
					new[]
					{
						string.Empty
					}
				};
				panel.currentTabIndex = 0;
			}
			panel.setType(panel == GameCanvas.panel ? 0 : 1);
			SetTab(panel);
		}

		internal static bool PaintTabHeader(Panel panel, mGraphics g)
		{
			CustomPanelMenu customPanelMenu = GetCustomPanel(panel);
			if (customPanelMenu == null || customPanelMenu.config == null || customPanelMenu.config.PaintTabHeaderAction == null)
				return false;
			customPanelMenu.config.PaintTabHeaderAction(panel, g);
			return true;
		}

		internal static void SetTab(Panel panel)
		{
			CustomPanelMenu customPanelMenu = GetCustomPanel(panel);
			if (customPanelMenu == null || customPanelMenu.config == null)
				return;
			customPanelMenu.config.SetTabAction?.Invoke(panel);
		}

		internal static void DoFire(Panel panel)
		{
			CustomPanelMenu customPanelMenu = GetCustomPanel(panel);
			if (customPanelMenu == null || customPanelMenu.config == null)
				return;
			customPanelMenu.config.DoFireItemAction?.Invoke(panel);
		}

		internal static void Paint(Panel panel, mGraphics g)
		{
			CustomPanelMenu customPanelMenu = GetCustomPanel(panel);
			if (customPanelMenu == null || customPanelMenu.config == null)
				return;
			customPanelMenu.config.PaintAction?.Invoke(panel, g);
		}

		internal static void PaintTopInfo(Panel panel, mGraphics g)
		{
			CustomPanelMenu customPanelMenu = GetCustomPanel(panel);
			if (customPanelMenu == null || customPanelMenu.config == null)
				return;
			if (customPanelMenu.config.PaintTopInfoAction != null)
			{
				customPanelMenu.config.PaintTopInfoAction(panel, g);
				return;
			}
			SmallImage.drawSmallImage(g, Utils.ID_NPC_MOD_FACE, panel.X + 25, 50, 0, 33);
			mFont.tahoma_7b_white.drawString(g, Strings.communityMod, panel.X + 60, 4, mFont.LEFT, mFont.tahoma_7b_dark);
			mFont.tahoma_7_yellow.drawString(g, Strings.gameVersion + ": v" + GameMidlet.VERSION, panel.X + 60, 16, mFont.LEFT, mFont.tahoma_7_grey);
			g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
			mFont.tahoma_7_yellow.drawString(g, mResources.character + ": " + Char.myCharz().cName, panel.X + 60, 27, mFont.LEFT, mFont.tahoma_7_grey);
			string serverName = (ServerListScreen.nameServer != null && ServerListScreen.ipSelect >= 0 && ServerListScreen.ipSelect < ServerListScreen.nameServer.Length)
				? ServerListScreen.nameServer[ServerListScreen.ipSelect]
				: string.Empty;
			mFont.tahoma_7_yellow.drawString(g, mResources.account + " " + mResources.account_server.ToLower() + " " + serverName, panel.X + 60, 38, mFont.LEFT, mFont.tahoma_7_grey);
		}
	}
}
