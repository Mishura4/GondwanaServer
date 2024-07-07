/**
 * Created by Virant "Dre" Jérémy for Amtenael
 */

using System.Linq;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.Language;
using DOL.Territories;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DOL.GS.Scripts
{
    public class TextNPC : AmteMob, ITextNPC
    {
        public TextNPCPolicy TextNPCData { get; set; }

        public TextNPCPolicy GetTextNPCPolicy(GameLiving target = null)
        {
            return TextNPCData;
        }

        public TextNPCPolicy GetOrCreateTextNPCPolicy(GameLiving target = null)
        {
            return GetTextNPCPolicy(target);
        }

        public TextNPC()
        {
            TextNPCData = new TextNPCPolicy(this);
            SetOwnBrain(new TextNPCBrain());
        }

        public bool? IsOutlawFriendly { get; set; }
        public bool? IsTerritoryLinked { get; set; }

        #region TextNPCPolicy
        public void SayRandomPhrase()
        {
            TextNPCData.SayRandomPhrase();
        }

        public override bool Interact(GamePlayer player)
        {
            if (!TextNPCData.CheckAccess(player) || !base.Interact(player))
                return false;

            if (IsTerritoryLinked == true && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TextNPC.NotInOwnedTerritory"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            return TextNPCData.Interact(player);
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!(source is GamePlayer player) || !base.WhisperReceive(source, str))
                return false;

            if (IsTerritoryLinked == true && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TextNPC.NotInOwnedTerritory"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            return TextNPCData.WhisperReceive(source, str);
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            return TextNPCData.ReceiveItem(source, item);
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            DBTextNPC textDB;
            try
            {
                textDB = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == obj.ObjectId);
            }
            catch
            {
                DBTextNPC.Init();
                textDB = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == obj.ObjectId);
            }

            if (textDB != null)
                TextNPCData.LoadFromDatabase(textDB);
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();
            TextNPCData.SaveIntoDatabase();
        }

        public override void DeleteFromDatabase()
        {
            base.DeleteFromDatabase();
            TextNPCData.DeleteFromDatabase();
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            if (IsTerritoryLinked == true && CurrentTerritory?.IsOwnedBy(player) != true)
                return eQuestIndicator.None;
            
            var result = base.GetQuestIndicator(player);
            if (result != eQuestIndicator.None)
                return result;

            foreach (var qid in QuestIdListToGive)
            {
                var quest = player.QuestList.OfType<PlayerQuest>().FirstOrDefault(pq => pq.QuestId == qid);
                if (quest == null)
                    continue;
                if (quest.VisibleGoals.OfType<DataQuestJsonGoal.GenericDataQuestGoal>().Any(g => g.Goal is EndGoal end && end.Target == this))
                    return eQuestIndicator.Finish;
            }

            return TextNPCData.Condition.CanGiveQuest != eQuestIndicator.None && TextNPCData.Condition.CheckAccess(player)
                ? TextNPCData.Condition.CanGiveQuest
                : eQuestIndicator.None;
        }
        #endregion
    }

    /// <summary>
    /// Provided only for compatibility
    /// </summary>
    public class EchangeurNPC : TextNPC { }
}