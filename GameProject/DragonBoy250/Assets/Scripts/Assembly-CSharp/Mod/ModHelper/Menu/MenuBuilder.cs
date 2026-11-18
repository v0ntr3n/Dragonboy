using System;
using System.Collections.Generic;
using System.Linq;

namespace Mod.ModHelper.Menu
{
	public class MenuBuilder : IActionListener
	{

		string chatPopup;

		bool isPosDefault = true;

		public List<MenuItem> menuItems = new List<MenuItem>();

		public int x, y;

		public void perform(int idAction, object p)
		{
			IdAction id = (IdAction)idAction;
			switch (id)
			{
			case IdAction.None:
				break;
			case IdAction.MenuSelect:
				onMenuSelected(p);
				break;
			}
		}

		public MenuBuilder setChatPopup(string chatPopup)
		{
			this.chatPopup = chatPopup;
			return this;
		}

		//public MenuBuilder setPosDefault()
		//{
		//    isPosDefault = true;
		//    return this;
		//}

		public MenuBuilder setPos(int x, int y)
		{
			isPosDefault = false;
			this.x = x;
			this.y = y;
			return this;
		}

		//public MenuBuilder addItem(string caption, Action action)
		//{
		//    return addItem(caption, new(action));
		//}

		public MenuBuilder addItem(string caption, MenuAction action)
		{
			menuItems.Add(new MenuItem(caption, action));
			return this;
		}

		public MenuBuilder addItem(bool ifCondition, string caption, MenuAction action)
		{
			if (ifCondition)
				menuItems.Add(new MenuItem(caption, action));
			return this;
		}

		public MenuBuilder map<T>(MyVector myVector, Func<T, MenuItem> func)
		{
			for (int i = 0; i < myVector.size(); i++)
			{
				T item = (T)myVector.elementAt(i);
				menuItems.Add(func.Invoke(item));
			}
			return this;
		}

		public MenuBuilder map<T>(IEnumerable<T> values, Func<T, MenuItem> func)
		{
			foreach (T item in values)
			{
				menuItems.Add(func.Invoke(item));
			}
			return this;
		}

		public void start()
		{
			MyVector myVector = getMyVectorStartMenu();
			if (myVector.size() > 0)
			{
				if (isPosDefault)
					GameCanvas.menu.startAt(myVector, 3);
				else
					GameCanvas.menu.startAt(myVector, x, y);
			}
			if (!string.IsNullOrEmpty(chatPopup))
				ChatPopup.addChatPopup(chatPopup, 100000, new Npc(5, 0, -100, 100, 5, Utils.ID_NPC_MOD_FACE));
		}

		private sealed class MenuCommandPayload
		{
			public readonly int selected;
			public readonly MenuAction action;
			public readonly string[] captions;

			public MenuCommandPayload(int selected, MenuAction action, string[] captions)
			{
				this.selected = selected;
				this.action = action;
				this.captions = captions;
			}
		}

		MyVector getMyVectorStartMenu()
		{
			int count = menuItems.Count;
			string[] captionsArray = new string[count];
			for (int i = 0; i < count; i++)
				captionsArray[i] = menuItems[i].caption;

			MyVector myVector = new MyVector();
			for (int i = 0; i < count; i++)
			{
				MenuItem menuItem = menuItems[i];
				myVector.addElement(new Command(menuItem.caption, this, (int)IdAction.MenuSelect,
					new MenuCommandPayload(i, menuItem.action, captionsArray)));
			}

			return myVector;
		}

		static void onMenuSelected(object p)
		{
			if (p is not MenuCommandPayload payload)
				return;

			int selected = payload.selected;
			MenuAction action = payload.action;
			string[] captions = payload.captions;

			string caption = captions[selected];
			if (Char.chatPopup != null && Char.chatPopup.c.avatar == Utils.ID_NPC_MOD_FACE)
				Char.chatPopup = null;
			action.Invoke(selected, caption, captions);
		}
	}
}
