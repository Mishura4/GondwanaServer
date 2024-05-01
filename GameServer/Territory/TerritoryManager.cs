using DOL.Database;
using DOL.events.server;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.ServerProperties;
using DOL.Language;
using DOL.MobGroups;
using DOLDatabase.Tables;
using log4net;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static DOL.GS.Area;
using static DOL.GS.GameObject;

namespace DOL.Territories
{
    public class TerritoryManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static TerritoryManager instance;
        public static readonly ushort NEUTRAL_EMBLEM = 256;
        private readonly string BOSS_CLASS = "DOL.GS.Scripts.TerritoryBoss";
        private static readonly int DAILY_TAX = GS.ServerProperties.Properties.DAILY_TAX;
        private static readonly int TERRITORY_BANNER_PERCENT_OFF = GS.ServerProperties.Properties.TERRITORY_BANNER_PERCENT_OFF;
        private static readonly int DAILY_MERIT_POINTS = GS.ServerProperties.Properties.DAILY_MERIT_POINTS;
        private static Dictionary<Timer, Territory> m_TerritoriesAttacked;

        public static TerritoryManager Instance => instance ?? (instance = new TerritoryManager());

        public List<Territory> Territories
        {
            get;
        }

        private TerritoryManager()
        {
            Territories = new List<Territory>();
            m_TerritoriesAttacked = new Dictionary<Timer, Territory>();
        }

        public bool Init()
        {
            return true;
        }

        public void TerritoryAttacked(Territory territory)
        {
            if (!m_TerritoriesAttacked.ContainsValue(territory))
            {
                Timer timer = new Timer(20000);
                timer.Elapsed += TerritoryAttackedCallback;
                timer.Enabled = true;
                m_TerritoriesAttacked.Add(timer, territory);
                GuildMgr.GetGuildByName(territory.GuildOwner).SendMessageToGuildMembersKey("TerritoryManager.Territory.Attacked", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow, territory.Name);
            }
        }

        private void TerritoryAttackedCallback(object sender, ElapsedEventArgs e)
        {
            Timer timer = sender as Timer;
            timer.Stop();
            timer.Dispose();
            m_TerritoriesAttacked.Remove(timer);
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
                            var areaDb = GameServer.Database.SelectObjects<DBArea>(DB.Column("area_id").IsEqualTo(territoryDb.AreaId))?.FirstOrDefault();

                            if (areaDb == null)
                            {
                                log.Error($"Cannot find Area in Database with ID: {territoryDb.AreaId}");
                                continue;
                            }

                            var area = zone.GetAreas(a => a is AbstractArea area && string.Equals(area.Description, areaDb.Description)).FirstOrDefault();

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
                                    var coordinates = GetCoordinates(area);

                                    Territory territory = new Territory(area, territoryDb.AreaId, new Vector3(coordinates.X, coordinates.Y, coordinates.Z), territoryDb.RegionId, territoryDb.ZoneId, territoryDb.GroupId, mobinfo.Mob, territoryDb.IsBannerSummoned, guild: territoryDb.GuidldOwner, bonus: territoryDb.Bonus, id: territoryDb.ObjectId);

                                    Instance.Territories.Add(territory);

                                    if (!string.IsNullOrEmpty(territory.GuildOwner) && territory.IsBannerSummoned)
                                    {
                                        Guild guild = GuildMgr.GetGuildByName(territory.GuildOwner);
                                        if (guild != null)
                                        {
                                            Instance.ApplyEmblemToTerritory(territory, guild);
                                        }
                                        else
                                        {
                                            log.Error($"Territory Manager cant find guild {territory.GuildOwner}");
                                        }
                                    }

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

        public bool DoesPlayerOwnsTerritory(GamePlayer player)
        {
            foreach (var item in player.CurrentAreas)
            {
                var matched = this.Territories.FirstOrDefault(t => t.Area.ID.Equals(item.ID));

                if (matched != null && matched.GuildOwner != null && matched.GuildOwner.Equals(player.GuildName))
                {
                    return true;
                }
            }

            return false;
        }

        public Territory GetCurrentTerritory(IEnumerable<IArea> areas)
        {
            foreach (var item in areas)
            {
                // TODO: Fix race condition while loading territories
                var matched = this.Territories.FirstOrDefault(t => t.Area.ID.Equals(item.ID));

                if (matched != null)
                {
                    return matched;
                }
            }

            return null;
        }

        public void ChangeGuildOwner(Guild guild, Territory territory)
        {
            if (guild == null || territory == null)
            {
                return;
            }

            this.ApplyTerritoryChange(guild, territory, false);
        }

        public void ChangeGuildOwner(GameNPC mob, Guild guild)
        {
            if (guild is not { GuildType: Guild.eGuildType.PlayerGuild } || mob == null || string.IsNullOrEmpty(mob.InternalID))
            {
                return;
            }

            Territory territory = this.Territories.FirstOrDefault(t => t.BossId.Equals(mob.InternalID));
            //For Boss change also Guild for Mob linked in same Event
            if (mob.EventID != null)
            {
                var gameEvent = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(mob.EventID));

                if (gameEvent?.Mobs?.Any() == true)
                {
                    gameEvent.Mobs.ForEach(m => m.GuildName = guild.Name);
                }
            }

            if (territory == null || territory.Mobs == null)
            {
                log.Error("Cannot get Territory from MobId: " + mob.InternalID);
                return;
            }

            this.ApplyTerritoryChange(guild, territory, true);
        }

