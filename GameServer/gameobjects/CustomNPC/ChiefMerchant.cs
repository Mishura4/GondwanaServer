using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.PlayerTitles;
using System;
using DOL.GS.Scripts;
using System.Collections.Generic;

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
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.IsTrader"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.Token"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (player.Level >= 20)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.Ask"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;
            if (player == null)
                return false;

            if (str.ToLower() == "tokens" || str.ToLower() == "jetons")
            {
                string tokenMessage = LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.TokenDesc");
                player.Out.SendMessage(tokenMessage, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var player = source as GamePlayer;

            if (item == null || player == null)
            {
                return base.ReceiveItem(source, item);
            }

            if (player.HasAbility(DOL.GS.Abilities.Trading))
            {
                return HandleTradingTaskTokens(player, item);
            }

            if (item.Id_nb.Equals(CHIEF_ITEM_ID) && player.Level >= 20)
            {
                player.AddUsableSkill(SkillBase.GetAbility(DOL.GS.Abilities.Trading, 1));
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChiefMerchant.Done"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Inventory.RemoveItem(item);
                player.Out.SendNPCsQuestEffect(this, this.GetQuestIndicator(player));
                player.SaveIntoDatabase();
                player.Out.SendUpdatePlayerSkills();
                return true;
            }

            return base.ReceiveItem(source, item);
        }

        private bool HandleTradingTaskTokens(GamePlayer player, InventoryItem item)
        {
            int level = item.Id_nb switch
            {
                "TaskToken_Trader" => 1,
                "TaskToken_Trader_lv1" => 1,
                "TaskToken_Trader_lv2" => 2,
                "TaskToken_Trader_lv3" => 3,
                "TaskToken_Trader_lv4" => 4,
                "TaskToken_Trader_lv5" => 5,
                _ => 0
            };

            if (TaskMaster.AssignTitle(player, PlayerTitleMgr.TaskTitles.Trader, level, "Titles.Trader"))
            {
                player.Inventory.RemoveItem(item);
            }
            else
            {
                return TaskMaster.RefuseItem(player, item);
            }

            return true;
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