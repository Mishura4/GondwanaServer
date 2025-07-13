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
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("VampiirBolt")]
    public class VampiirBoltSpellHandler : SpellHandler
    {
        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (Caster.InCombat == true)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.VampiirBolt.CannotCastInCombat"), eChatType.CT_SpellResisted);
                return false;
            }
            return base.CheckBeginCast(selectedTarget, quiet);
        }
        protected override bool ExecuteSpell(GameLiving target, bool force)
        {
            foreach (GameLiving targ in SelectTargets(target, force))
            {
                DealDamage(targ);
            }

            return true;
        }

        private void DealDamage(GameLiving target)
        {
            int ticksToTarget = (int)(m_caster.GetDistanceTo(target) * 100 / 85); // 85 units per 1/10s
            int delay = 1 + ticksToTarget / 100;
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, (ushort)(delay), false, 1);
            }
            BoltOnTargetAction bolt = new BoltOnTargetAction(Caster, target, this);
            bolt.Start(1 + ticksToTarget);
        }

        public override void FinishSpellCast(GameLiving target, bool force = false)
        {
            if (target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.NoEffectOnKeepComponent"), eChatType.CT_SpellResisted);
                return;
            }
            base.FinishSpellCast(target, force);
        }

        protected class BoltOnTargetAction : RegionAction
        {
            protected readonly GameLiving m_boltTarget;
            protected readonly VampiirBoltSpellHandler m_handler;

            public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, VampiirBoltSpellHandler spellHandler)
                : base(actionSource)
            {
                if (boltTarget == null)
                    throw new ArgumentNullException("boltTarget");
                if (spellHandler == null)
                    throw new ArgumentNullException("spellHandler");
                m_boltTarget = boltTarget;
                m_handler = spellHandler;
            }

            public override void OnTick()
            {
                GameLiving target = m_boltTarget;
                GameLiving caster = (GameLiving)m_actionSource;
                if (target == null || target.CurrentRegionID != caster.CurrentRegionID || target.ObjectState != GameObject.eObjectState.Active || !target.IsAlive)
                    return;

                int power = 0;
                if (target.Mana > 0)
                {
                    if (target is GameNPC)
                        power = (int)Math.Round(((double)(target.Level) * (double)(m_handler.Spell.Value) * 2) / 100);
                    else
                        power = (int)Math.Round((double)(target.MaxMana) * (((double)m_handler.Spell.Value) / 250));

                    if (target.Mana < power)
                        power = target.Mana;

                    caster.Mana += power;

                    target.Mana -= power;
                }
                if (power > 0 && target is GamePlayer targetPlayer)
                {
                    targetPlayer.Out.SendMessage(LanguageMgr.GetTranslation(targetPlayer.Client, "SpellHandler.VampiirBolt.PowerTaken", targetPlayer.GetPersonalizedName(caster), power), eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow);
                }
                if (caster is GamePlayer casterPlayer)
                {
                    if (power > 0)
                    {
                        casterPlayer.Out.SendMessage(LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.VampiirBolt.PowerReceived", power, casterPlayer.GetPersonalizedName(target)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        casterPlayer.Out.SendMessage(LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.VampiirBolt.NoPowerReceived", casterPlayer.GetPersonalizedName(target)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                }
                //Place the caster in combat
                if (target is GamePlayer)
                    caster.LastAttackTickPvP = caster.CurrentRegion.Time;
                else
                    caster.LastAttackTickPvE = caster.CurrentRegion.Time;

                //create the attack data for the bolt
                AttackData ad = new AttackData();
                ad.Attacker = caster;
                ad.Target = target;
                ad.DamageType = eDamageType.Heat;
                ad.AttackType = AttackData.eAttackType.Spell;
                ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
                ad.SpellHandler = m_handler;
                target.OnAttackedByEnemy(ad);

                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
            }
        }

        public VampiirBoltSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}