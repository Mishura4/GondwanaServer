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

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CharacterSelectRequest, "Handles setting SessionID and the active character", eClientStatus.LoggedIn)]
    public class CharacterSelectRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            // 'GamePlayer' instantiation was moved to 'WorldInitRequestHandler' for game versions superior to 1124, because it didn't prevent multiple instantiation correctly.
            // Prior versions are still handled here because they're untested.
            if (client.Version >= GameClient.eClientVersion.Version1124)
            {
                client.Out.SendLoginGranted();
                client.Out.SendSessionID();
                return;
            }

            packet.Skip(4); // Skip the first 4 bytes
            packet.Skip(1);

            string charName = packet.ReadString(28);

            // TODO Character handling
            if (charName.Equals("noname"))
            {
                client.Out.SendLoginGranted();
                client.Out.SendSessionID();
            }
            else
            {
                // SH: Also load the player if client player is NOT null but their charnames differ!!!
                // only load player when on charscreen and player is not loaded yet
                // packet is sent on every region change (and twice after "play" was pressed)
                if (((client.Player == null && client.Account.Characters != null) || (client.Player != null && client.Player.Name.ToLower() != charName.ToLower())) && client.ClientState == GameClient.eClientState.CharScreen)
                {
                    bool charFound = false;
                    for (int i = 0; i < client.Account.Characters.Length; i++)
                    {
                        if (client.Account.Characters[i] != null
                            && client.Account.Characters[i].Name == charName)
                        {
                            charFound = true;

                            // Notify Character Selection Event, last hope to fix any bad data before Loading.
                            GameEventMgr.Notify(DatabaseEvent.CharacterSelected, new CharacterEventArgs(client.Account.Characters[i], client));
                            client.LoadPlayer(i);
                            break;
                        }
                    }

                    if (charFound == false)
                        client.ActiveCharIndex = -1;
                    else
                    {
                        // Log character play
                        AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterLogin, string.Empty, charName);
                    }
                }

                client.Out.SendLoginGranted();
                client.Out.SendSessionID();
            }
        }
    }
}