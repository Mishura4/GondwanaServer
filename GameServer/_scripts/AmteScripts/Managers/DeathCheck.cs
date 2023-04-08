using DOL.Database;
using DOL.GS;
using DOL.Language;
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
            var deaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KilledId").IsEqualTo(player.InternalID).And(DB.Column("isWanted").IsEqualTo(0).And(DB.Column("DeathDate").IsGreatherThan("SUBTIME(NOW(), '3:0:0')").And(DB.Column("ExitFromJail").IsEqualTo(0)))));

            var reportedDeaths = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("KilledId").IsEqualTo(player.InternalID).And(DB.Column("isWanted").IsEqualTo(1).And(DB.Column("DeathDate").IsGreatherThan("SUBTIME(NOW(), '3:0:0')").And(DB.Column("ExitFromJail").IsEqualTo(0)))));

            if (deaths == null || !deaths.Any() || reportedDeaths == null || reportedDeaths.Any())
            {
                return 0;
            }

            int reported = 0;

            var death = deaths.OrderByDescending(d => d.DeathDate).FirstOrDefault();

            var client = WorldMgr.GetClientByPlayerID(death.KillerId, true, true);
            string newsMessage = "";

            if (client != null)
            {
                client.Player.Reputation--;
                client.Player.SaveIntoDatabase();
                reported++;
                client.Out.SendMessage("Vous avez perdu 1 point de réputation pour avoir tué " + client.Player.GetPersonalizedName(player), DOL.GS.PacketHandler.eChatType.CT_System, DOL.GS.PacketHandler.eChatLoc.CL_SystemWindow);
                death.IsWanted = true;
                death.Dirty = true;
                GameServer.Database.SaveObject(death);
                newsMessage = LanguageMgr.GetTranslation(client, "GameObjects.GamePlayer.Wanted", client.Player.GetPersonalizedName(player));
                NewsMgr.CreateNews("GameObjects.GamePlayer.Wanted", player.Realm, eNewsType.RvRGlobal, false, true, client.Player.Name);
            }
            else
            {
                var killer = GameServer.Database.FindObjectByKey<DOLCharacters>(death.KillerId);

                if (killer != null)
                {
                    killer.Reputation--;
                    GameServer.Database.SaveObject(killer);
                    death.IsWanted = true;
                    death.Dirty = true;
                    reported++;
                    GameServer.Database.SaveObject(death);
                    newsMessage = LanguageMgr.GetTranslation(client, "GameObjects.GamePlayer.Wanted", killer.Name);
                    NewsMgr.CreateNews("GameObjects.GamePlayer.Wanted", player.Realm, eNewsType.RvRGlobal, false, true, killer.Name);
                }
            }

            if (DOL.GS.ServerProperties.Properties.DISCORD_ACTIVE)
            {
                DolWebHook hook = new DolWebHook(DOL.GS.ServerProperties.Properties.DISCORD_WEBHOOK_ID);
                hook.SendMessage(newsMessage);
            }
            return reported;
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