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
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&addbind",
        ePrivLevel.GM,
        "Commands.GM.AddBind.Description",
        "Commands.GM.AddBind.Usage")]
    public class AddBindCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            ushort bindRadius = 750;
            if (args.Length >= 2)
            {
                try
                {
                    bindRadius = UInt16.Parse(args[1]);
                }
                catch (Exception e)
                {
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.Error", e.Message));
                    return;
                }
            }
            BindPoint bp = new BindPoint();
            bp.X = client.Player.Position.X;
            bp.Y = client.Player.Position.Y;
            bp.Z = client.Player.Position.Z;
            bp.Region = client.Player.Position.RegionID;
            bp.Radius = bindRadius;
            GameServer.Database.AddObject(bp);
            client.Player.CurrentRegion.AddArea(new Area.BindArea("bind point", bp));
            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.AddBind.BindPointAdded", bp.X, bp.Y, bp.Z, bp.Radius, bp.Region));
        }
    }
}