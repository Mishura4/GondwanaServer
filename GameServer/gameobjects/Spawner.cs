using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string inactiveDefaultGroupStatusAddsKey = "Spawner_inactive_adds";
        private readonly string activeDefaultGroupStatusAddsKey = "Spawner_active_adds";
        public string inactiveGroupStatusAddsKey;
        private string activeGroupStatusAddsKey;
        private string dbId;
        private List<GameNPC> loadedAdds;
        private bool isAddsGroupMasterGroup;
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
                return "spwn_" + (this.dbId != null ? this.dbId.Substring(0, 8) : Guid.NewGuid().ToString().Substring(0, 8));
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
                    this.dbId = db.ObjectId;
                    this.percentLifeAddsActivity = db.PercentLifeAddsActivity;
                    this.lifePercentTriggerSpawn = db.LifePercentTriggerSpawn;
                    this.addRespawnTimerSecs = db.AddRespawnTimerSecs;
                    this.percentageOfPlayersInRadius = db.PercentageOfPlayerInRadius;
                    this.isAggroType = db.IsAggroType;
                    this.npcTemplate1 = db.NpcTemplate1;
                    this.npcTemplate2 = db.NpcTemplate2;
                    this.npcTemplate3 = db.NpcTemplate3;
                    this.npcTemplate4 = db.NpcTemplate4;
                    this.npcTemplate5 = db.NpcTemplate5;
                    this.npcTemplate6 = db.NpcTemplate6;
                    if (this.isAggroType && lifePercentTriggerSpawn is > 0 and < 100)
                    {
                        log.Warn($"spawner {this.InternalID} is marked as spawning on aggro, but also has a lifePercentTriggerSpawn; it will be set to not spawn on aggro");
                        this.isAggroType = false;
                    }

                    if (db.MasterGroupId != null)
                    {
                        this.npcTemplate1 = -1;
                        this.npcTemplate2 = -1;
                        this.npcTemplate3 = -1;
                        this.npcTemplate4 = -1;
                        this.npcTemplate5 = -1;
                        this.npcTemplate6 = -1;
                        this.isAddsGroupMasterGroup = true;
                        this.addsGroupmobId = db.MasterGroupId;

                        //add Spawner to GroupMob for interractions
                        var spawnerGroup = GameServer.Database.SelectObjects<GroupMobDb>(DB.Column("GroupId").IsEqualTo(this.SpawnerGroupId)).FirstOrDefault();

                        if (spawnerGroup == null)
                        {
                            this.AddSpawnerToMobGroup();
                        }

                        this.UpdateMasterGroupInDatabase();
                    }

                    this.addsRespawnCountTotal = db.AddsRespawnCount;
                    this.addsRespawnCurrentCount = 0;

                    if (db.ActiveStatusId != null)
                        this.activeGroupStatusAddsKey = db.ActiveStatusId;

                    if (db.InactiveStatusId != null)
                        this.inactiveGroupStatusAddsKey = db.InactiveStatusId;
                }
            }
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();

            var result = GameServer.Database.SelectObjects<SpawnerTemplate>(DB.Column("MobID").IsEqualTo(this.dbId));

            if (result != null)
            {
                var db = result.FirstOrDefault();

                if (db != null)
                {
                    db.IsAggroType = this.isAggroType;
                    db.NpcTemplate1 = this.npcTemplate1;
                    db.NpcTemplate2 = this.npcTemplate2;
                    db.NpcTemplate3 = this.npcTemplate3;
                    db.NpcTemplate4 = this.npcTemplate4;
                    db.NpcTemplate5 = this.npcTemplate5;
                    db.NpcTemplate6 = this.npcTemplate6;
                    db.AddRespawnTimerSecs = this.addRespawnTimerSecs;
                    db.MasterGroupId = this.isAddsGroupMasterGroup ? this.addsGroupmobId : null;
                    db.AddsRespawnCount = this.addsRespawnCountTotal;
                    db.PercentLifeAddsActivity = this.percentLifeAddsActivity;
                    db.LifePercentTriggerSpawn = this.lifePercentTriggerSpawn;
                    db.PercentageOfPlayerInRadius = this.percentageOfPlayersInRadius;

                    if (!this.ActiveGroupStatusAddsKey.Equals(this.activeDefaultGroupStatusAddsKey))
                    {
                        db.ActiveStatusId = this.ActiveGroupStatusAddsKey;
                    }

                    if (!this.InactiveGroupStatusAddsKey.Equals(this.inactiveDefaultGroupStatusAddsKey))
                    {
                        db.InactiveStatusId = this.InactiveGroupStatusAddsKey;
                    }

                    GameServer.Database.SaveObject(db);
                }
            }
        }


        /// <summary>
        /// Method effective for NpcTemplates pops only
        /// </summary>
        private void ClearNPCTemplatesOldMobs()
        {
            if (this.addsGroupmobId != null && this.loadedAdds != null && MobGroupManager.Instance.Groups.ContainsKey(this.addsGroupmobId))
            {
                MobGroupManager.Instance.RemoveGroupsAndMobs(this.addsGroupmobId, true);
                lock (m_addsLock)
                {
                    loadedAdds = null;
                }
            }
        }

        private void UpdateMasterGroupInDatabase()
        {
            var masterGroup = GameServer.Database.SelectObjects<GroupMobDb>(DB.Column("GroupId").IsEqualTo(this.addsGroupmobId)).FirstOrDefault();
            if (masterGroup != null)
            {
                masterGroup.SlaveGroupId = this.SpawnerGroupId;

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
                            log.Warn($"couldn't create class {templates[i].ClassType} for template {templates[i].TemplateId} for Spawner {this.InternalID}");
                        }
                    }
                }
                loadedAdds = npcs.Where(n => n != null).ToList();
            }
            else
            {
                int npcCount = (int)(GetPlayersInRadius(1000).Cast<GamePlayer>().Count() / 100.0 * percentageOfPlayersInRadius + 0.5);
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
                        log.Warn($"couldn't create class {template.ClassType} for template {template.TemplateId} for Spawner {this.InternalID}");
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
            float xOffset = (float)Math.Cos(angle) * distance;
            float yOffset = (float)Math.Sin(angle) * distance;

            npc.Position = new System.Numerics.Vector3(this.Position.X + xOffset, this.Position.Y + yOffset, this.Position.Z);
            npc.Heading = this.Heading;
            npc.RespawnInterval = -1;
            npc.CurrentRegion = WorldMgr.GetRegion(this.CurrentRegionID);
            npc.AddToWorld();
            npc.OwnerID = this.InternalID;
            if (this.Faction != null)
            {
                npc.Faction = FactionMgr.GetFactionByID(this.Faction.ID);
            }
        }

        private void AddSpawnerToMobGroup()
        {
            if (!MobGroupManager.Instance.Groups.ContainsKey(this.SpawnerGroupId))
            {
                MobGroupManager.Instance.AddMobToGroup(this, this.SpawnerGroupId, false);
            }
        }

        private void AddToMobGroupToNPCTemplates(IEnumerable<GameNPC> npcs)
        {
            this.addsGroupmobId = "spwn_add_" + (this.dbId != null ? this.dbId.Substring(0, 8) : Guid.NewGuid().ToString().Substring(0, 8));
            foreach (var npc in npcs)
            {
                MobGroupManager.Instance.AddMobToGroup(npc, this.addsGroupmobId, true);
            }

            GroupMobStatusDb status;
            bool active = this.percentLifeAddsActivity == 0 || this.HealthPercent <= this.percentLifeAddsActivity;
            if (active)
            {
                status = this.GetActiveStatus();
                isAddsActiveStatus = true;
            }
            else
            {
                status = this.GetInativeStatus();
            }

            Task.Run(async () =>
            {
                //Delay animation on mob added to world
                await Task.Delay(500);
                MobGroupManager.Instance.Groups[this.addsGroupmobId].SetGroupInfo(status, true, true);
                if (active && IsAlive && Brain is StandardMobBrain { HasAggro: true } myBrain)
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
                if (isAggroType && CanSpawnAdds())
                {
                    this.LoadAdds();
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

            if (!this.isAggroType && IsAlive)
            {
                lock (m_addsLock)
                {
                    if (CanSpawnAdds())
                    {
                        this.LoadAdds();
                    }

                    if (!isAddsActiveStatus && (this.percentLifeAddsActivity == 0 || this.HealthPercent <= this.percentLifeAddsActivity))
                    {
                        ActivateAdds();
                    }
                }
            }
        }

        private void ActivateAdds()
        {
            if (addsGroupmobId != null && MobGroupManager.Instance.Groups.ContainsKey(addsGroupmobId))
            {
                this.isAddsActiveStatus = true;
                if (this.isAddsGroupMasterGroup)
                {
                    MobGroupManager.Instance.Groups[this.addsGroupmobId].ResetGroupInfo(true);
                }
                else
                {
                    MobGroupManager.Instance.Groups[this.addsGroupmobId].SetGroupInfo(this.GetActiveStatus(), false, true);
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
                                if (n.IsAlive && n.Brain is StandardMobBrain friendBrain && this.Brain is StandardMobBrain myBrain)
                                {
                                    myBrain.AddAggroListTo(friendBrain);
                                }
                            });
                        }
                    }
                });
            }
        }

        //Load adds if respawn is passed
        public void LoadAdds()
        {
            if (isAddsGroupMasterGroup)
            {
                if (this.addsGroupmobId != null && MobGroupManager.Instance.Groups.TryGetValue(this.addsGroupmobId, out MobGroup mobGroup))
                {
                    mobGroup.ReloadMobsFromDatabase();
                    loadedAdds = mobGroup.NPCs;
                    loadedAdds.ForEach(n => n.Spawn());

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
                                        myBrain.AddAggroListTo(friendBrain);
                                    }
                                });
                            }
                        }
                    });
                }
            }
            else
            {
                this.InstanciateMobs();
            }

            this.npcAddsNextPopupTimeStamp = DateTime.Now.AddSeconds(this.addRespawnTimerSecs);
        }

        protected void Cleanup()
        {
            // Cleanup previous adds
            lock (m_addsLock)
            {
                if (loadedAdds != null)
                {
                    loadedAdds.ForEach(add =>
                    {
                        add.RemoveFromWorld();
                        add.Delete();
                    });
                    loadedAdds = null;
                }
                if (this.isAddsGroupMasterGroup && addsGroupmobId != null)
                {
                    //remove mastergroup mob if present
                    if (!MobGroupManager.Instance.Groups.ContainsKey(this.addsGroupmobId))
                    {
                        //on server load add groups to remove list
                        MobGroupManager.Instance.GroupsToRemoveOnServerLoad.Add(this.addsGroupmobId);
                    }

                    //reset groupinfo
                    if (MobGroupManager.Instance.Groups.TryGetValue(this.SpawnerGroupId, out var spawnerGroup))
                    {
                        spawnerGroup.ResetGroupInfo(true);
                    }
                }
                else
                {
                    //Handle repop by clearing npctemplate pops
                    this.ClearNPCTemplatesOldMobs();
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            Cleanup();
            //reset adds currentCount respawn
            this.addsRespawnCurrentCount = 0;
            this.npcAddsNextPopupTimeStamp = null;
            this.isAddsActiveStatus = false;
        }

        public void OnGroupMobDead(DOLEvent e, object sender, EventArgs arguments)
        {
            //check group
            MobGroup senderGroup = sender as MobGroup;

            //Check is npc is in combat to allow respawn only in this case
            if (senderGroup != null && senderGroup.GroupId.Equals(this.addsGroupmobId) && this.InCombat)
            {
                //own group is dead
                this.isAddsActiveStatus = false;

                bool respawnValueIsCorrect = false;
                int respawnTimeInMs = addRespawnTimerSecs * 1000;
                if (respawnTimeInMs > 0 && respawnTimeInMs < int.MaxValue)
                {
                    respawnValueIsCorrect = true;
                }

                //Check if group can respawn
                if (this.addsRespawnCountTotal > 0 && this.addsRespawnCurrentCount < this.addsRespawnCountTotal && respawnValueIsCorrect)
                {
                    this.addsRespawnCurrentCount++;
                    foreach (var npc in MobGroupManager.Instance.Groups[this.addsGroupmobId].NPCs)
                    {
                        npc.RespawnInterval = respawnTimeInMs;
                        npc.StartRespawn();
                        npc.RespawnInterval = -1;
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
                    loadedAdds.ForEach(n =>
                    {
                        n.CanRespawn = false;
                        n.Die(this);
                    });
                    loadedAdds = null;
                }
            }
            base.Die(killer);
        }


        private GroupMobStatusDb GetInativeStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(this.InactiveGroupStatusAddsKey));

            if (result != null && result.Any())
            {
                return result.First();
            }
            //Default
            var inactiveDefaultList = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(this.inactiveDefaultGroupStatusAddsKey));
            GroupMobStatusDb inactiveStatus = inactiveDefaultList?.FirstOrDefault();

            if (inactiveStatus == null)
            {
                eFlags f = eFlags.PEACE | eFlags.CANTTARGET;

                inactiveStatus = new GroupMobStatusDb()
                {
                    Flag = (int)f,
                    SetInvincible = true.ToString(),
                    GroupStatusId = this.InactiveGroupStatusAddsKey
                };
                GameServer.Database.AddObject(inactiveStatus);
            }

            return inactiveStatus;
        }


        public override bool AddToWorld()
        {
            base.AddToWorld();

            //register handler
            GameEventMgr.AddHandler(GameEvents.GroupMobEvent.MobGroupDead, this.OnGroupMobDead);
            return true;
        }


        /// <inheritdoc />
        public override void Delete()
        {
            base.Delete();
        }

        /// <inheritdoc />
        public override bool RemoveFromWorld()
        {
            Cleanup();
            GameEventMgr.RemoveHandler(GameEvents.GroupMobEvent.MobGroupDead, this.OnGroupMobDead);
            return base.RemoveFromWorld();
        }


        private GroupMobStatusDb GetActiveStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(this.ActiveGroupStatusAddsKey));
            if (result != null && result.Any())
            {
                return result.First();
            }

            var activeDefaultList = GameServer.Database.SelectObjects<GroupMobStatusDb>(DB.Column("GroupStatusId").IsEqualTo(this.activeDefaultGroupStatusAddsKey));
            GroupMobStatusDb activeStatus = activeDefaultList?.FirstOrDefault();

            if (activeStatus == null)
            {
                activeStatus = new GroupMobStatusDb()
                {
                    SetInvincible = false.ToString(),
                    Flag = 0,
                    GroupStatusId = this.ActiveGroupStatusAddsKey
                };
                GameServer.Database.AddObject(activeStatus);
            }

            return activeStatus;
        }
    }
}
