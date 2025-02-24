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
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    ///
    /// </summary>
    [SpellHandlerAttribute("PowerHeal")]
    public class PowerHealSpellHandler : SpellHandler
    {
        // constructor
        public PowerHealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
        /// <summary>
        /// Execute heal spell
        /// </summary>
        /// <param name="target"></param>
        protected override bool ExecuteSpell(GameLiving target, bool force = false)
        {
            var targets = SelectTargets(target, force);
            if (targets.Count <= 0) return false;

            bool healed = false;

            if (Spell.Value < 0 && m_caster is GamePlayer player)
                Spell.Value = (Spell.Value * -0.01) * player.MaxMana;

            int spellValue = (int)Math.Round(Spell.Value);

            foreach (GameLiving healTarget in targets)
            {
                if (healTarget is GamePlayer
                    && (
                    ((GamePlayer)healTarget).CharacterClass is PlayerClass.ClassVampiir
                    || ((GamePlayer)healTarget).CharacterClass is PlayerClass.ClassMaulerAlb
                    || ((GamePlayer)healTarget).CharacterClass is PlayerClass.ClassMaulerHib
                    || ((GamePlayer)healTarget).CharacterClass is PlayerClass.ClassMaulerMid))
                    continue;

                if (Spell.Value < 0)
                    // Restore a percentage of the target's mana
                    spellValue = (int)Math.Round((Spell.Value * -0.01) * healTarget.MaxMana);

                healed |= HealTarget(healTarget, spellValue);
            }

            // group heals seem to use full power even if no heals
            if (!healed && Spell.Target == "realm")
                m_caster.Mana -= PowerCost(target) >> 1; // only 1/2 power if no heal
            else
                m_caster.Mana -= PowerCost(target);

            // send animation for non pulsing spells only
            if (Spell.Pulse == 0)
            {
                if (healed)
                {
                    // send animation on all targets if healed
                    foreach (GameLiving healTarget in targets)
                        SendEffectAnimation(healTarget, 0, false, 1);
                }
                else
                {
                    // show resisted effect if not healed
                    SendEffectAnimation(Caster, 0, false, 0);
                }
            }

            if (!healed && Spell.CastTime == 0) m_startReuseTimer = false;

            return true;
        }

        /// <summary>
        /// Heals hit points of one target and sends needed messages, no spell effects
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount">amount of hit points to heal</param>
        /// <returns>true if heal was done</returns>
        public virtual bool HealTarget(GameLiving target, int amount)
        {
            if (target == null || target.ObjectState != GameLiving.eObjectState.Active) return false;

            // we can't heal enemy people
            if (!(Caster is TextNPC) && !GameServer.ServerRules.IsSameRealm(Caster, target, true))
                return false;

            if (!target.IsAlive)
            {
                //"You cannot heal the dead!" sshot550.tga
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "Spell.LifeTransfer.TargetDead", target.GetName(0, true)), eChatType.CT_SpellResisted);
                return false;
            }

            int heal = target.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, amount);

            if (heal == 0)
            {
                if (Spell.Pulse == 0)
                {
                    if (target == m_caster) MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PetConversion.ManaFull"), eChatType.CT_SpellResisted);
                    else MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PowerHeal.FullPowerOther", target.GetName(0, true)), eChatType.CT_SpellResisted);
                }
                return false;
            }

            if (m_caster == target)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PowerHeal.RestorePowerSelf", heal), eChatType.CT_Spell);
                if (heal < amount)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PowerHeal.FullPowerSelf"), eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PowerHeal.RestorePowerOther", target.GetName(0, false), heal), eChatType.CT_Spell);
                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.PowerHeal.PowerRestored", m_caster.GetName(0, false), heal), eChatType.CT_Spell);
                if (heal < amount)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PowerHeal.FullPowerOther", target.GetName(0, true)), eChatType.CT_Spell);
            }
            return true;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PowerHeal.MainDescription", Spell.Value);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}
