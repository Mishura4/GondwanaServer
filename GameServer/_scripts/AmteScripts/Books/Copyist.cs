using System;
using System.Linq;
using DOL.Database;
using DOL.GS.Finance;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Scripts
{
    public class Copyist : AmteMob
    {
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.Client.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Copyist.InteractText01") + "\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Copyist.InteractText02"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            GamePlayer p = source as GamePlayer;
            if (p == null || item == null)
                return false;

            if (item.Id_nb.StartsWith("scroll"))
            {
                var book = GameServer.Database.SelectObject<DBBook>(b => b.Name == item.Name);
                if (book != null)
                {
                    if (book.PlayerID != p.InternalID)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"\"Copyist.ResponseText01\""), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return false;
                    }
                    if (!p.RemoveMoney(Currency.Copper.Mint(20000)))
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"\"Copyist.ResponseText02\""), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return false;
                    }

                    var iu = new ItemUnique(item.IUWrapper) { Id_nb = "scroll" + Guid.NewGuid() };
                    GameServer.Database.AddObject(iu);
                    var invItem = GameInventoryItem.Create(iu);
                    p.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, invItem);
                    InventoryLogging.LogInventoryAction(this, p, eInventoryActionType.Merchant, invItem, invItem.Count);
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"\"Copyist.ResponseText03" + p.Name + ".\""), eChatType.CT_System, eChatLoc.CL_PopupWindow);

                }
                else
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"\"Copyist.ResponseText04\""), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            }
            else
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,"\"Copyist.ResponseText05\""), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return false;
        }
    }
}
