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
            None = 0,
            Thief1 = 1 << 1,
            Thief2 = 1 << 2,
            Thief3 = 1 << 3,
            Thief4 = 1 << 4,
            Thief5 = 1 << 5,
            Trader1 = 1 << 6,
            Trader2 = 1 << 7,
            Trader3 = 1 << 8,
            Trader4 = 1 << 9,
            Trader5 = 1 << 10,
            DemonSlayer1 = 1 << 11,
            DemonSlayer2 = 1 << 12,
            DemonSlayer3 = 1 << 13,
            DemonSlayer4 = 1 << 14,
            DemonSlayer5 = 1 << 15,
            BountyHunter1 = 1 << 16,
            BountyHunter2 = 1 << 17,
            BountyHunter3 = 1 << 18,
            BountyHunter4 = 1 << 19,
            BountyHunter5 = 1 << 20,
            Wrath1 = 1 << 21,
            Wrath2 = 1 << 22,
            Wrath3 = 1 << 23,
            Wrath4 = 1 << 24,
            Wrath5 = 1 << 25,
            Adventurer1 = 1 << 26,
            Adventurer2 = 1 << 27,
            Adventurer3 = 1 << 28,
            Adventurer4 = 1 << 29,
            Adventurer5 = 1 << 30
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
