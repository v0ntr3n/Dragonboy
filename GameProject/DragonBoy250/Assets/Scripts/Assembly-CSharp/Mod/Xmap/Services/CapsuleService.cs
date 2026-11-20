namespace Mod.Xmap
{
	internal sealed class CapsuleService
	{
		readonly MapLookupService mapLookup;
		readonly XmapSettings settings;

		internal CapsuleService(MapLookupService mapLookup, XmapSettings settings)
		{
			this.mapLookup = mapLookup;
			this.settings = settings;
		}

		bool CanUseVip()
		{
			Char myChar = Char.myCharz();
			return settings.UseCapsuleVip && myChar != null && !myChar.IsCharDead() && HasCapsuleInBag(myChar.arrItemBag, MapLookupService.IdItemCapsuleVip, -1);
		}

		bool CanUseNormal()
		{
			Char myChar = Char.myCharz();
			return settings.UseCapsuleNormal && myChar != null && !myChar.IsCharDead() && HasCapsuleInBag(myChar.arrItemBag, MapLookupService.IdItemCapsuleNormal, 10);
		}

		static bool HasCapsuleInBag(Item[] items, short templateId, int minExclusiveQuantity)
		{
			if (items == null)
			{
				return false;
			}

			for (int i = 0; i < items.Length; i++)
			{
				Item item = items[i];
				if (item?.template != null && item.template.id == templateId && (minExclusiveQuantity < 0 || item.quantity > minExclusiveQuantity))
				{
					return true;
				}
			}

			return false;
		}

		internal bool CanUseAny()
		{
			return CanUseVip() || CanUseNormal();
		}

		internal void UseProbeCapsule()
		{
			if (CanUseVip())
			{
				Service.gI().useItem(0, 1, -1, MapLookupService.IdItemCapsuleVip);
			}
			else if (CanUseNormal())
			{
				Service.gI().useItem(0, 1, -1, MapLookupService.IdItemCapsuleNormal);
			}
		}
	}

}
