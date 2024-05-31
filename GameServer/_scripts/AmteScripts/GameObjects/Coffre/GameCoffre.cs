using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Numerics;
using DOL.Database;
using DOL.GameEvents;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Database.Attributes;
using DOL.Territories;
using log4net;
using DOL.MobGroups;
using DOL.Events;
using DOL.GS.GameEvents;

namespace DOL.GS.Scripts
{
    public class GameCoffre : GameStaticItem
    {
        public const string CROCHET = "Crochet"; //Id_nb des crochets
        public const int UNLOCK_TIME = 10; //Temps pour crocheter une serrure en secondes
        public readonly int LARGE_ITEM_DIST = 500;
        private GamePlayer m_interactPlayer;
        private DateTime m_lastInteract;
        public ushort TPID { get; set; }
        public bool ShouldRespawnToTPID { get; set; }
        public bool PickOnTouch { get; set; }
        public bool IsOpenableOnce { get; set; }
        public bool IsTerritoryLinked { get; set; }
        public int KeyLoseDur { get; set; }
        public string SwitchFamily { get; set; }
        public int SwitchOrder { get; set; }
        public bool IsSwitch { get; set; }
        public int SecondaryModel { get; set; }
        public string SwitchTriggerEventID { get; set; }

        private bool isActivated;
        private Timer proximityTimer;
        private Timer activationTimer;
        public int ActivatedDuration { get; set; }

        private void RespawnToTPID()
        {
            if (ShouldRespawnToTPID)
            {
                DBTP tp = GameServer.Database.SelectObject<DBTP>(DB.Column("TPID").IsEqualTo(this.TPID));
                if (tp != null)
                {
                    Position = GetPositionFromTPID(tp);
                }
            }
            AddToWorld();
        }

        private Vector3 GetPositionFromTPID(DBTP tp)
        {
            TPPoint currentTPPoint = new TPPoint(new Vector3(Position.X, Position.Y, Position.Z), (eTPPointType)tp.TPType);
            TPPoint nextTPPoint = currentTPPoint.GetNextTPPoint();

            return nextTPPoint != null ? new Vector3((float)nextTPPoint.Position.X, (float)nextTPPoint.Position.Y, (float)nextTPPoint.Position.Z) : Position;
        }

        private void ShowSecondaryModel()
        {
            if (SecondaryModel > 0 && IsSwitch)
            {
                Model = (ushort)SecondaryModel;
            }
        }

        private void RevertToPrimaryModel()
        {
            if (SecondaryModel > 0)
            {
                Model = Coffre.Model;
            }
        }

        private bool HasPlayerOpened(GamePlayer player)
        {
            var openedCoffre = GameServer.Database.SelectObject<CoffrexPlayer>(DB.Column("PlayerID").IsEqualTo(player.InternalID).And(DB.Column("CoffreID").IsEqualTo(this.InternalID)));
            return openedCoffre != null;
        }

        private void MarkCoffreAsOpenedByPlayer(GamePlayer player)
        {
            var newEntry = new CoffrexPlayer { PlayerID = player.InternalID, CoffreID = this.InternalID };
            GameServer.Database.AddObject(newEntry);
        }

        #region Variables - Constructeur
        private DBCoffre Coffre;
        private IList<CoffreItem> m_Items;
        public IList<CoffreItem> Items
        {
            get { return m_Items; }
        }

        public Timer RespawnTimer
        {
            get;
            set;
        }

        public static List<GameCoffre> Coffres;

        private int m_ItemChance;
        /// <summary>
        /// Pourcentage de chance de trouver un item dans le coffre (si 0 alors 50% de chance)
        /// </summary>
        public int ItemChance
        {
            get { return m_ItemChance; }
            set
            {
                if (value > 100)
                    m_ItemChance = 100;
                else if (value < 0)
                    m_ItemChance = 0;
                else
                    m_ItemChance = value;
            }
        }

        public override bool IsCoffre
        {
            get
            {
                return true;
            }
        }

        public int TrapRate
        {
            get;
            set;
        }

        public string NpctemplateId
        {
            get;
            set;
        }

        public int TpX
        {
            get;
            set;
        }

        public int TpY
        {
            get;
            set;
        }

        public int TpZ
        {
            get;
            set;
        }

        public int TPHeading
        {
            get;
            set;
        }

        public bool IsTeleporter
        {
            get;
            set;
        }

        public bool HasPickableAnim
        {
            get;
            set;
        }

        public int TpLevelRequirement
        {
            get;
            set;
        }

        public bool TpIsRenaissance
        {
            get;
            set;
        }

