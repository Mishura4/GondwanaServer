using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Quests;
using DOL.GS.Scripts;
using log4net;

namespace DOL.GS
{
    public class TaskMaster : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TextNPCCondition Condition { get; private set; }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            if (player.Reputation >= 0)
                return eQuestIndicator.Lore;
            else return eQuestIndicator.None;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player) && player.Reputation < 0)
                return false;

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.Greetings", player.RaceName), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TaskMaster.Taskline"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;
            if (player == null)
                return false;

            switch (str.ToLower())
            {
                case "tâches":
                case "tasks":
                    {
                        //get list of all itextnpcs from db, that have isintaskmaster==1
                        IList<DBTextNPC> taskGivingNPCs = GameServer.Database.SelectObjects<DBTextNPC>(DB.Column("IsInTaskMaster").IsEqualTo("1"));
                        foreach (var taskNPC in taskGivingNPCs)
                        {
                            Condition = new TextNPCCondition(taskNPC.Condition);
                            if (Condition.CheckAccess(player))
                            {
                                var text = taskNPC.MobName + "\n";
                                if (player.Client.Account.Language == "EN")
                                    text += taskNPC.TaskDescEN;
                                else if (player.Client.Account.Language == "FR")
                                    text += taskNPC.TaskDescFR;
                                //get taskNPC 
                                var mob = DOLDB<Mob>.SelectObject(DB.Column("Mob_ID").IsEqualTo(taskNPC.MobID));
                                var region = WorldMgr.GetRegion(mob.Region);
                                if (region != null)
                                {
                                    var zone = region.GetZone(mob.X, mob.Y);
                                    if (zone != null)
                                    {
                                        player.Out.SendMessage(text + "\n" + zone.Description + "\n", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                    }
                                }
                            }
                        }
                        break;
                    }
            }
            return true;
        }
    }
}
