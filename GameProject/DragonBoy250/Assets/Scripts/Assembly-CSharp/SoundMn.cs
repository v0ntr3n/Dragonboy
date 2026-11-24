using System;
using Mod;

public class SoundMn
{
	public class MediaPlayer
	{
	}

	public class SoundPool
	{
	}

	public class AssetManager
	{
	}

	public static bool IsDelAcc;

	public static SoundMn gIz;

	public static bool isSound = false;

	public static float volume = 0f;

	private static int MAX_VOLUME = 10;

	public static MediaPlayer[] music;

	public static SoundPool[] sound;

	public static int[] soundID;

	public static int AIR_SHIP;

	public static int RAIN = 1;

	public static int TAITAONANGLUONG = 2;

	public static int GET_ITEM;

	public static int MOVE = 1;

	public static int LOW_PUNCH = 2;

	public static int LOW_KICK = 3;

	public static int FLY = 4;

	public static int JUMP = 5;

	public static int PANEL_OPEN = 6;

	public static int BUTTON_CLOSE = 7;

	public static int BUTTON_CLICK = 8;

	public static int MEDIUM_PUNCH = 9;

	public static int MEDIUM_KICK = 10;

	public static int PANEL_CLICK = 11;

	public static int EAT_PEAN = 12;

	public static int OPEN_DIALOG = 13;

	public static int NORMAL_KAME = 14;

	public static int NAMEK_KAME = 15;

	public static int XAYDA_KAME = 16;

	public static int EXPLODE_1 = 17;

	public static int EXPLODE_2 = 18;

	public static int TRAIDAT_KAME = 19;

	public static int HP_UP = 20;

	public static int THAIDUONGHASAN = 21;

	public static int HOISINH = 22;

	public static int GONG = 23;

	public static int KHICHAY = 24;

	public static int BIG_EXPLODE = 25;

	public static int NAMEK_LAZER = 26;

	public static int NAMEK_CHARGE = 27;

	public static int RADAR_CLICK = 28;

	public static int RADAR_ITEM = 29;

	public static int FIREWORK = 30;

	public static int KAMEX10_0 = 31;

	public static int KAMEX10_1 = 32;

	public static int DESTROY_0 = 33;

	public static int DESTROY_1 = 34;

	public static int MAFUBA_0 = 35;

	public static int MAFUBA_1 = 36;

	public static int MAFUBA_2 = 37;

	public static int DESTROY_2 = 38;

	public bool freePool;

	public int poolCount;

	public static int cout = 1;

	public static void init(AssetManager ac)
	{
	}

	public static SoundMn gI()
	{
		if (gIz == null)
		{
			gIz = new SoundMn();
		}
		return gIz;
	}

	public void loadSound(int mapID)
	{
	}

	public void getSoundOption()
	{
		if (GameEvents.OnGetSoundOption())
		{
			return;
		}
		if (GameCanvas.loginScr.isLogin2 && Char.myCharz().taskMaint != null && Char.myCharz().taskMaint.taskId >= 2)
		{
			Panel.strTool = new string[10]
			{
				mResources.radaCard,
				mResources.quayso,
				mResources.gameInfo,
				mResources.change_flag,
				mResources.change_zone,
				mResources.chat_world,
				mResources.account,
				mResources.option,
				mResources.change_account,
				mResources.REGISTOPROTECT
			};
			if (Char.myCharz().havePet)
			{
				Panel.strTool = new string[11]
				{
					mResources.radaCard,
					mResources.quayso,
					mResources.gameInfo,
					mResources.pet,
					mResources.change_flag,
					mResources.change_zone,
					mResources.chat_world,
					mResources.account,
					mResources.option,
					mResources.change_account,
					mResources.REGISTOPROTECT
				};
			}
		}
		else
		{
			Panel.strTool = new string[9]
			{
				mResources.radaCard,
				mResources.quayso,
				mResources.gameInfo,
				mResources.change_flag,
				mResources.change_zone,
				mResources.chat_world,
				mResources.account,
				mResources.option,
				mResources.change_account
			};
			if (Char.myCharz().havePet)
			{
				Panel.strTool = new string[10]
				{
					mResources.radaCard,
					mResources.quayso,
					mResources.gameInfo,
					mResources.pet,
					mResources.change_flag,
					mResources.change_zone,
					mResources.chat_world,
					mResources.account,
					mResources.option,
					mResources.change_account
				};
			}
		}
		if (IsDelAcc)
		{
			string[] array = new string[Panel.strTool.Length + 1];
			for (int i = 0; i < Panel.strTool.Length; i++)
			{
				array[i] = Panel.strTool[i];
			}
			array[Panel.strTool.Length] = mResources.delacc;
			Panel.strTool = array;
		}
	}

