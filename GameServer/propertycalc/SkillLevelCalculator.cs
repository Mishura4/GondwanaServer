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

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Skill Level calculator
    /// 
    /// BuffBonusCategory1 is used for buffs, uncapped
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.Skill_First, eProperty.Skill_Augmentation)]
    [PropertyCalculator(eProperty.Skill_Darkness, eProperty.Skill_Enchantments)]
    [PropertyCalculator(eProperty.Skill_Blades, eProperty.Skill_Last)]
    [PropertyCalculator(eProperty.Skill_Learning, eProperty.Skill_ElementalKnight3)]
    public class SkillLevelCalculator : PropertyCalculator
    {
        public SkillLevelCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            int value = living.BaseBuffBonusCategory[(int)property]; // one buff category just in case..
            value += CalcValueFromItems(living, property);
            if (living is GamePlayer player)
                return value + player.RealmLevel / 10;
            return value + (living.EffectiveLevel / 5);
        }

        public override int CalcValueFromItems(GameLiving living, eProperty property)
        {
            if (living is GamePlayer player)
            {
                int itemBonus = player.ItemBonus[(int)property];

                if (SkillBase.CheckPropertyType(property, ePropertyType.SkillMeleeWeapon))
                    itemBonus += player.ItemBonus[(int)eProperty.AllMeleeWeaponSkills];
                if (SkillBase.CheckPropertyType(property, ePropertyType.SkillMagical))
                    itemBonus += player.ItemBonus[(int)eProperty.AllMagicSkills];
                if (SkillBase.CheckPropertyType(property, ePropertyType.SkillDualWield))
                    itemBonus += player.ItemBonus[(int)eProperty.AllDualWieldingSkills];
                if (SkillBase.CheckPropertyType(property, ePropertyType.SkillArchery))
                    itemBonus += player.ItemBonus[(int)eProperty.AllArcherySkills];

                int itemCap = player.Level / 5 + 1;
                itemBonus += player.ItemBonus[(int)eProperty.AllSkills];

                if (itemBonus > itemCap)
                    itemBonus = itemCap;
                return itemBonus;
            }
            return 0;
        }
    }
}
