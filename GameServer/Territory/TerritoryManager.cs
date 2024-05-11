﻿using DOL.Database;
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
using Microsoft.CodeAnalysis.Operations;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
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
                territory.OwnerGuild?.SendMessageToGuildMembersKey("TerritoryManager.Territory.Attacked", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow, territory.Name);
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

            if (values == null)
            {
                log.Info(count + " Territoires Chargés");
                return;
            }

            foreach (TerritoryDb territoryDb in values)
            {
                List<IArea> areas = new List<IArea>();

                if (!WorldMgr.Zones.TryGetValue(territoryDb.ZoneId, out Zone zone) || zone == null)
                {
                    log.Error($"Cannot find Zone {territoryDb.ZoneId} for territory {territoryDb.Name}");
                    continue;
                }

                foreach (var areaID in territoryDb.AreaIDs.Split('|'))
                {
                    var areaDb = GameServer.Database.SelectObjects<DBArea>(DB.Column("area_id").IsEqualTo(areaID))?.FirstOrDefault();

                    if (areaDb == null)
                    {
                        log.Error($"Cannot find Area in Database with ID {areaID} for territory {territoryDb.Name}");
                        continue;
                    }

                    var area = zone.GetAreas().OfType<AbstractArea>().FirstOrDefault(a => string.Equals(areaID, a.DbArea.ObjectId));
                    if (area == null)
                    {
                        log.Error($"Cannot find Area {areaID} for territory {territoryDb.Name}");
                        continue;
                    }

                    areas.Add(area);
                }

                var mobinfo = Instance.FindBossFromGroupId(territoryDb.GroupId);
                if (mobinfo.Error == null)
                {
                    if (!territoryDb.BossMobId.Equals(mobinfo.Mob.InternalID))
                    {
                        log.Error($"Boss Id does not match from GroupId {territoryDb.GroupId} and Found Bossid from groupId (event search) {mobinfo.Mob.InternalID} , {territoryDb.BossMobId} identified in database");
                        continue;
                    }

                    Territory territory = new Territory(zone, areas, mobinfo.Mob, territoryDb);

                    Instance.Territories.Add(territory);

                    count++;
                }
                else
                {
                    log.Error(mobinfo.Error);
                }
            }

            GuildMgr.GetAllGuilds().ForEach(g => g.LoadTerritories());
            log.Info(count + " Territoires Chargés");
        }

        public static Territory GetCurrentTerritory(GameObject obj)
        {
            return GetCurrentTerritory(obj.CurrentAreas);
        }

        public static Territory GetCurrentTerritory(IEnumerable<IArea> areas)
        {
            return Instance.Territories.FirstOrDefault(t => areas.Any(t.IsInTerritory));
        }

        public static Territory GetTerritoryAtArea(string areaID)
        {
            return Instance.Territories.FirstOrDefault(t => t.Areas.OfType<AbstractArea>().Any(a => string.Equals(a.DbArea.ObjectId, areaID)));
        }

        public static Territory GetTerritoryAtArea(IArea area)
        {
            return Instance.Territories.FirstOrDefault(t => t.Areas.Contains(area));
        }

        public static Territory GetTerritoryByID(string ID)
        {
            return Instance.Territories.FirstOrDefault(t => string.Equals(t.ID, ID));
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

            territory.OwnerGuild = guild;
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
                string line = territory.Name + " / " + territory.Zone.Description;

                if (territory.IsNeutral())
                {
                    line += " / " + neutral;
                    otherTerritories.Add(line);
                }
                else
                {
                    if (territory.IsOwnedBy(player))
                    {
                        ownedTerritories.Add(line);
                    }
                    else
                    {
                        line += " / " + territory.OwnerGuild?.Name ?? "????";
                        otherTerritories.Add(line);
                    }
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
            if (!WorldMgr.Regions.TryGetValue(regionId, out Region region) || groupId == null || boss == null || areaId == null)
            {
                return false;
            }

            var coords = GetCoordinates(area);
            if (coords == null)
            {
                return false;
            }

            var zone = region.GetZone(coords.X, coords.Y);
            if (zone == null)
            {
                return false;
            }

            var territory = new Territory(zone, new List<IArea>{area}, (area as AbstractArea)?.Description ?? "New territory", boss, new Vector3(coords.X, coords.Y, coords.Z), regionId, groupId);
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
            foreach (var guildGroup in this.Territories.GroupBy(t => t.OwnerGuild))
            {
                var guild = guildGroup.Key;

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
                        territory.OwnerGuild = null;
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