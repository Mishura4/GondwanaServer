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
            var dyingArgs = args as DyingEventArgs;
            if (dyingArgs != null)
            {
                var killer = dyingArgs.Killer;
                var killed = sender as GamePlayer;
                var killerPlayer = killer as GamePlayer;
                //Player isWanted when Killed by Guard
                if (killed != null && killerPlayer != null)
                {
                    //If killer is GM, let go
                    if (killerPlayer != null && killerPlayer.Client.Account.PrivLevel > 1)
                    {
                        return;
                    }

                    if (killed.Reputation < 0)
                    {
                        return;
                    }

                    if (IsKillAllowedArea(killed))
                    {
                        return;
                    }

                    bool isLegitimeKiller = killer is GuardNPC || killerPlayer != null;
                    //Log interplayer kills & Killed by Guard
                    //Dot not log killed by npcs
                    if (isLegitimeKiller)
                    {
                        if (killerPlayer != null)
                        {
                            if (DeathCheck.Instance.IsChainKiller(killerPlayer, killed))
                            {
                                killerPlayer.Reputation -= 1;
                                if (killerPlayer.Reputation == 1)
                                    killerPlayer.Reputation -= 1;
                                killerPlayer.SaveIntoDatabase();
                                killerPlayer.Out.SendMessage("Vous avez perdu 1 point de réputation pour cause d'assassinats multiples.", PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                                string newsMessage = LanguageMgr.GetTranslation(killerPlayer.Client, "GameObjects.GamePlayer.Wanted", killer.Name);
                                NewsMgr.CreateNews("GameObjects.GamePlayer.Wanted", killerPlayer.Realm, eNewsType.RvRGlobal, false, true, killerPlayer.Name);
                                if (DOL.GS.ServerProperties.Properties.DISCORD_ACTIVE)
                                {
                                    DolWebHook hook = new DolWebHook(DOL.GS.ServerProperties.Properties.DISCORD_WEBHOOK_ID);
                                    hook.SendMessage(newsMessage);
                                }
                            }
                        }
                        bool isWanted = killerPlayer.Reputation < 0;
                        //Log Death
                        GameServer.Database.AddObject(new DBDeathLog((GameObject)sender, killer, isWanted));
                    }
                }
                else
                {
                    //Check if Guard was killed
                    if (sender is GuardNPC && killerPlayer != null && !IsKillAllowedArea(killerPlayer))
                    {
                        if (killerPlayer.Group != null)
                        {
                            foreach (GamePlayer player in killerPlayer.Group.GetMembersInTheGroup())
                            {
                                GuardKillLostReputation(player);
                            }
                        }
                        else
                        {
                            GuardKillLostReputation(killerPlayer);
                        }
                    }
                }
            }
        }

        private static void GuardKillLostReputation(GamePlayer player)
        {
            if (player.Client.Account.PrivLevel == 1)
            {
                player.Reputation--;
                player.SaveIntoDatabase();
                player.Out.SendMessage("Vous avez perdu 1 points de réputation pour cause d'assassinat de garde", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        /// <summary>
        /// Is kill allowed ? Allow kills in PvP zones etc..
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private static bool IsKillAllowedArea(GamePlayer player)
        {
            if (player.isInBG ||
                player.CurrentRegion.IsRvR ||
                PvpManager.Instance.IsPvPRegion(player.CurrentRegion.ID) ||
                player.CurrentAreas.Any(a => a.IsPvP) ||
                player.CurrentZone.AllowReputation)
            {
                return true;
            }

            return false;
        }
    }
}
