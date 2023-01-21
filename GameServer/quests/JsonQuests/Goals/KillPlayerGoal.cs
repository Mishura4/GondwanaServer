using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DOL.GS.Quests
{
    public class KillPlayerGoal : DataQuestJsonGoal
    {
        private readonly int m_killCount = 1;
        private readonly GameNPC m_target;
        private readonly Area.Circle m_area;
        private readonly ushort m_areaRegion;
        private readonly bool hasArea = false;

        public override eQuestGoalType Type => eQuestGoalType.Kill;
        public override int ProgressTotal => m_killCount;
		public override QuestZonePoint PointA { get; }
        Region m_region;
        ushort m_regionId;

        public KillPlayerGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
        {
            m_killCount = db.KillCount;
            if (db.TargetRegion != null && db.TargetRegion != "")
                m_region = WorldMgr.GetRegion((ushort)db.TargetRegion);
            if (db.AreaRadius != null && db.AreaRadius != "" && db.AreaRegion != null && db.AreaRegion != "" && db.AreaCenter != null)
            {
                hasArea = true;
                m_area = new Area.Circle($"{quest.Name} KillPlayerGoal {goalId}", new Vector3((float)db.AreaCenter.X, (float)db.AreaCenter.Y, (float)db.AreaCenter.Z), (int)db.AreaRadius);
                m_area.DisplayMessage = !false;
                m_areaRegion = db.AreaRegion;

                var reg = WorldMgr.GetRegion(m_areaRegion);
                reg.AddArea(m_area);
				PointA = new QuestZonePoint(reg.GetZone(m_area.Position), m_area.Position);
            }
        }

        public override Dictionary<string, object> GetDatabaseJsonObject()
        {
            var dict = base.GetDatabaseJsonObject();
            dict.Add("TargetRegion", m_regionId);
            dict.Add("KillCount", m_killCount);
            dict.Add("AreaCenter", m_area.Position);
            dict.Add("AreaRadius", m_area.Radius);
            dict.Add("AreaRegion", m_areaRegion);
            return dict;
        }

        public override bool CanInteractWith(PlayerQuest questData, PlayerGoalState state, GameObject target)
            => state?.IsActive == true && target is GamePlayer && (m_region == null || target.CurrentRegion == m_region);

        public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
        {
            // Enemy of player with quest was killed, check quests and steps
            if (e == GameLivingEvent.EnemyKilled && args is EnemyKilledEventArgs killedArgs)
            {
                var killed = killedArgs.Target;
                if (killed == null || (m_region != null && m_region != killed.CurrentRegion)
                    || !(killed is GamePlayer)
                    || (hasArea && !m_area.IsContaining(killed.Position, false)))
                    return;
			Console.WriteLine(killed.CurrentRegion);
			Console.WriteLine(m_region);
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
