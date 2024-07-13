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
    public abstract class DemonslayerTitle : TaskTitle
    {
        public DemonslayerTitle(int level)
        {
            MeleeBonus = level * 2;
            SpellBonus = level * 3;
            CritBonus = level * 2;
        }

        public int MeleeBonus { get; init; }

        public int SpellBonus { get; init; }

        public int CritBonus { get; init; }

        /// <inheritdoc />
        public override void OnTitleSelect(GamePlayer player)
        {
            player.BaseBuffBonusCategory[eProperty.MeleeDamage] += MeleeBonus;
            player.BaseBuffBonusCategory[eProperty.SpellDamage] += SpellBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalSpellHitChance] += CritBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalMeleeHitChance] += CritBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalHealHitChance] += CritBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalArcheryHitChance] += CritBonus;
            base.OnTitleSelect(player);
            player.Out.SendCharStatsUpdate();
        }

        /// <inheritdoc />
        public override void OnTitleDeselect(GamePlayer player)
        {
            player.BaseBuffBonusCategory[eProperty.MeleeDamage] -= MeleeBonus;
            player.BaseBuffBonusCategory[eProperty.SpellDamage] -= SpellBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalSpellHitChance] -= CritBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalMeleeHitChance] -= CritBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalHealHitChance] -= CritBonus;
            player.BaseBuffBonusCategory[eProperty.CriticalArcheryHitChance] -= CritBonus;
            base.OnTitleDeselect(player);
            player.Out.SendCharStatsUpdate();
        }

        /// <inheritdoc />
        public override string GetStatsTranslation(string language)
        {
            return LanguageMgr.GetTranslation(language, "PlayerStatistic.Bonus.DemonslayerTitle", MeleeBonus, SpellBonus, CritBonus);
        }
    }

    [TaskTitleFlag(eTitleFlags.DemonSlayer1)]
    public sealed class DemonslayerTitleLevel1 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level1";

        public DemonslayerTitleLevel1() : base(1)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.DemonSlayer2)]
    public sealed class DemonslayerTitleLevel2 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level2";

        public DemonslayerTitleLevel2() : base(2)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.DemonSlayer3)]
    public sealed class DemonslayerTitleLevel3 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level3";

        public DemonslayerTitleLevel3() : base(3)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.DemonSlayer4)]
    public sealed class DemonslayerTitleLevel4 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level4";

        public DemonslayerTitleLevel4() : base(4)
        {
        }
    }
    
    [TaskTitleFlag(eTitleFlags.DemonSlayer5)]
    public sealed class DemonslayerTitleLevel5 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level5";

        public DemonslayerTitleLevel5() : base(5)
        {
        }
    }
}