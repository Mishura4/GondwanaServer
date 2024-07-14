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

namespace DOL.GS.PlayerTitles
{
    public abstract class AdventurerTitle : TaskTitle
    {
        public int MaxSpeedBonus { get; init; }
        
        public int RangeBonus { get; init; }

        protected AdventurerTitle(int level)
        {
            MaxSpeedBonus = 5 * level;
            RangeBonus = 2 * level;
        }

        /// <inheritdoc />
        public override void OnTitleSelect(GamePlayer player)
        {
            // Speed buff is handled manually in MaxSpeedCalculator
            player.BaseBuffBonusCategory[eProperty.SpellRange] += RangeBonus;
            player.BaseBuffBonusCategory[eProperty.ArcheryRange] += RangeBonus;
            base.OnTitleSelect(player);
            player.UpdateMaxSpeed();
        }

        /// <inheritdoc />
        public override void OnTitleDeselect(GamePlayer player)
        {
            // Speed buff is handled manually in MaxSpeedCalculator
            player.BaseBuffBonusCategory[eProperty.SpellRange] -= RangeBonus;
            player.BaseBuffBonusCategory[eProperty.ArcheryRange] -= RangeBonus;
            base.OnTitleDeselect(player);
            player.UpdateMaxSpeed();
        }

        /// <inheritdoc />
        public override string GetStatsTranslation(string language)
        {
            return LanguageMgr.GetTranslation(language, "PlayerStatistic.Bonus.AdventurerTitle", MaxSpeedBonus, RangeBonus);
        }
    }

    [TaskTitleFlag(eTitleFlags.Adventurer1)]
    public class AdventurerTitleLevel1 : AdventurerTitle
    {
        public override string TitleKey => "Titles.Adventurer.Level1";

        /// <inheritdoc />
        public AdventurerTitleLevel1() : base(1)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Adventurer2)]
    public class AdventurerTitleLevel2 : AdventurerTitle
    {
        public override string TitleKey => "Titles.Adventurer.Level2";

        /// <inheritdoc />
        public AdventurerTitleLevel2() : base(2)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Adventurer3)]
    public class AdventurerTitleLevel3 : AdventurerTitle
    {
        public override string TitleKey => "Titles.Adventurer.Level3";

        /// <inheritdoc />
        public AdventurerTitleLevel3() : base(3)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Adventurer4)]
    public class AdventurerTitleLevel4 : AdventurerTitle
    {
        public override string TitleKey => "Titles.Adventurer.Level4";

        /// <inheritdoc />
        public AdventurerTitleLevel4() : base(4)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.Adventurer5)]
    public class AdventurerTitleLevel5 : AdventurerTitle
    {
        public override string TitleKey => "Titles.Adventurer.Level5";

        /// <inheritdoc />
        public AdventurerTitleLevel5() : base(5)
        {
        }
    }
}
