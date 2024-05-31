using DOL.Database;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.GameEvents;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.MobGroups
{
    public class MobGroupManager
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static MobGroupManager instance;

        public static MobGroupManager Instance => instance ?? (instance = new MobGroupManager());

        private MobGroupManager()
        {
            this.Groups = new Dictionary<string, MobGroup>();
            this.GroupsToRemoveOnServerLoad = new List<string>();
        }

        public Dictionary<string, MobGroup> Groups
        {
            get;
        }

        public List<string> GroupsToRemoveOnServerLoad
        {
            get;
        }

        public void HandleNpcDeath(GameNPC npc, GameObject killer)
        {
            if (npc.MobGroups == null)
            {
                return;
            }

            foreach (MobGroup group in npc.MobGroups.Where(g => g.IsAllDead(npc)))
            {
                //Handle interaction if any slave group
                this.HandleInteraction(group);

                //Reset GroupInfo
                group.ResetGroupInfo();

                //Notify
                GameEventMgr.Notify(GroupMobEvent.MobGroupDead, group);
                var mobGroupEvent = GameEventManager.Instance.Events.FirstOrDefault(e =>
                                                                                        e.KillStartingGroupMobId?.Equals(group.GroupId) == true &&
                                                                                        !e.StartedTime.HasValue &&
                                                                                        e.Status == EventStatus.NotOver &&
                                                                                        e.StartConditionType == StartingConditionType.Kill);
                if (mobGroupEvent != null)
                {
                    Task.Run(() => GameEventManager.Instance.StartEvent(mobGroupEvent, null, killer as GamePlayer));
                }
            }
        }

        private void HandleInteraction(MobGroup master)
        {
            if (master.SlaveGroupId != null && master.GroupInteractions != null && this.Groups.TryGetValue(master.SlaveGroupId, out MobGroup slave))
            {
                slave.GroupInfos = MobGroup.CopyGroupInfo(master.GroupInteractions);
                slave.SaveToDabatase();
                slave.ApplyGroupInfos(slave.GroupId.StartsWith("spwn_add_"));
                slave.HasOriginalStatus = false;
            }
        }

        public List<string> GetInfos(MobGroup mobGroup)
        {
            if (mobGroup == null)
            {
                return null;
            }

            var infos = new List<string>();
            infos.Add(" - GroupId : " + mobGroup.GroupId);
            infos.Add(" - Db Id : " + (mobGroup.InternalId ?? string.Empty));
            infos.Add(" ** GroupInfos ** ");
            infos.Add(" - Effect : " + mobGroup.GroupInfos.Effect);
            if (!mobGroup.GroupInfos.Flag.HasValue)
            {
                infos.Add(" - Flags: -");
            }
            else
            {
                infos.Add(string.Format(" - Flags: {0} (0x{1})", ((GameNPC.eFlags)mobGroup.GroupInfos.Flag.Value).ToString("G"), mobGroup.GroupInfos.Flag.Value.ToString("X")));
            }

            infos.Add(" - IsInvincible : " + (mobGroup.GroupInfos.IsInvincible?.ToString() ?? "-"));
            infos.Add(" - Model : " + (mobGroup.GroupInfos.Model?.ToString() ?? "-"));
            infos.Add(" - Race : " + (mobGroup.GroupInfos.Race?.ToString() ?? "-"));
            infos.Add(" - VisibleSlot : " + (mobGroup.GroupInfos.VisibleSlot?.ToString() ?? "-"));
            infos.Add("");
            infos.Add("MobGroup Origin StatusId: " + (mobGroup?.mobGroupOriginFk ?? "-"));
            infos.Add("MobGroup Interact StatusId: " + (mobGroup?.mobGroupInterfactFk ?? "-"));
            infos.Add(" - SlaveGroupId : " + (mobGroup.SlaveGroupId ?? "-"));
            infos.Add("");
            infos.Add("ON Completed QUEST:");
            infos.Add("CompletedQuestID: " + (mobGroup.CompletedQuestID > 0 ? mobGroup.CompletedQuestID.ToString() : "-"));
            infos.Add("CompletedStepQuestID: " + (mobGroup.CompletedStepQuestID > 0 ? mobGroup.CompletedStepQuestID.ToString() : "-"));
            infos.Add("CompletedQuestCount: " + (mobGroup.CompletedQuestID > 0 ? mobGroup.CompletedQuestCount.ToString() : "-"));
            infos.Add("IsQuestConditionFriendly (will become): " + (mobGroup.CompletedQuestID > 0 ? mobGroup.IsQuestConditionFriendly ? "Friendly" : "Aggressive" : "-"));
            infos.Add("CompletedQuestNPCModel: " + (mobGroup.CompletedQuestNPCModel != 0 ? mobGroup.CompletedQuestNPCModel.ToString() : "-"));
            infos.Add("CompletedQuestNPCFlags: " + (mobGroup.CompletedQuestNPCFlags ?? "-"));
            infos.Add("CompletedQuestNPCSize: " + (mobGroup.CompletedQuestNPCSize != 0 ? mobGroup.CompletedQuestNPCSize.ToString() : "-"));
            infos.Add("CompletedQuestAggro: " + (mobGroup.CompletedQuestAggro != 0 ? mobGroup.CompletedQuestAggro.ToString() : "-"));
            infos.Add("CompletedQuestRange: " + (mobGroup.CompletedQuestRange != 0 ? mobGroup.CompletedQuestRange.ToString() : "-"));
            if (mobGroup.GroupInteractions != null)
            {
                infos.Add(" Actions on Group Killed : ");
                infos.Add(" - Set Effect : " + mobGroup.GroupInteractions.Effect);
                infos.Add(" - Set Flag : " + mobGroup.GroupInteractions.Flag?.ToString() ?? "-");
                infos.Add(" - Set IsInvincible : " + (mobGroup.GroupInteractions.IsInvincible?.ToString() ?? "-"));
                infos.Add(" - Set Model : " + (mobGroup.GroupInteractions.Model?.ToString() ?? "-"));
                infos.Add(" - Set Race : " + (mobGroup.GroupInteractions.Race?.ToString() ?? "-"));
                infos.Add(" - Set VisibleSlot : " + (mobGroup.GroupInteractions.VisibleSlot?.ToString() ?? "-"));
            }
            infos.Add("******************");
            infos.Add(" - NPC Count: " + mobGroup.NPCs.Count);
            mobGroup.NPCs.ForEach(n => infos.Add(string.Format("Name: {0} | Id: {1} | Region: {2} | Alive: {3} ", n.Name, n.ObjectID, n.CurrentRegionID, n.IsAlive)));
            return infos;
        }

        public string GetGroupIdFromMobId(string mobId)
        {
            if (mobId == null)
            {
                return null;
            }

            foreach (var group in this.Groups)
            {
                if (group.Value.NPCs.Any(npc => npc.InternalID.Equals(mobId)))
                {
                    return group.Key;
                }
            }

            return null;
        }

        public bool RemoveGroupsAndMobs(string groupId, bool isLoadedFromScript = false)
        {
            if (!this.Groups.ContainsKey(groupId))
            {
                return false;
            }

            if (!isLoadedFromScript)
            {
                foreach (var npc in this.Groups[groupId].NPCs.ToList())
                {
                    this.RemoveMobFromGroup(npc, groupId);
                }
            }

            this.Groups[groupId].NPCs.Clear();
            this.Groups.Remove(groupId);
            var db = GameServer.Database.SelectObjects<GroupMobDb>(DB.Column("GroupId").IsEqualTo(groupId))?.FirstOrDefault();


            if (db != null)
            {
                GameServer.Database.DeleteObject(db);
            }

            return true;
        }


        public bool LoadFromDatabase()
        {
            var groups = GameServer.Database.SelectAllObjects<GroupMobXMobs>();

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    if (!this.Groups.ContainsKey(group.GroupId))
                    {
                        var groupDb = GameServer.Database.SelectObjects<GroupMobDb>(DB.Column("GroupId").IsEqualTo(group.GroupId))?.FirstOrDefault();
                        if (groupDb != null)
                        {
                            var groupInteraction = groupDb.GroupMobInteract_FK_Id != null ?
                                                    GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(groupDb.GroupMobInteract_FK_Id))?.FirstOrDefault() : null;
                            var originalStatus = groupDb.GroupMobOrigin_FK_Id != null ?
                                                    GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(groupDb.GroupMobOrigin_FK_Id))?.FirstOrDefault() : null;
                            this.Groups.Add(group.GroupId, new MobGroup(groupDb, groupInteraction, originalStatus));
                        }
                    }

                    if (WorldMgr.Regions.ContainsKey(group.RegionID))
                    {
                        var mobInWorld = WorldMgr.Regions[group.RegionID].Objects?.FirstOrDefault(o => o?.InternalID?.Equals(group.MobID) == true && o is GameNPC) as GameNPC;

                        if (mobInWorld != null && this.Groups.TryGetValue(group.GroupId, out MobGroup mobGroup))
                        {
                            if (!mobGroup.NPCs.Exists(m => m.InternalID.Equals(mobInWorld.InternalID)))
                            {
                                mobGroup.NPCs.Add(mobInWorld);
                                mobInWorld.AddToMobGroup(mobGroup);
                            }
                        }
                    }
                }
                Instance.Groups.Foreach(g => g.Value.ApplyGroupInfos());
            }

            //remove npc from spawner interractions
            foreach (var groupId in this.GroupsToRemoveOnServerLoad)
            {
                if (this.Groups.ContainsKey(groupId))
                {
                    foreach (var npc in this.Groups[groupId].NPCs)
                    {
                        npc.RemoveFromWorld();
                        npc.Delete();
                    }
                }
            }

            return true;
        }

        public bool AddMobToGroup(GameNPC npc, string groupId, bool isLoadedFromScript = false)
        {
            if (npc == null || groupId == null)
            {
                return false;
            }

            bool isnew = false;
            MobGroup mobGroup;
            if (!this.Groups.TryGetValue(groupId, out mobGroup))
            {
                mobGroup = new MobGroup(groupId, isLoadedFromScript);
                this.Groups.Add(groupId, mobGroup);
                isnew = true;
            }

            mobGroup.NPCs.Add(npc);
            npc.AddToMobGroup(mobGroup);

            if (isnew && !isLoadedFromScript)
            {
                var newGroup = new GroupMobDb() { GroupId = groupId };
                GameServer.Database.AddObject(newGroup);
                mobGroup.InternalId = newGroup.ObjectId;

                GameServer.Database.AddObject(new GroupMobXMobs()
                {
                    GroupId = groupId,
                    MobID = npc.InternalID,
                    RegionID = npc.CurrentRegionID
                });
            }
            else if (!isLoadedFromScript)
            {
                var exists = GameServer.Database.SelectObjects<GroupMobXMobs>(DB.Column("MobID").IsEqualTo(npc.InternalID))?.FirstOrDefault();
                if (exists != null)
                {
                    exists.RegionID = npc.CurrentRegionID;
                    exists.GroupId = groupId;
                    GameServer.Database.SaveObject(exists);
                }
                else
                {
                    GroupMobXMobs newgroup = new GroupMobXMobs()
                    {
                        MobID = npc.InternalID,
                        GroupId = groupId,
                        RegionID = npc.CurrentRegionID
                    };

                    GameServer.Database.AddObject(newgroup);
                }
            }

            return true;
        }


        public bool RemoveMobFromGroup(GameNPC npc, string groupId)
        {
            if (npc == null || groupId == null)
            {
                return false;
            }

            if (!this.Groups.TryGetValue(groupId, out MobGroup group))
            {
                log.Error($"Impossible to remove Group because inmemory Groups does not contain groupId: {groupId}");
                return false;
            }


            if (!group.NPCs.Remove(npc))
            {
                log.Error($"Impossible to remove NPC {npc.InternalID} from groupId: {groupId}");
                return false;
            }

            var grp = GameServer.Database.SelectObjects<GroupMobXMobs>(DB.Column("MobID").IsEqualTo(npc.InternalID).And(DB.Column("GroupId").IsEqualTo(groupId)))?.FirstOrDefault();

            if (grp == null)
            {
                log.Error($"Impossible to remove GroupMobXMobs entry with MobId: {npc.InternalID} and groupId: {groupId}");
                group.NPCs.Add(npc);
                return false;
            }
            else
            {
                npc.RemoveFromMobGroup(group);
                return GameServer.Database.DeleteObject(grp);
            }
        }

        public List<GameNPC> GetMobsBySwitchFamily(string switchFamily)
        {
            List<GameNPC> mobs = new List<GameNPC>();

            foreach (var group in Groups.Values)
            {
                if (group.SwitchFamily == switchFamily)
                {
                    mobs.AddRange(group.NPCs);
                }
            }

            return mobs;
        }
    }
}