        public bool IsLargeCoffre
        {
            get;
            set;
        }
        public string RemovedByEventID
        {
            get;
            set;
        }

        public int TpEffect
        {
            get;
            set;
        }

        public int TpRegion
        {
            get;
            set;
        }

        public bool IsOpeningRenaissanceType
        {
            get;
            set;
        }

        public int PunishSpellId
        {
            get;
            set;
        }


        public int CoffreOpeningInterval
        {
            get;
            set;
        }

        public DateTime? LastTimeChecked
        {
            get;
            set;
        }

        public int AllChance
        {
            get
            {
                return m_Items.Sum(item => item.Chance);
            }
        }
        public DateTime LastOpen;
        /// <summary>
        /// Temps de réapparition d'un item (en minutes)
        /// </summary>
        public int ItemInterval;
        public string KeyItem = "";
        public int LockDifficult;

        public GameCoffre()
        {
            LastOpen = DateTime.MinValue;
            ItemInterval = 5;
            m_Items = new List<CoffreItem>();
        }

        public GameCoffre(IList<CoffreItem> Items)
        {
            HasPickableAnim = true;
            LastOpen = DateTime.MinValue;
            HasPickableAnim = true;
            ItemInterval = 5;
            m_Items = Items;
        }
        #endregion

        public override bool AddToWorld()
        {
            base.AddToWorld();

            if (Coffres == null)
            {
                Coffres = new List<GameCoffre>();
            }

            Coffres.Add(this);

            if (PickOnTouch || ItemInterval > 0)
            {
                proximityTimer = new Timer(1500);
                proximityTimer.Elapsed += (sender, e) => CheckPlayerProximity();
                proximityTimer.Start();
            }

            return true;
        }

        public override bool RemoveFromWorld()
        {
            if (proximityTimer != null)
            {
                proximityTimer.Stop();
                proximityTimer.Dispose();
                proximityTimer = null;
            }

            Coffres.Remove(this);
            return base.RemoveFromWorld();
        }

        private void CheckPlayerProximity()
        {
           if (PickOnTouch)
            {
                foreach (GamePlayer player in GetPlayersInRadius(90))
                {
                    if (player.IsAlive && IsWithinRadius(player, 90))
                    {
                        if (IsSwitch)
                        {
                            CheckSwitchActivation(player);
                        }
                        else
                        {
                            InteractEnd(player);
                        }
                    }
                }
            }
        }

        private void CheckSwitchActivation(GamePlayer player)
        {
            if (!IsSwitch || isActivated) return;

            var switchesInFamily = Coffres.Where(c => c.SwitchFamily == SwitchFamily).OrderBy(c => c.SwitchOrder).ToList();
            int currentIndex = switchesInFamily.IndexOf(this);

            if (currentIndex == -1 || (currentIndex > 0 && !switchesInFamily[currentIndex - 1].isActivated))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.SwitchCannotActivate"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            isActivated = true;
            ShowSecondaryModel();
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.SwitchActivated"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            if (switchesInFamily.All(c => c.isActivated))
            {
                ActivateSwitchFamily(player);
            }
        }

        private void ActivateSwitchFamily(GamePlayer player)
        {
            OpenLinkedDoors();
            /*StartLinkedEvents();*/
            KillLinkedMobs();

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.SwitchAllActivated"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            GameEventMgr.Notify(SwitchEvent.SwitchActivated, this);

            if (!string.IsNullOrEmpty(SwitchTriggerEventID))
            {
                var gameEvent = GameEventManager.Instance.GetEventByID(SwitchTriggerEventID);
                if (gameEvent != null)
                {
                    GameEventManager.Instance.StartEvent(gameEvent);
                }
            }

            if (ActivatedDuration > 0)
            {
                activationTimer = new Timer(ActivatedDuration * 1000);
                activationTimer.Elapsed += (sender, e) => DeactivateSwitchFamily();
                activationTimer.Start();
            }
        }

        private void OpenLinkedDoors()
        {
            var doorsToOpen = DoorMgr.GetDoorsBySwitchFamily(SwitchFamily);
            foreach (var door in doorsToOpen)
            {
                if (door is GameDoor gameDoor)
                {
                    gameDoor.UnlockBySwitch();
                }
            }
        }

        private void DeactivateSwitchFamily()
        {
            activationTimer.Stop();

            foreach (var switchCoffre in Coffres.Where(c => c.SwitchFamily == SwitchFamily))
            {
                switchCoffre.isActivated = false;
                switchCoffre.RevertToPrimaryModel();
            }

            RevertDoors();

            var mobsToRespawn = MobGroupManager.Instance.GetMobsBySwitchFamily(SwitchFamily);
            foreach (var mob in mobsToRespawn)
            {
                mob.AddToWorld();
            }
        }

