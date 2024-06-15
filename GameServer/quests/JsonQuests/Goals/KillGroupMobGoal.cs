using DOL.Database;
using DOL.Events;
using DOL.GS.Geometry;
using DOL.MobGroups;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DOL.GS.Quests
{
    public class KillGroupMobGoal : DataQuestJsonGoal
    {
        private readonly Area.Circle m_area;
        private readonly ushort m_areaRegion;
        private readonly bool hasArea = false;

        public override eQuestGoalType Type => eQuestGoalType.Kill;
        public override int ProgressTotal => 1;
        public override QuestZonePoint PointA { get; }
        Region m_region;
        ushort m_regionId;
        string m_targetName;
        public KillGroupMobGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
        {
            m_regionId = (ushort)db.TargetRegion;
            m_region = WorldMgr.GetRegion(m_regionId);
            m_targetName = (string)db.TargetName;
            if (m_targetName == null)
                throw new Exception($"[DataQuestJson] Quest {quest.Id}: can't load the goal id {goalId}, the target groupnpc (name: {db.TargetName}, reg: {db.TargetRegion}) is not found");

            if (db.AreaRadius != null && db.AreaRadius != "" && db.AreaRegion != null && db.AreaRegion != "" && db.AreaCenter != null)
            {
                hasArea = true;
                m_area = new Area.Circle($"{quest.Name} KillGroupMobGoal {goalId}", Coordinate.Create((int)((float)db.AreaCenter.X), (int)((float)db.AreaCenter.Y), (int)((float)db.AreaCenter.Z)), (int)db.AreaRadius);
                m_area.DisplayMessage = !false;
                m_areaRegion = db.AreaRegion;

                var reg = WorldMgr.GetRegion(m_areaRegion);
                reg.AddArea(m_area);
                PointA = new QuestZonePoint(reg.GetZone(m_area.Coordinate), m_area.Coordinate);
            }
            else if (m_region != null && db.AreaCenter != null)
            {
                var pos = Coordinate.Create((int)((float)db.AreaCenter.X), (int)((float)db.AreaCenter.Y), (int)((float)db.AreaCenter.Z));
                PointA = new QuestZonePoint(m_region.GetZone(pos), pos);
            }
        }

        public override Dictionary<string, object> GetDatabaseJsonObject()
        {
            var dict = base.GetDatabaseJsonObject();
            dict.Add("TargetName", m_targetName);
            dict.Add("TargetRegion", m_regionId);
            dict.Add("AreaCenter", m_area.Coordinate);
            dict.Add("AreaRadius", m_area.Radius);
            dict.Add("AreaRegion", m_areaRegion);
            return dict;
        }

        public override bool CanInteractWith(PlayerQuest questData, PlayerGoalState state, GameObject target)
            => state?.IsActive == true
            && (target is GameNPC npc && MobGroupManager.Instance.GetGroupIdFromMobId(npc.InternalID) == m_targetName)
            && (m_region == null || target.CurrentRegion == m_region);

        public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
        {
            // Enemy of player with quest was killed, check quests and steps
            if (e == GameLivingEvent.EnemyKilled && args is EnemyKilledEventArgs killedArgs)
            {
                var killed = killedArgs.Target;

                if (killed == null
                || m_region != killed.CurrentRegion
                || (hasArea && !m_area.IsContaining(killed.Coordinate, false)))
                    return;

                if (killed is GameNPC killedNpc && MobGroupManager.Instance.Groups.TryGetValue(m_targetName, out MobGroup targetGroup) && !targetGroup.IsAllDead(killedNpc))
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
