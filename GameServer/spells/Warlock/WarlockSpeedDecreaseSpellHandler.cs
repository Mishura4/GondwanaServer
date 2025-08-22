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
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell handler for speed decreasing spells
    /// </summary>
    [SpellHandler("WarlockSpeedDecrease")]
    public class WarlockSpeedDecreaseSpellHandler : AbstractMorphSpellHandler
    {
        private const string WARLOCK_PRE_EFFECTIVENESS_KEY = "Warlock_PreEffectivenessDebuff";

        /// <inheritdoc />
        public override ushort GetModelFor(GameLiving living)
        {
            if (living.Realm == eRealm.Albion)
                return 581;
            else if (living.Realm == eRealm.Midgard)
                return 574;
            else if (living.Realm == eRealm.Hibernia)
                return 594;
            return 0;
        }

        /// <inheritdoc />
        public override bool HasPositiveOrSpeedEffect()
        {
            return true;
        }

        // constructor
        public WarlockSpeedDecreaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            Priority = 1;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.HasAbility(Abilities.CCImmunity) || target.HasAbility(Abilities.RootImmunity))
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DamageImmunity", m_caster.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                return false;
            }
            
            if (target.EffectList.GetOfType<AdrenalineSpellEffect>() != null)
            {
                (m_caster as GamePlayer)?.SendTranslatedMessage("Adrenaline.Target.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, m_caster.GetPersonalizedName(target));
                (target as GamePlayer)?.SendTranslatedMessage("Adrenaline.Self.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }
            
            if (target.EffectList.GetOfType<ChargeEffect>() != null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.Target.TooFast", m_caster.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                return false;
            }
            
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
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

            var timer = new UnbreakableSpeedDecreaseSpellHandler.RestoreSpeedTimer(effect);
            effect.Owner.TempProperties.setProperty(effect, timer);
            timer.Interval = 650;
            timer.Start(1 + (effect.Duration >> 1));

            effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);

            // Optional Effectiveness debuff (Vampiir-style)
            // Use Spell.LifeDrainReturn as percentage to reduce Effectiveness by, if > 0.
            if (Spell.LifeDrainReturn > 0 && effect.Owner is GamePlayer effPlayer)
            {
                bool vampAlreadyActive = SpellHandler.FindEffectOnTarget(effPlayer, "VampiirEffectivenessDeBuff") != null;

                object existingSave = effPlayer.TempProperties.getProperty<object>(WARLOCK_PRE_EFFECTIVENESS_KEY, null);

                if (!vampAlreadyActive && existingSave == null)
                {
                    double prev = effPlayer.Effectiveness;
                    effPlayer.TempProperties.setProperty(WARLOCK_PRE_EFFECTIVENESS_KEY, prev);

                    double pct = Spell.LifeDrainReturn;
                    double reduced = prev - (pct * prev) / 100.0;
                    effPlayer.Effectiveness = reduced > 0 ? reduced : 0;

                    effPlayer.Out.SendUpdateWeaponAndArmorStats();
                    effPlayer.Out.SendStatusUpdate();
                }
            }

            // Optional Silence for the same duration (AmnesiaChance == 1)
            if (Spell.AmnesiaChance == 1 && effect.Owner is GamePlayer)
            {
                effect.Owner.SilencedCount++;
                effect.Owner.StopCurrentSpellcast();
                effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            }

            base.OnEffectStart(effect);
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

            // --- Restore Effectiveness if we changed it here ---
            if (Spell.LifeDrainReturn > 0 && effect.Owner is GamePlayer effPlayer)
            {
                object prevObj = effPlayer.TempProperties.getProperty<object>(WARLOCK_PRE_EFFECTIVENESS_KEY, null);
                if (prevObj is double prev)
                {
                    effPlayer.Effectiveness = prev;
                    effPlayer.TempProperties.removeProperty(WARLOCK_PRE_EFFECTIVENESS_KEY);
                    effPlayer.Out.SendUpdateWeaponAndArmorStats();
                    effPlayer.Out.SendStatusUpdate();
                }
            }

            // --- Remove Silence if applied ---
            if (Spell.AmnesiaChance == 1 && effect.Owner is GamePlayer)
            {
                effect.Owner.SilencedCount--;
            }

            base.OnEffectExpires(effect, noMessages);
            return 60000;
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = base.CalculateEffectDuration(target, effectiveness);
            duration *= target.GetModified(eProperty.MythicalCrowdDuration) * 0.01;
            duration *= target.GetModified(eProperty.SpeedDecreaseDuration) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = (Spell.Duration * 4);
            return (int)duration;
        }

        protected static void SendUpdates(GameLiving owner)
        {
            if (owner.IsMezzed || owner.IsStunned)
                return;

            owner.UpdateMaxSpeed();
        }

        /// <inheritdoc cref="UnbreakableSpeedDecreaseSpellHandler.GetDelveDescription"/>
        public override string GetDelveDescription(GameClient delveClient)
        {
            string description;

            if (Spell.Value > 0)
            {
                if (Spell.Value >= 99)
                    description = LanguageMgr.GetTranslation(delveClient, "SpellDescription.SpeedDecrease.Rooted");
                else
                    description = LanguageMgr.GetTranslation(delveClient, "SpellDescription.SpeedDecrease.MainDescription", Spell.Value);
            }
            else
            {
                description = string.Empty;
            }

            if (Spell.LifeDrainReturn > 0)
            {
                string frogtext = LanguageMgr.GetTranslation(delveClient, "SpellDescription.WarlockSpeedDecrease.Frog");
                string vampMain = LanguageMgr.GetTranslation(delveClient, "SpellDescription.VampiirEffectivenessDeBuff.MainDescription", (int)Spell.LifeDrainReturn);
                string vampExtra = LanguageMgr.GetTranslation(delveClient, "SpellDescription.VampiirEffectivenessDeBuff.CombatCastable");

                description += "\n\n" + frogtext + "\n\n" + vampMain + "\n\n" + vampExtra;
            }

            if (Spell.AmnesiaChance == 1)
            {
                int durationSeconds = Spell.Duration / 1000;
                string silenceMain = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Silence.MainDescription", durationSeconds);

                description += "\n\n" + silenceMain;
            }

            if (Spell.SubSpellID != 0)
            {
                Spell subSpell = SkillBase.GetSpellByID((int)Spell.SubSpellID);
                if (subSpell != null)
                {
                    ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(m_caster, subSpell, null);
                    if (subSpellHandler != null)
                    {
                        string subspelldesc = subSpellHandler.GetDelveDescription(delveClient);
                        description += "\n\n" + subspelldesc;
                    }
                }
            }

            if (Spell.IsSecondary)
            {
                string secondaryMessage = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Warlock.SecondarySpell");
                description += "\n\n" + secondaryMessage;
            }

            if (Spell.IsPrimary)
            {
                string secondaryMessage = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Warlock.PrimarySpell");
                description += "\n\n" + secondaryMessage;
            }

            return description;
        }
    }
}
