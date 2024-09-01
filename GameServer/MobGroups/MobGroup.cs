using DOL.Database;
using DOL.GS;
using DOL.GS.Commands;
using DOL.GS.Quests;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameNPC;
using static DOL.GS.Quests.DataQuestJsonGoal;

namespace DOL.MobGroups
{
    public class MobGroup
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private MobGroupInfo originalGroupInfo;
        private bool isLoadedFromScript;

        public string SwitchFamily { get; set; }

        public MobGroup(string id, bool isLoadedFromScript)
        {
            this.GroupId = id;
            this.isLoadedFromScript = isLoadedFromScript;
            this.GroupInfos = new MobGroupInfo();
            this.HasOriginalStatus = false;
            this.SwitchFamily = null;
        }

        public MobGroup(GroupMobDb db, GroupMobStatusDb groupInteract, GroupMobStatusDb originalStatus)
        {
            this.InternalId = db.ObjectId;
            this.GroupId = db.GroupId;
            this.SlaveGroupId = db.SlaveGroupId;
            this.IsQuestConditionFriendly = db.IsQuestConditionFriendly;
            this.CompletedQuestNPCFlags = db.CompletedQuestNPCFlags;
            this.CompletedQuestNPCModel = db.CompletedQuestNPCModel;
            this.CompletedQuestNPCSize = db.CompletedQuestNPCSize;
            this.CompletedQuestAggro = db.CompletedQuestAggro;
            this.CompletedQuestRange = db.CompletedQuestRange;
            this.CompletedStepQuestID = db.CompletedStepQuestID;
            this.CompletedQuestID = db.CompletedQuestID;
            this.CompletedQuestCount = db.CompletedQuestCount;
            this.GroupInfos = new MobGroupInfo()
            {
                Effect = db.Effect != null ? int.TryParse(db.Effect, out int effect) ? effect : (int?)null : (int?)null,
                Flag = db.Flag > 0 ? (long?)db.Flag : (long?)null,
                IsInvincible = db.IsInvincible != null ? bool.TryParse(db.IsInvincible, out bool dbInv) ? dbInv : (bool?)null : (bool?)null,
                Model = db.Model != null ? int.TryParse(db.Model, out int model) ? model : (int?)null : (int?)null,
                Race = db.Race != null ? Enum.TryParse(db.Race, out eRace race) ? race : (eRace?)null : (eRace?)null,
                VisibleSlot = db.VisibleSlot != null ? byte.TryParse(db.VisibleSlot, out byte slot) ? slot : (byte?)null : (byte?)null
            };

            this.mobGroupOriginFk = originalStatus?.GroupStatusId;
            this.originalGroupInfo = GetMobInfoFromSource(originalStatus);
            this.SetGroupInteractions(groupInteract);
            this.HasOriginalStatus = IsStatusOriginal();
            this.SwitchFamily = db.SwitchFamily;
            this.AssistRange = db.AssistRange;
        }

        /// <summary>
        /// Is this npc-player relation allows Friendly interact 
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestCompleted(GameNPC npc, GamePlayer player)
        {
            return npc.MobGroups?.TrueForAll(g => g.HasPlayerCompletedQuests(player)) == true;
        }

        /// <summary>
        /// Is this npc-player relation allows Friendly interact 
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestFriendly(GameNPC npc, GamePlayer player)
        {
            if (npc.MobGroups == null)
                return false;

            bool hasObjectives = false;
            bool hasUnsatisfiedObjectives = false;
            foreach (MobGroup group in npc.MobGroups.Where(g => g.CompletedQuestID >= 0 && g.IsQuestConditionFriendly))
            {
                hasObjectives = true;
                if (!group.HasPlayerCompletedQuests(player))
                {
                    hasUnsatisfiedObjectives = true;
                    break;
                }
            }
            return hasObjectives && !hasUnsatisfiedObjectives;
        }


        /// <summary>
        /// Is NPC aggressive on Quest Associated Condition
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestAggresive(GameNPC npc, GamePlayer player)
        {
            if (npc.MobGroups == null)
                return false;

            bool hasObjectives = false;
            bool hasUnsatisfiedObjectives = false;
            foreach (MobGroup group in npc.MobGroups.Where(g => g.CompletedQuestID >= 0 && !g.IsQuestConditionFriendly))
            {
                hasObjectives = true;
                if (!group.HasPlayerCompletedQuests(player))
                {
                    hasUnsatisfiedObjectives = true;
                    break;
                }
            }
            return hasObjectives && !hasUnsatisfiedObjectives;
        }


