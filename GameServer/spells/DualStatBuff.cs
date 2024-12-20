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
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    public abstract class DualStatBuff : SingleStatBuff
    {
        public override eBuffBonusCategory BonusCategory1 { get { return eBuffBonusCategory.SpecBuff; } }
        public override eBuffBonusCategory BonusCategory2 { get { return eBuffBonusCategory.SpecBuff; } }

        protected DualStatBuff(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.DualStatBuff.MainDescription", LanguageMgr.GetProperty(delveClient, Property1), LanguageMgr.GetProperty(delveClient, Property2), Spell.Value);
        }
    }

    [SpellHandler("StrengthConstitutionBuff")]
    public class StrengthConBuff : DualStatBuff
    {
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.HasAbility(Abilities.VampiirStrength)
               || target.HasAbility(Abilities.VampiirConstitution))
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DualStatBuff.AlreadyHasEffect"), eChatType.CT_Spell);
                return false;
            }
            return base.ApplyEffectOnTarget(target, effectiveness);
        }
        public override eProperty Property1 { get { return eProperty.Strength; } }
        public override eProperty Property2 { get { return eProperty.Constitution; } }

        public StrengthConBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler("DexterityQuicknessBuff")]
    public class DexterityQuiBuff : DualStatBuff
    {
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.HasAbility(Abilities.VampiirDexterity)
               || target.HasAbility(Abilities.VampiirQuickness))
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.DualStatBuff.AlreadyHasEffect"), eChatType.CT_Spell);
                return true;
            }
            return base.ApplyEffectOnTarget(target, effectiveness);
        }
        public override eProperty Property1 { get { return eProperty.Dexterity; } }
        public override eProperty Property2 { get { return eProperty.Quickness; } }

        public DexterityQuiBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
