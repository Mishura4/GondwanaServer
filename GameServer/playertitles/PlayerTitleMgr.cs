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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace DOL.GS.PlayerTitles
{
    /// <summary>
    /// Handles loading of player titles.
    /// </summary>
    public static class PlayerTitleMgr
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Holds all player titles.
        /// </summary>
        private static readonly HashSet<IPlayerTitle> m_titles = new HashSet<IPlayerTitle>();

        /// <summary>
        /// Holds special "empty" title instance.
        /// </summary>
        public static readonly ClearTitle ClearTitle = new ClearTitle();

        public sealed class TaskTitleHolder
        {
            public AdventurerTitle[] Adventurer { get; init; }

            public BountyhunterTitle[] Bountyhunter { get; init; }

            public DemonslayerTitle[] Demonslayer { get; init; }

            public ThiefTitle[] Thief { get; init; }

            public TraderTitle[] Trader { get; init; }

            public WrathTitle[] Wrath { get; init; }

            public DuelistTitle[] Duelist { get; init; }

            internal TaskTitleHolder()
            {
                Adventurer = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(AdventurerTitleLevel1).FullName) as AdventurerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(AdventurerTitleLevel2).FullName) as AdventurerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(AdventurerTitleLevel3).FullName) as AdventurerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(AdventurerTitleLevel4).FullName) as AdventurerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(AdventurerTitleLevel5).FullName) as AdventurerTitle,
                };

                Duelist = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DuelistTitleLevel1).FullName) as DuelistTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DuelistTitleLevel2).FullName) as DuelistTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DuelistTitleLevel3).FullName) as DuelistTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DuelistTitleLevel4).FullName) as DuelistTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DuelistTitleLevel5).FullName) as DuelistTitle,
                };

                Bountyhunter = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(BountyhunterTitleLevel1).FullName) as BountyhunterTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(BountyhunterTitleLevel2).FullName) as BountyhunterTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(BountyhunterTitleLevel3).FullName) as BountyhunterTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(BountyhunterTitleLevel4).FullName) as BountyhunterTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(BountyhunterTitleLevel5).FullName) as BountyhunterTitle,
                };
                
                Demonslayer = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DemonslayerTitleLevel1).FullName) as DemonslayerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DemonslayerTitleLevel2).FullName) as DemonslayerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DemonslayerTitleLevel3).FullName) as DemonslayerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DemonslayerTitleLevel4).FullName) as DemonslayerTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(DemonslayerTitleLevel5).FullName) as DemonslayerTitle,
                };
                
                Thief = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(ThiefTitleLevel1).FullName) as ThiefTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(ThiefTitleLevel2).FullName) as ThiefTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(ThiefTitleLevel3).FullName) as ThiefTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(ThiefTitleLevel4).FullName) as ThiefTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(ThiefTitleLevel5).FullName) as ThiefTitle,
                };
                
                Trader = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(TraderTitleLevel1).FullName) as TraderTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(TraderTitleLevel2).FullName) as TraderTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(TraderTitleLevel3).FullName) as TraderTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(TraderTitleLevel4).FullName) as TraderTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(TraderTitleLevel5).FullName) as TraderTitle,
                };
                
                Wrath = new[]
                {
                    PlayerTitleMgr.GetTitleByTypeName(typeof(WrathTitleLevel1).FullName) as WrathTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(WrathTitleLevel2).FullName) as WrathTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(WrathTitleLevel3).FullName) as WrathTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(WrathTitleLevel4).FullName) as WrathTitle,
                    PlayerTitleMgr.GetTitleByTypeName(typeof(WrathTitleLevel5).FullName) as WrathTitle,
                };
            }
        }
        
        public static TaskTitleHolder TaskTitles { get; private set; }

        /// <summary>
        /// Initializes/loads all known player titles.
        /// </summary>
        /// <returns>true if successful</returns>
        public static bool Init()
        {
            m_titles.Clear();
            foreach (Type t in ScriptMgr.GetDerivedClasses(typeof(IPlayerTitle)))
            {
                if (t == ClearTitle.GetType()) continue;

                IPlayerTitle title;
                try
                {
                    title = (IPlayerTitle)Activator.CreateInstance(t);
                }
                catch (Exception e)
                {
                    log.ErrorFormat("Error loading player title '{0}': {1}", t.FullName, e);
                    continue;
                }

                m_titles.Add(title);
                log.DebugFormat("Loaded player title: {0}", title.GetType().FullName);
            }

            log.InfoFormat("Loaded {0} player titles", m_titles.Count);

            TaskTitles = new TaskTitleHolder();

            return true;
        }

        /// <summary>
        /// Gets all titles that are suitable for player.
        /// </summary>
        /// <param name="player">The player for title checks.</param>
        /// <returns>All title suitable for given player or an empty list if none.</returns>
        public static ICollection GetPlayerTitles(GamePlayer player)
        {
            var titles = new HashSet<IPlayerTitle>();

            titles.Add(ClearTitle);

            return titles.Concat(m_titles.Where(t => t.IsSuitable(player))).ToArray();
        }

        /// <summary>
        /// Gets the title by its type name.
        /// </summary>
        /// <param name="s">The type name to search for.</param>
        /// <returns>Found title or null.</returns>
        public static IPlayerTitle GetTitleByTypeName(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            return m_titles.FirstOrDefault(t => t.GetType().FullName == s);
        }

        /// <summary>
        /// Registers a title.
        /// </summary>
        /// <param name="title">The title to register.</param>
        /// <returns>true if successful.</returns>
        public static bool RegisterTitle(IPlayerTitle title)
        {
            if (title == null)
                return false;

            Type t = title.GetType();

            if (m_titles.Any(ttl => ttl.GetType() == t))
                return false;

            m_titles.Add(title);
            return true;
        }
    }
}
