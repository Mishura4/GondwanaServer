using DOL.Database;
using DOL.events.server;
using DOL.Events;
using DOL.GameEvents;
using DOL.Geometry;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.MobGroups;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.Area;
using static DOL.GS.GameObject;

namespace DOL.Territory
{
    public class TerritoryManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static TerritoryManager instance;
        public static readonly ushort NEUTRAL_EMBLEM = 256;
        private readonly string BOSS_CLASS = "DOL.GS.Scripts.TerritoryBoss";
        private readonly string GUARD_CLASS = "DOL.GS.Scripts.TerritoryGuard";
        private readonly string GUARD_BASIC_TEMPLATE = "gvg_guard_Basique";
        private static readonly int DAILY_TAX = GS.ServerProperties.Properties.DAILY_TAX;
        private static readonly int DAILY_MERIT_POINTS = GS.ServerProperties.Properties.DAILY_MERIT_POINTS;

        public static TerritoryManager Instance => instance ?? (instance = new TerritoryManager());

        public List<Territory> Territories
        {
            get;
        }

        private TerritoryManager()
        {
            this.Territories = new List<Territory>();
        }

        public bool Init()
        {
            return true;
        }


        [GameEventLoaded]
        public static void LoadTerritories(DOLEvent e, object sender, EventArgs arguments)
        {
            var values = GameServer.Database.SelectAllObjects<TerritoryDb>();
            int count = 0;

            if (values != null)
            {
                foreach (var territoryDb in values)
                {
                    if (WorldMgr.Regions.ContainsKey(territoryDb.RegionId))
                    {
                        var zone = WorldMgr.Regions[territoryDb.RegionId].Zones.FirstOrDefault(z => z.ID.Equals(territoryDb.ZoneId));

                        if (zone != null)
                        {
                            var areaDb = GameServer.Database.SelectObjects<DBArea>("area_id = @id", new QueryParameter("id", territoryDb.AreaId))?.FirstOrDefault();

                            if (areaDb == null)
                            {
                                log.Error($"Cannot find Area in Database with ID: {territoryDb.AreaId}");
                                continue;
                            }

                            var area = zone.GetAreasOfSpot(new System.Numerics.Vector3(territoryDb.AreaX, territoryDb.AreaY, 0), false)?.FirstOrDefault(a => ((AbstractArea)a).Description.Equals(areaDb.Description));

                            if (area != null)
                            {
                                var mobinfo = Instance.FindBossFromGroupId(territoryDb.GroupId);

                                if (mobinfo.Error == null)
                                {
                                    if (!territoryDb.BossMobId.Equals(mobinfo.Mob.InternalID))
                                    {
                                        log.Error($"Boss Id does not match from GroupId {territoryDb.GroupId} and Found Bossid from groupId (event search) {mobinfo.Mob.InternalID} , {territoryDb.BossMobId} identified in database");
                                        continue;
                                    }

                                    Instance.Territories.Add(new Territory(area, territoryDb.AreaId, territoryDb.RegionId, territoryDb.ZoneId, territoryDb.GroupId, mobinfo.Mob, bonus: territoryDb.Bonus, id: territoryDb.ObjectId));
                                    count++;
                                }
                                else
                                {
                                    log.Error(mobinfo.Error);
                                }
                            }
                        }
                    }
                }
            }

            GuildMgr.GetAllGuilds().ForEach(g => g.LoadTerritories());
            log.Info(count + " Territoires Chargés");
        }

        public bool IsTerritoryArea(IEnumerable<IArea> areas)
        {
            foreach (var item in areas)
            {
                bool matched = this.Territories.Any(t => t.Area.ID.Equals(item.ID));

                if (matched)
                {
                    return true;
                }
            }

            return false;
        }

        public void ChangeGuildOwner(Guild guild, Territory territory)
        {
            if (guild == null || territory == null)
            {
                return;
            }

            this.ApplyTerritoryChange(guild, territory, false);
        }