        private static MobGroupInfo GetMobInfoFromSource(GroupMobStatusDb source)
        {
            return source == null ? null : new MobGroupInfo()
            {
                Effect = source.Effect != null ? int.TryParse(source.Effect, out int grEffect) ? grEffect : (int?)null : (int?)null,
                Flag = source.Flag.HasValue ? source.Flag : null,
                IsInvincible = source.SetInvincible != null ? bool.TryParse(source.SetInvincible, out bool inv) ? inv : (bool?)null : (bool?)null,
                Model = source.Model != null ? int.TryParse(source.Model, out int grModel) ? grModel : (int?)null : (int?)null,
                Race = source.Race != null ? Enum.TryParse(source.Race, out eRace grRace) ? grRace : (eRace?)null : (eRace?)null,
                VisibleSlot = source.VisibleSlot != null ? byte.TryParse(source.VisibleSlot, out byte grSlot) ? grSlot : (byte?)null : (byte?)null
            };
        }


        public static MobGroupInfo CopyGroupInfo(MobGroupInfo copy)
        {
            return copy == null ? null : new MobGroupInfo()
            {
                Effect = copy.Effect,
                Flag = copy.Flag,
                IsInvincible = copy.IsInvincible,
                Model = copy.Model,
                Race = copy.Race,
                VisibleSlot = copy.VisibleSlot
            };
        }

        public bool IsStatusOriginal()
        {
            if (this.originalGroupInfo == null)
            {
                return false;
            }

            if (this.GroupInfos.Effect != this.originalGroupInfo.Effect)
            {
                return false;
            }

            if (this.GroupInfos.Flag != this.originalGroupInfo.Flag)
            {
                return false;
            }

            if (this.GroupInfos.IsInvincible != this.originalGroupInfo.IsInvincible)
            {
                return false;
            }

            if (this.GroupInfos.Model != this.originalGroupInfo.Model)
            {
                return false;
            }

            if (this.GroupInfos.Race != this.originalGroupInfo.Race)
            {
                return false;
            }

            if (this.GroupInfos.VisibleSlot != this.originalGroupInfo.VisibleSlot)
            {
                return false;
            }

            return true;
        }

        public bool IsAllDead(GameNPC exclude = null)
        {
            lock (m_NPCs)
            {
                return m_NPCs.All(m => exclude == m || !m.IsAlive);
            }
        }

        public bool HasPlayerCompletedQuests(GamePlayer player)
        {
            if (CompletedQuestID <= 0)
                return false;

            if (CompletedQuestCount > 0)
            {
                var finishedCount = player.QuestListFinished.Where(q => q.QuestId == CompletedQuestID).Count();
                if (finishedCount < CompletedQuestCount)
                {
                    return false;
                }
            }

            if (CompletedStepQuestID > 0)
            {

                var currentQuest = player.QuestList.Where(q => q.QuestId == CompletedQuestID).Select(q => q.Goals).OfType<GenericDataQuestGoal>().Any(jgoal => jgoal.Goal.GoalId == CompletedStepQuestID && jgoal.Status == eQuestGoalStatus.Active);

                if (currentQuest == null)
                {
                    return false;
                }
            }
            return true;
        }

        public string mobGroupInterfactFk
        {
            get;
            private set;
        }

        public string mobGroupOriginFk
        {
            get;
            private set;
        }

        public string InternalId
        {
            get;
            set;
        }

        public string GroupId
        {
            get;
            set;
        }

        public string SlaveGroupId
        {
            get;
            set;
        }

        public bool IsQuestConditionFriendly
        {
            get;
            set;
        }

        public string CompletedQuestNPCFlags
        {
            get;
            set;
        }
        public ushort CompletedQuestNPCModel
        {
            get;
            set;
        }
        public ushort CompletedQuestNPCSize
        {
            get;
            set;
        }
        public ushort CompletedQuestAggro
        {
            get;
            set;
        }
        public ushort CompletedQuestRange
        {
            get;
            set;
        }
        public ushort CompletedStepQuestID
        {
            get;
            set;
        }

        public int CompletedQuestID
        {
            get;
            set;
        }

        public int CompletedQuestCount
        {
            get;
            set;
        }

        public int AssistRange
        {
            get;
            set;
        } = WorldMgr.INFO_DISTANCE;

        public bool HasOriginalStatus
        {
            get;
            set;
        }

        public MobGroupInfo GroupInfos
        {
            get;
            set;
        }

        public ImmutableList<GameNPC> NPCs
        {
            get
            {
                lock (m_NPCs)
                {
                    return m_NPCs.ToImmutableList();
                }
            }
        }

