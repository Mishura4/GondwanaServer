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
using System.Security.Policy;
using DOL.Events;
using DOL.Language;

namespace DOL.GS.PlayerTitles
{
    public class TaskTitleFlag : Attribute
    {
        public TaskTitle.eTitleFlags Flag { get; init; }

        public TaskTitleFlag(TaskTitle.eTitleFlags flag)
        {
            Flag = flag;
        }
    }
    
    public abstract class TaskTitle : EventPlayerTitle
    {
        [Flags]
        public enum eTitleFlags
        {
            None = 0x00000000,
            Thief1 = 0x00000001,
            Thief2 = 0x00000002,
            Thief3 = 0x00000004,
            Thief4 = 0x00000008,
            Thief5 = 0x00000010,
            Trader1 = 0x00000020,
            Trader2 = 0x00000040,
            Trader3 = 0x00000080,
            Trader4 = 0x00000100,
            Trader5 = 0x00000200,
            DemonSlayer1 = 0x00000400,
            DemonSlayer2 = 0x00000800,
            DemonSlayer3 = 0x00001080,
            DemonSlayer4 = 0x00002000,
            DemonSlayer5 = 0x00004000,
            BountyHunter1 = 0x00008000,
            BountyHunter2 = 0x00010000,
            BountyHunter3 = 0x00020000,
            BountyHunter4 = 0x00040000,
            BountyHunter5 = 0x00080000,
            Wrath1 = 0x00100000,
            Wrath2 = 0x00200000,
            Wrath3 = 0x00400000,
            Wrath4 = 0x00800000,
            Wrath5 = 0x01000000,
            Adventurer1 = 0x01000000,
            Adventurer2 = 0x02000000,
            Adventurer3 = 0x04000000,
            Adventurer4 = 0x08000000,
            Adventurer5 = 0x10000000
        }
        
        public abstract string TitleKey { get; }

        public override string GetDescription(GamePlayer player)
        {
            return LanguageMgr.TryTranslateOrDefault(player, TitleKey, TitleKey);
        }

        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return LanguageMgr.TryTranslateOrDefault(source, TitleKey, TitleKey);
        }

        public override DOLEvent Event => GamePlayerEvent.GiveItem;

        private static eTitleFlags GetFlag(Type type)
        {
            TaskTitleFlag att = (TaskTitleFlag)Attribute.GetCustomAttribute(type, typeof(TaskTitleFlag));

            return att?.Flag ?? eTitleFlags.None;
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return (player.TaskTitleFlags & GetFlag(GetType())) != 0;
        }

        /// <inheritdoc />
        public override void OnTitleGained(GamePlayer player)
        {
            player.TaskTitleFlags |= GetFlag(GetType());
            base.OnTitleGained(player);
        }

        /// <inheritdoc />
        public override void OnTitleLost(GamePlayer player)
        {
            player.TaskTitleFlags &= ~GetFlag(GetType());
            base.OnTitleLost(player);
        }

        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer p = sender as GamePlayer;
            if (p != null && p.Titles.Contains(this))
            {
                p.UpdateCurrentTitle();
                return;
            }
            base.EventCallback(e, sender, arguments);
        }
    }
}
