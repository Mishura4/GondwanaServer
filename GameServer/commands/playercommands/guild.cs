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
using DOL.AI.Brain;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using DOL.Territories;
using DOL.GS.Finance;
using DOL.GS.Scripts;
using System.Numerics;

namespace DOL.GS.Commands
{
    /// <summary>
    /// command handler for /gc command
    /// </summary>
    [Cmd(
        "&gc",
        new string[] { "&guildcommand" },
        ePrivLevel.Player,
        "Commands.Players.Guild.Description",
        "Commands.Players.Guild.Usage")]
    public class GuildCommandHandler : AbstractCommandHandler, ICommandHandler
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public long GuildFormCost = Money.GetMoney(0, 0, 1, 0, 0); //Cost to form guild : live = 1g : (mith/plat/gold/silver/copper)
        /// <summary>
        /// Checks if a guildname has valid characters
        /// </summary>
        /// <param name="guildName"></param>
        /// <returns></returns>
        public static bool IsValidGuildName(string guildName)
        {
            if (!Regex.IsMatch(guildName, @"^[a-zA-Z àâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ]+$") || guildName.Length < 0)

            {
                return false;
            }
            return true;
        }
        private static bool IsNearRegistrar(GamePlayer player)
        {
            foreach (GameNPC registrar in player.GetNPCsInRadius(500))
            {
                if (registrar is GuildRegistrar)
                    return true;
            }
            return false;
        }
        private static bool GuildFormCheck(GamePlayer leader)
        {
            Group group = leader.Group;
            #region No group check - Ensure we still have a group
            if (group == null)
            {
                leader.Out.SendMessage(LanguageMgr.GetTranslation(leader.Client.Account.Language, "Commands.Players.Guild.FormNoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            #endregion
            #region Enough members to form Check - Ensure our group still has enough players in to form
            if (group.MemberCount < Properties.GUILD_NUM)
            {
                leader.Out.SendMessage(LanguageMgr.GetTranslation(leader.Client.Account.Language, "Commands.Players.Guild.FormNoMembers" + Properties.GUILD_NUM), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            #endregion

            return true;
        }

        protected void CreateGuild(GamePlayer player, byte response)
        {
            #region Player Declines
            if (response != 0x01)
            {
                //remove all guild consider to enable re try
                foreach (GamePlayer ply in player.Group.GetPlayersInTheGroup())
                {
                    ply.TempProperties.removeProperty("Guild_Consider");
                }
                player.Group.Leader.TempProperties.removeProperty("Guild_Name");
                player.Group.SendMessageToGroupMembers(
                    player,
                    LanguageMgr.GetTranslation(
                        player.Client.Account.Language,
                        "Commands.Players.Guild.GuildDeclined"
                    ),
                    eChatType.CT_Group,
                    eChatLoc.CL_ChatWindow
                );
                return;
            }
            #endregion
            #region Player Accepts
            player.Group.SendMessageToGroupMembers(
                player,
                LanguageMgr.GetTranslation(
                    player.Client.Account.Language,
                    "Commands.Players.Guild.GuildAccept"
                ),
                eChatType.CT_Group,
                eChatLoc.CL_ChatWindow
            );
            player.TempProperties.setProperty("Guild_Consider", true);
            var guildname = player.Group.Leader.TempProperties.getProperty<string>("Guild_Name");

            var memnum = player.Group.GetPlayersInTheGroup().Count(p => p.TempProperties.getProperty<bool>("Guild_Consider"));

            if (!GuildFormCheck(player) || memnum != player.Group.MemberCount) return;

            if (Properties.GUILD_NUM > 1)
            {
                Group group = player.Group;
                lock (group)
                {
                    Guild newGuild = GuildMgr.CreateGuild(player.Realm, guildname, player);
                    if (newGuild == null)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.UnableToCreateLead", guildname, player.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        foreach (GamePlayer ply in group.GetPlayersInTheGroup())
                        {
                            if (ply != group.Leader)
                            {
                                newGuild.AddPlayer(ply);
                            }
                            else
                            {
                                newGuild.AddPlayer(ply, newGuild.GetRankByID(0));
                            }
                            ply.TempProperties.removeProperty("Guild_Consider");
                        }
                        player.Group.Leader.TempProperties.removeProperty("Guild_Name");
                        player.Group.Leader.RemoveMoney(Currency.Copper.Mint(10000));
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.GuildCreated", guildname, player.Group.Leader.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                        // refresh the social window
                        newGuild.UpdateGuildWindow();
                    }
                }
            }
            else
            {
                Guild newGuild = GuildMgr.CreateGuild(player.Realm, guildname, player);

                if (newGuild == null)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.UnableToCreateLead", guildname, player.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    newGuild.AddPlayer(player, newGuild.GetRankByID(0));
                    player.TempProperties.removeProperty("Guild_Name");
                    player.RemoveMoney(Currency.Copper.Mint(10000));
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.GuildCreated", guildname, player.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    // refresh the social window
                    newGuild.UpdateMember(player);
                }
            }
            #endregion
        }

        /// <summary>
        /// method to handle /gc commands from a client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "gc", 500))
                return;

            try
            {
                if (args.Length == 1)
                {
                    DisplayHelp(client);
                    return;
                }

                if (client.Player.IsIncapacitated)
                {
                    return;
                }


                string message;

                // Use this to aid in debugging social window commands
                //string debugArgs = "";
                //foreach (string arg in args)
                //{
                //    debugArgs += arg + " ";
                //}
                //log.Debug(debugArgs);

                switch (args[1])
                {
                    #region Create
                    // --------------------------------------------------------------------------------
                    // CREATE
                    // --------------------------------------------------------------------------------
                    case "create":
                        {
                            if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
                                return;

                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMCreate"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            GameLiving guildLeader = client.Player.TargetObject as GameLiving;
                            string guildname = String.Join(" ", args, 2, args.Length - 2);
                            guildname = GameServer.Database.Escape(guildname);
                            if (!GuildMgr.DoesGuildExist(guildname))
                            {
                                if (guildLeader == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (!IsValidGuildName(guildname))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InvalidLetters"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                else
                                {
                                    Guild newGuild = GuildMgr.CreateGuild(client.Player.Realm, guildname, client.Player);
                                    if (newGuild == null)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.UnableToCreate", newGuild.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    else
                                    {
                                        newGuild.AddPlayer((GamePlayer)guildLeader);
                                        ((GamePlayer)guildLeader).GuildRank = ((GamePlayer)guildLeader).Guild.GetRankByID(0);
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildCreated", guildname, ((GamePlayer)guildLeader).Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    return;
                                }
                            }
                            else
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildExists"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Purge
                    // --------------------------------------------------------------------------------
                    // PURGE
                    // --------------------------------------------------------------------------------
                    case "purge":
                        {
                            if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
                                return;

                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMPurge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            string guildname = String.Join(" ", args, 2, args.Length - 2);
                            if (!GuildMgr.DoesGuildExist(guildname))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildNotExist"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (GuildMgr.DeleteGuild(guildname))
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Purged", guildname), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }
                        break;
                    #endregion
                    #region Rename
                    // --------------------------------------------------------------------------------
                    // RENAME
                    // --------------------------------------------------------------------------------
                    case "rename":
                        {
                            if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
                                return;

                            if (args.Length < 5)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRename"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            int i;
                            for (i = 2; i < args.Length; i++)
                            {
                                if (args[i] == "to")
                                    break;
                            }

                            string oldguildname = String.Join(" ", args, 2, i - 2);
                            string newguildname = String.Join(" ", args, i + 1, args.Length - i - 1);
                            if (!GuildMgr.DoesGuildExist(oldguildname))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildNotExist"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            Guild myguild = GuildMgr.GetGuildByName(oldguildname);
                            myguild.Name = newguildname;
                            GuildMgr.AddGuild(myguild);
                            foreach (GamePlayer ply in myguild.GetListOfOnlineMembers())
                            {
                                ply.GuildName = newguildname;
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region AddPlayer
                    // --------------------------------------------------------------------------------
                    // ADDPLAYER
                    // --------------------------------------------------------------------------------
                    case "addplayer":
                        {
                            if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
                                return;

                            if (args.Length < 5)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMAddPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            int i;
                            for (i = 2; i < args.Length; i++)
                            {
                                if (args[i] == "to")
                                    break;
                            }

                            string playername = String.Join(" ", args, 2, i - 2);
                            string guildname = String.Join(" ", args, i + 1, args.Length - i - 1);

                            GuildMgr.GetGuildByName(guildname).AddPlayer(WorldMgr.GetClientByPlayerName(playername, true, false).Player, true);
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region RemovePlayer
                    // --------------------------------------------------------------------------------
                    // REMOVEPLAYER
                    // --------------------------------------------------------------------------------
                    case "removeplayer":
                        {
                            if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
                                return;

                            if (args.Length < 5)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRemovePlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            int i;
                            for (i = 2; i < args.Length; i++)
                            {
                                if (args[i] == "from")
                                    break;
                            }

                            string playername = String.Join(" ", args, 2, i - 2);
                            string guildname = String.Join(" ", args, i + 1, args.Length - i - 1);

                            if (!GuildMgr.DoesGuildExist(guildname))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildNotExist"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            GuildMgr.GetGuildByName(guildname).RemovePlayer("gamemaster", WorldMgr.GetClientByPlayerName(playername, true, false).Player);
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region RealmPoints

                    case "realmpoints":
                        {
                            if (client.Account.PrivLevel <= (uint)ePrivLevel.Player)
                                break;

                            if (args.Length < 4)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRealmPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            Guild guild;
                            if (args.Length >= 5)
                            {
                                string guildname = String.Join(" ", args, 4, args.Length - 4);

                                if (!GuildMgr.DoesGuildExist(guildname))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildNotExist"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                guild = GuildMgr.GetGuildByName(guildname);
                            }
                            else
                            {
                                guild = client.Player.Guild;
                                if (guild == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRealmPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            long points;
                            try
                            {
                                points = long.Parse(args[3]);
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRealmPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            switch (args[2])
                            {
                                case "set":
                                    guild.RealmPoints = points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RealmPointsSet", guild.Name, points), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                case "add":
                                    guild.RealmPoints += points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RealmPointsSet", guild.Name, guild.RealmPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                case "remove":
                                case "rm":
                                    guild.RealmPoints -= points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RealmPointsSet", guild.Name, guild.RealmPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                default:
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRealmPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                            }
                            break;
                        }
                    #endregion
                    #region MeritPoints

                    case "meritpoints":
                        {
                            if (client.Account.PrivLevel <= (uint)ePrivLevel.Player)
                                break;

                            if (args.Length < 4)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMMeritPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            Guild guild;
                            if (args.Length >= 5)
                            {
                                string guildname = String.Join(" ", args, 4, args.Length - 4);

                                if (!GuildMgr.DoesGuildExist(guildname))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildNotExist"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                guild = GuildMgr.GetGuildByName(guildname);
                            }
                            else
                            {
                                guild = client.Player.Guild;
                                if (guild == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMMeritPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            long points;
                            try
                            {
                                points = long.Parse(args[3]);
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMMeritPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            switch (args[2])
                            {
                                case "set":
                                    guild.MeritPoints = points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.MeritPointsSet", guild.Name, points), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                case "add":
                                    guild.MeritPoints += points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.MeritPointsSet", guild.Name, guild.MeritPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                case "remove":
                                case "rm":
                                    guild.MeritPoints -= points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.MeritPointsSet", guild.Name, guild.MeritPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                default:
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMMeritPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                            }
                            break;
                        }
                    #endregion
                    #region BountyPoints

                    case "bountypoints":
                        {
                            if (client.Account.PrivLevel <= (uint)ePrivLevel.Player)
                                break;

                            if (args.Length < 4)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMBountyPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            Guild guild;
                            if (args.Length >= 5)
                            {
                                string guildname = String.Join(" ", args, 4, args.Length - 4);

                                if (!GuildMgr.DoesGuildExist(guildname))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildNotExist"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                guild = GuildMgr.GetGuildByName(guildname);
                            }
                            else
                            {
                                guild = client.Player.Guild;
                                if (guild == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMBountyPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            long points;
                            try
                            {
                                points = long.Parse(args[3]);
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMBountyPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            switch (args[2])
                            {
                                case "set":
                                    guild.BountyPoints = points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BountyPointsSet", guild.Name, points), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                case "add":
                                    guild.BountyPoints += points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BountyPointsSet", guild.Name, guild.BountyPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                case "remove":
                                case "rm":
                                    guild.BountyPoints -= points;
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BountyPointsSet", guild.Name, guild.BountyPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;

                                default:
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMBountyPoints"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                            }
                            break;
                        }
                    #endregion
                    #region Invite
                    /****************************************guild member command***********************************************/
                    // --------------------------------------------------------------------------------
                    // INVITE
                    // --------------------------------------------------------------------------------
                    case "invite":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Invite))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            GamePlayer obj = client.Player.TargetObject as GamePlayer;
                            if (args.Length > 2)
                            {
                                GameClient temp = WorldMgr.GetClientByPlayerName(args[2], true, true);
                                if (temp != null)
                                    obj = temp.Player;
                            }
                            if (obj == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteNoSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (obj == client.Player)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteNoSelf"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (obj.Guild != null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.AlreadyInGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!obj.IsAlive)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!GameServer.ServerRules.IsAllowedToGroup(client.Player, obj, true))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteNotThis"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!GameServer.ServerRules.IsAllowedToJoinGuild(obj, client.Player.Guild))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteNotThis"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            obj.Out.SendGuildInviteCommand(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteRecieved", obj.GetPersonalizedName(client.Player), client.Player.Guild.Name));
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InviteSent", client.Player.GetPersonalizedName(obj), client.Player.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Remove
                    // --------------------------------------------------------------------------------
                    // REMOVE
                    // --------------------------------------------------------------------------------
                    case "remove":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Remove))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildRemove"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            object obj = null;
                            string playername = args[2];
                            if (playername == "")
                                obj = client.Player.TargetObject as GamePlayer;
                            else
                            {
                                GameClient myclient = WorldMgr.GetClientByPlayerName(playername, true, false);
                                if (myclient == null)
                                {
                                    // Patch 1.84: look for offline players
                                    obj = DOLDB<DOLCharacters>.SelectObject(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(playername));
                                }
                                else
                                    obj = myclient.Player;
                            }
                            if (obj == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            string guildId = "";
                            ushort guildRank = 9;
                            string plyName = "";
                            GamePlayer ply = obj as GamePlayer;
                            DOLCharacters ch = obj as DOLCharacters;
                            if (obj is GamePlayer)
                            {
                                plyName = ply.Name;
                                guildId = ply.GuildID;
                                if (ply.GuildRank != null)
                                    guildRank = ply.GuildRank.RankLevel;
                            }
                            else
                            {
                                plyName = ch.Name;
                                guildId = ch.GuildID;
                                guildRank = (byte)ch.GuildRank;
                            }
                            if (guildId != client.Player.GuildID)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            foreach (GamePlayer plyon in client.Player.Guild.GetListOfOnlineMembers())
                            {
                                plyon.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.MemberRemoved", client.Player.Name, plyName), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            if (obj is GamePlayer)
                                client.Player.Guild.RemovePlayer(client.Player.Name, ply);
                            else
                            {
                                ch.GuildID = "";
                                ch.GuildRank = 9;
                                GameServer.Database.SaveObject(ch);
                            }

                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Remove account
                    // --------------------------------------------------------------------------------
                    // REMOVE ACCOUNT (Patch 1.84)
                    // --------------------------------------------------------------------------------
                    case "removeaccount":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Remove))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildRemAccount"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            string playername = String.Join(" ", args, 2, args.Length - 2);
                            // Patch 1.84: look for offline players
                            var chs = DOLDB<DOLCharacters>.SelectObjects(DB.Column(nameof(DOLCharacters.AccountName)).IsEqualTo(playername).And(DB.Column(nameof(DOLCharacters.GuildID)).IsEqualTo(client.Player.GuildID)));
                            if (chs.Count > 0)
                            {
                                GameClient myclient = WorldMgr.GetClientByAccountName(playername, false);
                                string plys = "";
                                bool isOnline = (myclient != null);
                                foreach (DOLCharacters ch in chs)
                                {
                                    plys += (plys != "" ? "," : "") + ch.Name;
                                    if (isOnline && ch.Name == myclient.Player.Name)
                                        client.Player.Guild.RemovePlayer(client.Player.Name, myclient.Player);
                                    else
                                    {
                                        ch.GuildID = "";
                                        ch.GuildRank = 9;
                                        GameServer.Database.SaveObject(ch);
                                    }
                                }

                                foreach (GamePlayer ply in client.Player.Guild.GetListOfOnlineMembers())
                                {
                                    ply.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.AccountRemoved", client.Player.Name, plys), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                }
                            }
                            else
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPlayersInAcc"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Info
                    // --------------------------------------------------------------------------------
                    // INFO
                    // --------------------------------------------------------------------------------
                    case "info":
                        {
                            bool typed = false;
                            if (args.Length != 3)
                                typed = true;

                            if (client.Player.Guild == null)
                            {
                                if (!(args.Length == 3 && args[2] == "1"))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                }
                                return;
                            }

                            if (typed)
                            {
                                /*
                                 * Guild Info for Clan Cotswold:
                                 * Realm Points: xxx Bouty Points: xxx Merit Points: xxx
                                 * Guild Level: xx
                                 * Dues: 0% Bank: 0 copper pieces
                                 * Current Merit Bonus: None
                                 * Banner available for purchase
                                 * Webpage: xxx
                                 * Contact Email:
                                 * Message: motd
                                 * Officer Message: xxx
                                 * Alliance Message: xxx
                                 * Claimed Keep: xxx
                                 */
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuild", client.Player.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoRP", client.Player.Guild.RealmPoints), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoBP", client.Player.Guild.BountyPoints), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoMP", client.Player.Guild.MeritPoints), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuildLevel", client.Player.Guild.GuildLevel), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuildBank", Money.GetString(long.Parse(client.Player.Guild.GetGuildBank().ToString()))), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuildDues", client.Player.Guild.GetGuildDuesPercent().ToString() + "%"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                                double bonusPercentage = GetBuffBonusPercentage(client.Player.Guild);
                                double GetBuffBonusPercentage(Guild guild)
                                {
                                    int guildLevel = (int)client.Player.Guild.GuildLevel;
                                    double bonusPercentage = 0.0;
                                    switch (guild.BonusType)
                                    {
                                        case Guild.eBonusType.Experience:
                                            bonusPercentage = ServerProperties.Properties.GUILD_BUFF_XP;
                                            break;
                                        case Guild.eBonusType.RealmPoints:
                                            bonusPercentage = ServerProperties.Properties.GUILD_BUFF_RP;
                                            break;
                                        case Guild.eBonusType.BountyPoints:
                                            bonusPercentage = ServerProperties.Properties.GUILD_BUFF_BP;
                                            break;
                                    }

                                    if (guildLevel >= 8 && guildLevel <= 15)
                                    {
                                        bonusPercentage *= 1.5;
                                    }
                                    else if (guildLevel > 15)
                                    {
                                        bonusPercentage *= 2.0;
                                    }
                                    bonusPercentage += 0;

                                    return bonusPercentage;
                                }

                                if (bonusPercentage > 0)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuildBuff", Guild.BonusTypeToName(client.Player.Guild.BonusType), bonusPercentage), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuildBuffNone"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                    if (client.Player.Guild.HasGuildBanner)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoBanner", client.Player.Guild.GuildBannerStatus(client.Player)), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else if (client.Player.Guild.GuildLevel >= 7)
                                {
                                    TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

                                    if (lostTime.TotalMinutes < Properties.GUILD_BANNER_LOST_TIME)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoBanner.Lost"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoBanner.PurchaseAvailable"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    }
                                }

                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoWebpage", client.Player.Guild.Webpage), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoCEmail", client.Player.Guild.Email), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                                string motd = client.Player.Guild.Motd;
                                if (!Util.IsEmpty(motd) && client.Player.GuildRank.GcHear)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoMotd", motd), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }

                                string omotd = client.Player.Guild.Omotd;
                                if (!Util.IsEmpty(omotd) && client.Player.GuildRank.OcHear)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoOMotd", omotd), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }

                                if (client.Player.Guild.alliance != null)
                                {
                                    string amotd = client.Player.Guild.alliance.Dballiance.Motd;
                                    if (!Util.IsEmpty(amotd) && client.Player.GuildRank.AcHear)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoaMotd", amotd), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    }
                                }
                                if (client.Player.Guild.ClaimedKeeps.Count > 0)
                                {
                                    foreach (AbstractGameKeep keep in client.Player.Guild.ClaimedKeeps)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Keep", keep.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    }
                                }
                            }
                            else
                            {
                                switch (args[2])
                                {
                                    case "1": // show guild info
                                        {
                                            if (client.Player.Guild == null)
                                                return;

                                            int housenum;
                                            if (client.Player.Guild.GuildOwnsHouse)
                                            {
                                                housenum = client.Player.Guild.GuildHouseNumber;
                                            }
                                            else
                                                housenum = 0;

                                            string mes = "I";
                                            mes += ',' + client.Player.Guild.GuildLevel.ToString(); // Guild Level
                                            mes += ',' + client.Player.Guild.GetGuildBank().ToString(); // Guild Bank money
                                            mes += ',' + client.Player.Guild.GetGuildDuesPercent().ToString(); // Guild Dues enable/disable
                                            mes += ',' + client.Player.Guild.BountyPoints.ToString(); // Guild Bounty
                                            mes += ',' + client.Player.Guild.RealmPoints.ToString(); // Guild Experience
                                            mes += ',' + client.Player.Guild.MeritPoints.ToString(); // Guild Merit Points
                                            mes += ',' + housenum.ToString(); // Guild houseLot ?
                                            mes += ',' + (client.Player.Guild.MemberOnlineCount + 1).ToString(); // online Guild member ?
                                            mes += ',' + client.Player.Guild.GuildBannerStatus(client.Player); //"Banner available for purchase", "Missing banner buying permissions"
                                            mes += ",\"" + client.Player.Guild.Motd + '\"'; // Guild Motd
                                            mes += ",\"" + client.Player.Guild.Omotd + '\"'; // Guild oMotd
                                            client.Out.SendMessage(mes, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
                                            break;
                                        }
                                    case "2": //enable/disable social windows
                                        {
                                            // "P,ShowGuildWindow,ShowAllianceWindow,?,ShowLFGuildWindow(only with guild),0,0" // news and friend windows always showed
                                            client.Out.SendMessage("P," + (client.Player.Guild == null ? "0" : "1") + (client.Player.Guild.AllianceId != string.Empty ? "0" : "1") + ",0,0,0,0", eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }

                            SendSocialWindowData(client, 1, 1, 2);
                            break;
                        }
                    #endregion
                    #region Buybanner
                    case "buybanner":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            long bannerPrice;

                            if (client.Account.PrivLevel > 1) // GMs can buy a banner for any guild
                            {
                                bannerPrice = 0;
                            }
                            else if (client.Player.Guild.GuildType == Guild.eGuildType.RvRGuild) // RvR guilds can buy a banner for 1000 MP
                            {
                                bannerPrice = 1000;
                            }
                            else // Player guilds with level >= 7 can buy a banner
                            {
                                if (client.Player.Guild.GuildType != Guild.eGuildType.PlayerGuild)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.Guild.GuildLevel < 7)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.BuyBanner))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                bannerPrice = (client.Player.Guild.GuildLevel * 100);
                            }


                            if (client.Player.Guild.HasGuildBanner)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerAlready"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

                                if (lostTime.TotalMinutes < Properties.GUILD_BANNER_LOST_TIME)
                                {
                                    int hoursLeft = (int)((Properties.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 30) / 60);
                                    if (hoursLeft < 2)
                                    {
                                        int minutesLeft = (int)(Properties.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 1);
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Banner.LostMinutes", minutesLeft), eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Banner.LostHours", hoursLeft), eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
                                    }
                                    return;
                                }
                            }


                            client.Player.Guild.UpdateGuildWindow();

                            if (client.Account.PrivLevel > (int)ePrivLevel.Player)
                            {
                                ConfirmBannerBuy(client.Player, 0x01);
                            }
                            else
                            {
                                if (client.Player.Guild.MeritPoints > bannerPrice)
                                {
                                    client.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Banner.BuyPrice", bannerPrice), ConfirmBannerBuy);
                                    client.Player.TempProperties.setProperty(GUILD_BANNER_PRICE, bannerPrice);
                                }
                                else
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerNotAfford", (Math.Max(0, bannerPrice - client.Player.Guild.MeritPoints))), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            break;
                        }
                    #endregion
                    #region Summon
                    case "summon":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1 && client.Player.Guild.GuildType != Guild.eGuildType.RvRGuild)
                            {
                                if (client.Player.Guild.IsSystemGuild)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.Guild.GuildLevel < 7)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Summon))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            if (!client.Player.Guild.HasGuildBanner)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerNone"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Group == null && !Properties.GUILD_BANNER_ALLOW_SOLO && client.Account.PrivLevel == (int)ePrivLevel.Player)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerNoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Player.Guild.ActiveGuildBanner != null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerGuildSummoned"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Player.Group != null)
                            {
                                foreach (GamePlayer groupPlayer in client.Player.Group.GetPlayersInTheGroup())
                                {
                                    if (groupPlayer.GuildBanner != null)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerGroupSummoned"), eChatType.CT_Group, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                }
                            }

                            if (GameServer.ServerRules.IsAllowedToSummonBanner(client.Player, false))
                            {
                                client.Player.Guild.ActiveGuildBanner = new GuildBanner(client.Player);
                                foreach (GamePlayer player in client.Player.Guild.GetListOfOnlineMembers())
                                {
                                    if (player == client.Player)
                                    {
                                        player.Out.SendMessage(
                                            LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerSummoned.You", client.Player.Name),
                                            eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                        );
                                    }
                                    else
                                    {
                                        player.Out.SendMessage(
                                            LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerSummoned", client.Player.Name),
                                            eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                        );
                                    }
                                }
                                client.Player.Guild.UpdateGuildWindow();
                            }

                            break;
                        }
                    #endregion
                    #region Buff
                    // --------------------------------------------------------------------------------
                    // GUILD BUFF
                    // --------------------------------------------------------------------------------
                    case "buff":
                        {
                            if (client.Player.Guild == null || (client.Player.Guild.IsSystemGuild == true && client.Account.PrivLevel <= 1))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Player.Guild.IsSystemGuild && client.Account.PrivLevel <= 1)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader) || !client.Player.Guild.HasRank(client.Player, Guild.eRank.Buff))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            int guildLevel = (int)client.Player.Guild.GuildLevel;
                            int meritPointCost = 1000;
                            double bonusMultiplier = 1.0;

                            if (guildLevel >= 8 && guildLevel <= 15)
                            {
                                meritPointCost = 2000;
                                bonusMultiplier *= 1.5;
                            }
                            else if (guildLevel >= 16)
                            {
                                meritPointCost = 3000;
                                bonusMultiplier *= 2;
                            }

                            if (client.Player.Guild.MeritPoints < meritPointCost)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.MeritPointReq", (Math.Max(0, meritPointCost - client.Player.Guild.MeritPoints)), meritPointCost), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Player.Guild.BonusType == Guild.eBonusType.None && args.Length > 2)
                            {
                                if (args[2] == "rps")
                                {
                                    if (Properties.GUILD_BUFF_RP > 0)
                                    {
                                        client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.RealmPoints);
                                        client.Out.SendCustomDialog(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.Activate.RP",
                                                meritPointCost
                                            ),
                                            ConfirmBuffBuy);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotAvailable"
                                            ),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                    }
                                    return;
                                }
                                else if (args[2] == "bps")
                                {
                                    if (Properties.GUILD_BUFF_BP > 0)
                                    {
                                        client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.BountyPoints);
                                        client.Out.SendCustomDialog(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.Buy",
                                                meritPointCost
                                            ),
                                            ConfirmBuffBuy);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotAvailable"
                                            ),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                    }
                                    return;
                                }
                                else if (args[2] == "crafting")
                                {
                                    if (Properties.GUILD_BUFF_CRAFTING > 0)
                                    {
                                        client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.CraftingHaste);
                                        client.Out.SendCustomDialog(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.Activate.Crafting",
                                                meritPointCost
                                            ),
                                            ConfirmBuffBuy);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotAvailable"
                                            ),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                    }
                                    return;
                                }
                                else if (args[2] == "xp")
                                {
                                    if (Properties.GUILD_BUFF_XP > 0)
                                    {
                                        client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.Experience);
                                        client.Out.SendCustomDialog(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.Activate.XP",
                                                meritPointCost
                                            ),
                                            ConfirmBuffBuy);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotAvailable"),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                    }
                                    return;
                                }
                                else if (args[2] == "artifact")
                                {
                                    if (Properties.GUILD_BUFF_ARTIFACT_XP > 0)
                                    {
                                        client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.ArtifactXP);
                                        client.Out.SendCustomDialog(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.Activate.Artifact",
                                                meritPointCost
                                            ),
                                            ConfirmBuffBuy);
                                    }
                                    else
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotAvailable"),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                    }
                                    return;
                                }
                                else if (args[2] == "mlxp")
                                {
                                    if (Properties.GUILD_BUFF_MASTERLEVEL_XP > 0)
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotImplemented"
                                            ),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                        //client.Player.TempProperties.setProperty(GUILD_BUFF_TYPE, Guild.eBonusType.MasterLevelXP);
                                        //client.Out.SendCustomDialog("Are you sure you want to activate a guild Masterlevel XP buff for 1000 merit points?", ConfirmBuffBuy);
                                        return;

                                    }
                                    else
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Buff.NotAvailable"),
                                            eChatType.CT_System,
                                            eChatLoc.CL_SystemWindow);
                                    }

                                    return;
                                }
                                else
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildBuff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            else
                            {
                                if (client.Player.Guild.BonusType == Guild.eBonusType.None)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildBuff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    TimeSpan totalBuffDuration = TimeSpan.FromMinutes(ServerProperties.Properties.GUILD_BUFF_DURATION_MINUTES);
                                    TimeSpan bonusTime = DateTime.Now.Subtract(client.Player.Guild.BonusStartTime);
                                    TimeSpan remainingTime = totalBuffDuration - bonusTime;
                                    remainingTime = remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
                                    int remainingHours = remainingTime.Hours;
                                    int remainingMinutes = remainingTime.Minutes;

                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.ActiveBuff", remainingHours, remainingMinutes), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                            }

                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InfoGuildBuffAvailable"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            if (ServerProperties.Properties.GUILD_BUFF_ARTIFACT_XP > 0)
                                client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.ArtifactXP), (ServerProperties.Properties.GUILD_BUFF_ARTIFACT_XP * bonusMultiplier)), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            if (ServerProperties.Properties.GUILD_BUFF_BP > 0)
                                client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.BountyPoints), (ServerProperties.Properties.GUILD_BUFF_BP * bonusMultiplier)), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            if (ServerProperties.Properties.GUILD_BUFF_CRAFTING > 0)
                                client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.CraftingHaste), (ServerProperties.Properties.GUILD_BUFF_CRAFTING * bonusMultiplier)), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            if (ServerProperties.Properties.GUILD_BUFF_XP > 0)
                                client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.Experience), (ServerProperties.Properties.GUILD_BUFF_XP * bonusMultiplier)), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            //if (ServerProperties.Properties.GUILD_BUFF_MASTERLEVEL_XP > 0)
                            //    client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.MasterLevelXP), ServerProperties.Properties.GUILD_BUFF_MASTERLEVEL_XP), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            if (ServerProperties.Properties.GUILD_BUFF_RP > 0)
                                client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.RealmPoints), (ServerProperties.Properties.GUILD_BUFF_RP * bonusMultiplier)), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            return;
                        }
                    #endregion
                    #region Unsummon
                    case "unsummon":
                        {
                            if (client.Player.InCombat)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Player.GuildBanner != null) // player is wearing the banner they want to unsummon
                            {
                                GuildBanner banner = client.Player.GuildBanner;
                                banner.Stop();
                                if (banner.Guild != null)
                                {
                                    banner.Guild.ActiveGuildBanner = null;
                                    foreach (GamePlayer player in banner.Guild.GetListOfOnlineMembers())
                                    {
                                        if (player != client.Player)
                                        {
                                            player.Out.SendMessage(
                                                LanguageMgr.GetTranslation(
                                                    player.Client.Account.Language,
                                                    "Commands.Players.Guild.BannerUnsummoned",
                                                    client.Player.Name
                                                ),
                                                eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                            );
                                        }
                                    }
                                }
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerUnsummoned.You"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                banner.Guild.UpdateGuildWindow();
                            }
                            else // player is not wearing a banner, find banner in guild
                            {
                                if (client.Player.Guild == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                GuildBanner banner = client.Player.Guild.ActiveGuildBanner;

                                if (banner == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerUnsummon.NoBanner"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (banner.CarryingPlayer == null)
                                {
                                    if (banner.BannerItem.WorldItem != null)
                                    {
                                        // Banner on the ground as an item
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerUnsummon.Dropped"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                    else
                                    {
                                        // Banner bugged. Still marked as "active" but has no carrier and is not actually on the ground
                                        if (log.IsWarnEnabled)
                                        {
                                            log.WarnFormat("Player {0} is unsummoning bugged banner {1}", client.Player.Name, banner.BannerItem.Id_nb);
                                        }
                                        banner.Guild.ActiveGuildBanner = null;
                                        banner.Stop();
                                        foreach (GamePlayer player in banner.Guild.GetListOfOnlineMembers())
                                        {
                                            player.Out.SendMessage(
                                                LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerUnsummoned.Forced", client.Player.Name, "(?)"),
                                                eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                            );
                                        }
                                        return;
                                    }
                                }

                                GamePlayer carryingPlayer = banner.CarryingPlayer;
                                if (banner.CarryingPlayer.Guild == client.Player.Guild)
                                {
                                    if (banner.CarryingPlayer.GuildRank.RankLevel <= client.Player.GuildRank.RankLevel)
                                    {
                                        // Player currently carrying is higher rank
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BannerUnsummon.Denied", banner.CarryingPlayer.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                    banner.Stop();
                                    foreach (GamePlayer player in banner.Guild.GetListOfOnlineMembers())
                                    {
                                        if (player == carryingPlayer)
                                        {
                                            player.Out.SendMessage(
                                                LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerUnsummoned.You.Forced", client.Player.Name),
                                                eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                            );
                                        }
                                        else
                                        {
                                            player.Out.SendMessage(
                                                LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerUnsummoned.Forced", client.Player.Name, carryingPlayer.Name),
                                                eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                            );
                                        }
                                    }
                                }
                                else
                                {
                                    // Banner carried by group member not in guild
                                    banner.Guild.ActiveGuildBanner = null;
                                    banner.Stop();
                                    carryingPlayer.Out.SendMessage(
                                        LanguageMgr.GetTranslation(carryingPlayer.Client.Account.Language, "Commands.Players.Guild.BannerUnsummoned.You.Forced", client.Player.Name),
                                        eChatType.CT_Group, eChatLoc.CL_SystemWindow
                                    );
                                    foreach (GamePlayer player in banner.Guild.GetListOfOnlineMembers())
                                    {
                                        player.Out.SendMessage(
                                            LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerUnsummoned.Forced", client.Player.Name),
                                            eChatType.CT_Guild, eChatLoc.CL_SystemWindow
                                        );
                                    }
                                }
                                banner.Guild.UpdateGuildWindow();
                            }
                            break;
                        }
                    #endregion
                    #region Ranks
                    case "ranks":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            if (!client.Player.GuildRank.GcHear)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            foreach (DBRank rank in client.Player.Guild.Ranks)
                            {
                                client.Out.SendMessage("RANK: " + rank.RankLevel.ToString() + " NAME: " + rank.Title, eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("AcHear: " + (rank.AcHear ? "y" : "n") + " AcSpeak: " + (rank.AcSpeak ? "y" : "n"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("OcHear: " + (rank.OcHear ? "y" : "n") + " OcSpeak: " + (rank.OcSpeak ? "y" : "n"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("GcHear: " + (rank.GcHear ? "y" : "n") + " GcSpeak: " + (rank.GcSpeak ? "y" : "n"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("Emblem: " + (rank.Emblem ? "y" : "n") + " Promote: " + (rank.Promote ? "y" : "n"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("Remove: " + (rank.Remove ? "y" : "n") + " View: " + (rank.View ? "y" : "n"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("Dues: " + (rank.Dues ? "y" : "n") + " Withdraw: " + (rank.Withdraw ? "y" : "n"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            break;
                        }
                    #endregion
                    #region Webpage
                    case "webpage":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            message = String.Join(" ", args, 2, args.Length - 2);
                            client.Player.Guild.Webpage = message;
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.WebpageSet", client.Player.Guild.Webpage), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                            break;
                        }
                    #endregion
                    #region Email
                    case "email":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            message = String.Join(" ", args, 2, args.Length - 2);
                            client.Player.Guild.Email = message;
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.EmailSet", client.Player.Guild.Email), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                            break;
                        }
                    #endregion
                    #region JailRelease

                    case "jailrelease":
                        {
                            GamePlayer player = client.Player;

                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.NoGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Player.Guild.IsSystemGuild && client.Account.PrivLevel <= 1)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            Guild guild = client.Player.Guild;

                            if (args.Length < 3)
                            {
                                DisplayHelp(client);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                if (client.Player.Guild.IsSystemGuild)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.Guild.GuildLevel < 6)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.GuildRank.RankLevel > 3)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.RankTooLow"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            Prisoner jailedPlayer = JailMgr.GetPrisoner(args[2]);
                            if (jailedPlayer == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.NotInJail", args[2]), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (Properties.GUILD_JAILRELEASE_GUILD_ONLY)
                            {
                                string guildID = WorldMgr.GetClientByPlayerName(jailedPlayer.Name, true, false)?.Player.GuildID ?? DOLDB<DOLCharacters>.SelectObject(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(jailedPlayer.Name))?.GuildID;
                                if (guildID == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (!guildID.Equals(client.Player.GuildID))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.PlayerNotInGuild", args[2]), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            client.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.ConfirmCost", jailedPlayer.Name, client.Account.PrivLevel > 1 ? 0 : jailedPlayer.Cost), (GamePlayer player, byte response) =>
                            {
                                if (response == 1)
                                {
                                    if (client.Account.PrivLevel == 1)
                                    {
                                        var cost = jailedPlayer.Cost * 10000;
                                        if (guild.GetGuildBank() < cost)
                                        {
                                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.NotEnoughMoney", jailedPlayer.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            return;
                                        }

                                        guild.WithdrawGuildBank(player, cost, false);
                                    }
                                    JailMgr.Relacher(jailedPlayer.Name, true);
                                    foreach (GamePlayer onlinePlayer in guild.GetListOfOnlineMembers())
                                    {
                                        onlinePlayer.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.JailRelease.Released", jailedPlayer.Name, client.Player.Name), eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
                                    }

                                    NewsMgr.CreateNews("Commands.Players.Guild.JailRelease.Released", guild.Realm, eNewsType.RvRGlobal, false, true, jailedPlayer.Name, client.Player.Name);
                                    if (Properties.DISCORD_ACTIVE)
                                    {
                                        DolWebHook hook = new DolWebHook(Properties.DISCORD_WEBHOOK_ID);
                                        hook.SendMessage(LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "Commands.Players.Guild.JailRelease.Released", jailedPlayer.Name, client.Player.Name));
                                    }
                                }
                            });
                            break;
                        }
                    #endregion

                    #region Territorybanner
                    case "territorybanner":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                if (client.Player.Guild.IsSystemGuild)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.Guild.GuildLevel < 3)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            var territory = TerritoryManager.GetCurrentTerritory(client.Player);
                            if (territory == null)
                            {
                                client.SendTranslation("Commands.Players.Guild.TerritoryBanner.NotInTerritory");
                                return;
                            }

                            if (territory.Type == Territory.eType.Subterritory)
                            {
                                client.SendTranslation("Commands.Players.Guild.CantInSubterritory");
                                return;
                            }

                            if (!territory.IsOwnedBy(client.Player))
                            {
                                client.SendTranslation("Commands.Players.Guild.TerritoryBanner.NotInTerritory");
                                return;
                            }

                            if (client.Player.GuildRank.RankLevel > 2 && client.Account.PrivLevel == 1)
                            {
                                client.SendTranslation("Commands.Players.Guild.TerritoryBanner.Denied");
                                return;
                            }

                            if (territory.IsBannerSummoned && client.Account.PrivLevel == 1)
                            {
                                client.SendTranslation("Commands.Players.Guild.TerritoryBanner.AlreadySummoned");
                                return;
                            }

                            client.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryBanner.ConfirmCost", Properties.GUILD_BANNER_MERIT_PRICE), (GamePlayer player, byte response) =>
                            {
                                if (response == 1)
                                {
                                    if (player.Guild.MeritPoints < (long)Properties.GUILD_BANNER_MERIT_PRICE)
                                    {
                                        client.SendTranslation("Commands.Players.Guild.TerritoryBanner.NotEnoughMerit", eChatType.CT_System, eChatLoc.CL_SystemWindow, Properties.GUILD_BANNER_MERIT_PRICE);
                                        return;
                                    }

                                    player.Guild.RemoveMeritPoints(Properties.GUILD_BANNER_MERIT_PRICE);
                                    if (territory.IsBannerSummoned)
                                        territory.ToggleBanner(false);
                                    territory.ToggleBanner(true);
                                    foreach (GamePlayer guildPlayer in player.Guild.GetListOfOnlineMembers())
                                    {
                                        guildPlayer.SendTranslatedMessage("Commands.Players.Guild.TerritoryBanner.Summoned", eChatType.CT_Guild, eChatLoc.CL_SystemWindow, guildPlayer.GetPersonalizedName(player), territory.Name);
                                    }
                                }
                            });
                        }
                        break;

                    #endregion
                    #region TerritoryPortal
                    case "territoryportal":
                        {
                            GamePlayer player = client.Player;

                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.NoGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                if (client.Player.Guild.IsSystemGuild)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.Guild.GuildLevel < 4)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            Guild guild = player.Guild;
                            var territory = TerritoryManager.GetCurrentTerritory(player);
                            if (territory == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.NotInTerritory"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (territory.PortalCoordinate == null)
                            {
                                client.SendTranslation("Commands.Players.Guild.TerritoryPortal.NotSupported");
                                return;
                            }

                            if (!territory.IsOwnedBy(player) || !GameServer.ServerRules.IsInPvPArea(player))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.NotInTerritory"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel == 1)
                            {
                                if (player.GuildRank.RankLevel > 4)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.Denied"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                var availableTick = guild.GuildPortalAvailableTick;
                                if (availableTick > GameServer.Instance.TickCount)
                                {
                                    client.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.Cooldown")), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (guild.MeritPoints < (long)Properties.GUILD_PORTAL_MERIT_PRICE)
                                {
                                    client.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.NotEnoughMerit"), Properties.GUILD_PORTAL_MERIT_PRICE), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            client.Out.SendCustomDialog(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.ConfirmCost"), client.Account.PrivLevel > 1 ? 0 : Properties.GUILD_PORTAL_MERIT_PRICE), (GamePlayer player, byte response) =>
                            {
                                if (response == 1)
                                {
                                    if (client.Account.PrivLevel == 1)
                                    {
                                        if (player.Guild.MeritPoints < (long)Properties.GUILD_PORTAL_MERIT_PRICE)
                                        {
                                            client.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.TerritoryPortal.NotEnoughMerit"), Properties.GUILD_PORTAL_MERIT_PRICE), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            return;
                                        }

                                        player.Guild.RemoveMeritPoints(Properties.GUILD_PORTAL_MERIT_PRICE);
                                        if (Properties.GUILD_COMBAT_ZONE_COOLDOWN > 0)
                                        {
                                            player.Guild.GuildPortalAvailableTick = GameServer.Instance.TickCount + (uint)(Properties.GUILD_PORTAL_COOLDOWN) * 60 * 1000;
                                        }
                                    }
                                    territory.SpawnPortalNpc(player);
                                }
                            });
                        }
                        break;

                        case "buyterritorydefender":
                        {
                            if (args.Length < 3)
                            {
                                client.Player.SendTranslatedMessage("Commands.Players.Guild.Help.BuyTerritoryDefender");
                                return;
                            }

                            GamePlayer player = client.Player;

                            if (player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.NoGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!player.Guild.HasRank(client.Player, Guild.eRank.TerritoryDefenders))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            long maxDefenders = player.Guild.MaxTerritoryDefenders;
                            if (maxDefenders <= 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.DefenderLimit"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            var territory = TerritoryManager.GetCurrentTerritory(player);
                            if (territory?.IsOwnedBy(player) != true)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.NotInTerritory"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (territory.Type == Territory.eType.Subterritory)
                            {
                                client.SendTranslation("Commands.Players.Guild.CantInSubterritory");
                                return;
                            }

                            if (maxDefenders <= territory.NumMercenaries)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.DefenderLimit", maxDefenders), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            NpcTemplate npcTemplate;

                            switch (args[2]) {
                                default:
                                    DisplaySyntax(client, "buyterritorydefender");
                                    return;

                                case "guard":
                                    npcTemplate = territory.GuardNPCTemplate;
                                    break;

                                case "healer":
                                    npcTemplate = territory.HealerNPCTemplate;
                                    break;

                                case "mage":
                                    npcTemplate = territory.MageNPCTemplate;
                                    break;

                                case "archer":
                                    npcTemplate = territory.ArcherNPCTemplate;
                                    break;
                            };

                            if (npcTemplate == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.UnsupportedType", territory.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            int price = client.Account.PrivLevel > 1 ? 0 : Properties.TERRITORY_DEFENDER_PRICE;
                            if (price > 0)
                            {
                                if (client.Player.Guild.GetGuildBank() < price * 10000)
                                {
                                    player.SendTranslatedMessage("Commands.Players.Guild.BuyDefender.NoMoney", eChatType.CT_System, eChatLoc.CL_ChatWindow, price);
                                    return;
                                }
                                client.Out.SendCustomDialog(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.ConfirmCost"), territory.Name, price), (GamePlayer player, byte response) =>
                                {
                                    if (response == 1)
                                    {
                                        if (!player.Guild.WithdrawGuildBank(player, price * 10000, false))
                                        {
                                            player.SendTranslatedMessage("Commands.Players.Guild.BuyDefender.NoMoney", eChatType.CT_System, eChatLoc.CL_ChatWindow, price);
                                            return;
                                        }
                                        try
                                        {
                                            GameNPC npc = territory.AddMercenary(player, npcTemplate);
                                            player.SendTranslatedMessage("Commands.Players.Guild.BuyDefender.Bought", eChatType.CT_System, eChatLoc.CL_ChatWindow, player.GetPersonalizedName(npc), territory.Name);
                                        }
                                        catch (Exception ex)
                                        {
                                            player.SendTranslatedMessage("Language.InternalError");
                                            throw;
                                        }
                                    }
                                });

                            }
                            else
                            {
                                try
                                {
                                    GameNPC npc = territory.AddMercenary(player, npcTemplate);
                                    player.SendTranslatedMessage("Commands.Players.Guild.BuyDefender.Bought", eChatType.CT_System, eChatLoc.CL_ChatWindow, player.GetPersonalizedName(npc), territory.Name);
                                }
                                catch (Exception ex)
                                {
                                    player.SendTranslatedMessage("Language.InternalError");
                                    throw;
                                }
                            }
                            break;
                        }

                        case "moveterritorydefender":
                        {
                            GamePlayer player = client.Player;
                            if (player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.BuyDefender.NoGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!player.Guild.HasRank(client.Player, Guild.eRank.TerritoryDefenders))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            var target = player.TargetObject as GameNPC;
                            if (target == null || !target.IsMercenary || target.CurrentTerritory?.IsOwnedBy(player) != true)
                            {
                                player.SendTranslatedMessage("Commands.Players.Guild.MoveDefender.BadTarget", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            var territory = TerritoryManager.GetCurrentTerritory(player);
                            if (territory != target.CurrentTerritory)
                            {
                                player.SendTranslatedMessage("Commands.Players.Guild.MoveDefender.BadTerritory", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (!target.MoveTo(player.Position))
                            {
                                player.SendTranslatedMessage("Commands.Players.Guild.MoveDefender.BadPosition", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                player.SendTranslatedMessage("Commands.Players.Guild.MoveDefender.Moved", eChatType.CT_System, eChatLoc.CL_SystemWindow, player.GetPersonalizedName(target));
                                target.SaveIntoDatabase();
                            }
                            return;
                        }

                    #endregion
                    #region CombatZone

                    case "combatzone":
                        {
                            GamePlayer player = client.Player;
                            Guild guild = player.Guild;
                            Region region = player.CurrentRegion;

                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.NoGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                break;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                if (client.Player.Guild.IsSystemGuild)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (client.Player.Guild.GuildLevel < 5)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            if (region.IsRvR || PvpManager.Instance.IsPvPRegion(region.ID))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.CantInPvPRegion"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                break;
                            }

                            if (region.IsDungeon)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.CantInDungeon"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                break;
                            }

                            if (client.Account.PrivLevel == 1)
                            {
                                // For players, check guild rank, cooldown, price, and area proximity
                                if (player.GuildRank.RankLevel > 2)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.Denied"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    break;
                                }
                                var availableTick = guild.GuildCombatZoneAvailableTick;
                                if (availableTick > GameServer.Instance.TickCount)
                                {
                                    uint totalSeconds = (availableTick - GameServer.Instance.TickCount) / 1000;
                                    uint diffHours = totalSeconds / 3600;
                                    uint diffMinutes = (totalSeconds % (3600)) / 60;
                                    uint diffSeconds = totalSeconds % (60);
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.Cooldown", diffHours, diffMinutes, diffSeconds), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    break;
                                }

                                if (guild.MeritPoints < (long)Properties.GUILD_COMBAT_ZONE_MERIT_PRICE)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.NotEnoughMerit", Properties.GUILD_COMBAT_ZONE_MERIT_PRICE), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    break;
                                }

                                if (Properties.GUILD_COMBAT_ZONE_DISTANCE_FROM_AREAS > 0)
                                {
                                    AbstractArea closeArea = (AbstractArea)region.FindAnyAreaInRadius(player.Coordinate, Properties.GUILD_COMBAT_ZONE_DISTANCE_FROM_AREAS, true);
                                    if (closeArea != null)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.TooCloseToArea", closeArea.GetDescriptionForPlayer(player)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        break;
                                    }
                                }

                                client.Out.SendCustomDialog(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.ConfirmCost"), Properties.GUILD_COMBAT_ZONE_MERIT_PRICE), (GamePlayer player, byte response) =>
                                {
                                    if (response == 1)
                                    {
                                        if (player.Guild.MeritPoints < (long)Properties.GUILD_COMBAT_ZONE_MERIT_PRICE)
                                        {
                                            client.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.CombatZone.NotEnoughMerit"), Properties.GUILD_COMBAT_ZONE_MERIT_PRICE), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            return;
                                        }

                                        player.Guild.RemoveMeritPoints(Properties.GUILD_COMBAT_ZONE_MERIT_PRICE);
                                        if (Properties.GUILD_COMBAT_ZONE_COOLDOWN > 0)
                                        {
                                            player.Guild.GuildCombatZoneAvailableTick = GameServer.Instance.TickCount + (uint)(Properties.GUILD_COMBAT_ZONE_COOLDOWN) * 60 * 1000;
                                        }
                                        region.CreateCombatZone(guild, player.Position);
                                    }
                                });
                            }
                            else
                            {
                                // GMs can spawn it anywhere for free except PVP RVR & dungeons
                                region.CreateCombatZone(guild, player.Position);
                            }
                        }
                        break;
                    #endregion
                    #region List
                    // --------------------------------------------------------------------------------
                    // LIST
                    // --------------------------------------------------------------------------------
                    case "list":
                        {
                            // Changing this to list online only, not sure if this is live like or not but list can be huge
                            // and spam client.  - Tolakram
                            List<Guild> guildList = GuildMgr.GetAllGuilds();
                            foreach (Guild guild in guildList)
                            {
                                if (guild.MemberOnlineCount > 0)
                                {
                                    string mesg = guild.Name + "  " + guild.MemberOnlineCount + " members ";
                                    client.Out.SendMessage(mesg, eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Edit
                    // --------------------------------------------------------------------------------
                    // EDIT
                    // --------------------------------------------------------------------------------
                    case "edit":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            GCEditCommand(client, args);
                        }
                        client.Player.Guild.UpdateGuildWindow();
                        break;
                    #endregion
                    #region Form
                    // --------------------------------------------------------------------------------
                    // FORM
                    // --------------------------------------------------------------------------------
                    case "form":
                        {
                            Group group = client.Player.Group;
                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildForm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #region Near Registrar
                            if (!IsNearRegistrar(client.Player))
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.FormNoRegistrar"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region No group Check
                            if (group == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.FormNoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region Groupleader Check
                            if (group != null && client.Player != client.Player.Group.Leader)
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Form.NoLeader"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region Enough members to form Check
                            if (group.MemberCount < Properties.GUILD_NUM)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.FormNoMembers" + Properties.GUILD_NUM), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region Player already in guild check and Cross Realm Check

                            foreach (GamePlayer ply in group.GetPlayersInTheGroup())
                            {
                                if (ply.Guild != null)
                                {
                                    client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.AlreadyInGuildName", ply.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (ply.Realm != client.Player.Realm && ServerProperties.Properties.ALLOW_CROSS_REALM_GUILDS == false)
                                {
                                    client.Out.SendMessage(
                                        LanguageMgr.GetTranslation(
                                            client.Account.Language,
                                            "Commands.Players.Guild.Form.NotSameRealm"),
                                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            #endregion
                            #region Guild Length Naming Checks
                            //Check length of guild name.
                            string guildname = String.Join(" ", args, 2, args.Length - 2);
                            if (guildname.Length > 30)
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Form.TooLong"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region Valid Characters Check
                            if (!IsValidGuildName(guildname))
                            {
                                // Mannen doesn't know the live server message, so someone needs to enter it . ;-)
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.InvalidLetters"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region Guild Exist Checks
                            if (GuildMgr.DoesGuildExist(guildname))
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.GuildExists"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion
                            #region Enoguh money to form Check
                            if (client.Player.Group.Leader.CopperBalance < GuildFormCost)
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Form.NoMoney",
                                        GuildFormCost),
                                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            #endregion


                            client.Player.Group.Leader.TempProperties.setProperty("Guild_Name", guildname);
                            if (GuildFormCheck(client.Player))
                            {
                                client.Player.Group.Leader.TempProperties.setProperty("Guild_Consider", true);
                                foreach (GamePlayer p in group.GetPlayersInTheGroup().Where(p => p != @group.Leader))
                                {
                                    p.Out.SendCustomDialog(
                                        LanguageMgr.GetTranslation(
                                            client.Account.Language,
                                            "Commands.Players.Guild.Form.ConfirmCreate",
                                            guildname,
                                            client.Player.Name),
                                            new CustomDialogResponse(CreateGuild));
                                }
                            }
                        }
                        break;
                    #endregion
                    #region Quit
                    // --------------------------------------------------------------------------------
                    // QUIT
                    // --------------------------------------------------------------------------------
                    case "quit":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Out.SendGuildLeaveCommand(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.ConfirmLeave", client.Player.Guild.Name));
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Promote
                    // --------------------------------------------------------------------------------
                    // PROMOTE
                    // /gc promote [name] <rank#>' to promote player to a superior rank
                    // --------------------------------------------------------------------------------
                    case "promote":
                        {
                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildPromote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                if (client.Player.Guild == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Promote))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            object obj = null;
                            ushort newrank;

                            try
                            {
                                if (args.Length >= 4)
                                {
                                    newrank = Convert.ToUInt16(args[3]);
                                    GameClient onlineClient = WorldMgr.GetClientByPlayerName(args[2], true, false);
                                    if (onlineClient == null)
                                    {
                                        // Patch 1.84: look for offline players
                                        obj = DOLDB<DOLCharacters>.SelectObject(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(args[2]));
                                    }
                                    else
                                    {
                                        obj = onlineClient.Player;
                                    }

                                    if (obj == null)
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.NoPlayerWithName", args[2]),
                                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                }
                                else
                                {
                                    newrank = Convert.ToUInt16(args[2]);
                                    obj = client.Player.TargetObject as GamePlayer;
                                    if (obj == null)
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.NoPlayerSelected"
                                            ),
                                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Help.GuildPromote"),
                                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                }

                                if (newrank > 9)
                                {
                                    client.Out.SendMessage(
                                        LanguageMgr.GetTranslation(
                                            client.Account.Language,
                                            "Commands.Players.Guild.Rank.ErrorChanging"
                                        ),
                                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            catch
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Rank.ErrorChanging"
                                    ),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Help.GuildPromote"),
                                    eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            //First Check Routines, GuildIDControl search for player or character.
                            string guildId = "";
                            string plyName = "";
                            ushort currentTargetGuildRank = 9;
                            Guild guild;
                            GamePlayer ply = obj as GamePlayer;
                            DOLCharacters ch = obj as DOLCharacters;

                            if (ply != null)
                            {
                                plyName = ply.Name;
                                guild = ply.Guild;
                                guildId = ply.GuildID;
                                currentTargetGuildRank = ply.GuildRank.RankLevel;
                            }
                            else if (ch != null)
                            {
                                guild = GuildMgr.GetGuildByGuildID(ch.GuildID);
                                plyName = ch.Name;
                                guildId = ch.GuildID;
                                currentTargetGuildRank = ch.GuildRank;
                            }
                            else
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.PlayerNotFound"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (guildId != client.Player.GuildID && client.Account.PrivLevel <= 1)
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.NotInYourGuild"),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            //Second Check, Autorisation Checks, a player can promote another to it's own RealmRank or above only if: newrank(rank to be applied) >= commandUserGuildRank(usercommandRealmRank)

                            ushort commandUserGuildRank = client.Player.GuildRank.RankLevel;

                            //if (commandUserGuildRank != 0 && (newrank < commandUserGuildRank || newrank < 0)) // Do we have to authorize Self Retrograde for GuildMaster?
                            if (((newrank < commandUserGuildRank) || (newrank < 0)) && client.Account.PrivLevel == 1)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PromoteHigherThanPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (newrank > currentTargetGuildRank && commandUserGuildRank != 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PromoteHaveToUseDemote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (obj is GamePlayer)
                            {
                                ply.GuildRank = guild.GetRankByID(newrank);
                                ply.SaveIntoDatabase();
                                ply.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PromotedSelf", newrank.ToString()), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                ply.Out.SendUpdatePlayer();
                            }
                            else
                            {
                                ch.GuildRank = newrank;
                                GameServer.Database.SaveObject(ch);
                                GameServer.Database.FillObjectRelations(ch);
                            }
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PromotedOther", plyName, newrank.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Demote
                    // --------------------------------------------------------------------------------
                    // DEMOTE
                    // --------------------------------------------------------------------------------
                    case "demote":
                        {
                            if (args.Length < 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDemote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (client.Account.PrivLevel <= 1)
                            {
                                if (client.Player.Guild == null)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Demote))
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }

                            object obj = null;
                            ushort newrank;

                            try
                            {
                                if (args.Length >= 4)
                                {
                                    newrank = Convert.ToUInt16(args[3]);
                                    GameClient onlineClient = WorldMgr.GetClientByPlayerName(args[2], true, false);
                                    if (onlineClient == null)
                                    {
                                        // Patch 1.84: look for offline players
                                        obj = DOLDB<DOLCharacters>.SelectObject(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(args[2]));
                                    }
                                    else
                                    {
                                        obj = onlineClient.Player;
                                    }

                                    if (obj == null)
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.NoPlayerWithName", args[2]),
                                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                }
                                else
                                {
                                    newrank = Convert.ToUInt16(args[2]);
                                    obj = client.Player.TargetObject as GamePlayer;
                                    if (obj == null)
                                    {
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.NoPlayerSelected"
                                            ),
                                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage(
                                            LanguageMgr.GetTranslation(
                                                client.Account.Language,
                                                "Commands.Players.Guild.Help.GuildPromote"),
                                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                }

                                if (newrank > 9)
                                {
                                    client.Out.SendMessage(
                                        LanguageMgr.GetTranslation(
                                            client.Account.Language,
                                            "Commands.Players.Guild.Rank.ErrorChanging"
                                        ),
                                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                            }
                            catch
                            {
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Rank.ErrorChanging"
                                    ),
                                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Help.GuildPromote"),
                                    eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            string guildId = "";
                            ushort guildRank = 1;
                            string plyName = "";
                            Guild guild;
                            GamePlayer ply = obj as GamePlayer;
                            DOLCharacters ch = obj as DOLCharacters;
                            if (obj is GamePlayer)
                            {
                                plyName = ply.Name;
                                guildId = ply.GuildID;
                                guild = ply.Guild;
                                if (ply.GuildRank != null)
                                    guildRank = ply.GuildRank.RankLevel;
                            }
                            else
                            {
                                plyName = ch.Name;
                                guildId = ch.GuildID;
                                guildRank = ch.GuildRank;
                                guild = GuildMgr.GetGuildByGuildID(guildId);
                            }
                            if (guildId != client.Player.GuildID && client.Account.PrivLevel <= 1)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            try
                            {
                                if (newrank < guildRank || newrank > 10)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.DemotedHigherThanPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (obj is GamePlayer)
                                {
                                    ply.GuildRank = guild.GetRankByID(newrank);
                                    ply.SaveIntoDatabase();
                                    ply.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Demoted.Self", newrank.ToString()), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    ply.Out.SendUpdatePlayer();
                                }
                                else
                                {
                                    ch.GuildRank = newrank;
                                    GameServer.Database.SaveObject(ch);
                                }
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Demoted.Other", plyName, client.Player.Name, newrank.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                guild.UpdateGuildWindow();
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.InvalidRank"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }
                        break;
                    #endregion
                    #region Who
                    // --------------------------------------------------------------------------------
                    // WHO
                    // --------------------------------------------------------------------------------
                    case "who":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            int ind = 0;
                            int startInd = 0;

                            #region Social Window
                            if (args.Length == 6 && args[2] == "window")
                            {
                                int sortTemp;
                                byte showTemp;
                                int page;

                                //Lets get the variables that were sent over
                                if (Int32.TryParse(args[3], out sortTemp) && Int32.TryParse(args[4], out page) && Byte.TryParse(args[5], out showTemp) && sortTemp >= -7 && sortTemp <= 7)
                                {
                                    SendSocialWindowData(client, sortTemp, page, showTemp);
                                }
                                return;
                            }
                            #endregion

                            #region Alliance Who
                            else if (args.Length == 3)
                            {
                                if (args[2] == "alliance" || args[2] == "a")
                                {
                                    foreach (Guild guild in client.Player.Guild.alliance.Guilds)
                                    {
                                        lock (guild.GetListOfOnlineMembers())
                                        {
                                            foreach (GamePlayer ply in guild.GetListOfOnlineMembers())
                                            {
                                                if (ply.Client.IsPlaying && !ply.IsAnonymous)
                                                {
                                                    ind++;
                                                    string zoneName = (ply.CurrentZone == null ? "(null)" : ply.CurrentZone.Description);
                                                    string mesg = LanguageMgr.GetTranslation(
                                                                        client.Account.Language,
                                                                        "Commands.Players.Guild.GetListOfOnlineMembers",
                                                                        ind, ply.Name, guild.Name, ply.Level, ply.CharacterClass, zoneName);
                                                    client.Out.SendMessage(mesg, eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                                }
                                            }
                                        }
                                    }
                                    return;
                                }
                                else
                                {
                                    int.TryParse(args[2], out startInd);
                                }
                            }
                            #endregion

                            #region Who
                            IList<GamePlayer> onlineGuildMembers = client.Player.Guild.GetListOfOnlineMembers();

                            foreach (GamePlayer ply in onlineGuildMembers)
                            {
                                if (ply.Client.IsPlaying && !ply.IsAnonymous)
                                {
                                    if (startInd + ind > startInd + WhoCommandHandler.MAX_LIST_SIZE)
                                        break;
                                    ind++;
                                    string zoneName = (ply.CurrentZone == null ? "(null)" : ply.CurrentZone.Description);
                                    string mesg;
                                    if (ply.GuildRank.Title != null)
                                        mesg = ind.ToString() + ") " + ply.Name + " <" + ply.GuildRank.Title + "> the Level " + ply.Level.ToString() + " " + ply.CharacterClass.Name + " in " + zoneName;
                                    else
                                        mesg = ind.ToString() + ") " + ply.Name + " <" + ply.GuildRank.RankLevel.ToString() + "> the Level " + ply.Level.ToString() + " " + ply.CharacterClass.Name + " in " + zoneName;
                                    if (ServerProperties.Properties.ALLOW_CHANGE_LANGUAGE)
                                        mesg += " <" + ply.Client.Account.Language + ">";
                                    if (ind >= startInd)
                                        client.Out.SendMessage(mesg, eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                            }
                            if (ind > WhoCommandHandler.MAX_LIST_SIZE && ind < onlineGuildMembers.Count)
                                client.Out.SendMessage(
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Who.List.Truncated",
                                        onlineGuildMembers.Count),
                                    eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            else client.Out.SendMessage(
                                LanguageMgr.GetTranslation(
                                    client.Account.Language,
                                    "Commands.Players.Guild.TotalMemberOnline",
                                    ind.ToString()),
                                eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

                            break;
                            #endregion
                        }
                    #endregion
                    #region Leader
                    // --------------------------------------------------------------------------------
                    // LEADER
                    // --------------------------------------------------------------------------------
                    case "leader":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            GamePlayer newLeader = client.Player.TargetObject as GamePlayer;
                            if (args.Length > 2)
                            {
                                GameClient temp = WorldMgr.GetClientByPlayerName(args[2], true, false);
                                if (temp != null && GameServer.ServerRules.IsAllowedToGroup(client.Player, temp.Player, true))
                                    newLeader = temp.Player;
                            }
                            if (newLeader == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (newLeader.Guild != client.Player.Guild)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            newLeader.GuildRank = newLeader.Guild.GetRankByID(0);
                            newLeader.SaveIntoDatabase();
                            newLeader.Out.SendMessage(LanguageMgr.GetTranslation(newLeader.Client, "Commands.Players.Guild.MadeLeader", newLeader.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            foreach (GamePlayer ply in client.Player.Guild.GetListOfOnlineMembers())
                            {
                                ply.Out.SendMessage(LanguageMgr.GetTranslation(ply.Client, "Commands.Players.Guild.MadeLeaderOther", newLeader.Name, newLeader.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Emblem
                    // --------------------------------------------------------------------------------
                    // EMBLEM
                    // --------------------------------------------------------------------------------
                    case "emblem":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.Emblem != 0)
                            {
                                if (client.Player.TargetObject is EmblemNPC == false)
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.EmblemAlready"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                client.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.EmblemRedo"), new CustomDialogResponse(EmblemChange));
                                return;
                            }
                            if (client.Player.TargetObject is EmblemNPC == false)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.EmblemNPCNotSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Out.SendEmblemDialogue();

                            client.Player.Guild.UpdateGuildWindow();
                            break;
                        }
                    #endregion
                    #region Autoremove
                    case "autoremove":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Remove))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (args.Length == 4 && args[3].ToLower() == "account")
                            {
                                //#warning how can player name  !=  account if args[3] = account ?
                                string playername = args[3];
                                string accountId = "";

                                GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], false, true);
                                if (targetClient != null)
                                {
                                    OnCommand(client, new string[] { "gc", "remove", args[3] });
                                    accountId = targetClient.Account.Name;
                                }
                                else
                                {
                                    DOLCharacters c = DOLDB<DOLCharacters>.SelectObject(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(playername));

                                    if (c == null)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                    accountId = c.AccountName;
                                }
                                List<DOLCharacters> chars = new List<DOLCharacters>();
                                chars.AddRange(DOLDB<DOLCharacters>.SelectObjects(DB.Column(nameof(DOLCharacters.AccountName)).IsEqualTo(accountId)));
                                //chars.AddRange((Character[])DOLDB<CharacterArchive>.SelectObjects("AccountID = '" + accountId + "'"));

                                foreach (DOLCharacters ply in chars)
                                {
                                    ply.GuildID = "";
                                    ply.GuildRank = 0;
                                }
                                GameServer.Database.SaveObject(chars);
                                break;
                            }
                            else if (args.Length == 3)
                            {
                                GameClient targetClient = WorldMgr.GetClientByPlayerName(args[2], false, true);
                                if (targetClient != null)
                                {
                                    OnCommand(client, new string[] { "gc", "remove", args[2] });
                                    return;
                                }
                                else
                                {
                                    var c = DOLDB<DOLCharacters>.SelectObject(DB.Column(nameof(DOLCharacters.Name)).IsEqualTo(args[2]));
                                    if (c == null)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                    if (c.GuildID != client.Player.GuildID)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                    else
                                    {
                                        c.GuildID = "";
                                        c.GuildRank = 0;
                                        GameServer.Database.SaveObject(c);
                                    }
                                }
                                break;
                            }
                            else
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAutoRemoveAcc"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAutoRemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region MOTD
                    // --------------------------------------------------------------------------------
                    // MOTD
                    // --------------------------------------------------------------------------------
                    case "motd":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            message = String.Join(" ", args, 2, args.Length - 2);
                            client.Player.Guild.Motd = message;
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.MotdSet"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region AMOTD
                    // --------------------------------------------------------------------------------
                    // AMOTD
                    // --------------------------------------------------------------------------------
                    case "amotd":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.AllianceId == string.Empty)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            message = String.Join(" ", args, 2, args.Length - 2);
                            client.Player.Guild.alliance.Dballiance.Motd = message;
                            GameServer.Database.SaveObject(client.Player.Guild.alliance.Dballiance);
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.AMotdSet"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region OMOTD
                    // --------------------------------------------------------------------------------
                    // OMOTD
                    // --------------------------------------------------------------------------------
                    case "omotd":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            message = String.Join(" ", args, 2, args.Length - 2);
                            client.Player.Guild.Omotd = message;
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.OMotdSet"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Alliance
                    // --------------------------------------------------------------------------------
                    // ALLIANCE
                    // --------------------------------------------------------------------------------
                    case "alliance":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            Alliance alliance = null;
                            if (client.Player.Guild.AllianceId != null && client.Player.Guild.AllianceId != string.Empty)
                            {
                                alliance = client.Player.Guild.alliance;
                            }
                            else
                            {
                                DisplayMessage(
                                    client,
                                    LanguageMgr.GetTranslation(
                                        client.Account.Language,
                                        "Commands.Players.Guild.Alliance.NotMember")
                                    );
                                return;
                            }

                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Info", alliance.Dballiance.AllianceName));
                            DBGuild leader = alliance.Dballiance.DBguildleader;
                            if (leader != null)
                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Leader", leader.GuildName));
                            else
                                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.NoLeader"));

                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Members"));
                            int i = 0;
                            foreach (DBGuild guild in alliance.Dballiance.DBguilds)
                                if (guild != null)
                                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Member", i++, guild.GuildName));
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Alliance Invite
                    // --------------------------------------------------------------------------------
                    // AINVITE
                    // --------------------------------------------------------------------------------
                    case "ainvite":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            GamePlayer obj = client.Player.TargetObject as GamePlayer;
                            if (obj == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (obj.GuildRank.RankLevel != 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.NoGMSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (obj.Guild.alliance != null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.AlreadyOther"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.alliance != null && client.Player.Guild.alliance == obj.Guild.alliance)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.AlreadyIn"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (ServerProperties.Properties.ALLIANCE_MAX == 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Disabled"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (ServerProperties.Properties.ALLIANCE_MAX != -1)
                            {
                                if (client.Player.Guild.alliance != null)
                                {
                                    if (client.Player.Guild.alliance.Guilds.Count + 1 > ServerProperties.Properties.ALLIANCE_MAX)
                                    {
                                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Max"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                }
                            }
                            obj.TempProperties.setProperty("allianceinvite", client.Player); //finish that
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Invite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            obj.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Invited", client.Player.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Alliance Invite Accept
                    // --------------------------------------------------------------------------------
                    // AINVITE
                    // --------------------------------------------------------------------------------
                    case "aaccept":
                        {
                            AllianceInvite(client.Player, 0x01);
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Alliance Invite Cancel
                    // --------------------------------------------------------------------------------
                    // ACANCEL
                    // --------------------------------------------------------------------------------
                    case "acancel":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            GamePlayer obj = client.Player.TargetObject as GamePlayer;
                            if (obj == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            GamePlayer inviter = client.Player.TempProperties.getProperty<object>("allianceinvite", null) as GamePlayer;
                            if (inviter == client.Player)
                                obj.TempProperties.removeProperty("allianceinvite");
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.AnsCancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            obj.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.AnsCancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    #endregion
                    #region Alliance Invite Decline
                    // --------------------------------------------------------------------------------
                    // ADECLINE
                    // --------------------------------------------------------------------------------
                    case "adecline":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            GamePlayer inviter = client.Player.TempProperties.getProperty<object>("allianceinvite", null) as GamePlayer;
                            client.Player.TempProperties.removeProperty("allianceinvite");
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.Declined"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            inviter.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.DeclinedOther"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            return;
                        }
                    #endregion
                    #region Alliance Remove
                    // --------------------------------------------------------------------------------
                    // AREMOVE
                    // --------------------------------------------------------------------------------
                    case "aremove":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.alliance == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.GuildID != client.Player.Guild.alliance.Dballiance.DBguildleader.GuildID)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.NotLeader"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (args.Length > 3)
                            {
                                if (args[2] == "alliance")
                                {
                                    try
                                    {
                                        int index = Convert.ToInt32(args[3]);
                                        Guild myguild = (Guild)client.Player.Guild.alliance.Guilds[index];
                                        if (myguild != null)
                                            client.Player.Guild.alliance.RemoveGuild(myguild);
                                    }
                                    catch
                                    {
                                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.IndexNotVal"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }

                                }
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildARemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildARemoveAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            else
                            {
                                GamePlayer obj = client.Player.TargetObject as GamePlayer;
                                if (obj == null)
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (obj.Guild == null)
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.MemNotSel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                if (obj.Guild.alliance != client.Player.Guild.alliance)
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.MemNotSel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }
                                client.Player.Guild.alliance.RemoveGuild(obj.Guild);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Alliance Leave
                    // --------------------------------------------------------------------------------
                    // ALEAVE
                    // --------------------------------------------------------------------------------
                    case "aleave":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.alliance == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Alliance.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            client.Player.Guild.alliance.RemoveGuild(client.Player.Guild);
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Claim
                    // --------------------------------------------------------------------------------
                    //ClAIM
                    // --------------------------------------------------------------------------------
                    case "claim":
                        {
                            if (client.Player.Guild == null || (client.Player.Guild.IsSystemGuild == true && client.Account.PrivLevel <= 1))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.Position, WorldMgr.VISIBILITY_DISTANCE);
                            if (keep == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.ClaimNotNear"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (keep.CheckForClaim(client.Player))
                            {
                                keep.Claim(client.Player);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Release
                    // --------------------------------------------------------------------------------
                    //RELEASE
                    // --------------------------------------------------------------------------------
                    case "release":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Release))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 1)
                            {
                                if (client.Player.Guild.ClaimedKeeps[0].CheckForRelease(client.Player))
                                {
                                    client.Player.Guild.ClaimedKeeps[0].Release();
                                }
                            }
                            else
                            {
                                foreach (AbstractArea area in client.Player.CurrentAreas)
                                {
                                    if (area is KeepArea && ((KeepArea)area).Keep.Guild == client.Player.Guild)
                                    {
                                        if (((KeepArea)area).Keep.CheckForRelease(client.Player))
                                        {
                                            ((KeepArea)area).Keep.Release();
                                        }
                                    }
                                }
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Upgrade
                    // --------------------------------------------------------------------------------
                    //UPGRADE
                    // --------------------------------------------------------------------------------
                    case "upgrade":
                        {
                            client.Out.SendMessage("Keep upgrading is currently disabled!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                            /* un-comment this to work on allowing keep upgrading
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.GotAccess(client.Player, Guild.eGuildRank.Upgrade))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (args.Length != 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.KeepNoLevel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            byte targetlevel = 0;
                            try
                            {
                                targetlevel = Convert.ToByte(args[2]);
                                if (targetlevel > 10 || targetlevel < 1)
                                    return;
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Upgrade.ScndArg"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 1)
                            {
                                foreach (AbstractGameKeep keep in client.Player.Guild.ClaimedKeeps)
                                    keep.StartChangeLevel(targetlevel);
                            }
                            else
                            {
                                foreach (AbstractArea area in client.Player.CurrentAreas)
                                {
                                    if (area is KeepArea && ((KeepArea)area).Keep.Guild == client.Player.Guild)
                                        ((KeepArea)area).Keep.StartChangeLevel(targetlevel);
                                }
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                             */
                        }
                    #endregion
                    #region Type
                    //TYPE
                    // --------------------------------------------------------------------------------
                    case "type":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Upgrade))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            int type = 0;
                            try
                            {
                                type = Convert.ToInt32(args[2]);
                                if (type != 1 || type != 2 || type != 4)
                                    return;
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Upgrade.ScndArg"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            //client.Player.Guild.ClaimedKeep.Release();
                            client.Player.Guild.UpdateGuildWindow();
                            return;
                        }
                    #endregion
                    #region Noteself
                    case "noteself":
                    case "note":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            string note = String.Join(" ", args, 2, args.Length - 2);
                            client.Player.GuildNote = note;
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoteSet", note), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            client.Player.Guild.UpdateGuildWindow();
                            break;
                        }
                    #endregion
                    #region Dues
                    case "dues":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.IsSystemGuild && client.Account.PrivLevel <= 1)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.SystemGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.GuildLevel < 2)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Dues))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (args[2] == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDues"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            long amount = long.Parse(args[2]);
                            if (amount == 0)
                            {
                                client.Player.Guild.SetGuildDues(false);
                                client.Player.Guild.SetGuildDuesMaxPercent(0);
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.DuesOff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            else if (amount > 0 && amount <= 100)
                            {
                                long max = Properties.GUILD_NEW_DUES_SYSTEM ? long.Min(Properties.GUILD_DUES_MAX_VALUE, client.Player.Guild.GuildLevel * 5) : Properties.GUILD_DUES_MAX_VALUE;
                                if (amount <= max || client.Account.PrivLevel > 1)
                                {
                                    client.Player.Guild.SetGuildDues(true);
                                    client.Player.Guild.SetGuildDuesMaxPercent(amount);
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.DuesOn", amount), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                else
                                {
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.DuesMax", amount), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                            }
                            else
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDues"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Deposit
                    case "deposit":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }

                            double amount = double.Parse(args[2]);
                            if (amount < 0 || amount > 1000000001)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.DepositInvalid"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else if (client.Player.CopperBalance < amount)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.DepositTooMuch"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Player.Guild.SetGuildBank(client.Player, amount);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Withdraw
                    case "withdraw":
                        {
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Withdraw))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            double amount = double.Parse(args[2]);
                            if (amount < 0 || amount > 1000000001)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.WithdrawInvalid"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                if (client.Player.Guild.WithdrawGuildBank(client.Player, amount))
                                {
                                    client.Player.SendTranslatedMessage("Commands.Players.Guild.Withdrawamount", eChatType.CT_Guild, eChatLoc.CL_SystemWindow, Money.GetString(long.Parse(amount.ToString())));
                                    client.Player.Guild.UpdateGuildWindow();
                                    return;
                                }
                                else
                                {
                                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Player.Client, "Commands.Players.Guild.WithdrawTooMuch"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                            }
                            client.Player.Guild.UpdateGuildWindow();
                        }
                        break;
                    #endregion
                    #region Logins
                    case "logins":
                        {
                            client.Player.ShowGuildLogins = !client.Player.ShowGuildLogins;

                            if (client.Player.ShowGuildLogins)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.LoginsOn"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.LoginsOff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            break;
                        }
                    #endregion

                    #region territories
                    case "territories":
                        IList<string> infos = TerritoryManager.Instance.GetTerritoriesInformations(client.Player);
                        client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Territories.WindowTitle"), infos);
                        break;
                    #endregion

                    #region subterritories
                    case "subterritories":
                        infos = TerritoryManager.Instance.GetSubterritoriesInformations(client.Player);
                        client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Subterritories.WindowTitle"), infos);
                        break;
                    #endregion


                    #region Default
                    default:
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.UnknownCommand", args[1]), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            DisplayHelp(client);
                        }
                        break;
                        #endregion
                }
            }
            catch (Exception e)
            {
                log.Error("Error in /gc script, " + args[1] + " command: " + e.ToString());

                DisplayHelp(client);
            }
        }

        private const string GUILD_BANNER_PRICE = "GUILD_BANNER_PRICE";

        protected void ConfirmBannerBuy(GamePlayer player, byte response)
        {
            if (response != 0x01)
                return;

            if (player.Guild.HasGuildBanner)
                return;

            long bannerPrice = 0;
            if (player.Client.Account.PrivLevel <= (int)ePrivLevel.Player)
            {

                bannerPrice = player.TempProperties.getProperty<long>(GUILD_BANNER_PRICE, 0);
                player.TempProperties.removeProperty(GUILD_BANNER_PRICE);

                if (player.Guild.MeritPoints < bannerPrice)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerNotAfford"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                player.Guild.RemoveMeritPoints(bannerPrice);
            }
            player.Guild.HasGuildBanner = true;
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerBought", bannerPrice), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

        }

        private const string GUILD_BUFF_TYPE = "GUILD_BUFF_TYPE";

        protected void ConfirmBuffBuy(GamePlayer player, byte response)
        {
            if (response != 0x01)
                return;

            if (player == null || player.Guild == null)
                return;

            int guildLevel = (int)player.Guild.GuildLevel;
            int meritPointCost = 1000;

            if (guildLevel >= 8 && guildLevel <= 15)
            {
                meritPointCost = 2000;
            }
            else if (guildLevel >= 16)
            {
                meritPointCost = 3000;
            }

            Guild.eBonusType buffType = player.TempProperties.getProperty<Guild.eBonusType>(GUILD_BUFF_TYPE, Guild.eBonusType.None);
            player.TempProperties.removeProperty(GUILD_BUFF_TYPE);

            if (buffType == Guild.eBonusType.None || player.Guild.MeritPoints < meritPointCost || player.Guild.BonusType != Guild.eBonusType.None)
                return;

            player.Guild.BonusType = buffType;
            player.Guild.RemoveMeritPoints(meritPointCost);
            player.Guild.BonusStartTime = DateTime.Now;

            string buffName = Guild.BonusTypeToName(buffType);

            foreach (GamePlayer ply in player.Guild.GetListOfOnlineMembers())
            {
                ply.Out.SendMessage(LanguageMgr.GetTranslation(ply.Client.Account.Language, "Commands.Players.Guild.BuffActivated", player.Name, buffName, meritPointCost), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            }
            player.Guild.UpdateGuildWindow();
        }


        /// <summary>
        /// method to handle the aliance invite
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reponse"></param>
        protected void AllianceInvite(GamePlayer player, byte reponse)
        {
            if (reponse != 0x01)
                return; //declined

            GamePlayer inviter = player.TempProperties.getProperty<object>("allianceinvite", null) as GamePlayer;

            if (player.Guild == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.Alliance.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (inviter == null || inviter.Guild == null)
            {
                return;
            }

            if (!player.Guild.HasRank(player, Guild.eRank.Alli))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            player.TempProperties.removeProperty("allianceinvite");

            if (inviter.Guild.alliance == null)
            {
                //create alliance
                Alliance alli = new Alliance();
                DBAlliance dballi = new DBAlliance();
                dballi.AllianceName = inviter.Guild.Name;
                dballi.LeaderGuildID = inviter.GuildID;
                dballi.DBguildleader = null;
                dballi.Motd = "";
                alli.Dballiance = dballi;
                alli.Guilds.Add(inviter.Guild);
                inviter.Guild.alliance = alli;
                inviter.Guild.AllianceId = inviter.Guild.alliance.Dballiance.ObjectId;
            }
            inviter.Guild.alliance.AddGuild(player.Guild);
            inviter.Guild.alliance.SaveIntoDatabase();
            player.Guild.UpdateGuildWindow();
            inviter.Guild.UpdateGuildWindow();
        }

        /// <summary>
        /// method to handle the emblem change
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reponse"></param>
        public static void EmblemChange(GamePlayer player, byte reponse)
        {
            if (reponse != 0x01)
                return;
            if (player.TargetObject is EmblemNPC == false)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.EmblemNeedNPC"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (player.CopperBalance < GuildMgr.COST_RE_EMBLEM) //200 gold to re-emblem
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.EmblemNeedGold"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            player.Out.SendEmblemDialogue();
            player.Guild.UpdateGuildWindow();
        }

        public void DisplayHelp(GameClient client)
        {
            if (client.Account.PrivLevel > 1)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMCommands"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMCreate"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMPurge"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRename"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMAddPlayer"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRemovePlayer"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMRealmPoints"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMMeritPoints"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildGMBountyPoints"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            }
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildUsage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildForm"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildInfo"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildRanks"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildCancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDecline"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildClaim"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildQuit"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildMotd"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAMotd"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildOMotd"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildPromote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDemote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildRemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildRemAccount"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEmblem"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEdit"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildLeader"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAccept"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildWho"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildList"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAAccept"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildACancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildADecline"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildAInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildARemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildARemoveAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildNoteSelf"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDues"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildDeposit"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildWithdraw"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildWebpage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEmail"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildBuff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildBuyBanner"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildBannerSummon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildBannerUnsummon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildTerritories"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildSubTerritories"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.TerritoryBanner"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.TerritoryPortal"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.BuyTerritoryDefender"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.MoveTerritoryDefender"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.CombatZone"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.JailRelease"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// method to handle commands for /gc edit
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public int GCEditCommand(GameClient client, string[] args)
        {
            if (args.Length < 4)
            {
                DisplayEditHelp(client);
                return 0;
            }

            bool reponse = true;
            int vault = 0;
            if (args.Length > 4)
            {
                if (args[3] == "vault")
                {
                    if (args.Length < 7 || !Int32.TryParse(args[4], out vault) || vault <= 0 || vault > GuildVault.NUM_VAULTS)
                    {
                        DisplayEditHelp(client);
                        return 1;
                    }
                    if (args[6].StartsWith('y'))
                        reponse = true;
                    else if (args[6].StartsWith('n'))
                        reponse = false;
                }
                else
                {
                    if (args[4].StartsWith("y"))
                        reponse = true;
                    else if (args[4].StartsWith("n"))
                        reponse = false;
                    else if (args[3] is not ("title" or "ranklevel"))
                    {
                        DisplayEditHelp(client);
                        return 1;
                    }
                }
            }
            string status = reponse ? "Commands.Players.Guild.SetEnabled" : "Commands.Players.Guild.SetDisabled";
            byte number;
            try
            {
                number = Convert.ToByte(args[2]);
                if (number > 9 || number < 0)
                    return 0;
            }
            catch
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.ThirdArgNotNum"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return 0;
            }

            if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.NoPrivileges"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return 1;
            }

            switch (args[3])
            {
                case "title":
                    {
                        string message = String.Join(" ", args, 4, args.Length - 4);
                        client.Player.Guild.GetRankByID(number).Title = message;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankTitleSet", number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                        client.Player.Guild.UpdateGuildWindow();
                    }
                    break;
                case "ranklevel":
                    {
                        if (args.Length >= 5)
                        {
                            byte lvl = Convert.ToByte(args[4]);
                            client.Player.Guild.GetRankByID(number).RankLevel = lvl;
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankLevelSet", lvl.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            DisplayEditHelp(client);
                        }
                    }
                    break;

                case "emblem":
                    {
                        client.Player.Guild.GetRankByID(number).Emblem = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankEmblemSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "gchear":
                    {
                        client.Player.Guild.GetRankByID(number).GcHear = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankGCHearSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "gcspeak":
                    {
                        client.Player.Guild.GetRankByID(number).GcSpeak = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankGCSpeakSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "ochear":
                    {
                        client.Player.Guild.GetRankByID(number).OcHear = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankOCHearSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "ocspeak":
                    {
                        client.Player.Guild.GetRankByID(number).OcSpeak = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankOCSpeakSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "achear":
                    {
                        client.Player.Guild.GetRankByID(number).AcHear = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankACHearSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "acspeak":
                    {
                        client.Player.Guild.GetRankByID(number).AcSpeak = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankACSpeakSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "invite":
                    {
                        client.Player.Guild.GetRankByID(number).Invite = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankInviteSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "promote":
                    {
                        client.Player.Guild.GetRankByID(number).Promote = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankPromoteSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "remove":
                    {
                        client.Player.Guild.GetRankByID(number).Remove = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankRemoveSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "alli":
                    {
                        client.Player.Guild.GetRankByID(number).Alli = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankAlliSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "view":
                    {
                        client.Player.Guild.GetRankByID(number).View = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankViewSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "buff":
                    {
                        client.Player.Guild.GetRankByID(number).Buff = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankBuffSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "claim":
                    {
                        client.Player.Guild.GetRankByID(number).Claim = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankClaimSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "upgrade":
                    {
                        client.Player.Guild.GetRankByID(number).Upgrade = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankUpgradeSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "release":
                    {
                        client.Player.Guild.GetRankByID(number).Release = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankReleaseSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "dues":
                    {
                        client.Player.Guild.GetRankByID(number).Release = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankDuesSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "withdraw":
                    {
                        client.Player.Guild.GetRankByID(number).Release = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankWithdrawSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "buybanner":
                    {
                        client.Player.Guild.GetRankByID(number).BuyBanner = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankBuyBannerSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "summon":
                    {
                        client.Player.Guild.GetRankByID(number).Summon = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankSummonSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "territorydefenders":
                    {
                        client.Player.Guild.GetRankByID(number).TerritoryDefenders = reponse;
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankTerritoryDefendersSet", LanguageMgr.GetTranslation(client.Account.Language, status), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case "vault":
                    {
                        if (vault is < 1 or > 3)
                        {
                            DisplayEditHelp(client);
                            return 1;
                        }
                        switch (args[5])
                        {
                            case "view":
                                {
                                    client.Player.Guild.GetRankByID(number).SetViewVault(vault - 1, reponse);
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankVaultViewSet", LanguageMgr.GetTranslation(client.Account.Language, status), vault, number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                break;
                            case "deposit":
                                {
                                    client.Player.Guild.GetRankByID(number).SetDepositInVault(vault - 1, reponse);
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankVaultDepositSet", LanguageMgr.GetTranslation(client.Account.Language, status), vault, number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                break;
                            case "withdraw":
                                {
                                    client.Player.Guild.GetRankByID(number).SetWithdrawFromVault(vault - 1, reponse);
                                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.RankVaultWithdrawSet", LanguageMgr.GetTranslation(client.Account.Language, status), vault, number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                }
                                break;
                            default:
                                {
                                    DisplayEditHelp(client);
                                    return 0;
                                }
                        }
                    }
                    break;
                default:
                    {
                        DisplayEditHelp(client);
                        return 0;
                    }
            } //switch
            DBRank rank = client.Player.Guild.GetRankByID(number);
            if (rank != null)
                GameServer.Database.SaveObject(rank);
            return 1;
        }

        /// <summary>
        /// Send social window data to the client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sort"></param>
        /// <param name="page"></param>
        /// <param name="offline">0 = false, 1 = true, 2 to try and recall last setting used by player</param>
        private void SendSocialWindowData(GameClient client, int sort, int page, byte offline)
        {
            Dictionary<string, GuildMgr.GuildMemberDisplay> allGuildMembers = GuildMgr.GetAllGuildMembers(client.Player.GuildID);

            if (allGuildMembers == null || allGuildMembers.Count == 0)
            {
                return;
            }

            bool showOffline = false;

            if (offline < 2)
            {
                showOffline = (offline == 0 ? false : true);
            }
            else
            {
                // try to recall last setting
                showOffline = client.Player.TempProperties.getProperty<bool>("SOCIALSHOWOFFLINE", false);
            }

            client.Player.TempProperties.setProperty("SOCIALSHOWOFFLINE", showOffline);

            //The type of sorting we will be sending
            GuildMgr.GuildMemberDisplay.eSocialWindowSort sortOrder = (GuildMgr.GuildMemberDisplay.eSocialWindowSort)sort;

            //Let's sort the sorted list - we don't need to sort if sort = name
            SortedList<string, GuildMgr.GuildMemberDisplay> sortedWindowList = null;

            GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Name;

            #region Determine Sort
            switch (sortOrder)
            {
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ClassAsc:
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ClassDesc:
                    sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.ClassID;
                    break;
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.GroupAsc:
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.GroupDesc:
                    sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Group;
                    break;
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.LevelAsc:
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.LevelDesc:
                    sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Level;
                    break;
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.NoteAsc:
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.NoteDesc:
                    sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Note;
                    break;
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.RankAsc:
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.RankDesc:
                    sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Rank;
                    break;
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ZoneOrOnlineAsc:
                case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ZoneOrOnlineDesc:
                    sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.ZoneOrOnline;
                    break;
            }
            #endregion

            if (showOffline == false) // show only a sorted list of online players
            {
                IList<GamePlayer> onlineGuildPlayers = client.Player.Guild.GetListOfOnlineMembers();
                sortedWindowList = new SortedList<string, GuildMgr.GuildMemberDisplay>(onlineGuildPlayers.Count);

                foreach (GamePlayer player in onlineGuildPlayers)
                {
                    if (allGuildMembers.ContainsKey(player.InternalID))
                    {
                        GuildMgr.GuildMemberDisplay memberDisplay = allGuildMembers[player.InternalID];
                        memberDisplay.UpdateMember(player);
                        string key = memberDisplay[sortColumn];

                        if (sortedWindowList.ContainsKey(key))
                            key += sortedWindowList.Count.ToString();

                        sortedWindowList.Add(key, memberDisplay);
                    }
                }
            }
            else // sort and display entire list
            {
                sortedWindowList = new SortedList<string, GuildMgr.GuildMemberDisplay>();
                int keyIncrement = 0;

                foreach (GuildMgr.GuildMemberDisplay memberDisplay in allGuildMembers.Values)
                {
                    GamePlayer p = client.Player.Guild.GetOnlineMemberByID(memberDisplay.InternalID);
                    if (p != null)
                    {
                        //Update to make sure we have the most up to date info
                        memberDisplay.UpdateMember(p);
                    }
                    else
                    {
                        //Make sure that since they are offline they get the offline flag!
                        memberDisplay.GroupSize = "0";
                    }
                    //Add based on the new index
                    string key = memberDisplay[sortColumn];

                    if (sortedWindowList.ContainsKey(key))
                    {
                        key += keyIncrement++;
                    }

                    try
                    {
                        sortedWindowList.Add(key, memberDisplay);
                    }
                    catch
                    {
                        if (log.IsErrorEnabled)
                            log.Error(string.Format("Sorted List duplicate entry - Key: {0} Member: {1}. Replacing - Member: {2}.  Sorted count: {3}.  Guild ID: {4}", key, memberDisplay.Name, sortedWindowList[key].Name, sortedWindowList.Count, client.Player.GuildID));
                    }
                }
            }

            //Finally lets send the list we made

            IList<GuildMgr.GuildMemberDisplay> finalList = sortedWindowList.Values;

            int i = 0;
            string[] buffer = new string[10];
            for (i = 0; i < 10 && finalList.Count > i + (page - 1) * 10; i++)
            {
                GuildMgr.GuildMemberDisplay memberDisplay;

                if ((int)sortOrder > 0)
                {
                    //They want it normal
                    memberDisplay = finalList[i + (page - 1) * 10];
                }
                else
                {
                    //They want it in reverse
                    memberDisplay = finalList[(finalList.Count - 1) - (i + (page - 1) * 10)];
                }

                buffer[i] = memberDisplay.ToString((i + 1) + (page - 1) * 10, finalList.Count);
            }

            client.Out.SendMessage("TE," + page.ToString() + "," + finalList.Count + "," + i.ToString(), eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

            foreach (string member in buffer)
                client.Player.Out.SendMessage(member, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

        }

        public void DisplayEditHelp(GameClient client)
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildUsage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditTitle"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditRankLevel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditEmblem"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditGCHear"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditGCSpeak"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditOCHear"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditOCSpeak"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditACHear"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditACSpeak"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditPromote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditRemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditView"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditClaim"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditUpgrade"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditRelease"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditDues"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildTerritories"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildSubTerritories"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.TerritoryBanner"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.TerritoryPortal"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.CombatZone"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage("/gc edit <ranknum> buff <y/n>", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditWithdraw"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditVault"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditBuyBanner"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditSummon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Guild.Help.GuildEditTerritoryDefenders"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
        }
    }
}