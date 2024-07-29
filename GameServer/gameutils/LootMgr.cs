/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using DOL.Database;
using log4net;
using Microsoft.Diagnostics.Runtime.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Diagnostics;

namespace DOL.GS
{
    /// <summary>
    /// the LootMgr holds pointers to all LootGenerators at 
    /// associates the correct LootGenerator with a given Mob
    /// </summary>
    public sealed class LootMgr
    {
        public record LootConditions(
            List<String> MobName = null,
            List<String> MobGuild = null,
            List<int> MobFaction = null,
            List<ushort> RegionIDs = null,
            List<ushort> MobModel = null,
            List<ushort> MobBodyType = null,
            List<short> MobRace = null,
            bool? Renaissance = null,
            bool? GoodReputation = null,
            bool? Boss = null)
        {
            public bool? IsMobNameMet(GameLiving victim) => MobName?.Contains(victim.Name);

            public bool? IsMobGuildMet(GameLiving victim) => MobGuild is null ? null : !string.IsNullOrEmpty(victim.GuildName) && MobGuild.Contains(victim.GuildName.ToLowerInvariant());

            public bool? IsMobFactionMet(GameLiving victim) => MobFaction is null ? null : victim is GameNPC npc && MobFaction.Contains(npc.Faction?.ID ?? 0);

            public bool? IsRegionMet(GameLiving victim) => RegionIDs?.Contains(victim.CurrentRegionID);
            
            public bool? IsMobModelMet(GameLiving victim) => MobModel?.Contains(victim.Model);

            public bool? IsMobBodyTypeMet(GameLiving victim) => MobBodyType is null ? null : victim is GameNPC { BodyType: { } bodyType } && MobBodyType.Contains(bodyType);
            
            public bool? IsMobRaceMet(GameLiving victim) => MobRace?.Contains(victim.Race);

            public bool? IsRenaissanceMet(GamePlayer killer) => Renaissance == null ? null : killer.IsRenaissance == Renaissance;

            public bool? IsReputationMet(GamePlayer killer) => GoodReputation == null ? null : (killer.Reputation >= 0) == GoodReputation;

            public bool? IsBossMet(GameLiving victim) => Boss == null ? null : (victim as GameNPC)?.IsBoss == Boss;
            
            public bool Matches(GameLiving victim, GamePlayer killer)
            {
                // Do the integer conditions first because strings are more expensive to check
                return IsMobFactionMet(victim) is null or true &&
                    IsRegionMet(victim) is null or true &&
                    IsMobModelMet(victim) is null or true &&
                    IsMobBodyTypeMet(victim) is null or true &&
                    IsMobRaceMet(victim) is null or true &&
                    IsBossMet(victim) is null or true &&

                    IsRenaissanceMet(killer) is null or true &&
                    IsReputationMet(killer) is null or true &&

                    IsMobNameMet(victim) is null or true &&
                    IsMobGuildMet(victim) is null or true;
            }
        }
        
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Map holding one generator for each different class, to reuse similar generators...
        /// </summary>
        static readonly HybridDictionary m_ClassGenerators = new HybridDictionary();

        /// <summary>
        /// List of global Lootgenerators 
        /// </summary>
        static readonly HybridDictionary m_globalGenerators = new();
        
        /// <summary>
        /// List of Lootgenerators related by mobname
        /// </summary>
        static readonly HybridDictionary m_mobNameGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by mobguild
        /// </summary>
        static readonly HybridDictionary m_mobGuildGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by region ID
        /// </summary>
        static readonly HybridDictionary m_mobRegionGenerators = new HybridDictionary();

        // /// <summary>
        // /// List of Lootgenerators related by mobfaction
        // /// </summary>
        static readonly HybridDictionary m_mobFactionGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by mobmodel
        /// </summary>
        static readonly HybridDictionary m_mobModelGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by mobbodytype
        /// </summary>
        static readonly HybridDictionary m_mobBodyTypeGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by mobrace
        /// </summary>
        static readonly HybridDictionary m_mobRaceGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by isRenaissance
        /// </summary>
        static readonly HybridDictionary m_isRenaissanceGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by isGoodReput
        /// </summary>
        static readonly HybridDictionary m_isGoodReputGenerators = new HybridDictionary();

