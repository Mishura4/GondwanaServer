using DOL.Database;
using DOL.Events;
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

        public bool IsAllOthersGroupMobDead(GameNPC npc)
        {
            if (npc == null || npc.CurrentGroupMob == null)
            {
                return false;
            }

            if (!this.Groups.ContainsKey(npc.CurrentGroupMob.GroupId))
            {
                return false;
            }

            bool allDead = this.Groups[npc.CurrentGroupMob.GroupId].NPCs.All(m => !m.IsAlive);

            if (allDead)
            {
                //Handle interaction if any slave group
                this.HandleInteraction(this.Groups[npc.CurrentGroupMob.GroupId]);

                //Reset GroupInfo
                this.Groups[npc.CurrentGroupMob.GroupId].ResetGroupInfo();

                //Notify
                GameEventMgr.Notify(GroupMobEvent.MobGroupDead, this.Groups[npc.CurrentGroupMob.GroupId]);
            }

            return allDead;
        }

        private void HandleInteraction(MobGroup master)
        {
            if (master.SlaveGroupId != null && this.Groups.ContainsKey(master.SlaveGroupId) && master.GroupInteractions != null)
            {
                var slave = this.Groups[master.SlaveGroupId];

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

                        if (mobInWorld != null && this.Groups.ContainsKey(group.GroupId))
                        {
                            if (this.Groups[group.GroupId].NPCs.FirstOrDefault(m => m.InternalID.Equals(mobInWorld.InternalID)) == null)
                            {
                                this.Groups[group.GroupId].NPCs.Add(mobInWorld);
                                mobInWorld.CurrentGroupMob = this.Groups[group.GroupId];
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
            if (!this.Groups.ContainsKey(groupId))
            {
                this.Groups.Add(groupId, new MobGroup(groupId, isLoadedFromScript));
                isnew = true;
            }

            this.Groups[groupId].NPCs.Add(npc);
            npc.CurrentGroupMob = this.Groups[groupId];

            if (isnew && !isLoadedFromScript)
            {
                var newGroup = new GroupMobDb() { GroupId = groupId };
                GameServer.Database.AddObject(newGroup);
                this.Groups[groupId].InternalId = newGroup.ObjectId;

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

            if (!this.Groups.ContainsKey(groupId))
            {
                log.Error($"Impossible to remove Group because inmemory Groups does not contain groupId: {groupId}");
                return false;
            }


            if (!this.Groups[groupId].NPCs.Remove(npc))
            {
                log.Error($"Impossible to remove NPC {npc.InternalID} from groupId: {groupId}");
                return false;
            }

            var grp = GameServer.Database.SelectObjects<GroupMobXMobs>(DB.Column("Mob_ID").IsEqualTo(npc.InternalID).And(DB.Column("GroupId").IsEqualTo(groupId)))?.FirstOrDefault();

            if (grp == null)
            {
                log.Error($"Impossible to remove GroupMobXMobs entry with MobId: {npc.InternalID} and groupId: {groupId}");
                this.Groups[groupId].NPCs.Add(npc);
                return false;
            }
            else
            {
                npc.CurrentGroupMob = null;
                return GameServer.Database.DeleteObject(grp);
            }
        }
    }
}