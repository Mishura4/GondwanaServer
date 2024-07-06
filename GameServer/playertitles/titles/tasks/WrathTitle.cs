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
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath1)]
    public class WrathTitleLevel1 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level1";
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath2)]
    public class WrathTitleLevel2 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level2";
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath3)]
    public class WrathTitleLevel3 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level3";
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath4)]
    public class WrathTitleLevel4 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level4";
    }
    
    [TaskTitleFlag(eTitleFlags.Wrath5)]
    public class WrathTitleLevel5 : WrathTitle
    {
        public override string TitleKey => "Titles.Wrath.Level5";
    }
}