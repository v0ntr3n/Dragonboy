using System;
using Assets.src.e;

public class SmallImage
{
	public static int[][] smallImg;

	public static SmallImage instance;

	public static Image[] imgbig;

	public static Small[] imgNew;

	public static MyVector vKeys = new MyVector();

	public static Image imgEmpty = null;

	public static sbyte[] newSmallVersion;

	public static MyVector vt_images_watingDowload = new MyVector();

	public static int smallCount;

	public static short maxSmall;

	public SmallImage()
	{
		readImage();
	}

	public static void loadBigRMS()
	{
		if (imgbig == null || imgbig[0] == null)
		{
			Image b0 = GameCanvas.loadImageRMS("/img/Big0.png");
			if (b0 == null)
			{
				imgbig = null;
				return;
			}
			imgbig = new Image[6]
			{
				b0,
				GameCanvas.loadImageRMS("/img/Big1.png"),
				GameCanvas.loadImageRMS("/img/Big2.png"),
				GameCanvas.loadImageRMS("/img/Big3.png"),
				GameCanvas.loadImageRMS("/img/Big4.png"),
				GameCanvas.loadImageRMS("/img/Big5.png")
			};
		}
	}

	public static void freeBig()
	{
		if (imgbig != null)
		{
			for (int i = 0; i < imgbig.Length; i++)
			{
				if (imgbig[i] != null && imgbig[i] != imgEmpty)
				{
					imgbig[i].DestroyTexture();
				}
			}
		}
		imgbig = null;
		mSystem.gcc();
	}

	public static void loadBigImage()
	{
		imgEmpty = Image.createRGBImage(new int[1], 1, 1, true);
	}

	public static void init()
	{
		instance = null;
		instance = new SmallImage();
	}

	public void readData(byte[] data)
	{
	}

	public void readImage()
	{
		int num = 0;
		try
		{
			DataInputStream dataInputStream = new DataInputStream(Rms.loadRMS("NR_image"));
			short num2 = dataInputStream.readShort();
			smallImg = new int[num2][];
			for (int i = 0; i < smallImg.Length; i++)
			{
				smallImg[i] = new int[5];
			}
			for (int j = 0; j < num2; j++)
			{
				num++;
				smallImg[j][0] = dataInputStream.readUnsignedByte();
				smallImg[j][1] = dataInputStream.readShort();
				smallImg[j][2] = dataInputStream.readShort();
				smallImg[j][3] = dataInputStream.readShort();
				smallImg[j][4] = dataInputStream.readShort();
			}
		}
		catch (Exception ex)
		{
			Cout.LogError3("Loi readImage: " + ex.ToString() + "i= " + num);
		}
	}

	private static void evictSmallEntry(int idx)
	{
		if (imgNew == null || idx < 0 || idx >= imgNew.Length)
		{
			return;
		}
		Small entry = imgNew[idx];
		if (entry != null)
		{
			if (entry.img != null && entry.img != imgEmpty)
			{
				entry.img.DestroyTexture();
			}
			entry.img = null;
			imgNew[idx] = null;
		}
	}

	public static void clearHastable()
	{
		if (imgNew != null)
		{
			for (int i = 0; i < imgNew.Length; i++)
			{
				evictSmallEntry(i);
			}
		}
	}

