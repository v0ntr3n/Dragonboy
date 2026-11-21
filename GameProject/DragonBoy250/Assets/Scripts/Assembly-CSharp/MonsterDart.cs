public class MonsterDart : Effect2
{
	public int va;

	private DartInfo info;

	public static MyRandom r = new MyRandom();

	public int angle;

	public int vx;

	public int vy;

	public int x;

	public int y;

	public int z;

	public int xTo;

	public int yTo;

	private int life;

	public bool isSpeedUp;

	public long dame;

	public long dameMp;

	public Char c;

	public bool isBoss;

	public MyVector darts = new MyVector();

	private int dx;

	private int dy;

	public static int[] ARROWINDEX = new int[18]
	{
		0, 15, 37, 52, 75, 105, 127, 142, 165, 195,
		217, 232, 255, 285, 307, 322, 345, 370
	};

	public static int[] TRANSFORM = new int[16]
	{
		0, 0, 0, 7, 6, 6, 6, 2, 2, 3,
		3, 4, 5, 5, 5, 1
	};

	public static sbyte[] FRAME = new sbyte[25]
	{
		0, 1, 2, 1, 0, 1, 2, 1, 0, 1,
		2, 1, 0, 1, 2, 1, 0, 1, 2, 1,
		0, 1, 2, 1, 0
	};

	public MonsterDart(int x, int y, bool isBoss, long dame, long dameMp, Char c, int dartType)
	{
		info = (GameScr.darts != null && dartType >= 0 && dartType < GameScr.darts.Length) ? GameScr.darts[dartType] : null;
		this.x = x;
		this.y = y;
		this.isBoss = isBoss;
		this.dame = dame;
		this.dameMp = dameMp;
		this.c = c;
		if (info != null)
		{
			va = info.va;
		}
		if (c != null)
		{
			setAngle(Res.angle(c.cx - x, c.cy - y));
		}
		if (x >= GameScr.cmx && x <= GameScr.cmx + GameCanvas.w)
		{
			SoundMn.gI().mobKame(dartType);
		}
	}

	public MonsterDart(int x, int y, bool isBoss, long dame, long dameMp, int xTo, int yTo, int dartType)
	{
		info = (GameScr.darts != null && dartType >= 0 && dartType < GameScr.darts.Length) ? GameScr.darts[dartType] : null;
		this.x = x;
		this.y = y;
		this.isBoss = isBoss;
		this.dame = dame;
		this.dameMp = dameMp;
		this.xTo = xTo;
		this.yTo = yTo;
		if (info != null)
		{
			va = info.va;
		}
		setAngle(Res.angle(xTo - x, yTo - y));
		if (x >= GameScr.cmx && x <= GameScr.cmx + GameCanvas.w)
		{
			SoundMn.gI().mobKame(dartType);
		}
		c = null;
	}

	public void setAngle(int angle)
	{
		this.angle = angle;
		vx = va * Res.cos(angle) >> 10;
		vy = va * Res.sin(angle) >> 10;
	}

	public static void addMonsterDart(int x, int y, bool isBoss, long dame, long dameMp, Char c, int dartType)
	{
		if (c == null || GameScr.darts == null || dartType < 0 || dartType >= GameScr.darts.Length || GameScr.darts[dartType] == null)
		{
			return;
		}
		Effect2.vEffect2.addElement(new MonsterDart(x, y, isBoss, dame, dameMp, c, dartType));
	}

	public static void addMonsterDart(int x, int y, bool isBoss, long dame, long dameMp, int xTo, int yTo, int dartType)
	{
		if (GameScr.darts == null || dartType < 0 || dartType >= GameScr.darts.Length || GameScr.darts[dartType] == null)
		{
			return;
		}
		Effect2.vEffect2.addElement(new MonsterDart(x, y, isBoss, dame, dameMp, xTo, yTo, dartType));
	}

	public override void update()
	{
		if (info == null)
		{
			Effect2.vEffect2.removeElement(this);
			return;
		}
		for (int i = 0; i < info.nUpdate; i++)
		{
			if (info.tail != null && info.tail.Length > 0)
			{
				darts.addElement(new SmallDart(x, y));
			}
			dx = ((c == null) ? xTo : c.cx) - x;
			dy = ((c == null) ? yTo : c.cy) - 10 - y;
			int num = 60;
			if (TileMap.mapID == 0)
			{
				num = 600;
			}
			life++;
			if ((c != null && (c.statusMe == 5 || c.statusMe == 14)) || c == null)
			{
				x += (((c == null) ? xTo : c.cx) - x) / 2;
				y += (((c == null) ? yTo : c.cy) - y) / 2;
			}
			if ((Res.abs(dx) < 16 && Res.abs(dy) < 16) || life > num)
			{
				if (c != null && c.charID >= 0 && dameMp != -1)
				{
					if (dameMp != -100)
					{
						c.doInjure(dame, dameMp, false, true);
					}
					else
					{
						ServerEffect.addServerEffect(80, c, 1);
					}
				}
				Effect2.vEffect2.removeElement(this);
				if (dameMp != -100)
				{
					ServerEffect.addServerEffect(81, c, 1);
					if (x >= GameScr.cmx && x <= GameScr.cmx + GameCanvas.w)
					{
						SoundMn.gI().explode_2();
					}
				}
			}
			int num2 = Res.angle(dx, dy);
			if (Math.abs(num2 - angle) < 90 || dx * dx + dy * dy > 4096)
			{
				if (Math.abs(num2 - angle) < 15)
				{
					angle = num2;
				}
				else if ((num2 - angle >= 0 && num2 - angle < 180) || num2 - angle < -180)
				{
					angle = Res.fixangle(angle + 15);
				}
				else
				{
					angle = Res.fixangle(angle - 15);
				}
			}
			if (!isSpeedUp && va < 8192)
			{
				va += 1024;
			}
			vx = va * Res.cos(angle) >> 10;
			vy = va * Res.sin(angle) >> 10;
			dx += vx;
			int num3 = dx >> 10;
			x += num3;
			dx &= 1023;
			dy += vy;
			int num4 = dy >> 10;
			y += num4;
			dy &= 1023;
		}
		int tailLen = (info.tail != null) ? info.tail.Length : 0;
		for (int j = darts.size() - 1; j >= 0; j--)
		{
			SmallDart smallDart = (SmallDart)darts.elementAt(j);
			smallDart.index++;
			if (smallDart.index >= tailLen)
			{
				darts.removeElementAt(j);
			}
		}
	}

	public static int findDirIndexFromAngle(int angle)
	{
		for (int i = 0; i < ARROWINDEX.Length - 1; i++)
		{
			if (angle >= ARROWINDEX[i] && angle <= ARROWINDEX[i + 1])
			{
				if (i >= 16)
				{
					return 0;
				}
				return i;
			}
		}
		return 0;
	}

	public override void paint(mGraphics g)
	{
		if (info == null || info.headBorder == null || info.headBorder.Length == 0)
		{
			return;
		}
		int num = findDirIndexFromAngle(360 - angle);
		int num2 = FRAME[num];
		int transform = TRANSFORM[num];
		if (info.tailBorder != null && info.tailBorder.Length > 0)
		{
			for (int i = darts.size() / 2; i < darts.size(); i++)
			{
				SmallDart smallDart = (SmallDart)darts.elementAt(i);
				if (smallDart != null && smallDart.index >= 0 && smallDart.index < info.tailBorder.Length)
				{
					SmallImage.drawSmallImage(g, info.tailBorder[smallDart.index], smallDart.x, smallDart.y, 0, 3);
				}
			}
		}
		int num3 = GameCanvas.gameTick % info.headBorder.Length;
		if (info.headBorder[num3] != null && num2 >= 0 && num2 < info.headBorder[num3].Length)
		{
			SmallImage.drawSmallImage(g, info.headBorder[num3][num2], x, y, transform, 3);
		}
		if (info.tail != null && info.tail.Length > 0)
		{
			for (int j = 0; j < darts.size(); j++)
			{
				SmallDart smallDart2 = (SmallDart)darts.elementAt(j);
				if (smallDart2 != null && smallDart2.index >= 0 && smallDart2.index < info.tail.Length)
				{
					SmallImage.drawSmallImage(g, info.tail[smallDart2.index], smallDart2.x, smallDart2.y, 0, 3);
				}
			}
		}
		if (info.head != null && num3 < info.head.Length && info.head[num3] != null && num2 >= 0 && num2 < info.head[num3].Length)
		{
			SmallImage.drawSmallImage(g, info.head[num3][num2], x, y, transform, 3);
		}
		if (info.xd1 != null && info.xd2 != null)
		{
			for (int k = 0; k < darts.size(); k++)
			{
				SmallDart smallDart3 = (SmallDart)darts.elementAt(k);
				if (smallDart3 != null && Res.abs(r.nextInt(100)) < info.xdPercent)
				{
					short[] xdArr = (GameCanvas.gameTick % 2 != 0) ? info.xd2 : info.xd1;
					if (smallDart3.index >= 0 && smallDart3.index < xdArr.Length)
					{
						SmallImage.drawSmallImage(g, xdArr[smallDart3.index], smallDart3.x, smallDart3.y, 0, 3);
					}
				}
			}
		}
	}

	public static void addMonsterDart(int x2, int y2, bool checkIsBoss, int dame2, int dameMp2, Mob mobToAttack, sbyte dartType)
	{
		if (mobToAttack == null)
		{
			return;
		}
		addMonsterDart(x2, y2, checkIsBoss, dame2, dameMp2, mobToAttack.x, mobToAttack.y, dartType);
	}
}
