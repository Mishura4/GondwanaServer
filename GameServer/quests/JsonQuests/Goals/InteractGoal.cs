using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
    public class InteractGoal : DataQuestJsonGoal
    {
        private readonly GameNPC m_target;
        public override GameNPC Target { get => m_target; }
        private readonly string m_text;

        public override eQuestGoalType Type => eQuestGoalType.Unknown;
        public override int ProgressTotal => 1;
        public override QuestZonePoint PointA => new(m_target);
        public override bool hasInteraction { get; set; } = true;

        public InteractGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
        {
            m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName ?? "", (ushort)db.TargetRegion, eRealm.None)
                .FirstOrDefault(quest.Npc);
            m_text = db.Text;
        }

        public override Dictionary<string, object> GetDatabaseJsonObject()
        {
            var dict = base.GetDatabaseJsonObject();
            dict.Add("TargetName", m_target.Name);
            dict.Add("TargetRegion", m_target.CurrentRegionID);
            dict.Add("Text", m_text);
            return dict;
        }

        public override bool CanInteractWith(PlayerQuest questData, PlayerGoalState state, GameObject target)
            => state?.IsActive == true && target.Name == m_target.Name && target.CurrentRegion == m_target.CurrentRegion;

        public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
        {
            if (args is InteractWithEventArgs interact && interact.Target is ITextNPC textNPC && textNPC.TextNPCData.CheckQuestAvailable(Quest.Name, GoalId))
                return;

            var player = quest.Owner;
            if (e == GameObjectEvent.InteractWith && args is InteractWithEventArgs interacting && interacting.Target.Name == m_target.Name && interacting.Target.CurrentRegion == m_target.CurrentRegion)
            {
                if (!string.IsNullOrWhiteSpace(m_text))
                    ChatUtil.SendPopup(player, BehaviourUtils.GetPersonalizedMessage(m_text, player));
                AdvanceGoal(quest, goal);
            }
        }
    }
}