        /// <summary>
        /// List of Lootgenerators related by isBoss
        /// </summary>
        static readonly HybridDictionary m_isBossGenerators = new HybridDictionary();

        /// <summary>
        /// Initializes the LootMgr. This function must be called
        /// before the LootMgr can be used!
        /// </summary>
        public static bool Init()
        {
            if (log.IsInfoEnabled)
                log.Info("Loading LootGenerators...");

            IList<LootGenerator> m_lootGenerators;
            try
            {
                m_lootGenerators = GameServer.Database.SelectAllObjects<LootGenerator>();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("LootMgr: LootGenerators could not be loaded", e);
                return false;
            }

            if (m_lootGenerators != null) // did we find any loot generators
            {
                foreach (LootGenerator dbGenerator in m_lootGenerators)
                {
                    ILootGenerator generator = GetGeneratorInCache(dbGenerator);
                    if (generator == null)
                    {
                        Type generatorType = null;
                        foreach (Assembly asm in ScriptMgr.Scripts)
                        {
                            generatorType = asm.GetType(dbGenerator.LootGeneratorClass);
                            if (generatorType != null)
                                break;
                        }
                        if (generatorType == null)
                        {
                            generatorType = Assembly.GetAssembly(typeof(GameServer)).GetType(dbGenerator.LootGeneratorClass);
                        }

                        if (generatorType == null)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Could not find LootGenerator: " + dbGenerator.LootGeneratorClass + "!!!");
                            continue;
                        }
                        generator = (ILootGenerator)Activator.CreateInstance(generatorType);

                        PutGeneratorInCache(dbGenerator, generator);
                    }
                    RegisterLootGenerator(generator, dbGenerator.MobName, dbGenerator.MobGuild, dbGenerator.MobFaction, dbGenerator.RegionID, dbGenerator.MobModel, dbGenerator.MobBodyType, dbGenerator.MobRace, dbGenerator.IsRenaissance, dbGenerator.IsGoodReput, dbGenerator.IsBoss, dbGenerator.CondMustBeSetTogether);
                }
            }
            if (log.IsDebugEnabled)
            {
                log.Debug("Found " + m_globalGenerators.Count + " Global LootGenerators");
                log.Debug("Found " + m_mobNameGenerators.Count + " Mobnames registered by LootGenerators");
                log.Debug("Found " + m_mobGuildGenerators.Count + " Guildnames registered by LootGenerators");
            }

            // no loot generators loaded...
            if (m_globalGenerators.Count == 0 && m_mobNameGenerators.Count == 0 && m_globalGenerators.Count == 0)
            {
                ILootGenerator baseGenerator = new LootGeneratorMoney();
                RegisterLootGenerator(baseGenerator, null, null, null, null, null, null, null, null, null, null, false);
                if (log.IsInfoEnabled)
                    log.Info("No LootGenerator found, adding LootGeneratorMoney for all mobs as default.");
            }

            if (log.IsInfoEnabled)
                log.Info("LootGenerator initialized: true");
            return true;
        }

        /// <summary>
        /// Stores a generator in a cache to reused the same generators multiple times
        /// </summary>
        /// <param name="dbGenerator"></param>
        /// <param name="generator"></param>
        private static void PutGeneratorInCache(LootGenerator dbGenerator, ILootGenerator generator)
        {
            m_ClassGenerators[dbGenerator.LootGeneratorClass + dbGenerator.ExclusivePriority] = generator;
        }

        /// <summary>
        ///  Returns a generator from cache
        /// </summary>
        /// <param name="dbGenerator"></param>
        /// <returns></returns>
        private static ILootGenerator GetGeneratorInCache(LootGenerator dbGenerator)
        {
            if (m_ClassGenerators[dbGenerator.LootGeneratorClass + dbGenerator.ExclusivePriority] != null)
            {
                return (ILootGenerator)m_ClassGenerators[dbGenerator.LootGeneratorClass + dbGenerator.ExclusivePriority];
            }
            return null;
        }

