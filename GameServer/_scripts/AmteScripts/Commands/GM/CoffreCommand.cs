using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Commands;
using System.Linq;

namespace DOL.GS.Scripts
{
	[CmdAttribute(
		 "&coffre",
		 ePrivLevel.GM,
		 "Gestions des coffres",
		 "'/coffre create' créé un nouveau coffre (100% chance d'apparition, 1h d'intervalle entre les items)",
		 "'/coffre model <model>' change le skin du coffre selectionné",
		 "'/coffre item <chance> <interval>' change le nombre de chance d'apparition d'un item, interval d'apparition d'un item en minutes",
		 "'/coffre add <id_nb> <chance>' ajoute ou modifie un item (id_nb) avec son taux de chance d'apparition au coffre selectionné",
		 "'/coffre remove <id_nb>' retire un item (id_nb) du coffre selectionné",
		 "'/coffre name <name>' change le nom du coffre selectionné",
		 "'/coffre movehere' déplace le coffre selectionné à votre position",
		 "'/coffre delete' supprime le coffre selectionné",
		 "'/coffre reset' remet à zero la derniere fois que le coffre a été ouvert",
		 "'/coffre info' donne toutes les informations du coffre selectionné",
		 "'/coffre copy' copie le coffre selectionné à votre position",
		 "'/coffre randomcopy' copie le coffre selectionné à votre position mais change les valeurs de plus ou moin 10%",
		 "'/coffre key <id_nb>' Id_nb de la clef necessaire à l'ouverture du coffre (\"nokey\" pour retirer la clé)",
         "'/coffre difficult <difficulté>' difficulté pour crocheter le coffre (en %) si 0, le coffre ne peut pas être crocheté",
         "'/coffre traprate <value>' Set la valeur du TrapRate, qui est le pourcentage de faire pop un mob",
         "'/coffre npctemplate <value>' Set le npctemplate associé au pop mob de ce coffre",
         "'/coffre respawn <name>' Respawn un coffre en donnant son name (reset du timer a 0)",
         "'/coffre isteleporter' Alterne l'etat IsTeleporter du coffre",
         "'/coffre teleporter <X> <Y> <Z> <heading> <RegionID>' Définit la destination du Téléporteur de ce coffre",
         "'/coffre tprequirement <level>' Definit le Level minimum pour pouvoir utiliser le Téléporteur de ce coffre",
         "'/coffre tpeffect <SpellID>' Definit l'effect utilisé par la téléportation de ce coffre basé sur son SpellID",
         "'/coffre tpisrenaissance' Alterne l'état IsRenaissance du coffre",
         "'/coffre isOpeningRenaissance' Alterne l'état isOpeningRenaissanceType du coffre",
         "'/coffre punishSpellId <SpellId>' Définit le SpellID pour punir le joueur si il n'est pas Isrenaissance",
         "'/coffre pickableAnim' Alterne l'état de HasPickableAnim ou Activer ou désactiver l'emote pickup",
         "'/coffre interval <minutes>' Change l'interval d'ouverture d'un coffre en minutes",
         "'/coffre longdistance <true|false>' Change la distance d'interraction du coffre. (utile pour les gros coffres)")]
    public class CoffreCommandHandler : AbstractCommandHandler, ICommandHandler
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

			GameCoffre coffre = player.TargetObject as GameCoffre;
			switch(args[1].ToLower())
			{
					#region create - model
				case "create":
					coffre = new GameCoffre
					         	{
					         		Name = "Coffre",
					         		Model = 1596,
					                Position = player.Position,
					         		Heading = player.Heading,
					         		CurrentRegionID = player.CurrentRegionID,
					         		ItemInterval = 60,
					         		ItemChance = 100
					         	};
                    coffre.InitTimer();
                    coffre.LoadedFromScript = false;
					coffre.AddToWorld();
					coffre.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Vous avez créé un coffre (OID:" + coffre.ObjectID + ")");
					break;

				case "model":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						coffre.Model = (ushort)int.Parse(args[2]);
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le model " + coffre.Model);
					break;
					#endregion

                case "interval":
                    int min = 0;

                    if (coffre != null && args.Length == 3 && int.TryParse(args[2], out min) && min >= 0)
                    {
                        coffre.CoffreOpeningInterval = min;
                        coffre.SaveIntoDatabase();
                        ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant l'interval d'ouverture de " + min + " minutes");
                    }
                    else
                    {
                        DisplaySyntax(client);                        
                    }

                    break;

                case "longdistance":
                    bool isLongDitance = false;

                    if (coffre != null && args.Length == 3 && bool.TryParse(args[2], out isLongDitance))
                    {
                        coffre.IsLargeCoffre = isLongDitance;
                        coffre.SaveIntoDatabase();
                        ChatUtil.SendSystemMessage(client, "La valeur LongDistance du coffre \"" + coffre.Name + " est maintenant de: " + isLongDitance);
                    }
                    else
                    {
                        DisplaySyntax(client);
                    }

                    break;

					#region item - add - remove
				case "item":
					if(coffre == null || args.Length < 4)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						coffre.ItemChance = int.Parse(args[2]);
						coffre.ItemInterval = int.Parse(args[3]);
						coffre.InitTimer();
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" a maintenant " + coffre.ItemChance + " chances de faire apparaitre un item toutes les " + coffre.ItemInterval + " minutes.");
					break;

