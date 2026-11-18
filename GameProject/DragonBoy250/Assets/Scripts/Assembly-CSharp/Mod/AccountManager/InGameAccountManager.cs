using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Mod.Graphics;
using Mod.ModHelper;
using Mod.R;
using UnityEngine;

namespace Mod.AccountManager
{
	internal class InGameAccountManager : mScreen
	{

		static readonly TimeSpan ImportRegexTimeout = TimeSpan.FromSeconds(2);
		static readonly Regex regexMatchUserAo = new Regex("^User[0-9]{1,}$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

		static readonly Texture2D earthOverlay = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(earthOverlay));
		static readonly Texture2D namekOverlay = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(namekOverlay));
		static readonly Texture2D saiyanOverlay = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(saiyanOverlay));
		static Texture2D add = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(add));
		static Texture2D addMultiple = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(addMultiple));
		static Texture2D hide = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(hide));
		static Texture2D show = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(show));
		static Texture2D up = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(up));
		static Texture2D down = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(down));
		static Texture2D help = Resources.Load<Texture2D>("InGameAccountManager/img/" + nameof(help));
		static readonly Dictionary<int, Texture2D> icons = new Dictionary<int, Texture2D>();

		static readonly Command closeAccountManager = new Command("", ActionListener.gI(), (int)CommandType.CloseAccountManager, null)
		{
			imgFocus = new Image()
		};
		static Command selectAccountToLogin;
		static Command addAccount;
		static Command editAccount;
		static Command deleteAccount;
		static Command finishInputAccount;
		static Command editCustomServer;
		static Command finishEditCustomServer;
		static Command cancelEditCustomServer;
		static readonly Command closeInputAccount = new Command("", ActionListener.gI(), (int)CommandType.CloseInputAccount, null)
		{
			imgFocus = new Image()
		};
		static Command cancelInputAccount;
		static Command toggleHidePassword;
		static Command importAccounts;
		static Command moveAccountUp;
		static Command moveAccountDown;
		static Command helpImportAccounts;
		static Command finishImportAccounts;
		static Command cancelImportAccounts;
		static readonly Command closeImportAccounts = new Command("", ActionListener.gI(), (int)CommandType.CloseImportAccounts, null)
		{
			imgFocus = new Image()
		};

		static ComboBox selectServer;
		static ScrollableMenuItems<Account> scrollableMenuAccounts;

		static TField tfUser;
		static TField tfPass;
		static TField tfCustomServerName;
		static TField tfCustomServerAddress;
		static TField tfCustomServerPort;
		static TField tfInputDataImportAccounts;
		static TField tfInputRegexMatchLinesImportAccounts;
		static TField tfInputRegexMatchAccountInfoImportAccounts;
		static Server[] defaultServers;
		static Server customServer;
		static int currentAccountInfoX;

		static bool isSwitchedToMe;
		static bool isAddingAccount;
		static bool isEditingAccount;
		static bool isEditingCustomServer;
		static bool isImportingAccounts;

		static readonly int TITLE_HEIGHT = 30;
		static readonly int ACCOUNT_HEIGHT = 34;
		static readonly int X_LIST_ACCOUNTS = 50;
		static readonly int Y_LIST_ACCOUNTS = 30;
		static readonly int DEFAULT_STEP_SCROLL = 70;
		static readonly int WIDTH_ACCOUNT_INFO = 160;
		static readonly int INPUT_ACCOUNT_WIDTH = 200;
		static readonly int INPUT_ACCOUNT_HEIGHT = 180;
		static readonly int SELECT_SERVER_WIDTH = 500;
		static readonly int SELECT_SERVER_HEIGHT = 300;
		static readonly int IMPORT_ACCOUNTS_WIDTH = 250;
		static readonly int IMPORT_ACCOUNTS_HEIGHT = 215;

		//[name]:[address]:[port]:[language]:[typesv]:[isnew],...
		//Super 2:dragon11.teamobi.com:17001:0:1:1,0,0
		static InGameAccountManager instance;
		static List<Account> accounts => InGameAccountDAO.Accounts;

		static int selectedAccountIndex
		{
			get => InGameAccountDAO.SelectedAccountIndex;
			set => InGameAccountDAO.SelectedAccountIndex = value;
		}

		internal static Account SelectedAccount => InGameAccountDAO.SelectedAccount;

		internal static Server SelectedServer { get; set; }

		static InGameAccountManager gI()
		{
			instance ??= new InGameAccountManager();
			return instance;
		}

		public override void paint(mGraphics g)
		{
			g.setColor(0);
			g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
			GameCanvas.paintBGGameScr(g);
			g.reset();
			PaintListAccounts(g);
			PaintCurrentAccountInfo(g);
			base.paint(g);
			if (isImportingAccounts)
				PaintImportAccounts(g);
			else if (isEditingCustomServer)
				PaintEditCustomServer(g);
			else if (isAddingAccount || isEditingAccount)
				PaintInputAccount(g);
		}

		public override void switchToMe()
		{
			isSwitchedToMe = true;
			string[] serverNames = ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0
				? ServerListScreen.nameServer
				: new[] { GameEvents.TEAMOBI2026_SERVER_NAME };
			if (defaultServers == null || defaultServers.Length != serverNames.Length)
			{
				defaultServers = new Server[serverNames.Length];
				for (int i = 0; i < defaultServers.Length; i++)
					defaultServers[i] = new Server(i);
			}
			isAddingAccount = isEditingAccount = false;
			GetAccountsArea(out int x, out int y, out int width, out int height, true);
			scrollableMenuAccounts.X = x;
			scrollableMenuAccounts.Y = y;
			scrollableMenuAccounts.Width = width;
			scrollableMenuAccounts.Height = height;
			scrollableMenuAccounts.Reset();
			editAccount = new Command(Strings.edit, ActionListener.gI(), (int)CommandType.EditAccount, null)
			{
				w = 50
			};
			deleteAccount = new Command(Strings.delete, ActionListener.gI(), (int)CommandType.ConfirmDeleteAccount, null)
			{
				w = 50
			};
			selectAccountToLogin = new Command(Strings.select, ActionListener.gI(), (int)CommandType.SelectAccountToLogin, null)
			{
				w = 50
			};
			finishInputAccount = new Command(Strings.save, ActionListener.gI(), (int)CommandType.FinishInputAccount, null);
			editCustomServer = new Command(Strings.inGameAccountManagerEditServer, ActionListener.gI(), (int)CommandType.EditCustomServer, null);
			finishEditCustomServer = new Command(Strings.save, ActionListener.gI(), (int)CommandType.FinishEditCustomServer, null);
			cancelEditCustomServer = new Command(mResources.CANCEL, ActionListener.gI(), (int)CommandType.CancelEditCustomServer, null);
			cancelInputAccount = new Command(mResources.CANCEL, ActionListener.gI(), (int)CommandType.CloseInputAccount, null);
			selectServer = new ComboBox(mResources.server, serverNames.ToList().Append(Strings.custom).ToList());
			finishImportAccounts = new Command(Strings.import, ActionListener.gI(), (int)CommandType.FinishImportAccounts, null);
			cancelImportAccounts = new Command(mResources.CANCEL, ActionListener.gI(), (int)CommandType.CloseImportAccounts, null);

			tfUser = new TField
			{
				name = (mResources.language != 2 ? mResources.phone + "/" : string.Empty) + mResources.email,
				width = INPUT_ACCOUNT_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};
			tfPass = new TField
			{
				name = mResources.password,
				width = INPUT_ACCOUNT_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};
			tfUser.setIputType(TField.INPUT_TYPE_ANY);
			tfPass.setIputType(TField.INPUT_TYPE_PASSWORD);

			tfCustomServerName = new TField
			{
				name = Strings.inGameAccountManagerServerName,
				width = SELECT_SERVER_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};
			tfCustomServerAddress = new TField
			{
				name = Strings.inGameAccountManagerServerAddress,
				width = SELECT_SERVER_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};
			tfCustomServerPort = new TField
			{
				name = Strings.inGameAccountManagerServerPort,
				width = SELECT_SERVER_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};
			tfInputDataImportAccounts = new TField
			{
				name = Strings.inGameAccountManagerImportAccountsInputData,
				width = IMPORT_ACCOUNTS_WIDTH - 10,
				height = ITEM_HEIGHT + 2
				// multiline = true,
			};
			tfInputRegexMatchLinesImportAccounts = new TField
			{
				name = Strings.inGameAccountManagerRegexMatchLines,
				width = IMPORT_ACCOUNTS_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};
			tfInputRegexMatchAccountInfoImportAccounts = new TField
			{
				name = Strings.inGameAccountManagerRegexMatchAccountInfo,
				width = IMPORT_ACCOUNTS_WIDTH - 10,
				height = ITEM_HEIGHT + 2
			};

			tfCustomServerName.setIputType(TField.INPUT_TYPE_ANY);
			tfCustomServerAddress.setIputType(TField.INPUT_TYPE_ANY);
			tfCustomServerPort.setIputType(TField.INPUT_TYPE_NUMERIC);
			tfInputDataImportAccounts.setIputType(TField.INPUT_TYPE_ANY);
			tfInputRegexMatchLinesImportAccounts.setIputType(TField.INPUT_TYPE_ANY);
			tfInputRegexMatchAccountInfoImportAccounts.setIputType(TField.INPUT_TYPE_ANY);

			tfInputDataImportAccounts.setMaxTextLenght(int.MaxValue);

			UpdateSizeAndPos();
			base.switchToMe();
		}

		public override void update()
		{
			GameScr.cmx++;
			if (GameScr.cmx > GameCanvas.w * 3 + 100)
				GameScr.cmx = 100;
			if (isImportingAccounts && !selectServer.IsShowingListItems)
			{
				if (!InfoDlg.isLock)
				{
					tfInputDataImportAccounts.update();
					tfInputRegexMatchLinesImportAccounts.update();
					tfInputRegexMatchAccountInfoImportAccounts.update();
				}
			}
			else if (isEditingCustomServer)
			{
				tfCustomServerName.update();
				tfCustomServerAddress.update();
				tfCustomServerPort.update();
			}
			else if (isAddingAccount || isEditingAccount)
			{
				if (!selectServer.IsShowingListItems)
				{
					tfUser.update();
					tfPass.update();
				}
			}
			scrollableMenuAccounts.Update();
			selectServer.Update();
			if (isImportingAccounts && selectServer.SelectedIndex == selectServer.Items.Count - 1)
				selectServer.SelectedIndex = 0;
			GetAccountsArea(out _, out _, out int width, out int height, true);
			scrollableMenuAccounts.Width = width;
			scrollableMenuAccounts.Height = height;
			if (scrollableMenuAccounts.CurrentItemIndex != -1)
			{
				int minX = GameCanvas.w - WIDTH_ACCOUNT_INFO - 40;
				if (currentAccountInfoX > minX)
				{
					if (currentAccountInfoX - 50 < minX)
						currentAccountInfoX = minX;
					else
						currentAccountInfoX -= 50;
				}
			}
			else
			{
				if (currentAccountInfoX < GameCanvas.w)
				{
					if (currentAccountInfoX + 50 > GameCanvas.w)
						currentAccountInfoX = GameCanvas.w;
					else
						currentAccountInfoX += 50;
				}
			}
			UpdateButtonsPos();
			base.update();
		}

		public override void updateKey()
		{
			if (isImportingAccounts)
				UpdateKeyImportAccounts();
			else if (isEditingCustomServer)
				UpdateKeyEditCustomServer();
			else if (isAddingAccount || isEditingAccount)
				UpdateKeyInputAccount();
			else
				UpdateKeyMain();
			base.updateKey();
			GameCanvas.clearKeyPressed();
		}

		public override void keyPress(int keyCode)
		{
			if (isImportingAccounts)
			{
				if (tfInputDataImportAccounts.isFocus)
					tfInputDataImportAccounts.keyPressed(keyCode);
				else if (tfInputRegexMatchLinesImportAccounts.isFocus)
					tfInputRegexMatchLinesImportAccounts.keyPressed(keyCode);
				else if (tfInputRegexMatchAccountInfoImportAccounts.isFocus)
					tfInputRegexMatchAccountInfoImportAccounts.keyPressed(keyCode);
			}
			else if (isEditingCustomServer)
			{
				if (tfCustomServerName.isFocus)
					tfCustomServerName.keyPressed(keyCode);
				else if (tfCustomServerAddress.isFocus)
					tfCustomServerAddress.keyPressed(keyCode);
				else if (tfCustomServerPort.isFocus)
					tfCustomServerPort.keyPressed(keyCode);
			}
			else if (isAddingAccount || isEditingAccount)
			{
				if (tfUser.isFocus)
					tfUser.keyPressed(keyCode);
				else if (tfPass.isFocus)
					tfPass.keyPressed(keyCode);
			}
			base.keyPress(keyCode);
		}

		static void ApplyLoadedAccountServerSelection()
		{
			if (InGameAccountDAO.SelectedAccountIndex != -1 && InGameAccountDAO.SelectedAccount != null && InGameAccountDAO.SelectedAccount.Server != null && !InGameAccountDAO.SelectedAccount.Server.IsCustomIP())
			{
				int maxIdx = ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0
					? ServerListScreen.nameServer.Length - 1
					: 0;
				int clampedIdx = Mathf.Clamp(InGameAccountDAO.SelectedAccount.Server.index, 0, maxIdx);
				ServerListScreen.ipSelect = clampedIdx;
				Rms.saveRMSInt("svselect", clampedIdx);
			}
			if (InGameAccountDAO.SelectedAccountIndex != -1 && InGameAccountDAO.SelectedAccount != null)
				SelectedServer = InGameAccountDAO.SelectedAccount.Server;
		}

		internal static void AddUserAoToAccountManager()
		{
			InGameAccountDAO.AddUserAoToAccountManager();
		}

		internal static void ResetSelectedAccountIndex()
		{
			InGameAccountDAO.ResetSelectedAccountIndex();
		}

		static void PaintListAccounts(mGraphics g)
		{
			GetAccountsArea(out int x, out int y, out int width, out int height, false);
			PopUp.paintPopUp(g, x - 10, y - 10, width + 20, TITLE_HEIGHT + 50, 0, true);
			mFont.tahoma_7b_dark.drawString(g, Strings.accounts, x + 10, y, 0);
			addAccount.paint(g);
			if (scrollableMenuAccounts.CurrentItemIndex > -1)
			{
				moveAccountDown.paint(g);
				moveAccountUp.paint(g);
			}
			PopUp.paintPopUp(g, x - 10, y + TITLE_HEIGHT - 10, width + 20, height - TITLE_HEIGHT + 20, 0, true);
			GetAccountsArea(out _, out y, out _, out height, true);
			if (accounts.Count > height / ACCOUNT_HEIGHT)
			{
				g.setColor(0x998978);
				int scrollThumbHeight = height * height / ACCOUNT_HEIGHT / accounts.Count;
				int scrollThumbY = y + Mathf.Clamp(height * scrollableMenuAccounts.CurrentOffset / ACCOUNT_HEIGHT / accounts.Count, 0, height - scrollThumbHeight);
				g.fillRect(x + width + 3, scrollThumbY, 4, scrollThumbHeight);
			}
			g.setColor(0xD3A46F);
			g.fillRect(x, y, width, height);
			g.setColor(Color.black);
			g.drawRect(x - 1, y - 1, width + 1, height + 1);
			g.setClip(x, y, width, height);
			scrollableMenuAccounts.Paint(g);
			g.reset();
			closeAccountManager.paint(g);
		}

		static string GetServerDisplayName(Server server)
		{
			if (server == null)
				return ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0
					? ServerListScreen.nameServer[0]
					: GameEvents.TEAMOBI2026_SERVER_NAME;
			if (server.IsCustomIP())
				return server.name ?? string.Empty;
			return ServerListScreen.nameServer != null && ServerListScreen.nameServer.Length > 0
				? ServerListScreen.nameServer[Mathf.Clamp(server.index, 0, ServerListScreen.nameServer.Length - 1)]
				: GameEvents.TEAMOBI2026_SERVER_NAME;
		}

		static void PaintCurrentAccountInfo(mGraphics g)
		{
			if (currentAccountInfoX == GameCanvas.w)
				return;
			int y = Y_LIST_ACCOUNTS;
			int height = GameCanvas.h - y * 2;
			int currentIndex = scrollableMenuAccounts.CurrentItemIndex;
			Account account = null;
			if (currentIndex >= 0 && currentIndex < accounts.Count)
				account = accounts[currentIndex];
			PopUp.paintPopUp(g, currentAccountInfoX - 10, y - 10, WIDTH_ACCOUNT_INFO + 20, height + 20, account == null ? 0 : (int)account.Type, true);
			height -= editAccount.h;
			PopUp.paintPopUp(g, currentAccountInfoX, y, WIDTH_ACCOUNT_INFO, height, 0xffffff, false);
			editAccount.paint(g);
			deleteAccount.paint(g);
			selectAccountToLogin.paint(g);
			if (account == null)
				return;
			g.setClip(currentAccountInfoX, y, WIDTH_ACCOUNT_INFO, height);
			int offsetXAccountMainInfo = currentAccountInfoX;
			Texture2D icon = null;
			if (account.Info != null)
			{
				EnsureSmallImage(account.Info.Icon);
				icon = icons[account.Info.Icon];
			}
			if (icon != null)
			{
				float iconWidth = ACCOUNT_HEIGHT * icon.width / (float)icon.height;
				offsetXAccountMainInfo += 65;
				DrawTexture(currentAccountInfoX + 10, y + 10, iconWidth, ACCOUNT_HEIGHT, icon, ScaleMode.ScaleToFit);
				if (account.PetInfo != null)
				{
					EnsureSmallImage(account.PetInfo.Icon);
					Texture2D petIcon = icons[account.PetInfo.Icon];
					if (petIcon != null)
					{
						float petIconWidth = ACCOUNT_HEIGHT / 2f * petIcon.width / petIcon.height;
						offsetXAccountMainInfo -= ACCOUNT_HEIGHT / 2;
						offsetXAccountMainInfo += 30;
						DrawTexture(currentAccountInfoX + 47, y + 10 + ACCOUNT_HEIGHT / 2f, petIconWidth, ACCOUNT_HEIGHT / 2f, petIcon, ScaleMode.ScaleToFit);
					}
				}
			}
			else
				offsetXAccountMainInfo += 20;
			mFont mfont = mFont.tahoma_7b_dark;
			if (account.Type == AccountType.Unregistered)
				mfont = mFont.tahoma_7b_green2;
			string displayName = account.Info != null && !string.IsNullOrEmpty(account.Info.Name) ? account.Info.Name : (account.Username ?? string.Empty);
			mfont.drawString(g, displayName, offsetXAccountMainInfo, y + 5, 0);
			string serverName = GetServerDisplayName(account.Server);
			string lastTimeLogin = (account.LastTimeLogin != DateTime.MinValue ? Strings.lastLogin + ": " : "") + account.GetLastTimeLogin();
			if (mFont.tahoma_7_greySmall.getWidth(lastTimeLogin) + offsetXAccountMainInfo - currentAccountInfoX > WIDTH_ACCOUNT_INFO)
			{
				//g.setColor(new Color(0, 0, 0, .3f));
				//g.fillRect(offsetXAccountMainInfo - 1, y + 19, 75, 1);
				mFont.tahoma_7_greySmall.drawString(g, mResources.server + ' ' + serverName, offsetXAccountMainInfo, y + 15, 0);
				mFont.tahoma_7_greySmall.drawString(g, Strings.lastLogin + ":", offsetXAccountMainInfo, y + 25, 0);
				mFont.tahoma_7_greySmall.drawString(g, account.GetLastTimeLogin(), offsetXAccountMainInfo, y + 35, 0);
			}
			else
			{
				//g.setColor(new Color(0, 0, 0, .3f));
				//g.fillRect(offsetXAccountMainInfo - 1, y + 24, 75, 1);
				mFont.tahoma_7_greySmall.drawString(g, mResources.server + ' ' + serverName, offsetXAccountMainInfo, y + 15, 0);
				mFont.tahoma_7_greySmall.drawString(g, lastTimeLogin, offsetXAccountMainInfo, y + 25, 0);
			}

			g.setColor(new Color(0, 0, 0, .3f));
			g.fillRect(currentAccountInfoX + 20, y + 50, WIDTH_ACCOUNT_INFO - 40, 1);
			mFont.tahoma_7b_dark.drawString(g, Strings.info, currentAccountInfoX + WIDTH_ACCOUNT_INFO / 2 - mFont.tahoma_7b_dark.getWidth(Strings.info) / 2, y + 55, mFont.LEFT);
			int offsetY = 65;
			int offsetX = currentAccountInfoX;
			string charIdText = account.Info != null ? account.Info.CharID.ToString() : "-";
			string maxHpText = account.Info != null ? Utils.FormatWithSIPrefix(account.Info.MaxHP) : "-";
			string maxMpText = account.Info != null ? Utils.FormatWithSIPrefix(account.Info.MaxMP) : "-";
			string expText = account.Info != null ? Utils.FormatWithSIPrefix(account.Info.EXP) : "-";
			string genderText = account.Info != null ? account.Info.GetGender() : "-";
			if (account.PetInfo != null)
			{
				offsetX += 10;
				int center = currentAccountInfoX + WIDTH_ACCOUNT_INFO / 2;
				mFont.tahoma_7b_focus.drawString(g, Strings.master, currentAccountInfoX + WIDTH_ACCOUNT_INFO / 4 - mFont.tahoma_7b_focus.getWidth(Strings.master) / 2, y + offsetY, 0);
				mFont.tahoma_7b_focus.drawString(g, mResources.pet, currentAccountInfoX + WIDTH_ACCOUNT_INFO * 3 / 4 - mFont.tahoma_7b_focus.getWidth(mResources.pet) / 2, y + offsetY, 0);
				offsetY += 5;
				g.fillRect(center, y + offsetY, 1, 58);
				offsetY += 7;
				mFont.tahoma_7_greySmall.drawString(g, "CharID: " + charIdText, currentAccountInfoX + 10, y + offsetY, 0);
				mFont.tahoma_7_greySmall.drawString(g, Strings.name + ": " + account.PetInfo.Name, currentAccountInfoX + 10 + WIDTH_ACCOUNT_INFO / 2, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, mResources.HP + ": " + maxHpText, currentAccountInfoX + 10, y + offsetY, 0);
				mFont.tahoma_7_greySmall.drawString(g, mResources.HP + ": " + Utils.FormatWithSIPrefix(account.PetInfo.MaxHP), currentAccountInfoX + 10 + WIDTH_ACCOUNT_INFO / 2, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, mResources.KI + ": " + maxMpText, currentAccountInfoX + 10, y + offsetY, 0);
				mFont.tahoma_7_greySmall.drawString(g, mResources.KI + ": " + Utils.FormatWithSIPrefix(account.PetInfo.MaxMP), currentAccountInfoX + 10 + WIDTH_ACCOUNT_INFO / 2, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, mResources.power + ": " + expText, currentAccountInfoX + 10, y + offsetY, 0);
				mFont.tahoma_7_greySmall.drawString(g, mResources.power + ": " + Utils.FormatWithSIPrefix(account.PetInfo.EXP), currentAccountInfoX + 10 + WIDTH_ACCOUNT_INFO / 2, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, Strings.gender + ": " + genderText, currentAccountInfoX + 10, y + offsetY, 0);
				mFont.tahoma_7_greySmall.drawString(g, Strings.gender + ": " + account.PetInfo.GetGender(), currentAccountInfoX + 10 + WIDTH_ACCOUNT_INFO / 2, y + offsetY, 0);
				offsetY += 10;
			}
			else
			{
				offsetY += 2;
				offsetX += 20;
				mFont.tahoma_7_greySmall.drawString(g, "CharID: " + charIdText, currentAccountInfoX + 20, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, mResources.HP + ": " + maxHpText, currentAccountInfoX + 20, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, mResources.KI + ": " + maxMpText, currentAccountInfoX + 20, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, mResources.power + ": " + expText, currentAccountInfoX + 20, y + offsetY, 0);
				offsetY += 10;
				mFont.tahoma_7_greySmall.drawString(g, Strings.gender + ": " + genderText, currentAccountInfoX + 20, y + offsetY, 0);
				offsetY += 10;
			}
			offsetY += 5;

			g.drawImage(Panel.imgXu, offsetX, y + offsetY);
			offsetX += Panel.imgXu.getWidth() + 5;
			mFont.tahoma_7_greySmall.drawString(g, Utils.FormatWithSIPrefix(account.Gold), offsetX, y + offsetY, 0);
			offsetX += mFont.tahoma_7_greySmall.getWidth(Utils.FormatWithSIPrefix(account.Gold)) + 5;
			g.drawImage(Panel.imgLuong, offsetX, y + offsetY);
			offsetX += Panel.imgLuong.getWidth() + 5;
			mFont.tahoma_7_greySmall.drawString(g, Utils.FormatWithSIPrefix(account.Gem), offsetX, y + offsetY, 0);
			offsetX += mFont.tahoma_7_greySmall.getWidth(Utils.FormatWithSIPrefix(account.Gem)) + 5;
			g.drawImage(Panel.imgLuongKhoa, offsetX, y + offsetY);
			offsetX += Panel.imgLuongKhoa.getWidth() + 5;
			mFont.tahoma_7_greySmall.drawString(g, Utils.FormatWithSIPrefix(account.Ruby), offsetX, y + offsetY, 0);
			offsetY += 15;
			g.fillRect(currentAccountInfoX + 20, y + offsetY, WIDTH_ACCOUNT_INFO - 40, 1);

		}

		static void PaintInputAccount(mGraphics g)
		{
			g.setColor(new Color(0, 0, 0, .5f));
			g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
			GetInputAccountArea(out int x, out int y, out int width, out int height, false);
			PopUp.paintPopUp(g, x, y, width, TITLE_HEIGHT + 10, -1, true);
			if (isAddingAccount)
				mFont.tahoma_7b_dark.drawString(g, Strings.inGameAccountManagerAddAccount, x + width / 2, y + 10, mFont.CENTER);
			else if (isEditingAccount)
				mFont.tahoma_7b_dark.drawString(g, Strings.inGameAccountManagerEditAccount, x + width / 2, y + 10, mFont.CENTER);
			closeInputAccount.paint(g);
			toggleHidePassword.paint(g);
			importAccounts.paint(g);
			GetInputAccountArea(out _, out y, out _, out height, true);
			PopUp.paintPopUp(g, x, y, width, height, -1, true);
			tfUser.paint(g);
			tfPass.paint(g);
			g.reset();
			bool shouldEditServer = !selectServer.IsShowingListItems && selectServer.SelectedIndex == selectServer.Items.Count - 1;
			if (shouldEditServer)
				editCustomServer.paint(g);
			else
				finishInputAccount.paint(g);
			cancelInputAccount.paint(g);
			selectServer.Paint(g);
			g.reset();
		}

		static void PaintEditCustomServer(mGraphics g)
		{
			g.setColor(new Color(0, 0, 0, .75f));
			g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
			GetInputAccountArea(out int x, out int y, out int width, out int height, false);
			PopUp.paintPopUp(g, x, y, width, height, -1, true);
			mFont.tahoma_7b_dark.drawString(g, Strings.inGameAccountManagerEditServer, x + width / 2, y + 10, mFont.CENTER);
			GetInputAccountArea(out _, out y, out _, out height, true);
			PopUp.paintPopUp(g, x, y, width, height, -1, true);
			tfCustomServerName.paint(g);
			tfCustomServerAddress.paint(g);
			tfCustomServerPort.paint(g);
			g.reset();
			finishEditCustomServer.paint(g);
			cancelEditCustomServer.paint(g);
		}

		static void PaintImportAccounts(mGraphics g)
		{
			g.setColor(new Color(0, 0, 0, .75f));
			g.fillRect(0, 0, GameCanvas.w, GameCanvas.h);
			GetImportAccountsArea(out int x, out int y, out int width, out int height, false);
			PopUp.paintPopUp(g, x, y, width, height, -1, true);
			mFont.tahoma_7b_dark.drawString(g, Strings.inGameAccountManagerImportAccounts, x + width / 2, y + 10, mFont.CENTER);
			helpImportAccounts.paint(g);
			closeImportAccounts.paint(g);
			GetImportAccountsArea(out _, out y, out _, out height, true);
			PopUp.paintPopUp(g, x, y, width, height, -1, true);
			tfInputDataImportAccounts.paint(g);
			tfInputRegexMatchLinesImportAccounts.paint(g);
			tfInputRegexMatchAccountInfoImportAccounts.paint(g);
			g.reset();
			finishImportAccounts.paint(g);
			cancelImportAccounts.paint(g);
			g.reset();
			selectServer.Paint(g);
		}

		static void UpdateKeyInputAccount()
		{
			selectServer.UpdateKey();
			if (selectServer.IsShowingListItems)
				return;
			if (closeInputAccount.isPointerPressInside())
				closeInputAccount.performAction();
			if (toggleHidePassword.isPointerPressInside())
				toggleHidePassword.performAction();
			if (importAccounts.isPointerPressInside())
				importAccounts.performAction();
			bool shouldEditServer = !selectServer.IsShowingListItems && selectServer.SelectedIndex == selectServer.Items.Count - 1;
			if (shouldEditServer)
			{
				if (editCustomServer.isPointerPressInside())
					editCustomServer.performAction();
			}
			else
			{
				if (finishInputAccount.isPointerPressInside())
					finishInputAccount.performAction();
			}
			if (cancelInputAccount.isPointerPressInside())
				cancelInputAccount.performAction();
			if (GameCanvas.keyPressed[16])
			{
				GameCanvas.clearKeyPressed();
				if (tfUser.isFocus)
				{
					tfUser.isFocus = false;
					tfPass.isFocus = true;
				}
				else if (tfPass.isFocus)
				{
					tfPass.isFocus = false;
					selectServer.IsFocus = true;
				}
				else if (selectServer.IsFocus)
					selectServer.IsFocus = false;
				else
					tfUser.isFocus = true;
			}
		}

		static void UpdateKeyEditCustomServer()
		{
			if (finishEditCustomServer.isPointerPressInside())
				finishEditCustomServer.performAction();
			if (cancelEditCustomServer.isPointerPressInside())
				cancelEditCustomServer.performAction();
			if (GameCanvas.keyPressed[16])
			{
				GameCanvas.clearKeyPressed();
				if (tfCustomServerName.isFocus)
				{
					tfCustomServerName.isFocus = false;
					tfCustomServerAddress.isFocus = true;
				}
				else if (tfCustomServerAddress.isFocus)
				{
					tfCustomServerAddress.isFocus = false;
					tfCustomServerPort.isFocus = true;
				}
				else if (tfCustomServerPort.isFocus)
					tfCustomServerPort.isFocus = false;
				else
					tfCustomServerName.isFocus = true;
			}
		}

		static void UpdateKeyImportAccounts()
		{
			if (InfoDlg.isLock)
				return;
			selectServer.UpdateKey();
			if (selectServer.IsShowingListItems)
				return;
			if (finishImportAccounts.isPointerPressInside())
				finishImportAccounts.performAction();
			if (cancelImportAccounts.isPointerPressInside())
				cancelImportAccounts.performAction();
			if (closeImportAccounts.isPointerPressInside())
				closeImportAccounts.performAction();
			if (helpImportAccounts.isPointerPressInside())
				helpImportAccounts.performAction();
			if (GameCanvas.keyPressed[16])
			{
				GameCanvas.clearKeyPressed();
				if (tfInputDataImportAccounts.isFocus)
				{
					tfInputDataImportAccounts.isFocus = false;
					tfInputRegexMatchLinesImportAccounts.isFocus = true;
				}
				else if (tfInputRegexMatchLinesImportAccounts.isFocus)
				{
					tfInputRegexMatchLinesImportAccounts.isFocus = false;
					tfInputRegexMatchAccountInfoImportAccounts.isFocus = true;
				}
				else if (tfInputRegexMatchAccountInfoImportAccounts.isFocus)
					tfInputRegexMatchAccountInfoImportAccounts.isFocus = false;
				else
					tfInputDataImportAccounts.isFocus = true;
			}
		}

		static void UpdateKeyMain()
		{
			if (GameCanvas.keyPressed[13] && scrollableMenuAccounts.CurrentItemIndex == -1)
				closeAccountManager.performAction();
			GetAccountsArea(out int x, out int y, out int width, out int height, true);
			scrollableMenuAccounts.UpdateKey();
			if (closeAccountManager.isPointerPressInside())
				closeAccountManager.performAction();
			if (editAccount.isPointerPressInside())
				editAccount.performAction();
			if (deleteAccount.isPointerPressInside())
				deleteAccount.performAction();
			if (selectAccountToLogin.isPointerPressInside())
				selectAccountToLogin.performAction();
			if (addAccount.isPointerPressInside())
				addAccount.performAction();
			if (scrollableMenuAccounts.CurrentItemIndex > -1)
			{
				if (moveAccountUp.isPointerPressInside())
					moveAccountUp.performAction();
				if (moveAccountDown.isPointerPressInside())
					moveAccountDown.performAction();
			}

			if (GameCanvas.keyPressed[!Main.isPC ? 5 : 25])
			{
				if (scrollableMenuAccounts.CurrentItemIndex != -1)
					selectAccountToLogin.performAction();
			}
		}

		internal static void UpdateSizeAndPos()
		{
			if (!isSwitchedToMe)
				return;
			if (scrollableMenuAccounts.CurrentItemIndex != -1)
				currentAccountInfoX = GameCanvas.w - WIDTH_ACCOUNT_INFO - 40;
			else
				currentAccountInfoX = GameCanvas.w;
			UpdateButtonsPos();

			GetInputAccountArea(out int x, out int y, out int width, out int height, true);
			tfUser.x = tfPass.x = selectServer.X = tfCustomServerName.x = tfCustomServerAddress.x = tfCustomServerPort.x = x + 10;
			tfUser.y = tfCustomServerName.y = y + 15;
			tfPass.y = tfCustomServerAddress.y = tfUser.y + ITEM_HEIGHT + 15;
			selectServer.Y = tfCustomServerPort.y = tfPass.y + ITEM_HEIGHT + 15;
			tfUser.width = tfPass.width = selectServer.Width = tfCustomServerName.width = tfCustomServerAddress.width = tfCustomServerPort.width = width - 20;

			GetImportAccountsArea(out x, out y, out width, out height, true);
			tfInputDataImportAccounts.x = tfInputRegexMatchLinesImportAccounts.x = tfInputRegexMatchAccountInfoImportAccounts.x = x + 10;
			tfInputDataImportAccounts.y = y + 15;
			tfInputRegexMatchLinesImportAccounts.y = tfInputDataImportAccounts.y + ITEM_HEIGHT + 15;
			tfInputRegexMatchAccountInfoImportAccounts.y = tfInputRegexMatchLinesImportAccounts.y + ITEM_HEIGHT + 15;
			tfInputDataImportAccounts.width = tfInputRegexMatchLinesImportAccounts.width = tfInputRegexMatchAccountInfoImportAccounts.width = width - 20;
			if (isImportingAccounts)
			{
				selectServer.X = tfInputDataImportAccounts.x;
				selectServer.Y = tfInputRegexMatchAccountInfoImportAccounts.y + ITEM_HEIGHT + 15;
				selectServer.Width = tfInputDataImportAccounts.width;
			}

			GetAccountsArea(out _, out _, out width, out height, true);
			scrollableMenuAccounts.Width = width;
			scrollableMenuAccounts.Height = height;
		}

		static void UpdateButtonsPos()
		{
			closeAccountManager.x = GameCanvas.w - 63;
			closeAccountManager.y = addAccount.y = moveAccountDown.y = moveAccountUp.y = 25;
			int currentAccountInfoWidth = GameCanvas.w - currentAccountInfoX;
			if (currentAccountInfoWidth > 0)
				closeAccountManager.x -= currentAccountInfoWidth - 20;
			addAccount.x = closeAccountManager.x - addAccount.w - 5;
			moveAccountDown.x = addAccount.x - moveAccountUp.w - 5;
			moveAccountUp.x = moveAccountDown.x - moveAccountUp.w - 5;

			editAccount.y = deleteAccount.y = selectAccountToLogin.y = GameCanvas.h - 50;
			editAccount.x = currentAccountInfoX;
			deleteAccount.x = currentAccountInfoX + (WIDTH_ACCOUNT_INFO - deleteAccount.w) / 2;
			selectAccountToLogin.x = currentAccountInfoX + WIDTH_ACCOUNT_INFO - selectAccountToLogin.w;

			GetInputAccountArea(out int x, out int y, out int width, out int height, false);
			closeInputAccount.y = toggleHidePassword.y = importAccounts.y = y + 5;
			closeInputAccount.x = x + width - closeInputAccount.img.getWidth() - 5;
			toggleHidePassword.x = closeInputAccount.x - toggleHidePassword.img.getWidth() - 5;
			importAccounts.x = x + 5;
			finishInputAccount.x = editCustomServer.x = finishEditCustomServer.x = x + 10;
			cancelInputAccount.x = cancelEditCustomServer.x = x + width - cancelInputAccount.w - 10;
			finishInputAccount.y = cancelInputAccount.y = editCustomServer.y = finishEditCustomServer.y = cancelEditCustomServer.y = y + height - finishInputAccount.h - 10;
			finishInputAccount.w = editCustomServer.w = cancelInputAccount.w = finishEditCustomServer.w = cancelEditCustomServer.w = (width - 30) / 2;

			GetImportAccountsArea(out x, out y, out width, out height, false);
			closeImportAccounts.y = helpImportAccounts.y = y + 5;
			closeImportAccounts.x = x + width - closeImportAccounts.img.getWidth() - 5;
			helpImportAccounts.x = x + 5;
			finishImportAccounts.x = x + 10;
			cancelImportAccounts.x = x + width - cancelImportAccounts.w - 10;
			finishImportAccounts.y = cancelImportAccounts.y = y + height - finishImportAccounts.h - 10;
			finishImportAccounts.w = cancelImportAccounts.w = (width - 30) / 2;
		}

		internal static void OnStart()
		{
			closeAccountManager.img = closeInputAccount.img = closeImportAccounts.img = GameCanvas.loadImage("/mainImage/myTexture2dbtX.png");
			add = CustomGraphics.Resize(add, add.width * mGraphics.zoomLevel / 4, add.height * mGraphics.zoomLevel / 4);
			hide = CustomGraphics.Resize(hide, hide.width * mGraphics.zoomLevel / 4, hide.height * mGraphics.zoomLevel / 4);
			show = CustomGraphics.Resize(show, show.width * mGraphics.zoomLevel / 4, show.height * mGraphics.zoomLevel / 4);
			up = CustomGraphics.Resize(up, up.width * mGraphics.zoomLevel / 4, up.height * mGraphics.zoomLevel / 4);
			down = CustomGraphics.Resize(down, down.width * mGraphics.zoomLevel / 4, down.height * mGraphics.zoomLevel / 4);
			addMultiple = CustomGraphics.Resize(addMultiple, addMultiple.width * mGraphics.zoomLevel / 4, addMultiple.height * mGraphics.zoomLevel / 4);
			help = CustomGraphics.Resize(help, help.width * mGraphics.zoomLevel / 4, help.height * mGraphics.zoomLevel / 4);
			InGameAccountDAO.Load();
			ApplyLoadedAccountServerSelection();
			scrollableMenuAccounts = new ScrollableMenuItems<Account>(accounts)
			{
				PaintItemAction = PaintAccount,
				CurrentItemIndex = -1,
				ItemHeight = ACCOUNT_HEIGHT,
				StepScroll = DEFAULT_STEP_SCROLL,
				AllowSelectNone = true
			};
			addAccount = new Command("", ActionListener.gI(), (int)CommandType.AddAccount, null)
			{
				img = Image.createImage(add.width, add.height),
				imgFocus = new Image(),
				w = add.width / mGraphics.zoomLevel,
				h = add.height / mGraphics.zoomLevel
			};
			addAccount.img.texture = add;
			toggleHidePassword = new Command("", ActionListener.gI(), (int)CommandType.ToggleHidePassword, null)
			{
				img = Image.createImage(show.width, show.height),
				imgFocus = new Image(),
				w = show.width / mGraphics.zoomLevel,
				h = show.height / mGraphics.zoomLevel
			};
			toggleHidePassword.img.texture = show;
			importAccounts = new Command("", ActionListener.gI(), (int)CommandType.ImportAccounts, null)
			{
				img = Image.createImage(addMultiple.width, addMultiple.height),
				imgFocus = new Image(),
				w = addMultiple.width / mGraphics.zoomLevel,
				h = addMultiple.height / mGraphics.zoomLevel
			};
			importAccounts.img.texture = addMultiple;
			helpImportAccounts = new Command("", ActionListener.gI(), (int)CommandType.HelpImportAccounts, null)
			{
				img = Image.createImage(help.width, help.height),
				imgFocus = new Image(),
				w = help.width / mGraphics.zoomLevel,
				h = help.height / mGraphics.zoomLevel
			};
			helpImportAccounts.img.texture = help;
			moveAccountUp = new Command("", ActionListener.gI(), (int)CommandType.MoveAccountUp, null)
			{
				img = Image.createImage(up.width, up.height),
				imgFocus = new Image(),
				w = up.width / mGraphics.zoomLevel,
				h = up.height / mGraphics.zoomLevel
			};
			moveAccountUp.img.texture = up;
			moveAccountDown = new Command("", ActionListener.gI(), (int)CommandType.MoveAccountDown, null)
			{
				img = Image.createImage(down.width, down.height),
				imgFocus = new Image(),
				w = down.width / mGraphics.zoomLevel,
				h = down.height / mGraphics.zoomLevel
			};
			moveAccountDown.img.texture = down;
		}

		internal static void OnCloseAndPause()
		{
			Account live = InGameAccountDAO.SelectedAccount;
			if (live != null)
			{
				InGameAccountDAO.SaveMergedLoggedInAccount(live);
			}
			else
			{
				InGameAccountDAO.Save();
			}
		}

		static void PaintAccount(mGraphics g, int i, int x, int y, int width, int height)
		{
			if (i < 0 || i >= accounts.Count)
				return;
			Account account = accounts[i];
			if (account == null)
				return;
			Texture2D icon = null;
			if (account.Info != null)
			{
				EnsureSmallImage(account.Info.Icon);
				icon = icons[account.Info.Icon];
			}
			if (icon != null)
			{
				float iconWidth = (height - 2) * icon.width / (float)icon.height;
				BeginGroup(scrollableMenuAccounts.X, scrollableMenuAccounts.Y, scrollableMenuAccounts.Width, scrollableMenuAccounts.Height);
				DrawTexture(10, i * height + 2 - scrollableMenuAccounts.CurrentOffset, iconWidth, height - 2, icon, ScaleMode.ScaleToFit);
				GUI.EndGroup();
			}
			if (account.PetInfo != null)
			{
				EnsureSmallImage(account.PetInfo.Icon);
				Texture2D petIcon = icons[account.PetInfo.Icon];
				if (petIcon != null)
				{
					float iconWidth = height / 2f * petIcon.width / petIcon.height;
					BeginGroup(scrollableMenuAccounts.X, scrollableMenuAccounts.Y, scrollableMenuAccounts.Width, scrollableMenuAccounts.Height);
					DrawTexture(45, i * height - scrollableMenuAccounts.CurrentOffset + height / 2f, iconWidth, height / 2f, petIcon, ScaleMode.ScaleToFit);
					GUI.EndGroup();
				}
			}
			Texture2D overlay = account.Info != null ? GetOverlay(account.Info.Gender) : null;
			if (overlay != null)
			{
				float overlayWidth = (height - 1) * overlay.width / (float)overlay.height;
				BeginGroup(scrollableMenuAccounts.X, scrollableMenuAccounts.Y, scrollableMenuAccounts.Width, scrollableMenuAccounts.Height);
				DrawTexture(width - overlayWidth, i * height - scrollableMenuAccounts.CurrentOffset + 1, overlayWidth, height - 1, overlay, ScaleMode.ScaleToFit);
				GUI.EndGroup();
			}
			mFont mfont = mFont.tahoma_7b_dark;
			if (account.Type == AccountType.Unregistered)
				mfont = mFont.tahoma_7b_green2;
			string displayName = account.Info != null && !string.IsNullOrEmpty(account.Info.Name) ? account.Info.Name : (account.Username ?? string.Empty);
			mfont.drawString(g, displayName, x + 80, y + 2, 0);

			string serverName = GetServerDisplayName(account.Server);
			mFont.tahoma_7_greySmall.drawString(g, account.GetLastTimeLogin(), x + 80, y + height - 12, 0);
			mFont.tahoma_7_greySmall.drawString(g, mResources.server + ' ' + serverName, x + 80, y + height - 22, 0);
			//g.setColor(new Color(0, 0, 0, .3f));
			//g.fillRect(x + 79, y + height - 25, 75, 1);
		}

		static void EnsureSmallImage(int id)
		{
			Texture2D texture = null;
			if (!icons.ContainsKey(id) || icons[id] == null)
			{
				texture = Resources.Load($"{Main.res}/x{mGraphics.zoomLevel}/SmallImage/Small{id}") as Texture2D;
				if (texture == null)
				{
					sbyte[] data = Rms.loadRMS(mGraphics.zoomLevel + "Small" + id);
					if (data != null)
					{
						texture = new Texture2D(1, 1);
						texture.LoadImage(ArrayCast.cast(data));
					}
				}
				if (texture != null)
				{
					texture.filterMode = FilterMode.Bilinear;
					texture.wrapMode = TextureWrapMode.Clamp;
					texture.anisoLevel = 1;
				}
			}
			if (!icons.ContainsKey(id))
				icons.Add(id, texture);
			else if (icons[id] == null)
				icons[id] = texture;
		}

		static Texture2D GetOverlay(sbyte gender)
		{
			switch (gender)
			{
			case 0:
				return earthOverlay;
			case 1:
				return namekOverlay;
			case 2:
				return saiyanOverlay;
			default:
				return null;
			}
		}

		static void GetAccountsArea(out int x, out int y, out int width, out int height, bool withoutTitle)
		{
			x = X_LIST_ACCOUNTS;
			y = Y_LIST_ACCOUNTS;
			width = GameCanvas.w - x * 2;
			height = GameCanvas.h - y * 2;
			if (withoutTitle)
			{
				y += TITLE_HEIGHT;
				height -= TITLE_HEIGHT;
			}
			int currentAccountInfoWidth = GameCanvas.w - currentAccountInfoX;
			if (currentAccountInfoWidth > 0)
				width -= currentAccountInfoWidth - 20;
		}

		static void GetInputAccountArea(out int x, out int y, out int width, out int height, bool withoutTitle)
		{
			x = GameCanvas.w / 2 - INPUT_ACCOUNT_WIDTH / 2;
			y = GameCanvas.h / 2 - INPUT_ACCOUNT_HEIGHT / 2;
			width = INPUT_ACCOUNT_WIDTH;
			height = INPUT_ACCOUNT_HEIGHT;
			if (withoutTitle)
			{
				y += TITLE_HEIGHT;
				height -= TITLE_HEIGHT;
			}
		}

		static void GetImportAccountsArea(out int x, out int y, out int width, out int height, bool withoutTitle)
		{
			x = GameCanvas.w / 2 - IMPORT_ACCOUNTS_WIDTH / 2;
			y = GameCanvas.h / 2 - IMPORT_ACCOUNTS_HEIGHT / 2;
			width = IMPORT_ACCOUNTS_WIDTH;
			height = IMPORT_ACCOUNTS_HEIGHT;
			if (withoutTitle)
			{
				y += TITLE_HEIGHT;
				height -= TITLE_HEIGHT;
			}
		}

		static bool IsPointerIn(int x, int y, int w, int h)
		{
			return GameCanvas.pxMouse >= x && GameCanvas.pxMouse <= x + w && GameCanvas.pyMouse >= y && GameCanvas.pyMouse <= y + h;
		}

		static void BeginGroup(int x, int y, int w, int h)
		{
			GUI.BeginGroup(new Rect(x * mGraphics.zoomLevel, y * mGraphics.zoomLevel, w * mGraphics.zoomLevel, h * mGraphics.zoomLevel));
		}

		static void DrawTexture(float x, float y, float w, float h, Texture texture, ScaleMode scaleMode = ScaleMode.StretchToFill)
		{
			GUI.DrawTexture(new Rect(x * mGraphics.zoomLevel, y * mGraphics.zoomLevel, w * mGraphics.zoomLevel, h * mGraphics.zoomLevel), texture, scaleMode);
		}
		enum CommandType
		{
			CloseAccountManager = 1,
			AddAccount,
			EditAccount,
			ConfirmDeleteAccount,
			DeleteAccount,
			SelectAccountToLogin,
			OpenAccountManager = 7,
			FinishInputAccount,
			CloseInputAccount,
			EditCustomServer,
			FinishEditCustomServer,
			CancelEditCustomServer,
			ToggleHidePassword,
			MoveAccountUp,
			MoveAccountDown,
			ImportAccounts,
			HelpImportAccounts,
			FinishImportAccounts,
			CloseImportAccounts
		}

		internal class ActionListener : IActionListener
		{
			static ActionListener _instance;

			public void perform(int id, object obj)
			{
				switch ((CommandType)id)
				{
				case CommandType.OpenAccountManager:
					InGameAccountManager.gI().switchToMe();
					break;
				case CommandType.CloseAccountManager:
					GameCanvas.serverScreen.switchToMe();
					break;
				case CommandType.AddAccount:
					isAddingAccount = true;
					tfUser.isFocus = false;
					tfPass.isFocus = false;
					tfUser.setText("");
					tfPass.setText("");
					tfPass.setIputType(TField.INPUT_TYPE_PASSWORD);
					toggleHidePassword.img.texture = show;
					break;
				case CommandType.EditAccount:
				{
					int editIndex = scrollableMenuAccounts.CurrentItemIndex;
					if (editIndex < 0 || editIndex >= accounts.Count)
						break;
					Account editAcc = accounts[editIndex];
					if (editAcc == null)
						break;
					isEditingAccount = true;
					tfUser.isFocus = false;
					tfPass.isFocus = false;
					tfUser.setText(editAcc.Username ?? string.Empty);
					tfPass.setText(editAcc.Password ?? string.Empty);
					tfPass.setIputType(TField.INPUT_TYPE_PASSWORD);
					toggleHidePassword.img.texture = show;
					Server server = editAcc.Server;
					if (server != null && server.IsCustomIP())
					{
						customServer = server;
						selectServer.SelectedIndex = selectServer.Items.Count - 1;
					}
					else
						selectServer.SelectedIndex = Mathf.Clamp(server != null ? server.index : 0, 0, System.Math.Max(0, selectServer.Items.Count - 2));
					break;
				}
				case CommandType.ConfirmDeleteAccount:
					if (scrollableMenuAccounts.CurrentItemIndex < 0 || scrollableMenuAccounts.CurrentItemIndex >= accounts.Count)
						break;
					GameCanvas.startYesNoDlg(Strings.inGameAccountManagerConfirmDeleteAcc, new Command(mResources.YES, gI(), (int)CommandType.DeleteAccount, null), new Command(mResources.NO, 2001));
					break;
				case CommandType.DeleteAccount:
				{
					int deleteIndex = scrollableMenuAccounts.CurrentItemIndex;
					if (deleteIndex < 0 || deleteIndex >= accounts.Count)
					{
						InfoDlg.hide();
						GameCanvas.currentDialog = null;
						break;
					}
					accounts.RemoveAt(deleteIndex);
					if (accounts.Count == 0)
					{
						selectedAccountIndex = scrollableMenuAccounts.CurrentItemIndex = -1;
					}
					else
					{
						if (selectedAccountIndex == deleteIndex)
							selectedAccountIndex = System.Math.Min(deleteIndex, accounts.Count - 1);
						else if (selectedAccountIndex > deleteIndex)
							selectedAccountIndex--;
						if (scrollableMenuAccounts.CurrentItemIndex >= accounts.Count)
							scrollableMenuAccounts.CurrentItemIndex = accounts.Count - 1;
					}
					InfoDlg.hide();
					GameCanvas.currentDialog = null;
					InGameAccountDAO.Save();
					break;
				}
				case CommandType.SelectAccountToLogin:
				{
					int loginIndex = scrollableMenuAccounts.CurrentItemIndex;
					if (loginIndex < 0 || loginIndex >= accounts.Count)
						break;
					selectedAccountIndex = loginIndex;
					InGameAccountDAO.Save();
					Account selectedAcc = SelectedAccount;
					if (selectedAcc == null)
						break;
					SelectedServer = selectedAcc.Server;
					Rms.saveRMSString("acc", "acc");
					Rms.saveRMSString("pass", "pass");
					Session_ME.gI().close();
					Session_ME2.gI().close();
					GameCanvas.connect();
					GameCanvas.serverScreen.switchToMe();
					break;
				}
				case CommandType.FinishInputAccount:
				{
					if (string.IsNullOrEmpty(tfUser.getText()))
					{
						GameCanvas.startOKDlg(mResources.userBlank.TrimEnd('.') + '!');
						break;
					}
					bool isUserAo = regexMatchUserAo.IsMatch(tfUser.getText());
					if (string.IsNullOrEmpty(tfPass.getText()) && !isUserAo)
					{
						GameCanvas.startOKDlg(mResources.passwordBlank + '!');
						break;
					}
					int selectedServerIndex = selectServer.SelectedIndex;
					int customServerOptionIndex = selectServer.Items.Count - 1;
					if (selectedServerIndex < 0 || selectedServerIndex >= selectServer.Items.Count)
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerServerBlank + '!');
						break;
					}
					if (isUserAo && selectedServerIndex == customServerOptionIndex)
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerUnregisteredAccountMustBeOnTeaMobiServer + '!');
						break;
					}
					bool isCustomServer = selectedServerIndex == customServerOptionIndex;
					if (!isCustomServer && (defaultServers == null || selectedServerIndex >= defaultServers.Length))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerServerBlank + '!');
						break;
					}
					if (isAddingAccount)
					{
						Account acc = new Account
						{
							Username = tfUser.getText(),
							Password = tfPass.getText(),
							Server = isCustomServer ? customServer : defaultServers[selectedServerIndex]
						};
						if (scrollableMenuAccounts.CurrentItemIndex < 0 || scrollableMenuAccounts.CurrentItemIndex >= accounts.Count)
							accounts.Add(acc);
						else
							accounts.Insert(scrollableMenuAccounts.CurrentItemIndex + 1, acc);
					}
					else if (isEditingAccount)
					{
						int editIndex = scrollableMenuAccounts.CurrentItemIndex;
						if (editIndex < 0 || editIndex >= accounts.Count)
						{
							isAddingAccount = isEditingAccount = false;
							break;
						}
						Account acc = accounts[editIndex];
						string userName = tfUser.getText();
						string password = tfPass.getText();
						if (acc != null && acc.Username == userName && acc.Password == password && acc.Server != null)
						{
							if (!acc.Server.IsCustomIP() && !isCustomServer && acc.Server.index == selectedServerIndex)
								goto closeAndSaveData;
							if (acc.Server.IsCustomIP() && isCustomServer && customServer != null && acc.Server == customServer)
								goto closeAndSaveData;
						}
						accounts[editIndex] = new Account
						{
							Username = userName,
							Password = password,
							Server = isCustomServer ? customServer : defaultServers[selectedServerIndex]
						};
					}
					closeAndSaveData: ;
					isAddingAccount = isEditingAccount = false;
					InGameAccountDAO.Save();
					break;
				}
				case CommandType.CloseInputAccount:
					isAddingAccount = isEditingAccount = false;
					break;
				case CommandType.EditCustomServer:
				{
					int editIndex = scrollableMenuAccounts.CurrentItemIndex;
					if (isEditingAccount && editIndex >= 0 && editIndex < accounts.Count &&
					    accounts[editIndex] != null && accounts[editIndex].Server != null && accounts[editIndex].Server.IsCustomIP())
					{
						tfCustomServerName.setText(accounts[editIndex].Server.name);
						tfCustomServerAddress.setText(accounts[editIndex].Server.hostnameOrIPAddress);
						tfCustomServerPort.setText(accounts[editIndex].Server.port.ToString());
					}
					isEditingCustomServer = true;
					break;
				}
				case CommandType.FinishEditCustomServer:
					if (string.IsNullOrEmpty(tfCustomServerName.getText()))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerServerNameBlank + '!');
						break;
					}
					if (string.IsNullOrEmpty(tfCustomServerAddress.getText()))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerServerAddressBlank + '!');
						break;
					}
					if (string.IsNullOrEmpty(tfCustomServerPort.getText()))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerServerPortBlank + '!');
						break;
					}
					if (!int.TryParse(tfCustomServerPort.getText(), out int port))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerServerPortInvalid + '!');
						break;
					}
					if (port < ushort.MinValue || port > ushort.MaxValue)
					{
						GameCanvas.startOKDlg(string.Format(Strings.inputNumberOutOfRange, ushort.MinValue, ushort.MaxValue) + '!');
						break;
					}
					customServer = new Server(tfCustomServerName.getText(), tfCustomServerAddress.getText(), (ushort)port);
					isEditingCustomServer = false;
					goto case CommandType.FinishInputAccount;
				case CommandType.CancelEditCustomServer:
					isEditingCustomServer = false;
					break;
				case CommandType.ToggleHidePassword:
					tfPass.setIputType(tfPass.inputType == TField.INPUT_TYPE_PASSWORD ? TField.INPUT_TYPE_ANY : TField.INPUT_TYPE_PASSWORD);
					toggleHidePassword.img.texture = tfPass.inputType == TField.INPUT_TYPE_PASSWORD ? show : hide;
					break;
				case CommandType.MoveAccountUp:
				{
					int index = scrollableMenuAccounts.CurrentItemIndex;
					if (index > 0 && index < accounts.Count)
					{
						Account acc = accounts[index];
						accounts.RemoveAt(index);
						accounts.Insert(index - 1, acc);
						if (selectedAccountIndex == index)
							selectedAccountIndex = index - 1;
						else if (selectedAccountIndex == index - 1)
							selectedAccountIndex = index;
						scrollableMenuAccounts.CurrentItemIndex = index - 1;
						InGameAccountDAO.Save();
					}
					break;
				}
				case CommandType.MoveAccountDown:
				{
					int index = scrollableMenuAccounts.CurrentItemIndex;
					if (index >= 0 && index < accounts.Count - 1)
					{
						Account acc = accounts[index];
						accounts.RemoveAt(index);
						accounts.Insert(index + 1, acc);
						if (selectedAccountIndex == index)
							selectedAccountIndex = index + 1;
						else if (selectedAccountIndex == index + 1)
							selectedAccountIndex = index;
						scrollableMenuAccounts.CurrentItemIndex = index + 1;
						InGameAccountDAO.Save();
					}
					break;
				}
				case CommandType.ImportAccounts:
					selectServer.X = tfInputDataImportAccounts.x;
					selectServer.Y = tfInputRegexMatchAccountInfoImportAccounts.y + ITEM_HEIGHT + 15;
					selectServer.Width = tfInputDataImportAccounts.width;
					isImportingAccounts = true;
					break;
				case CommandType.HelpImportAccounts:
					GameCanvas.startOKDlg(Strings.inGameAccountManagerImportAccountsHelp);
					break;
				case CommandType.FinishImportAccounts:
					string data = tfInputDataImportAccounts.getText();
					if (string.IsNullOrWhiteSpace(data))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerImportAccountsInputDataBlank + '!');
						break;
					}
					string linesPattern = tfInputRegexMatchLinesImportAccounts.getText();
					if (string.IsNullOrWhiteSpace(linesPattern))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerRegexMatchLinesBlank + '!');
						break;
					}
					string accountInfoPattern = tfInputRegexMatchAccountInfoImportAccounts.getText();
					if (string.IsNullOrWhiteSpace(accountInfoPattern))
					{
						GameCanvas.startOKDlg(Strings.inGameAccountManagerRegexMatchAccountInfoBlank + '!');
						break;
					}
					Regex linesRegex;
					Regex accountInfoRegex;
					try
					{
						linesRegex = new Regex(linesPattern, RegexOptions.None, ImportRegexTimeout);
						accountInfoRegex = new Regex(accountInfoPattern, RegexOptions.None, ImportRegexTimeout);
					}
					catch (ArgumentException ex)
					{
						GameCanvas.startOKDlg(ex.Message);
						break;
					}
					int fallbackServerIndex = selectServer.SelectedIndex;
					Server[] serversSnapshot = defaultServers;
					InfoDlg.showWait();
					new Thread(() =>
					{
						List<Account> importedAccounts = new List<Account>();
						int countAccountAdded = 0;
						int countAccountMissed = 0;
						string timeoutError = null;
						try
						{
							while (!string.IsNullOrWhiteSpace(data))
							{
								Match lineMatch = linesRegex.Match(data);
								Match accountInfoMatch;
								int advanceIndex;

								if (!lineMatch.Success)
								{
									accountInfoMatch = accountInfoRegex.Match(data);
									if (!accountInfoMatch.Success || accountInfoMatch.Length == 0)
									{
										countAccountMissed++;
										break;
									}
									advanceIndex = System.Math.Max(1, accountInfoMatch.Index + accountInfoMatch.Length);
								}
								else if (lineMatch.Length == 0)
								{
									countAccountMissed++;
									int nextNewline = data.IndexOf('\n', lineMatch.Index);
									int skip = nextNewline >= 0 ? nextNewline + 1 : System.Math.Max(1, lineMatch.Index + 1);
									data = skip < data.Length ? data.Substring(skip) : string.Empty;
									continue;
								}
								else
								{
									string lineContent = lineMatch.Groups.Count > 1 ? lineMatch.Groups[1].Value : lineMatch.Value;
									accountInfoMatch = accountInfoRegex.Match(lineContent);
									advanceIndex = System.Math.Max(1, lineMatch.Index + lineMatch.Length);
								}

								if (!accountInfoMatch.Success)
								{
									countAccountMissed++;
									data = advanceIndex < data.Length ? data.Substring(advanceIndex) : string.Empty;
									continue;
								}

								try
								{
									Account account = new Account
									{
										Username = accountInfoMatch.Groups[1].Value,
										Password = accountInfoMatch.Groups[2].Value
									};
									try
									{
										int index = int.Parse(accountInfoMatch.Groups[3].Value);
										if (serversSnapshot == null || index < 0 || index >= serversSnapshot.Length)
											throw new IndexOutOfRangeException();
										account.Server = new Server(index);
									}
									catch
									{
										if (serversSnapshot != null && fallbackServerIndex >= 0 && fallbackServerIndex < serversSnapshot.Length)
											account.Server = serversSnapshot[fallbackServerIndex];
										else
											account.Server = new Server(0);
									}
									importedAccounts.Add(account);
									data = advanceIndex < data.Length ? data.Substring(advanceIndex) : string.Empty;
									countAccountAdded++;
								}
								catch
								{
									countAccountMissed++;
									data = advanceIndex < data.Length ? data.Substring(advanceIndex) : string.Empty;
								}
							}
						}
						catch (RegexMatchTimeoutException)
						{
							timeoutError = "Regex match timed out while importing accounts!";
						}

						MainThreadDispatcher.Dispatch(() =>
						{
							if (importedAccounts.Count > 0)
							{
								if (scrollableMenuAccounts.CurrentItemIndex == -1 ||
								    scrollableMenuAccounts.CurrentItemIndex >= accounts.Count)
								{
									accounts.AddRange(importedAccounts);
								}
								else
								{
									accounts.InsertRange(scrollableMenuAccounts.CurrentItemIndex + 1, importedAccounts);
								}
								InGameAccountDAO.Save();
							}

							InfoDlg.hide();
							if (timeoutError != null && countAccountAdded == 0)
							{
								GameCanvas.startOKDlg(timeoutError);
							}
							else
							{
								int total = countAccountAdded + countAccountMissed;
								float addedPercent = total > 0 ? countAccountAdded * 100f / total : 0f;
								float missedPercent = total > 0 ? countAccountMissed * 100f / total : 0f;
								GameCanvas.startOKDlg(string.Format(
									Strings.inGameAccountManagerImportAccountsResult,
									countAccountAdded,
									addedPercent,
									countAccountMissed,
									missedPercent) + '!');
							}
							GetInputAccountArea(out int x, out int y, out int width, out int height, true);
							selectServer.X = x + 10;
							selectServer.Y = tfCustomServerPort.y = y + 15 + ITEM_HEIGHT + 15 + ITEM_HEIGHT + 15;
							selectServer.Width = width - 20;
							isImportingAccounts = false;
						});
					})
					{
						IsBackground = true
					}.Start();
					break;
				case CommandType.CloseImportAccounts:
					GetInputAccountArea(out int x, out int y, out int width, out int height, true);
					selectServer.X = x + 10;
					selectServer.Y = tfCustomServerPort.y = y + 15 + ITEM_HEIGHT + 15 + ITEM_HEIGHT + 15;
					selectServer.Width = width - 20;
					isImportingAccounts = false;
					break;
				}
			}
			internal static ActionListener gI()
			{
				if (_instance == null)
					_instance = new ActionListener();
				return _instance;
			}
		}
	}
}
