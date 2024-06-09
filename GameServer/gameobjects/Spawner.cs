﻿using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Geometry;
using DOL.GS.Styles;
using DOL.MobGroups;
using DOLDatabase.Tables;
using log4net;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// This Class spanws Mob and add them to a GroupMob
    /// </summary>
    public class Spawner
        : AmteMob
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string inactiveDefaultGroupStatusAddsKey = "Spawner_inactive_adds";
        private readonly string activeDefaultGroupStatusAddsKey = "Spawner_active_adds";
        public string inactiveGroupStatusAddsKey;
        private string activeGroupStatusAddsKey;
        private string dbId;
        private List<GameNPC> loadedAdds;
        private bool isPredefinedSpawns;
        private string addsGroupmobId;
        private bool isAddsActiveStatus;
        private int lifePercentTriggerSpawn;
        private int percentLifeAddsActivity;
        private bool isAggroType;
        private int npcTemplate1;
        private int npcTemplate2;
        private int npcTemplate3;
        private int npcTemplate4;
        private int npcTemplate5;
        private int npcTemplate6;
        private int addsRespawnCountTotal;
        private int addsRespawnCurrentCount;
        private int addRespawnTimerSecs;
        private DateTime? npcAddsNextPopupTimeStamp;

        private readonly object m_addsLock = new object();

        /// <summary>
        /// Timer to reset the adds after a reset
        /// </summary>
        private RegionTimer addsResetTimer;

        /// <summary>
        /// On resetting, time to keep the adds alive before despawning
        /// </summary>
        private static readonly int MILLISECONDS_BEFORE_RESET_ADDS = 20 * 60 * 1000; // 20 minutes

        /// <summary>
        /// If > 0, the spawner will spawn (percentageOfPlayersInRadius% of players in a radius of 1000) mobs randomly picked between the valid templates
        /// If <= 0, up to 6 mobs will be spawned, each for every valid template
        /// </summary>
        private int percentageOfPlayersInRadius;


        public Spawner()
            : base()
        {
        }

        public Spawner(INpcTemplate template)
            : base(template)
        {
        }

        public string SpawnerGroupId
        {
            get
            {
                return "spwn_" + (dbId != null ? dbId.Substring(0, 8) : Guid.NewGuid().ToString().Substring(0, 8));
            }
        }


        public string InactiveGroupStatusAddsKey
        {
            get
            {
                if (inactiveGroupStatusAddsKey != null)
                {
                    return inactiveGroupStatusAddsKey;
                }

                return inactiveDefaultGroupStatusAddsKey;
            }
        }

        public string ActiveGroupStatusAddsKey
        {
            get
            {
                if (activeGroupStatusAddsKey != null)
                {
                    return activeGroupStatusAddsKey;
                }

                return activeDefaultGroupStatusAddsKey;
            }
        }


        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            var result = GameServer.Database.SelectObjects<SpawnerTemplate>(DB.Column("MobID").IsEqualTo(obj.ObjectId));
            if (result != null)
            {
                var db = result.FirstOrDefault();

                if (db != null)
                {
                    dbId = db.ObjectId;
                    percentLifeAddsActivity = db.PercentLifeAddsActivity;
                    lifePercentTriggerSpawn = db.LifePercentTriggerSpawn;
                    addRespawnTimerSecs = db.AddRespawnTimerSecs;
                    percentageOfPlayersInRadius = db.PercentageOfPlayerInRadius;
                    isAggroType = db.IsAggroType;
                    npcTemplate1 = db.NpcTemplate1;
                    npcTemplate2 = db.NpcTemplate2;
                    npcTemplate3 = db.NpcTemplate3;
                    npcTemplate4 = db.NpcTemplate4;
                    npcTemplate5 = db.NpcTemplate5;
                    npcTemplate6 = db.NpcTemplate6;
                    if (isAggroType && lifePercentTriggerSpawn is > 0 and < 100)
                    {
                        log.Warn($"spawner {InternalID} is marked as spawning on aggro, but also has a lifePercentTriggerSpawn; it will be set to not spawn on aggro");
                        isAggroType = false;
                    }

                    if (db.MasterGroupId != null)
                    {
                        npcTemplate1 = -1;
                        npcTemplate2 = -1;
                        npcTemplate3 = -1;
                        npcTemplate4 = -1;
                        npcTemplate5 = -1;
                        npcTemplate6 = -1;
                        isPredefinedSpawns = true;
                        addsGroupmobId = db.MasterGroupId;

                        //add Spawner to GroupMob for interractions
                        var spawnerGroup = GameServer.Database.SelectObjects<GroupMobDb>(DB.Column("GroupId").IsEqualTo(SpawnerGroupId)).FirstOrDefault();

                        if (spawnerGroup == null)
                        {
                            AddSpawnerToMobGroup();
                        }

                        //remove mastergroup mob if present
                        if (MobGroupManager.Instance.Groups.TryGetValue(this.addsGroupmobId, out var mobGroup))
                        {
                            mobGroup.NPCs.ForEach(n =>
                            {
                                n.RemoveFromWorld();
                                n.Delete();
                            });
                        }
                        else
                        {
                            //on server load add groups to remove list
                            MobGroupManager.Instance.GroupsToRemoveOnServerLoad.Add(addsGroupmobId);
                        }

                        UpdateMasterGroupInDatabase();
                    }

                    addsRespawnCountTotal = db.AddsRespawnCount;
                    addsRespawnCurrentCount = 0;

                    if (db.ActiveStatusId != null)
                        activeGroupStatusAddsKey = db.ActiveStatusId;

                    if (db.InactiveStatusId != null)
                        inactiveGroupStatusAddsKey = db.InactiveStatusId;

                    loadedAdds = null;
                    Cleanup();
                }
            }
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();

            var result = GameServer.Database.SelectObjects<SpawnerTemplate>(DB.Column("MobID").IsEqualTo(dbId));

            if (result != null)
            {
                var db = result.FirstOrDefault();

                if (db != null)
                {
                    db.IsAggroType = isAggroType;
                    db.NpcTemplate1 = npcTemplate1;
                    db.NpcTemplate2 = npcTemplate2;
                    db.NpcTemplate3 = npcTemplate3;
                    db.NpcTemplate4 = npcTemplate4;
                    db.NpcTemplate5 = npcTemplate5;
                    db.NpcTemplate6 = npcTemplate6;
                    db.AddRespawnTimerSecs = addRespawnTimerSecs;
                    db.MasterGroupId = isPredefinedSpawns ? addsGroupmobId : null;
                    db.AddsRespawnCount = addsRespawnCountTotal;
                    db.PercentLifeAddsActivity = percentLifeAddsActivity;
                    db.LifePercentTriggerSpawn = lifePercentTriggerSpawn;
                    db.PercentageOfPlayerInRadius = percentageOfPlayersInRadius;

                    if (!ActiveGroupStatusAddsKey.Equals(activeDefaultGroupStatusAddsKey))
                    {
                        db.ActiveStatusId = ActiveGroupStatusAddsKey;
                    }

                    if (!InactiveGroupStatusAddsKey.Equals(inactiveDefaultGroupStatusAddsKey))
                    {
                        db.InactiveStatusId = InactiveGroupStatusAddsKey;
                    }

                    GameServer.Database.SaveObject(db);
                }
            }
        }

        private void UpdateMasterGroupInDatabase()
        {
            var masterGroup = GameServer.Database.SelectObjects<GroupMobDb>(DB.Column("GroupId").IsEqualTo(addsGroupmobId)).FirstOrDefault();
            if (masterGroup != null)
            {
                masterGroup.SlaveGroupId = SpawnerGroupId;

                //Set default interract if null
                if (masterGroup.GroupMobInteract_FK_Id == null)
                {
                    masterGroup.GroupMobInteract_FK_Id = activeDefaultGroupStatusAddsKey;
                }

                GameServer.Database.SaveObject(masterGroup);
            }
        }

        private void InstanciateMobs()
        {
            NpcTemplate[] templates = new NpcTemplate[6]
            {
                NpcTemplateMgr.GetTemplate(npcTemplate1),
                NpcTemplateMgr.GetTemplate(npcTemplate2),
                NpcTemplateMgr.GetTemplate(npcTemplate3),
                NpcTemplateMgr.GetTemplate(npcTemplate4),
                NpcTemplateMgr.GetTemplate(npcTemplate5),
                NpcTemplateMgr.GetTemplate(npcTemplate6)
            };
            if (percentageOfPlayersInRadius <= 0)
            {
                GameNPC[] npcs = new GameNPC[6];
                for (int i = 0; i < npcs.Length; i++)
                {
                    if (templates[i] != null)
                    {
                        foreach (var asm in ScriptMgr.GameServerScripts)
                        {
                            try
                            {
                                GameNPC npc = !String.IsNullOrEmpty(templates[i].ClassType) ? asm.CreateInstance(templates[i].ClassType, false) as GameNPC : new GameNPC();
                                npc.LoadTemplate(templates[i]);
                                npc.Name = templates[i].Name;
                                npcs[i] = npc;
                                break;
                            }
                            catch
                            {
                            }
                        }
                        if (npcs[i] == null)
                        {
                            log.Warn($"couldn't create class {templates[i].ClassType} for template {templates[i].TemplateId} for Spawner {InternalID}");
                        }
                    }
                }
                loadedAdds = npcs.Where(n => n != null).ToList();
            }
            else
            {
                int npcCount = (int)(GetPlayersInRadius(1000).Cast<GamePlayer>().Count(p => p.Client.Account.PrivLevel <= 1) / 100.0 * percentageOfPlayersInRadius + 0.5);
                List<NpcTemplate> validTemplates = templates.Where(t => t != null).ToList();
                if (npcCount <= 0 || !validTemplates.Any())
                {
                    return;
                }
                // Determine the number of mobs per template, we will split the remainder of the division between templates
                // For example : 8 NPCs to spawn between npcTemplate1, npcTemplate2, npcTemplate3 => 6 evenly split, 2 extras
                // The extras will be spread across the lowest valid templates (e.g. npcTemplate1, npcTemplate2, npcTemplate3 => 1 and 2 get an extra)
                GameNPC[] npcs = new GameNPC[npcCount];
                int maxSpawnsPerTemplate = Math.DivRem(npcCount, validTemplates.Count, out int remainder);
                int currentTemplateIdx = 0;
                int spawnsInTemplate = 0;
                for (int i = 0; i < npcCount; ++i)
                {
                    NpcTemplate template = validTemplates[currentTemplateIdx];
                    ++spawnsInTemplate;
                    foreach (var asm in ScriptMgr.GameServerScripts)
                    {
                        try
                        {
                            GameNPC npc = !String.IsNullOrEmpty(template.ClassType) ? asm.CreateInstance(template.ClassType, false) as GameNPC : new GameNPC();
                            npc.LoadTemplate(template);
                            npc.Name = template.Name;
                            npcs[i] = npc;
                            break;
                        }
                        catch
                        {
                        }
                    }
                    if (npcs[i] == null)
                    {
                        log.Warn($"couldn't create class {template.ClassType} for template {template.TemplateId} for Spawner {InternalID}");
                    }
                    if (!(spawnsInTemplate < maxSpawnsPerTemplate))
                    {
                        if (remainder > 0 && spawnsInTemplate == maxSpawnsPerTemplate) // We didn't have a perfect split, deduct one extra for the remainder of the division
                        {
                            --remainder;
                        }
                        else
                        {
                            spawnsInTemplate = 0;
                            ++currentTemplateIdx;
                        }
                    }
                }
                loadedAdds = npcs.Where(n => n != null).ToList();
            }

            if (!loadedAdds.Any())
            {
                return;
            }

            for (int i = 0; i < loadedAdds.Count; ++i)
            {
                SetCircularPosition(loadedAdds[i], i, loadedAdds.Count, WorldMgr.GIVE_ITEM_DISTANCE);
            }

            AddToMobGroupToNPCTemplates(loadedAdds);
        }

        private void SetCircularPosition(GameNPC npc, int index, int total, float distance)
        {
            double angle = (Math.PI * 2 * index) / total;

            npc.Position = Position + Vector.Create(Angle.Radians(angle), distance);
            npc.LoadedFromScript = true;
            npc.AddToWorld();
            npc.OwnerID = InternalID;
            if (Faction != null)
            {
                npc.Faction = FactionMgr.GetFactionByID(Faction.ID);
            }
        }

        private void AddSpawnerToMobGroup()
        {
            if (!MobGroupManager.Instance.Groups.ContainsKey(SpawnerGroupId))
            {
                MobGroupManager.Instance.AddMobToGroup(this, SpawnerGroupId, false);
            }
        }

        private void AddToMobGroupToNPCTemplates(IEnumerable<GameNPC> npcs)
        {
            addsGroupmobId = "spwn_add_" + (dbId != null ? dbId.Substring(0, 8) : Guid.NewGuid().ToString().Substring(0, 8));
            foreach (var npc in npcs)
            {
                MobGroupManager.Instance.AddMobToGroup(npc, addsGroupmobId, true);
            }

            GroupMobStatusDb status;
            bool active = percentLifeAddsActivity == 0 || HealthPercent <= percentLifeAddsActivity;
            if (active)
            {
                status = GetActiveStatus();
                isAddsActiveStatus = true;
                Task.Run(async () =>
                {
                    //Delay animation on mob added to world
                    await Task.Delay(500);
                    if (IsAlive && Brain is StandardMobBrain { HasAggro: true } myBrain)
                    {
                        lock (m_addsLock)
                        {
                            loadedAdds.ForEach(n =>
                            {
                                if (n.IsAlive && n.Brain is StandardMobBrain friendBrain)
                                {
                                    myBrain.AddAggroListTo(friendBrain);
                                }
                            });
                        }
                    }
                });
            }
            else
            {
                status = GetInativeStatus();
            }

            MobGroupManager.Instance.Groups[addsGroupmobId].SetGroupInfo(status, true, true);
        }

        private bool CanSpawnAdds()
        {
            if (loadedAdds != null)
                return false;
            if (npcAddsNextPopupTimeStamp != null && npcAddsNextPopupTimeStamp.Value < DateTime.Now)
                return false;
            if (lifePercentTriggerSpawn > 0 && lifePercentTriggerSpawn < HealthPercent)
                return false;
            return true;
        }

        public override void StartAttack(GameObject target)
        {
            base.StartAttack(target);

            lock (m_addsLock)
            {
                if (addsResetTimer != null)
                {
                    addsResetTimer.Stop();
                    addsResetTimer = null;
                }
                if (loadedAdds != null && Brain is StandardMobBrain myBrain)
                {
                    loadedAdds.ForEach(n =>
                    {
                        if (n.IsAlive && n.Brain is StandardMobBrain friendBrain)
                        {
                            foreach (var aggroEntry in myBrain.AggroTable)
                            {
                                friendBrain.AggroTable.TryAdd(aggroEntry.Key, aggroEntry.Value);
                            }
                        }
                    });
                }
                if (isAggroType && CanSpawnAdds())
                {
                    LoadAdds();
                }
            }
        }

        public override void TakeDamage(AttackData ad)
        {
            base.TakeDamage(ad);

            if (!ad.CausesCombat)
            {
                return;
            }

            if (!isAggroType && IsAlive)
            {
                lock (m_addsLock)
                {
                    if (addsResetTimer != null)
                    {
                        addsResetTimer.Stop();
                        addsResetTimer = null;
                    }

                    if (CanSpawnAdds())
                    {
                        LoadAdds();
                    }

                    if (!isAddsActiveStatus && (percentLifeAddsActivity == 0 || HealthPercent <= percentLifeAddsActivity))
                    {
                        ActivateAdds();
                    }
                }
            }
        }

        private void ActivateAdds()
        {
            if (addsGroupmobId == null || !MobGroupManager.Instance.Groups.TryGetValue(addsGroupmobId, out MobGroup mobGroup))
                return;

            isAddsActiveStatus = true;
            if (isPredefinedSpawns)
            {
                mobGroup.ResetGroupInfo(true);
            }
            else
            {
                mobGroup.SetGroupInfo(GetActiveStatus(), true, true);
            }

            Task.Run(async () =>
            {
                // Delay attacks
                await Task.Delay(500);
                if (IsAlive)
                {
                    lock (m_addsLock)
                    {
                        loadedAdds?.ForEach(n =>
                        {
                            if (n.IsAlive && n.Brain is StandardMobBrain friendBrain && Brain is StandardMobBrain myBrain)
                            {
                                myBrain.AddAggroListTo(friendBrain);
                            }
                        });
                    }
                }
            });
        }

        /// <summary>
        /// Load adds - all preconditions must be met
        /// </summary>
        public void LoadAdds()
        {
            if (isPredefinedSpawns)
            {
                if (addsGroupmobId != null && MobGroupManager.Instance.Groups.TryGetValue(addsGroupmobId, out MobGroup mobGroup))
                {
                    mobGroup.ReloadMobsFromDatabase();
                    loadedAdds = mobGroup.NPCs;
                    Task.Run(async () =>
                    {
                        //Delay animation on mob added to world
                        await Task.Delay(500);
                        if (IsAlive && Brain is StandardMobBrain { HasAggro: true } myBrain)
                        {
                            lock (m_addsLock)
                            {
                                mobGroup.ResetGroupInfo(true);
                                loadedAdds.ForEach(n =>
                                {
                                    if (n.IsAlive && n.Brain is StandardMobBrain friendBrain)
                                    {
                                        foreach (var aggroEntry in myBrain.AggroTable)
                                        {
                                            friendBrain.AggroTable.TryAdd(aggroEntry.Key, aggroEntry.Value);
                                        }
                                    }
                                });
                            }
                        }
                    });
                }
            }
            else
            {
                InstanciateMobs();
            }

            if (loadedAdds is { Count: >0 })
            {
                GameEventMgr.AddHandler(GameEvents.GroupMobEvent.MobGroupDead, OnGroupMobDead);
                if (Group == null)
                {
                    Group = new Group(this);
                    Group.AddMember(this);
                }
                loadedAdds.ForEach(n =>
                {
                    Group.AddMember(n);
                    n.AutoRespawn = false;
                    n.RespawnInterval = addRespawnTimerSecs * 1000;
                });
            }

            npcAddsNextPopupTimeStamp = DateTime.Now.AddSeconds(addRespawnTimerSecs);
        }

        private void CleanupAddsUnsafe()
        {
            if (loadedAdds != null)
            {
                if (addsGroupmobId != null && !isPredefinedSpawns)
                {
                    MobGroupManager.Instance.RemoveGroupsAndMobs(addsGroupmobId, true);
                }
                GameEventMgr.RemoveHandler(GameEvents.GroupMobEvent.MobGroupDead, OnGroupMobDead);
                if (this.Group != null && this.Group.LivingLeader == this)
                {
                    this.Group.DisbandGroup();
                    this.Group = null;
                }
                loadedAdds.ForEach(add =>
                {
                    add.RemoveFromWorld();
                    add.Delete();
                });
                loadedAdds = null;
            }
        }

        protected void Cleanup()
        {
            // Cleanup previous adds
            lock (m_addsLock)
            {
                //reset adds currentCount respawn
                addsRespawnCurrentCount = 0;
                npcAddsNextPopupTimeStamp = null;
                isAddsActiveStatus = false;
                if (addsResetTimer != null)
                {
                    addsResetTimer.Stop();
                    addsResetTimer = null;
                }
                CleanupAddsUnsafe();

                //reset groupinfo
                if (MobGroupManager.Instance.Groups.TryGetValue(SpawnerGroupId, out var spawnerGroup))
                {
                    spawnerGroup.ResetGroupInfo(true);
                }
            }
        }

        public override void Reset()
        {
            base.Reset();

            lock (m_addsLock)
            {
                if (loadedAdds != null && addsResetTimer == null)
                {
                    addsResetTimer = new RegionTimer(this, timer =>
                    {
                        Cleanup();
                        return 0;
                    }, MILLISECONDS_BEFORE_RESET_ADDS);
                }
            }
        }

        public void OnGroupMobDead(DOLEvent e, object sender, EventArgs arguments)
        {
            //check group
            if (sender is not MobGroup senderGroup)
            {
                return;
            }

            //Check if npc is in combat to allow respawn only in this case
            if (InCombat && String.Equals(senderGroup.GroupId, addsGroupmobId))
            {
                //Check if group can respawn
                if (addsRespawnCountTotal > 0 && addRespawnTimerSecs > 0 && addsRespawnCurrentCount < addsRespawnCountTotal)
                {
                    addsRespawnCurrentCount++;
                    foreach (var npc in senderGroup.NPCs)
                    {
                        npc.StartRespawn();
                    }
                }
                else
                {
                    lock (m_addsLock)
                    {
                        CleanupAddsUnsafe();
                    }
                }
            }
        }

        public override void Die(GameObject killer)
        {
            lock (m_addsLock)
            {
                if (loadedAdds != null)
                {
                    GameEventMgr.RemoveHandler(GameEvents.GroupMobEvent.MobGroupDead, OnGroupMobDead);
                    loadedAdds.ForEach(n =>
                    {
                        if (n.IsAlive)
                        {
                            n.Die(this);
                        }
                    });
                    addsRespawnCurrentCount = 0;
                    npcAddsNextPopupTimeStamp = null;
                    isAddsActiveStatus = false;
                }
            }
            base.Die(killer);
        }


        private GroupMobStatusDb GetInativeStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(InactiveGroupStatusAddsKey));

            if (result != null && result.Any())
            {
                return result.First();
            }
            //Default
            var inactiveDefaultList = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(inactiveDefaultGroupStatusAddsKey));
            GroupMobStatusDb inactiveStatus = inactiveDefaultList?.FirstOrDefault();

            if (inactiveStatus == null)
            {
                eFlags f = eFlags.PEACE | eFlags.CANTTARGET;

                inactiveStatus = new GroupMobStatusDb()
                {
                    Flag = (int)f,
                    SetInvincible = true.ToString(),
                    GroupStatusId = InactiveGroupStatusAddsKey
                };
                GameServer.Database.AddObject(inactiveStatus);
            }

            return inactiveStatus;
        }


        public override bool AddToWorld()
        {
            base.AddToWorld();
            Cleanup();
            //register handler
            return true;
        }

        /// <inheritdoc />
        public override bool RemoveFromWorld()
        {
            Cleanup();
            return base.RemoveFromWorld();
        }


        private GroupMobStatusDb GetActiveStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(ActiveGroupStatusAddsKey));
            if (result != null && result.Any())
            {
                return result.First();
            }

            var activeDefaultList = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(activeDefaultGroupStatusAddsKey));
            GroupMobStatusDb activeStatus = activeDefaultList?.FirstOrDefault();

            if (activeStatus == null)
            {
                activeStatus = new GroupMobStatusDb()
                {
                    SetInvincible = false.ToString(),
                    Flag = 0,
                    GroupStatusId = ActiveGroupStatusAddsKey
                };
                GameServer.Database.AddObject(activeStatus);
            }

            return activeStatus;
        }
    }
}
