using DOL.Database;
using DOL.GameEvents;
using DOL.Geometry;
using DOL.GS;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.MobGroups;
using DOL.Territories;
using DOLDatabase.Tables;
using Microsoft.Diagnostics.Runtime;
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
        "Commands.GM.GMTerritories.Usage.Create",
        "Commands.GM.GMTerritories.Usage.CreateSub",
        "Commands.GM.GMTerritories.Usage.CreateLord",
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
                case "create":
                    {
                        if (args.Length < 5)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        areaId = args[3];
                        string name = args[2];
                        string groupId = args[4];

                        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(groupId))
                        {
                            DisplaySyntax(client);
                            break;
                        }

                        var areaDb = GameServer.Database.SelectObject<DBArea>(DB.Column("Area_ID").IsEqualTo(areaId));
                        if (areaDb == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoSuchArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            break;
                        }

                        if (!WorldMgr.Regions.TryGetValue(areaDb.Region, out GS.Region region))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.AreaBadRegion", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId, areaDb.Region);
                            return;
                        }

                        AbstractArea area = region.Zones.SelectMany(z => z.GetAreas().OfType<AbstractArea>()).FirstOrDefault(a => string.Equals(a.DbArea?.ObjectId, areaId));
                        if (area == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoSuchArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            break;
                        }

                        if (area.ZoneIn == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoZoneForArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            return;
                        }

                        if (!MobGroupManager.Instance.Groups.TryGetValue(groupId, out MobGroup group))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.BadGroupID", eChatType.CT_System, eChatLoc.CL_SystemWindow, groupId);
                            break;
                        }

                        var mobinfo = TerritoryManager.Instance.FindBossFromGroupId(groupId);
                        if (mobinfo.Error != null)
                        {
                            client.Out.SendMessage(mobinfo.Error, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        bool saved = TerritoryManager.Instance.AddTerritory(Territory.eType.Normal, area, areaId, areaDb.Region, group, mobinfo.Mob);
                        if (!saved)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.SaveFailed", eChatType.CT_System, eChatLoc.CL_SystemWindow, name);
                            break;
                        }
                        client.SendTranslation("Commands.GM.GMTerritories.Saved", eChatType.CT_System, eChatLoc.CL_SystemWindow, name, area.Description);
                        break;
                    }

                case "createsub":
                    {
                        if (args.Length < 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        areaId = args[3];
                        string name = args[2];

                        if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(name))
                        {
                            DisplaySyntax(client);
                            break;
                        }

                        var areaDb = GameServer.Database.SelectObject<DBArea>(DB.Column("Area_ID").IsEqualTo(areaId));
                        if (areaDb == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoSuchArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            break;
                        }

                        if (!WorldMgr.Regions.TryGetValue(areaDb.Region, out GS.Region region))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.AreaBadRegion", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId, areaDb.Region);
                            return;
                        }

                        AbstractArea area = region.Zones.SelectMany(z => z.GetAreas().OfType<AbstractArea>()).FirstOrDefault(a => string.Equals(a.DbArea?.ObjectId, areaId));
                        if (area == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoSuchArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            break;
                        }

                        if (area.ZoneIn == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoZoneForArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            return;
                        }

                        bool saved = TerritoryManager.Instance.AddTerritory(Territory.eType.Subterritory, area, areaId, areaDb.Region);
                        if (!saved)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.SaveFailed", eChatType.CT_System, eChatLoc.CL_SystemWindow, name);
                            break;
                        }
                        client.SendTranslation("Commands.GM.GMTerritories.Saved", eChatType.CT_System, eChatLoc.CL_SystemWindow, name, area.Description);
                    }
                    break;

                case "createlord":
                    {
                        territory = TerritoryManager.GetCurrentTerritory(client.Player);
                        if (territory == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotInTerritory");
                            return;
                        }
                        if (territory.Type != Territory.eType.Subterritory)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NotASubterritory");
                            return;
                        }
                        TerritoryLord lord = new TerritoryLord();
                        lord.Name = "Lord of " + territory.Name;
                        lord.CurrentRegionID = client.Player.CurrentRegionID;
                        lord.Position = client.Player.Position;
                        lord.LoadedFromScript = false;
                        lord.CurrentTerritory = territory;
                        lord.AddToWorld();
                        lord.SaveIntoDatabase();
                        territory.Boss = lord;
                        territory.BossId = lord.InternalID;
                        territory.SaveIntoDatabase();
                    }
                    break;

                case "add":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client, "add");
                            break;
                        }

                        areaId = args[2];
                        var areaDb = GameServer.Database.SelectObject<DBArea>(DB.Column("Area_Id").IsEqualTo(args[2]));
                        if (areaDb == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.NoSuchArea", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId);
                            break;
                        }

                        if (args.Length > 3)
                        {
                            if (!string.Equals(args[3], "to") || args.Length != 5)
                            {
                                DisplaySyntax(client, "add");
                                break;
                            }

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

                        if (areaDb.Region != territory.RegionId)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.Add.WrongRegion", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.RegionId, areaDb.Region);
                            return;
                        }

                        if (!WorldMgr.Regions.TryGetValue(areaDb.Region, out GS.Region region))
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.AreaBadRegion", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId, areaDb.Region);
                            return;
                        }

                        var area = territory.Zone.GetAreas().OfType<AbstractArea>().FirstOrDefault(a => string.Equals(a.DbArea?.ObjectId, areaId));
                        if (area == null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.Add.WrongZone", eChatType.CT_System, eChatLoc.CL_SystemWindow, areaId, territory.Name, territory.Zone.ID);
                            return;
                        }
                        var territoryAtArea = TerritoryManager.GetTerritoryAtArea(area);
                        if (territoryAtArea != null)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.Add.AlreadyHas", eChatType.CT_System, eChatLoc.CL_SystemWindow, area.Description, territoryAtArea.Name);
                            return;
                        }
                        territory.AddArea(area);
                        Guild oldGuild = territory.OwnerGuild;
                        if (oldGuild != null)
                        {
                            // Quick way to refresh guild names, emblems, etc
                            territory.OwnerGuild = null;
                            territory.OwnerGuild = oldGuild;
                        }
                        territory.SaveIntoDatabase();
                        client.SendTranslation("Commands.GM.GMTerritories.Add.Added", eChatType.CT_System, eChatLoc.CL_SystemWindow, area.Description, territory.Name);
                        break;
                    }

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

                case "expiration":
                    {
                        if (args.Length < 3 || !long.TryParse(args[2], out long minutes))
                        {
                            DisplaySyntax(client, "expiration");
                            return;
                        }
                        if (args.Length > 3)
                        {
                            string id = args[3];
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
                        territory.Expiration = minutes;
                        if (minutes == 0)
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.ExpirationRemoved", eChatType.CT_System, eChatLoc.CL_ChatWindow, territory.Name);
                        }
                        else
                        {
                            client.SendTranslation("Commands.GM.GMTerritories.ExpirationSet", eChatType.CT_System, eChatLoc.CL_ChatWindow, territory.Name, LanguageMgr.TranslateTimeShort(client, 0, (int)minutes));
                        }
                        territory.SaveIntoDatabase();
                    }
                    break;

                case "setportal":
                    {
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
                            var playerPosition = client.Player.Position;
                            position = new Vector3((int)playerPosition.X, (int)playerPosition.Y, (int)playerPosition.Z);
                        }
                        territory.PortalPosition = position;
                        territory.SaveIntoDatabase();
                        client.SendTranslation("Commands.GM.GMTerritories.PortalSet", eChatType.CT_System, eChatLoc.CL_SystemWindow, territory.Name, position.X, position.Y, position.Z);
                    }
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