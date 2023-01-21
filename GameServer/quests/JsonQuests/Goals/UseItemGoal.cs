using DOL.Database;
using DOL.Events;
using DOL.GS.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DOL.GS.Quests
{
	public class UseItemGoal : DataQuestJsonGoal
	{
		private readonly GameNPC m_target;
		public override GameNPC Target { get => m_target;}
		private readonly ItemTemplate m_item;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override ItemTemplate QuestItem => m_item;
		private readonly Area.Circle m_area;
		private readonly ushort m_areaRegion;
		private readonly bool hasArea = false;
		private readonly bool destroyItem = false;
		public override QuestZonePoint PointA { get; }
		public override bool hasInteraction { get; set; } = false;

		public UseItemGoal(DataQuestJson quest, int goalId, dynamic db) : base(quest, goalId, (object)db)
		{
			m_item = GameServer.Database.FindObjectByKey<ItemTemplate>((string)db.Item);
			
			if( db.AreaRadius != null && db.AreaRadius != "" && db.AreaRegion != null && db.AreaRegion != ""  && db.AreaCenter != null)
			{
				hasArea = true;
				m_area = new Area.Circle($"{quest.Name} EnterAreaGoal {goalId}", new Vector3((float)db.AreaCenter.X, (float)db.AreaCenter.Y, (float)db.AreaCenter.Z), (int)db.AreaRadius);
				m_area.DisplayMessage = !false;
				m_areaRegion = db.AreaRegion;

				var reg = WorldMgr.GetRegion(m_areaRegion);
				reg.AddArea(m_area);
				PointA = new QuestZonePoint(reg.GetZone(m_area.Position), m_area.Position);
			}
			if( db.TargetName != null && db.TargetName != "" && db.TargetRegion != null && db.TargetRegion != "" )
			{
				hasInteraction = true;
				m_target = WorldMgr.GetNPCsByNameFromRegion((string)db.TargetName ??  "", (ushort)db.TargetRegion, eRealm.None)
					.FirstOrDefault(quest.Npc);
			}
			if( db.DestroyItem != null && db.DestroyItem != "" )
			{
				destroyItem = db.DestroyItem;
			}
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("Item", m_item.Id_nb);
			dict.Add("TargetName", m_target.Name);
			dict.Add("DestroyItem", destroyItem);
			dict.Add("TargetRegion", m_target.CurrentRegionID);
			dict.Add("AreaCenter", m_area.Position);
			dict.Add("AreaRadius", m_area.Radius);
			dict.Add("AreaRegion", m_areaRegion);
			return dict;
		}

		public override void NotifyActive(PlayerQuest quest, PlayerGoalState goal, DOLEvent e, object sender, EventArgs args)
		{
			var player = quest.Owner;
			if((!hasArea || (hasArea && m_area.IsContaining(player.Position, false))) && e == GamePlayerEvent.UseSlot && args is UseSlotEventArgs useSlot)
			{
				var usedItem = player.Inventory.GetItem((eInventorySlot)useSlot.Slot);
				if (usedItem.Id_nb == QuestItem.Id_nb && (m_target==null || player.TargetObject == m_target))
				{
					if(destroyItem)
					{
						player.Inventory.RemoveCountFromStack(usedItem, 1);
					}
					AdvanceGoal(quest, goal);
				}
			}
		}
	}
}
