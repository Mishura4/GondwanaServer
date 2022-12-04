using DOL.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class OutlawMerchant
        : GameMerchant
    {

        public override bool Interact(GamePlayer player)
        {
            if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
            {
                this.TurnTo(player, 5000);
                player.Out.SendMessage("...", PacketHandler.eChatType.CT_Chat, PacketHandler.eChatLoc.CL_PopupWindow);
                return true;
            }

            TurnTo(player, 10000);
            SendMerchantWindow(player);
            return true;
        }

        public override void OnPlayerSell(GamePlayer player, InventoryItem item)
        {
            if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
            {
                this.TurnTo(player, 5000);
                player.Out.SendMessage("...", PacketHandler.eChatType.CT_Chat, PacketHandler.eChatLoc.CL_PopupWindow);
                return;
            }

            base.OnPlayerSell(player, item);
        }

        public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
        {
            if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
            {
                this.TurnTo(player, 5000);
                player.Out.SendMessage("...", PacketHandler.eChatType.CT_Chat, PacketHandler.eChatLoc.CL_PopupWindow);
                return;
            }

            base.OnPlayerBuy(player, item_slot, number);
        }


        public override void LoadFromDatabase(DataObject merchantobject)
        {
            base.LoadFromDatabase(merchantobject);
            if (!(merchantobject is Mob))
            {
                return;
            }

            Mob merchant = (Mob)merchantobject;
            if (merchant.ItemsListTemplateID != null && merchant.ItemsListTemplateID.Length > 0)
            {
                m_tradeItems = new OutlawTradeItems(merchant.ItemsListTemplateID);
            }
        }
    }
}