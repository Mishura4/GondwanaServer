using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.Finance;
using DOL.Language;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Summary description for Banquier.
    /// </summary>
    public class Banquier : GameNPC
    {
        public Banquier()
        {
            SetOwnBrain(new BlankBrain());
            GuildName = "Banquier";
        }

        public override bool ReceiveMoney(GameLiving source, long money)
        {
            return ReceiveMoney(source, money, true);
        }

        public bool ReceiveMoney(GameLiving source, long money, bool removeMoney)
        {
            if (source == null || money <= 0) return false;
            if (!(source is GamePlayer))
                return false;
            GamePlayer player = source as GamePlayer;
            DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(player.InternalID);
            if (bank == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.AccountCreate"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                bank = new DBBanque(player.InternalID);
                GameServer.Database.AddObject(bank);
            }

            if (removeMoney)
            {
                if (!player.RemoveMoney(Currency.Copper.Mint(money)))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.DontHaveAmount"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                InventoryLogging.LogInventoryAction(source, this, eInventoryActionType.Other, money);
            }
            bank.Money = money + bank.Money;

            GameServer.Database.SaveObject(bank);

            string message = "";
            if (Money.GetMithril(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Mithril", Money.GetMithril(bank.Money)) + " ";
            if (Money.GetPlatinum(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Platinum", Money.GetPlatinum(bank.Money)) + " ";
            if (Money.GetGold(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Gold", Money.GetGold(bank.Money)) + " ";
            if (Money.GetSilver(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Silver", Money.GetSilver(bank.Money)) + " ";
            if (Money.GetCopper(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Copper", Money.GetCopper(bank.Money)) + " ";

            if (message != "")
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Moneyamount", message);
            else
                message = LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Nomoney");
            player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (!(source is GamePlayer)) return false;
            GamePlayer player = source as GamePlayer;

            if (player != null && player.Reputation < 0)
            {
                TurnTo(player, 5000);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.InteractOutlaw"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (!item.Id_nb.StartsWith("BANQUE_CHEQUE")) return false;

            if (player.Inventory.RemoveCountFromStack(item, item.Count))
            {
                ReceiveMoney(player, item.Price, false);
                InventoryLogging.LogInventoryAction(source, this, eInventoryActionType.Other, item, item.Count);
                InventoryLogging.LogInventoryAction(this, source, eInventoryActionType.Other, item.Price);
            }
            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (player.Reputation < 0)
            {
                TurnTo(player, 5000);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.InteractOutlaw"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(player.InternalID);
            if (bank == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.InteractText01") + "\r\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.InteractText02"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            string message = LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Greetings1", player.Name) + " ";
            if (Money.GetMithril(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Mithril", Money.GetMithril(bank.Money)) + " ";
            if (Money.GetPlatinum(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Platinum", Money.GetPlatinum(bank.Money)) + " ";
            if (Money.GetGold(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Gold", Money.GetGold(bank.Money)) + " ";
            if (Money.GetSilver(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Silver", Money.GetSilver(bank.Money)) + " ";
            if (Money.GetCopper(bank.Money) != 0)
                message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Copper", Money.GetCopper(bank.Money)) + " ";
            message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Greetings2") + "\r\n";
            message += LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Greetings3") + "\n\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Greetings4") + "\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Greetings5") + "\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.Greetings6");
            player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;
            GamePlayer player = source as GamePlayer;
            if (player == null)
                return true;

            if (player.Reputation < 0)
            {
                TurnTo(player, 5000);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.InteractOutlaw"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return true;
            }

            DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(player.InternalID);
            if (bank == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.NoAccountYet1") + "\r\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.NoAccountYet2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            switch (str.ToLower())
            {
                case "retirer de l'argent":
                case "withdraw money":
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.HowMuchWithdraw") + "\r\n" + LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.WithdrawAmount"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
                case "la totalité":
                case "everything":
                    WithdrawMoney(bank, player, bank.Money);
                    break;
                case "quelques pièces":
                case "some coins":
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.WithdrawSomeCoins"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
                case "faire un chèque":
                case "write a check":
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.WriteCheck"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
                case "encaisser un chèque":
                case "cash a check":
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Banker.CashCheck"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
            }
            return true;
        }

        /// <summary>
        /// Retire de l'argent dans la banque et le donne au joueur
        /// </summary>
        public static bool WithdrawMoney(DBBanque bank, GamePlayer player, long money)
        {
            if (bank.Money < money)
                return false;

            bank.Money -= money;
            GameServer.Database.SaveObject(bank);
            player.AddMoney(Currency.Copper.Mint(money));
            player.SaveIntoDatabase();
            return true;
        }

        /// <summary>
        /// Retire de l'argent dans la banque sans le donner au joueur
        /// </summary>
        public static bool TakeMoney(DBBanque bank, GamePlayer player, long money)
        {
            if (bank.Money < money)
                return false;

            bank.Money -= money;
            GameServer.Database.SaveObject(bank);
            return true;
        }
    }
}
