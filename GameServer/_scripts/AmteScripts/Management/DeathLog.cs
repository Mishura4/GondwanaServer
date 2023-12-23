using System;
using System.Linq;
using System.Reflection;
using AmteScripts.Managers;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.Language;
using GameServerScripts.Amtescripts.Managers;
using log4net;


namespace DOL.GS.GameEvents
{
    public static class DeathLog
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(LivingKillEnnemy));

            log.Info("DeathLog initialized");
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(LivingKillEnnemy));
        }


        public static void LivingKillEnnemy(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not DyingEventArgs { Killer: not null } dyingArgs)
                return;
            var killer = dyingArgs.Killer;
            if (sender is GamePlayer playerVictim)
            {
                if (killer is GamePlayer playerKiller)
                {
                    if (playerVictim.Reputation < 0)
                    {
                        // Ignore kills on Outlaws
                        return;
                    }
                    if (playerKiller.Client.Account.PrivLevel > 1)
                    {
                        // If killer is GM, let go
                        return;
                    }

                    if (GameServer.ServerRules.IsInPvPArea(playerVictim))
                    {
                        // PvP area
                        return;
                    }

                    if (DeathCheck.Instance.IsChainKiller(playerKiller, playerVictim))
                    {
                        // Automatically report player
                        --playerKiller.Reputation;
                        if (!playerKiller.Wanted)
                        {
                            playerKiller.Wanted = true;
                        }
                        GameServer.Database.AddObject(new DBDeathLog(playerVictim, playerKiller, true));
                        playerKiller.Out.SendMessage(LanguageMgr.GetTranslation(playerKiller.Client, "GameObjects.GamePlayer.Multiplekills", playerKiller.GetPersonalizedName(playerVictim)), PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        GameServer.Database.AddObject(new DBDeathLog(playerVictim, playerKiller, false));
                    }
                }
            }
            else if (sender is GuardNPC)
            {
                if (killer is GamePlayer playerKiller)
                {
                    if (!GameServer.ServerRules.IsInPvPArea(playerKiller))
                    {
                        if (playerKiller.Group != null)
                        {
                            foreach (GamePlayer groupPlayer in playerKiller.Group.GetMembersInTheGroup())
                            {
                                // I've copy pasted this from the code that was there before, but this seems grief-prone
                                GuardKillLostReputation(groupPlayer);
                            }
                        }
                        else
                        {
                            GuardKillLostReputation(playerKiller);
                        }
                    }
                }
            }
        }

        private static void GuardKillLostReputation(GamePlayer player)
        {
            if (player.Client.Account.PrivLevel <= 1)
            {
                player.Reputation--;
                player.SaveIntoDatabase();
                player.Out.SendMessage("Vous avez perdu 1 points de réputation pour cause d'assassinat de garde", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
