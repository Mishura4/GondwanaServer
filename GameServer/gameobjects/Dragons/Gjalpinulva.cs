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
using System.Text;
using DOL.Database;
using DOL.Events;
using System.Reflection;
using System.Collections;
using DOL.AI.Brain;
using DOL.GS.Geometry;

namespace DOL.GS
{
    /// <summary>
    /// The Midgard dragon.
    /// </summary>
    /// <author>Aredhel</author>	
    public class Gjalpinulva : GameDragon
    {
        #region Add Spawns

        private ArrayList m_retrieverList = new ArrayList();

        /// <summary>
        /// Spawn dogs that will despawn again after 60 seconds; there is
        /// a 25% chance that a retriever will spawn.
        /// </summary>
        /// <returns>Whether or not any retrievers were spawned.</returns>
        public override bool CheckAddSpawns()
        {
            base.CheckAddSpawns();  // In order to reset HealthPercentOld.

            Yell("Come to me my babies!");

            GameNPC dogSpawn;
            bool isRetriever = false;
            m_retrieverList.Clear();
            int numAdds = Math.Max(1, PlayersInLair * 2 + 1);

            // Spawn dogs, in most cases (75% chance) these dogs will be level 
            // 37 and con green (decoys), in some cases (25%) they will be
            // retrievers, who will try to get out of the lair and, if successful,
            // cause Gjalpinulva to spawn a couple of deep purple adds.

            for (int dog = 1; dog <= numAdds; ++dog)
            {
                isRetriever = Util.Chance(25);
                var spawnCoordinate = Coordinate + Vector.Create(x: Util.Random(300, 600), y: Util.Random(300, 600));
                dogSpawn = SpawnTimedAdd((isRetriever) ? 610 : 611, (isRetriever) ? Util.Random(47, 53) : 37, spawnCoordinate, 60, isRetriever);

                // We got a retriever, tell it who its master is and which exit
                // to run to.

                if (isRetriever)
                {
                    if (dogSpawn != null && dogSpawn.Brain != null && dogSpawn.Brain is RetrieverMobBrain)
                    {
                        (dogSpawn.Brain as RetrieverMobBrain).Master = this;
                        m_retrieverList.Add(dogSpawn);
                        dogSpawn.PathTo(GetExitCoordinates(Util.Random(1, 4)), 200);    // Pick 1 out of 4 possible exits.
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Get coordinates for the given exit (1 = NW, 2 = SW, 
        /// 3 = SE, 4 = NE).
        /// </summary>
        /// <returns>Coordinates.</returns>
        private Coordinate GetExitCoordinates(int exitNo)
        {
            // Get target coordinates (hardcoded). Yeah I know, this is
            // ugly, but to get this right NPC pathing is a must; as it
            // is at the moment, there is no way of knowing where the exits 
            // are (from the PoV of an NPC).

            switch (exitNo)
            {
                case 1: return Coordinate.Create(x: 707026, y: 1019564 );
                case 2: return Coordinate.Create(x: 706924, y: 1023596 );
                case 3: return Coordinate.Create(x: 711441, y: 1023175 );
                case 4: return Coordinate.Create(x: 710708, y: 1018894 );
                default: return SpawnPosition.Coordinate;
            }
        }

        /// <summary>
        /// Invoked when retriever type mob has reached its target location.
        /// </summary>
        /// <param name="sender">The retriever mob.</param>
        public override void OnRetrieverArrived(GameNPC sender)
        {
            base.OnRetrieverArrived(sender);
            if (sender == null || sender == this) return;

            // Spawn nasty adds.

            if (m_retrieverList.Contains(sender))
                SpawnDrakulvs(Util.Random(7, 10), sender.Coordinate);
        }

        /// <summary>
        /// Spawn some (10-20) nasty level 62-68 drakulvs around the spot the
        /// retriever has reported back from, then make these spawns aggro the
        /// raid inside the lair.
        /// </summary>
        /// <param name="numAdds"></param>
        private void SpawnDrakulvs(int numAdds, Coordinate coordinate)
        {
            GameNPC drakulv;
            bool isDisciple = false;
            for (int add = 0; add < numAdds; ++add)
            {
                isDisciple = Util.Chance(25);
                var randomCoordinate = coordinate + Vector.Create(x: Util.Random(250), y: Util.Random(250));
                drakulv = SpawnTimedAdd((isDisciple) ? 613 : 612, Util.Random(62, 68), randomCoordinate, 120, false);

                if (drakulv != null && drakulv.Brain is StandardMobBrain && this.Brain is DragonBrain)
                {
                    (Brain as DragonBrain).AddAggroListTo(drakulv.Brain as StandardMobBrain);
                }
            }
        }

        #endregion

        #region Glare

        /// <summary>
        /// The Glare spell.
        /// </summary>
        protected override Spell Glare
        {
            get
            {
                if (m_glareSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 5701;
                    spell.Description = "Glare";
                    spell.Name = "Dragon Glare";
                    spell.Range = 2500;
                    spell.Radius = 700;
                    spell.Damage = 2000 * DragonDifficulty / 100;
                    spell.RecastDelay = 10;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.SpellID = 6001;
                    spell.Target = "enemy";
                    spell.Type = "DirectDamage";
                    m_glareSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_glareSpell);
                }
                return m_glareSpell;
            }
        }

        #endregion

        #region Breath

        /// <summary>
        /// The Breath spell.
        /// </summary>
        protected override Spell Breath
        {
            get
            {
                if (m_breathSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 208;
                    spell.Description = "Nuke";
                    spell.Name = "Dragon Nuke";
                    spell.Range = 700;
                    spell.Radius = 700;
                    spell.Damage = 2000 * DragonDifficulty / 100;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.SpellID = 6012;
                    spell.Target = "enemy";
                    spell.Type = "DirectDamage";
                    m_breathSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_breathSpell);
                }
                return m_breathSpell;
            }
        }

        /// <summary>
        /// The resist debuff spell.
        /// </summary>
        protected override Spell ResistDebuff
        {
            get
            {
                if (m_resistDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 778;
                    spell.Icon = 778;
                    spell.Description = "Cold Resist Debuff";
                    spell.Name = "Biting Cold";
                    spell.Range = 700;
                    spell.Radius = 700;
                    spell.Value = 30;
                    spell.Duration = 30 * DragonDifficulty / 100;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.SpellID = 6013;
                    spell.Target = "enemy";
                    spell.Type = "ColdResistDebuff";
                    spell.Message1 = "You feel more vulnerable to cold!";
                    spell.Message2 = "{0} seems vulnerable to cold!";
                    m_resistDebuffSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_resistDebuffSpell);
                }
                return m_resistDebuffSpell;
            }
        }

        #endregion

        #region Melee Debuff

        /// <summary>
        /// The melee debuff spell.
        /// </summary>
        protected override Spell MeleeDebuff
        {
            get
            {
                if (m_meleeDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 13082;
                    spell.Icon = 13082;
                    spell.Description = "Fumble Chance Debuff";
                    spell.Name = "Growing Trepidation";
                    spell.Range = 700;
                    spell.Radius = 700;
                    spell.Value = 50;
                    spell.Duration = 90 * DragonDifficulty / 100;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.SpellID = 6003;
                    spell.Target = "enemy";
                    spell.Type = "FumbleChanceDebuff";
                    m_meleeDebuffSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_meleeDebuffSpell);
                }
                return m_meleeDebuffSpell;
            }
        }

        #endregion

        #region Ranged Debuff

        /// <summary>
        /// The ranged debuff spell.
        /// </summary>
        protected override Spell RangedDebuff
        {
            get
            {
                if (m_rangedDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 590;
                    spell.Icon = 590;
                    spell.Description = "Nearsight";
                    spell.Name = "Dazzling Light";
                    spell.Range = 700;
                    spell.Radius = 700;
                    spell.Value = 100;
                    spell.Duration = 90 * DragonDifficulty / 100;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.SpellID = 6003;
                    spell.Target = "enemy";
                    spell.Type = "Nearsight";
                    spell.Message1 = "You are blinded!";
                    spell.Message2 = "{0} is blinded!";
                    m_rangedDebuffSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_rangedDebuffSpell);
                }
                return m_rangedDebuffSpell;
            }
        }

        #endregion
    }
}