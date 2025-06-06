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
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("PowerDrain")]
    public class PowerDrain : DirectDamageSpellHandler
    {
        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target is not { IsAlive: true, ObjectState: GameObject.eObjectState.Active }) return false;

            AttackData ad = CalculateDamageToTarget(target, effectiveness);

            // Attacked living may modify the attack data.
            ad.Target.ModifyAttack(ad);

            SendDamageMessages(ad);
            DamageTarget(ad, true);
            DrainPower(target, ad);
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            return true;
        }

        public virtual void DrainPower(GameLiving target, AttackData ad)
        {
            if (ad == null || !m_caster.IsAlive)
                return;

            GameLiving owner = Owner();
            if (owner == null)
                return;

            int powerGain = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
            powerGain = owner.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, powerGain);

            //remove mana from target if ts's not a player
            if (!(target is GamePlayer))
                target.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, -powerGain);

            if (owner is GamePlayer player && player.Client != null)
            {
                if (powerGain > 0)
                    MessageToOwner(LanguageMgr.GetTranslation(player.Client, "SpellHandler.PowerDrain.PowerGain", powerGain), eChatType.CT_Spell);
                else
                    MessageToOwner(LanguageMgr.GetTranslation(player.Client, "SpellHandler.PowerDrain.NoAbsorb"), eChatType.CT_SpellResisted);
            }
        }

        protected virtual GameLiving Owner()
        {
            return Caster;
        }

        protected virtual void MessageToOwner(String message, eChatType chatType)
        {
            base.MessageToCaster(message, chatType);
        }

        public PowerDrain(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.PowerDrain.MainDescription", Spell.Damage, LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType), Spell.LifeDrainReturn);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}
