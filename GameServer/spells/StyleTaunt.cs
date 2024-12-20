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
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("StyleTaunt")]
    public class StyleTaunt : SpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target)
            => 0;

        public override bool IsOverwritable(GameSpellEffect compare)
            => false;

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target is not GameNPC npc)
            {
                return false;
            }
            AttackData ad = Caster.TempProperties.getProperty<object>(GameLiving.LAST_ATTACK_DATA, null) as AttackData;
            if (ad != null)
            {
                IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                {
                    int aggro = Convert.ToInt32(ad.Damage * Spell.Value);
                    aggroBrain.AddToAggroList(Caster, aggro);

                    //log.DebugFormat("Damage: {0}, Taunt Value: {1}, (de)Taunt Amount {2}", ad.Damage, Spell.Value, aggro.ToString());
                }
            }
            return false;
        }

        public StyleTaunt(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            if (Spell.Value > 0)
                return LanguageMgr.GetTranslation(delveClient, "SpellDescription.StyleTaunt.Increase", Spell.Value);
            else
                return LanguageMgr.GetTranslation(delveClient, "SpellDescription.StyleTaunt.Decrease", Math.Abs(Spell.Value));
        }
    }
}