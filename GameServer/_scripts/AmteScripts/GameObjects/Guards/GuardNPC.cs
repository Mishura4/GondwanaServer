using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Finance;
using DOL.Language;
using DOL.GS.PacketHandler;
using GameServerScripts.Amtescripts.Managers;


namespace DOL.GS.Scripts
{
    public interface IGuardNPC
    {

    }

    public class GuardNPC : AmteMob, IGuardNPC
    {
        public GuardNPC()
        {
            SetOwnBrain(new GuardNPCBrain());
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Interact.Text1"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Interact.Text2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Interact.Text3"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            var player = source as GamePlayer;
            if (!base.WhisperReceive(source, text) || player == null)  //|| BlacklistMgr.IsBlacklisted(player))
                return false;

            switch (text)
            {
                case "Signaler":
                case "Report":

                    int reported = DeathCheck.Instance.ReportPlayer(player);
                    //if (BlacklistMgr.ReportPlayer(player)) Old Way not used anymore
                    if (reported > 0)
                    {
                        string words = reported == 1 ? LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Report.Oneplayer") : LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Report.Moreplayers", reported);
                        player.Out.SendMessage(words, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    }
                    else
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Report.Toolate"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;

                case "Voir":
                case "Look":
                    StringBuilder sb = new StringBuilder();
                    var names = this.GetOutlawsName();

                    if (names == null)
                    {
                        sb.AppendLine(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Response.Nobodywanted"));
                        player.Out.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        break;
                    }

                    sb.AppendLine(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Response.Blacklist"));
                    names.ForEach(s => sb.AppendLine(s));
                    player.Out.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
            }
            return true;
        }

        public override bool ReceiveItem(GameLiving source, Database.InventoryItem item)
        {
            var player = source as AmtePlayer;
            if (player == null || item == null || !item.Id_nb.StartsWith(player.HeadTemplate.Id_nb))
                return false;

            if (!item.CanDropAsLoot)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Response.Dontknow"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (new DateTime(2000, 1, 1).Add(new TimeSpan(0, 0, item.MaxCondition)) < DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Response.Rottenhead"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (!player.Inventory.RemoveCountFromStack(item, 1))
                return false;
            int reward = ServerProperties.Properties.REWARD_OUTLAW_HEAD_GOLD;
            List<string> messages = item.Template.MessageArticle.Split(';').ToList();

            if (messages.Count >= 2)
            {
                reward *= (int)(-int.Parse(messages[1]) / 0.5);
            }

            var prime = Money.GetMoney(0, 0, reward, 0, 0);
            player.AddMoney(Currency.Copper.Mint(prime));
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardNPC.Response.Headreward", reward), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override void WalkToSpawn(short speed)
        {
            base.WalkToSpawn(MaxSpeed);
        }

        public IEnumerable<string> GetOutlawsName()
        {
            IList<DBDeathLog> kills = GameServer.Database.SelectObjects<DBDeathLog>(DB.Column("isWanted").IsEqualTo(1).And(DB.Column("ExitFromJail").IsEqualTo(0)));
            return kills.Select(k => k.Killer).Distinct();
        }
    }

    public class GuardTextNPC : TextNPC, IGuardNPC
    {
        public GuardTextNPC()
        {
            SetOwnBrain(new GuardNPCBrain());
        }
    }
}