        private void RevertDoors()
        {
            var doorsToRevert = DoorMgr.GetDoorsBySwitchFamily(SwitchFamily);
            foreach (var door in doorsToRevert)
            {
                if (door is GameDoor gameDoor)
                {
                    gameDoor.LockBySwitch();
                }
            }
        }

        /*private void StartLinkedEvents()
        {
            var eventsToStart = GameEventManager.Instance.GetEventsBySwitchFamily(SwitchFamily);
            foreach (var ev in eventsToStart)
            {
                GameEventManager.Instance.StartEvent(ev);
            }
        }*/

        private void KillLinkedMobs()
        {
            var mobsToKill = MobGroupManager.Instance.GetMobsBySwitchFamily(SwitchFamily);
            foreach (var mob in mobsToKill)
            {
                mob.Die(null);
            }
        }

        /// <summary>
        ///  Get Coffres and their new EventIds from Db
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Tuple<GameStaticItem, string>> GetCoffresUsedInEventsInDb(ushort region)
        {
            var coffres = GameServer.Database.SelectObjects<DBCoffre>(DB.Column("EventID").IsNotNull().And(DB.Column("Region").IsEqualTo(region)));

            if (coffres == null)
            {
                return null;
            }

            List<Tuple<GameStaticItem, string>> values = new List<Tuple<GameStaticItem, string>>();

            foreach (var dbCoffre in coffres)
            {
                GameCoffre coffreInRegion = WorldMgr.Regions[region].Objects.FirstOrDefault(o => o != null && o as GameCoffre != null && o.InternalID.Equals(dbCoffre.ObjectId)) as GameCoffre;

                if (coffreInRegion != null)
                {
                    values.Add(new Tuple<GameStaticItem, string>(coffreInRegion, dbCoffre.EventID));
                }
            }

            return values;
        }

        public void RespawnCoffre()
        {
            if (ShouldRespawnToTPID)
            {
                RespawnToTPID();
            }
            else
            {
                base.AddToWorld();
            }
        }

        #region Interact - GetRandomItem
        public override bool Interact(GamePlayer player)
        {
            if (PickOnTouch && this.IsWithinRadius(player, 80))
            {
                return InteractEnd(player);
            }

            if (IsSwitch)
            {
                CheckSwitchActivation(player);
                return true;
            }

            if (IsOpenableOnce && HasPlayerOpened(player))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.UniqueTreasureTaken"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (IsTerritoryLinked && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.TerritoryNotOwned"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (!base.Interact(player) || !player.IsAlive) return false;
            if (!this.IsWithinRadius(player, (IsLargeCoffre ? LARGE_ITEM_DIST : WorldMgr.GIVE_ITEM_DISTANCE + 60))) return false;

            if (IsOpenableOnce)
            {
                MarkCoffreAsOpenedByPlayer(player);
            }

            if (HasPickableAnim)
            {
                foreach (GamePlayer otherPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (otherPlayer != null)
                        otherPlayer.Out.SendEmoteAnimation(player, eEmote.PlayerPickup);
                }
            }

            if (LockDifficult > 0 || KeyItem != "")
            {
                if (m_interactPlayer != null && player != m_interactPlayer && (m_lastInteract.Ticks + 200000000) > DateTime.Now.Ticks)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.AlreadyOpen"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return true;
                }
                if (KeyItem != "" && player.Inventory.GetFirstItemByID(KeyItem, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
                {
                    InventoryItem it = player.Inventory.GetFirstItemByID(KeyItem, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                    if (!KeyItem.StartsWith("oneuse"))
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.Oneuse") + player.Inventory.GetFirstItemByID(KeyItem, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack).Name + ".", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        if (KeyLoseDur > 0)
                        {
                            it.Durability -= KeyLoseDur;
                            if (it.Durability <= 0)
                            {
                                player.Inventory.RemoveItem(it);
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.KeyDestroyed"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                player.Out.SendInventoryItemsUpdate(new InventoryItem[] { it });
                            }
                        }
                    }
                    else
                    {
                        player.TempProperties.setProperty("CoffreItem", it);
                        player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client, "GameChest.UseItem", it.Name), OneUseOpen);
                        m_interactPlayer = player;
                        m_lastInteract = DateTime.Now;
                        return true;
                    }
                }
                else if (LockDifficult > 0 && player.Inventory.GetFirstItemByID(CROCHET, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
                {
                    player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client, "GameChest.PickLock"), Unlock);
                    m_interactPlayer = player;
                    m_lastInteract = DateTime.Now;
                    return true;
                }
                else
                {
                    if (LockDifficult == 0 && KeyItem != "")
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.LockDifficult1"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    else if (LockDifficult > 0 && KeyItem == "")
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.LockDifficult2"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    else
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.LockDifficult3"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return true;
                }
            }

