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
using AmteScripts.Managers;
using DOL.Language;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.InviteToGroup, "Handle Invite to Group Request.", eClientStatus.PlayerInGame)]
    public class InviteToGroupHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            new HandleGroupInviteAction(client.Player).Start(1);
        }

        /// <summary>
        /// Handles group invlite actions
        /// </summary>
        protected class HandleGroupInviteAction : RegionAction
        {
            /// <summary>
            /// constructs a new HandleGroupInviteAction
            /// </summary>
            /// <param name="actionSource">The action source</param>
            public HandleGroupInviteAction(GamePlayer actionSource) : base(actionSource)
            {
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            public override void OnTick()
            {
                var player = (GamePlayer)m_actionSource;

                if (player.TargetObject == null || player.TargetObject == player)
                {
                    ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "InviteToGroupHandler.InvalidTarget"));
                    return;
                }

                if (!(player.TargetObject is GamePlayer))
                {
                    ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "InviteToGroupHandler.InvalidTarget"));
                    return;
                }

                var target = (GamePlayer)player.TargetObject;

                if (player.Group != null && player.Group.Leader != player)
                {
                    ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "InviteToGroupHandler.NotLeader"));
                    return;
                }

                if (player.Group != null && player.Group.MemberCount >= ServerProperties.Properties.GROUP_MAX_MEMBER)
                {
                    ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "InviteToGroupHandler.GroupFull"));
                    return;
                }

                if (!GameServer.ServerRules.IsAllowedToGroup(player, target, false))
                    return;
                
                if (!PvpManager.CanGroup(player, player, false))
                {
                    return;
                }
                
                if (target.Group != null)
                {
                    ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "InviteToGroupHandler.TargetInGroup"));
                    return;
                }

                ChatUtil.SendSystemMessage(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "InviteToGroupHandler.Invited", player.GetPersonalizedName(target)));
                target.Out.SendGroupInviteCommand(player, LanguageMgr.GetTranslation(target.Client.Account.Language, "InviteToGroupHandler.InvitePrompt1", target.GetPersonalizedName(player), player.GetPronoun(1, false)) + "\n" + LanguageMgr.GetTranslation(target.Client.Account.Language, "InviteToGroupHandler.InvitePrompt2"));
                ChatUtil.SendSystemMessage(target, LanguageMgr.GetTranslation(target.Client.Account.Language, "InviteToGroupHandler.InviteNotification", target.GetPersonalizedName(player), player.GetPronoun(1, false)));
            }
        }
    }
}