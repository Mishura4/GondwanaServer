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
using System.Collections.Generic;
using System.Numerics;

namespace DOL.GS
{
    /// <summary>
    /// the LootMgr holds pointers to all LootGenerators at 
    /// associates the correct LootGenerator with a given Mob
    /// </summary>
    public sealed class LootMgr
    {
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
        static readonly IList m_globalGenerators = new ArrayList();

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
        /// List of Lootgenerators related by isBadReput
        /// </summary>
        static readonly HybridDictionary m_isBadReputGenerators = new HybridDictionary();

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
                    RegisterLootGenerator(generator, dbGenerator.MobName, dbGenerator.MobGuild, dbGenerator.MobFaction, dbGenerator.RegionID, dbGenerator.MobModel, dbGenerator.MobBodyType, dbGenerator.MobRace, dbGenerator.IsRenaissance, dbGenerator.IsGoodReput, dbGenerator.IsBadReput, dbGenerator.IsBoss, dbGenerator.CondMustBeSetTogether);
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
                RegisterLootGenerator(baseGenerator, null, null, null, 0, null, null, null, false, false, false, false, false);
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
            UnRegisterLootGenerator(generator, mobname, mobguild, mobfaction, 0, null, null, null, false, false, false, false, false);
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
        public static void UnRegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction, int mobregion, string mobmodel, string mobbodytype, string mobrace, bool isRenaissance, bool isGoodReput, bool isBadReput, bool isBoss, bool condMustBeSetTogether)
        {
            if (generator == null)
                return;

            // Loot Generator Name Indexed
            if (!Util.IsEmpty(mobname))
            {

                try
                {
                    // Parse CSV
                    List<string> mobNames = Util.SplitCSV(mobname);

                    foreach (string mob in mobNames)
                    {
                        if ((IList)m_mobNameGenerators[mob] != null)
                        {
                            ((IList)m_mobNameGenerators[mob]).Remove(generator);
                        }
                    }

                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobNames for Removing LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            // Loot Generator Guild Indexed
            if (!Util.IsEmpty(mobguild))
            {

                try
                {
                    // Parse CSV
                    List<string> mobGuilds = Util.SplitCSV(mobguild);

                    foreach (string guild in mobGuilds)
                    {
                        if ((IList)m_mobGuildGenerators[guild] != null)
                        {
                            ((IList)m_mobGuildGenerators[guild]).Remove(generator);
                        }
                    }

                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobGuilds for Removing LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            // Loot Generator Faction Indexed
            if (!Util.IsEmpty(mobfaction))
            {

                try
                {
                    // Parse CSV
                    List<string> mobFactions = Util.SplitCSV(mobfaction);

                    foreach (string sfaction in mobFactions)
                    {
                        try
                        {
                            int ifaction = int.Parse(sfaction);

                            if ((IList)m_mobFactionGenerators[ifaction] != null)
                                ((IList)m_mobFactionGenerators[ifaction]).Remove(generator);
                        }
                        catch
                        {
                            if (log.IsDebugEnabled)
                                log.Debug("Could not parse faction [" + sfaction + "] into an integer.");
                        }
                    }

                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobFactions for Removing LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobmodel))
            {
                try
                {
                    List<string> mobModels = Util.SplitCSV(mobmodel);
                    foreach (string model in mobModels)
                    {
                        if ((IList)m_mobModelGenerators[model] != null)
                        {
                            ((IList)m_mobModelGenerators[model]).Remove(generator);
                        }
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobModels for Removing LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobbodytype))
            {
                try
                {
                    // Parse CSV
                    List<string> mobBodyTypes = Util.SplitCSV(mobbodytype);

                    foreach (string bodytype in mobBodyTypes)
                    {
                        if ((IList)m_mobBodyTypeGenerators[bodytype] != null)
                        {
                            ((IList)m_mobBodyTypeGenerators[bodytype]).Remove(generator);
                        }
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobBodyTypes for Removing LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobrace))
            {
                try
                {
                    // Parse CSV
                    List<string> mobRaces = Util.SplitCSV(mobrace);

                    foreach (string race in mobRaces)
                    {
                        if ((IList)m_mobRaceGenerators[race] != null)
                        {
                            ((IList)m_mobRaceGenerators[race]).Remove(generator);
                        }
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobRaces for Removing LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (isRenaissance)
            {
                if ((IList)m_isRenaissanceGenerators[isRenaissance.ToString()] != null)
                {
                    ((IList)m_isRenaissanceGenerators[isRenaissance.ToString()]).Remove(generator);
                }
            }

            if (isGoodReput)
            {
                if ((IList)m_isGoodReputGenerators[isGoodReput.ToString()] != null)
                {
                    ((IList)m_isGoodReputGenerators[isGoodReput.ToString()]).Remove(generator);
                }
            }

            if (isBadReput)
            {
                if ((IList)m_isBadReputGenerators[isBadReput.ToString()] != null)
                {
                    ((IList)m_isBadReputGenerators[isBadReput.ToString()]).Remove(generator);
                }
            }

            if (isBoss)
            {
                if ((IList)m_isBossGenerators[isBoss.ToString()] != null)
                {
                    ((IList)m_isBossGenerators[isBoss.ToString()]).Remove(generator);
                }
            }

            // Loot Generator Region Indexed
            if (mobregion > 0)
            {
                IList regionList = (IList)m_mobRegionGenerators[mobregion];
                if (regionList != null)
                {
                    regionList.Remove(generator);
                }
            }

            if (Util.IsEmpty(mobname) && Util.IsEmpty(mobguild) && Util.IsEmpty(mobfaction) && Util.IsEmpty(mobmodel) && Util.IsEmpty(mobbodytype) && Util.IsEmpty(mobrace) && !isRenaissance && !isGoodReput && !isBadReput && !isBoss && mobregion == 0)
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
        /// <param name="isBadReput"></param>
        /// <param name="isBoss"></param>
        /// <param name="condMustBeSetTogether"></param>
        public static void RegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction, int mobregion, string mobmodel, string mobbodytype, string mobrace, bool isRenaissance, bool isGoodReput, bool isBadReput, bool isBoss, bool condMustBeSetTogether)
        {
            if (generator == null)
                return;

            // Loot Generator Name Indexed
            if (!Util.IsEmpty(mobname))
            {
                // Parse CSV
                try
                {
                    List<string> mobNames = Util.SplitCSV(mobname);

                    foreach (string mob in mobNames)
                    {
                        if ((IList)m_mobNameGenerators[mob] == null)
                        {
                            m_mobNameGenerators[mob] = new ArrayList();
                        }
                        ((IList)m_mobNameGenerators[mob]).Add(generator);
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobNames for Registering LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            // Loot Generator Guild Indexed
            if (!Util.IsEmpty(mobguild))
            {
                // Parse CSV
                try
                {
                    List<string> mobGuilds = Util.SplitCSV(mobguild);

                    foreach (string guild in mobGuilds)
                    {
                        if ((IList)m_mobGuildGenerators[guild] == null)
                        {
                            m_mobGuildGenerators[guild] = new ArrayList();
                        }
                        ((IList)m_mobGuildGenerators[guild]).Add(generator);
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobGuilds for Registering LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            // Loot Generator Mob Faction Indexed
            if (!Util.IsEmpty(mobfaction))
            {
                // Parse CSV
                try
                {
                    List<string> mobFactions = Util.SplitCSV(mobfaction);

                    foreach (string sfaction in mobFactions)
                    {
                        try
                        {
                            int ifaction = int.Parse(sfaction);

                            if ((IList)m_mobFactionGenerators[ifaction] == null)
                                m_mobFactionGenerators[ifaction] = new ArrayList();

                            ((IList)m_mobFactionGenerators[ifaction]).Add(generator);
                        }
                        catch
                        {
                            if (log.IsDebugEnabled)
                                log.Debug("Could not parse faction string [" + sfaction + "] into an integer.");
                        }
                    }// foreach
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobFactions for Registering LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobmodel))
            {
                try
                {
                    List<string> mobModels = Util.SplitCSV(mobmodel);
                    foreach (string model in mobModels)
                    {
                        if ((IList)m_mobModelGenerators[model] == null)
                        {
                            m_mobModelGenerators[model] = new ArrayList();
                        }
                        ((IList)m_mobModelGenerators[model]).Add(generator);
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobModels for Registering LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobbodytype))
            {
                // Parse CSV
                try
                {
                    List<string> mobBodyTypes = Util.SplitCSV(mobbodytype);

                    foreach (string bodytype in mobBodyTypes)
                    {
                        if ((IList)m_mobBodyTypeGenerators[bodytype] == null)
                        {
                            m_mobBodyTypeGenerators[bodytype] = new ArrayList();
                        }
                        ((IList)m_mobBodyTypeGenerators[bodytype]).Add(generator);
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobBodyTypes for Registering LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (!Util.IsEmpty(mobrace))
            {
                // Parse CSV
                try
                {
                    List<string> mobRaces = Util.SplitCSV(mobrace);

                    foreach (string race in mobRaces)
                    {
                        if ((IList)m_mobRaceGenerators[race] == null)
                        {
                            m_mobRaceGenerators[race] = new ArrayList();
                        }
                        ((IList)m_mobRaceGenerators[race]).Add(generator);
                    }
                }
                catch
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Could not Parse mobRaces for Registering LootGenerator : " + generator.GetType().FullName);
                    }
                }
            }

            if (isRenaissance)
            {
                if ((IList)m_isRenaissanceGenerators[isRenaissance.ToString()] == null)
                {
                    m_isRenaissanceGenerators[isRenaissance.ToString()] = new ArrayList();
                }
                ((IList)m_isRenaissanceGenerators[isRenaissance.ToString()]).Add(generator);
            }

            if (isGoodReput)
            {
                if ((IList)m_isGoodReputGenerators[isGoodReput.ToString()] == null)
                {
                    m_isGoodReputGenerators[isGoodReput.ToString()] = new ArrayList();
                }
                ((IList)m_isGoodReputGenerators[isGoodReput.ToString()]).Add(generator);
            }

            if (isBadReput)
            {
                if ((IList)m_isBadReputGenerators[isBadReput.ToString()] == null)
                {
                    m_isBadReputGenerators[isBadReput.ToString()] = new ArrayList();
                }
                ((IList)m_isBadReputGenerators[isBadReput.ToString()]).Add(generator);
            }

            if (isBoss)
            {
                if ((IList)m_isBossGenerators[isBoss.ToString()] == null)
                {
                    m_isBossGenerators[isBoss.ToString()] = new ArrayList();
                }
                ((IList)m_isBossGenerators[isBoss.ToString()]).Add(generator);
            }

            // Loot Generator Region Indexed
            if (mobregion > 0)
            {
                IList regionList = (IList)m_mobRegionGenerators[mobregion];
                if (regionList == null)
                {
                    regionList = new ArrayList();
                    m_mobRegionGenerators[mobregion] = regionList;
                }
                regionList.Add(generator);
            }

            if (Util.IsEmpty(mobname) && Util.IsEmpty(mobguild) && Util.IsEmpty(mobfaction) && Util.IsEmpty(mobmodel) && Util.IsEmpty(mobbodytype) && Util.IsEmpty(mobrace) && !isRenaissance && !isGoodReput && !isBadReput && !isBoss && mobregion == 0)
            {
                m_globalGenerators.Add(generator);
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
                foreach (ILootGenerator gen in m_globalGenerators)
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
            IList filteredGenerators = new ArrayList();
            ILootGenerator exclusiveGenerator = null;

            IList nameGenerators = (IList)m_mobNameGenerators[mob.Name];
            IList guildGenerators = (IList)m_mobGuildGenerators[mob.GuildName];
            IList regionGenerators = (IList)m_mobRegionGenerators[(int)mob.CurrentRegionID];
            IList factionGenerators = null;
            if (mob.Faction != null)
                factionGenerators = (IList)m_mobFactionGenerators[mob.Faction.ID];
            IList modelGenerators = (IList)m_mobModelGenerators[mob.Model.ToString()];
            IList bodyTypeGenerators = (IList)m_mobBodyTypeGenerators[mob.BodyType.ToString()];
            IList raceGenerators = (IList)m_mobRaceGenerators[mob.Race.ToString()];
            IList renaissanceGenerators = (IList)m_isRenaissanceGenerators[player.IsRenaissance.ToString()];
            IList goodReputGenerators = player.Reputation == 0 ? (IList)m_isGoodReputGenerators["True"] : null;
            IList badReputGenerators = player.Reputation < 0 ? (IList)m_isBadReputGenerators["True"] : null;
            IList bossGenerators = (IList)m_isBossGenerators[mob.IsBoss.ToString()];

            ArrayList allGenerators = new ArrayList();

            allGenerators.AddRange(m_globalGenerators);

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
            if (goodReputGenerators != null)
                allGenerators.AddRange(goodReputGenerators);
            if (badReputGenerators != null)
                allGenerators.AddRange(badReputGenerators);
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

                if (generator is LootGenerator lootGenerator && lootGenerator.CondMustBeSetTogether)
                {
                    if (!ConditionsMetTogether(lootGenerator, mob, player))
                        continue;
                }

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

        private static bool ConditionsMetTogether(LootGenerator generator, GameNPC mob, GamePlayer player)
        {
            var conditionsMet = true;

            if (!Util.IsEmpty(generator.MobModel) && generator.MobModel != mob.Model.ToString())
                conditionsMet = false;
            if (!Util.IsEmpty(generator.MobBodyType) && generator.MobBodyType != mob.BodyType.ToString())
                conditionsMet = false;
            if (!Util.IsEmpty(generator.MobRace) && generator.MobRace != mob.Race.ToString())
                conditionsMet = false;
            if (generator.IsRenaissance && !player.IsRenaissance)
                conditionsMet = false;
            if (generator.IsGoodReput && player.Reputation != 0)
                conditionsMet = false;
            if (generator.IsBadReput && player.Reputation >= 0)
                conditionsMet = false;
            if (generator.IsBoss && !mob.IsBoss)
                conditionsMet = false;

            return conditionsMet;
        }
    }
}