	public void getStrOption()
	{
		if (GameEvents.OnSoundMnGetStrOption())
		{
			return;
		}
		string text = "[x]   ";
		string text2 = "[  ]   ";
		if (Main.isPC)
		{
			Panel.strCauhinh = new string[4]
			{
				(!Char.isPaintAura) ? (text2 + mResources.aura_off.Trim()) : (text + mResources.aura_off.Trim()),
				(!Char.isPaintAura2) ? (text2 + mResources.aura_off_2.Trim()) : (text + mResources.aura_off_2.Trim()),
				text2 + mResources.turnOffSound,
				(mGraphics.zoomLevel <= 1) ? (text2 + mResources.x2Screen) : (text + mResources.x1Screen)
			};
			return;
		}
		string text3 = ((GameScr.isAnalog != 0) ? (text + mResources.turnOffAnalog) : (text2 + mResources.turnOnAnalog));
		if (!GameCanvas.isTouch)
		{
			text3 = (GameScr.isPaintChatVip ? (text + mResources.serverchat_off) : (text2 + mResources.serverchat_off));
		}
		Panel.strCauhinh = new string[5]
		{
			(!Char.isPaintAura) ? (text2 + mResources.aura_off.Trim()) : (text + mResources.aura_off.Trim()),
			(!Char.isPaintAura2) ? (text2 + mResources.aura_off_2.Trim()) : (text + mResources.aura_off_2.Trim()),
			text2 + mResources.turnOffSound,
			(!GameCanvas.lowGraphic) ? (text2 + mResources.cauhinhthap) : (text + mResources.cauhinhthap),
			text3
		};
	}

	public void HP_MPup()
	{
	}

	public void charPunch(bool isKick, float volumn)
	{
	}

	public void thaiduonghasan()
	{
	}

	public void rain()
	{
	}

	public void gongName()
	{
	}

	public void gong()
	{
	}

	public void getItem()
	{
	}

	public void soundToolOption()
	{
		GameCanvas.isPlaySound = false;
		Sound.stopAll = true;
		Rms.saveRMSInt("isPlaySound", 0);
		getStrOption();
	}

	public void chatVipToolOption()
	{
		GameScr.isPaintChatVip = !GameScr.isPaintChatVip;
		if (GameScr.isPaintChatVip)
		{
			Rms.saveRMSInt("serverchat", 0);
		}
		else
		{
			Rms.saveRMSInt("serverchat", 1);
		}
		getStrOption();
	}

	public void analogToolOption()
	{
		if (GameScr.isAnalog == 0)
		{
			GameScr.isAnalog = 1;
			Rms.saveRMSInt("analog", GameScr.isAnalog);
			GameScr.setSkillBarPosition();
		}
		else
		{
			GameScr.isAnalog = 0;
			Rms.saveRMSInt("analog", GameScr.isAnalog);
			GameScr.setSkillBarPosition();
		}
		getStrOption();
	}

	public void CaseAnalog()
	{
		if (!Main.isPC)
		{
			if (!GameCanvas.isTouch)
			{
				chatVipToolOption();
			}
			else
			{
				analogToolOption();
			}
		}
	}

