/**
 * Created by Virant "Dre" Jérémy for Amtenael
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using System.Reflection;
using log4net;
using DOL.Language;
using DOL.Territories;
using DOL.GS.Geometry;
using System.Linq;
using DOL.GS.Quests;
using static System.Net.Mime.MediaTypeNames;

namespace DOL.GS.Scripts
{
    public class TeleportNPC : GameNPC
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Variables
        public Dictionary<string, JumpPos> JumpPositions;
        private int m_Range;
        private byte m_MinLevel;
        private string m_Text = "{5}";
        private string m_Text_Refuse = "Vous n'avez pas le niveau requis pour être téléporté.";
        protected DBTeleportNPC db;
        protected bool m_busy;
        public bool IsTerritoryLinked { get; set; }
        
        public ushort RequiredModel { get; set; }

        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        public byte MinLevel
        {
            get { return m_MinLevel; }
            set { m_MinLevel = value; }
        }

        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        public string Text_Refuse
        {
            get { return m_Text_Refuse; }
            set { m_Text_Refuse = value; }
        }

        public bool? IsOutlawFriendly
        {
            get;
            set;
        }

        public bool ShowTPIndicator { get; set; }
        private static Dictionary<GamePlayer, string> WhisperTracker = new Dictionary<GamePlayer, string>();

        #endregion

        #region Interaction
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (!WillTalkTo(player))
                return false;

            if (m_busy)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"TeleportNPC.Busy"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (!string.IsNullOrEmpty(m_Text))
            {
                var list = GetList(player);
                if (!string.IsNullOrEmpty(list))
                {
                    var text = string.Format(m_Text, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName, GetList(player));
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the NPC will talk to a player if true, or respond with some variance of "I hate you!" to the player if false
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool WillTalkTo(GamePlayer player, bool silent = false)
        {
            if (IsTerritoryLinked == true)
            {
                switch (CurrentTerritory?.IsOwnedBy(player))
                {
                    case true:
                        break;
                    
                    case null:
                        log.Warn($"TextNPC {Name} (${InternalID}) has `IsTerritoryLinked = true`, but is not in a territory");
                        goto case false;
                        
                    case false:
                        if (!silent)
                        {
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleportNPC.NotInOwnedTerritory"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        }
                        return false;
                }
            }

            if (RequiredModel != 0 && player.Model != RequiredModel)
            {
                if (!silent)
                {
                    player.Out.SendMessage("...", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                }
                return false;
            }

            if (this.IsOutlawFriendly.HasValue)
            {
                if (this.IsOutlawFriendly.Value)
                {
                    if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
                    {
                        if (!silent)
                        {
                            var text = string.Format(m_Text_Refuse, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                            player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        }
                        return false;
                    }
                }
                else
                {
                    if (player.Reputation < 0 && player.Client.Account.PrivLevel == 1)
                    {
                        if (!silent)
                        {
                            var text = string.Format(m_Text_Refuse, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                            player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        }
                        return false;
                    }
                }
            }

            if (player.Level < m_MinLevel || m_Range > 0)
            {
                if (!string.IsNullOrEmpty(m_Text_Refuse))
                {
                    var text = string.Format(m_Text_Refuse, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                }
                return false;
            }

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str) || source is not GamePlayer player) return false;

            if (!WillTalkTo(player))
            {
                return false;
            }

            if (m_busy)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"TeleportNPC.Busy"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (!JumpPositions.TryGetValue(str, out var jumpPos) || !jumpPos.CanJump(player))
            {
                WhisperTracker[player] = str;
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"TeleportNPC.UnknownDestination"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }
            
            WhisperTracker[player] = str;

            RegionTimer TimerTL = new RegionTimer(this, Teleportation);
            TimerTL.Properties.setProperty("TP", jumpPos);
            TimerTL.Properties.setProperty("player", player);
            TimerTL.Start(3000);
            foreach (GamePlayer players in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                players.Out.SendSpellCastAnimation(this, 1, 120);
                players.Out.SendEmoteAnimation(player, eEmote.Bind);
            }
            m_busy = true;

            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (!(source is GamePlayer player) || String.IsNullOrEmpty(item?.Id_nb))
                return false;

            if (IsTerritoryLinked && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleportNPC.NotInOwnedTerritory"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            foreach (JumpPos pos in JumpPositions.Values)
            {
                if (pos.Conditions.Item.Equals(item.Id_nb, StringComparison.CurrentCultureIgnoreCase))
                {
                    RegionTimer TimerTL = new RegionTimer(this, Teleportation);
                    TimerTL.Properties.setProperty("TP", pos);
                    TimerTL.Properties.setProperty("player", player);
                    TimerTL.Start(3000);
                    foreach (GamePlayer players in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        players.Out.SendSpellCastAnimation(this, 1, 20);
                        players.Out.SendEmoteAnimation(player, eEmote.Bind);
                    }
                    m_busy = true;
                    return false;
                }
            }
            return false;
        }

        protected virtual int Teleportation(RegionTimer timer)
        {
            JumpPos pos = timer.Properties.getProperty<JumpPos>("TP", null);
            GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);
            if (pos == null || player == null) return 0;
            if (player.InCombat)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"TeleportNPC.NoTPCombat"), eChatType.CT_Important,
                                       eChatLoc.CL_SystemWindow);
            else
                pos.Jump(this, player);
            m_busy = false;
            return 0;
        }
        #endregion

        #region JumpArea
        public void JumpArea()
        {
            if (m_Range <= 0 || JumpPositions.Count < 1)
                return;

            JumpPos pos = JumpPositions["Area"];
            foreach (GamePlayer player in GetPlayersInRadius((ushort)m_Range))
            {
                if (player.Level >= m_MinLevel)
                {
                    if (player.InCombat)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"TeleportNPC.NoTPCombat"),
                                               eChatType.CT_Important,
                                               eChatLoc.CL_SystemWindow);
                    else
                        pos.Jump(this, player);
                }
                else
                    player.Out.SendMessage(string.Format(m_Text_Refuse, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
        #endregion

        #region Database
        public override void LoadFromDatabase(DataObject mobobject)
        {
            base.LoadFromDatabase(mobobject);

            db = GameServer.Database.SelectObject<DBTeleportNPC>(t => t.MobID == InternalID);
            if (db == null)
                return;
            m_Range = db.Range;
            m_MinLevel = db.Level;
            m_Text = db.Text;
            m_Text_Refuse = db.Text_Refuse;
            IsTerritoryLinked = db.IsTerritoryLinked;
            ShowTPIndicator = db.ShowTPIndicator;

            //Set this value only when OR Exclusive
            if (db.IsOutlawFriendly ^ db.IsRegularFriendly)
            {
                if (db.IsRegularFriendly)
                {
                    IsOutlawFriendly = false;
                }

                if (db.IsOutlawFriendly)
                {
                    IsOutlawFriendly = true;
                }
            }
            else if (db.IsRegularFriendly && db.IsOutlawFriendly)
            {
                log.Error("Cannot load IsOutlawFriendly Status because both values are set. Update database(TeleportNPC) for id: " + this.InternalID + " npc: " + this.Name);
            }

            LoadJumpPos();
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();

            bool add = (db == null);
            if (add)
                db = new DBTeleportNPC();

            db.JumpPosition = GetJumpPosString();
            db.Level = m_MinLevel;
            db.MobID = InternalID;
            db.Range = m_Range;
            db.Text = m_Text;
            db.Text_Refuse = m_Text_Refuse;
            db.IsTerritoryLinked = IsTerritoryLinked;

            if (IsOutlawFriendly.HasValue)
            {
                if (IsOutlawFriendly.Value)
                {
                    db.IsOutlawFriendly = true;
                }
                else
                {
                    db.IsRegularFriendly = true;
                }
            }

            if (add)
                GameServer.Database.AddObject(db);
            else
                GameServer.Database.SaveObject(db);
        }
        public override void DeleteFromDatabase()
        {
            base.DeleteFromDatabase();
            if (db != null)
                GameServer.Database.DeleteObject(db);
        }

        private void LoadJumpPos()
        {
            string[] objs = db.JumpPosition.Split('|');
            JumpPositions = new Dictionary<string, JumpPos>(Math.Max(objs.Length, 1));

            foreach (string S_pos in objs)
            {
                if (string.IsNullOrEmpty(S_pos))
                    continue;
                try
                {
                    JumpPos pos = new JumpPos(S_pos);
                    if (!string.IsNullOrEmpty(pos.Name))
                        JumpPositions.Add(pos.Name, pos);
                }
                catch { }
            }
        }

        private string GetJumpPosString()
        {
            if (JumpPositions == null || JumpPositions.Count == 0)
                return "";

            string str = "";
            foreach (JumpPos pos in JumpPositions.Values)
                str += pos + "|";
            return str;
        }
        #endregion

        #region JumpPos Gestion
        public string GetList(GamePlayer player)
        {
            StringBuilder sb = new StringBuilder();
            foreach (JumpPos pos in JumpPositions.Values)
            {
                if (pos.IsInList(player))
                {
                    sb.Append('\n');
                    sb.Append('[');
                    sb.Append(pos.Name);
                    sb.Append("]");
                }
            }
            return sb.ToString();
        }

        public void AddJumpPos(string name, int x, int y, int z, ushort heading, ushort regionID)
        {
            if (JumpPositions.ContainsKey(name))
                JumpPositions[name] = new JumpPos(name, x, y, z, heading, regionID);
            else
                JumpPositions.Add(name, new JumpPos(name, x, y, z, heading, regionID));
        }

        public bool RemoveJumpPos(string name)
        {
            if (JumpPositions.ContainsKey(name))
            {
                JumpPositions.Remove(name);
                return true;
            }
            return false;
        }

        public ArrayList GetJumpList()
        {
            ArrayList jumps = new ArrayList(JumpPositions.Values);
            return jumps;
        }
        #endregion

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            if (JumpPositions == null)
                JumpPositions = new Dictionary<string, JumpPos>();

            if (!(Brain is TeleportNPCBrain))
                SetOwnBrain(new TeleportNPCBrain());

            if (ShowTPIndicator)
            {
                if (m_teleporterIndicator == null)
                {
                    m_teleporterIndicator = new GameNPC();
                    m_teleporterIndicator.Name = "";
                    m_teleporterIndicator.Model = 1923;  // Set your model ID here
                    m_teleporterIndicator.Flags ^= eFlags.PEACE;
                    m_teleporterIndicator.Flags ^= eFlags.CANTTARGET;
                    m_teleporterIndicator.Flags ^= eFlags.DONTSHOWNAME;
                    m_teleporterIndicator.Flags ^= eFlags.FLYING;
                    m_teleporterIndicator.Position = Position + Vector.Create(z: 1);
                    m_teleporterIndicator.CurrentRegionID = CurrentRegionID;
                    m_teleporterIndicator.AddToWorld();
                }
            }

            return true;
        }

        public override bool RemoveFromWorld()
        {
            if (m_teleporterIndicator != null)
            {
                m_teleporterIndicator.RemoveFromWorld();
                m_teleporterIndicator = null;
            }
            return base.RemoveFromWorld();
        }

        public class JumpPos
        {
            public Position Position = Position.Nowhere;
            public string Name;
            public TeleportCondition Conditions;

            public JumpPos(string SaveStr)
            {
                try
                {
                    string[] args = SaveStr.Split(';');
                    Name = args[0];
                    Position = Position.Create(
                        regionID: ushort.Parse(args[5]),
                        x: int.Parse(args[1]),
                        y: int.Parse(args[2]),
                        z: int.Parse(args[3]),
                        heading: ushort.Parse(args[4])
                    );
                    Conditions = new TeleportCondition(args.Length > 6 ? args[6] : "");
                    Conditions.RequiredWhisper = args.Length > 7 ? args[7] : null;
                }
                catch
                {
                    log.Error("TELEPORTNPC: Erreur lors du parsing de \"" + SaveStr + "\".");
                }
            }

            public JumpPos(string name, int x, int y, int z, ushort heading, ushort regionID)
            {
                Name = name;
                Position = Position.Create(
                    regionID, x, y, z, heading
                );
                Conditions = new TeleportCondition("");
            }

            public override string ToString()
            {
                return Name + ";" + Position.X + ";" + Position.Y + ";" + Position.Z + ";" +
                    Position.Orientation.InHeading + ";" + Position.RegionID + ";" + Conditions.GetStringDB();
            }

            public bool IsInList(GamePlayer player)
            {
                return Conditions.Visible && CanJump(player);
            }

            public bool CanJump(GamePlayer player)
            {
                if (!Conditions.IsActiveAtTick(WorldMgr.GetCurrentGameTime(player)))
                    return false;
                if (player.Level < Conditions.LevelMin || player.Level > Conditions.LevelMax)
                    return false;
                if (!string.IsNullOrEmpty(Conditions.Item) && player.Inventory.GetFirstItemByID(Conditions.Item, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) == null)
                    return false;
                if (!string.IsNullOrEmpty(Conditions.RequiredWhisper) && WhisperTracker.TryGetValue(player, out var lastWhisper) && lastWhisper != Conditions.RequiredWhisper)
                    return false;
                if (Conditions.RequiredCompletedQuestID > 0)
                {
                    if (Conditions.RequiredQuestStepID > 0 && !IsPlayerOnQuestStep(player, Conditions.RequiredCompletedQuestID, Conditions.RequiredQuestStepID))
                        return false;
                    if (player.HasFinishedQuest(DataQuestJsonMgr.GetQuest((ushort)Conditions.RequiredCompletedQuestID)) == 0)
                        return false;
                }
                return true;
            }

            private bool IsPlayerOnQuestStep(GamePlayer player, int questID, int stepID)
            {
                var quest = player.IsDoingQuest(DataQuestJsonMgr.GetQuest((ushort)questID));
                if (quest != null)
                {
                    // Ensure the quest has started and check if any of its goals match the given stepID and are active
                    return quest.GoalStates.Any(g => g.GoalId == stepID && g.IsActive);
                }
                return false;
            }

            public void Jump(GameLiving source, GamePlayer player)
            {
                if (player.Level < Conditions.LevelMin || player.Level > Conditions.LevelMax)
                    return;
                if (!string.IsNullOrEmpty(Conditions.Item))
                {
                    if (!player.Inventory.RemoveTemplate(Conditions.Item, 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                        return;
                    InventoryLogging.LogInventoryAction(player, source, eInventoryActionType.Other, Conditions.ItemTemplate, 1);
                }
                player.MoveTo(Position);
                if (Conditions.Bind)
                    player.Bind(true);
            }
        }

        public class TeleportCondition
        {
            private string _item;

            public bool Bind;
            public bool Visible = true;

            public string Item
            {
                get { return _item; }
                set
                {
                    _item = value;
                    ItemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(_item);
                }
            }
            public int LevelMin;
            public int LevelMax = 50;
            public int HourMin;
            public int HourMax = 24;
            public ItemTemplate ItemTemplate { get; private set; }
            public string RequiredWhisper { get; set; }
            public int RequiredCompletedQuestID { get; set; }
            public int RequiredQuestStepID { get; set; }
        
            public bool IsActiveAtTick(uint tick)
            {
                if (HourMin <= 0 && HourMax >= 24)
                {
                    return true;
                }
            
                uint minTick = ((uint)HourMin) * 60 * 60 * 1000;
                uint maxTick = ((uint)HourMax) * 60 * 60 * 1000;

                //Heure
                if (maxTick < minTick && (minTick > tick || tick <= maxTick))
                    return false;
                if (maxTick > minTick && (minTick > tick || tick >= maxTick))
                    return false;
                if (maxTick == minTick && tick != minTick)
                    return false;
                return true;
            }

            public TeleportCondition(string db)
            {
                if (db.Length >= 1)
                {
                    try
                    {
                        string[] args = db.Split('/');
                        foreach (string s in args)
                        {
                            string[] arg = s.Split('=');
                            switch (arg[0])
                            {
                                case "Bind":
                                    Bind = bool.Parse(arg[1]);
                                    break;
                                case "Visible":
                                    Visible = bool.Parse(arg[1]);
                                    break;
                                case "Item":
                                    Item = arg[1];
                                    break;
                                case "LevelMin":
                                    LevelMin = int.Parse(arg[1]);
                                    break;
                                case "LevelMax":
                                    LevelMax = int.Parse(arg[1]);
                                    break;
                                case "HourMin":
                                    HourMin = int.Parse(arg[1]);
                                    break;
                                case "HourMax":
                                    HourMax = int.Parse(arg[1]);
                                    break;
                                case "RequiredWhisper":
                                    RequiredWhisper = arg[1];
                                    break;
                                case "RequiredCompletedQuestID":
                                    RequiredCompletedQuestID = int.Parse(arg[1]);
                                    break;
                                case "RequiredQuestStepID":
                                    RequiredQuestStepID = int.Parse(arg[1]);
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        log.Error("TELEPORTNPC: Erreur lors du parse de \"" + db + "\".");
                    }
                }
            }

            public string GetStringDB()
            {
                StringBuilder sb = new StringBuilder();
                if (!Visible)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("Visible=");
                    sb.Append(Visible);
                }
                if (Bind)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("Bind=");
                    sb.Append(Bind);
                }
                if (!string.IsNullOrEmpty(Item))
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("Item=");
                    sb.Append(Item);
                }
                if (LevelMin > 1)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("LevelMin=");
                    sb.Append(LevelMin);
                }
                if (LevelMax < 50)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("LevelMax=");
                    sb.Append(LevelMax);
                }
                if (HourMin >= 0)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("HourMin=");
                    sb.Append(HourMin);
                }
                if (HourMax >= 0)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("HourMax=");
                    sb.Append(HourMax);
                }
                if (!string.IsNullOrEmpty(RequiredWhisper))
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("RequiredWhisper=");
                    sb.Append(RequiredWhisper);
                }
                if (RequiredCompletedQuestID != 0)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("RequiredCompletedQuestID=");
                    sb.Append(RequiredCompletedQuestID);
                }
                if (RequiredQuestStepID != 0)
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append("RequiredQuestStepID=");
                    sb.Append(RequiredQuestStepID);
                }
                return sb.ToString();
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Bind le joueur après l'avoir TP : ");
                sb.Append(Bind ? "oui" : "non");
                sb.Append("\nVisible dans la liste: ");
                sb.Append(Visible ? "oui" : "non");
                if (!string.IsNullOrEmpty(Item))
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Item: ");
                    sb.Append(Item);
                }
                if (LevelMin > 1 || LevelMax < 50)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Niveaux entre ");
                    sb.Append(LevelMin);
                    sb.Append(" et ");
                    sb.Append(LevelMax);
                }
                if (HourMin >= 0 || HourMax >= 0)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Heures entre ");
                    sb.Append(HourMin);
                    sb.Append(" et ");
                    sb.Append(HourMax);
                }
                if (!string.IsNullOrEmpty(RequiredWhisper))
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Required whisper: ");
                    sb.Append(RequiredWhisper);
                }
                if (RequiredCompletedQuestID != 0)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Required completed quest ID: ");
                    sb.Append(RequiredCompletedQuestID);
                }
                if (RequiredQuestStepID != 0)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append("Required quest step ID: ");
                    sb.Append(RequiredQuestStepID);
                }
                return sb.ToString();
            }
        }
    }
}