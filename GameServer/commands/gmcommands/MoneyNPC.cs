using DOL.GS;
using DOL.GS.Commands;
using System.Collections.Generic;

namespace DOL.commands.gmcommands
{
    [CmdAttribute("&moneynpc",
      ePrivLevel.GM,
      "'/MoneyNPC info'Affiche les infos lié au MoneyNPC en Target",
      "'/MoneyNPC money <or>'Change la somme requise en spécifiant l'or requis",
      "'/MoneyNPC resource1 <templateid> <amount>' Change la resource 1 requise en spécifiant la quantité nécessaire",
      "'/MoneyNPC resource2 <templateid> <amount>' Change la resource 2 requise en spécifiant la quantité nécessaire",
      "'/MoneyNPC resource3 <templateid> <amount>' Change la resource 3 requise en spécifiant la quantité nécessaire",
      "'/MoneyNPC resource4 <templateid> <amount>' Change la resource 4 requise en spécifiant la quantité nécessaire",
      "'/MoneyNPC eventid <id>'Change l'ID de l'event que s'occupe ce npc",
      "'/MoneyNPC reset'Remet à zero la somme d'argent en cours")]
    public class MoneyNPC :
         AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {

            if (args.Length == 1)
            {
                DisplaySyntax(client);
            }

            MoneyEventNPC moneyNpc = client.Player.TargetObject as MoneyEventNPC;

            if (args.Length >= 2)
            {
                switch (args[1].ToLowerInvariant())
                {
                    case "info":
                        if (client.Player.TargetObject == null)
                        {
                            ShowWrongTarget(client);
                        }

                        if (client.Player.TargetObject is MoneyEventNPC npc)
                        {
                            ShowInfo(client, npc);
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;

                    case "money":

                        if (moneyNpc != null)
                        {
                            if (args.Length == 3)
                            {
                                if (int.TryParse(args[2], out int money))
                                {
                                    moneyNpc.RequiredMoney = Money.GetMoney(0, 0, money, 0, 0);
                                    moneyNpc.SaveIntoDatabase();
                                    client.Out.SendMessage("La somme requise de l'event " + (moneyNpc.ServingEventID ?? string.Empty) + " est maintenant de :" + Money.GetString(moneyNpc.CurrentMoney), GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    DisplaySyntax(client);
                                }
                            }
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;

                    case "resource4":

                        if (moneyNpc != null)
                        {
                            if (args.Length == 4)
                            {
                                if (int.TryParse(args[3], out int amount))
                                {
                                    moneyNpc.RequiredResource4 = amount;
                                    moneyNpc.SaveIntoDatabase();
                                    client.Out.SendMessage("La somme requise de l'event " + (moneyNpc.ServingEventID ?? string.Empty) + " est maintenant de :" + Money.GetString(moneyNpc.CurrentMoney), GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            DisplaySyntax(client);
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;

                    case "resource1":

                        if (moneyNpc != null)
                        {
                            if (args.Length == 4)
                            {
                                if (int.TryParse(args[3], out int amount))
                                {
                                    moneyNpc.RequiredResource1 = amount;
                                    moneyNpc.SaveIntoDatabase();
                                    client.Out.SendMessage("La somme requise de l'event " + (moneyNpc.ServingEventID ?? string.Empty) + " est maintenant de :" + Money.GetString(moneyNpc.CurrentMoney), GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            DisplaySyntax(client);
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;

                    case "resource2":

                        if (moneyNpc != null)
                        {
                            if (args.Length == 4)
                            {
                                if (int.TryParse(args[3], out int amount))
                                {
                                    moneyNpc.RequiredResource2 = amount;
                                    moneyNpc.SaveIntoDatabase();
                                    client.Out.SendMessage("La somme requise de l'event " + (moneyNpc.ServingEventID ?? string.Empty) + " est maintenant de :" + Money.GetString(moneyNpc.CurrentMoney), GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            DisplaySyntax(client);
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;

                    case "resource3":

                        if (moneyNpc != null)
                        {
                            if (args.Length == 4)
                            {
                                if (int.TryParse(args[3], out int amount))
                                {
                                    moneyNpc.RequiredResource3 = amount;
                                    moneyNpc.SaveIntoDatabase();
                                    client.Out.SendMessage("La somme requise de l'event " + (moneyNpc.ServingEventID ?? string.Empty) + " est maintenant de :" + Money.GetString(moneyNpc.CurrentMoney), GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            DisplaySyntax(client);
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;

                    case "eventid":

                        if (client.Player.TargetObject is MoneyEventNPC eventNpc)
                        {
                            if (args.Length == 3)
                            {
                                string id = args[2];
                                eventNpc.ServingEventID = id;
                                eventNpc.SaveIntoDatabase();
                                client.Out.SendMessage("Le NPC sert maintenant l'Event ID:" + id, GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                DisplaySyntax(client);
                            }
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }

                        break;

                    case "reset":

                        if (client.Player.TargetObject is MoneyEventNPC resetNPC)
                        {
                            resetNPC.CurrentCopper = 0;
                            resetNPC.CurrentSilver = 0;
                            resetNPC.CurrentGold = 0;
                            resetNPC.CurrentMithril = 0;
                            resetNPC.CurrentPlatinum = 0;
                            resetNPC.SaveIntoDatabase();
                            client.Out.SendMessage("La somme en cours du NPC :" + resetNPC.Name + " est remise à zero", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            ShowWrongTarget(client);
                        }
                        break;


                    default:
                        DisplaySyntax(client);
                        break;
                }
            }
        }

        private void ShowWrongTarget(GameClient client)
        {
            client.Out.SendMessage("Vous devez avoir en Target un type de NPC: DOL.GS.MoneyEventNPC", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }

        private void ShowInfo(GameClient client, MoneyEventNPC npc)
        {
            List<string> infos = new List<string>();
            infos.Add(" -- Mob ID : " + npc.InternalID);
            infos.Add(" -- Mob Name: " + npc.Name);
            infos.Add(" -- EventID: " + npc.ServingEventID);
            infos.Add(" -- CurrentMoney Raw: " + npc.CurrentMoney);
            infos.Add(" -- Current Money: " + Money.GetString(npc.CurrentMoney));
            infos.Add(" -- RequiredMoney Raw: " + npc.RequiredMoney);
            infos.Add(" -- RequiredMoney: " + Money.GetString(npc.RequiredMoney));
            infos.Add(" -- InteractText: " + npc.InteractText);
            infos.Add(" -- NeedMoreMoneyText: " + npc.NeedMoreMoneyText);
            infos.Add(" -- ValidateText: " + npc.ValidateText);
            client.Out.SendCustomTextWindow("[ " + npc.Name + " ]", infos);
        }
    }
}