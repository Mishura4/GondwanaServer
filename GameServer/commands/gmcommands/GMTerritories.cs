using DOL.Database;
using DOL.GameEvents;
using DOL.Geometry;
using DOL.GS;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.MobGroups;
using DOL.Territories;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector3 = System.Numerics.Vector3;

namespace DOL.commands.gmcommands
{
    [CmdAttribute(
        "&GMTerritories",
        ePrivLevel.GM,
        "Commands.GM.GMTerritories.Description",
        "Commands.GM.GMTerritories.Usage.Add",
        "Commands.GM.GMTerritories.Usage.Info",
        "Commands.GM.GMTerritories.Usage.Clear",
        "Commands.GM.GMTerritories.Usage.Claim",
        "Commands.GM.GMTerritories.Usage.SetPortal",
        "Commands.GM.GMTerritories.Usage.BonusAdd",
        "Commands.GM.GMTerritories.Usage.BonusRemove",
         "Commands.GM.GMTerritories.Resist")]
    public class GMTerritoires
        : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            Territories.Territory territory = null;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }
            string areaId = null;


            switch (args[1].ToLowerInvariant())
            {
                case "add":

                    if (args.Length < 6)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    areaId = args[2];
                    ushort zoneId = 0;
                    string name = args[4];
                    string groupId = args[5];

                    if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(groupId))
                    {
                        DisplaySyntax(client);
                        break;
                    }

                    if (ushort.TryParse(args[3], out zoneId))
                    {
                        var areaDb = GameServer.Database.SelectObjects<DBArea>(DB.Column("id").IsEqualTo(areaId))?.FirstOrDefault();

                        if (areaDb == null)
                        {
                            client.Out.SendMessage("Impossible de trouver l'area avec l'id : " + areaId, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        if (!WorldMgr.Regions.ContainsKey(areaDb.Region))
                        {
                            client.Out.SendMessage("Impossible de trouver la region : " + areaDb.Region, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        var zone = WorldMgr.Regions[areaDb.Region].Zones.FirstOrDefault(z => z.ID.Equals(zoneId));

                        if (zone == null)
                        {
                            client.Out.SendMessage("Impossible de trouver la zone : " + zoneId + " dans la region " + areaDb.Region, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        if (!MobGroupManager.Instance.Groups.ContainsKey(groupId))
                        {
                            client.Out.SendMessage("Impossible de trouver le groupeId : " + groupId, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        var areas = WorldMgr.Regions[areaDb.Region].GetAreasOfZone(zone, new System.Numerics.Vector3(areaDb.X, areaDb.Y, 0), false);

                        if (areas == null)
                        {
                            client.Out.SendMessage("Impossible de trouver des areas dans la zone : " + zone.ID, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        var area = areas.FirstOrDefault(a => ((AbstractArea)a).Description.Equals(areaDb.Description));

                        if (area == null)
                        {
                            client.Out.SendMessage($"Impossible de trouver des l'area {areaDb.Description} dans la zone {zone.ID}", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        var mobinfo = TerritoryManager.Instance.FindBossFromGroupId(groupId);

                        if (mobinfo.Error != null)
                        {
                            client.Out.SendMessage(mobinfo.Error, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        bool saved = TerritoryManager.Instance.AddTerritory(area, areaId, areaDb.Region, groupId, mobinfo.Mob);

                        if (!saved)
                        {
                            client.Out.SendMessage("Le Territoire " + name + " n'a pas pu etre sauvegardé dans la base de données.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        client.Out.SendMessage("Le Territoire " + name + " a été créé correctement.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }

                    break;

                case "info":
                    if (args.Length > 2)
                    {
                        string id = args[2];
                        territory = TerritoryManager.GetTerritoryByID(id);
                        if (territory == null)
                        {
                            territory = TerritoryManager.GetTerritoryAtArea(id);
                            if (territory == null)
                            {
                                client.SendTranslation("Commands.GM.GMTerritories.TerritoryNotFound", eChatType.CT_System, eChatLoc.CL_SystemWindow, id);
                                return;
                            }
                        }
                    }
                    else
                    {
                        territory = TerritoryManager.GetCurrentTerritory(client.Player);
                        if (territory == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotInTerritory");
                            return;
                        }
                    }

                    IList<string> infos = territory.GetInformations();

                    client.Out.SendCustomTextWindow($"[ Territoire {territory.Name} ]", infos);
                    break;

                case "setportal":
                    int arg = 2;
                    if (args.Length > arg)
                    {
                        string id = args[arg];
                        territory = TerritoryManager.GetTerritoryByID(id);
                        if (territory == null)
                        {
                            territory = TerritoryManager.GetTerritoryAtArea(id);
                            if (territory == null)
                            {
                                client.SendTranslation("Commands.GM.GMTerritories.TerritoryNotFound", eChatType.CT_System, eChatLoc.CL_SystemWindow, id);
                                return;
                            }
                        }
                        ++arg;
                    }
                    else
                    {
                        territory = TerritoryManager.GetCurrentTerritory(client.Player);
                        if (territory == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotInTerritory");
                            return;
                        }
                    }
                    Vector3 position;
                    if (args.Length > arg)
                    {
                        if (string.Equals(args[arg], "remove"))
                        {
                            territory.PortalPosition = null;
                            client.SendTranslation("Commands.GM.GMTerritories.PortalRemoved", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.Name);
                            return;
                        }
                        if (args.Length < arg + 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        if (!int.TryParse(args[arg], out int x))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.BadCoordinate", eChatType.CT_System, eChatLoc.CL_SystemWindow, args[arg]);
                            return;
                        }
                        if (!int.TryParse(args[arg + 1], out int y))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.BadCoordinate", eChatType.CT_System, eChatLoc.CL_SystemWindow, args[arg + 1]);
                            return;
                        }
                        if (!int.TryParse(args[arg + 2], out int z))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.BadCoordinate", eChatType.CT_System, eChatLoc.CL_SystemWindow, args[arg + 2]);
                            return;
                        }
                        position = new Vector3(x, y, z);
                    }
                    else
                    {
                        position = client.Player.Position;
                    }
                    territory.PortalPosition = position;
                    client.SendTranslation("Commands.GM.GMTerritories.PortalSet", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.Name, position.X, position.Y, position.Z);
                    break;

                case "clear":
                    if (args.Length > 2)
                    {
                        string id = args[2];
                        territory = TerritoryManager.GetTerritoryByID(id);
                        if (territory == null)
                        {
                            territory = TerritoryManager.GetTerritoryAtArea(id);
                            if (territory == null)
                            {
                                client.SendTranslation("Commands.GM.GMTerritories.TerritoryNotFound", eChatType.CT_System, eChatLoc.CL_SystemWindow, id);
                                return;
                            }
                        }
                    }
                    else
                    {
                        territory = TerritoryManager.GetCurrentTerritory(client.Player);
                        if (territory == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotInTerritory");
                            return;
                        }
                    }

                    territory.OwnerGuild = null;
                    client.SendTranslation("Commands.GM.GMTerritories.Cleared", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.Name);
                    break;

                case "claim":
                    if (args.Length > 2)
                    {
                        string id = args[2];
                        territory = TerritoryManager.GetTerritoryByID(id);
                        if (territory == null)
                        {
                            territory = TerritoryManager.GetTerritoryAtArea(id);
                            if (territory == null)
                            {
                                client.SendTranslation("Commands.GM.GMTerritories.TerritoryNotFound", eChatType.CT_System, eChatLoc.CL_SystemWindow, id);
                                return;
                            }
                        }
                    }
                    else
                    {
                        territory = TerritoryManager.GetCurrentTerritory(client.Player);
                        if (territory == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotInTerritory");
                            return;
                        }
                    }

                    territory.OwnerGuild = client.Player.Guild;
                    if (client.Player.Guild == null)
                    {
                        client.SendTranslation("Commands.GM.GMTerritories.Cleared", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.Name);
                    }
                    else
                    {
                        client.SendTranslation("Commands.GM.GMTerritories.Claimed", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.Name, client.Player.Guild.Name);
                    }
                    break;

                case "bonus":

                    if (args.Length < 4)
                    {
                        DisplaySyntax(client);
                        break;
                    }

                    string action = args[2].ToLowerInvariant();
                    var arg3 = args[3].ToLowerInvariant();
                    var bonus = this.GetResist(arg3);
                    if (args.Length > 4)
                    {
                        string id = args[4];
                        territory = TerritoryManager.GetTerritoryByID(id);
                        if (territory == null)
                        {
                            territory = TerritoryManager.GetTerritoryAtArea(id);
                            if (territory == null)
                            {
                                client.SendTranslation("Commands.GM.GMTerritories.TerritoryNotFound", eChatType.CT_System, eChatLoc.CL_SystemWindow, id);
                                return;
                            }
                        }
                    }
                    else
                    {
                        territory = TerritoryManager.GetCurrentTerritory(client.Player);
                        if (territory == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotInTerritory");
                            return;
                        }
                    }

                    if (action == "add")
                    {
                        if (bonus != null)
                        {
                            int current = 0;
                            territory.BonusResist.TryGetValue(bonus.Value, out current);
                            territory.BonusResist[bonus.Value] = current + 1;
                        }
                        else switch (arg3)
                        {
                            case "melee":
                                territory.BonusMeleeAbsorption += 1;
                                break;

                            case "spell":
                                territory.BonusSpellAbsorption += 1;
                                break;

                            case "dot":
                                territory.BonusDoTAbsorption += 1;
                                break;

                            case "debuffduration":
                                territory.BonusReducedDebuffDuration += 1;
                                break;

                            case "spellrange":
                                territory.BonusSpellRange += 1;
                                break;

                            default:
                                DisplaySyntax(client);
                                return;
                        }

                        if (territory.OwnerGuild != null)
                        {
                            territory.OwnerGuild.RemoveTerritory(territory);
                            territory.OwnerGuild.AddTerritory(territory, true);
                        }

                        territory.SaveIntoDatabase();

                        client.Out.SendMessage("Le Territoire avec AreaId " + areaId + " a désormais un bonus supplémentaire de " + (bonus?.ToString() ?? arg3), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        break;
                    }
                    else if (action == "remove")
                    {
                        int index;

                        if (bonus != null)
                        {
                            if (!territory.BonusResist.TryGetValue(bonus.Value, out int current))
                                return;

                            if (current == 1)
                            {
                                territory.BonusResist.Remove(bonus.Value);
                            }
                            else
                                territory.BonusResist[bonus.Value] = current - 1;
                        }
                        else switch (arg3)
                        {
                            case "melee":
                                territory.BonusMeleeAbsorption -= 1;
                                break;

                            case "spell":
                                territory.BonusSpellAbsorption -= 1;
break;

                            case "dot":
                                territory.BonusDoTAbsorption -= 1;
                                break;

                            case "debuffduration":
                                territory.BonusReducedDebuffDuration -= 1;
                                break;

                            case "spellrange":
                                territory.BonusSpellRange -= 1;
                                break;

                            default:
                                DisplaySyntax(client);
                                return;
                        }

                        if (territory.OwnerGuild != null)
                        {
                            territory.OwnerGuild.RemoveTerritory(territory);
                            territory.OwnerGuild.AddTerritory(territory, true);
                        }

                        territory.SaveIntoDatabase();

                        client.Out.SendMessage("Le bonus de " + (bonus?.ToString() ?? arg3) + " a été retiré de Territoire avec AreaId " + areaId, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }

                    break;

                /*  <nature|crush|slash|thrust|body|cold|energy|heat|matter|spirit>
                 * 	Natural = eProperty.Resist_Natural,
                    Crush = eProperty.Resist_Crush,
                    Slash = eProperty.Resist_Slash,
                    Thrust = eProperty.Resist_Thrust,
                    Body = eProperty.Resist_Body,
                    Cold = eProperty.Resist_Cold,
                    Energy = eProperty.Resist_Energy,
                    Heat = eProperty.Resist_Heat,
                    Matter = eProperty.Resist_Matter,
                    Spirit = eProperty.Resist_Spirit
                 *
                 * */


                default:
                    DisplaySyntax(client);
                    break;
            }
        }


        private eResist? GetResist(string resist)
        {
            switch (resist)
            {

                case "nature": return eResist.Natural;
                case "crush": return eResist.Crush;
                case "slash": return eResist.Slash;
                case "thrust": return eResist.Thrust;
                case "body": return eResist.Body;
                case "cold": return eResist.Cold;
                case "energy": return eResist.Energy;
                case "heat": return eResist.Heat;
                case "matter": return eResist.Matter;
                case "spirit": return eResist.Spirit;

                default:
                    return null;
            }
        }
    }
}