namespace Mod.PickMob.Services
{
	public static class MovementService
	{
		const int ID_ICON_ITEM_TDLT = 4387;

		public static void Move(Char myChar, int x, int y)
		{
			int[] vs = GetPointYsdMax(myChar.cx, x);
			if (vs[1] >= y || vs[1] >= myChar.cy && (myChar.statusMe == 2 || myChar.statusMe == 1))
			{
				vs[0] = x;
				vs[1] = y;
			}

			if (ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
			{
				Utils.TeleportMyChar(vs[0], vs[1]);
				return;
			}

			myChar.currentMovePoint = new MovePoint(vs[0], vs[1]);
		}

		static int[] GetPointYsdMax(int xStart, int xEnd)
		{
			int ysdMin = TileMap.pxh;
			int x = -1;

			if (xStart > xEnd)
			{
				for (int i = xEnd; i < xStart; i += 24)
				{
					int ysd = GetYsd(i);
					if (ysd < ysdMin)
					{
						ysdMin = ysd;
						x = i;
					}
				}
			}
			else
			{
				for (int i = xEnd; i > xStart; i -= 24)
				{
					int ysd = GetYsd(i);
					if (ysd < ysdMin)
					{
						ysdMin = ysd;
						x = i;
					}
				}
			}
			int[] vs =
			{
				x, ysdMin
			};
			return vs;
		}

		static int GetYsd(int xsd)
		{
			Char myChar = Char.myCharz();
			int dmin = TileMap.pxh;
			int d;
			int ysdBest = -1;
			for (int i = 24; i < TileMap.pxh; i += 24)
			{
				if (TileMap.tileTypeAt(xsd, i, 2))
				{
					d = Res.abs(i - myChar.cy);
					if (d < dmin)
					{
						dmin = d;
						ysdBest = i;
					}
				}
			}
			return ysdBest;
		}
	}
}
