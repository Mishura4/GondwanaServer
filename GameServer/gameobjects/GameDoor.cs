/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.Utils;
using DOL.GS.Quests;
using System.Threading;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PropertyCalc;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.GS.PacketHandler.Client.v168;
using System.Numerics;
using DOL.MobGroups;
using System.Linq;

namespace DOL.GS
{
    /// <summary>
    /// GameDoor is class for regular door
    /// </summary>
    public class GameDoor : GameLiving, IDoor
    {
        private bool m_openDead = false;
        private static Timer m_timer;
        protected volatile uint m_lastUpdateTickCount = uint.MinValue;
        private readonly object m_LockObject = new object();
        private uint m_flags = 0;
        private string m_group_mob_id;
        private string m_switchFamily;
        private int originalPunishSpellValue;

        public string SwitchFamily
        {
            get
            {
                return m_switchFamily;
            }
            set
            {
                m_switchFamily = value;
            }
        }

        /// <summary>
        /// The time interval after which door will be closed, in milliseconds
        /// On live this is usually 5 seconds
        /// </summary>
        protected const int CLOSE_DOOR_TIME = 8000;
        /// <summary>
        /// The timed action that will close the door
        /// </summary>
        protected GameTimer m_closeDoorAction;

        /// <summary>
        /// Creates a new GameDoor object
        /// </summary>
        public GameDoor()
            : base()
        {
            m_state = eDoorState.Closed;
            m_model = 0xFFFF;
        }

        /// <summary>
        /// Loads this door from a door table slot
        /// </summary>
        /// <param name="obj">DBDoor</param>
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            DBDoor m_dbdoor = obj as DBDoor;
            if (m_dbdoor == null) return;
            Zone curZone = WorldMgr.GetZone((ushort)(m_dbdoor.InternalID / 1000000));
            if (curZone == null) return;
            this.CurrentRegion = curZone.ZoneRegion;
            m_name = m_dbdoor.Name;
            m_Heading = (ushort)m_dbdoor.Heading;
            Position = new Vector3(m_dbdoor.X, m_dbdoor.Y, m_dbdoor.Z);
            m_level = 0;
            m_model = 0xFFFF;
            m_doorID = m_dbdoor.InternalID;
            m_guildName = m_dbdoor.Guild;
            m_Realm = (eRealm)m_dbdoor.Realm;
            m_level = m_dbdoor.Level;
            m_health = m_dbdoor.MaxHealth;
            m_maxHealth = m_dbdoor.MaxHealth;
            m_locked = m_dbdoor.Locked;
            m_flags = m_dbdoor.Flags;
            m_group_mob_id = m_dbdoor.Group_Mob_Id;
            m_key = m_dbdoor.Key;
            m_key_Chance = m_dbdoor.Key_Chance;
            m_isRenaissance = m_dbdoor.IsRenaissance;
            m_punishSpell = m_dbdoor.PunishSpell;
            m_switchFamily = m_dbdoor.SwitchFamily;
            originalPunishSpellValue = m_dbdoor.PunishSpell;

            // Open mile gates on PVE and PVP server types
            if (CurrentRegion.IsFrontier && (GameServer.Instance.Configuration.ServerType == eGameServerType.GST_PvE
                || GameServer.Instance.Configuration.ServerType == eGameServerType.GST_PvP))
                State = eDoorState.Open;

            this.AddToWorld();
        }
        /// <summary>
        /// save this door to a door table slot
        /// </summary>
        public override void SaveIntoDatabase()
        {
            DBDoor obj = null;
            if (InternalID != null)
                obj = GameServer.Database.FindObjectByKey<DBDoor>(InternalID);
            if (obj == null)
                obj = new DBDoor();
            obj.Name = this.Name;
            obj.InternalID = this.DoorID;
            obj.Type = DoorID / 100000000;
            obj.Guild = this.GuildName;
            obj.Flags = this.Flag;
            obj.Realm = (byte)this.Realm;
            obj.Level = this.Level;
            obj.MaxHealth = this.MaxHealth;
            obj.Health = this.MaxHealth;
            obj.Locked = this.Locked;
            obj.Group_Mob_Id = Group_Mob_Id;
            obj.Key = Key;
            obj.Key_Chance = Key_Chance;
            obj.IsRenaissance = IsRenaissance;
            obj.PunishSpell = PunishSpell;
            obj.SwitchFamily = this.SwitchFamily;
            if (InternalID == null)
            {
                GameServer.Database.AddObject(obj);
                InternalID = obj.ObjectId;
            }
            else
                GameServer.Database.SaveObject(obj);
        }

        #region Properties

        private int m_locked;
        /// <summary>
        /// door open = 0 / lock = 1 
        /// </summary>
        public virtual int Locked
        {
            get { return m_locked; }
            set { m_locked = value; }
        }

        /// <summary>
        /// this hold the door index which is unique
        /// </summary>
        private int m_doorID;

        /// <summary>
        /// door index which is unique
        /// </summary>
        public virtual int DoorID
        {
            get { return m_doorID; }
            set { m_doorID = value; }
        }

