using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DOL.GS.Quests
{
    public class KillGoal : DataQuestJsonGoal
    {
        private readonly int m_killCount = 1;
        private readonly GameNPC m_target;
        private readonly Area.Circle m_area;
        private readonly ushort m_areaRegion;
        private readonly bool hasArea = false;

        public override eQuestGoalType Type => eQuestGoalType.Kill;
        public override int ProgressTotal => m_killCount;
        public override QuestZonePoint PointA => new(m_target);

        public KillGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
        {
            m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName, (ushort)db.TargetRegion, eRealm.None).FirstOrDefault();
            if (m_target == null)
                throw new Exception($"[DataQuestJson] Quest {quest.Id}: can't load the goal id {goalId}, the target npc (name: {db.TargetName}, reg: {db.TargetRegion}) is not found");
            m_killCount = db.KillCount;

            if (db.AreaRadius != null && db.AreaRadius != "" && db.AreaRegion != null && db.AreaRegion != "" && db.AreaCenter != null)
            {
                hasArea = true;
                m_area = new Area.Circle($"{quest.Name} EnterAreaGoal {goalId}", new Vector3((float)db.AreaCenter.X, (float)db.AreaCenter.Y, (float)db.AreaCenter.Z), (int)db.AreaRadius);
                m_area.DisplayMessage = !false;
                m_areaRegion = db.AreaRegion;

                var reg = WorldMgr.GetRegion(m_areaRegion);
                reg.AddArea(m_area);
            }
        }

        public override Dictionary<string, object> GetDatabaseJsonObject()
        {
            var dict = base.GetDatabaseJsonObject();
            dict.Add("TargetName", m_target.Name);
            dict.Add("TargetRegion", m_target.CurrentRegionID);
            dict.Add("KillCount", m_killCount);
            dict.Add("AreaCenter", m_area.Position);
            dict.Add("AreaRadius", m_area.Radius);
            dict.Add("AreaRegion", m_areaRegion);
            return dict;
        }

        public override bool CanInteractWith(PlayerQuest questData, PlayerGoalState state, GameObject target)
            => state?.IsActive == true && target.Name == m_target.Name && target.CurrentRegion == m_target.CurrentRegion;

        public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
        {
            // Enemy of player with quest was killed, check quests and steps
            if (e == GameLivingEvent.EnemyKilled && args is EnemyKilledEventArgs killedArgs)
            {
                var killed = killedArgs.Target;
                if (killed == null || m_target.Name != killed.Name || m_target.CurrentRegion != killed.CurrentRegion
                    || (hasArea && !m_area.IsContaining(killed.Position, false)))
                    return;
                AdvanceGoal(quest, goal);
            }
        }
        public override void Unload()
        {
            WorldMgr.GetRegion(m_areaRegion)?.RemoveArea(m_area);
            base.Unload();
        }
    }
}
