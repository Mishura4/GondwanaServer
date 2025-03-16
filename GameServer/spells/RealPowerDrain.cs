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
    [SpellHandlerAttribute("RealPowerDrain")]
    public class RealPowerDrain : PowerDrain
    {

        public override void DrainPower(GameLiving target, AttackData ad)
        {
            if (ad == null || !m_caster.IsAlive)
                return;

            GameLiving owner = Owner();
            if (owner == null)
                return;

            int powerGain = (ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn / 100;
            powerGain = owner.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, powerGain);

            //remove mana from target
            target.ChangeMana(m_caster, GameLiving.eManaChangeType.Spell, -Spell.AmnesiaChance * target.MaxMana / 100);

            if (powerGain > 0)
            {
                MessageToOwner(LanguageMgr.GetTranslation((owner as GamePlayer)?.Client, "SpellHandler.PowerDrain.PowerGain", powerGain), eChatType.CT_Spell);
                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.PowerDrain.PowerLoss", powerGain, target.GetPersonalizedName(m_caster)), eChatType.CT_Spell);
            }
            else
                MessageToOwner(LanguageMgr.GetTranslation((owner as GamePlayer)?.Client, "SpellHandler.PowerDrain.NoAbsorb"), eChatType.CT_SpellResisted);
        }

        public RealPowerDrain(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.RealPowerDrain.MainDescription", Spell.Damage, LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType), Spell.LifeDrainReturn, Spell.AmnesiaChance);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}
