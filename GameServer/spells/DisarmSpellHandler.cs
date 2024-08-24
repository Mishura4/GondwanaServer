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
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Simulates disarming a target by stopping their attack
    /// </summary>
    [SpellHandler("Disarm")]
    public class DisarmSpellHandler : SpellHandler
    {
        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }
        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            if (effect.Owner.Realm == 0 || Caster.Realm == 0)
            {
                effect.Owner.LastAttackedByEnemyTickPvE = effect.Owner.CurrentRegion.Time;
                Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
            }
            else
            {
                effect.Owner.LastAttackedByEnemyTickPvP = effect.Owner.CurrentRegion.Time;
                Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
            }
            effect.Owner.DisarmedTime = effect.Owner.CurrentRegion.Time + CalculateEffectDuration(effect.Owner, Caster.Effectiveness);
            effect.Owner.StopAttack();

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

            effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            if (effect.Owner is GameNPC)
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)effect.Owner).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, 1);
            }
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
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
            return base.OnEffectExpires(effect, noMessages);
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

        /// <summary>
        /// Delve Info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();

                list.Add("Function: " + (Spell.SpellType == "" ? "(not implemented)" : Spell.SpellType));
                list.Add(" "); //empty line
                list.Add(Spell.Description);
                list.Add(" "); //empty line
                if (Spell.Duration != 0) list.Add(string.Format("Duration: {0}sec", (int)Spell.Duration / 1000));
                list.Add("Target: " + Spell.Target);
                if (Spell.Range != 0) list.Add("Range: " + Spell.Range);
                if (Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
                list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
                if (Spell.RecastDelay > 60000) list.Add("Recast time: " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
                else if (Spell.RecastDelay > 0) list.Add("Recast time: " + (Spell.RecastDelay / 1000).ToString() + " sec");
                return list;
            }
        }

        // constructor
        public DisarmSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
