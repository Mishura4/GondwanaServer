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
using DOL.Database;
using DOL.GS.Styles;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    /// <summary>
    /// callback handler for a spec that is activated by clicking on an associated icon
    /// </summary>
    public interface ISpecActionHandler
    {
        void Execute(Specialization ab, GamePlayer player);
    }

    /// <summary>
    /// Specialization can be in some way an ability too and can have icons then
    /// its level depends from skill points that were spent to it through trainers
    /// </summary>
    public class Specialization : NamedSkill
    {
        /// <summary>
        /// Level required for this spec (used for sorting)
        /// can be negative for sorting, as long as Spec Level is positive it will match
        /// </summary>
        private int m_levelRequired = 0;

        /// <summary>
        /// Script Constructor
        /// </summary>
        public Specialization(string keyname, string displayname, ushort icon)
            : this(keyname, displayname, icon, icon)
        {

        }
        /// <summary>
        /// Default constructor
        /// </summary>
        public Specialization(string keyname, string displayname, ushort icon, int ID)
            : base(keyname, displayname, ID, icon, 1, ID)
        {
        }

        /// <summary>
        /// Level Required for this Specialization
        /// Don't change this unless you know what you're doing !
        /// </summary>
        public int LevelRequired
        {
            get { return m_levelRequired; }
            set { m_levelRequired = value; }
        }


        /// <summary>
        /// type of skill
        /// </summary>
        public override eSkillPage SkillType
        {
            get
            {
                return eSkillPage.Specialization;
            }
        }

        /// <summary>
        /// Is this Specialization Trainable ?
        /// </summary>
        public virtual bool Trainable
        {
            get { return true; }
        }

        /// <summary>
        /// Can This Specialization be saved in Player record ?
        /// </summary>
        public virtual bool AllowSave
        {
            get { return true; }
        }

        /// <summary>
        /// Is this Specialization Handling Hybrid lists ?
        /// </summary>
        public virtual bool HybridSpellList
        {
            get { return false; }
        }

        #region Getters

        /// <summary>
        /// Default getter for SpellLines
        /// Retrieve spell line depending on advanced class and class hint
        /// Order by Baseline
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public virtual List<SpellLine> GetSpellLinesForLiving(GameLiving living)
        {
            return GetSpellLinesForLiving(living, GetSpecLevelForLiving(living));
        }

        /// <summary>
        /// Default getter for SpellLines with a "step" hint used to display future upgrade
        /// Retrieve spell line depending on advanced class and class hint
        /// Order by Baseline
        /// </summary>
        /// <param name="living"></param>
        /// <param name="step">step is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        public virtual List<SpellLine> PretendSpellLinesForLiving(GameLiving living, int step)
        {
            return GetSpellLinesForLiving(living, step);
        }

        /// <summary>
        /// Default getter for SpellLines
        /// Retrieve spell line depending on advanced class and class hint
        /// Order by Baseline
        /// </summary>
        /// <param name="living"></param>
        /// <param name="level">level is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        protected virtual List<SpellLine> GetSpellLinesForLiving(GameLiving living, int level)
        {
            List<SpellLine> list = new List<SpellLine>();
            IList<Tuple<SpellLine, int>> spsl = SkillBase.GetSpecsSpellLines(KeyName);

            // Get Spell Lines by order of appearance
            if (living is GamePlayer)
            {

                GamePlayer player = (GamePlayer)living;

                // select only spec line if is advanced class...
                var tmp = spsl.Where(item => (item.Item1.IsBaseLine || player.CharacterClass.HasAdvancedFromBaseClass()))
                    .OrderBy(item => (item.Item1.IsBaseLine ? 0 : 1)).ThenBy(item => item.Item1.ID);

                // try with class hint
                var baseline = tmp.Where(item => item.Item1.IsBaseLine && item.Item2 == player.CharacterClass.ID);
                if (baseline.Any())
                {
                    foreach (Tuple<SpellLine, int> ls in baseline)
                    {
                        ls.Item1.Level = player.Level;
                        list.Add(ls.Item1);
                    }
                }
                else
                {
                    foreach (Tuple<SpellLine, int> ls in tmp.Where(item => item.Item1.IsBaseLine && item.Item2 == 0))
                    {
                        ls.Item1.Level = player.Level;
                        list.Add(ls.Item1);
                    }
                }

                // try spec with class hint
                var specline = tmp.Where(item => !item.Item1.IsBaseLine && item.Item2 == player.CharacterClass.ID);
                if (specline.Any())
                {
                    foreach (Tuple<SpellLine, int> ls in specline)
                    {
                        ls.Item1.Level = level;
                        list.Add(ls.Item1);
                    }
                }
                else
                {
                    foreach (Tuple<SpellLine, int> ls in tmp.Where(item => !item.Item1.IsBaseLine && item.Item2 == 0))
                    {
                        ls.Item1.Level = level;
                        list.Add(ls.Item1);
                    }
                }

            }
            else
            {
                // default - not a player, add all...
                foreach (Tuple<SpellLine, int> ls in spsl.OrderBy(item => (item.Item1.IsBaseLine ? 0 : 1)).ThenBy(item => item.Item1.ID))
                {
                    // default living spec is (Level * 0.66 + 1) on Live (no real proof...)
                    // here : Level - (Level / 4) = 0.75
                    if (ls.Item1.IsBaseLine)
                        ls.Item1.Level = living.Level;
                    else
                        ls.Item1.Level = Math.Max(1, living.Level - (living.Level >> 2));
                    list.Add(ls.Item1);
                }
            }

            return list;
        }

        /// <summary>
        /// Default Getter For Spells
        /// Retrieve Spell index by SpellLine, List Spell by Level Order
        /// Select Only enabled Spells by spec or living level constraint.
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public virtual IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living)
        {
            return GetLinesSpellsForLiving(living, GetSpecLevelForLiving(living));
        }

        /// <summary>
        /// Getter For Spells with a "step" hint used to display future upgrade
        /// Retrieve Spell index by SpellLine, List Spell by Level Order
        /// Select Only enabled Spells by spec or living level constraint.
        /// </summary>
        /// <param name="living"></param>
        /// <param name="step">step is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        public virtual IDictionary<SpellLine, List<Skill>> PretendLinesSpellsForLiving(GameLiving living, int step)
        {
            IDictionary<SpellLine, List<Skill>> dict = new Dictionary<SpellLine, List<Skill>>();
            foreach (SpellLine sl in GetSpellLinesForLiving(living, step))
            {
                var spells = SkillBase.GetSpellList(sl.KeyName)
                    .Where(item => item.Level <= sl.Level)
                    .OrderBy(item => item.Level)
                    .ThenBy(item => item.ID).Cast<Skill>().ToList();
                dict.Add(sl, spells);
            }

            return dict;
        }

        /// <summary>
        /// Default Getter For Spells
        /// Retrieve Spell index by SpellLine, List Spell by Level Order
        /// Select Only enabled Spells by spec or living level constraint.
        /// </summary>
        /// <param name="living"></param>
        /// <param name="level">level is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        protected virtual IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(GameLiving living, int level)
        {
            IDictionary<SpellLine, List<Skill>> dict = new Dictionary<SpellLine, List<Skill>>();
            foreach (SpellLine sl in GetSpellLinesForLiving(living, level))
            {
                dict.Add(sl, SelectSkills(sl, living));
            }

            return dict;
        }

        /// <summary>
        /// Get skills from the skill line & filter them
        /// Base method filters by spell line level
        /// </summary>
        /// <param name="sl">Skill line to get skills from</param>
        /// <param name="living">Living to filter for</param>
        /// <returns></returns>
        protected virtual List<Skill> SelectSkills(SpellLine sl, GameLiving living)
        {
            var lineMaxVersions = AllowMultipleSpellVersions(sl, living);
            if (lineMaxVersions <= 0)
            {
                return SkillBase.GetSpellList(sl.KeyName)
                    .Where(item => item.Level <= sl.Level)
                    .OrderBy(item => item.Level)
                    .ThenBy(item => item.ID).Cast<Skill>().ToList();
            }
            else
            {
                // An hybrid dictionary is composed of a spellline "base"
                // SpecLine and Baseline are mixed in spellline named "base"
                // baseline are displayed first (improvement are easier this way)
                // specline are displayed secondly (ordered in appareance order)
                // some class/spelline are allowed to display the "2 -best" spell
                // this is hardcoded in AllowMultipleSpellVersions...
                IEnumerable<Spell> allSpells = SkillBase.GetSpellList(sl.KeyName).Where(item => item.Level <= sl.Level);

                IEnumerable<IEnumerable<Spell>> spellsGroupedByType =
                    allSpells.Where(item => item.Group == 0)
                        .GroupBy(item => new { item.SpellType, item.Target, item.IsAoE, item.IsInstantCast, item.HasSubSpell })
                        .Select(group => group.OrderByDescending(it => it.Level).Take(lineMaxVersions));

                IEnumerable<IEnumerable<Spell>> spellsGroupedByGroupId =
                    allSpells.Where(item => item.Group != 0)
                        .GroupBy(item => item.Group)
                        .Select(group => group.OrderByDescending(it => it.Level).Take(lineMaxVersions));

                // sort by reverse level for multiple version
                List<Skill> spellsFilteredByGroup =
                    spellsGroupedByGroupId
                        .SelectMany(el => el)
                        .Union(spellsGroupedByType.SelectMany(el => el))
                        .Where(item => item != null) // TODO: baseline ordering?
                        .OrderByDescending(item => item.Level)
                        .Cast<Skill>()
                        .ToList();
                
                return spellsFilteredByGroup;
            }
        }

        /// <summary>		
        /// Should we allow multiple versions of each spell type in this spell line
        /// Used for hybrid classes
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        protected virtual int AllowMultipleSpellVersions(SpellLine line, GameLiving living)
        {
            if (living is GamePlayer player)
            {
                switch (line.Spec)
                {
                    case Specs.Enhancement:
                        if ((line.IsBaseLine || player.CharacterClass.ID == (int)eCharacterClass.Cleric) && player.CharacterClass.ID != (int)eCharacterClass.Heretic)
                            return 2;
                        break;

                    case Specs.Nurture:
                        if (line.IsBaseLine || player.CharacterClass.ID == (int)eCharacterClass.Druid)
                            return 2;
                        break;
                }
                
            }

            return line.MaxSpellVersions;
        }

        /// <summary>
        /// Default getter for Ability
        /// Return Abilities it lists depending on spec level
        /// Override to change the condition...
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public virtual List<Ability> GetAbilitiesForLiving(GameLiving living)
        {
            return GetAbilitiesForLiving(living, GetSpecLevelForLiving(living));
        }

        /// <summary>
        /// Getter for Ability with a "step" hint used to display future upgrade
        /// Return Abilities it lists depending on spec level
        /// Override to change the condition...
        /// </summary>
        /// <param name="living"></param>
        /// <param name="step">step is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        public virtual List<Ability> PretendAbilitiesForLiving(GameLiving living, int step)
        {
            return SkillBase.GetSpecAbilityList(KeyName, living is GamePlayer ? ((GamePlayer)living).CharacterClass.ID : 0)
                .Where(k => k.SpecLevelRequirement <= step)
                .OrderBy(k => k.SpecLevelRequirement).ToList();
        }

        /// <summary>
        /// Default getter for Ability
        /// Return Abilities it lists depending on spec level
        /// Override to change the condition...
        /// </summary>
        /// <param name="living"></param>
        /// <param name="level">level is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        protected virtual List<Ability> GetAbilitiesForLiving(GameLiving living, int level)
        {
            // Select only Enabled and Max Level Abilities
            List<Ability> abs = SkillBase.GetSpecAbilityList(KeyName, living is GamePlayer ? ((GamePlayer)living).CharacterClass.ID : 0);

            // Get order of first appearing skills
            IOrderedEnumerable<Ability> order = abs.GroupBy(item => item.KeyName)
                .Select(ins => ins.OrderBy(it => it.SpecLevelRequirement).First())
                .Where(item => item.SpecLevelRequirement <= level)
                .OrderBy(item => item.SpecLevelRequirement)
                .ThenBy(item => item.ID);

            // Get best of skills
            List<Ability> best = abs.Where(item => item.SpecLevelRequirement <= level)
                .GroupBy(item => item.KeyName)
                .Select(ins => ins.OrderByDescending(it => it.SpecLevelRequirement).First()).ToList();

            List<Ability> results = new List<Ability>();
            // make some kind of "Join" between the order of appearance and the best abilities.
            foreach (Ability ab in order)
            {
                for (int r = 0; r < best.Count; r++)
                {
                    if (best[r].KeyName == ab.KeyName)
                    {
                        results.Add(best[r]);
                        best.RemoveAt(r);
                        break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Default Getter For Styles
        /// Return Styles depending on spec level
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public virtual List<Style> GetStylesForLiving(GameLiving living)
        {
            return GetStylesForLiving(living, GetSpecLevelForLiving(living));
        }

        /// <summary>
        /// Getter For Styles with a "step" hint used to display future upgrade
        /// Return Styles depending on spec level
        /// </summary>
        /// <param name="living"></param>
        /// <param name="step">step is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        public virtual List<Style> PretendStylesForLiving(GameLiving living, int step)
        {
            return GetStylesForLiving(living, step);
        }

        /// <summary>
        /// Default Getter For Styles
        /// Return Styles depending on spec level
        /// </summary>
        /// <param name="living"></param>
        /// <param name="level">level is only used when called for pretending some level (for trainer display)</param>
        /// <returns></returns>
        protected virtual List<Style> GetStylesForLiving(GameLiving living, int level)
        {
            // Try with Class ID 0 if no class id styles
            int classid = 0;
            if (living is GamePlayer)
            {
                classid = ((GamePlayer)living).CharacterClass.ID;
            }

            List<Style> styles = null;
            if (classid == 0)
            {
                styles = SkillBase.GetStyleList(KeyName, classid);
            }
            else
            {
                styles = SkillBase.GetStyleList(KeyName, classid);

                if (styles.Count == 0)
                    styles = SkillBase.GetStyleList(KeyName, 0);
            }

            // Select only enabled Styles and Order them
            return styles.Where(item => item.SpecLevelRequirement <= level)
                .OrderBy(item => item.SpecLevelRequirement)
                .ThenBy(item => item.ID).ToList();

        }

        public virtual int GetSpecLevelForLiving(GameLiving living)
        {
            return Level;
        }
        #endregion
    }

    public class UntrainableSpecialization : Specialization
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public UntrainableSpecialization(string keyname, string displayname, ushort icon, int ID)
            : base(keyname, displayname, icon, ID)
        {
        }

        /// <summary>
        /// Is this Specialization Trainable ?
        /// </summary>
        public override bool Trainable
        {
            get { return false; }
        }
    }

    public class CareerSpecialization : UntrainableSpecialization
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public CareerSpecialization(string keyname, string displayname, ushort icon, int ID)
            : base(keyname, displayname, icon, ID)
        {
        }

        /// <summary>
        /// Can This Specialization be saved in Player record ?
        /// </summary>
        public override bool AllowSave
        {
            get { return false; }
        }

        /// <summary>
        /// Career level are always considered spec'ed up to user level
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public override int GetSpecLevelForLiving(GameLiving living)
        {
            return Math.Max(0, (int)living.Level);
        }
    }
}
