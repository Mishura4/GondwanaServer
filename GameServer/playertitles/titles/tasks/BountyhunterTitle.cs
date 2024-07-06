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
    public abstract class BountyhunterTitle : TaskTitle
    {
    }

    [TaskTitleFlag(eTitleFlags.BountyHunter1)]
    public class BountyhunterTitleLevel1 : BountyhunterTitle
    {
        public override string TitleKey => "Titles.Bountyhunter.Level1";
    }
    
    [TaskTitleFlag(eTitleFlags.BountyHunter2)]
    public class BountyhunterTitleLevel2 : BountyhunterTitle
    {
        public override string TitleKey => "Titles.Bountyhunter.Level2";
    }
    
    [TaskTitleFlag(eTitleFlags.BountyHunter3)]
    public class BountyhunterTitleLevel3 : BountyhunterTitle
    {
        public override string TitleKey => "Titles.Bountyhunter.Level3";
    }
    
    [TaskTitleFlag(eTitleFlags.BountyHunter4)]
    public class BountyhunterTitleLevel4 : BountyhunterTitle
    {
        public override string TitleKey => "Titles.Bountyhunter.Level4";
    }
    
    [TaskTitleFlag(eTitleFlags.BountyHunter5)]
    public class BountyhunterTitleLevel5 : BountyhunterTitle
    {
        public override string TitleKey => "Titles.Bountyhunter.Level5";
    }
}