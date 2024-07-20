/*
- Interaction
- Réponse:
	- Texte
	- Spell animation
	- Emotes
- phrase/emotes aléatoire en cc général
- Conditions:
	- Level
	- guilde
	- race
	- classe
	- prp
*/

using System.Linq;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Quests;
using DOL.Territories;
using DOL.Language;

namespace DOL.GS.Scripts
{
    public class TextNPCMerchant : GameMerchant, ITextNPC
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

        public TextNPCMerchant() : base()
        {
            TextNPCData = new TextNPCPolicy(this);
            SetOwnBrain(new TextNPCBrain());
        }

        public void SayRandomPhrase()
        {
            TextNPCData.SayRandomPhrase();
        }

        public override bool Interact(GamePlayer player)
        {
            if (!TextNPCData.CanInteractWith(player) || !base.Interact(player))
                return false;

            if (TextNPCData.IsTerritoryLinked.HasValue && TextNPCData.IsTerritoryLinked.Value && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
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

            if (TextNPCData.IsTerritoryLinked.HasValue && TextNPCData.IsTerritoryLinked.Value && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
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
            var result = base.GetQuestIndicator(player);
            if (result != eQuestIndicator.None)
                return result;

            foreach (var qid in QuestIdListToGive)
            {
                var quest = player.QuestList.OfType<PlayerQuest>().FirstOrDefault(pq => pq.QuestId == qid);
                if (quest == null)
                    continue;
                if (quest.VisibleGoals.OfType<DataQuestJsonGoal.GenericDataQuestGoal>()
                    .Any(g => g.Goal is EndGoal end && end.Target == this))
                    return eQuestIndicator.Finish;
            }

            return TextNPCData.Condition.CanGiveQuest != eQuestIndicator.None && TextNPCData.Condition.CheckAccess(player)
                ? TextNPCData.Condition.CanGiveQuest
                : eQuestIndicator.None;
        }
    }
}