        public void RemoveAllMobs(bool isLoadedFromScript)
        {
            lock (m_NPCs)
            {
                foreach (var npc in m_NPCs)
                {
                    npc.RemoveFromMobGroup(this);
                }
                m_NPCs.Clear();
                if (!isLoadedFromScript)
                    GameServer.Database.DeleteObject(GameServer.Database.SelectObjects<GroupMobXMobs>(g => g.GroupId == GroupId) ?? new List<GroupMobXMobs>());
            }
        }

        public bool RemoveMob(GameNPC npc)
        {
            lock (m_NPCs)
            {
                if (!m_NPCs.Remove(npc))
                {
                    return false;
                }

                var grpxmob = GameServer.Database.SelectObjects<GroupMobXMobs>(g => g.MobID == npc.InternalID && g.GroupId == GroupId)?.FirstOrDefault();
                if (grpxmob != null)
                {
                    GameServer.Database.DeleteObject(grpxmob);
                }
                return true;
            }
        }

        public void AddMob(GameNPC npc, bool isLoadedFromScript = false)
        {
            lock (m_NPCs)
            {
                if (m_NPCs.Contains(npc))
                    return;
                
                m_NPCs.Add(npc);
            }
            
            // TODO: move this to a SaveIntoDatabase method
            var exists = GameServer.Database.SelectObjects<GroupMobXMobs>(g => g.MobID == npc.InternalID && g.GroupId == GroupId)?.FirstOrDefault();
            if (exists != null)
            {
                exists.RegionID = npc.CurrentRegionID;
                exists.GroupId = GroupId;
                GameServer.Database.SaveObject(exists);
            }
            else
            {
                GroupMobXMobs newgroup = new GroupMobXMobs()
                {
                    MobID = npc.InternalID,
                    GroupId = GroupId,
                    RegionID = npc.CurrentRegionID
                };

                GameServer.Database.AddObject(newgroup);
            }
        }

        public void ForeachMob(Action<GameNPC> func)
        {
            lock (m_NPCs)
            {
                m_NPCs.ForEach(func);
            }
        }

        public void ForeachMob(Action<GameNPC> func, Func<IEnumerable<GameNPC>, IEnumerable<GameNPC>> filter)
        {
            lock (m_NPCs)
            {
                foreach (var npc in filter(m_NPCs))
                {
                    func(npc);
                }
            }
        }

        private List<GameNPC> m_NPCs = new List<GameNPC>();

        public MobGroupInfo GroupInteractions
        {
            get;
            set;
        }

        public void SetGroupInteractions(GroupMobStatusDb groupInteract)
        {
            this.mobGroupInterfactFk = groupInteract?.GroupStatusId;
            this.GroupInteractions = GetMobInfoFromSource(groupInteract);
        }

        public void SetGroupInfo(GroupMobStatusDb status, bool isOriginalStatus, bool isLoadedFromScript = false)
        {
            this.GroupInfos = GetMobInfoFromSource(status);
            this.mobGroupOriginFk = status?.GroupStatusId;
            this.HasOriginalStatus = isOriginalStatus;
            this.originalGroupInfo = GetMobInfoFromSource(status);
            this.ApplyGroupInfos(isLoadedFromScript);
        }

        public void ApplyGroupInfos(GameNPC npc, bool isLoadedFromScript = false)
        {
            if (this.GroupInfos.Flag.HasValue)
            {
                if (this.GroupInfos.Flag < 0)
                {
                    npc.Flags |= (eFlags)(this.GroupInfos.Flag * -1);
                }
                else
                {
                    npc.Flags = (eFlags)this.GroupInfos.Flag.Value;
                }
            }

            if (this.GroupInfos.Model.HasValue)
            {
                npc.Model = (ushort)this.GroupInfos.Model.Value;
            }

            if (this.GroupInfos.Race.HasValue)
            {
                npc.Race = (short)this.GroupInfos.Race.Value;
            }

            if (this.GroupInfos.VisibleSlot.HasValue)
            {
                npc.VisibleActiveWeaponSlots = this.GroupInfos.VisibleSlot.Value;

                if (npc.IsAlive && npc.ObjectState == GameObject.eObjectState.Active)
                {
                    foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendLivingEquipmentUpdate(npc);
                    }
                }
            }

