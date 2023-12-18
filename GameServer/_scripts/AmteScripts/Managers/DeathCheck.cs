using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServerScripts.Amtescripts.Managers
{
    public class DeathCheck
    {
        private static DeathCheck instance;

        public static DeathCheck Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }
                instance = new DeathCheck();
                return instance;
            }
        }
        private DeathCheck() { }

        public int ReportPlayer(GamePlayer victim)
        {
            IList<DBDeathLog> deaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KilledId").IsEqualTo(victim.InternalID).And(DB.Column("DeathDate").IsGreatherThan("SUBTIME(NOW(), '3:0:0')").And(DB.Column("WasPunished").IsEqualTo(0))));

            if (deaths == null || deaths.Count == 0)
            {
                victim.Out.SendMessage(LanguageMgr.GetTranslation(victim.Client.Account.Language, "GuardNPC.Report.Toolate"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return 0;
            }

            int reported = 0;

            foreach (var death in deaths.OrderByDescending(d => d.DeathDate))
            {
                if (!death.IsReported)
                {
                    death.IsReported = true;
                    GameClient killerClient = WorldMgr.GetClientByPlayerID(death.KillerId, true, true);
                    if (killerClient != null) // killer is online
                    {
                        --killerClient.Player.Reputation;
                        killerClient.Out.SendMessage(LanguageMgr.GetTranslation(killerClient.Account.Language, "GameObjects.GamePlayer.Murder", killerClient.Player.GetPersonalizedName(victim)), DOL.GS.PacketHandler.eChatType.CT_System, DOL.GS.PacketHandler.eChatLoc.CL_SystemWindow);
                        if (!killerClient.Player.Wanted)
                        {
                            killerClient.Player.Wanted = true;
                        }
                        ++reported;
                    }
                    else // killer is offline
                    {
                        DOLCharacters killer = GameServer.Database.FindObjectByKey<DOLCharacters>(death.KillerId);
                        if (!killer.IsWanted)
                        {
                            ++reported;
                            --killer.Reputation;
                            killer.IsWanted = true;
                            NewsMgr.CreateNews("GameObjects.GamePlayer.Wanted", victim.Realm, eNewsType.RvRGlobal, false, true, killer.Name);
                            if (DOL.GS.ServerProperties.Properties.DISCORD_ACTIVE)
                            {
                                DolWebHook hook = new DolWebHook(DOL.GS.ServerProperties.Properties.DISCORD_WEBHOOK_ID);
                                hook.SendMessage(LanguageMgr.GetTranslation(DOL.GS.ServerProperties.Properties.SERV_LANGUAGE, "GameObjects.GamePlayer.Wanted", death.KillerId));
                            }
                        }
                        GameServer.Database.SaveObject(killer);
                    }
                    GameServer.Database.SaveObject(death);
                }
            }
            if (reported == 0 && deaths.Count > 0)
            {
                victim.Out.SendMessage(LanguageMgr.GetTranslation(victim.Client.Account.Language, "GuardNPC.Report.AlreadyReported"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return 0;
            }
            return reported;
        }

        public bool IsChainKiller(GamePlayer killer, GamePlayer killed)
        {
            if (DOL.GS.ServerProperties.Properties.REPUTATION_CHAIN_KILL_COUNT < 1)
            {
                return false;
            }

            var deaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KillerId").IsEqualTo(killer.InternalID)
                .And(DB.Column("KilledId").IsEqualTo(killed.InternalID)
                .And(DB.Column("DeathDate").IsGreatherThan("SUBTIME(NOW(), '0:20:0')"))));

            return deaths != null && deaths.Count >= DOL.GS.ServerProperties.Properties.REPUTATION_CHAIN_KILL_COUNT;
        }
    }
}