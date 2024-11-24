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
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Events;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Made by Dargon/Mannik
    /// 
    /// Thanks to PlanarChaosRvrTwo, and Batlas for their assistance for making it not interupt
    /// Special thanks to ontheDOL for actually solving my issues with interupt :)
    /// 
    /// Spell handler for slow spells. These spells have no immunity, and is not lost on hit.
    /// </summary>
    [SpellHandler("Slow")]
    public class SlowSpellHandler : SpellHandler
    {
        /// <summary>
        /// Apply the effect.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.HasAbility(Abilities.CCImmunity) || target.HasAbility(Abilities.RootImmunity))
            {
                if (m_caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellHandler.DamageImmunity", player.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                else
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageImmunity", target.Name), eChatType.CT_SpellResisted);
                return true;
            }
            if (target.EffectList.GetOfType<ChargeEffect>() != null)
            {
                if (m_caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellHandler.Target.TooFast", player.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                else
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.Target.TooFast", target.Name), eChatType.CT_SpellResisted);
                return true;
            }
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        /// <inheritdoc />
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = Spell.Duration;
            if (!(GameServer.ServerRules.IsPveOnlyBonus(eProperty.NegativeReduction) && GameServer.ServerRules.IsPvPAction(Caster, target)))
                duration *= (1.0 - target.GetModified(eProperty.NegativeReduction) * 0.01);
            duration *= (target.GetModified(eProperty.MythicalCrowdDuration) * 0.01);
            duration *= (target.GetModified(eProperty.SpeedDecreaseDuration) * 0.01);

            if (duration < 1)
                duration = 1;
            return (int)duration;
        }

        /// <summary>
        /// Sends updates on effect start/stop
        /// </summary>
        /// <param name="owner"></param>
        protected static void SendUpdates(GameLiving owner)
        {
            if (owner.IsMezzed || owner.IsStunned)
                return;

            GamePlayer player = owner as GamePlayer;
            if (player != null)
                player.Out.SendUpdateMaxSpeed();

            GameNPC npc = owner as GameNPC;
        }

        /// <summary>
        /// When an applied effect starts
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
            {
                if (m_caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellHandler.SlowSpellHandler.TooFastForEffect", player.GetPersonalizedName(effect.Owner)), eChatType.CT_SpellResisted);
                else
                    MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.SlowSpellHandler.TooFastForEffect", effect.Owner.Name), eChatType.CT_SpellResisted);
                return;
            }

            effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect, 1.0 - Spell.Value * 0.01);
            SendUpdates(effect.Owner);

            string casterLanguage = (m_caster as GamePlayer)?.Client?.Account?.Language ?? "EN";
            GamePlayer ownerPlayer = effect.Owner as GamePlayer;

            if (ownerPlayer != null)
            {
                string message1 = string.IsNullOrEmpty(Spell.Message1) ? string.Empty : Spell.GetFormattedMessage1(ownerPlayer);
                MessageToLiving(effect.Owner, message1, eChatType.CT_Spell);
            }
            else
            {
                string message1 = string.IsNullOrEmpty(Spell.Message1) ? string.Empty : LanguageMgr.GetTranslation(casterLanguage, Spell.Message1, effect.Owner.GetName(0, false));
                MessageToLiving(effect.Owner, message1, eChatType.CT_Spell);
            }

            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    string personalizedTargetName = player.GetPersonalizedName(effect.Owner);

                    string message2 = string.IsNullOrEmpty(Spell.Message2) ? string.Empty : Spell.GetFormattedMessage2(player, personalizedTargetName);
                    player.MessageFromArea(effect.Owner, message2, eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            GameTimer timer = (GameTimer)effect.Owner.TempProperties.getProperty<object>(effect, null);
            effect.Owner.TempProperties.removeProperty(effect);
            if (timer != null) timer.Stop();
            effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);
            SendUpdates(effect.Owner);

            if (!noMessages)
            {
                string casterLanguage = (m_caster as GamePlayer)?.Client?.Account?.Language ?? "EN";
                GamePlayer ownerPlayer = effect.Owner as GamePlayer;

                if (ownerPlayer != null)
                {
                    string message3 = string.IsNullOrEmpty(Spell.Message3) ? string.Empty : Spell.GetFormattedMessage3(ownerPlayer);
                    MessageToLiving(effect.Owner, message3, eChatType.CT_SpellExpires);
                }
                else
                {
                    string message3 = string.IsNullOrEmpty(Spell.Message3) ? string.Empty : LanguageMgr.GetTranslation(casterLanguage, Spell.Message3, effect.Owner.GetName(0, false));
                    MessageToLiving(effect.Owner, message3, eChatType.CT_SpellExpires);
                }

                foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(effect.Owner == player))
                    {
                        string personalizedTargetName = player.GetPersonalizedName(effect.Owner);

                        string message4 = string.IsNullOrEmpty(Spell.Message4) ? string.Empty : Spell.GetFormattedMessage4(player, personalizedTargetName);
                        player.MessageFromArea(effect.Owner, message4, eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                    }
                }
            }

            return 0;
        }

        // constructor
        public SlowSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }

        public override string ShortDescription
        {
            get
            {
                string description;

                if (Spell.Value >= 99)
                {
                    description = LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellDescription.SpeedDecrease.Rooted");
                }
                else
                {
                    description = LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellDescription.Slow.MainDescription", Spell.Value);
                }

                if (Spell.IsSecondary)
                {
                    string secondaryMessage = LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellDescription.Warlock.SecondarySpell");
                    description += "\n\n" + secondaryMessage;
                }

                return description;
            }
        }
    }
}