        public void RestoreTerritoryGuildNames(Territory territory)
        {
            if (territory == null || territory.Mobs == null || territory.Boss == null)
            {
                log.Error($"Impossible to clear territory. One Value is Null: Territory: {territory == null}, Mobs: {territory?.Mobs == null}, Boss: {territory?.Boss == null}");
                return;
            }

            if (territory.Boss != null)
            {
                var gameEvents = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(territory.Boss.EventID));

                if (gameEvents?.Mobs?.Any() == true)
                {
                    gameEvents.Mobs.ForEach(m =>
                    {
                        if (territory.OriginalGuilds.ContainsKey(m.InternalID))
                        {
                            m.GuildName = territory.OriginalGuilds[m.InternalID];
                        }
                        else
                        {
                            m.GuildName = null;
                        }
                    });
                }
            }

            if (territory.GuildOwner != null)
            {
                Guild oldOwner = GuildMgr.GetGuildByName(territory.GuildOwner);

                if (oldOwner != null)
                {
                    oldOwner.RemoveTerritory(territory);
                }
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

            territory.GuildOwner = null;
            territory.Boss.RestoreOriginalGuildName();
            territory.SaveIntoDatabase();
        }

        private void ChangeMagicAndPhysicalResistance(GameNPC mob, int value, bool isSubstract)
        {
            eProperty Property1 = eProperty.Resist_Heat;
            eProperty Property2 = eProperty.Resist_Cold;
            eProperty Property3 = eProperty.Resist_Matter;
            eProperty Property4 = eProperty.Resist_Body;
            eProperty Property5 = eProperty.Resist_Spirit;
            eProperty Property6 = eProperty.Resist_Energy;
            eProperty Property7 = eProperty.Resist_Crush;
            eProperty Property8 = eProperty.Resist_Slash;
            eProperty Property9 = eProperty.Resist_Thrust;
            ApplyBonus(mob, Property1, value, isSubstract);
            ApplyBonus(mob, Property2, value, isSubstract);
            ApplyBonus(mob, Property3, value, isSubstract);
            ApplyBonus(mob, Property4, value, isSubstract);
            ApplyBonus(mob, Property5, value, isSubstract);
            ApplyBonus(mob, Property6, value, isSubstract);
            ApplyBonus(mob, Property7, value, isSubstract);
            ApplyBonus(mob, Property8, value, isSubstract);
            ApplyBonus(mob, Property9, value, isSubstract);
        }