        /// <summary>
        /// Get the ZoneID of this door
        /// </summary>
        public virtual ushort ZoneID
        {
            get { return (ushort)(DoorID / 1000000); }
        }

        private int m_type;

        /// <summary>
        /// Door Type
        /// </summary>
        public virtual int Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        /// <summary>
        /// This is used to identify what sound a door makes when open / close
        /// </summary>
        public virtual uint Flag
        {
            get { return m_flags; }
            set { m_flags = value; }
        }

        /// <summary>
        /// This hold the state of door
        /// </summary>
        protected eDoorState m_state;

        /// <summary>
        /// The state of door (open or close)
        /// </summary>
        public virtual eDoorState State
        {
            get { return m_state; }
            set
            {
                if (m_state != value)
                {
                    lock (m_LockObject)
                    {
                        m_state = value;
                        foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            player.SendDoorUpdate(this);
                        }
                    }
                }
            }
        }

        private void TriggerPunishSpell(GameLiving opener)
        {
            if (PunishSpell > 0 && opener != null)
            {
                DBSpell punishspell = GameServer.Database.SelectObjects<DBSpell>(DB.Column("SpellID").IsEqualTo(PunishSpell)).FirstOrDefault();

                // check if the player is punished
                if (punishspell != null)
                {
                    foreach (GamePlayer pl in opener.GetPlayersInRadius(5000))
                    {
                        pl.Out.SendSpellEffectAnimation(opener, opener, (ushort)PunishSpell, 0, false, 5);
                    }
                    if (opener is GamePlayer player)
                        player.Out.SendSpellEffectAnimation(opener, opener, (ushort)PunishSpell, 0, false, 5);
                    opener.TakeDamage(opener, eDamageType.Energy, (int)punishspell.Damage, 0);
                }
            }
        }

        public void UnlockBySwitch()
        {
            if (Locked == 1)
            {
                Locked = 0;
                State = eDoorState.Open;
                PunishSpell = 0;
                SaveIntoDatabase();
            }
        }

        public void LockBySwitch()
        {
            if (Locked == 0)
            {
                Locked = 1;
                State = eDoorState.Closed;
                PunishSpell = originalPunishSpellValue;
                SaveIntoDatabase();
            }
        }

        public void OpenBySwitch()
        {
            UnlockBySwitch();
            if (HealthPercent > 40 || !m_openDead)
            {
                lock (m_LockObject)
                {
                    if (m_closeDoorAction == null)
                    {
                        m_closeDoorAction = new CloseDoorAction(this);
                    }
                    m_closeDoorAction.Start(CLOSE_DOOR_TIME);
                }
            }
        }

        #endregion

        /// <summary>
        /// Call this function to open the door
        /// </summary>
        public virtual void Open(GameLiving opener = null)
        {
            GamePlayer player = opener as GamePlayer;

            // Check if the all the mob in groupmob are dead
            if (!String.IsNullOrEmpty(Group_Mob_Id) && MobGroupManager.Instance.Groups.ContainsKey(Group_Mob_Id))
            {
                bool allDead = MobGroupManager.Instance.Groups[Group_Mob_Id].NPCs.All(m => !m.IsAlive);
                if (!allDead)
                {
                    if (player != null)
                        player.Out.SendMessage("Il faut éliminer les monstres dans les alentours pour ouvrir cette porte !", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    TriggerPunishSpell(opener);
                    return;
                }
            }

            // Check if the opener have the renaissance status
            if (IsRenaissance && player != null && !player.IsRenaissance)
            {
                player.Out.SendMessage("Vous n'avez pas aquis le pouvoir de la Pierre Philosophale, vous ne pouvez pas entrer ici !", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                TriggerPunishSpell(opener);
                return;
            }

            // Check if a key is required
            if (!String.IsNullOrEmpty(Key) && opener != null && opener.Inventory.GetFirstItemByID(Key, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) == null)
            {
                if (player != null)
                    player.Out.SendMessage("Vous avez besoin d'une clé pour ouvrir cette porte !", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                TriggerPunishSpell(opener);
                return;
            }
            else if (!String.IsNullOrEmpty(Key) && opener != null && Key.StartsWith("oneuse"))
            {
                opener.Inventory.RemoveCountFromStack(opener.Inventory.GetFirstItemByID(Key, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack), 1);
            }

            // Check the opener fail to open the door
            if (Util.Chance(Key_Chance))
            {
                if (player != null)
                    player.Out.SendMessage("Vous échouez à ouvrir cette porte !", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (Locked == 0)
                this.State = eDoorState.Open;

            if (HealthPercent > 40 || !m_openDead)
            {
                lock (m_LockObject)
                {
                    if (m_closeDoorAction == null)
                    {
                        m_closeDoorAction = new CloseDoorAction(this);
                    }
                    m_closeDoorAction.Start(CLOSE_DOOR_TIME);
                }
            }
        }

        public virtual byte Status
        {
            get
            {
                //	if( this.HealthPercent == 0 ) return 0x01;//broken
                return 0x00;
            }
        }

        /// <summary>
        /// Call this function to close the door
        /// </summary>
        public virtual void Close(GameLiving closer = null)
        {
            if (!m_openDead)
                this.State = eDoorState.Closed;
            m_closeDoorAction = null;
        }

        /// <summary>
        /// Allow a NPC to manipulate the door
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="open"></param>
        public virtual void NPCManipulateDoorRequest(GameNPC npc, bool open)
        {
            npc.TurnTo(this);
            if (open && m_state != eDoorState.Open)
                this.Open();
            else if (!open && m_state != eDoorState.Closed)
                this.Close();

        }

        public override int Health
        {
            get { return m_health; }
            set
            {

                int maxhealth = MaxHealth;
                if (value >= maxhealth)
                {
                    m_health = maxhealth;

                    XPGainers.Clear();
                }
                else if (value > 0)
                {
                    m_health = value;
                }
                else
                {
                    m_health = 0;
                }

                if (IsAlive && m_health < maxhealth)
                {
                    StartHealthRegeneration();
                }
            }
        }

        /// <summary>
        /// Get the solidity of the door
        /// </summary>
        public override int MaxHealth
        {
            get { return 5 * GetModified(eProperty.MaxHealth); }
        }

        /// <summary>
        /// No regeneration over time of the door
        /// </summary>
        /// <param name="killer"></param>
        public override void Die(GameObject killer)
        {
            base.Die(killer);
            StartHealthRegeneration();
        }

        /// <summary>
        /// Broadcasts the Door Update to all players around
        /// </summary>
        public override void BroadcastUpdate()
        {
            base.BroadcastUpdate();

            m_lastUpdateTickCount = GameTimer.GetTickCount();
        }

        private static long m_healthregentimer = 0;
        private int m_punishSpell;
        private bool m_isRenaissance;
        private short m_key_Chance;
        private string m_key;

        public virtual void RegenDoorHealth()
        {
            Health = 0;
            if (Locked == 0)
                Open();

            m_healthregentimer = 9999;
            m_timer = new Timer(new TimerCallback(StartHealthRegen), null, 0, 1000);

        }

        public virtual void StartHealthRegen(object param)
        {
            if (HealthPercent >= 40)
            {
                m_timer.Dispose();
                m_openDead = false;
                Close();
                return;
            }

            if (Health == MaxHealth)
            {
                m_timer.Dispose();
                m_openDead = false;
                Close();
                return;
            }

            if (m_healthregentimer <= 0)
            {
                m_timer.Dispose();
                m_openDead = false;
                Close();
                return;
            }
            this.Health += this.Level * 2;
            m_healthregentimer -= 10;
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {

            if (!m_openDead && this.Realm != eRealm.Door)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);

                double damageDealt = damageAmount + criticalAmount;
            }

            GamePlayer attackerPlayer = source as GamePlayer;
            if (attackerPlayer != null)
            {
                if (!m_openDead && this.Realm != eRealm.Door)
                {
                    attackerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                }
                if (!m_openDead && this.Realm != eRealm.Door)
                {
                    Health -= damageAmount + criticalAmount;

                    if (!IsAlive)
                    {
                        attackerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        Die(source);
                        m_openDead = true;
                        RegenDoorHealth();
                        if (Locked == 0)
                            Open();

                        Group attackerGroup = attackerPlayer.Group;
                        if (attackerGroup != null)
                        {
                            foreach (GameLiving living in attackerGroup.GetMembersInTheGroup())
                            {
                                ((GamePlayer)living).Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Link a group mob at the door
        /// </summary>
        public string Group_Mob_Id
        {
            get
            {
                return m_group_mob_id;
            }

            set
            {
                m_group_mob_id = value;
            }
        }

        /// <summary>
        /// Itemtemplate id to open the door
        /// </summary>
        public string Key
        {
            get
            {
                return m_key;
            }

            set
            {
                m_key = value;
            }
        }

        /// <summary>
        /// Chance of fail to open the door
        /// </summary>
        public short Key_Chance
        {
            get
            {
                return m_key_Chance;
            }

            set
            {
                m_key_Chance = value;
            }
        }

        /// <summary>
        /// If need the Renaissance state
        /// </summary>
        public bool IsRenaissance
        {
            get
            {
                return m_isRenaissance;
            }

            set
            {
                m_isRenaissance = value;
            }
        }

        /// <summary>
        /// Spell Id to punish the opener
        /// </summary>
        public int PunishSpell
        {
            get
            {
                return m_punishSpell;
            }

            set
            {
                m_punishSpell = value;
            }
        }

        /// <summary>
        /// The action that closes the door after specified duration
        /// </summary>
        protected class CloseDoorAction : RegionAction
        {
            /// <summary>
            /// Constructs a new close door action
            /// </summary>
            /// <param name="door">The door that should be closed</param>
            public CloseDoorAction(GameDoor door)
                : base(door)
            {
            }

            /// <summary>
            /// This function is called to close the door 10 seconds after it was opened
            /// </summary>
            protected override void OnTick()
            {
                GameDoor door = (GameDoor)m_actionSource;
                door.Close();
            }
        }
    }
}
