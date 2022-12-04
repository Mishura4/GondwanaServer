using DOL.Database;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public int ReportPlayer(GamePlayer player)
        {
            var deaths = GameServer.Database.SelectObjects<DBDeathLog>("KilledId = @killed AND isWanted = 0 AND DeathDate > SUBTIME(NOW(), '3:0:0') AND ExitFromJail = 0", new QueryParameter("killed", player.InternalID));

            if (deaths == null || !deaths.Any())
            {
                return 0;
            }

            int reported = 0;

            var death = deaths.OrderByDescending(d => d.DeathDate).FirstOrDefault();

            var client = WorldMgr.GetClientByPlayerID(death.KillerId, true, true);

            if (client != null)
            {
                client.Player.Reputation--;
                client.Player.SaveIntoDatabase();
                reported++;
                client.Out.SendMessage("Vous avez perdu 1 point de réputation pour avoir tué " + player.Name, DOL.GS.PacketHandler.eChatType.CT_System, DOL.GS.PacketHandler.eChatLoc.CL_SystemWindow);
                death.IsWanted = true;
                GameServer.Database.SaveObject(death);
            }
            else
            {
                var killer = GameServer.Database.FindObjectByKey<DOLCharacters>(death.KillerId);

                if (killer != null)
                {
                    killer.Reputation--;
                    GameServer.Database.SaveObject(killer);
                    death.IsWanted = true;
                    reported++;
                    GameServer.Database.SaveObject(death);
                }
            }

            return reported;
        }

        public bool IsChainKiller(GamePlayer killer, GamePlayer killed)
        {
            var deaths = GameServer.Database.SelectObjects<DBDeathLog>("KillerId = @KillerId AND KilledId = @KilledId AND DeathDate > SUBTIME(NOW(), '0:10:0')", new QueryParameter[] {
                    new QueryParameter("KillerId", killer.InternalID),
                    new QueryParameter("KilledId", killed.InternalID)
                });

            if (deaths == null || !deaths.Any() || deaths.Count == 1)
            {
                return false;
            }

            return true;
        }
    }
}