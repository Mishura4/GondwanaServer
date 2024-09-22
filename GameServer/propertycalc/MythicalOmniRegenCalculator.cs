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

using DOL.GS.Keeps;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The health regen rate calculator
    /// 
    /// BuffBonusCategory1 is used for all buffs
    /// BuffBonusCategory2 is used for all debuffs (positive values expected here)
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.MythicalOmniRegen)]
    public class MythicalOmniRegenCalculator : PropertyCalculator
    {
        public MythicalOmniRegenCalculator() { }

        /// <summary>
        /// calculates the final property value
        /// </summary>
        /// <param name="living"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public override int CalcValue(GameLiving living, eProperty property)
        {
            double regen = 0;

            regen += living.ItemBonus[(int)property];

            int debuff = living.SpecBuffBonusCategory[(int)property];
            if (debuff < 0)
                debuff = -debuff;

            regen += living.BaseBuffBonusCategory[(int)property] - debuff;

            if (regen < 0)
                return 0;

            if (living.IsSitting && !living.InCombat)
                regen *= 2;

            return (int)regen;
        }
    }
}
