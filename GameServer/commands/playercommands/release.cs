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

using DOL.Database;
using DOL.events.gameobjects;
using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&release", new string[] { "&rel" },
        ePrivLevel.Player,
        "Commands.Players.Release.Description",
        "Commands.Players.Release.Usage")]
    public class ReleaseCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player.CurrentRegion.IsRvR && !client.Player.CurrentRegion.IsDungeon)
            {
                client.Player.Release(GamePlayer.eReleaseType.RvR, false);
                return;
            }

            //Check if player should go to jail
            if (this.ReleaseOutlawsToJail(client))
                return;

            if (args.Length > 1 && args[1].ToLower() == "city")
            {
                client.Player.Release(GamePlayer.eReleaseType.City, false);
                return;
            }

            if (args.Length > 1 && args[1].ToLower() == "house")
            {
                client.Player.Release(GamePlayer.eReleaseType.House, false);
                return;
            }
            client.Player.Release(GamePlayer.eReleaseType.Normal, false);
        }

        private bool ReleaseOutlawsToJail(GameClient client)
        {
            IList<DBDeathLog> deaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KillerId").IsEqualTo(client.Player.InternalID)
                .And(DB.Column("IsWanted").IsEqualTo(1)
                .And(DB.Column("ExitFromJail").IsEqualTo("0"))));
            
            if (deaths != null && deaths.Count > 0)
            {
                client.Player.Release(GamePlayer.eReleaseType.Jail, true);
                return true;
            }
            return false;
        }
    }
}