            return InteractEnd(player);
        }

        private void OneUseOpen(GamePlayer player, byte response)
        {
            if (response == 0x00) return;
            InventoryItem it = player.TempProperties.getProperty<InventoryItem>("CoffreItem", null);
            player.TempProperties.removeProperty("CoffreItem");
            if (it == null || m_interactPlayer != player || !player.Inventory.RemoveCountFromStack(it, 1))
            {
                if (m_interactPlayer == player)
                {
                    m_interactPlayer = null;
                    m_lastInteract = DateTime.MinValue;
                }
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.ItemError"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Loot, it, 1);
            InteractEnd(player);
        }

        private bool InteractEnd(GamePlayer player)
        {
            bool gotItemOrUsedTeleporter = false;
            if (!IsTeleporter)
            {
                if (this.CoffreOpeningInterval == 0 || !this.LastTimeChecked.HasValue || (DateTime.Now - this.LastTimeChecked.Value) > TimeSpan.FromMinutes(this.CoffreOpeningInterval))
                {
                    this.LastTimeChecked = DateTime.Now;
                    CoffreItem coffre = GetRandomItem();
                    if (coffre.Id_nb == "" && coffre.Chance == 0)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.NothingInteresting1"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    else
                    {
                        ItemTemplate item = GameServer.Database.SelectObject<ItemTemplate>(DB.Column("Id_nb").IsEqualTo(GameServer.Database.Escape(coffre.Id_nb)));
                        if (item == null)
                        {
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.NothingInteresting2"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            coffre.Chance = 0;
                        }
                        else
                        {
                            if (!IsOpeningRenaissanceType)
                            {
                                gotItemOrUsedTeleporter = GetItem(player, item);
                            }
                            else
                            {
                                if (player.IsRenaissance)
                                {
                                    gotItemOrUsedTeleporter = GetItem(player, item);
                                }
                                else
                                {
                                    if (PunishSpellId > 0)
                                    {
                                        var spell = GameServer.Database.SelectObject<DBSpell>(DB.Column("SpellID").IsEqualTo(PunishSpellId));

                                        if (spell != null)
                                        {
                                            foreach (GamePlayer pl in this.GetPlayersInRadius(5000))
                                            {
                                                pl.Out.SendSpellEffectAnimation(pl, pl, (ushort)PunishSpellId, 0, false, 5);
                                            }
                                            player.TakeDamage(this, eDamageType.Energy, (int)spell.Damage, 0);
                                        }
                                    }
                                }
                            }
                            HandlePopMob();
                        }
                    }
                }
                else
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.AlreadyOpen2"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
            }
            else
            {
                bool failed = false;
                if (TpLevelRequirement > 0 && player.Level < TpLevelRequirement)
                {
                    failed = true;
                }

                if (TpIsRenaissance && !player.IsRenaissance)
                {
                    failed = true;
                }

                if (failed)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.Cannotteleport"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    HandleTeleporter(player);
                    gotItemOrUsedTeleporter = true;
                }
            }

            ShowSecondaryModel();

            if (gotItemOrUsedTeleporter && ItemInterval != 0)
            {
                m_lastInteract = DateTime.MinValue;
                LastOpen = DateTime.Now;
                RemoveFromWorld(ItemInterval * 60);
                if (this.EventID == null || (CanRespawnWithinEvent))
                {
                    RespawnTimer.Start();
                }
                SaveIntoDatabase();
            }

            m_interactPlayer = null;

            return true;
        }

        private bool GetItem(GamePlayer player, ItemTemplate item)
        {
            if (player.Inventory.AddTemplate(GameInventoryItem.Create(item), 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.GetItem"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                InventoryLogging.LogInventoryAction(this, player, eInventoryActionType.Loot, item, 1);
                return true;
            }
            else
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.GetItemBagFull"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            return false;
        }

        private void HandleTeleporter(GamePlayer player)
        {
            if (TpX > 0 && TpY > 0 && TpZ > 0 && TpRegion > 0)
            {
                RegionTimer TimerTL = new RegionTimer(this, Teleportation);
                TimerTL.Properties.setProperty("TP", new GameLocation("Coffre Location", (ushort)TpRegion, TpX, TpY, TpZ, (ushort)TPHeading));
                TimerTL.Properties.setProperty("player", player);
                TimerTL.Start(3000);

                {
                    if (TpEffect > 0)
                    {
                        //handle effect
                        player.Out.SendSpellCastAnimation(player, (ushort)TpEffect, 20);
                    }
                }
            }
        }

        private int Teleportation(RegionTimer timer)
        {
            GameLocation pos = timer.Properties.getProperty<GameLocation>("TP", null);
            GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);

            player.MoveTo(pos.RegionID, pos.Position.X, pos.Position.Y, pos.Position.Z, pos.Heading);

            return 0;
        }

