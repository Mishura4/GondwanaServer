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
using DOL.Events;
using DOL.Language;
using static DOL.GS.PlayerTitles.TaskTitle;

namespace DOL.GS.PlayerTitles
{
    public abstract class WrathTitle : TaskTitle
    {
        public int TensionRateBonus { get; init; }
        
        public int TensionConservationBonus { get; init; }

        protected WrathTitle(int level)
        {
            TensionRateBonus = 3 * level;
            TensionConservationBonus = 5 * level;
        }

        /// <inheritdoc />
        public override void OnTitleSelect(GamePlayer player)
        {
            player.BaseBuffBonusCategory[eProperty.MythicalTension] += TensionRateBonus;
            player.BaseBuffBonusCategory[eProperty.TensionConservationBonus] += TensionConservationBonus;
            base.OnTitleSelect(player);
        }

        /// <inheritdoc />
        public override void OnTitleDeselect(GamePlayer player)
        {
            player.BaseBuffBonusCategory[eProperty.MythicalTension] -= TensionRateBonus;
            player.BaseBuffBonusCategory[eProperty.TensionConservationBonus] -= TensionConservationBonus;
            base.OnTitleDeselect(player);
        }

        /// <inheritdoc />
        public override string GetStatsTranslation(string language)
        {
            return LanguageMgr.GetTranslation(language, "PlayerStatistic.Bonus.WrathTitle", TensionRateBonus, TensionConservationBonus);
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath1)]
    public class WrathTitleLevel1 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level1";

        /// <inheritdoc />
        public WrathTitleLevel1() : base(1)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath2)]
    public class WrathTitleLevel2 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level2";

        /// <inheritdoc />
        public WrathTitleLevel2() : base(2)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath3)]
    public class WrathTitleLevel3 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level3";

        /// <inheritdoc />
        public WrathTitleLevel3() : base(3)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath4)]
    public class WrathTitleLevel4 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level4";

        /// <inheritdoc />
        public WrathTitleLevel4() : base(4)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath5)]
    public class WrathTitleLevel5 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level5";

        /// <inheritdoc />
        public WrathTitleLevel5() : base(5)
        {
        }
    }
}