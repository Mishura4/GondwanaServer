﻿/*
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
    /// Calculator for Mythical Tension
    /// </summary>
    [PropertyCalculator(eProperty.MythicalTension)]
    public class MythicalTensionCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int value = living.BaseBuffBonusCategory[eProperty.MythicalTension] + living.BuffBonusCategory4[eProperty.MythicalTension];
            if (living is GamePlayer)
            {
                value += Math.Min(75, living.ItemBonus[(int)property]); // cap 75% from items
            }
            value -= living.DebuffCategory[eProperty.MythicalTension];
            return Math.Max(-100, value);
        }
    }
}