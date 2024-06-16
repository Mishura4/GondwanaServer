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
            string titleKey = null;

            switch (item.Id_nb)
            {
                case "TaskToken_Trader_lv1":
                    AssignTitle(player, new TraderTitleLevel1());
                    titleKey = "Titles.Trader.Level1";
                    break;
                case "TaskToken_Trader_lv2":
                    AssignTitle(player, new TraderTitleLevel2());
                    titleKey = "Titles.Trader.Level2";
                    break;
                case "TaskToken_Trader_lv3":
                    AssignTitle(player, new TraderTitleLevel3());
                    titleKey = "Titles.Trader.Level3";
                    break;
                case "TaskToken_Trader_lv4":
                    AssignTitle(player, new TraderTitleLevel4());
                    titleKey = "Titles.Trader.Level4";
                    break;
                case "TaskToken_Trader_lv5":
                    AssignTitle(player, new TraderTitleLevel5());
                    titleKey = "Titles.Trader.Level5";
                    break;
                default:
                    return false;
            }

            if (titleKey != null)
            {
                string titleName = LanguageMgr.GetTranslation(player.Client.Account.Language, titleKey);
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.GiveTitle", titleName);
                player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
            }

            player.Inventory.RemoveItem(item);

            return true;
        }

        private void AssignTitle(GamePlayer player, IPlayerTitle title)
        {
            if (!player.Titles.Contains(title))
            {
                player.Titles.Add(title);
                title.OnTitleGained(player);
                player.UpdateCurrentTitle();
            }
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