        private void HandlePopMob()
        {
            if (TrapRate > 0 && NpctemplateId != null)
            {
                var rand = new Random(DateTime.Now.Millisecond);
                if (rand.Next(1, TrapRate + 1) <= TrapRate)
                {
                    var template = GameServer.Database.SelectObject<DBNpcTemplate>(DB.Column("TemplateId").IsEqualTo(NpctemplateId));
                    if (template != null)
                    {
                        var mob = new AmteMob(new NpcTemplate(template))
                        {
                            Position = new Vector3(Position.X, Position.Y, Position.Z),
                            Heading = Heading,
                            CurrentRegionID = CurrentRegionID,
                            CurrentRegion = CurrentRegion,
                            Size = 50,
                            Name = template.Name
                        };

                        mob.RespawnInterval = -1;
                        mob.AddToWorld();
                    }
                }
            }
        }

        private void Repop_Elapsed(object sender, ElapsedEventArgs e)
        {
            RevertToPrimaryModel();
            AddToWorld();
        }
        #endregion

        #region Serrure
        private void Unlock(GamePlayer player, byte response)
        {
            if (response == 0x00)
            {
                m_interactPlayer = null;
                return;
            }

            RegionTimer timer = new RegionTimer(player, UnlockCallback);
            timer.Properties.setProperty("X", player.Position.X);
            timer.Properties.setProperty("Y", player.Position.Y);
            timer.Properties.setProperty("Z", player.Position.Z);
            timer.Properties.setProperty("Head", (int)player.Heading);
            timer.Properties.setProperty("player", player);
            timer.Start(500);
            player.Out.SendTimerWindow("Crochetage", UNLOCK_TIME);
        }

        private int UnlockCallback(RegionTimer timer)
        {
            m_interactPlayer = null;
            GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);
            int Xpos = timer.Properties.getProperty("X", 0);
            int Ypos = timer.Properties.getProperty("Y", 0);
            int Zpos = timer.Properties.getProperty("Z", 0);
            int Head = timer.Properties.getProperty("Head", 0);
            if (player == null)
                return 0;
            if (Xpos != (int)player.Position.X || Ypos != (int)player.Position.Y || Zpos != (int)player.Position.Z || Head != player.Heading || player.InCombat)
            {
                player.Out.SendCloseTimerWindow();
                return 0;
            }

            int time = timer.Properties.getProperty("time", 0) + 500;
            timer.Properties.setProperty("time", time);
            if (time < UNLOCK_TIME * 1000)
                return 500;
            player.Out.SendCloseTimerWindow();

            int Chance = 100 - LockDifficult;

            //Dexterité
            float dextChance = (float)(player.Dexterity) / 125;
            if (dextChance > 1.0f)
                dextChance = 1.0f;
            if (dextChance < 0.1f)
                dextChance = 0.1f;
            Chance = (int)(dextChance * Chance);

            //Races
            switch (player.RaceName)
            {
                case "Half Ogre":
                case "Troll":
                    Chance -= 2;
                    break;

                case "Highlander":
                case "Firbolg":
                case "Dwarf":
                case "Norseman":
                    Chance -= 1;
                    break;

                case "Elf":
                    Chance += 1;
                    break;
            }

            //Classes
            switch (player.CharacterClass.ID)
            {
                case (int)eCharacterClass.AlbionRogue:
                case (int)eCharacterClass.Stalker:
                case (int)eCharacterClass.MidgardRogue:
                    Chance += 1;
                    break;

                case (int)eCharacterClass.Infiltrator:
                case (int)eCharacterClass.Minstrel:
                case (int)eCharacterClass.Scout:
                case (int)eCharacterClass.Hunter:
                case (int)eCharacterClass.Shadowblade:
                case (int)eCharacterClass.Nightshade:
                case (int)eCharacterClass.Ranger:
                    Chance += 2;
                    break;
            }

            if (Chance >= 100)
                Chance = 99;

