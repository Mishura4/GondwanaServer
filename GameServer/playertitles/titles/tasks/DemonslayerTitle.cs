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
    public abstract class DemonslayerTitle : EventPlayerTitle
    {
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

        public override bool IsSuitable(GamePlayer player)
        {
            // This will be managed by the TaskMaster script
            return false;
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

    public class DemonslayerTitleLevel1 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level1";
    }

    public class DemonslayerTitleLevel2 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level2";
    }

    public class DemonslayerTitleLevel3 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level3";
    }

    public class DemonslayerTitleLevel4 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level4";
    }

    public class DemonslayerTitleLevel5 : DemonslayerTitle
    {
        public override string TitleKey => "Titles.Demonslayer.Level5";
    }
}