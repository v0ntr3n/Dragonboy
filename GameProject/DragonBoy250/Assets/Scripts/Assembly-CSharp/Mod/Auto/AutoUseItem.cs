using System.Collections.Generic;

namespace Mod.Auto
{
	public class AutoUseItem : IActionListener, IChatable
	{

		static AutoUseItem _Instance;

		static readonly List<Item> listItemAuto = new List<Item>();

		static Item itemToAuto;

		static Item activeBuyItem;

		static long nextBuyTime;

		static Item activeSellItem;

		static long nextSellTime;

		static bool sellWaitingConfirm;

		static readonly string[] inputDelay = new string[2]
		{
			"Nhập delay", "giây"
		};

		static readonly string[] inputSellQuantity = new string[2]
		{
			"Nhập số lượng bán", "số lượng"
		};

		static readonly string[] inputBuyQuantity = new string[2]
		{
			"Nhập số lượng mua", "số lượng"
		};

		public void perform(int idAction, object p)
		{
			switch (idAction)
			{
			case 1:
				OpenTFAutoUseItem((Item)p);
				break;
			case 2:
				RemoveItemAuto((int)p);
				break;
			case 4:
				AddItemAutoUse((Item)p);
				break;
			case 3:
				OpenTFAutoTradeItem((Item)p);
				break;
			}
		}

