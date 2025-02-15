using AmteScripts.Managers;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Spells;
using System;

namespace DOL.GS.Scripts
{
    public class TeleporterPvP : GameNPC
    {
        private bool _isBusy;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            SetOwnBrain(new BlankBrain());
            return true;
        }

        /// <summary>
        /// Old helper from your script
        /// </summary>
        private bool _BaseSay(GamePlayer player, string str = "Partir")
        {
            if (_isBusy)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterPvP.Busy"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            TurnTo(player);

            if (SpellHandler.FindEffectOnTarget(player, "Damnation") != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterRvR.DamnationRefusal1", player.Name),
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterRvR.DamnationRefusal2"),
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            // If PvP is closed or level < 20, only accept certain keywords
            if (!PvpManager.Instance.IsOpen || player.Level < 20)
            {
                if (!string.Equals(str, "Partir", StringComparison.OrdinalIgnoreCase))
                {
                    // Show the "cannot help you" messages
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language,
                            "TeleporterPvP.CannotHelpPart1", player.Name),
                        eChatType.CT_System, eChatLoc.CL_PopupWindow);

                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language,
                            "TeleporterPvP.CannotHelpPart2"),
                        eChatType.CT_System, eChatLoc.CL_PopupWindow);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The player clicks on the NPC
        /// </summary>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (_BaseSay(player, ""))
                return true; // stops if teleporter is busy or player is disqualified

            if (player.Group != null && PvpManager.Instance.IsPlayerInQueue(player))
            {
                PlayerJoinedGroup(player);
                return true;
            }

