using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.Language;
using System.Collections.Generic;
using System.Linq;


namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&banque",
        ePrivLevel.Player,
        "Commands.Players.Banque.Description",
        "Commands.Players.Banque.Usage",
        "Commands.Players.Banque.Usage.Cheque")]
    public class BanqueCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }

            try
            {
                Banquier target = client.Player.TargetObject as Banquier;
                if (target != null)
                {
                    DBBanque bank = GameServer.Database.FindObjectByKey<DBBanque>(client.Player.InternalID);
                    if (args[1].ToLower() == "chèque" || args[1].ToLower() == "cheque")
                    {
                        if (bank == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.NoAccount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        long newMoney = GetMoney(args, 2);
                        if (newMoney > 1000000000)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.MaxValue"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        if (!Banquier.TakeMoney(bank, client.Player, newMoney))
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.NoMoney"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }


                        ItemUnique item = new ItemUnique
                        {
                            Model = 499,
                            Id_nb = "BANQUE_CHEQUE_" + client.Player.Name + "_" + (DateTime.Now.Ticks / 10000).ToString("X8"),
                            Price = newMoney,
                            Weight = 2,
                            Name = LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.Name", client.Player.Name),
                            Description = LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.Description1", Money.GetString(newMoney)) + "\n\n" + LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.Description2")
                        };
                        GameServer.Database.AddObject(item);

                        string message = "";
                        GameInventoryItem inventoryItem = GameInventoryItem.Create(item);
                        if (!client.Player.Inventory.AddTemplate(inventoryItem, 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                        {
                            ItemTemplate dummyVault = CharacterVaultKeeper.GetDummyVaultItem(client.Player);
                            CharacterVault vault = new CharacterVault(client.Player, 0, dummyVault);
                            if (!vault.AddItem(client.Player, inventoryItem, true))
                            {
                                vault = new CharacterVault(client.Player, 1, dummyVault);
                                if (!vault.AddItem(client.Player, inventoryItem, true))
                                {
                                    bank.Money += newMoney;
                                    GameServer.Database.SaveObject(bank);
                                    GameServer.Database.DeleteObject(item);
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.InventoryFull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            message += LanguageMgr.GetTranslation(client.Account.Language, "Banque.Cheque.MovedToVault") + "\n";
                        }
                        message += LanguageMgr.GetTranslation(client.Account.Language, "Banque.AccountOverview", Money.GetString(bank.Money));
                        client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        InventoryLogging.LogInventoryAction(client.Player, target, eInventoryActionType.Other, newMoney);
                        InventoryLogging.LogInventoryAction(target, client.Player, eInventoryActionType.Other, item);
                    }
                    else
                    {
                        if (bank == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Withdraw.NoAccount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        long newMoney = GetMoney(args, 1);
                        if (!Banquier.WithdrawMoney(bank, client.Player, newMoney))
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Withdraw.NoMoney"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        string message = LanguageMgr.GetTranslation(client.Account.Language, "Banque.AccountOverview", Money.GetString(bank.Money));
                        client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        InventoryLogging.LogInventoryAction(target, client.Player, eInventoryActionType.Other, newMoney);
                    }
                }
                else
                {
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Banque.Withdraw.SelectNPC"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                client.Out.SendUpdateMoney();
            }
            catch (Exception)
            {
                DisplaySyntax(client);
            }
        }

        private static long GetMoney(string[] args, int offset)
        {
            int C;
            int S = 0;
            int G = 0;
            int P = 0;
            if (int.TryParse(args[offset], out C))
            {
                if (args.Length > offset + 1 && int.TryParse(args[offset + 1], out S))
                {
                    if (args.Length > offset + 2 && int.TryParse(args[offset + 2], out G))
                    {
                        if (args.Length > offset + 3 && int.TryParse(args[offset + 3], out P))
                            P = Math.Max(0, Math.Min(P, 999));
                        G = Math.Max(0, Math.Min(G, 999));
                    }
                    S = Math.Max(0, Math.Min(S, 99));
                }
                C = Math.Max(0, Math.Min(C, 99));
            }

            return Money.GetMoney(0, P, G, S, C);
        }
    }
}