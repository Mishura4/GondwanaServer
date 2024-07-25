﻿using DOL.Database;
using DOL.Events;
using DOL.GS.GameEvents;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Quests
{
    public class SwitchGoal : DataQuestJsonGoal
    {
        private readonly int m_switchCount = 1;
        private readonly string switchFamily;
        private readonly string switchActivatedMessage;
        private readonly ushort m_targetRegion;
        private readonly Area.Circle m_area;
        private readonly bool hasArea = false;

        public override eQuestGoalType Type => eQuestGoalType.Unknown;
        public override int ProgressTotal => m_switchCount;
        public override QuestZonePoint PointA { get; }

        public SwitchGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
        {
            switchFamily = db.TargetName;
            switchActivatedMessage = db.Text;
            m_switchCount = db.KillCount;
            m_targetRegion = (ushort)db.TargetRegion;

            if (db.AreaRadius != null && db.AreaRadius != "" && db.AreaRegion != null && db.AreaRegion != "" && db.AreaCenter != null)
            {
                hasArea = true;
                m_area = new Area.Circle($"{quest.Name} SwitchGoal {goalId}", Coordinate.Create((int)((float)db.AreaCenter.X), (int)((float)db.AreaCenter.Y), (int)((float)db.AreaCenter.Z)), (int)db.AreaRadius);
                m_area.DisplayMessage = false;

                var reg = WorldMgr.GetRegion(m_targetRegion);
                reg.AddArea(m_area);
                PointA = new QuestZonePoint(reg.GetZone(m_area.Coordinate), m_area.Coordinate);
            }
        }

        public override Dictionary<string, object> GetDatabaseJsonObject()
        {
            var dict = base.GetDatabaseJsonObject();
            dict.Add("TargetName", switchFamily);
            dict.Add("TargetRegion", m_targetRegion);
            dict.Add("KillCount", m_switchCount);
            dict.Add("AreaCenter", m_area.Coordinate);
            dict.Add("AreaRadius", m_area.Radius);
            dict.Add("AreaRegion", m_targetRegion);
            dict.Add("Text", switchActivatedMessage);
            return dict;
        }

        public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
        {
            if (e == SwitchEvent.SwitchActivated && args is SwitchEventArgs switchArgs)
            {
                if (switchArgs.Coffre.SwitchFamily == switchFamily && switchArgs.Coffre.isActivated)
                {
                    var switchesInFamily = GameCoffre.Coffres.Where(c => c.SwitchFamily == switchFamily);
                    if (switchesInFamily.Count(c => c.isActivated) >= m_switchCount)
                    {
                        quest.Owner.Out.SendMessage(switchActivatedMessage, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        AdvanceGoal(quest, goal);
                    }
                }
            }
        }

        public override void Unload()
        {
            if (hasArea)
            {
                WorldMgr.GetRegion(m_targetRegion)?.RemoveArea(m_area);
            }
            base.Unload();
        }
    }
}