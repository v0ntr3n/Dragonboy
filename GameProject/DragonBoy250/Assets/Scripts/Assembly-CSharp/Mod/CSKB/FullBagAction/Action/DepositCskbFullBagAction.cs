using System.Collections;
using Mod.Xmap;
using UnityEngine;

namespace Mod.CSKB
{
	internal sealed class DepositCskbFullBagAction : ICskbFullBagAction
	{
		const float OpenDepositMenuTimeoutSeconds = 5f;
		const float OpenDepositMenuRetrySeconds = 2.5f;

		public IEnumerator Execute(CskbFullBagActionContext context)
		{
			if (!XmapController.gI.IsActing && TileMap.mapID != CskbConstants.MapMarketId)
			{
				XmapController.start(CskbConstants.MapMarketId);
				yield return null;
			}

			if (TileMap.mapID != CskbConstants.MapMarketId || XmapController.gI.IsActing)
			{
				yield break;
			}

			Npc npc = Utils.findNpc(CskbConstants.DepositNpcId);
			if (npc == null)
			{
				yield break;
			}

			if (!CskbNpcHelper.IsMyCharInNpcPosition(npc))
			{
				Utils.teleToNpc(CskbConstants.DepositNpcId);
				yield return new WaitForSecondsRealtime(0.5f);
			}

			if (CskbNpcHelper.ShouldOpenShopMenu(npc))
			{
				yield return OpenDepositMenu(npc);
			}

			if (TryGetDepositItem(out Item itemCSKBForDeposit))
			{
				yield return new WaitForSecondsRealtime(1f);
				Service.gI().kigui(0, itemCSKBForDeposit.itemId, 0, UpCSKB.moneyToDeposit, context.CapsuleInBag.quantity);
			}
		}

		static IEnumerator OpenDepositMenu(Npc npc)
		{
			float deadline = Time.realtimeSinceStartup + OpenDepositMenuTimeoutSeconds;
			while (Time.realtimeSinceStartup < deadline)
			{
				yield return CskbNpcHelper.OpenShopMenu(npc, CskbConstants.DepositNpcId, 1);
				float retryDeadline = Mathf.Min(deadline, Time.realtimeSinceStartup + OpenDepositMenuRetrySeconds);
				while (!IsDepositShopReady() && Time.realtimeSinceStartup < retryDeadline)
				{
					yield return null;
				}

				if (IsDepositShopReady())
				{
					break;
				}
			}

			if (!IsDepositShopReady() && GameCanvas.panel != null && GameCanvas.panel.isShow)
			{
				GameCanvas.panel.hide();
			}

			GameCanvas.menu.doCloseMenu();
			Char.chatPopup = null;
		}

		static bool IsDepositShopReady()
		{
			Char c = Char.myCharz();
			return c is { arrItemShop: { Length: > 4 } arr } && arr[4] != null;
		}

		static bool TryGetDepositItem(out Item itemCSKBForDeposit)
		{
			itemCSKBForDeposit = null;
			if (GameCanvas.panel is null || !GameCanvas.panel.isShow)
			{
				return false;
			}

			Item[][] itemShops = Char.myCharz().arrItemShop;
			if (itemShops == null || itemShops.Length <= 4 || itemShops[4] == null)
			{
				return false;
			}

			Item[] consignmentItems = itemShops[4];
			for (int i = 0; i < consignmentItems.Length; i++)
			{
				Item candidate = consignmentItems[i];
				if (candidate?.template != null && candidate.template.id == CskbConstants.IdCapsuleKb && candidate.buyType == 0)
				{
					itemCSKBForDeposit = candidate;
					return true;
				}
			}

			return false;
		}
	}
}
