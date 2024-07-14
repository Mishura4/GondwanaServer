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
using DOL.Language;
using DOL.Events;

namespace DOL.GS.PlayerTitles
{
    public abstract class ThiefTitle : TaskTitle
    {
        public int BonusRobberyChance { get; init; }

        public int RobberyDelayReduction { get; init; }

        public ThiefTitle(int level)
        {
            BonusRobberyChance = level * 5;
            RobberyDelayReduction = level * 5;
        }

        /// <inheritdoc />
        public override void OnTitleSelect(GamePlayer player)
        {
            player.BaseBuffBonusCategory[eProperty.RobberyChanceBonus] += BonusRobberyChance;
            player.BaseBuffBonusCategory[eProperty.RobberyDelayReduction] += RobberyDelayReduction;
            base.OnTitleSelect(player);
        }

        /// <inheritdoc />
        public override void OnTitleDeselect(GamePlayer player)
        {
            player.BaseBuffBonusCategory[eProperty.RobberyChanceBonus] -= BonusRobberyChance;
            player.BaseBuffBonusCategory[eProperty.RobberyDelayReduction] -= RobberyDelayReduction;
            base.OnTitleDeselect(player);
        }

        /// <inheritdoc />
        public override string GetStatsTranslation(string language)
        {
            return LanguageMgr.GetTranslation(language, "PlayerStatistic.Bonus.ThiefTitle", BonusRobberyChance, RobberyDelayReduction);
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Thief1)]
    public class ThiefTitleLevel1 : ThiefTitle
    {
        public override string TitleKey => "Titles.Thief.Level1";

        public ThiefTitleLevel1() : base(1)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Thief2)]
    public class ThiefTitleLevel2 : ThiefTitle
    {
        public override string TitleKey => "Titles.Thief.Level2";

        public ThiefTitleLevel2() : base(2)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Thief3)]
    public class ThiefTitleLevel3 : ThiefTitle
    {
        public override string TitleKey => "Titles.Thief.Level3";

        public ThiefTitleLevel3() : base(3)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Thief4)]
    public class ThiefTitleLevel4 : ThiefTitle
    {
        public override string TitleKey => "Titles.Thief.Level4";

        public ThiefTitleLevel4() : base(4)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Thief5)]
    public class ThiefTitleLevel5 : ThiefTitle
    {
        public override string TitleKey => "Titles.Thief.Level5";

        public ThiefTitleLevel5() : base(5)
        {
        }
    }
}