				case "add":
					if(coffre == null || args.Length < 4)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						if(!coffre.ModifyItemList(args[2], int.Parse(args[3])))
						{
							ChatUtil.SendSystemMessage(client, "L'item \""+args[2]+"\" n'existe pas !");
							break;
						}
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" peut faire apparaitre un item \""+args[2]+"\" avec un taux de chance de "+args[3]);
					break;
				case "remove":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						if(!coffre.DeleteItemFromItemList(args[2]))
						{
							ChatUtil.SendSystemMessage(client, "L'item \""+args[2]+"\" n'existe pas !");
							break;
						}
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" ne peut plus faire apparaitre l'item \""+args[2]+"\"");
					break;
					#endregion

					#region name - movehere - delete
				case "name":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.Name = args[2];
					coffre.SaveIntoDatabase();
					break;

				case "movehere":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}

					coffre.Position = player.Position;
					coffre.Heading = player.Heading;
					coffre.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été déplacé à votre position.");
					break;

				case "delete":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.Delete();
					coffre.DeleteFromDatabase();
					ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été supprimé.");
					break;
					#endregion

					#region reset - info
				case "reset":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					coffre.LastOpen = DateTime.MinValue;
					coffre.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été remit à zéro.");
					break;

				case "info":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}
					player.Out.SendCustomTextWindow(coffre.Name, coffre.DelveInfo());
					break;
					#endregion

					#region copy - randomcopy
				case "randomcopy":
				case "copy":
					if(coffre == null)
					{
						DisplaySyntax(client);
						break;
					}

					GameCoffre coffre2;
					if(args[1].ToLower() == "randomcopy")
					{
						List<GameCoffre.CoffreItem> items = new List<GameCoffre.CoffreItem>(coffre.Items);
						foreach(GameCoffre.CoffreItem item in items)
						{
							if(Util.Chance(50))
								item.Chance += (int)(item.Chance * Util.RandomDouble() / 10);
							else if(item.Chance > 1)
							{
								item.Chance -= (int)(item.Chance * Util.RandomDouble() / 10);
								if(item.Chance < 1)
									item.Chance = 1;
							}
						}
						coffre2 = new GameCoffre(items);
					}
					else
						coffre2 = new GameCoffre(coffre.Items);

					coffre2.Name = coffre.Name + "_cpy";
					coffre2.Position = player.Position;
					coffre2.Heading = player.Heading;
					coffre2.CurrentRegion = player.CurrentRegion;
					coffre2.Model = coffre.Model;
					coffre2.ItemInterval = coffre.ItemInterval;
					coffre2.TpEffect = coffre.TpEffect;
                    coffre2.TpIsRenaissance = coffre.TpIsRenaissance;
                    coffre2.TpLevelRequirement = coffre.TpLevelRequirement;
                    coffre2.TpX = coffre.TpX;
                    coffre2.TpY = coffre.TpY;
                    coffre2.TpZ = coffre.TpZ;
                    coffre2.TpRegion = coffre.TpRegion;
                    coffre2.TrapRate = coffre.TrapRate;
                    coffre2.NpctemplateId = coffre.NpctemplateId;
                    coffre2.PunishSpellId = coffre.PunishSpellId;
                    coffre2.IsOpeningRenaissanceType = coffre.IsOpeningRenaissanceType;
                    coffre2.IsTeleporter = coffre.IsTeleporter;
                    coffre2.InitTimer();    

					coffre2.ItemChance = coffre.ItemChance;
					if (args[1].ToLower() == "randomcopy")
					{
						if (Util.Chance(50))
							coffre2.ItemInterval += (int) (coffre2.ItemInterval*Util.RandomDouble()/10);
						else if (coffre2.ItemInterval > 1)
						{
							coffre2.ItemInterval -= (int) (coffre2.ItemInterval*Util.RandomDouble()/10);
							if (coffre2.ItemInterval < 1)
								coffre2.ItemInterval = 1;
						}
						if (Util.Chance(50) && coffre2.ItemChance < 100)
						{
							coffre2.ItemChance += (int) (coffre2.ItemChance*Util.RandomDouble()/10);
							if (coffre2.ItemChance > 100)
								coffre2.ItemChance = 100;
						}
						else if (coffre2.ItemChance > 0)
						{
							coffre2.ItemChance -= (int) (coffre2.ItemChance*Util.RandomDouble()/10);
							if (coffre2.ItemChance < 0)
								coffre2.ItemChance = 0;
						}
					}
					coffre2.AddToWorld();
					coffre2.SaveIntoDatabase();
					ChatUtil.SendSystemMessage(client, "Vous avez créé un coffre (OID:"+coffre2.ObjectID+")");
					break;
					#endregion

					#region key - difficult
				case "key":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					if(args[2].ToLower() == "nokey")
						coffre.KeyItem = "";
					else
					{
						if (GameServer.Database.FindObjectByKey<ItemTemplate>(args[2]) == null)
						{
							ChatUtil.SendSystemMessage(client, "La clef ayant l'id_nb \"" + args[2] + "\" n'existe pas.");
							break;
						}
						coffre.KeyItem = args[2];
					}
					coffre.SaveIntoDatabase();

					if(coffre.KeyItem == "")
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" n'a plus besoin de clef pour être ouvert.");
					else
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" a besoin de la clef ayant l'id_nb \""+coffre.KeyItem+"\" pour être ouvert.");
					break;

				case "difficult":
					if(coffre == null || args.Length < 3)
					{
						DisplaySyntax(client);
						break;
					}
					try
					{
						int num = int.Parse(args[2]);
						if(num >= 100)
							num = 99;
						if(num < 0)
							num = 0;
						coffre.LockDifficult = num;
						coffre.SaveIntoDatabase();
					}
					catch
					{
						DisplaySyntax(client);
						break;
					}
					if(coffre.LockDifficult > 0)
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" a maintenant une difficulté pour être crocheter de "+coffre.LockDifficult+"%.");
					else
						ChatUtil.SendSystemMessage(client, "Le coffre \""+coffre.Name+"\" ne peut plus être crocheté.");
					break;

                case "traprate":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.TrapRate = int.Parse(args[2]);
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le traprate de " + coffre.TrapRate);
                    break;

                case "npctemplate":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.NpctemplateId = args[2];
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le npctemplate de " + coffre.NpctemplateId);
                    break;

                case "isteleporter":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.IsTeleporter = !coffre.IsTeleporter;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut IsTeleporter du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.IsTeleporter);
                    break;

                case "pickableanim":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.HasPickableAnim = !coffre.HasPickableAnim;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut HasPickbleAnim du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.HasPickableAnim);
                    break;

                case "isopeningrenaissance":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.IsOpeningRenaissanceType = !coffre.IsOpeningRenaissanceType;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut IsOpeningRenaissanceType du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.IsOpeningRenaissanceType);
                    break;

                case "respawn":
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    GameCoffre selectedCoffre = null;
                    try
                    {
                        string name = args[2];

                        if (name != null)
                        {
                            var coffres = GameCoffre.Coffres.Where(c => c.Name.Equals(name));

                            if (coffres != null && coffres.Count() == 1)
                            {
                                selectedCoffre = coffres.First();
                            }                           
                        }                   

                        if (selectedCoffre == null)
                        {
                            ChatUtil.SendSystemMessage(client, "Le coffre \"" + name + "\" n'a pas été trouvé ou plusieurs coffres avec le meme nom existent.");
                            break;
                        }
                        else
                        {
                            selectedCoffre.RespawnTimer.Stop();
                            selectedCoffre.RespawnCoffre();
                        }                     
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + selectedCoffre.Name + "\" apparait devant vous.");
                    break;

                case "teleporter":
                    if (coffre == null || args.Length <= 6)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    int X;
                    int Y;
                    int Z;
                    ushort heading;
                    ushort RegionID;
                    try
                    {
                        X = int.Parse(args[2]);
                        Y = int.Parse(args[3]);
                        Z = int.Parse(args[4]);         
                        heading = (ushort)int.Parse(args[5]);
                        RegionID = (ushort)int.Parse(args[6]);   
                    }
                    catch { DisplaySyntax(client); return; }

                    coffre.TpX = X;
                    coffre.TpY = Y;
                    coffre.TpZ = Z;
                    coffre.TpRegion = RegionID;
                    coffre.TPHeading = heading;
                    coffre.SaveIntoDatabase();
                    player.Out.SendMessage("Le Coffre \"" + coffre + "\" a recu une nouvelle destination de téléportation",  PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);

                    if (!coffre.IsTeleporter)
                    {
                        player.Out.SendMessage("Le statut IsTeleporter du Coffre \"" + coffre + "\" n'est pas activé, changez le pour activer la téléportation.", PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                    }
                    break;

                case "tprequirement":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        int level = int.Parse(args[2]);
                        if (level < 1)
                        {
                            level = 1;
                        }

                        coffre.TpLevelRequirement = level;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le Niveau minimum pour utiliser le téléporteur du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.TpLevelRequirement);
                    break;

                case "punishspellid":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        int spellID = int.Parse(args[2]);   
                        coffre.PunishSpellId = spellID;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le PunishSpellID du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.PunishSpellId);
                    break;

                case "tpeffect":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {            
                        coffre.TpEffect = int.Parse(args[2]);
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "L'effet utilisé pour le téléporteur du coffre \"" + coffre.Name + "\" est maitenant lié au SpellID: " + coffre.TpEffect);
                    break;

                case "tpisrenaissance":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.TpIsRenaissance = !coffre.TpIsRenaissance;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut IsRenaissance du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.TpIsRenaissance);
                    break;
					#endregion
			}
		}
	}
}