	public void CaseSizeScr()
	{
		if (GameCanvas.lowGraphic)
		{
			Rms.saveRMSInt("lowGraphic", 0);
			GameCanvas.startOK(mResources.plsRestartGame, 8885, null);
		}
		else
		{
			Rms.saveRMSInt("lowGraphic", 1);
			GameCanvas.startOK(mResources.plsRestartGame, 8885, null);
		}
		getStrOption();
	}

	public void AuraToolOption()
	{
		if (Char.isPaintAura)
		{
			Rms.saveRMSInt("isPaintAura", 0);
			Char.isPaintAura = false;
		}
		else
		{
			Rms.saveRMSInt("isPaintAura", 1);
			Char.isPaintAura = true;
		}
		getStrOption();
	}

	public void AuraToolOption2()
	{
		if (Char.isPaintAura2)
		{
			Rms.saveRMSInt("isPaintAura2", 0);
			Char.isPaintAura2 = false;
		}
		else
		{
			Rms.saveRMSInt("isPaintAura2", 1);
			Char.isPaintAura2 = true;
		}
		getStrOption();
	}

	public void HatToolOption()
	{
		Service.gI().sendOptHat(0);
	}

	public void update()
	{
	}

	public void closeSound()
	{
		Sound.stopAll = true;
	}

	public void openSound()
	{
		Sound.stopAll = true;
	}

	public void bigeExlode()
	{
	}

	public void explode_1()
	{
	}

	public void explode_2()
	{
	}

	public void traidatKame()
	{
	}

	public void namekKame()
	{
	}

	public void nameLazer()
	{
	}

	public void xaydaKame()
	{
	}

	public void mobKame(int type)
	{
	}

	public void charRun(float volumn)
	{
	}

	public void monkeyRun(float volumn)
	{
	}

	public void charFall()
	{
	}

	public void charJump()
	{
	}

	public void panelOpen()
	{
	}

	public void buttonClose()
	{
	}

	public void buttonClick()
	{
	}

	public void stopMove()
	{
	}

	public void charFly()
	{
	}

	public void stopFly()
	{
	}

	public void openMenu()
	{
	}

	public void panelClick()
	{
	}

	public void eatPeans()
	{
	}

	public void openDialog()
	{
	}

	public void hoisinh()
	{
	}

	public void taitao()
	{
	}

	public void taitaoPause()
	{
	}

	public bool isPlayRain()
	{
		return false;
	}

	public bool isPlayAirShip()
	{
		return false;
	}

	public void airShip()
	{
	}

	public void pauseAirShip()
	{
	}

	public void resumeAirShip()
	{
	}

	public void stopAll()
	{
	}

	public void backToRegister()
	{
		if (!Mod.Utils.IsOpenedByExternalAccountManager &&
		    Mod.AccountManager.InGameAccountManager.SelectedAccount != null &&
		    Mod.AccountManager.InGameAccountManager.SelectedAccount.Type == Mod.AccountManager.AccountType.Unregistered &&
		    string.IsNullOrEmpty(Rms.loadRMSString(Rms.RMS_userAo + ServerListScreen.ipSelect)))
		{
			Rms.saveRMSString(Rms.RMS_userAo + ServerListScreen.ipSelect, Mod.AccountManager.InGameAccountManager.SelectedAccount.Username);
		}
		Session_ME.gI().close();
		GameCanvas.panel.hide();
		if (GameCanvas.loginScr == null)
		{
			GameCanvas.loginScr = new LoginScr();
		}
		GameCanvas.loginScr.isLogin2 = true;
		GameCanvas.loginScr.tfUser.setText(string.Empty);
		GameCanvas.loginScr.tfPass.setText(string.Empty);
		GameCanvas.loginScr.actRegister();
		GameCanvas.loginScr.switchToMe();
	}

	public void newKame()
	{
	}

	public void radarClick()
	{
	}

	public void radarItem()
	{
	}

	public static void playSound(int x, int y, int id, float volume)
	{
	}
}