        public static void ClearEmblem(Territory territory, GameNPC initNpc = null)
        {
            Guild guild = GuildMgr.GetGuildByName(territory.GuildOwner);
            int guildLevel = (int)(guild != null ? guild.GuildLevel : 0);
            int mobBannerResist = territory.CurrentBannerResist;

            territory.IsBannerSummoned = false;
            foreach (var mob in territory.Mobs)
            {
                RestoreOriginalEmblem(mob);
                if (mobBannerResist > 0)
                {
                    // Unapply magic and physical resistance bonus
                    Instance.ChangeMagicAndPhysicalResistance(mob, mobBannerResist, true);
                }
            }

            if (mobBannerResist > 0)
            {
                Instance.ChangeMagicAndPhysicalResistance(territory.Boss, mobBannerResist, true);
            }
            RestoreOriginalEmblem(territory.Boss);

            var firstMob = initNpc ?? territory.Mobs.FirstOrDefault(m => m.CurrentZone != null);
            foreach (GameObject item in firstMob.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.ITEM, firstMob.Position.X, firstMob.Position.Y, firstMob.Position.Z, WorldMgr.VISIBILITY_DISTANCE, new System.Collections.ArrayList(), true))
            {
                if (item is TerritoryBanner ban)
                {
                    ban.Emblem = ban.OriginalEmblem;
                }
            }
        }

        private static void ApplyNewEmblem(string guildName, GameNPC mob)
        {
            if (string.IsNullOrWhiteSpace(guildName) || mob.ObjectState != eObjectState.Active || mob.CurrentRegion == null || mob.Inventory == null || mob.Inventory.VisibleItems == null)
                return;
            var guild = GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return;
            foreach (var item in mob.Inventory.VisibleItems.Where(i => i.SlotPosition == 26 || i.SlotPosition == 11))
            {
                item.Emblem = guild.Emblem;
            }
        }

        private static void RestoreOriginalEmblem(GameNPC mob)
        {
            if (mob.ObjectState != eObjectState.Active || mob.CurrentRegion == null || mob.Inventory == null || mob.Inventory.VisibleItems == null)
                return;

            foreach (var item in mob.Inventory.VisibleItems.Where(i => i.SlotPosition == 11 || i.SlotPosition == 26))
            {
                var equipment = GameServer.Database.SelectObjects<NPCEquipment>(DB.Column("TemplateID").IsEqualTo(mob.EquipmentTemplateID)
                                                    .And(DB.Column("TemplateID").IsEqualTo(item.SlotPosition)))?.FirstOrDefault();

                if (equipment != null)
                {
                    item.Emblem = equipment.Emblem;
                }
            }

            mob.BroadcastLivingEquipmentUpdate();
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

        public IList<string> GetTerritoriesInformations(GamePlayer player)
        {
            List<string> infos = new List<string>();
            List<string> ownedTerritories = new List<string>();
            List<string> otherTerritories = new List<string>();
            string language = player.Client?.Account?.Language ?? Properties.SERV_LANGUAGE;
            string neutral = LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TerritoryNeutral");

            foreach (var territory in this.Territories)
            {
                string line = (((AbstractArea)territory.Area).Description + " / ");

                var zone = WorldMgr.Regions[territory.RegionId].Zones.FirstOrDefault(z => z.ID.Equals(territory.ZoneId));

                if (zone != null)
                {
                    line += zone.Description + " / ";
                }

                if (!string.IsNullOrEmpty(territory.GuildOwner))
                {
                    line += territory.GuildOwner;
                    if (territory.GuildOwner.Equals(player.Guild?.Name))
                    {
                        ownedTerritories.Add(line);
                    }
                    else
                    {
                        otherTerritories.Add(line);
                    }
                }
                else
                {
                    line += neutral;
                    otherTerritories.Add(line);
                }
            }
            if (ownedTerritories.Any())
            {
                infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TerritoriesOwned", player.Guild.Name));
                infos.AddRange(ownedTerritories);
                infos.Add(string.Empty);
                infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TerritoriesOther"));
            }
            else
            {
                infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TerritoriesList"));
            }
            infos.AddRange(otherTerritories);

            // ---- Guild Territories Info ----
            infos.Add(string.Empty);
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.GuildInfo"));
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.DailyRent", CalculateGuildTerritoryTax(player.Guild)));
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.DailyMeritPoints", ownedTerritories.Count * DAILY_MERIT_POINTS));
            string timeBeforeRent;
            if (ownedTerritories.Count == 0)
            {
                timeBeforeRent = LanguageMgr.GetTranslation(language, "Language.NotApplicable");
            }
            else
            {
                var nextPayment = WorldMgr.GetTimeBeforeNextDay() / 1000;
                var seconds = nextPayment % 60;
                var minutes = nextPayment / 60 % 60;
                var hours = nextPayment / 3600;
                timeBeforeRent = LanguageMgr.TranslateTimeShort(player, hours, minutes, seconds);
            }
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TimeBeforeRent", timeBeforeRent));
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TotalXPBonus", Math.Min(10, ownedTerritories.Count * 2)));
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.TotalBPBonus", ownedTerritories.Count == 0 ? 0 : player.Guild.TerritoryBonusBountyPoints * 2));

            // ---- Territory Bonuses ----
            infos.Add(string.Empty);
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.Bonuses"));
            infos.Add(LanguageMgr.GetTranslation(language, "Language.Resists") + ':');
            infos.Add("-- " + GetInfoResist(language, eResist.Natural, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Spirit, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Crush, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Slash, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Thrust, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Body, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Cold, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Heat, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Energy, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + GetInfoResist(language, eResist.Matter, player.Guild, ownedTerritories.Count));
            infos.Add("-- " + LanguageMgr.GetTranslation(language, "Language.DamageType.Melee.Noun") + (ownedTerritories.Count == 0 ? ": 0%" : $": {player.Guild.TerritoryMeleeAbsorption}%"));
            infos.Add("-- " + LanguageMgr.GetTranslation(language, "Language.DamageType.Spell.Noun") + (ownedTerritories.Count == 0 ? ": 0%" : $": {player.Guild.TerritorySpellAbsorption}%"));
            infos.Add("-- " + LanguageMgr.GetTranslation(language, "Language.DamageType.DoT.Noun") + (ownedTerritories.Count == 0 ? ": 0%" : $": {player.Guild.TerritoryDotAbsorption}%"));
            infos.Add("-- " + LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.DebuffDuration") + (ownedTerritories.Count == 0 ? ": 0%" : $": {player.Guild.TerritoryDebuffDurationReduction}%"));
            infos.Add(LanguageMgr.GetTranslation(language, "Commands.Players.Guild.Territories.SpellRange") + (ownedTerritories.Count == 0 ? ": 0%" : $": {player.Guild.TerritorySpellRangeBonus}%"));
            return infos;
        }

        private string GetInfoResist(string language, eResist resist, Guild guild, int numTerritories)
        {
            if (numTerritories == 0)
            {
                return LanguageMgr.GetResistNoun(language, resist) + ": 0%";
            }
            else
            {
                return LanguageMgr.GetResistNoun(language, resist) + $": {guild.GetResistFromTerritories(resist)}%";
            }
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

            var territory = new Territory(area, areaId, new Vector3(coords.X, coords.Y, coords.Z), regionId, zone.ID, groupId, boss, false);
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

        private void ApplyTerritoryChange(Guild guild, Territory territory, bool saveChange)
        {
            //remove Territory from old Guild if any
            if (!string.IsNullOrEmpty(territory.GuildOwner) && territory.GuildOwner != guild.Name)
            {
                var oldGuild = GuildMgr.GetGuildByName(territory.GuildOwner);

                if (oldGuild != null)
                {
                    oldGuild.RemoveTerritory(territory);
                }
                ClearEmblem(territory);
                territory.ClearPortal();
            }

            guild.AddTerritory(territory, saveChange);
            territory.GuildOwner = guild.Name;

            territory.Mobs.ForEach(m => m.GuildName = guild.Name);
            territory.Boss.GuildName = guild.Name;

            if (saveChange)
                territory.SaveIntoDatabase();
        }

        /// <summary>
        /// Method used to apply bonuses
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="Property"></param>
        /// <param name="Value"></param>
        /// <param name="IsSubstracted"></param>
        private void ApplyBonus(GameLiving owner, eProperty Property, int Value, bool IsSubstracted)
        {
            IPropertyIndexer tblBonusCat;
            if (Property != eProperty.Undefined)
            {
                tblBonusCat = owner.BaseBuffBonusCategory;
                if (IsSubstracted)
                    tblBonusCat[(int)Property] -= Value;
                else
                    tblBonusCat[(int)Property] += Value;
            }
        }

        private void ApplyEmblemToTerritory(Territory territory, Guild guild, GameNPC initSearchNPC = null)
        {
            territory.IsBannerSummoned = true;
            var cls = WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentZone.ID.Equals(territory.ZoneId));


            int guildLevel = (int)(guild != null ? guild.GuildLevel : 0);
            int mobBannerResist = Properties.TERRITORYMOB_BANNER_RESIST + (guildLevel >= 15 ? 15 : 0);

            foreach (var mob in territory.Mobs)
            {
                ApplyNewEmblem(guild.Name, mob);

                // Apply magic and physical resistance bonus
                ChangeMagicAndPhysicalResistance(mob, mobBannerResist, false);

                cls.Foreach(c => c.Out.SendLivingEquipmentUpdate(mob));
            }

            // Apply magic and physical resistance bonus
            ChangeMagicAndPhysicalResistance(territory.Boss, mobBannerResist, false);
            territory.CurrentBannerResist = mobBannerResist;
            ApplyNewEmblem(guild.Name, territory.Boss);

            var firstMob = initSearchNPC ?? territory.Mobs.FirstOrDefault(m => m.CurrentZone != null);
            foreach (GameObject item in firstMob.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.ITEM, firstMob.Position.X, firstMob.Position.Y, firstMob.Position.Z, WorldMgr.VISIBILITY_DISTANCE, new System.Collections.ArrayList(), true))
            {
                if (item is TerritoryBanner ban)
                {
                    ban.Emblem = guild.Emblem;
                }
            }
        }

        public static void ApplyEmblemToTerritory(Territory territory, Guild guild, bool saveterritory, GameNPC initSearchNPC = null)
        {
            Instance.ApplyEmblemToTerritory(territory, guild, initSearchNPC);
            if (saveterritory)
            {
                territory.SaveIntoDatabase();
            }
        }

        public static AreaCoordinate GetCoordinates(IArea area)
        {
            return area switch
            {
                DOL.GS.Area.Circle circle => new AreaCoordinate() { X = circle.Position.X, Y = circle.Position.Y, Z = circle.Position.Z },
                Square sq => new AreaCoordinate() { X = sq.Position.X, Y = sq.Position.Y, Z = sq.Position.Z },
                Polygon poly => new AreaCoordinate() { X = poly.Position.X, Y = poly.Position.Y, Z = poly.Position.Z },
                _ => null
            };
        }

        public int CalculateGuildTerritoryTax(Guild guild)
        {
            if (guild == null || guild.Territories?.Any() != true)
                return 0;

            int total = 0;
            int counter = 0;
            foreach (var territory in guild.Territories)
            {
                counter++;
                int tax = counter >= 6 ? DAILY_TAX + 10 : DAILY_TAX;
                total += territory.IsBannerSummoned ? (int)Math.Round((tax * (1.0d - TERRITORY_BANNER_PERCENT_OFF / 100D))) : tax;
            }
            return total;
        }

        public void ProceedPayments()
        {
            foreach (var guildGroup in this.Territories.GroupBy(t => t.GuildOwner))
            {
                var guildName = guildGroup.Key;
                if (guildName == null)
                {
                    continue;
                }
                var guild = GuildMgr.GetGuildByName(guildName);

                if (guild == null)
                {
                    continue;
                }
                int count = guildGroup.Count();
                bool shouldRemoveTerritories = false;
                var players = guild.GetListOfOnlineMembers();
                int tax = CalculateGuildTerritoryTax(guild);

                if (guild.TryPayTerritoryTax(Money.GetMoney(0, 0, tax, 0, 0)))
                {
                    players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryPaid", tax),
                                                           eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                    int mp = count * DAILY_MERIT_POINTS;
                    guild.GainMeritPoints(mp);
                    players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryMeritPoints", mp),
                                                           eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                }
                else
                {
                    players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryNoMoney"),
                                                           eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                    foreach (var territory in guildGroup)
                    {
                        this.RestoreTerritoryGuildNames(territory);
                        ClearEmblem(territory);
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