        public void ChangeGuildOwner(string mobId, Guild guild, string equipment = null, bool isBoss = false)
        {
            if (guild == null || string.IsNullOrEmpty(mobId))
            {
                return;
            }

            Territory territory = null;

            if (isBoss)
            {
                territory = this.Territories.FirstOrDefault(t => t.BossId.Equals(mobId));
            }
            else
            {
                territory = GetTerritoryFromMobId(mobId);
            }

            if (territory == null || territory.Mobs == null)
            {
                log.Error("Cannot get Territory from MobId: " + mobId);
                return;
            }

            this.ApplyTerritoryChange(guild, territory, true, equipment);
        }

        public void ClearTerritory(Territory territory)
        {
            if (territory == null || territory.Mobs == null || territory.Boss == null)
                return;

            var cls = WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentZone.ID.Equals(territory.ZoneId));

            foreach (var mob in territory.Mobs.Where(m => m.GetType().FullName.Equals(GUARD_CLASS)))
            {
                foreach (var item in mob.Inventory.VisibleItems)
                {
                    item.Color = NEUTRAL_EMBLEM;
                    item.Emblem = 0;
                }

                cls.Foreach(c => c.Out.SendLivingEquipmentUpdate(mob));
            }

            foreach (var mob in territory.Mobs)
            {
                if (territory.OriginalGuilds.ContainsKey(mob.InternalID))
                {
                    mob.GuildName = territory.OriginalGuilds[mob.InternalID];
                }
                else
                {
                    mob.GuildName = null;
                }
            }

            Guild oldOwner = GuildMgr.GetGuildByName(territory.GuildOwner);

            if (oldOwner != null)
            {
                oldOwner.RemoveTerritory(territory.AreaId);
            }