            if (player.Client.Account.PrivLevel >= 2)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.PickLockChance") + Chance + "/100", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            if (Chance > 0 && Util.Chance(Chance))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.PickLockSuccess"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                InteractEnd(player);
            }
            else
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GameChest.PickLockFail"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                player.Inventory.RemoveTemplate(CROCHET, 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            }
            return 0;
        }
        #endregion

        #region Item aléatoire
        private CoffreItem GetRandomItem()
        {
            if (!Util.Chance(ItemChance))
                return new CoffreItem("", 0);

            int num = Util.Random(1, AllChance);
            int i = 0;
            foreach (CoffreItem item in m_Items)
            {
                i += item.Chance;
                if (i >= num)
                    return item;
            }
            return new CoffreItem("", 0);
        }
        #endregion

        #region Gestion des items
        /// <summary>
        /// Ajoute ou modifie la chance d'apparition d'un item
        /// </summary>
        /// <param name="Id_nb">Id_nb de l'item à modifier ou ajouter</param>
        /// <param name="chance">Nombre de chance d'apparition de l'item</param>
        /// <returns>Retourne false si l'item n'existe pas dans la base de donné ItemTemplate</returns>
        public bool ModifyItemList(string Id_nb, int chance)
        {
            ItemTemplate item = GameServer.Database.SelectObject<ItemTemplate>(DB.Column("Id_nb").IsEqualTo(GameServer.Database.Escape(Id_nb)));
            if (item == null)
                return false;

            foreach (CoffreItem it in m_Items)
            {
                if (it.Id_nb == Id_nb)
                {
                    it.Chance = chance;
                    return true;
                }
            }

            m_Items.Add(new CoffreItem(Id_nb, chance));
            return true;
        }

        /// <summary>
        /// Supprime un item de la liste des items
        /// </summary>
        /// <param name="Id_nb">item à supprimer</param>
        /// <returns>Retourne true si l'item est supprimé</returns>
        public bool DeleteItemFromItemList(string Id_nb)
        {
            foreach (CoffreItem item in m_Items)
            {
                if (item.Id_nb == Id_nb)
                {
                    m_Items.Remove(item);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Database
        public override void LoadFromDatabase(DataObject obj)
        {
            DBCoffre coffre = obj as DBCoffre;
            if (coffre == null) return;
            Name = coffre.Name;
            Position = new Vector3(coffre.X, coffre.Y, coffre.Z);
            Heading = (ushort)(coffre.Heading & 0xFFF);
            HasPickableAnim = coffre.HasPickableAnim;
            CurrentRegionID = coffre.Region;
            Model = coffre.Model;
            LastOpen = coffre.LastOpen;
            ItemInterval = coffre.ItemInterval;
            InternalID = coffre.ObjectId;
            ItemChance = coffre.ItemChance;
            KeyItem = coffre.KeyItem;
            LockDifficult = coffre.LockDifficult;
            Coffre = coffre;
            TrapRate = coffre.TrapRate;
            NpctemplateId = coffre.NpctemplateId;
            TpX = coffre.TpX;
            TpY = coffre.TpY;
            TpZ = coffre.TpZ;
            TPHeading = coffre.TPHeading;
            PunishSpellId = coffre.PunishSpellId;
            TpLevelRequirement = coffre.TpLevelRequirement;
            TpIsRenaissance = coffre.TpIsRenaissance;
            IsOpeningRenaissanceType = coffre.IsOpeningRenaissanceType;
            IsTeleporter = coffre.IsTeleporter;
            TpEffect = coffre.TpEffect;
            TpRegion = coffre.TpRegion;
            EventID = coffre.EventID;
            CoffreOpeningInterval = coffre.CoffreOpeningInterval;
            IsLargeCoffre = coffre.IsLargeCoffre;
            RemovedByEventID = coffre.RemovedByEventID;
            Coffre.TPID = coffre.TPID;
            Coffre.ShouldRespawnToTPID = coffre.ShouldRespawnToTPID;
            PickOnTouch = coffre.PickOnTouch;
            IsOpenableOnce = coffre.IsOpenableOnce;
            IsTerritoryLinked = coffre.IsTerritoryLinked;
            KeyLoseDur = coffre.KeyLoseDur;
            SwitchFamily = coffre.SwitchFamily;
            SwitchOrder = coffre.SwitchOrder;
            IsSwitch = coffre.IsSwitch;
            ActivatedDuration = coffre.ActivatedDuration;
            SecondaryModel = coffre.SecondaryModel;

            InitTimer();

            m_Items = new List<CoffreItem>();
            if (coffre.ItemList != "")
                foreach (string item in coffre.ItemList.Split(';'))
                    m_Items.Add(new CoffreItem(item));
        }

        public void InitTimer()
        {
            double interval = (double)ItemInterval * 60D * 1000D;
            if (interval > int.MaxValue)
            {
                interval = (double)int.MaxValue;
            }

            if (interval == 0)
            {
                interval = 1;
            }

            RespawnTimer = new Timer(interval);
            RespawnTimer.Elapsed += Repop_Elapsed;
        }

        public override void SaveIntoDatabase()
        {
            if (Coffre == null)
                Coffre = new DBCoffre();

            Coffre.Name = Name;
            Coffre.X = (int)Position.X;
            Coffre.Y = (int)Position.Y;
            Coffre.Z = (int)Position.Z;
            Coffre.Heading = Heading;
            Coffre.Region = CurrentRegionID;
            Coffre.Model = Model;
            Coffre.LastOpen = LastOpen;
            Coffre.ItemInterval = ItemInterval;
            Coffre.ItemChance = ItemChance;
            Coffre.KeyItem = KeyItem;
            Coffre.LockDifficult = LockDifficult;
            Coffre.TrapRate = TrapRate;
            Coffre.NpctemplateId = NpctemplateId;
            Coffre.HasPickableAnim = HasPickableAnim;
            Coffre.TpX = TpX;
            Coffre.TpY = TpY;
            Coffre.TpZ = TpZ;
            Coffre.TPHeading = TPHeading;
            Coffre.PunishSpellId = PunishSpellId;
            Coffre.TpLevelRequirement = TpLevelRequirement;
            Coffre.TpIsRenaissance = TpIsRenaissance;
            Coffre.IsTeleporter = IsTeleporter;
            Coffre.TpEffect = TpEffect;
            Coffre.TpRegion = TpRegion;
            Coffre.IsOpeningRenaissanceType = IsOpeningRenaissanceType;
            Coffre.EventID = EventID;
            Coffre.CoffreOpeningInterval = CoffreOpeningInterval;
            Coffre.IsLargeCoffre = IsLargeCoffre;
            Coffre.RemovedByEventID = RemovedByEventID;
            Coffre.TPID = TPID;
            Coffre.ShouldRespawnToTPID = ShouldRespawnToTPID;
            Coffre.PickOnTouch = PickOnTouch;
            Coffre.IsOpenableOnce = IsOpenableOnce;
            Coffre.IsTerritoryLinked = IsTerritoryLinked;
            Coffre.KeyLoseDur = KeyLoseDur;
            Coffre.SwitchFamily = SwitchFamily;
            Coffre.SwitchOrder = SwitchOrder;
            Coffre.IsSwitch = IsSwitch;
            Coffre.ActivatedDuration = ActivatedDuration;
            Coffre.SecondaryModel = SecondaryModel;

            if (Items != null)
            {
                string list = "";
                foreach (CoffreItem item in m_Items)
                {
                    if (list.Length > 0)
                        list += ";";
                    list += item.Id_nb + "|" + item.Chance;
                }
                Coffre.ItemList = list;
            }

            if (InternalID == null)
            {
                GameServer.Database.AddObject(Coffre);
                InternalID = Coffre.ObjectId;
            }
            else
                GameServer.Database.SaveObject(Coffre);
        }

        public override void DeleteFromDatabase()
        {
            if (Coffre == null)
                return;
            GameServer.Database.DeleteObject(Coffre);
        }
        #endregion

        #region CoffreItem
        public class CoffreItem
        {
            public string Id_nb;
            public int Chance;

            public CoffreItem(string id_nb, int chance)
            {
                Id_nb = id_nb;
                Chance = chance;
            }

            public CoffreItem(string item)
            {
                string[] values = item.Split('|');
                if (values.Length < 2)
                    throw new Exception("Pas de caractère séparateur pour l'item \"" + item + "\"");
                Id_nb = values[0];
                try
                {
                    Chance = int.Parse(values[1]);
                }
                catch
                {
                    Chance = 0;
                }
            }
        }
        #endregion

        public IList<string> DelveInfo()
        {
            List<string> text = new List<string>
                {
                    " + OID: " + ObjectID,
                    " + Class: " + GetType(),
                    " + Position: " + Position + " Heading=" + Heading,
                    " + Realm: " + Realm,
                    " + Model: " + Model,
                    " + EventID: " + EventID,
                    " + Masqué par un EventID: " + RemovedByEventID,
                    "",
                    "-- Coffre --",
                    " + Chance d'apparition d'un item: " + ItemChance + "%",
                    " + Intervalle d'apparition d'un item: " + ItemInterval + " minutes",
                    " + Intervalle d'ouverture un coffre: " + this.CoffreOpeningInterval + " minutes",
                    " + Dernière fois que le coffre a été ouvert: " + LastOpen.ToShortDateString() + " " + LastOpen.ToShortTimeString(),
                    " + IsLongDistance type: " + this.IsLargeCoffre,
                    " + Respawn to TPID: " + ShouldRespawnToTPID,
                    " + TPID: " + TPID,
                    " + Pick on Touch: " + PickOnTouch,
                    " + Secondary Model: " + SecondaryModel,
                    " + Is Openable Once: " + IsOpenableOnce,
                    " + Is Territory Linked: " + IsTerritoryLinked,
                    " + KeyLoseDur: " + KeyLoseDur
                };
            if (LockDifficult > 0)
                text.Add(" + Difficulté pour crocheter le coffre: " + LockDifficult + "%");
            else
                text.Add(" + Ce coffre ne peut pas être crocheté");
            if (KeyItem != "")
                text.Add(" + Id_nb de la clef: " + KeyItem);
            else
                text.Add(" + Le coffre n'a pas besoin de clef");

            text.Add(" + PickableAnim: " + HasPickableAnim);

            text.Add("");
            text.Add(" + Listes des items (" + Items.Count + " items):");
            int i = 0;
            int TotalChance = 0;
            foreach (CoffreItem item in Items)
            {
                i++;
                TotalChance += item.Chance;
                text.Add("  " + i + ". " + item.Id_nb + " - " + item.Chance);
            }
            text.Add("Total des chances: " + TotalChance);

            text.Add("");
            text.Add("-- Teleport Info --");
            text.Add("IsTeleporter: " + this.IsTeleporter);
            text.Add("TP Level Requirement: " + this.TpLevelRequirement);
            text.Add("TP Effect: " + this.TpEffect);
            text.Add("X: " + TpX);
            text.Add("Y: " + TpY);
            text.Add("Z: " + TpZ);
            text.Add("TP Heading: " + this.TPHeading);
            text.Add("RegionID: " + TpRegion);

            text.Add("");
            text.Add("-- Trap Info --");
            text.Add("NPCTemplate: " + (this.NpctemplateId != null ? this.NpctemplateId : "-"));
            text.Add("Trap Rate: " + this.TrapRate);

            text.Add("");
            text.Add("-- Is Renaissance Info --");
            text.Add("IsOpeningRenaissanceType: " + this.IsOpeningRenaissanceType);
            text.Add("TpIsRenaissance: " + this.TpIsRenaissance);
            text.Add("PunishSpellId: " + this.PunishSpellId);

            text.Add("");
            text.Add("-- Is Switch Info --");
            text.Add("IsSwitch: " + this.IsSwitch);
            text.Add("Switch Family: " + this.SwitchFamily);
            text.Add("Switch Order: " + this.SwitchOrder);
            text.Add("Activated Duration: " + this.ActivatedDuration + " secondes");
            return text;
        }

        public override void CustomCopy(GameObject source)
        {
            base.CustomCopy(source);
            GameCoffre coffre = source as GameCoffre;
            if (coffre != null)
            {
                m_Items = coffre.Items;
                Name = coffre.Name + "_cpy";
                ItemInterval = coffre.ItemInterval;
                TpEffect = coffre.TpEffect;
                TpIsRenaissance = coffre.TpIsRenaissance;
                TpLevelRequirement = coffre.TpLevelRequirement;
                TpX = coffre.TpX;
                TpY = coffre.TpY;
                TpZ = coffre.TpZ;
                TpRegion = coffre.TpRegion;
                TrapRate = coffre.TrapRate;
                NpctemplateId = coffre.NpctemplateId;
                PunishSpellId = coffre.PunishSpellId;
                IsOpeningRenaissanceType = coffre.IsOpeningRenaissanceType;
                IsTeleporter = coffre.IsTeleporter;
                CoffreOpeningInterval = coffre.CoffreOpeningInterval;
                IsLargeCoffre = coffre.IsLargeCoffre;
                ItemChance = coffre.ItemChance;
                KeyItem = coffre.KeyItem;
                PickOnTouch = coffre.PickOnTouch;
                SecondaryModel = coffre.SecondaryModel;
                IsOpenableOnce = coffre.IsOpenableOnce;
                IsTerritoryLinked = coffre.IsTerritoryLinked;
                KeyLoseDur = coffre.KeyLoseDur;
                SwitchFamily = coffre.SwitchFamily;
                SwitchOrder = coffre.SwitchOrder;
                IsSwitch = coffre.IsSwitch;
                ActivatedDuration = coffre.ActivatedDuration;
                InitTimer();
            }
        }
    }
}
