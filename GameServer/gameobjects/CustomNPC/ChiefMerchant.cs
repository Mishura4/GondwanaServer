using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS
{
    public class ChiefMerchant
        : GameNPC
    {
        private readonly string CHIEF_ITEM_ID = "license_merchant";

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            TurnTo(player, 5000);
            
            if (player.HasAbility(DOL.GS.Abilities.Trading))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.Denied"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (player.Level >= 20)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.Ask"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var player = source as GamePlayer;

            if (item == null || player == null || player.HasAbility(DOL.GS.Abilities.Trading))
            {
                return base.ReceiveItem(source, item);
            }


            if (item.Id_nb.Equals(CHIEF_ITEM_ID) && player.Level >= 20)
            {
                //player.AddAbility(SkillBase.GetAbility(DOL.GS.Abilities.Trading, 1));
                player.AddUsableSkill(SkillBase.GetAbility(DOL.GS.Abilities.Trading, 1));
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.Done"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Inventory.RemoveItem(item);
                player.Out.SendNPCsQuestEffect(this, this.GetQuestIndicator(player));
                player.SaveIntoDatabase();
                player.Out.SendUpdatePlayer();
                return true;
            }

            return base.ReceiveItem(source, item);
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            if (player.Level >= 20 && !player.HasAbility(DOL.GS.Abilities.Trading))
            {
                return eQuestIndicator.Lore;
            }
            return base.GetQuestIndicator(player);
        }
    }
}