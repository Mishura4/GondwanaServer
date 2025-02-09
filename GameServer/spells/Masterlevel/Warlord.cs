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
using System.Collections.Generic;
using System.Reflection;
using log4net;
using System;
using System.Collections;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS;
using DOL.Events;
using System.Collections.Specialized;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Warlord
    #region Warlord-1
    //Gamesiegeweapon - getactiondelay
    #endregion

    //shared timer 1 for 2 - shared timer 4 for 8
    #region Warlord-2/8
    [SpellHandlerAttribute("PBAEHeal")]
    public class PBAEHealHandler : MasterlevelHandling
    {
        public override void FinishSpellCast(GameLiving target)
        {
            switch (Spell.DamageType)
            {
                case (eDamageType)((byte)1):
                    {
                        int value = (int)Spell.Value;
                        int life;
                        life = (m_caster.Health * value) / 100;
                        m_caster.Health -= life;
                    }
                    break;
            }
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target is not GamePlayer { ObjectState: GameObject.eObjectState.Active, IsAlive: false } player)
            {
                return false;
            }

            switch (Spell.DamageType)
            {
                //Warlord ML 2
                case (eDamageType)((byte)0):
                    {
                        int mana;
                        int health;
                        int end;
                        int value = (int)Spell.Value;
                        mana = (target.MaxMana * value) / 100;
                        end = (target.MaxEndurance * value) / 100;
                        health = (target.MaxHealth * value) / 100;

                        if (target.Health + health > target.MaxHealth)
                            target.Health = target.MaxHealth;
                        else
                            target.Health += health;

                        if (target.Mana + mana > target.MaxMana)
                            target.Mana = target.MaxMana;
                        else
                            target.Mana += mana;

                        if (target.Endurance + end > target.MaxEndurance)
                            target.Endurance = target.MaxEndurance;
                        else
                            target.Endurance += end;

                        SendEffectAnimation(target, 0, false, 1);
                    }
                    return true;
                //warlord ML8
                case (eDamageType)((byte)1):
                    {
                        int healvalue = (int)m_spell.Value;
                        int heal;
                        if (target.IsAlive && !GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
                        {
                            heal = target.ChangeHealth(target, GameLiving.eHealthChangeType.Spell, healvalue);
                            if (heal != 0) player.Out.SendMessage(player.GetPersonalizedName(m_caster) + " heal you for " + heal + " hit point!", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow);
                        }
                        heal = m_caster.ChangeHealth(Caster, GameLiving.eHealthChangeType.Spell, (int)(-m_caster.Health * 90 / 100));
                        if (heal != 0) MessageToCaster("You lose " + heal + " hit point" + (heal == 1 ? "." : "s."), eChatType.CT_Spell);

                        SendEffectAnimation(target, 0, false, 1);
                    }
                    return true;
            }
            return false;
        }

        // constructor
        public PBAEHealHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc;
            if (Spell.DamageType == 0) // ML2
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.PBAEHeal.ML2.MainDescription", Spell.Value);
            }
            else // ML8
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.PBAEHeal.ML8.MainDescription", Spell.Value, Spell.Damage);
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
    #endregion

    //shared timer 2
    #region Warlord-3
    [SpellHandlerAttribute("CoweringBellow")]
    public class CoweringBellowSpellHandler : FearSpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public override IList<GameLiving> SelectTargets(GameObject castTarget, bool force = false)
        {
            var list = new List<GameLiving>();
            GameLiving target = Caster;
            foreach (GameNPC npc in target.GetNPCsInRadius((ushort)Spell.Radius))
            {
                if (npc is GameNPC && npc.Brain is ControlledNpcBrain)//!(npc is NecromancerPet))
                    list.Add(npc);
            }
            return list;
        }

        public CoweringBellowSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.CoweringBellow.MainDescription", Spell.Radius);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
    #endregion

    //ML4~     //shared timer 3

    //shared timer 3
    #region Warlord-5
    [SpellHandlerAttribute("Critical")]
    public class CriticalDamageBuff : MasterlevelDualBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.CriticalSpellHitChance; } }
        public override eProperty Property2 { get { return eProperty.CriticalMeleeHitChance; } }

        public override eBuffBonusCategory BonusCategory1 { get => eBuffBonusCategory.UncappedBuff; }
        public override eBuffBonusCategory BonusCategory2 { get => eBuffBonusCategory.UncappedBuff; }

        public CriticalDamageBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;
            string spellTarget = LanguageMgr.GetTargetOfType(language, m_spell.Target.ToString());

            string mainDesc;
            if (Spell.Radius > 0)
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.Critical.MainDescription1", spellTarget, Spell.Value);
            }
            else
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.Critical.MainDescription2", Spell.Value);
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
    #endregion

    //ML6~     //shared timer 4

    //shared timer 3
    #region Warlord-7
    [SpellHandlerAttribute("CleansingAura")]
    public class CleansingAurauraSpellHandler : SpellHandler
    {
        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return true;
        }

        public CleansingAurauraSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc;
            if (Spell.Radius > 0)
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.CleansingAura.MainDescription1", Spell.Radius, Spell.Value);
            }
            else
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.CleansingAura.MainDescription2", Spell.Value);
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
    #endregion

    //shared timer 5
    #region Warlord-9
    [SpellHandlerAttribute("EffectivenessBuff")]
    public class EffectivenessBuff : MasterlevelHandling
    {
        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override bool HasPositiveEffect
        {
            get { return true; }
        }

        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Effectiveness += Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
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
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Effectiveness -= Spell.Value * 0.01;
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();
            }
            return 0;
        }

        public EffectivenessBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc;
            if (Spell.Radius > 0)
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.EffectivenessBuff.MainDescription1", Spell.Radius, Spell.Value);
            }
            else
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.EffectivenessBuff.MainDescription2", Spell.Value);
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
    #endregion

    //shared timer 5
    #region Warlord-10
    [SpellHandlerAttribute("MLABSBuff")]
    public class MLABSBuff : MasterlevelBuffHandling
    {
        public override eProperty Property1 { get { return eProperty.ArmorAbsorption; } }

        public MLABSBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc;
            if (Spell.Radius > 0)
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.MLABSBuff.MainDescription1", Spell.Radius, Spell.Value);
            }
            else
            {
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Warlord.MLABSBuff.MainDescription2", Spell.Value);
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
    #endregion
}