            territory.GuildOwner = null;
            territory.Boss.RestoreOriginalGuildName();
            territory.SaveIntoDatabase();
        }

        private static void ApplyNewEmblem(string guildName, GameNPC mob)
        {
            if (string.IsNullOrWhiteSpace(guildName) || mob.ObjectState != eObjectState.Active || mob.CurrentRegion == null || mob.Inventory == null || mob.Inventory.VisibleItems == null)
                return;
            var guild = GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return;
            foreach (var item in mob.Inventory.VisibleItems.Where(i => i.SlotPosition == 26))
                if (item.Emblem != 0 || item.Color == NEUTRAL_EMBLEM)
                    item.Emblem = guild.Emblem;
        }

        public MobInfo FindBossFromGroupId(string groupId)
        {
            var bossEvent = GameEventManager.Instance.Events.FirstOrDefault(e => e.KillStartingGroupMobId?.Equals(groupId) == true);

            if (bossEvent == null)
            {
                return new MobInfo()
                {
                    Error = "Impossible de trouver l'event lié au GroupId: " + groupId
                };
            }

            var boss = bossEvent.Mobs.FirstOrDefault(m => m.GetType().FullName.Equals(BOSS_CLASS));

            if (boss == null)
            {
                return new MobInfo()
                {
                    Error = $"Aucun mob avec la classe {BOSS_CLASS} a été trouvé dans l'Event {bossEvent.ID}"
                };
            }

            return new MobInfo()
            {
                Mob = boss
            };
        }

        public IList<string> GetTerritoriesInformations()
        {
            List<string> infos = new List<string>();

            foreach (var territory in this.Territories)
            {
                string line = (((AbstractArea)territory.Area).Description + " / ");

                var zone = WorldMgr.Regions[territory.RegionId].Zones.FirstOrDefault(z => z.ID.Equals(territory.ZoneId));

                if (zone != null)
                {
                    line += zone.Description + " / ";
                }

                line += territory.GuildOwner ?? "Neutre";
                infos.Add(line);
                infos.Add("");
            }

            return infos;
        }

        public static Territory GetTerritoryFromMobId(string mobId)
        {
            foreach (var territory in Instance.Territories)
            {
                if (territory.Mobs.Any(m => m.InternalID.Equals(mobId)))
                    return territory;
            }

            return null;
        }

        public bool AddTerritory(IArea area, string areaId, ushort regionId, string groupId, GameNPC boss)
        {
            if (!WorldMgr.Regions.ContainsKey(regionId) || groupId == null || boss == null || areaId == null)
            {
                return false;
            }

            var coords = GetCoordinates(area);

            if (coords == null)
            {
                return false;
            }

            var zone = WorldMgr.Regions[regionId].GetZone(coords.X, coords.Y);

            if (zone == null)
            {
                return false;
            }

            var territory = new Territory(area, areaId, regionId, zone.ID, groupId, boss);
            this.Territories.Add(territory);

            try
            {
                territory.SaveIntoDatabase();
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return false;
            }

            return true;
        }

        private void ApplyTerritoryChange(Guild guild, Territory territory, bool saveChange, string equipment = null)
        {
            //remove Territory from old Guild if any
            if (territory.GuildOwner != null)
            {
                var oldGuild = GuildMgr.GetGuildByName(territory.GuildOwner);

                if (oldGuild != null)
                {
                    oldGuild.RemoveTerritory(territory.AreaId);
                }
            }

            guild.AddTerritory(territory.AreaId, saveChange);
            territory.GuildOwner = guild.Name;

            if (equipment == null)
            {
                equipment = GUARD_BASIC_TEMPLATE;
            }
            var cls = WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentZone.ID.Equals(territory.ZoneId));

            foreach (var mob in territory.Mobs.Where(m => m.GetType().FullName.Equals(GUARD_CLASS)))
            {
                mob.LoadEquipmentTemplateFromDatabase(equipment);
                ApplyNewEmblem(guild.Name, mob);
                cls.Foreach(c => c.Out.SendLivingEquipmentUpdate(mob));
            }

            territory.Mobs.Foreach(m => m.GuildName = guild.Name);

            if (saveChange)
                territory.SaveIntoDatabase();
        }


        public static AreaCoordinate GetCoordinates(IArea area)
        {
            float x, y;

            if (area is DOL.GS.Area.Circle circle)
            {
                x = circle.Position.X;
                y = circle.Position.Y;
            }
            else if (area is Square sq)
            {
                x = sq.X;
                y = sq.Y;
            }
            else if (area is Polygon poly)
            {
                x = poly.X;
                y = poly.Y;
            }
            else
            {
                return null;
            }

            return new AreaCoordinate()
            {
                X = x,
                Y = y
            };
        }

        public void ProceedPayments()
        {
            foreach (var guildGroup in this.Territories.GroupBy(t => t.GuildOwner))
            {
                var guildName = guildGroup.Key;
                if (guildName != null)
                {
                    var guild = GuildMgr.GetGuildByName(guildName);

                    if (guild != null)
                    {
                        int count = guildGroup.Count();
                        bool shouldRemoveTerritories = false;
                        var players = guild.GetListOfOnlineMembers();

                        if (count < 6)
                        {
                            int sum = count * DAILY_TAX;
                            if (guild.TryPayTerritoryTax(Money.GetMoney(0, 0, sum, 0, 0)))
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryPaid", sum),
                                              eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                            }
                            else
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryNoMoney"),
                                                                            eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                                shouldRemoveTerritories = true;
                            }
                        }
                        else
                        {
                            int over = count - 5;
                            int baseAmount = 5 * DAILY_TAX;
                            int total = (over * (DAILY_TAX + 10)) + baseAmount;

                            if (guild.TryPayTerritoryTax(Money.GetMoney(0, 0, total, 0, 0)))
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryPaid", total),
                                              eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                            }
                            else
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryNoMoney"),
                                              eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                                shouldRemoveTerritories = true;
                            }
                        }


                        if (shouldRemoveTerritories)
                        {
                            foreach (var territory in guildGroup)
                            {
                                this.ClearTerritory(territory);
                            }
                        }
                        else
                        {
                            int mp = count * DAILY_MERIT_POINTS;
                            guild.GainMeritPoints(mp);
                            players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryMeritPoints", mp),
                                             eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                        }
                    }
                }
            }
        }
    }

    public class MobInfo
    {
        public GameNPC Mob
        {
            get;
            set;
        }

        public string Error
        {
            get;
            set;
        }
    }
}