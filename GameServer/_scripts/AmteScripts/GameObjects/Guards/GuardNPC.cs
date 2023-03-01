using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Finance;
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

            player.Out.SendMessage("Bonjour, que voulez-vous ?\n\n[Signaler] mon tueur !\n\n[Voir] la liste noire.",
                eChatType.CT_System, eChatLoc.CL_PopupWindow);
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

                    int reported = DeathCheck.Instance.ReportPlayer(player);
                    //if (BlacklistMgr.ReportPlayer(player)) Old Way not used anymore
                    if (reported > 0)
                    {
                        string words = reported == 1 ? "La personne qui vous a tué a été signalé !" : "Les " + reported + " personnes qui vont ont tués ont été signalés !";
                        player.Out.SendMessage(words, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    }
                    else
                        player.Out.SendMessage("C'est trop tard pour signaler votre tueur !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;

                case "Voir":
                    StringBuilder sb = new StringBuilder();
                    var names = this.GetOutlawsName();

                    if (names == null)
                    {
                        sb.AppendLine("Personne n'est recherchée actuellement.");
                        player.Out.SendMessage(sb.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        break;
                    }

                    sb.AppendLine("Les personnes suivantes sont sur la liste noire:");
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
                player.Out.SendMessage("Hmm, peut-être que... non, ça ne me dit rien !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (new DateTime(2000, 1, 1).Add(new TimeSpan(0, 0, item.MaxCondition)) < DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)))
            {
                player.Out.SendMessage("Elle a l'air pourri cette tête, je ne la reconnais pas !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (!player.Inventory.RemoveCountFromStack(item, 1))
                return false;

            var prime = Money.GetMoney(0, 0, ServerProperties.Properties.REWARD_OUTLAW_HEAD_GOLD, 0, 0);
            player.AddMoney(Currency.Copper.Mint(prime));
            player.Out.SendMessage("Merci de votre précieuse aide, voici " + ServerProperties.Properties.REWARD_OUTLAW_HEAD_GOLD + " pièces d'or pour vous !", eChatType.CT_System, eChatLoc.CL_PopupWindow);

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
