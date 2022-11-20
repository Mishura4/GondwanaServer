using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		"&echangeurnpc",
		ePrivLevel.GM,
		"Gestions des echangeurs (TextNPC)",
		"'/echangeur add <nombre d'item> <ID de l'item a donner>' Ajoute un item que le pnj accepte",
		"'/echangeur remove <ID de l'item a donner>' Retire un item",
		"'/echangeur money <ID de l'item a donner> <quantité>' Echange de l'argent",
		"'/echangeur xp <ID de l'item a donner> <quantité>' Mettre une quantité négative pour un pourmille (xp/1000) suivant le niveau du joueur",
		"'/echangeur item <ID de l'item a donner> <nombre d'item final> <ID de l'item final>' Echange des objets",
		"'/echangeur info' affiche les infos de l'échangeur",
		"'/echangeur pricemoney <ID de l'item a donner> <gold>' Définit la valeur en <gold> pour valider l'échange",
        "'/echangeur priceressource1 <ID de l'item a donner> <item_id> <nombre>' Définit l'objet requis par son <item_id> et le <nombre> requis pour la ressource 1",
        "'/echangeur priceressource2 <ID de l'item a donner> <item_id> <nombre>' Définit l'objet requis par son <item_id> et le <nombre> requis pour la ressource 2",
        "'/echangeur priceressource3 <ID de l'item a donner> <item_id> <nombre>' Définit l'objet requis par son <item_id> et le <nombre> requis pour la ressource 3",
        "'/echangeur priceressource remove <1|2|3> <ID de l'item a donner>' Supprime la condition de Price Ressource numéro <1|2|3> pour l'item à donner.",
		"Pour la réponse, ajoutez une réponse avec l'ID de l'item à donner grace aux commandes des textnpc")]
	public class EchangeurNPCCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if(client.Player == null) return;
			GamePlayer player = client.Player;

			if(args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			ITextNPC npc = player.TargetObject as ITextNPC;
			string item;
			switch (args[1].ToLower())
			{
				case "add":
					if (npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					int amount;
					if (!int.TryParse(args[2], out amount) || amount <= 0)
					{
						DisplaySyntax(client);
						return;
					}
					item = string.Join(" ", args, 3, args.Length - 3);
					if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
					{
                        npc.TextNPCData.EchangeurDB[item] = new DBEchangeur
						                        	{
						                        		ItemRecvCount = amount,
						                        		ItemRecvID = item,
						                        		ItemGiveCount = 0,
						                        		ItemGiveID = "",
						                        		GainMoney = 0,
						                        		GainXP = 0
						                        	};
                        npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage(item + " a été ajouté.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else
						player.Out.SendMessage(item + " existe déjà.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				case "remove":
					if (npc == null || args.Length < 3)
					{
						DisplaySyntax(client);
						return;
					}
					item = string.Join(" ", args, 2, args.Length - 2);
                    if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
						player.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
					{
                        DBEchangeur db = npc.TextNPCData.EchangeurDB[item];
                        npc.TextNPCData.EchangeurDB.Remove(item);
						GameServer.Database.DeleteObject(db);
                        if (npc.TextNPCData.Reponses.ContainsKey(item))
                            npc.TextNPCData.Reponses.Remove(item);
                        npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage(item + " a été retiré.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					break;

				case "money":
					if (npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					item = args[2];
					long money;
					if (!long.TryParse(args[3], out money) || money < 0)
					{
						DisplaySyntax(client);
						return;
					}
                    if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
						player.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
					{
                        npc.TextNPCData.EchangeurDB[item].GainMoney = money;
                        npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage(item + " donne " + Money.GetString(money) + " maintenant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					break;

				case "xp":
					if (npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					item = args[2];
					int xp;
					if (!int.TryParse(args[3], out xp))
					{
						DisplaySyntax(client);
						return;
					}
                    if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
						player.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
					{
                        npc.TextNPCData.EchangeurDB[item].GainXP = xp;
                        npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage(item + " donne " + (xp > 0 ? xp + " xp" : (-xp)+"/1000 du niveau en cours") + " maintenant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					break;

				case "item":
					if (npc == null || args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					item = args[2];
					string item2 = args[4];
					int count;
					if (!int.TryParse(args[3], out count) || count < 0)
					{
						DisplaySyntax(client);
						return;
					}
                    if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
						player.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					else
					{
                        npc.TextNPCData.EchangeurDB[item].ItemGiveCount = count;
                        npc.TextNPCData.EchangeurDB[item].ItemGiveID = item2;
                        npc.TextNPCData.SaveIntoDatabase();
						player.Out.SendMessage(item + " donne " + count + " " + item2 + " maintenant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					break;

				case "info":
					if (npc == null)
					{
						DisplaySyntax(client);
						return;
					}
					List<string> text = new List<string>();
                    foreach (KeyValuePair<string, DBEchangeur> pair in npc.TextNPCData.EchangeurDB)
					{
						text.Add(" - " + pair.Value.ItemRecvID + " (" + pair.Value.ItemRecvCount +"):");
						if (pair.Value.ItemGiveCount > 0)
							text.Add("     " + pair.Value.ItemGiveCount + " " + pair.Value.ItemGiveID);
						if (pair.Value.GainMoney > 0)
							text.Add("     " + Money.GetString(pair.Value.GainMoney));
						if (pair.Value.GainXP > 0)
							text.Add("     " + pair.Value.GainXP + "xp");
						if (pair.Value.GainXP < 0)
							text.Add("     " + (-pair.Value.GainXP) + "/1000 du niveau en cours");
                        if (npc.TextNPCData.Reponses.ContainsKey(pair.Value.ItemRecvID))
                            text.Add("     Réponse: " + npc.TextNPCData.Reponses[pair.Value.ItemRecvID]);
						text.Add(" . " + pair.Value.ChangedItemCount + " Items échangés");

                        text.Add(" PriceMoney: " + pair.Value.MoneyPrice + " or");
                        if (!string.IsNullOrEmpty(pair.Value.PriceRessource1))
                        {
                            var val1 = pair.Value.PriceRessource1.Split(new char[] { '|' });
                            if (val1.Length == 2)
                                text.Add(" PriceRessource1:  " + val1[1] + " " + val1[0]);
                        }
                        if (!string.IsNullOrEmpty(pair.Value.PriceRessource2))
                        {
                            var val2 = pair.Value.PriceRessource2.Split(new char[] { '|' });
                            if (val2.Length == 2)
                                text.Add(" PriceRessource2:  " + val2[1] + " " + val2[0]);
                        }
                        if (!string.IsNullOrEmpty(pair.Value.PriceRessource3))
                        {
                            var val3 = pair.Value.PriceRessource3.Split(new char[] { '|' });
                            if (val3.Length == 2)
                                text.Add(" PriceRessource3:  " + val3[1] + " " + val3[0]);
                        }


					}
					player.Out.SendCustomTextWindow("Info " + ((GameNPC)npc).Name, text);
					break;

                case "pricemoney":
                    if (npc == null || args.Length < 4)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    item = args[2];
                    int gold = 0;

                    if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
                        player.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else
                    {
                        if (int.TryParse(args[3], out gold))
                        {
                            npc.TextNPCData.EchangeurDB[item].MoneyPrice = gold;
                            npc.TextNPCData.SaveIntoDatabase();
                            player.Out.SendMessage("il faut désormais " + gold + " or(s) pour échanger " + item + " maintenant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            DisplaySyntax(client);
                        }
                    }

                    break;


                case "priceressource1":
                    this.ChangeRessource(1, npc, args, client);
                    break;
                case "priceressource2":
                    this.ChangeRessource(2, npc, args, client);
                    break;
                case "priceressource3":
                    this.ChangeRessource(3, npc, args, client);
                    break;


                case "priceressource":
                    if (args.Length != 5 || npc == null)
                    {
                        DisplaySyntax(client);
                        break;
                    }

                    if (args[2] != "remove")
                    {
                        DisplaySyntax(client);
                        break;
                    }

                    int num = 0;
                    if (int.TryParse(args[3], out num))
                    {
                        if (num == 1 || num == 2 || num == 3)
                        {
                            item = args[4];

                            if (!string.IsNullOrEmpty(item))
                            {
                                if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
                                    player.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                else
                                {
                                    if (num == 1)
                                    {
                                        npc.TextNPCData.EchangeurDB[item].PriceRessource1 = null;
                                    }
                                    else if (num == 2)
                                    {
                                        npc.TextNPCData.EchangeurDB[item].PriceRessource2 = null;
                                    }
                                    else
                                    {
                                        npc.TextNPCData.EchangeurDB[item].PriceRessource3 = null;
                                    }

                                    npc.TextNPCData.SaveIntoDatabase();
                                    player.Out.SendMessage("La ressource " + num + " a été supprimée pour l'objet " + item, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    break;
                                }
                            }
                        }                       
                    }

                    DisplaySyntax(client);
                    break;


				default:
					DisplaySyntax(client);
					break;
			}
			return;
		}

        private void ChangeRessource(int ressource, ITextNPC npc, string[] args, GameClient client)
        {
            if (npc == null || args.Length < 5)
            {
                DisplaySyntax(client);
                return;
            }
            string item = args[2];
            string exchangeItem = args[3];
            int itemCount = 0;

            if (string.IsNullOrEmpty(exchangeItem))
            {
                DisplaySyntax(client);
                return;
            }

            if (!npc.TextNPCData.EchangeurDB.ContainsKey(item))
                client.Out.SendMessage(item + " n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else
            {
                if (int.TryParse(args[4], out itemCount))
                {
                    if (ressource == 1)
                    {
                        npc.TextNPCData.EchangeurDB[item].PriceRessource1 = string.Format("{0}|{1}", exchangeItem, itemCount);
                    }
                    else if (ressource == 2)
                    {
                        npc.TextNPCData.EchangeurDB[item].PriceRessource2 = string.Format("{0}|{1}", exchangeItem, itemCount);
                    }
                    else
                    {
                        npc.TextNPCData.EchangeurDB[item].PriceRessource3 = string.Format("{0}|{1}", exchangeItem, itemCount);
                    }

                    npc.TextNPCData.SaveIntoDatabase();
                    client.Out.SendMessage("il faut désormais " + itemCount + " " + exchangeItem + " pour échanger " + item + " maintenant.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    DisplaySyntax(client);
                }
            }
        }

		public override void DisplaySyntax(GameClient client)
		{
			if(client==null || !client.IsPlaying)
				return;
			CmdAttribute[] attrib = (CmdAttribute[]) GetType().GetCustomAttributes(typeof(CmdAttribute), false);
			if(attrib.Length==0) 
				return;

			int i = 0;
			string lines = LanguageMgr.GetTranslation(client.Account.Language, attrib[0].Description) + "\n\n";
            foreach (string usage in attrib[0].Usage)
			{
				var str = LanguageMgr.GetTranslation(client.Account.Language, usage);
				i += str.Length;
				if(i > 2000)
				{
					client.Out.SendMessage(lines, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					lines = "";
					i = str.Length;
				}
				lines += str+"\n";
				//log.Debug(str.Length + " - " + i);
			}
			client.Out.SendMessage(lines, eChatType.CT_System, eChatLoc.CL_PopupWindow);
			return;
		}
	}
}
