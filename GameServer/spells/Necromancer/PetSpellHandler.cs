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
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.Language;
using DOL.GS.PacketHandler.Client.v168;
using System.Collections.Generic;

namespace DOL.GS.Spells
{
    [SpellHandler("PetSpell")]
    class PetSpellHandler : SpellHandler
    {
        public override bool CastSpell()
        {
            m_spellTarget = Caster.TargetObject as GameLiving;
            bool casted = true;

            if (GameServer.ServerRules.IsAllowedToCastSpell(Caster, m_spellTarget, Spell, SpellLine) && CheckBeginCast(m_spellTarget))
            {
                if (Spell.CastTime > 0)
                {
                    StartCastTimer(m_spellTarget);
                }
                else
                {
                    FinishSpellCast(m_spellTarget);
                }
            }
            else
                casted = false;

            if (!IsCasting)
                OnAfterSpellCastSequence();

            return casted;
        }

        public override int CalculateCastingTime()
        {
            int ticks = m_spell.CastTime;
            ticks = (int)(ticks * Math.Max(m_caster.CastingSpeedReductionCap, m_caster.DexterityCastTimeReduction));
            if (ticks < m_caster.MinimumCastingSpeed)
                ticks = m_caster.MinimumCastingSpeed;
            return ticks;
        }

        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (!base.CheckBeginCast(selectedTarget, quiet))
                return false;

            if (Caster.ControlledBrain == null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "PetSpellHandler.CheckBeginCast.NoControlledBrainForCast"), eChatType.CT_SpellResisted);
                return false;
            }
            return true;
        }

        public override void FinishSpellCast(GameLiving target)
        {
            GamePlayer player = Caster as GamePlayer;

            if (player == null || player.ControlledBrain == null)
                return;

            // No power cost, we'll drain power on the caster when
            // the pet actually starts casting it.
            // If there is an ID, create a sub spell for the pet.

            ControlledNpcBrain petBrain = player.ControlledBrain as ControlledNpcBrain;
            if (petBrain != null && Spell.SubSpellID > 0)
            {
                Spell spell = SkillBase.GetSpellByID(Spell.SubSpellID);
                if (spell != null && spell.SubSpellID == 0)
                {
                    spell.Level = Spell.Level;
                    petBrain.Notify(GameNPCEvent.PetSpell, this,
                        new PetSpellEventArgs(spell, SpellLine, target));
                }
            }

            // Facilitate Painworking.

            if (Spell.RecastDelay > 0 && m_startReuseTimer)
            {
                foreach (Spell spell in SkillBase.GetSpellList(SpellLine.KeyName))
                {
                    if (spell.SpellType == Spell.SpellType &&
                        spell.RecastDelay == Spell.RecastDelay
                        && spell.Group == Spell.Group)
                        Caster.DisableSkill(spell, spell.RecastDelay);
                }
            }
        }

        public PetSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string desc1 = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PetSpell.MainDescription1");
            string desc2 = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PetSpell.MainDescription2");

            Spell subSpell = SkillBase.GetSpellByID(Spell.SubSpellID);
            var subSpellHandler = ScriptMgr.CreateSpellHandler(m_caster, subSpell, null);
            string subSpellDescription = subSpellHandler.GetDelveDescription(delveClient);

            IList<string> allLines = subSpellHandler.DelveInfo;
            var filteredLines = new List<string>();
            foreach (string line in allLines)
            {
                if (line.Equals(subSpellHandler.Spell.Description, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                filteredLines.Add(line);
            }

            string subSpellDelveInfo = string.Join("\n", filteredLines);

            if (Spell.RecastDelay > 0)
            {
                string desc3 = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return desc1 + "\n" + desc2 + "\n\n" + subSpellDescription + "\n\n" + subSpellDelveInfo + "\n\n" + desc3;
            }

            return desc1 + "\n" + desc2 + "\n\n" + subSpellDescription + "\n\n" + subSpellDelveInfo;
        }
    }
}