        public static void UnRegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction)
        {
            UnRegisterLootGenerator(generator, mobname, mobguild, mobfaction, null, null, null, null, false, false, false, false, false);
        }

        private static LootConditions? ParseLootConditions(ILootGenerator generator, string mobname, string mobguild, string mobfaction, string mobregion, string mobmodel, string mobbodytype, string mobrace, bool? isRenaissance, bool? isGoodReput, bool? isBoss)
        {
            if (Util.IsEmpty(mobname) && Util.IsEmpty(mobguild) && Util.IsEmpty(mobfaction) && Util.IsEmpty(mobregion, true) && Util.IsEmpty(mobmodel) && Util.IsEmpty(mobbodytype) && Util.IsEmpty(mobrace) && isRenaissance is null && isGoodReput is null && isBoss is null)
            {
                return null;
            }
            
            List<string> mobNames = null;
            List<string> mobGuilds = null;
            List<int> mobFactions = null;
            List<ushort> mobRegions = null;
            List<ushort> mobModels = null;
            List<ushort> mobBodyTypes = null;
            List<short> mobRaces = null;
            // Loot Generator Name Indexed
            if (!Util.IsEmpty(mobname))
            {
                mobNames = new List<string>();
                // Parse CSV
                try
                {
                    mobNames = Util.SplitCSV(mobname);
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobNames for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            // Loot Generator Guild Indexed
            if (!Util.IsEmpty(mobguild))
            {
                mobGuilds = new List<string>();
                // Parse CSV
                try
                {
                    mobGuilds = Util.SplitCSV(mobguild.ToLowerInvariant());
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobGuilds for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            // Loot Generator Mob Faction Indexed
            if (!Util.IsEmpty(mobfaction))
            {
                mobFactions = new List<int>();
                // Parse CSV
                try
                {
                    foreach (string str in Util.SplitCSV(mobfaction))
                    {
                        mobFactions.Add(int.Parse(str));
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobFactions for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }
            
            if (!Util.IsEmpty(mobregion, true))
            {
                mobRegions = new List<ushort>();
                // Parse CSV
                try
                {
                    foreach (string str in Util.SplitCSV(mobregion))
                    {
                        mobRegions.Add(ushort.Parse(str));
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobRegions for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobmodel))
            {
                mobModels = new List<ushort>();
                try
                {
                    foreach (string str in Util.SplitCSV(mobmodel))
                    {
                        mobModels.Add(ushort.Parse(str));
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobModels for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobbodytype))
            {
                // Parse CSV
                mobBodyTypes = new List<ushort>();
                try
                {
                    foreach (string str in Util.SplitCSV(mobbodytype))
                    {
                        mobBodyTypes.Add(ushort.Parse(str));
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobBodyTypes for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }
            
            if (!Util.IsEmpty(mobrace))
            {
                mobRaces = new List<short>();
                // Parse CSV
                try
                {
                    foreach (string str in Util.SplitCSV(mobrace))
                    {
                        mobRaces.Add(short.Parse(str));
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobRaces for LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            return new LootConditions
            {
                MobName = mobNames,
                Boss = isBoss,
                GoodReputation = isGoodReput,
                MobBodyType = mobBodyTypes,
                MobFaction = mobFactions,
                MobGuild = mobGuilds,
                MobModel = mobModels,
                MobRace = mobRaces,
                RegionIDs = mobRegions
            };
        }

        /// <summary>
        /// Unregister a generator for the given parameters		
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="mobname"></param>
        /// <param name="mobguild"></param>
        /// <param name="mobfaction"></param>
        /// <param name="mobmodel"></param>
        /// <param name="mobbodytype"></param>
        /// <param name="mobrace"></param>
        /// <param name="isRenaissance"></param>
        /// <param name="isGoodReput"></param>
        /// <param name="isBadReput"></param>
        /// <param name="isBoss"></param>
        /// <param name="condMustBeSetTogether"></param>
        public static void UnRegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction, string mobregion, string mobmodel, string mobbodytype, string mobrace, bool isRenaissance, bool isGoodReput, bool isBadReput, bool isBoss, bool condMustBeSetTogether)
        {
            if (generator == null)
                return;

            LootConditions? condition = ParseLootConditions(generator, mobname, mobguild, mobfaction, mobregion, mobmodel, mobbodytype, mobrace, isRenaissance, isGoodReput, isBoss);
            if (condition == null)
            {
                m_globalGenerators.Add(generator, null);
                return;
            }

            if (!condMustBeSetTogether)
            {
                foreach (ushort bodytype in condition.MobBodyType ?? Enumerable.Empty<ushort>())
                {
                    ((IList)m_mobBodyTypeGenerators[bodytype])?.Remove(generator);
                }
                foreach (ushort model in condition.MobModel ?? Enumerable.Empty<ushort>())
                {
                    ((IList)m_mobModelGenerators[model])?.Remove(generator);
                }
                foreach (int faction in condition.MobFaction ?? Enumerable.Empty<int>())
                {
                    ((IList)m_mobFactionGenerators[faction])?.Remove(generator);
                } // foreach
                foreach (string guild in condition.MobGuild ?? Enumerable.Empty<string>())
                {
                    ((IList)m_mobGuildGenerators[guild])?.Remove(generator);
                }
                foreach (string mob in condition.MobName ?? Enumerable.Empty<string>())
                {
                    ((IList)m_mobNameGenerators[mob])?.Remove(generator);
                }
                foreach (short race in condition.MobRace ?? Enumerable.Empty<short>())
                {
                    ((IList)m_mobRaceGenerators[race])?.Remove(generator);
                }
                foreach (ushort region in condition.RegionIDs ?? Enumerable.Empty<ushort>())
                {
                    ((IList)m_mobRegionGenerators[region])?.Remove(generator);
                }

                if (isRenaissance != null)
                {
                    ((IList)m_isRenaissanceGenerators[isRenaissance])?.Remove(generator);
                }

                if (isGoodReput != null)
                {
                    ((IList)m_isGoodReputGenerators[isGoodReput])?.Remove(generator);
                }

                if (isBoss != null)
                {
                    ((IList)m_isBossGenerators[isBoss])?.Remove(generator);
                }
            }
            else
            {
                m_globalGenerators.Remove(generator);
            }
        }

        /// <summary>
        /// Register a generator for the given parameters,
        /// If all parameters are null a global generaotr for all mobs will be registered
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="mobname"></param>
        /// <param name="mobguild"></param>
        /// <param name="mobfaction"></param>
        /// <param name="mobregion"></param>
        /// <param name="mobmodel"></param>
        /// <param name="mobbodytype"></param>
        /// <param name="mobrace"></param>
        /// <param name="isRenaissance"></param>
        /// <param name="isGoodReput"></param>
        /// <param name="isBoss"></param>
        /// <param name="condMustBeSetTogether"></param>
        public static void RegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction, string mobregion, string mobmodel, string mobbodytype, string mobrace, bool? isRenaissance, bool? isGoodReput, bool? isBoss, bool condMustBeSetTogether)
        {
            if (generator == null)
                return;

            LootConditions? condition = ParseLootConditions(generator, mobname, mobguild, mobfaction, mobregion, mobmodel, mobbodytype, mobrace, isRenaissance, isGoodReput, isBoss);
            RegisterLootGenerator(generator, condition, condMustBeSetTogether);
        }
        
        public static void RegisterLootGenerator(ILootGenerator generator, LootConditions? condition, bool condMustBeSetTogether = false)
        {
            if (condition == null)
            {
                m_globalGenerators.Add(generator, null);
                return;
            }

            if (!condMustBeSetTogether)
            {
                foreach (ushort bodytype in condition.MobBodyType ?? Enumerable.Empty<ushort>())
                {
                    if ((IList)m_mobBodyTypeGenerators[bodytype] == null)
                    {
                        m_mobBodyTypeGenerators[bodytype] = new List<ILootGenerator>();
                    }
                    ((IList)m_mobBodyTypeGenerators[bodytype]).Add(generator);
                }
                foreach (ushort model in condition.MobModel ?? Enumerable.Empty<ushort>())
                {
                    if ((IList)m_mobModelGenerators[model] == null)
                    {
                        m_mobModelGenerators[model] = new List<ILootGenerator>();
                    }
                    ((IList)m_mobModelGenerators[model]).Add(generator);
                }
                foreach (int faction in condition.MobFaction ?? Enumerable.Empty<int>())
                {
                    if ((IList)m_mobFactionGenerators[faction] == null)
                        m_mobFactionGenerators[faction] = new List<ILootGenerator>();

                    ((IList)m_mobFactionGenerators[faction]).Add(generator);
                } // foreach
                foreach (string guild in condition.MobGuild ?? Enumerable.Empty<string>())
                {
                    if ((IList)m_mobGuildGenerators[guild] == null)
                    {
                        m_mobGuildGenerators[guild] = new List<ILootGenerator>();
                    }
                    ((IList)m_mobGuildGenerators[guild]).Add(generator);
                }
                foreach (string mob in condition.MobName ?? Enumerable.Empty<string>())
                {
                    if ((IList)m_mobNameGenerators[mob] == null)
                    {
                        m_mobNameGenerators[mob] = new List<ILootGenerator>();
                    }
                    ((IList)m_mobNameGenerators[mob]).Add(generator);
                }
                foreach (short race in condition.MobRace ?? Enumerable.Empty<short>())
                {
                    if ((IList)m_mobRaceGenerators[race] == null)
                    {
                        m_mobRaceGenerators[race] = new List<ILootGenerator>();
                    }
                    ((IList)m_mobRaceGenerators[race]).Add(generator);
                }
                foreach (ushort region in condition.RegionIDs ?? Enumerable.Empty<ushort>())
                {
                    if ((IList)m_mobRaceGenerators[region] == null)
                    {
                        m_mobRaceGenerators[region] = new List<ILootGenerator>();
                    }
                    ((IList)m_mobRaceGenerators[region]).Add(generator);
                }

                if (condition.Renaissance != null)
                {
                    if ((IList)m_isRenaissanceGenerators[condition.Renaissance] == null)
                    {
                        m_isRenaissanceGenerators[condition.Renaissance] = new List<ILootGenerator>();
                    }
                    ((IList)m_isRenaissanceGenerators[condition.Renaissance]).Add(generator);
                }

                if (condition.GoodReputation != null)
                {
                    if ((IList)m_isGoodReputGenerators[condition.GoodReputation] == null)
                    {
                        m_isGoodReputGenerators[condition.GoodReputation] = new List<ILootGenerator>();
                    }
                    ((IList)m_isGoodReputGenerators[condition.GoodReputation]).Add(generator);
                }

                if (condition.Boss != null)
                {
                    if ((IList)m_isBossGenerators[condition.Boss] == null)
                    {
                        m_isBossGenerators[condition.Boss] = new List<ILootGenerator>();
                    }
                    ((IList)m_isBossGenerators[condition.Boss]).Add(generator);
                }
            }
            else
            {
                m_globalGenerators.Add(generator, condition);
            }
        }

        /// <summary>
        /// Call the refresh method for each generator to update loot, if implemented
        /// </summary>
        /// <param name="mob"></param>
        public static void RefreshGenerators(GameNPC mob)
        {
            if (mob != null)
            {
                foreach (ILootGenerator gen in m_globalGenerators.Keys)
                {
                    gen.Refresh(mob);
                }
            }
        }


        /// <summary>
        /// Returns the loot for the given Mob
        /// </summary>		
        /// <param name="mob"></param>
        /// <param name="killer"></param>
        /// <returns></returns>
        public static ItemTemplate[] GetLoot(GameNPC mob, GameObject killer)
        {
            if (killer is not GamePlayer player)
                return Array.Empty<ItemTemplate>();

            LootList lootList = null;
            IList generators = GetLootGenerators(mob, player);
            foreach (ILootGenerator generator in generators)
            {
                try
                {
                    if (lootList == null)
                        lootList = generator.GenerateLoot(mob, killer);
                    else
                        lootList.AddAll(generator.GenerateLoot(mob, killer));
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("GetLoot", e);
                }
            }
            if (lootList != null)
                return lootList.GetLoot();
            else
                return Array.Empty<ItemTemplate>();
        }

        /// <summary>
        /// Returns the ILootGenerators for the given mobs
        /// </summary>
        /// <param name="mob"></param>
        /// <returns></returns>
        public static IList GetLootGenerators(GameNPC mob, GamePlayer player)
        {
            IList filteredGenerators = new List<ILootGenerator>();
            ILootGenerator exclusiveGenerator = null;

            var nameGenerators = (IList<ILootGenerator>)m_mobNameGenerators[mob.Name];
            var guildGenerators = (IList<ILootGenerator>)m_mobGuildGenerators[mob.GuildName];
            var regionGenerators = (IList<ILootGenerator>)m_mobRegionGenerators[(int)mob.CurrentRegionID];
            IList<ILootGenerator> factionGenerators = null;
            if (mob.Faction != null)
                factionGenerators = (IList<ILootGenerator>)m_mobFactionGenerators[mob.Faction.ID];
            var modelGenerators = (IList<ILootGenerator>)m_mobModelGenerators[mob.Model];
            var bodyTypeGenerators = (IList<ILootGenerator>)m_mobBodyTypeGenerators[mob.BodyType];
            var raceGenerators = (IList<ILootGenerator>)m_mobRaceGenerators[mob.Race];
            var renaissanceGenerators = (IList<ILootGenerator>)m_isRenaissanceGenerators[player.IsRenaissance];
            IList<ILootGenerator> reputationGenerators = (IList<ILootGenerator>)m_isGoodReputGenerators[player.Reputation >= 0];
            var bossGenerators = (IList<ILootGenerator>)m_isBossGenerators[mob.IsBoss];

            List<ILootGenerator> allGenerators = new();

            allGenerators.AddRange(
                m_globalGenerators.Cast<DictionaryEntry>()
                    .Where(e => ((LootConditions)e.Value)?.Matches(mob, player) is null or true)
                    .Select(e => e.Key)
                    .Cast<ILootGenerator>()
            );
            if (nameGenerators != null)
                allGenerators.AddRange(nameGenerators);
            if (guildGenerators != null)
                allGenerators.AddRange(guildGenerators);
            if (regionGenerators != null)
                allGenerators.AddRange(regionGenerators);
            if (factionGenerators != null)
                allGenerators.AddRange(factionGenerators);
            if (modelGenerators != null)
                allGenerators.AddRange(modelGenerators);
            if (bodyTypeGenerators != null)
                allGenerators.AddRange(bodyTypeGenerators);
            if (raceGenerators != null)
                allGenerators.AddRange(raceGenerators);
            if (renaissanceGenerators != null)
                allGenerators.AddRange(renaissanceGenerators);
            if (reputationGenerators != null)
                allGenerators.AddRange(reputationGenerators);
            if (bossGenerators != null)
                allGenerators.AddRange(bossGenerators);

            foreach (ILootGenerator generator in allGenerators)
            {
                if (generator.ExclusivePriority > 0)
                {
                    if (exclusiveGenerator == null || exclusiveGenerator.ExclusivePriority < generator.ExclusivePriority)
                        exclusiveGenerator = generator;
                }

                // if we found a exclusive generator skip adding other generators, since list will only contain exclusive generator.
                if (exclusiveGenerator != null)
                    continue;

                if (!filteredGenerators.Contains(generator))
                    filteredGenerators.Add(generator);
            }

            // if an exclusivegenerator is found only this one is used.
            if (exclusiveGenerator != null)
            {
                filteredGenerators.Clear();
                filteredGenerators.Add(exclusiveGenerator);
            }

            return filteredGenerators;
        }
    }
}
