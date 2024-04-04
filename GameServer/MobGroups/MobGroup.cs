using DOL.Database;
using DOL.GS;
using DOL.GS.Quests;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameNPC;
using static DOL.GS.Quests.DataQuestJsonGoal;

namespace DOL.MobGroups
{
    public class MobGroup
    {
        private MobGroupInfo originalGroupInfo;
        private bool isLoadedFromScript;

        public MobGroup(string id, bool isLoadedFromScript)
        {
            this.GroupId = id;
            this.isLoadedFromScript = isLoadedFromScript;
            this.NPCs = new List<GameNPC>();
            this.GroupInfos = new MobGroupInfo();
            this.HasOriginalStatus = false;
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
            this.NPCs = new List<GameNPC>();
            this.GroupInfos = new MobGroupInfo()
            {
                Effect = db.Effect != null ? int.TryParse(db.Effect, out int effect) ? effect : (int?)null : (int?)null,
                Flag = db.Flag > 0 ? (eFlags)db.Flag : (eFlags?)null,
                IsInvincible = db.IsInvincible != null ? bool.TryParse(db.IsInvincible, out bool dbInv) ? dbInv : (bool?)null : (bool?)null,
                Model = db.Model != null ? int.TryParse(db.Model, out int model) ? model : (int?)null : (int?)null,
                Race = db.Race != null ? Enum.TryParse(db.Race, out eRace race) ? race : (eRace?)null : (eRace?)null,
                VisibleSlot = db.VisibleSlot != null ? byte.TryParse(db.VisibleSlot, out byte slot) ? slot : (byte?)null : (byte?)null
            };

            this.mobGroupOriginFk = originalStatus?.GroupStatusId;
            this.originalGroupInfo = GetMobInfoFromSource(originalStatus);
            this.SetGroupInteractions(groupInteract);
            this.HasOriginalStatus = IsStatusOriginal();
        }

        /// <summary>
        /// Is this npc-player relation allows Friendly interact 
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestCompleted(GameNPC npc, GamePlayer player)
        {
            if (npc.CurrentGroupMob != null && npc.CurrentGroupMob.CompletedQuestID > 0)
            {
                if (npc.CurrentGroupMob.CompletedQuestCount > 0)
                {
                    var finishedCount = player.QuestListFinished.Where(q => q.QuestId == npc.CurrentGroupMob.CompletedQuestID).Count();
                    if (finishedCount >= npc.CurrentGroupMob.CompletedQuestCount)
                    {
                        return true;
                    }
                }

                if (npc.CurrentGroupMob.CompletedStepQuestID > 0)
                {
                    var currentQuest = player.QuestList.FirstOrDefault(q => q.QuestId == npc.CurrentGroupMob.CompletedQuestID
                    && q.Goals.Any(g => g is GenericDataQuestGoal jgoal && jgoal.Goal.GoalId == npc.CurrentGroupMob.CompletedStepQuestID));

                    if (currentQuest != null)
                    {
                        var currentGoal = currentQuest.Goals.FirstOrDefault(g => g is GenericDataQuestGoal jgoal && jgoal.Goal.GoalId == npc.CurrentGroupMob.CompletedStepQuestID);
                        if (currentGoal != null && currentGoal.Status == eQuestGoalStatus.Active)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Is this npc-player relation allows Friendly interact 
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestFriendly(GameNPC npc, GamePlayer player)
        {
            if (IsQuestCompleted(npc, player))
            {
                return npc.CurrentGroupMob.IsQuestConditionFriendly;
            }
            return false;
        }


        /// <summary>
        /// Is NPC aggressive on Quest Associated Condition
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestAggresive(GameNPC npc, GamePlayer player)
        {
            if (IsQuestCompleted(npc, player))
            {
                return !npc.CurrentGroupMob.IsQuestConditionFriendly;
            }
            return false;
        }


        private static MobGroupInfo GetMobInfoFromSource(GroupMobStatusDb source)
        {
            return source == null ? null : new MobGroupInfo()
            {
                Effect = source.Effect != null ? int.TryParse(source.Effect, out int grEffect) ? grEffect : (int?)null : (int?)null,
                Flag = source.Flag > 0 ? (eFlags)source.Flag : (eFlags?)null,
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
            return NPCs.All(m => exclude == m || !m.IsAlive);
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

        public List<GameNPC> NPCs
        {
            get;
            set;
        }

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

        public void ApplyGroupInfos(bool isLoadedFromScript = false)
        {
            this.NPCs.ForEach(n => n.Flags = this.GroupInfos.Flag.HasValue ? this.GroupInfos.Flag.Value : (eFlags)n.FlagsDb);

            if (this.GroupInfos.Model.HasValue)
            {
                this.NPCs.ForEach(n => n.Model = (ushort)this.GroupInfos.Model.Value);
            }
            else if (!isLoadedFromScript)
            {
                this.NPCs.ForEach(n => n.Model = n.ModelDb);
            }

            if (this.GroupInfos.Race.HasValue)
            {
                this.NPCs.ForEach(n => n.Race = (short)this.GroupInfos.Race.Value);
            }
            else if (!isLoadedFromScript)
            {
                this.NPCs.ForEach(n => n.Race = (short)n.RaceDb);
            }

            if (!isLoadedFromScript || this.GroupInfos.VisibleSlot.HasValue)
            {
                this.NPCs.ForEach(npc =>
                {
                    npc.VisibleActiveWeaponSlots = this.GroupInfos.VisibleSlot.HasValue ? this.GroupInfos.VisibleSlot.Value : npc.VisibleWeaponsDb;

                    foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendLivingEquipmentUpdate(npc);
                    }
                });
            }

            if (this.GroupInfos.Effect.HasValue)
            {
                var spell = GameServer.Database.SelectObjects<Database.DBSpell>(DB.Column("SpellID").IsEqualTo(this.GroupInfos.Effect.Value))?.FirstOrDefault();
                ushort effect = (ushort)this.GroupInfos.Effect.Value;

                if (spell != null)
                {
                    effect = (ushort)spell.ClientEffect;
                }

                Task.Delay(500).Wait();

                this.NPCs.ForEach(npc =>
                {
                    foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendSpellEffectAnimation(npc, npc, effect, 0, false, (byte)5);
                    }
                });
            }
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