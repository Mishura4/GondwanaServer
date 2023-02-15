using DOL.Database;
using DOL.Events;
using DOL.MobGroups;
using DOL.GS.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DOL.GS.Scripts;

namespace DOL.GS.Quests
{
    public class BringAFriendGoal : DataQuestJsonGoal
    {
        private readonly string m_target;
        public override eQuestGoalType Type => eQuestGoalType.Unknown;
        public override int ProgressTotal => m_friendCount;
        private readonly int m_friendCount = 1;
        public override QuestZonePoint PointA { get; }
        public override bool hasInteraction { get; set; } = true;
        private readonly Area.Circle m_area;
        private readonly ushort m_areaRegion;
        private readonly bool hasArea = false;
        public FollowingFriendMob lastFriend;

        public BringAFriendGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
        {
            m_target = db.TargetName;
            m_friendCount = db.Count;
            if (db.AreaRadius != null && db.AreaRadius != "" && db.AreaRegion != null && db.AreaRegion != "" && db.AreaCenter != null)
            {
                hasArea = true;
                m_area = new Area.Circle($"{quest.Name} EnterAreaGoal {goalId}", new Vector3((float)db.AreaCenter.X, (float)db.AreaCenter.Y, (float)db.AreaCenter.Z), (int)db.AreaRadius);
                m_area.DisplayMessage = false;
                m_areaRegion = db.AreaRegion;

                var reg = WorldMgr.GetRegion(m_areaRegion);
                reg.AddArea(m_area);
                PointA = new QuestZonePoint(reg.GetZone(m_area.Position), m_area.Position);
            }
        }

        public override Dictionary<string, object> GetDatabaseJsonObject()
        {
            var dict = base.GetDatabaseJsonObject();
            dict.Add("TargetName", m_target);
            dict.Add("Count", m_friendCount);
            dict.Add("AreaCenter", m_area.Position);
            dict.Add("AreaRadius", m_area.Radius);
            dict.Add("AreaRegion", m_areaRegion);
            return dict;
        }

        public override void AbortGoal(PlayerQuest questData)
        {
            if (lastFriend != null)
                lastFriend.ResetFriendMobs();
            base.AbortGoal(questData);
        }

        public override void EndGoal(PlayerQuest questData, PlayerGoalState goalData)
        {
            if (lastFriend != null)
                lastFriend.ResetTimer.Start();
            base.EndGoal(questData, goalData);
        }

        public override bool CanInteractWith(PlayerQuest questData, PlayerGoalState state, GameObject target)
            => state?.IsActive == true && target is GameNPC gameNPC && gameNPC.CurrentGroupMob != null && gameNPC.CurrentGroupMob.GroupId == m_target;

        public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
        {
            if (e == GameLivingEvent.BringAFriend && args is BringAFriendArgs bringAFriendArgs)
            {
                var friend = bringAFriendArgs.Friend as FollowingFriendMob;
                if (friend == null || (friend.Name != m_target && (friend.CurrentGroupMob == null || friend.CurrentGroupMob.GroupId != m_target)))
                    return;
                lastFriend = friend;
                if (bringAFriendArgs.Entered)
                {
                    AdvanceGoal(quest, goal);
                }
                else
                {
                    DecreaseGoal(quest, goal);
                }
            }
        }

        public override void Unload()
        {
            WorldMgr.GetRegion(m_areaRegion)?.RemoveArea(m_area);
            base.Unload();
        }
    }
}