            if (player.IsInPvP)
            {
                // The old "chicken out" approach
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterPvP.ChickenOutPart1"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterPvP.ChickenOutPart2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

                // Show bracket to leave PvP
                string msgInPvp = LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterPvP.AlreadyInPvp", player.Name)
                    + "\n\n[Leave PvP]";
                player.Out.SendMessage(msgInPvp, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            else
            {
                // Player is not in PvP => show advanced menu
                var session = PvpManager.Instance.CurrentSession;
                if (!PvpManager.Instance.IsOpen || session == null)
                {
                    // No session open
                    player.Out.SendMessage("PvP is not currently open; no session is active.",
                        eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }

                // Build the dynamic text based on GroupCompoOption
                // 1 = solo only, 2 = group only, 3 = both
                string msg = LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterPvP.SendToCombat", player.Name) + "\n";

                if (session.GroupCompoOption == 1 || session.GroupCompoOption == 3)
                {
                    msg += "\n[Teleport as Solo]";
                }

                if (session.GroupCompoOption == 2 || session.GroupCompoOption == 3)
                {
                    msg += "\n[Teleport your Group]";
                }

                // If groupCompoOption != 1 => we also have a queue for group-based sessions
                if (session.GroupCompoOption == 2 || session.GroupCompoOption == 3)
                {
                    if (player.Group == null)
                    {
                        bool isInQueue = PvpManager.Instance.IsPlayerInQueue(player);
                        if (!isInQueue)
                            msg += "\n[Join Waiting Queue]";
                        else
                            msg += "\n[Leave Waiting Queue]";
                    }
                }

                player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            return true;
        }

        public void PlayerJoinedGroup(GamePlayer player)
        {
            if (PvpManager.Instance.IsPlayerInQueue(player))
            {
                PvpManager.Instance.DequeueSolo(player);
            }
        }

        /// <summary>
        /// This is critical for clickable bracketed text to work.
        /// When the player clicks [XYZ], the client sends a whisper "/whisper NPCName XYZ".
        /// We'll parse that whisper here and forward to our OnChoiceClicked.
        /// </summary>
        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            if (!(source is GamePlayer player))
                return false;

            if (_BaseSay(player, str))
                return true;

            OnChoiceClicked(player, str);
            return true;
        }

        /// <summary>
        /// Interprets the bracket text or whisper string
        /// </summary>
        public void OnChoiceClicked(GamePlayer player, string choice)
        {
            if (_BaseSay(player, choice))
                return;

            switch (choice)
            {
                case "Teleport as Solo":
                    _HandleSolo(player);
                    break;

                case "Teleport your Group":
                    _HandleGroup(player);
                    break;

                case "Join Waiting Queue":
                    _HandleQueue(player);
                    break;

                case "Leave Waiting Queue":
                    _HandleLeaveQueue(player);
                    break;

                case "Leave PvP":
                    if (player.IsInPvP)
                        PvpManager.Instance.RemovePlayer(player);
                    break;

                case "Partir":
                    _HandleSolo(player);
                    break;

                default:
                    player.Out.SendMessage("Invalid choice: " + choice,
                        eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
            }
        }

        /// <summary>
        /// If the session allows solo, do a 2-sec cast and then call AddPlayer.
        /// </summary>
        private void _HandleSolo(GamePlayer player)
        {
            var session = PvpManager.Instance.CurrentSession;
            if (session == null || !PvpManager.Instance.IsOpen)
            {
                player.Out.SendMessage("No current PvP session is active!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            // If the session is group-only (2), forbid
            if (session.GroupCompoOption == 2)
            {
                player.Out.SendMessage("This PvP session only allows group entry.",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            _TeleportSolo(player);
        }

        /// <summary>
        /// If the session allows group, do a 2-sec cast
        /// and then call AddGroup. Must be group leader, etc.
        /// But we also show the cast animation to everyone in the group (in the same zone).
        /// </summary>
        private void _HandleGroup(GamePlayer player)
        {
            var session = PvpManager.Instance.CurrentSession;
            if (session == null || !PvpManager.Instance.IsOpen)
            {
                player.Out.SendMessage("No current PvP session is active!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            if (session.GroupCompoOption == 1)
            {
                player.Out.SendMessage("This session is solo-only, cannot bring a group!",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            if (player.Group == null)
            {
                player.Out.SendMessage("You are not in a group!",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }
            if (player.Group.Leader != player)
            {
                player.Out.SendMessage("You must be the group leader to do that!",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            _TeleportGroup(player);
        }

        /// <summary>
        /// The user wants to join the "solo queue"
        /// for group-based sessions (2 or 3).
        /// </summary>
        private void _HandleQueue(GamePlayer player)
        {
            var session = PvpManager.Instance.CurrentSession;
            if (session == null || !PvpManager.Instance.IsOpen)
            {
                player.Out.SendMessage("No current PvP session is active!",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            if (session.GroupCompoOption == 1)
            {
                player.Out.SendMessage("This session is solo-only, there's no group queue to join!",
                    eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            PvpManager.Instance.EnqueueSolo(player);
        }

        private void _HandleLeaveQueue(GamePlayer player)
        {
            PvpManager.Instance.DequeueSolo(player);
        }

        /// <summary>
        /// The 2-second cast approach for a solo player.
        /// We do an animation in a 2s timer. After that,
        /// we check if they are group only or not. If not => AddPlayer.
        /// </summary>
        private void _TeleportSolo(GamePlayer player)
        {
            _isBusy = true;
            var timer = new RegionTimer(this, TeleportSoloCallback);
            timer.Properties.setProperty("player", player);
            timer.Start(2000);

            // show cast animation to *nearby* players
            foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                plr.Out.SendSpellCastAnimation(this, 1, 20);
                plr.Out.SendEmoteAnimation(player, eEmote.Bind);
            }
        }

        private int TeleportSoloCallback(RegionTimer timer)
        {
            _isBusy = false;
            var player = timer.Properties.getProperty<GamePlayer>("player", null);
            if (player == null) return 0;

            if (player.InCombat)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "TeleporterPvP.InCombat"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return 0;
            }

            var session = PvpManager.Instance.CurrentSession;
            if (session == null || !PvpManager.Instance.IsOpen)
            {
                // remove them if was inside
                if (player.IsInPvP)
                    PvpManager.Instance.RemovePlayer(player);
                return 0;
            }

            // If group only => forbid
            if (session.GroupCompoOption == 2)
            {
                if (player.Group != null && player.Group.Leader == player)
                    PvpManager.Instance.AddGroup(player);
                else
                    player.Out.SendMessage("You are not the group leader or have no group. Cannot join group-based session!",
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                // otherwise treat it as a solo join or leaving scenario
                if (player.IsInPvP)
                    PvpManager.Instance.RemovePlayer(player);
                else
                    PvpManager.Instance.AddPlayer(player);
            }
            return 0;
        }

        /// <summary>
        /// We do a single 2s cast for the entire group. That means each group member
        /// in the zone sees a cast animation. After 2s, we call AddGroup(leader).
        /// </summary>
        private void _TeleportGroup(GamePlayer leader)
        {
            _isBusy = true;

            // Show cast animation to each group member in range
            var group = leader.Group;
            if (group != null)
            {
                foreach (var member in group.GetPlayersInTheGroup())
                {
                    // if you only want to do the effect if they are in the same zone or near leader, do so:
                    if (member.CurrentZone == leader.CurrentZone)
                    {
                        foreach (GamePlayer plr in member.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            plr.Out.SendSpellCastAnimation(this, 1, 20);
                            plr.Out.SendEmoteAnimation(member, eEmote.Bind);
                        }
                    }
                }
            }

            // Start a single timer for 2s => calls TeleportGroupCallback
            RegionTimer TimerTL = new RegionTimer(this, TeleportGroupCallback);
            TimerTL.Properties.setProperty("leader", leader);
            TimerTL.Start(2000);
        }

        private int TeleportGroupCallback(RegionTimer timer)
        {
            _isBusy = false;
            GamePlayer leader = timer.Properties.getProperty<GamePlayer>("leader", null);
            if (leader == null) return 0;

            if (leader.InCombat)
            {
                leader.Out.SendMessage(LanguageMgr.GetTranslation(leader.Client.Account.Language,
                    "TeleporterPvP.InCombat"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return 0;
            }

            var session = PvpManager.Instance.CurrentSession;
            if (session == null || !PvpManager.Instance.IsOpen)
            {
                // remove if was inside
                if (leader.IsInPvP)
                    PvpManager.Instance.RemovePlayer(leader);
                return 0;
            }

            // We expect group-only or group-allowed
            if (session.GroupCompoOption == 2 || session.GroupCompoOption == 3)
            {
                if (leader.Group != null && leader.Group.Leader == leader)
                {
                    PvpManager.Instance.AddGroup(leader);
                }
                else
                {
                    leader.Out.SendMessage("You are not the group leader or have no group. Cannot join group-based session!",
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
            else
            {
                // fallback => solo or leaving
                if (leader.IsInPvP)
                    PvpManager.Instance.RemovePlayer(leader);
                else
                    PvpManager.Instance.AddPlayer(leader);
            }

            return 0;
        }
    }
}