            if (this.GroupInfos.Effect.HasValue)
            {
                var spell = GameServer.Database.SelectObjects<Database.DBSpell>(DB.Column("SpellID").IsEqualTo(this.GroupInfos.Effect.Value))?.FirstOrDefault();
                ushort effect = (ushort)this.GroupInfos.Effect.Value;

                if (spell != null)
                {
                    effect = (ushort)spell.ClientEffect;
                }

                Task.Run(async () =>
                {
                    await Task.Delay(500);

                    if (npc.IsAlive && npc.ObjectState == GameObject.eObjectState.Active)
                    {
                        foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            player.Out.SendSpellEffectAnimation(npc, npc, effect, 0, false, (byte)5);
                        }
                    }
                });
            }
        }

        public void ApplyGroupInfos(bool isLoadedFromScript = false)
        {
            this.NPCs.ForEach(npc => ApplyGroupInfos(npc, isLoadedFromScript));
        }

        public void ResetGroupInfo(bool force = false)
        {
            if (this.originalGroupInfo != null && (force || !this.HasOriginalStatus))
            {
                this.GroupInfos = CopyGroupInfo(this.originalGroupInfo);
                this.HasOriginalStatus = true;
                this.ApplyGroupInfos();

                if (!isLoadedFromScript)
                {
                    this.SaveToDabatase();
                }
            }
        }

        public void ClearGroupInfosAndInterractions()
        {
            this.GroupInfos = new MobGroupInfo();
            this.mobGroupInterfactFk = null;
            this.mobGroupOriginFk = null;
            this.CompletedQuestCount = 0;
            this.CompletedQuestID = 0;
            this.CompletedStepQuestID = 0;
            this.IsQuestConditionFriendly = false;
            this.HasOriginalStatus = true;
            this.SlaveGroupId = null;
            this.CompletedQuestNPCFlags = null;
            this.CompletedQuestNPCModel = 0;
            this.CompletedQuestNPCSize = 0;
            this.CompletedQuestAggro = 0;
            this.CompletedQuestRange = 0;
            this.ApplyGroupInfos();
            this.SaveToDabatase();

            if (this.GroupInteractions != null && this.SlaveGroupId != null)
            {
                if (MobGroupManager.Instance.Groups.ContainsKey(this.SlaveGroupId))
                {
                    MobGroupManager.Instance.Groups[this.SlaveGroupId].ClearGroupInfosAndInterractions();
                }
            }
        }

        public void ReloadMobsFromDatabase()
        {
            foreach (var npc in this.NPCs)
            {
                if (npc.InternalID != null)
                {
                    var mob = GameServer.Database.FindObjectByKey<Mob>(npc.InternalID);

                    if (mob != null)
                    {
                        npc.LoadFromDatabase(mob);
                    }
                }
                if (!npc.IsAlive)
                {
                    npc.Spawn();
                }
                else
                {
                    npc.AddToWorld();
                }
            }

            this.ApplyGroupInfos();
        }

        public void SaveToDabatase()
        {
            GroupMobDb db = null;
            bool isNew = this.InternalId == null;

            if (this.InternalId == null)
            {
                db = new GroupMobDb();
            }
            else
            {
                db = GameServer.Database.FindObjectByKey<GroupMobDb>(this.InternalId);

                if (db == null)
                {
                    db = new GroupMobDb();
                    isNew = true;
                }
            }

            db.Flag = this.GroupInfos.Flag.HasValue ? (int)this.GroupInfos.Flag.Value : 0;
            db.Race = this.GroupInfos.Race?.ToString();
            db.VisibleSlot = this.GroupInfos.VisibleSlot?.ToString();
            db.Effect = this.GroupInfos.Effect?.ToString();
            db.Model = this.GroupInfos.Model?.ToString();
            db.GroupId = this.GroupId;
            db.SlaveGroupId = this.SlaveGroupId;
            db.IsInvincible = this.GroupInfos.IsInvincible?.ToString();
            db.ObjectId = this.InternalId;
            db.GroupMobInteract_FK_Id = this.mobGroupInterfactFk;
            db.GroupMobOrigin_FK_Id = this.mobGroupOriginFk;
            db.CompletedQuestCount = this.CompletedQuestCount;
            db.CompletedQuestID = this.CompletedQuestID;
            db.IsQuestConditionFriendly = this.IsQuestConditionFriendly;
            db.CompletedStepQuestID = this.CompletedStepQuestID;
            db.CompletedQuestNPCModel = this.CompletedQuestNPCModel;
            db.CompletedQuestNPCSize = this.CompletedQuestNPCSize;
            db.CompletedQuestAggro = this.CompletedQuestAggro;
            db.CompletedQuestRange = this.CompletedQuestRange;
            db.CompletedQuestNPCFlags = this.CompletedQuestNPCFlags;
            db.SwitchFamily = this.SwitchFamily;
            db.AssistRange = this.AssistRange;

            if (isNew)
            {
                if (GameServer.Database.AddObject(db))
                {
                    this.InternalId = db.ObjectId;
                }
            }
            else
            {
                GameServer.Database.SaveObject(db);
            }
        }
    }
}