		public void onChatFromMe(string text, string to)
		{
			if (ChatTextField.gI().tfChat.getText() == null || ChatTextField.gI().tfChat.getText().Equals(string.Empty) || text.Equals(string.Empty))
			{
				ChatTextField.gI().isShow = false;
			}
			else if (ChatTextField.gI().strChat.Equals(inputDelay[0]))
			{
				try
				{
					int delay = int.Parse(ChatTextField.gI().tfChat.getText());
					itemToAuto.Delay = delay < 100 ? 100 : delay;
					GameScr.info1.addInfo("Auto " + itemToAuto.Name + ": " + itemToAuto.Delay + " ms", 0);
					listItemAuto.Add(itemToAuto);
				}
				catch
				{
					GameScr.info1.addInfo("Delay Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ChatTextField.gI().ResetTF();
			}
			else if (ChatTextField.gI().strChat.Equals(inputBuyQuantity[0]))
			{
				try
				{
					int quantity = int.Parse(ChatTextField.gI().tfChat.getText());
					itemToAuto.Quantity = quantity;
					StartAutoBuy(itemToAuto);
				}
				catch
				{
					GameScr.info1.addInfo("Số Lượng Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ChatTextField.gI().ResetTF();
			}
			else
			{
				if (!ChatTextField.gI().strChat.Equals(inputSellQuantity[0]))
				{
					return;
				}
				try
				{
					int quantity2 = int.Parse(ChatTextField.gI().tfChat.getText());
					itemToAuto.Quantity = quantity2;
					StartAutoSell(itemToAuto);
				}
				catch
				{
					GameScr.info1.addInfo("Số Lượng Không Hợp Lệ, Vui Lòng Nhập Lại!", 0);
				}
				ChatTextField.gI().ResetTF();
			}
		}

		public void onCancelChat()
		{
		}

		public static AutoUseItem getInstance()
		{
			if (_Instance == null)
			{
				_Instance = new AutoUseItem();
			}
			return _Instance;
		}

		public static void Update()
		{
			UpdateAutoBuy();
			UpdateAutoSell();
			if (listItemAuto.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < listItemAuto.Count; i++)
			{
				Item item = listItemAuto[i];
				if (mSystem.currentTimeMillis() - item.LastTimeUse > item.Delay)
				{
					item.LastTimeUse = mSystem.currentTimeMillis();
					bool isUsed = Utils.useItem((short)item.Id);
					if (!isUsed)
					{
						GameScr.info1.addInfo("Không Thể Sử Dụng " + item.Name + "!", 0);
						listItemAuto.RemoveAt(i);
						i--;
						continue;
					}
					break;
				}
			}
		}

		public static bool isAutoUse(int templateId)
		{
			for (int i = 0; i < listItemAuto.Count; i++)
			{
				if (listItemAuto[i].Id == templateId)
				{
					return true;
				}
			}
			return false;
		}

		static void RemoveItemAuto(int templateId)
		{
			for (int i = 0; i < listItemAuto.Count; i++)
			{
				if (listItemAuto[i].Id == templateId)
				{
					listItemAuto.RemoveAt(i);
					break;
				}
			}
		}

		public void AddItemAutoUse(Item item)
		{
			if (isAutoUse(item.Id))
			{
				GameScr.info1.addInfo("Đã Tồn Tại " + item.Name + " Trong Danh Sách Auto!", 0);
				return;
			}
			if (item.Delay <= 0)
			{
				item.Delay = 100;
			}
			listItemAuto.Add(item);
		}

		static void OpenTFAutoUseItem(Item item)
		{
			itemToAuto = item;
			ChatTextField.gI().strChat = inputDelay[0];
			ChatTextField.gI().tfChat.name = inputDelay[1];
			GameCanvas.panel.isShow = false;
			ChatTextField.gI().startChat2(getInstance(), string.Empty);
		}

		static void OpenTFAutoTradeItem(Item item)
		{
			itemToAuto = item;
			GameCanvas.panel.isShow = false;
			if (item.IsSell)
			{
				ChatTextField.gI().strChat = inputSellQuantity[0];
				ChatTextField.gI().tfChat.name = inputSellQuantity[1];
			}
			else
			{
				ChatTextField.gI().strChat = inputBuyQuantity[0];
				ChatTextField.gI().tfChat.name = inputBuyQuantity[1];
			}
			ChatTextField.gI().startChat2(getInstance(), string.Empty);
		}

		static void StartAutoSell(Item item)
		{
			activeSellItem = item;
			sellWaitingConfirm = false;
			nextSellTime = mSystem.currentTimeMillis() + 100;
		}

		static void StartAutoBuy(Item item)
		{
			activeBuyItem = item;
			nextBuyTime = 0L;
		}

		static void UpdateAutoSell()
		{
			if (activeSellItem == null)
			{
				return;
			}
			if (!Session_ME.gI().isConnected() || Char.myChar == null || Char.myChar.arrItemBag == null)
			{
				activeSellItem = null;
				sellWaitingConfirm = false;
				return;
			}
			long now = mSystem.currentTimeMillis();
			if (now < nextSellTime)
			{
				return;
			}
			short index = activeSellItem.Index;
			if (!sellWaitingConfirm)
			{
				if (activeSellItem.Quantity <= 0)
				{
					activeSellItem = null;
					GameScr.info1.addInfo("Xong!", 0);
					return;
				}
				global::Item[] bag = Char.myChar.arrItemBag;
				if (index < 0 || index >= bag.Length || bag[index]?.template == null || bag[index].template.id != (short)activeSellItem.Id)
				{
					activeSellItem = null;
					GameScr.info1.addInfo("Không Tìm Thấy Item!", 0);
					return;
				}
				Service.gI().saleItem(0, 1, (short)(index + 3));
				sellWaitingConfirm = true;
				nextSellTime = now + 100;
				return;
			}
			Service.gI().saleItem(1, 1, index);
			sellWaitingConfirm = false;
			activeSellItem.Quantity--;
			if (Char.myChar.xu > 1963100000)
			{
				activeSellItem = null;
				GameScr.info1.addInfo("Xong!", 0);
				return;
			}
			nextSellTime = now + 1000;
		}

		static void UpdateAutoBuy()
		{
			if (activeBuyItem == null)
			{
				return;
			}
			if (!Session_ME.gI().isConnected() || Char.myChar == null || Char.myChar.arrItemBag == null)
			{
				activeBuyItem = null;
				return;
			}
			long now = mSystem.currentTimeMillis();
			if (now < nextBuyTime)
			{
				return;
			}
			if (activeBuyItem.Quantity <= 0 || GameScr.gI().isBagFull())
			{
				activeBuyItem = null;
				GameScr.info1.addInfo("Xong!", 0);
				return;
			}
			Service.gI().buyItem(!activeBuyItem.IsGold ? (sbyte)1 : (sbyte)0, activeBuyItem.Id, 0);
			activeBuyItem.Quantity--;
			nextBuyTime = now + 1000;
		}

		public class Item
		{

			public int Delay;
			public int Id;

			public short Index;

			public bool IsGold;

			public bool IsSell;

			public long LastTimeUse;

			public string Name;

			public int Quantity;

			public Item(int id, string name)
			{
				Id = id;
				Name = name;
			}

			public Item(int id, int delay)
			{
				Id = id;
				Delay = delay;
				LastTimeUse = 0L;
			}

			public Item(int id, short isGold, bool index, bool isSell)
			{
				Id = id;
				IsGold = index;
				Index = isGold;
				IsSell = isSell;
			}
		}
	}
}
