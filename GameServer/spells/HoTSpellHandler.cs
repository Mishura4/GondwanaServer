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
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.AI.Brain;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("HealOverTime")]
    public class HoTSpellHandler : SpellHandler
    {
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            // TODO: correct formula
            double eff = 1.25;
            if (Caster is GamePlayer)
            {
                double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
                if (lineSpec < 1)
                    lineSpec = 1;
                eff = 0.75;
                if (Spell.Level > 0)
                {
                    eff += (lineSpec - 1.0) / Spell.Level * 0.5;
                    if (eff > 1.25)
                        eff = 1.25;
                }
            }
            base.ApplyEffectOnTarget(target, eff);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new GameSpellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
            GamePlayer ownerPlayer = effect.Owner as GamePlayer;

            // Handle translation for player targets
            if (ownerPlayer != null)
            {
                MessageToLiving(effect.Owner, GetFormattedMessage(ownerPlayer, Spell.Message1), eChatType.CT_Spell);
            }
            else
            {
                // Handle non-player targets, such as NPCs
                MessageToLiving(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message1), eChatType.CT_Spell);
            }

            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    if (ownerPlayer != null)
                    {
                        player.MessageFromArea(effect.Owner, GetFormattedMessage(player, Spell.Message2, player.GetPersonalizedName(effect.Owner)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        player.MessageFromArea(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message2), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);
            OnDirectEffect(effect.Owner, effect.Effectiveness);
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target.ObjectState != GameObject.eObjectState.Active) return;
            if (target.IsAlive == false) return;

            base.OnDirectEffect(target, effectiveness);
            double heal = Spell.Value * effectiveness;

            target.Health += (int)heal;

            #region PVP DAMAGE

            if (target.DamageRvRMemory > 0 &&
                (target is NecromancerPet &&
                ((target as NecromancerPet)?.Brain as IControlledBrain)?.GetPlayerOwner() != null
                || target is GamePlayer))
            {
                if (target.DamageRvRMemory > 0)
                    target.DamageRvRMemory -= (long)Math.Max(heal, 0);
            }

            #endregion PVP DAMAGE

            //"You feel calm and healthy."
            MessageToLiving(target, Spell.Message1, eChatType.CT_Spell);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            if (!noMessages)
            {
                GamePlayer ownerPlayer = effect.Owner as GamePlayer;
                if (ownerPlayer != null)
                {
                    MessageToLiving(effect.Owner, GetFormattedMessage(ownerPlayer, Spell.Message3), eChatType.CT_SpellExpires);
                }
                else
                {
                    MessageToLiving(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message3), eChatType.CT_SpellExpires);
                }

                foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(effect.Owner == player))
                    {
                        if (ownerPlayer != null)
                        {
                            player.MessageFromArea(effect.Owner, GetFormattedMessage(player, Spell.Message4, player.GetPersonalizedName(effect.Owner)), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            player.MessageFromArea(effect.Owner, LanguageMgr.GetTranslation("ServerLanguageKey", Spell.Message4), eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                        }
                    }
                }
            }
            return 0;
        }

        private string GetFormattedMessage(GamePlayer player, string messageKey, params object[] args)
        {
            if (messageKey.StartsWith("Languages.DBSpells."))
            {
                string translationKey = messageKey;
                string translation;

                if (LanguageMgr.TryGetTranslation(out translation, player.Client.Account.Language, translationKey, args))
                {
                    return translation;
                }
                else
                {
                    return "(Translation not found)";
                }
            }
            return string.Format(messageKey, args);
        }

        public HoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string ShortDescription
            => $"The target regenerates {Spell.Value} health every {Spell.Frequency / 1000.0} sec.";
    }
}
