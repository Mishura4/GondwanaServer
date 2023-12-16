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
            IList<DBDeathLog> deaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KilledId").IsEqualTo(victim.InternalID).And(DB.Column("DeathDate").IsGreatherThan("SUBTIME(NOW(), '3:0:0')").And(DB.Column("ExitFromJail").IsEqualTo(0))));

            if (deaths == null || !deaths.Any())
            {
                victim.Out.SendMessage(LanguageMgr.GetTranslation(victim.Client.Account.Language, "GuardNPC.Report.Toolate"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return 0;
            }

            IEnumerable<DBDeathLog> orderedDeaths = deaths.OrderByDescending(d => d.DeathDate);

            List<DOLCharacters> reported = new List<DOLCharacters>();

            foreach (var death in orderedDeaths)
            {
                if (!death.IsWanted) // not reported yet
                {
                    GameClient killerClient = WorldMgr.GetClientByPlayerID(death.KillerId, true, true);
                    DOLCharacters killer = killerClient.Player.DBCharacter ?? GameServer.Database.FindObjectByKey<DOLCharacters>(death.KillerId);
                    string newsMessage = "";
                    // TODO: hidden name?
                    if (killer != null)
                    {
                        --killer.Reputation;
                        if (killerClient != null)
                        {
                            killerClient.Out.SendMessage("Vous avez perdu 1 point de réputation pour avoir tué " + killerClient.Player.GetPersonalizedName(victim), DOL.GS.PacketHandler.eChatType.CT_System, DOL.GS.PacketHandler.eChatLoc.CL_SystemWindow);
                        }
                        GameServer.Database.SaveObject(killer);
                        death.IsWanted = true;
                        death.Dirty = true;
                        GameServer.Database.SaveObject(death);
                        if (!reported.Contains(killer))
                        {
                            reported.Add(killer);
                            newsMessage = LanguageMgr.GetTranslation(victim.Client.Account.Language, "GameObjects.GamePlayer.Wanted", killer.Name);
                            NewsMgr.CreateNews("GameObjects.GamePlayer.Wanted", victim.Realm, eNewsType.RvRGlobal, false, true, killer.Name);
                            if (DOL.GS.ServerProperties.Properties.DISCORD_ACTIVE)
                            {
                                DolWebHook hook = new DolWebHook(DOL.GS.ServerProperties.Properties.DISCORD_WEBHOOK_ID);
                                hook.SendMessage(newsMessage);
                            }
                        }
                    }
                }
            }
            if (reported.Count == 0 && deaths.Count > 0)
            {
                victim.Out.SendMessage(LanguageMgr.GetTranslation(victim.Client.Account.Language, "GuardNPC.Report.AlreadyReported"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return 0;
            }
            return reported.Count;
        }

        public bool IsChainKiller(GamePlayer killer, GamePlayer killed)
        {
            var deaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KillerId").IsEqualTo(killer.InternalID)
                .And(DB.Column("KilledId").IsEqualTo(killed.InternalID)
                .And(DB.Column("DeathDate").IsGreatherThan("SUBTIME(NOW(), '0:10:0')"))));

            if (deaths == null || !deaths.Any())
            {
                return false;
            }

            return true;
        }
    }
}