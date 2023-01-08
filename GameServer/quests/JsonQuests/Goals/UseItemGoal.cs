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
		private readonly ItemTemplate m_item;

		public override eQuestGoalType Type => eQuestGoalType.Unknown;
		public override int ProgressTotal => 1;
		public override ItemTemplate QuestItem => m_item;
		private readonly Area.Circle m_area;
		private readonly ushort m_areaRegion;
		private readonly bool hasArea = false;

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
			}
		}

		public override Dictionary<string, object> GetDatabaseJsonObject()
		{
			var dict = base.GetDatabaseJsonObject();
			dict.Add("Item", m_item.Id_nb);
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
				if (usedItem.Id_nb == QuestItem.Id_nb)
					AdvanceGoal(quest, goal);
			}
		}
	}
}
