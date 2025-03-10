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
    [SpellHandler("SavageEnduranceHeal")]
    public class SavageEnduranceHeal : EnduranceHealSpellHandler
    {
        public override string CostType => "Health";
        public SavageEnduranceHeal(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        protected override void RemoveFromStat(int value)
        {
            m_caster.Health -= value;
        }

        public override int PowerCost(GameLiving target)
        {
            int cost = 0;
            if (m_spell.Power < 0)
                cost = (int)(m_caster.MaxHealth * Math.Abs(m_spell.Power) * 0.01);
            else
                cost = m_spell.Power;
            return cost;
        }

        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            int cost = PowerCost(Caster);
            if (Caster.Health < cost)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "SavageEnduranceHeal.CheckBeginCast.InsuffiscientHealth"), eChatType.CT_SpellResisted);
                return false;
            }
            return base.CheckBeginCast(Caster);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.SavageEnduranceHeal.MainDescription", Spell.Value);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}
