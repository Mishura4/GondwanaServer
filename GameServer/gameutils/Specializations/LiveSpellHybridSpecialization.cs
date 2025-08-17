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
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    /// <summary>
    /// This is used for a Hybrid Spec Spell Line (Not List Caster)
    /// </summary>
    public class LiveSpellHybridSpecialization : Specialization
    {
        public LiveSpellHybridSpecialization(string keyname, string displayname, ushort icon, int ID)
            : base(keyname, displayname, icon, ID)
        {
        }

        /// <summary>
        /// Is this Specialization Handling Hybrid lists ?
        /// This is always true for Hybrid Specs !
        /// </summary>
        public override bool HybridSpellList
        {
            get { return true; }
        }

        /// <summary>
        /// For Trainer Hybrid Skills aren't summarized !
        /// </summary>
        /// <param name="living"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public override IDictionary<SpellLine, List<Skill>> PretendLinesSpellsForLiving(GameLiving living, int step)
        {
            return base.GetLinesSpellsForLiving(living, step);
        }

        /// <summary>
        /// Get Summarized "Hybrid" Spell Dictionary
        /// List Caster use basic Specialization Getter...
        /// This would have pretty much no reason to be used by GameLiving... (Maybe as a shortcut to force them to use their best spells...)
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        protected override IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living, int level)
        {
            Dictionary<SpellLine, List<Skill>> buffer = new Dictionary<SpellLine, List<Skill>>();
            List<SpellLine> lines = GetSpellLinesForLiving(living, level);

            foreach (SpellLine ls in lines)
            {
                // buffer shouldn't contain duplicate lines
                if (buffer.ContainsKey(ls))
                    continue;

                // Add to Dictionary
                buffer.Add(ls, SelectSkills(ls, living));
            }

            return buffer;
        }
    }
}