	public static void createImage(int id)
	{
		if (id < 0 || imgNew == null || id >= imgNew.Length)
		{
			return;
		}
		if (imgNew[id] != null)
		{
			evictSmallEntry(id);
		}
		Res.outz("is request =" + id + " zoom=" + mGraphics.zoomLevel);
		if (mGraphics.zoomLevel == 1)
		{
			Image image = GameCanvas.loadImage("/SmallImage/Small" + id + ".png");
			if (image != null)
			{
				imgNew[id] = new Small(image, id);
				return;
			}
			imgNew[id] = new Small(imgEmpty, id);
			if (GameCanvas.currentScreen == GameCanvas._SelectCharScr || GameCanvas.currentScreen == CreateCharScr.instance)
			{
				Service.gI().requestIcon(id);
			}
			else
			{
				vt_images_watingDowload.addElement(imgNew[id]);
			}
			return;
		}
		Image image2 = GameCanvas.loadImage("/SmallImage/Small" + id + ".png");
		if (image2 != null)
		{
			imgNew[id] = new Small(image2, id);
			return;
		}
		bool flag = false;
		sbyte[] array = Rms.loadRMS(mGraphics.zoomLevel + "Small" + id);
		if (array != null)
		{
			if (newSmallVersion != null && (id >= newSmallVersion.Length || array.Length % 127 != newSmallVersion[id]))
			{
				flag = true;
			}
			if (!flag)
			{
				Image image3 = Image.createImage(array, 0, array.Length);
				if (image3 != null)
				{
					imgNew[id] = new Small(image3, id);
				}
				else
				{
					flag = true;
				}
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			imgNew[id] = new Small(imgEmpty, id);
			if (GameCanvas.currentScreen == GameCanvas._SelectCharScr || GameCanvas.currentScreen == CreateCharScr.instance)
			{
				Service.gI().requestIcon(id);
			}
			else
			{
				vt_images_watingDowload.addElement(imgNew[id]);
			}
		}
	}

	public static void drawSmallImage(mGraphics g, int id, int x, int y, int transform, int anchor)
	{
		if (id < 0)
		{
			return;
		}
		if (imgbig == null)
		{
			if (imgNew == null || id >= imgNew.Length)
			{
				return;
			}
			Small small = imgNew[id];
			if (small == null)
			{
				createImage(id);
			}
			else
			{
				small.paint(g, transform, x, y, anchor);
			}
		}
		else if (smallImg != null)
		{
			if (id >= smallImg.Length || smallImg[id] == null || smallImg[id][1] >= 256 || smallImg[id][3] >= 256 || smallImg[id][2] >= 256 || smallImg[id][4] >= 256)
			{
				if (imgNew == null || id >= imgNew.Length)
				{
					return;
				}
				Small small2 = imgNew[id];
				if (small2 == null)
				{
					createImage(id);
				}
				else
				{
					small2.paint(g, transform, x, y, anchor);
				}
			}
			else if (smallImg[id][0] >= 0 && smallImg[id][0] < imgbig.Length && imgbig[smallImg[id][0]] != null)
			{
				g.drawRegion(imgbig[smallImg[id][0]], smallImg[id][1], smallImg[id][2], smallImg[id][3], smallImg[id][4], transform, x, y, anchor);
			}
			else
			{
				if (imgNew == null || id >= imgNew.Length)
				{
					return;
				}
				Small small2 = imgNew[id];
				if (small2 == null)
				{
					createImage(id);
				}
				else
				{
					small2.paint(g, transform, x, y, anchor);
				}
			}
		}
		else if (GameCanvas.currentScreen != GameScr.gI())
		{
			if (imgNew == null || id >= imgNew.Length)
			{
				return;
			}
			Small small3 = imgNew[id];
			if (small3 == null)
			{
				createImage(id);
			}
			else
			{
				small3.paint(g, transform, x, y, anchor);
			}
		}
	}

	public static void drawSmallImage(mGraphics g, int id, int f, int x, int y, int w, int h, int transform, int anchor)
	{
		if (id < 0)
		{
			return;
		}
		if (imgbig == null)
		{
			if (imgNew == null || id >= imgNew.Length)
			{
				return;
			}
			Small small = imgNew[id];
			if (small == null)
			{
				createImage(id);
			}
			else
			{
				small.paint(g, transform, f, x, y, w, h, anchor);
			}
		}
		else if (smallImg != null)
		{
			if (id >= smallImg.Length || smallImg[id] == null || smallImg[id][1] >= 256 || smallImg[id][3] >= 256 || smallImg[id][2] >= 256 || smallImg[id][4] >= 256)
			{
				if (imgNew == null || id >= imgNew.Length)
				{
					return;
				}
				Small small2 = imgNew[id];
				if (small2 == null)
				{
					createImage(id);
				}
				else
				{
					small2.paint(g, transform, f, x, y, w, h, anchor);
				}
			}
			else if (smallImg[id][0] >= 0 && smallImg[id][0] < imgbig.Length && smallImg[id][0] != 4 && imgbig[smallImg[id][0]] != null)
			{
				g.drawRegion(imgbig[smallImg[id][0]], 0, f * w, w, h, transform, x, y, anchor);
			}
			else
			{
				if (imgNew == null || id >= imgNew.Length)
				{
					return;
				}
				Small small3 = imgNew[id];
				if (small3 == null)
				{
					createImage(id);
				}
				else
				{
					small3.paint(g, transform, f, x, y, w, h, anchor);
				}
			}
		}
		else if (GameCanvas.currentScreen != GameScr.gI())
		{
			if (imgNew == null || id >= imgNew.Length)
			{
				return;
			}
			Small small4 = imgNew[id];
			if (small4 == null)
			{
				createImage(id);
			}
			else
			{
				small4.paint(g, transform, f, x, y, w, h, anchor);
			}
		}
	}

	public static void update()
	{
		int num = 0;
		if (GameCanvas.gameTick % 1000 != 0 || imgNew == null)
		{
			return;
		}
		for (int i = 0; i < imgNew.Length; i++)
		{
			if (imgNew[i] != null)
			{
				imgNew[i].update();
				smallCount++;
				if (imgNew[i] != null)
				{
					num++;
				}
			}
		}
		int cap = GameCanvas.lowGraphic ? 200 : 512;
		if (num > cap)
		{
			int toEvict = num - cap;
			while (toEvict > 0)
			{
				int oldestIdx = -1;
				int minTick = int.MaxValue;
				for (int i = 0; i < imgNew.Length; i++)
				{
					Small s = imgNew[i];
					if (s == null)
					{
						continue;
					}
					if (Char.myCharz() != null && Char.myCharz().isCharBodyImageID(s.id))
					{
						continue;
					}
					if (GameCanvas.gameTick - s.lastPaintTick < 50)
					{
						continue;
					}
					if (s.lastPaintTick < minTick)
					{
						minTick = s.lastPaintTick;
						oldestIdx = i;
					}
				}
				if (oldestIdx < 0)
				{
					break;
				}
				evictSmallEntry(oldestIdx);
				toEvict--;
			}
		}
	}
}
