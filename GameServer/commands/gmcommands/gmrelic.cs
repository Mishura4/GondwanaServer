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
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gmrelic",
        ePrivLevel.GM,
        "Commands.GM.GMRelic.Description",
        "Commands.GM.GMRelic.Usage")]
    public class GMRelicCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length != 3 || (args[1] != "magic" && args[1] != "strength"))
            {
                DisplaySyntax(client);
                return;
            }

            DBRelic relic = new DBRelic();

            relic.Heading = client.Player.Orientation.InHeading;
            relic.OriginalRealm = int.Parse(args[2]);
            relic.Realm = 0;
            relic.Region = client.Player.Position.RegionID;
            relic.relicType = (args[1] == "strength") ? 0 : 1;
            relic.X = client.Player.Position.X;
            relic.Y = client.Player.Position.Y;
            relic.Z = client.Player.Position.Z;
            relic.RelicID = Util.Random(100);
            GameServer.Database.AddObject(relic);
            RelicMgr.Init();
        }
    }
}
