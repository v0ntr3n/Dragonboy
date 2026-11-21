using System.Collections.Generic;
using Mod.CustomPanel;

namespace Mod.Xmap
{
	internal static class XmapPanel
	{
		static readonly List<int> currentMaps = new List<int>();

		internal static void Show(List<int> maps)
		{
			currentMaps.Clear();
			currentMaps.AddRange(maps);
			CustomPanelMenu.Show(new CustomPanelMenuConfig
			{
				SetTabAction = SetTab,
				DoFireItemAction = DoFire,
				PaintTabHeaderAction = PaintTabHeader,
				PaintAction = Paint
			});
		}

		static void Paint(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintCollectionCaptionAndDescriptionTemplate(
				panel,
				g,
				currentMaps,
				mapID => TileMap.mapNames != null && mapID >= 0 && mapID < TileMap.mapNames.Length ? TileMap.mapNames[mapID] : $"Map {mapID}",
				mapID => $"ID: {mapID}");
		}

		static void PaintTabHeader(Panel panel, mGraphics g)
		{
			PaintPanelTemplates.PaintTabHeaderTemplate(panel, g, "Xmap");
		}

		static void SetTab(Panel panel)
		{
			SetTabPanelTemplates.setTabListTemplate(panel, currentMaps);
		}

		static void DoFire(Panel panel)
		{
			if (panel == null || panel.selected < 0 || panel.selected >= currentMaps.Count)
			{
				return;
			}
			InfoDlg.hide();
			panel.hide();
			XmapController.start(currentMaps[panel.selected]);
		}
	}
}
