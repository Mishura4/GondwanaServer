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
using System.Reflection;
using System.Collections.Generic;

using DOL.Language;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.GS.Spells;

using log4net;
using DOL.GS.Geometry;
using DOL.Bonus;
using System.Linq;

namespace DOL.GS
{
    /// <summary>
    /// This class represents an inventory item
    /// </summary>
    public class GameInventoryItem : InventoryItem, IGameInventoryItem, ITranslatableObject
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected GamePlayer m_owner = null;


        public GameInventoryItem()
            : base()
        {
            this.BonusConditions = new List<BonusCondition>();
        }

        public GameInventoryItem(ItemTemplate template)
            : base(template)
        {
            this.BonusConditions = BonusCondition.LoadFromString(template.BonusConditions)?.ToList();
        }

        public GameInventoryItem(ItemUnique template)
            : base(template)
        {
            this.BonusConditions = BonusCondition.LoadFromString(template.BonusConditions)?.ToList();
        }

        public GameInventoryItem(InventoryItem item)
            : base(item)
        {
            OwnerID = item.OwnerID;
            ObjectId = item.ObjectId;
            this.BonusConditions = BonusCondition.LoadFromString(item.Template?.BonusConditions)?.ToList();
        }

        public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier
        {
            get { return LanguageDataObject.eTranslationIdentifier.eItem; }
        }

        public List<BonusCondition> BonusConditions
        {
            get;
            protected set;
        }

        /// <summary>
        /// Holds the translation id.
        /// </summary>
        protected string m_translationId = "";

        /// <summary>
        /// Gets or sets the translation id.
        /// </summary>
        public string TranslationId
        {
            get { return m_translationId; }
            set { m_translationId = (value == null ? "" : value); }
        }

        /// <summary>
        /// Is this a valid item for this player?
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual bool CheckValid(GamePlayer player)
        {
            m_owner = player;
            return true;
        }

        /// <summary>
        /// Can this item be saved or loaded from the database?
        /// </summary>
        public virtual bool CanPersist
        {
            get
            {
                if (Id_nb == InventoryItem.BLANK_ITEM)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Can player equip this item?
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual bool CanEquip(GamePlayer player)
        {
            return GameServer.ServerRules.CheckAbilityToUseItem(player, Template);
        }

        #region Create From Object Source

        /// <summary>
        /// This is used to create a PlayerInventoryItem
        /// ClassType will be checked and the approrpiate GameInventoryItem created
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        [Obsolete("Use Create() instead")]
        public static GameInventoryItem Create<T>(ItemTemplate item)
        {
            return Create(item);
        }



        private BonusCondition GetBonusCondition(string bonusName)
        {
            return this.BonusConditions?.FirstOrDefault(b => b.BonusName.Equals(bonusName));
        }


        public bool IsBonusAllowed(string bonusName, GamePlayer player)
        {
            var bonusCondition = this.GetBonusCondition(bonusName);

            //If bonus not present, it is allowed
            if (bonusCondition == null)
            {
                return true;
            }

            if (bonusCondition.ChampionLevel > 0 && player.ChampionLevel < bonusCondition.ChampionLevel)
            {
                return false;
            }

            if (bonusCondition.MlLevel > 0 && player.MLLevel < bonusCondition.MlLevel)
            {
                return false;
            }

            if (bonusCondition.IsRenaissanceRequired && !player.IsRenaissance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This is used to create a PlayerInventoryItem
        /// template.ClassType will be checked and the approrpiate GameInventoryItem created
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        [Obsolete("Use Create() instead")]
        public static GameInventoryItem Create<T>(InventoryItem item)
        {
            return Create(item);
        }

        /// <summary>
        /// This is used to create a PlayerInventoryItem
        /// ClassType will be checked and the approrpiate GameInventoryItem created
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static GameInventoryItem Create(ItemTemplate item)
        {
            string classType = item.ClassType;
            var itemUnique = item as ItemUnique;

            if (!string.IsNullOrEmpty(classType))
            {
                var itemClass = item.ClassType.ToLower();
                var itemIsSpecial = itemClass.StartsWith("currency") || itemClass.StartsWith("token");
                if (!itemIsSpecial)
                {
                    GameInventoryItem gameItem;
                    if (itemUnique != null)
                        gameItem = ScriptMgr.CreateObjectFromClassType<GameInventoryItem, ItemUnique>(classType, itemUnique);
                    else
                        gameItem = ScriptMgr.CreateObjectFromClassType<GameInventoryItem, ItemTemplate>(classType, item);

                    if (gameItem != null)
                        return gameItem;

                    if (log.IsWarnEnabled)
                        log.WarnFormat("Failed to construct game inventory item of ClassType {0}!", classType);
                }
            }

            if (itemUnique != null)
                return new GameInventoryItem(itemUnique);

            return new GameInventoryItem(item);
        }

        /// <summary>
        /// This is used to create a PlayerInventoryItem
        /// template.ClassType will be checked and the approrpiate GameInventoryItem created
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static GameInventoryItem Create(InventoryItem item)
        {
            string classType = item.Template.ClassType;
            var itemIsNoCurrency = !item.ClassType.ToLower().StartsWith("currency.");

            if (!string.IsNullOrEmpty(classType) && itemIsNoCurrency)
            {
                GameInventoryItem gameItem = ScriptMgr.CreateObjectFromClassType<GameInventoryItem, InventoryItem>(classType, item);

                if (gameItem != null)
                    return gameItem;

                if (log.IsWarnEnabled)
                    log.WarnFormat("Failed to construct game inventory item of ClassType {0}!", classType);
            }

            return new GameInventoryItem(item);
        }

        #endregion

        /// <summary>
        /// Player receives this item (added to players inventory)
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnReceive(GamePlayer player)
        {
            m_owner = player;
        }

        /// <summary>
        /// Player loses this item (removed from inventory)
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnLose(GamePlayer player)
        {
            m_owner = null;
        }

        /// <summary>
        /// Drop this item on the ground
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual WorldInventoryItem Drop(GamePlayer player)
        {
            WorldInventoryItem worldItem = new WorldInventoryItem(this);

            var itemPosition = player.Position + Vector.Create(player.Orientation, length: 30);
            worldItem.Position = itemPosition;

            worldItem.AddOwner(player);
            worldItem.AddToWorld();

            return worldItem;
        }

        /// <summary>
        /// This object is being removed from the world
        /// </summary>
        public virtual void OnRemoveFromWorld()
        {
        }

        /// <summary>
        /// Player equips this item
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnEquipped(GamePlayer player)
        {
            CheckValid(player);
        }

        /// <summary>
        /// Player unequips this item
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnUnEquipped(GamePlayer player)
        {
            CheckValid(player);
        }

        /// <summary>
        /// This inventory is used for a spell cast (staves lose condition when spells are cast)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        public virtual void OnSpellCast(GameLiving owner, GameObject target, Spell spell)
        {
            OnStrikeTarget(owner, target);
        }

        /// <summary>
        /// This inventory strikes an enemy
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        public virtual void OnStrikeTarget(GameLiving owner, GameObject target)
        {
            if (owner is GamePlayer)
            {
                GamePlayer player = owner as GamePlayer;

                if (ConditionPercent > 70 && Util.Chance(ServerProperties.Properties.ITEM_CONDITION_LOSS_CHANCE))
                {
                    int oldPercent = ConditionPercent;
                    double con = GamePlayer.GetConLevel(player.Level, Level);
                    if (con < -3.0)
                        con = -3.0;
                    int sub = (int)(con + 4);
                    if (oldPercent < 91)
                    {
                        sub *= 2;
                    }

                    // Subtract condition
                    Condition -= sub;
                    if (Condition < 0)
                        Condition = 0;

                    if (ConditionPercent != oldPercent)
                    {
                        if (ConditionPercent == 90)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Attack.CouldRepair", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        else if (ConditionPercent == 80)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Attack.NeedRepair", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        else if (ConditionPercent == 70)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Attack.NeedRepairDire", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        player.Out.SendUpdateWeaponAndArmorStats();
                        player.Out.SendInventorySlotsUpdate(new int[] { SlotPosition });
                    }
                }
            }
        }

        /// <summary>
        /// This inventory is struck by an enemy
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enemy"></param>
        public virtual void OnStruckByEnemy(GameLiving owner, GameLiving enemy)
        {
            if (owner is GamePlayer)
            {
                GamePlayer player = owner as GamePlayer;

                if (ConditionPercent > 70 && Util.Chance(ServerProperties.Properties.ITEM_CONDITION_LOSS_CHANCE))
                {
                    int oldPercent = ConditionPercent;
                    double con = GamePlayer.GetConLevel(player.Level, Level);
                    if (con < -3.0)
                        con = -3.0;
                    int sub = (int)(con + 4);
                    if (oldPercent < 91)
                    {
                        sub *= 2;
                    }

                    // Subtract condition
                    Condition -= sub;
                    if (Condition < 0)
                        Condition = 0;

                    if (ConditionPercent != oldPercent)
                    {
                        if (ConditionPercent == 90)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Attack.CouldRepair", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        else if (ConditionPercent == 80)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Attack.NeedRepair", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        else if (ConditionPercent == 70)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Attack.NeedRepairDire", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        player.Out.SendUpdateWeaponAndArmorStats();
                        player.Out.SendInventorySlotsUpdate(new int[] { SlotPosition });
                    }
                }
            }
        }

        /// <summary>
        /// Try and use this item
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if item use is handled here</returns>
        public virtual bool Use(GamePlayer player)
        {
            return false;
        }


        /// <summary>
        /// Combine this item with the target item
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetItem"></param>
        /// <returns>true if combine is handled here</returns>
        public virtual bool Combine(GamePlayer player, InventoryItem targetItem)
        {
            return false;
        }

        /// <summary>
        /// Delve this item
        /// </summary>
        /// <param name="delve"></param>
        /// <param name="player"></param>
        public virtual void Delve(List<String> delve, GamePlayer player)
        {
            if (player == null)
                return;

            //**********************************
            //show crafter name
            //**********************************
            if (IsCrafted)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.CrafterName", Creator));
                delve.Add(" ");
            }
            if (Description != null && Description != "")
            {
                delve.Add(Description);
                delve.Add(" ");
            }

            WriteUsableClasses(delve, player.Client);
            if ((Object_Type >= (int)eObjectType.GenericWeapon) && (Object_Type <= (int)eObjectType._LastWeapon))
            {
                WriteMagicalBonuses(delve, player.Client, false);
                DelveWeaponStats(delve, player);
            }

            if (Object_Type == (int)eObjectType.Instrument)
            {
                WriteMagicalBonuses(delve, player.Client, false);
            }

            if (Object_Type >= (int)eObjectType.Cloth && Object_Type <= (int)eObjectType.Scale)
            {
                WriteMagicalBonuses(delve, player.Client, false);
                DelveArmorStats(delve, player);
            }

            if (Object_Type == (int)eObjectType.Shield)
            {
                WriteMagicalBonuses(delve, player.Client, false);
                DelveShieldStats(delve, player.Client);
            }

            if (Object_Type == (int)eObjectType.Magical || Object_Type == (int)eObjectType.AlchemyTincture || Object_Type == (int)eObjectType.SpellcraftGem)
            {
                WriteUsableClasses(delve, player.Client);
                WriteMagicalBonuses(delve, player.Client, false);
            }

            //***********************************
            //shows info for Poison Potions
            //***********************************
            if (Object_Type == (int)eObjectType.Poison)
            {
                WritePoisonInfo(delve, player.Client);
            }

            if (Object_Type == (int)eObjectType.Magical && Item_Type == (int)eInventorySlot.FirstBackpack) // potion
            {
                WritePotionInfo(delve, player.Client);
            }
            else if (CanUseEvery > 0)
            {
                // Items with a reuse timer (aka cooldown).
                delve.Add(" ");

                int minutes = CanUseEvery / 60;
                int seconds = CanUseEvery % 60;

                if (minutes == 0)
                {
                    delve.Add(String.Format("Can use item every: {0} sec", seconds));
                }
                else
                {
                    delve.Add(String.Format("Can use item every: {0}:{1:00} min", minutes, seconds));
                }

                // delve.Add(String.Format("Can use item every: {0:00}:{1:00}", minutes, seconds));

                int cooldown = CanUseAgainIn;

                if (cooldown > 0)
                {
                    minutes = cooldown / 60;
                    seconds = cooldown % 60;

                    if (minutes == 0)
                    {
                        delve.Add(String.Format("Can use again in: {0} sec", seconds));
                    }
                    else
                    {
                        delve.Add(String.Format("Can use again in: {0}:{1:00} min", minutes, seconds));
                    }
                }
            }

            if (!IsDropable || !IsPickable || IsIndestructible)
                delve.Add(" ");

            if (!IsPickable)
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.CannotTraded"));

            if (!IsDropable)
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.CannotSold"));

            if (IsIndestructible)
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.CannotDestroyed"));

            if (BonusLevel > 0)
            {
                delve.Add(" ");
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.BonusLevel", BonusLevel));
            }

            //Add admin info
            if (player.Client.Account.PrivLevel > 1)
            {
                WriteTechnicalInfo(delve, player.Client);
            }
        }

        protected virtual void WriteUsableClasses(IList<string> output, GameClient client)
        {
            if (Util.IsEmpty(AllowedClasses, true))
                return;

            output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteUsableClasses.UsableBy"));

            foreach (string allowed in Util.SplitCSV(AllowedClasses, true))
            {
                int classID = -1;
                if (int.TryParse(allowed, out classID))
                {
                    output.Add("- " + ((eCharacterClass)classID).ToString());
                }
                else
                {
                    log.Error(Id_nb + " has an invalid entry for allowed classes '" + allowed + "'");
                }
            }
        }


        protected virtual void WriteMagicalBonuses(IList<string> output, GameClient client, bool shortInfo)
        {
            int oldCount = output.Count;

            output.Add("Total utility: " + String.Format("{0:0.00}", GetTotalUtility()));
            output.Add(" ");

            WriteBonusLine(output, client, Bonus1Type, Bonus1);
            WriteBonusLine(output, client, Bonus2Type, Bonus2);
            WriteBonusLine(output, client, Bonus3Type, Bonus3);
            WriteBonusLine(output, client, Bonus4Type, Bonus4);
            WriteBonusLine(output, client, Bonus5Type, Bonus5);
            WriteBonusLine(output, client, Bonus6Type, Bonus6);
            WriteBonusLine(output, client, Bonus7Type, Bonus7);
            WriteBonusLine(output, client, Bonus8Type, Bonus8);
            WriteBonusLine(output, client, Bonus9Type, Bonus9);
            WriteBonusLine(output, client, Bonus10Type, Bonus10);
            WriteBonusLine(output, client, ExtraBonusType, ExtraBonus);


            output.Add(" ");

            /* BONUS REQUIREMENTS */
            if (this.BonusConditions?.Any(b => b.ChampionLevel > 0 || b.MlLevel > 0 || b.IsRenaissanceRequired) == true)
            {
                output.Add(" CONDITIONS DE BONUS: ");
                foreach (var condition in this.BonusConditions.Where(b => b.BonusName != nameof(this.ProcSpellID) || b.BonusName != nameof(this.ProcSpellID1)).OrderBy(b => b.BonusName))
                {
                    if (condition.ChampionLevel > 0 || condition.MlLevel > 0 || condition.IsRenaissanceRequired)
                        output.Add(condition.BonusName + "( " + this.GetBonusTypeFromBonusName(client, condition.BonusName) + " ): Level Champion: " + condition.ChampionLevel + " | ML Level: " + condition.MlLevel + " | Renaissance: " + (condition.IsRenaissanceRequired ? "Oui" : "Non"));
                }
            }

            if (output.Count > oldCount)
            {
                output.Add(" ");
                output.Insert(oldCount, LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MagicBonus"));
                output.Insert(oldCount, " ");
            }

            oldCount = output.Count;

            WriteFocusLine(client, output, Bonus1Type, Bonus1);
            WriteFocusLine(client, output, Bonus2Type, Bonus2);
            WriteFocusLine(client, output, Bonus3Type, Bonus3);
            WriteFocusLine(client, output, Bonus4Type, Bonus4);
            WriteFocusLine(client, output, Bonus5Type, Bonus5);
            WriteFocusLine(client, output, Bonus6Type, Bonus6);
            WriteFocusLine(client, output, Bonus7Type, Bonus7);
            WriteFocusLine(client, output, Bonus8Type, Bonus8);
            WriteFocusLine(client, output, Bonus9Type, Bonus9);
            WriteFocusLine(client, output, Bonus10Type, Bonus10);
            WriteFocusLine(client, output, ExtraBonusType, ExtraBonus);

            if (output.Count > oldCount)
            {
                output.Add(" ");
                output.Insert(oldCount, LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.FocusBonus"));
                output.Insert(oldCount, " ");
            }

            if (!shortInfo)
            {
                if (ProcSpellID != 0 || ProcSpellID1 != 0 || SpellID != 0 || SpellID1 != 0)
                {
                    int requiredLevel = LevelRequirement > 0 ? LevelRequirement : Math.Min(50, Level);
                    if (requiredLevel > 1)
                    {
                        output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.LevelRequired2", requiredLevel));
                        output.Add(" ");
                    }
                }

                if (Object_Type == (int)eObjectType.Magical && Item_Type == (int)eInventorySlot.FirstBackpack) // potion
                {
                    // let WritePotion handle the rest of the display
                    return;
                }


                #region Proc1
                if (ProcSpellID != 0)
                {
                    string spellNote = "";
                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MagicAbility"));
                    if (GlobalConstants.IsWeapon(Object_Type))
                    {
                        spellNote = LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.StrikeEnemy");
                    }
                    else if (GlobalConstants.IsArmor(Object_Type))
                    {
                        spellNote = LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.StrikeArmor");
                    }

                    SpellLine line = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                    if (line != null)
                    {
                        Spell procSpell = SkillBase.FindSpell(ProcSpellID, line);

                        if (procSpell != null)
                        {
                            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, procSpell, line);
                            if (spellHandler != null)
                            {
                                Util.AddRange(output, spellHandler.DelveInfo);
                                output.Add(" ");
                            }
                            else
                            {
                                output.Add("-" + procSpell.Name + " (Spell Handler Not Implemented)");
                            }

                            output.Add(spellNote);
                        }
                        else
                        {
                            output.Add("- Spell Not Found: " + ProcSpellID);
                        }
                    }
                    else
                    {
                        output.Add("- Item_Effects Spell Line Missing");
                    }

                    output.Add(" ");

                    if (this.BonusConditions != null)
                    {
                        var procCondition = this.BonusConditions.FirstOrDefault(b => b.BonusName.Equals(nameof(this.ProcSpellID)));

                        if (procCondition != null)
                        {
                            output.Add(" ProcSpellID proc Conditions: ");
                            output.Add(procCondition.BonusName + "( " + this.GetBonusTypeFromBonusName(client, procCondition.BonusName) + " ) : Level Champion: " + procCondition.ChampionLevel + " | ML Level: " + procCondition.MlLevel + " | Renaissance: " + (procCondition.IsRenaissanceRequired ? "Oui" : "Non"));
                            output.Add(" ");
                        }
                    }
                }
                #endregion
                #region Proc2
                if (ProcSpellID1 != 0)
                {
                    string spellNote = "";
                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MagicAbility"));
                    if (GlobalConstants.IsWeapon(Object_Type))
                    {
                        spellNote = LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.StrikeEnemy");
                    }
                    else if (GlobalConstants.IsArmor(Object_Type))
                    {
                        spellNote = LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.StrikeArmor");
                    }

                    SpellLine line = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                    if (line != null)
                    {
                        Spell procSpell = SkillBase.FindSpell(ProcSpellID1, line);

                        if (procSpell != null)
                        {
                            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, procSpell, line);
                            if (spellHandler != null)
                            {
                                Util.AddRange(output, spellHandler.DelveInfo);
                                output.Add(" ");
                            }
                            else
                            {
                                output.Add("-" + procSpell.Name + " (Spell Handler Not Implemented)");
                            }

                            output.Add(spellNote);
                        }
                        else
                        {
                            output.Add("- Spell Not Found: " + ProcSpellID1);
                        }
                    }
                    else
                    {
                        output.Add("- Item_Effects Spell Line Missing");
                    }

                    output.Add(" ");

                    if (this.BonusConditions != null)
                    {
                        var procCondition = this.BonusConditions.FirstOrDefault(b => b.BonusName.Equals(nameof(this.ProcSpellID1)));

                        if (procCondition != null)
                        {
                            output.Add(" ProcSpellID1 2 proc Conditions: ");
                            output.Add(procCondition.BonusName + "( " + this.GetBonusTypeFromBonusName(client, procCondition.BonusName) + " ) : Level Champion: " + procCondition.ChampionLevel + " | ML Level: " + procCondition.MlLevel + " | Renaissance: " + (procCondition.IsRenaissanceRequired ? "Oui" : "Non"));
                            output.Add(" ");
                        }
                    }
                }
                #endregion
                #region Charge1
                if (SpellID != 0)
                {
                    SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                    if (chargeEffectsLine != null)
                    {
                        Spell spell = SkillBase.FindSpell(SpellID, chargeEffectsLine);
                        if (spell != null)
                        {
                            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, spell, chargeEffectsLine);

                            if (spellHandler != null)
                            {
                                if (MaxCharges > 0)
                                {
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.ChargedMagic"));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.Charges", Charges));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MaxCharges", MaxCharges));
                                    output.Add(" ");
                                }

                                Util.AddRange(output, spellHandler.DelveInfo);
                                output.Add(" ");
                                output.Add("- This spell is cast when the item is used.");
                            }
                            else
                            {
                                output.Add("- Item_Effects Spell Line Missing");
                            }
                        }
                        else
                        {
                            output.Add("- Spell Not Found: " + SpellID);
                        }
                    }

                    output.Add(" ");
                }
                #endregion
                #region Charge2
                if (SpellID1 != 0)
                {
                    SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                    if (chargeEffectsLine != null)
                    {
                        Spell spell = SkillBase.FindSpell(SpellID1, chargeEffectsLine);
                        if (spell != null)
                        {
                            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, spell, chargeEffectsLine);

                            if (spellHandler != null)
                            {
                                if (MaxCharges > 0)
                                {
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.ChargedMagic"));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.Charges", Charges1));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MaxCharges", MaxCharges1));
                                    output.Add(" ");
                                }

                                Util.AddRange(output, spellHandler.DelveInfo);
                                output.Add(" ");
                                output.Add("- This spell is cast when the item is used.");
                            }
                            else
                            {
                                output.Add("- Item_Effects Spell Line Missing");
                            }
                        }
                        else
                        {
                            output.Add("- Spell Not Found: " + SpellID1);
                        }
                    }

                    output.Add(" ");
                }
                #endregion
                #region Poison
                if (PoisonSpellID != 0)
                {
                    if (GlobalConstants.IsWeapon(Object_Type) || (eObjectType)Object_Type == eObjectType.Poison)// Poisoned Weapon
                    {
                        SpellLine poisonLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mundane_Poisons);
                        if (poisonLine != null)
                        {
                            List<Spell> spells = SkillBase.GetSpellList(poisonLine.KeyName);
                            foreach (Spell spl in spells)
                            {
                                if (spl.ID == PoisonSpellID)
                                {
                                    output.Add(" ");
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.LevelRequired"));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.Level", spl.Level));
                                    output.Add(" ");
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.ChargedMagic"));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.Charges", PoisonCharges));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MaxCharges", PoisonMaxCharges));
                                    output.Add(" ");

                                    ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, spl, poisonLine);
                                    if (spellHandler != null)
                                    {
                                        Util.AddRange(output, spellHandler.DelveInfo);
                                        output.Add(" ");
                                    }
                                    else
                                    {
                                        output.Add("-" + spl.Name + "(Not implemented yet)");
                                    }
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.StrikeEnemy"));
                                    return;
                                }
                            }
                        }
                    }

                    SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                    if (chargeEffectsLine != null)
                    {
                        List<Spell> spells = SkillBase.GetSpellList(chargeEffectsLine.KeyName);
                        foreach (Spell spl in spells)
                        {
                            if (spl.ID == SpellID)
                            {
                                output.Add(" ");
                                output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.LevelRequired"));
                                output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.Level", spl.Level));
                                output.Add(" ");
                                if (MaxCharges > 0)
                                {
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.ChargedMagic"));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.Charges", Charges));
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MaxCharges", MaxCharges));
                                }
                                else
                                {
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MagicAbility"));
                                }
                                output.Add(" ");

                                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, spl, chargeEffectsLine);
                                if (spellHandler != null)
                                {
                                    Util.AddRange(output, spellHandler.DelveInfo);
                                    output.Add(" ");
                                }
                                else
                                {
                                    output.Add("-" + spl.Name + "(Not implemented yet)");
                                }
                                output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.UsedItem"));
                                output.Add(" ");
                                if (spl.RecastDelay > 0)
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.UseItem1", Util.FormatTime(spl.RecastDelay / 1000)));
                                else
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.UseItem2"));
                                long lastChargedItemUseTick = client.Player.TempProperties.getProperty<long>(GamePlayer.LAST_CHARGED_ITEM_USE_TICK);
                                long changeTime = client.Player.CurrentRegion.Time - lastChargedItemUseTick;
                                long recastDelay = (spl.RecastDelay > 0) ? spl.RecastDelay : 60000 * 3;
                                if (changeTime < recastDelay) //3 minutes reuse timer
                                    output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.UseItem3", Util.FormatTime((recastDelay - changeTime) / 1000)));
                                return;
                            }
                        }
                    }
                }
                #endregion
            }
        }

        public double GetTotalUtility()
        {
            double totalUti = 0;
            //based off of eProperty
            //1-8 == stats = *.6667
            //9 == power cap = *2
            //10 == maxHP =  *.25
            //11-19 == resists = *2
            //59 == Crafting skill gain = *2
            //71 == Robbery resist chance = *2
            //20-115 == skill = *5
            //116 == Crafting speed = *2
            //117 == Secondary style spell chance = *2
            //118 == Mythical regeneration = *5
            //119 == Tension gain = *2
            //145 == MaxSpeed = *2
            //146 == SpellReflectionChance = *2
            //147 == MaxConcentration = *2
            //148 == ArmorFactor = *1
            //149 == ArmorAbsorption = *2
            //150-155 Regeneration/Range = *5
            //156 == acuity = *.6667
            //163 == all magic = *5
            //164 == all melee = *5
            //167 == all dual weild = *5
            //168 == all archery = *5
            //169-172	evade/parry chance/fatigue consumption	*2
            //173	TOA Melee damage	*5
            //174	TOA Ranged damage	*5
            //175-186	TOA spell duration reduction *2
            //187	TOA Hit point Bonus	*0.25
            //188	TOA Archery speed	*5
            //189-190	TOA Arrow recover/Debuff	*2
            //191	TOA Casting speed	*5
            //192-195	TOA Debuff/Fatigue/healing	*2
            //196	TOA Power pool	*2
            //197	TOA Resist Pierce	*5
            //198	TOA Spell Damage	*5
            //199	TOA Spell Duration	*2
            //200	TOA Style Damage	*5
            //201-209	TOA Skill Cap	*2
            //210	TOA Hit point Cap	*0.25
            //211	TOA Power pool cap	*2
            //212 == weapon skills = *5
            //213 == all skills = *5
            //214-217 Critical Hit/waterspeed = *5
            //217-220	TOA Spell Level/Miss hit/Keep	*2
            //221-229	Mythical Resist and Cap	*4
            //230-231	TOA DPS/Magic Absorption	*2
            //232-235 Critical Heal/Mythical fall/Coin/Discumbering = *5
            //236-245	Mythical Stat and Cap Increase	*4
            //247-250 BP/XP/Natural/Extra HP = *5
            //251-252 Conversion/Style Absorb = *2
            //253-255 RP/Arcane = *5
            if (Bonus1Type != 0 &&
                Bonus1 != 0)
            {
                if (Bonus1Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus1 * .6667;
                }
                else if (Bonus1Type == 9 || Bonus1Type == 148)
                {
                    totalUti += Bonus1;
                }
                else if (Bonus1Type == 10 || Bonus1Type == 187 || Bonus1Type == 210)
                {
                    totalUti += Bonus1 * .25;
                }
                else if (Bonus1Type < 20
                    || Bonus1Type == 59
                    || Bonus1Type == 71
                    || Bonus1Type == 116
                    || Bonus1Type == 117
                    || Bonus1Type == 119
                    || Bonus1Type >= 145 && Bonus1Type <= 149
                    || Bonus1Type >= 169 && Bonus1Type <= 172
                    || Bonus1Type >= 175 && Bonus1Type <= 186
                    || Bonus1Type == 189
                    || Bonus1Type == 190
                    || Bonus1Type >= 192 && Bonus1Type <= 196
                    || Bonus1Type == 199
                    || Bonus1Type >= 201 && Bonus1Type <= 209
                    || Bonus1Type == 211
                    || Bonus1Type >= 217 && Bonus1Type <= 220
                    || Bonus1Type == 230
                    || Bonus1Type == 231
                    || Bonus1Type == 251
                    || Bonus1Type == 252)
                {
                    totalUti += Bonus1 * 2;
                }
                else if (Bonus1Type >= 221 && Bonus1Type <= 229 || Bonus1Type >= 236 && Bonus1Type <= 245)
                {
                    totalUti += Bonus1 * 4;
                }
                else if (Bonus1Type >= 20 && Bonus1Type <= 58 || Bonus1Type >= 60 && Bonus1Type <= 70 || Bonus1Type >= 72 && Bonus1Type <= 115)
                {
                    totalUti += Bonus1 * 5;
                }
                else if (Bonus1Type == 118
                  || Bonus1Type >= 150 && Bonus1Type <= 155
                  || Bonus1Type >= 163 && Bonus1Type <= 168
                  || Bonus1Type == 173
                  || Bonus1Type == 174
                  || Bonus1Type == 188
                  || Bonus1Type == 191
                  || Bonus1Type == 197
                  || Bonus1Type == 198
                  || Bonus1Type == 200
                  || Bonus1Type >= 212 && Bonus1Type <= 217
                  || Bonus1Type >= 232 && Bonus1Type <= 235
                  || Bonus1Type >= 247 && Bonus1Type <= 250
                  || Bonus1Type >= 253 && Bonus1Type <= 255)
                {
                    totalUti += Bonus1 * 5;
                }
            }

            if (Bonus2Type != 0 &&
                Bonus2 != 0)
            {
                if (Bonus2Type < 9 || Bonus2Type == 156)
                {
                    totalUti += Bonus2 * .6667;
                }
                else if (Bonus2Type == 9 || Bonus2Type == 148)
                {
                    totalUti += Bonus2;
                }
                else if (Bonus2Type == 10 || Bonus2Type == 187 || Bonus2Type == 210)
                {
                    totalUti += Bonus2 * .25;
                }
                else if (Bonus2Type < 20
                    || Bonus2Type == 59
                    || Bonus2Type == 71
                    || Bonus2Type == 116
                    || Bonus2Type == 117
                    || Bonus2Type == 119
                    || Bonus2Type >= 145 && Bonus2Type <= 149
                    || Bonus2Type >= 169 && Bonus2Type <= 172
                    || Bonus2Type >= 175 && Bonus2Type <= 186
                    || Bonus2Type == 189
                    || Bonus2Type == 190
                    || Bonus2Type >= 192 && Bonus2Type <= 196
                    || Bonus2Type == 199
                    || Bonus2Type >= 201 && Bonus2Type <= 209
                    || Bonus2Type == 211
                    || Bonus2Type >= 217 && Bonus2Type <= 220
                    || Bonus2Type == 230
                    || Bonus2Type == 231
                    || Bonus2Type == 251
                    || Bonus2Type == 252)
                {
                    totalUti += Bonus2 * 2;
                }
                else if (Bonus2Type >= 221 && Bonus2Type <= 229 || Bonus2Type >= 236 && Bonus2Type <= 245)
                {
                    totalUti += Bonus2 * 4;
                }
                else if (Bonus2Type >= 20 && Bonus2Type <= 58 || Bonus2Type >= 60 && Bonus2Type <= 70 || Bonus2Type >= 72 && Bonus2Type <= 115)
                {
                    totalUti += Bonus2 * 5;
                }
                else if (Bonus2Type == 118
                  || Bonus2Type >= 150 && Bonus2Type <= 155
                  || Bonus2Type >= 163 && Bonus2Type <= 168
                  || Bonus2Type == 173
                  || Bonus2Type == 174
                  || Bonus2Type == 188
                  || Bonus2Type == 191
                  || Bonus2Type == 197
                  || Bonus2Type == 198
                  || Bonus2Type == 200
                  || Bonus2Type >= 212 && Bonus2Type <= 217
                  || Bonus2Type >= 232 && Bonus2Type <= 235
                  || Bonus2Type >= 247 && Bonus2Type <= 250
                  || Bonus2Type >= 253 && Bonus2Type <= 255)
                {
                    totalUti += Bonus2 * 5;
                }
            }

            if (Bonus3Type != 0 &&
                Bonus3 != 0)
            {
                if (Bonus3Type < 9 || Bonus3Type == 156)
                {
                    totalUti += Bonus3 * .6667;
                }
                else if (Bonus3Type == 9 || Bonus3Type == 148)
                {
                    totalUti += Bonus3;
                }
                else if (Bonus3Type == 10 || Bonus3Type == 187 || Bonus3Type == 210)
                {
                    totalUti += Bonus3 * .25;
                }
                else if (Bonus3Type < 20
                    || Bonus3Type == 59
                    || Bonus3Type == 71
                    || Bonus3Type == 116
                    || Bonus3Type == 117
                    || Bonus3Type == 119
                    || Bonus3Type >= 145 && Bonus3Type <= 149
                    || Bonus3Type >= 169 && Bonus3Type <= 172
                    || Bonus3Type >= 175 && Bonus3Type <= 186
                    || Bonus3Type == 189
                    || Bonus3Type == 190
                    || Bonus3Type >= 192 && Bonus3Type <= 196
                    || Bonus3Type == 199
                    || Bonus3Type >= 201 && Bonus3Type <= 209
                    || Bonus3Type == 211
                    || Bonus3Type >= 217 && Bonus3Type <= 220
                    || Bonus3Type == 230
                    || Bonus3Type == 231
                    || Bonus3Type == 251
                    || Bonus3Type == 252)
                {
                    totalUti += Bonus3 * 2;
                }
                else if (Bonus3Type >= 221 && Bonus3Type <= 229 || Bonus3Type >= 236 && Bonus3Type <= 245)
                {
                    totalUti += Bonus3 * 4;
                }
                else if (Bonus3Type >= 20 && Bonus3Type <= 58 || Bonus3Type >= 60 && Bonus3Type <= 70 || Bonus3Type >= 72 && Bonus3Type <= 115)
                {
                    totalUti += Bonus3 * 5;
                }
                else if (Bonus3Type == 118
                  || Bonus3Type >= 150 && Bonus3Type <= 155
                  || Bonus3Type >= 163 && Bonus3Type <= 168
                  || Bonus3Type == 173
                  || Bonus3Type == 174
                  || Bonus3Type == 188
                  || Bonus3Type == 191
                  || Bonus3Type == 197
                  || Bonus3Type == 198
                  || Bonus3Type == 200
                  || Bonus3Type >= 212 && Bonus3Type <= 217
                  || Bonus3Type >= 232 && Bonus3Type <= 235
                  || Bonus3Type >= 247 && Bonus3Type <= 250
                  || Bonus3Type >= 253 && Bonus3Type <= 255)
                {
                    totalUti += Bonus3 * 5;
                }

            }

            if (Bonus4Type != 0 &&
                Bonus4 != 0)
            {
                if (Bonus4Type < 9 || Bonus4Type == 156)
                {
                    totalUti += Bonus4 * .6667;
                }
                else if (Bonus4Type == 9 || Bonus4Type == 148)
                {
                    totalUti += Bonus4;
                }
                else if (Bonus4Type == 10 || Bonus4Type == 187 || Bonus4Type == 210)
                {
                    totalUti += Bonus4 * .25;
                }
                else if (Bonus4Type < 20
                    || Bonus4Type == 59
                    || Bonus4Type == 71
                    || Bonus4Type == 116
                    || Bonus4Type == 117
                    || Bonus4Type == 119
                    || Bonus4Type >= 145 && Bonus4Type <= 149
                    || Bonus4Type >= 169 && Bonus4Type <= 172
                    || Bonus4Type >= 175 && Bonus4Type <= 186
                    || Bonus4Type == 189
                    || Bonus4Type == 190
                    || Bonus4Type >= 192 && Bonus4Type <= 196
                    || Bonus4Type == 199
                    || Bonus4Type >= 201 && Bonus4Type <= 209
                    || Bonus4Type == 211
                    || Bonus4Type >= 217 && Bonus4Type <= 220
                    || Bonus4Type == 230
                    || Bonus4Type == 231
                    || Bonus4Type == 251
                    || Bonus4Type == 252)
                {
                    totalUti += Bonus4 * 2;
                }
                else if (Bonus4Type >= 221 && Bonus4Type <= 229 || Bonus4Type >= 236 && Bonus4Type <= 245)
                {
                    totalUti += Bonus4 * 4;
                }
                else if (Bonus4Type >= 20 && Bonus4Type <= 58 || Bonus4Type >= 60 && Bonus4Type <= 70 || Bonus4Type >= 72 && Bonus4Type <= 115)
                {
                    totalUti += Bonus4 * 5;
                }
                else if (Bonus4Type == 118
                  || Bonus4Type >= 150 && Bonus4Type <= 155
                  || Bonus4Type >= 163 && Bonus4Type <= 168
                  || Bonus4Type == 173
                  || Bonus4Type == 174
                  || Bonus4Type == 188
                  || Bonus4Type == 191
                  || Bonus4Type == 197
                  || Bonus4Type == 198
                  || Bonus4Type == 200
                  || Bonus4Type >= 212 && Bonus4Type <= 217
                  || Bonus4Type >= 232 && Bonus4Type <= 235
                  || Bonus4Type >= 247 && Bonus4Type <= 250
                  || Bonus4Type >= 253 && Bonus4Type <= 255)
                {
                    totalUti += Bonus4 * 5;
                }

            }

            if (Bonus5Type != 0 &&
                Bonus5 != 0)
            {
                if (Bonus5Type < 9 || Bonus5Type == 156)
                {
                    totalUti += Bonus5 * .6667;
                }
                else if (Bonus5Type == 9 || Bonus5Type == 148)
                {
                    totalUti += Bonus5;
                }
                else if (Bonus5Type == 10 || Bonus5Type == 187 || Bonus5Type == 210)
                {
                    totalUti += Bonus5 * .25;
                }
                else if (Bonus5Type < 20
                    || Bonus5Type == 59
                    || Bonus5Type == 71
                    || Bonus5Type == 116
                    || Bonus5Type == 117
                    || Bonus5Type == 119
                    || Bonus5Type >= 145 && Bonus5Type <= 149
                    || Bonus5Type >= 169 && Bonus5Type <= 172
                    || Bonus5Type >= 175 && Bonus5Type <= 186
                    || Bonus5Type == 189
                    || Bonus5Type == 190
                    || Bonus5Type >= 192 && Bonus5Type <= 196
                    || Bonus5Type == 199
                    || Bonus5Type >= 201 && Bonus5Type <= 209
                    || Bonus5Type == 211
                    || Bonus5Type >= 217 && Bonus5Type <= 220
                    || Bonus5Type == 230
                    || Bonus5Type == 231
                    || Bonus5Type == 251
                    || Bonus5Type == 252)
                {
                    totalUti += Bonus5 * 2;
                }
                else if (Bonus5Type >= 221 && Bonus5Type <= 229 || Bonus5Type >= 236 && Bonus5Type <= 245)
                {
                    totalUti += Bonus5 * 4;
                }
                else if (Bonus5Type >= 20 && Bonus5Type <= 58 || Bonus5Type >= 60 && Bonus5Type <= 70 || Bonus5Type >= 72 && Bonus5Type <= 115)
                {
                    totalUti += Bonus5 * 5;
                }
                else if (Bonus5Type == 118
                  || Bonus5Type >= 150 && Bonus5Type <= 155
                  || Bonus5Type >= 163 && Bonus5Type <= 168
                  || Bonus5Type == 173
                  || Bonus5Type == 174
                  || Bonus5Type == 188
                  || Bonus5Type == 191
                  || Bonus5Type == 197
                  || Bonus5Type == 198
                  || Bonus5Type == 200
                  || Bonus5Type >= 212 && Bonus5Type <= 217
                  || Bonus5Type >= 232 && Bonus5Type <= 235
                  || Bonus5Type >= 247 && Bonus5Type <= 250
                  || Bonus5Type >= 253 && Bonus5Type <= 255)
                {
                    totalUti += Bonus5 * 5;
                }

            }

            if (Bonus6Type != 0 &&
                Bonus6 != 0)
            {
                if (Bonus6Type < 9 || Bonus6Type == 156)
                {
                    totalUti += Bonus6 * .6667;
                }
                else if (Bonus6Type == 9 || Bonus6Type == 148)
                {
                    totalUti += Bonus6;
                }
                else if (Bonus6Type == 10 || Bonus6Type == 187 || Bonus6Type == 210)
                {
                    totalUti += Bonus6 * .25;
                }
                else if (Bonus6Type < 20
                    || Bonus6Type == 59
                    || Bonus6Type == 71
                    || Bonus6Type == 116
                    || Bonus6Type == 117
                    || Bonus6Type == 119
                    || Bonus6Type >= 145 && Bonus6Type <= 149
                    || Bonus6Type >= 169 && Bonus6Type <= 172
                    || Bonus6Type >= 175 && Bonus6Type <= 186
                    || Bonus6Type == 189
                    || Bonus6Type == 190
                    || Bonus6Type >= 192 && Bonus6Type <= 196
                    || Bonus6Type == 199
                    || Bonus6Type >= 201 && Bonus6Type <= 209
                    || Bonus6Type == 211
                    || Bonus6Type >= 217 && Bonus6Type <= 220
                    || Bonus6Type == 230
                    || Bonus6Type == 231
                    || Bonus6Type == 251
                    || Bonus6Type == 252)
                {
                    totalUti += Bonus6 * 2;
                }
                else if (Bonus6Type >= 221 && Bonus6Type <= 229 || Bonus6Type >= 236 && Bonus6Type <= 245)
                {
                    totalUti += Bonus6 * 4;
                }
                else if (Bonus6Type >= 20 && Bonus6Type <= 58 || Bonus6Type >= 60 && Bonus6Type <= 70 || Bonus6Type >= 72 && Bonus6Type <= 115)
                {
                    totalUti += Bonus6 * 5;
                }
                else if (Bonus6Type == 118
                  || Bonus6Type >= 150 && Bonus6Type <= 155
                  || Bonus6Type >= 163 && Bonus6Type <= 168
                  || Bonus6Type == 173
                  || Bonus6Type == 174
                  || Bonus6Type == 188
                  || Bonus6Type == 191
                  || Bonus6Type == 197
                  || Bonus6Type == 198
                  || Bonus6Type == 200
                  || Bonus6Type >= 212 && Bonus6Type <= 217
                  || Bonus6Type >= 232 && Bonus6Type <= 235
                  || Bonus6Type >= 247 && Bonus6Type <= 250
                  || Bonus6Type >= 253 && Bonus6Type <= 255)
                {
                    totalUti += Bonus6 * 5;
                }
            }

            if (Bonus7Type != 0 &&
                Bonus7 != 0)
            {
                if (Bonus7Type < 9 || Bonus7Type == 156)
                {
                    totalUti += Bonus7 * .6667;
                }
                else if (Bonus7Type == 9 || Bonus7Type == 148)
                {
                    totalUti += Bonus7;
                }
                else if (Bonus7Type == 10 || Bonus7Type == 187 || Bonus7Type == 210)
                {
                    totalUti += Bonus7 * .25;
                }
                else if (Bonus7Type < 20
                    || Bonus7Type == 59
                    || Bonus7Type == 71
                    || Bonus7Type == 116
                    || Bonus7Type == 117
                    || Bonus7Type == 119
                    || Bonus7Type >= 145 && Bonus7Type <= 149
                    || Bonus7Type >= 169 && Bonus7Type <= 172
                    || Bonus7Type >= 175 && Bonus7Type <= 186
                    || Bonus7Type == 189
                    || Bonus7Type == 190
                    || Bonus7Type >= 192 && Bonus7Type <= 196
                    || Bonus7Type == 199
                    || Bonus7Type >= 201 && Bonus7Type <= 209
                    || Bonus7Type == 211
                    || Bonus7Type >= 217 && Bonus7Type <= 220
                    || Bonus7Type == 230
                    || Bonus7Type == 231
                    || Bonus7Type == 251
                    || Bonus7Type == 252)
                {
                    totalUti += Bonus7 * 2;
                }
                else if (Bonus7Type >= 221 && Bonus7Type <= 229 || Bonus7Type >= 236 && Bonus7Type <= 245)
                {
                    totalUti += Bonus7 * 4;
                }
                else if (Bonus7Type >= 20 && Bonus7Type <= 58 || Bonus7Type >= 60 && Bonus7Type <= 70 || Bonus7Type >= 72 && Bonus7Type <= 115)
                {
                    totalUti += Bonus7 * 5;
                }
                else if (Bonus7Type == 118
                  || Bonus7Type >= 150 && Bonus7Type <= 155
                  || Bonus7Type >= 163 && Bonus7Type <= 168
                  || Bonus7Type == 173
                  || Bonus7Type == 174
                  || Bonus7Type == 188
                  || Bonus7Type == 191
                  || Bonus7Type == 197
                  || Bonus7Type == 198
                  || Bonus7Type == 200
                  || Bonus7Type >= 212 && Bonus7Type <= 217
                  || Bonus7Type >= 232 && Bonus7Type <= 235
                  || Bonus7Type >= 247 && Bonus7Type <= 250
                  || Bonus7Type >= 253 && Bonus7Type <= 255)
                {
                    totalUti += Bonus7 * 5;
                }
            }
            if (Bonus8Type != 0 &&
                Bonus8 != 0)
            {
                if (Bonus8Type < 9 || Bonus8Type == 156)
                {
                    totalUti += Bonus8 * .6667;
                }
                else if (Bonus8Type == 9 || Bonus8Type == 148)
                {
                    totalUti += Bonus8;
                }
                else if (Bonus8Type == 10 || Bonus8Type == 187 || Bonus8Type == 210)
                {
                    totalUti += Bonus8 * .25;
                }
                else if (Bonus8Type < 20
                    || Bonus8Type == 59
                    || Bonus8Type == 71
                    || Bonus8Type == 116
                    || Bonus8Type == 117
                    || Bonus8Type == 119
                    || Bonus8Type >= 145 && Bonus8Type <= 149
                    || Bonus8Type >= 169 && Bonus8Type <= 172
                    || Bonus8Type >= 175 && Bonus8Type <= 186
                    || Bonus8Type == 189
                    || Bonus8Type == 190
                    || Bonus8Type >= 192 && Bonus8Type <= 196
                    || Bonus8Type == 199
                    || Bonus8Type >= 201 && Bonus8Type <= 209
                    || Bonus8Type == 211
                    || Bonus8Type >= 217 && Bonus8Type <= 220
                    || Bonus8Type == 230
                    || Bonus8Type == 231
                    || Bonus8Type == 251
                    || Bonus8Type == 252)
                {
                    totalUti += Bonus8 * 2;
                }
                else if (Bonus8Type >= 221 && Bonus8Type <= 229 || Bonus8Type >= 236 && Bonus8Type <= 245)
                {
                    totalUti += Bonus8 * 4;
                }
                else if (Bonus8Type >= 20 && Bonus8Type <= 58 || Bonus8Type >= 60 && Bonus8Type <= 70 || Bonus8Type >= 72 && Bonus8Type <= 115)
                {
                    totalUti += Bonus8 * 5;
                }
                else if (Bonus8Type == 118
                  || Bonus8Type >= 150 && Bonus8Type <= 155
                  || Bonus8Type >= 163 && Bonus8Type <= 168
                  || Bonus8Type == 173
                  || Bonus8Type == 174
                  || Bonus8Type == 188
                  || Bonus8Type == 191
                  || Bonus8Type == 197
                  || Bonus8Type == 198
                  || Bonus8Type == 200
                  || Bonus8Type >= 212 && Bonus8Type <= 217
                  || Bonus8Type >= 232 && Bonus8Type <= 235
                  || Bonus8Type >= 247 && Bonus8Type <= 250
                  || Bonus8Type >= 253 && Bonus8Type <= 255)
                {
                    totalUti += Bonus8 * 5;
                }
            }
            if (Bonus9Type != 0 &&
                Bonus9 != 0)
            {
                if (Bonus9Type < 9 || Bonus9Type == 156)
                {
                    totalUti += Bonus9 * .6667;
                }
                else if (Bonus9Type == 9 || Bonus9Type == 148)
                {
                    totalUti += Bonus9;
                }
                else if (Bonus9Type == 10 || Bonus9Type == 187 || Bonus9Type == 210)
                {
                    totalUti += Bonus9 * .25;
                }
                else if (Bonus9Type < 20
                    || Bonus9Type == 59
                    || Bonus9Type == 71
                    || Bonus9Type == 116
                    || Bonus9Type == 117
                    || Bonus9Type == 119
                    || Bonus9Type >= 145 && Bonus9Type <= 149
                    || Bonus9Type >= 169 && Bonus9Type <= 172
                    || Bonus9Type >= 175 && Bonus9Type <= 186
                    || Bonus9Type == 189
                    || Bonus9Type == 190
                    || Bonus9Type >= 192 && Bonus9Type <= 196
                    || Bonus9Type == 199
                    || Bonus9Type >= 201 && Bonus9Type <= 209
                    || Bonus9Type == 211
                    || Bonus9Type >= 217 && Bonus9Type <= 220
                    || Bonus9Type == 230
                    || Bonus9Type == 231
                    || Bonus9Type == 251
                    || Bonus9Type == 252)
                {
                    totalUti += Bonus9 * 2;
                }
                else if (Bonus9Type >= 221 && Bonus9Type <= 229 || Bonus9Type >= 236 && Bonus9Type <= 245)
                {
                    totalUti += Bonus9 * 4;
                }
                else if (Bonus9Type >= 20 && Bonus9Type <= 58 || Bonus9Type >= 60 && Bonus9Type <= 70 || Bonus9Type >= 72 && Bonus9Type <= 115)
                {
                    totalUti += Bonus9 * 5;
                }
                else if (Bonus9Type == 118
                  || Bonus9Type >= 150 && Bonus9Type <= 155
                  || Bonus9Type >= 163 && Bonus9Type <= 168
                  || Bonus9Type == 173
                  || Bonus9Type == 174
                  || Bonus9Type == 188
                  || Bonus9Type == 191
                  || Bonus9Type == 197
                  || Bonus9Type == 198
                  || Bonus9Type == 200
                  || Bonus9Type >= 212 && Bonus9Type <= 217
                  || Bonus9Type >= 232 && Bonus9Type <= 235
                  || Bonus9Type >= 247 && Bonus9Type <= 250
                  || Bonus9Type >= 253 && Bonus9Type <= 255)
                {
                    totalUti += Bonus9 * 5;
                }
            }
            if (Bonus10Type != 0 &&
                Bonus10 != 0)
            {
                if (Bonus10Type < 9 || Bonus10Type == 156)
                {
                    totalUti += Bonus10 * .6667;
                }
                else if (Bonus10Type == 9 || Bonus10Type == 148)
                {
                    totalUti += Bonus10;
                }
                else if (Bonus10Type == 10 || Bonus10Type == 187 || Bonus10Type == 210)
                {
                    totalUti += Bonus10 * .25;
                }
                else if (Bonus10Type < 20
                    || Bonus10Type == 59
                    || Bonus10Type == 71
                    || Bonus10Type == 116
                    || Bonus10Type == 117
                    || Bonus10Type == 119
                    || Bonus10Type >= 145 && Bonus10Type <= 149
                    || Bonus10Type >= 169 && Bonus10Type <= 172
                    || Bonus10Type >= 175 && Bonus10Type <= 186
                    || Bonus10Type == 189
                    || Bonus10Type == 190
                    || Bonus10Type >= 192 && Bonus10Type <= 196
                    || Bonus10Type == 199
                    || Bonus10Type >= 201 && Bonus10Type <= 209
                    || Bonus10Type == 211
                    || Bonus10Type >= 217 && Bonus10Type <= 220
                    || Bonus10Type == 230
                    || Bonus10Type == 231
                    || Bonus10Type == 251
                    || Bonus10Type == 252)
                {
                    totalUti += Bonus10 * 2;
                }
                else if (Bonus10Type >= 221 && Bonus10Type <= 229 || Bonus10Type >= 236 && Bonus10Type <= 245)
                {
                    totalUti += Bonus10 * 4;
                }
                else if (Bonus10Type >= 20 && Bonus10Type <= 58 || Bonus10Type >= 60 && Bonus10Type <= 70 || Bonus10Type >= 72 && Bonus10Type <= 115)
                {
                    totalUti += Bonus10 * 5;
                }
                else if (Bonus10Type == 118
                  || Bonus10Type >= 150 && Bonus10Type <= 155
                  || Bonus10Type >= 163 && Bonus10Type <= 168
                  || Bonus10Type == 173
                  || Bonus10Type == 174
                  || Bonus10Type == 188
                  || Bonus10Type == 191
                  || Bonus10Type == 197
                  || Bonus10Type == 198
                  || Bonus10Type == 200
                  || Bonus10Type >= 212 && Bonus10Type <= 217
                  || Bonus10Type >= 232 && Bonus10Type <= 235
                  || Bonus10Type >= 247 && Bonus10Type <= 250
                  || Bonus10Type >= 253 && Bonus10Type <= 255)
                {
                    totalUti += Bonus10 * 5;
                }
            }
            if (ExtraBonusType != 0 &&
                ExtraBonus != 0)
            {
                if (ExtraBonusType < 9 || Bonus1Type == 156)
                {
                    totalUti += ExtraBonus * .6667;
                }
                else if (ExtraBonusType == 9 || ExtraBonusType == 148)
                {
                    totalUti += ExtraBonus;
                }
                else if (ExtraBonusType == 10 || ExtraBonusType == 187 || ExtraBonusType == 210)
                {
                    totalUti += ExtraBonus * .25;
                }
                else if (ExtraBonusType < 20
                    || ExtraBonusType == 59
                    || ExtraBonusType == 71
                    || ExtraBonusType == 116
                    || ExtraBonusType == 117
                    || ExtraBonusType == 119
                    || ExtraBonusType >= 145 && ExtraBonusType <= 149
                    || ExtraBonusType >= 169 && ExtraBonusType <= 172
                    || ExtraBonusType >= 175 && ExtraBonusType <= 186
                    || ExtraBonusType == 189
                    || ExtraBonusType == 190
                    || ExtraBonusType >= 192 && ExtraBonusType <= 196
                    || ExtraBonusType == 199
                    || ExtraBonusType >= 201 && ExtraBonusType <= 209
                    || ExtraBonusType == 211
                    || ExtraBonusType >= 217 && ExtraBonusType <= 220
                    || ExtraBonusType == 230
                    || ExtraBonusType == 231
                    || ExtraBonusType == 251
                    || ExtraBonusType == 252)
                {
                    totalUti += ExtraBonus * 2;
                }
                else if (ExtraBonusType >= 221 && ExtraBonusType <= 229 || ExtraBonusType >= 236 && ExtraBonusType <= 245)
                {
                    totalUti += ExtraBonus * 4;
                }
                else if (ExtraBonusType >= 20 && ExtraBonusType <= 58 || ExtraBonusType >= 60 && ExtraBonusType <= 70 || ExtraBonusType >= 72 && ExtraBonusType <= 115)
                {
                    totalUti += ExtraBonus * 5;
                }
                else if (ExtraBonusType == 118
                  || ExtraBonusType >= 150 && ExtraBonusType <= 155
                  || ExtraBonusType >= 163 && ExtraBonusType <= 168
                  || ExtraBonusType == 173
                  || ExtraBonusType == 174
                  || ExtraBonusType == 188
                  || ExtraBonusType == 191
                  || ExtraBonusType == 197
                  || ExtraBonusType == 198
                  || ExtraBonusType == 200
                  || ExtraBonusType >= 212 && ExtraBonusType <= 217
                  || ExtraBonusType >= 232 && ExtraBonusType <= 235
                  || ExtraBonusType >= 247 && ExtraBonusType <= 250
                  || ExtraBonusType >= 253 && ExtraBonusType <= 255)
                {
                    totalUti += ExtraBonus * 10;
                }
            }

            return totalUti;
        }

        private double GetSingleUtility(int BonusType, int Bonus)
        {
            double totalUti = 0;

            if (BonusType != 0 &&
                Bonus != 0)
            {
                if (BonusType < 9 || BonusType == 156)
                {
                    totalUti += Bonus * .6667;
                }
                else if (BonusType == 9 || BonusType == 148)
                {
                    totalUti += Bonus;
                }
                else if (BonusType == 10 || BonusType == 187 || BonusType == 210)
                {
                    totalUti += Bonus * .25;
                }
                else if (BonusType < 20
                    || BonusType == 59
                    || BonusType == 71
                    || BonusType == 116
                    || BonusType == 117
                    || BonusType == 119
                    || BonusType >= 145 && BonusType <= 149
                    || BonusType >= 169 && BonusType <= 172
                    || BonusType >= 175 && BonusType <= 186
                    || BonusType == 189
                    || BonusType == 190
                    || BonusType >= 192 && BonusType <= 196
                    || BonusType == 199
                    || BonusType >= 201 && BonusType <= 209
                    || BonusType == 211
                    || BonusType >= 217 && BonusType <= 220
                    || BonusType == 230
                    || BonusType == 231
                    || BonusType == 251
                    || BonusType == 252)
                {
                    totalUti += Bonus * 2;
                }
                else if (BonusType >= 221 && BonusType <= 229 || BonusType >= 236 && BonusType <= 245)
                {
                    totalUti += Bonus * 4;
                }
                else if (BonusType >= 20 && BonusType <= 58 || BonusType >= 60 && BonusType <= 70 || BonusType >= 72 && BonusType <= 115)
                {
                    totalUti += Bonus * 5;
                }
                else if (BonusType == 118
                  || BonusType >= 150 && BonusType <= 155
                  || BonusType >= 163 && BonusType <= 168
                  || BonusType == 173
                  || BonusType == 174
                  || BonusType == 188
                  || BonusType == 191
                  || BonusType == 197
                  || BonusType == 198
                  || BonusType == 200
                  || BonusType >= 212 && BonusType <= 217
                  || BonusType >= 232 && BonusType <= 235
                  || BonusType >= 247 && BonusType <= 250
                  || BonusType >= 253 && BonusType <= 255)
                {
                    totalUti += Bonus * 5;
                }
            }


            return totalUti;
        }

        private string GetBonusTypeFromBonusName(GameClient client, string bonusName)
        {
            switch (bonusName)
            {
                case nameof(Bonus1):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus1Type);

                case nameof(Bonus2):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus2Type);

                case nameof(Bonus3):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus3Type);

                case nameof(Bonus4):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus4Type);

                case nameof(Bonus5):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus5Type);

                case nameof(Bonus6):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus6Type);

                case nameof(Bonus7):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus7Type);

                case nameof(Bonus8):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus8Type);

                case nameof(Bonus9):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus9Type);

                case nameof(Bonus10):
                    return SkillBase.GetPropertyName(client, (eProperty)Bonus10Type);

                case nameof(ProcSpellID):
                    return SkillBase.GetPropertyName(client, (eProperty)ProcSpellID);

                case nameof(ProcSpellID1):
                    return SkillBase.GetPropertyName(client, (eProperty)ProcSpellID1);

                default:
                    return string.Empty;
            }
        }

        protected virtual void WriteBonusLine(IList<string> list, GameClient client, int bonusCat, int bonusValue)
        {
            if (bonusCat != 0 && bonusValue != 0 && !SkillBase.CheckPropertyType((eProperty)bonusCat, ePropertyType.Focus))
            {
                string singleUti = String.Format("{0:0.00}", GetSingleUtility(bonusCat, bonusValue));
                //- Axe: 5 pts
                //- Strength: 15 pts
                //- Constitution: 15 pts
                //- Hits: 40 pts
                //- Fatigue: 8 pts
                //- Heat: 7%
                //Bonus to casting speed: 2%
                //Bonus to armor factor (AF): 18
                //Power: 6 % of power pool.
                string bonusValueStr = bonusValue.ToString("0 ;-0;0 ");
                string formattedLine = $"{singleUti} | {SkillBase.GetPropertyName(client, (eProperty)bonusCat)}: {bonusValueStr}";

                if (bonusCat == (int)eProperty.PowerPool
                    || (bonusCat >= (int)eProperty.Resist_First && bonusCat <= (int)eProperty.Resist_Last)
                    || (bonusCat >= (int)eProperty.ResCapBonus_First && bonusCat <= (int)eProperty.ResCapBonus_Last)
                    || bonusCat == (int)eProperty.Conversion
                    || bonusCat == (int)eProperty.ExtraHP
                    || bonusCat == (int)eProperty.RealmPoints
                    || bonusCat == (int)eProperty.StyleAbsorb
                    || bonusCat == (int)eProperty.ArcaneSyphon
                    || bonusCat == (int)eProperty.BountyPoints
                    || bonusCat == (int)eProperty.XpPoints
                    || bonusCat == (int)eProperty.CraftingSkillGain
                    || bonusCat == (int)eProperty.RobberyResist
                    || (bonusCat >= 116 && bonusCat <= 119)
                    || (bonusCat >= 145 && bonusCat <= 147)
                    || (bonusCat >= 149 && bonusCat <= 155)
                    || (bonusCat >= 169 && bonusCat <= 186)
                    || (bonusCat >= 188 && bonusCat <= 200)
                    || (bonusCat >= 231 && bonusCat <= 235)
                    || (bonusCat >= 247 && bonusCat <= 254))
                {
                    formattedLine += "%";
                }
                else
                {
                    formattedLine += LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteBonusLine.Points");
                }
                list.Add(formattedLine);
            }
        }

        protected virtual void WriteFocusLine(GameClient client, IList<string> list, int focusCat, int focusLevel)
        {
            if (SkillBase.CheckPropertyType((eProperty)focusCat, ePropertyType.Focus))
            {
                //- Body Magic: 4 lvls
                list.Add(string.Format("- {0}: {1} lvls", SkillBase.GetPropertyName(client, (eProperty)focusCat), focusLevel));
            }
        }


        protected virtual bool IsPvEBonus(eProperty property)
        {
            switch (property)
            {
                case eProperty.DefensiveBonus:
                case eProperty.BladeturnReinforcement:
                case eProperty.NegativeReduction:
                case eProperty.PieceAblative:
                case eProperty.ReactionaryStyleDamage:
                case eProperty.SpellPowerCost:
                case eProperty.StyleCostReduction:
                case eProperty.ToHitBonus:
                    return true;

                default:
                    return false;
            }
        }


        protected virtual void WritePoisonInfo(IList<string> list, GameClient client)
        {
            if (PoisonSpellID != 0)
            {
                SpellLine poisonLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mundane_Poisons);
                if (poisonLine != null)
                {
                    List<Spell> spells = SkillBase.GetSpellList(poisonLine.KeyName);

                    foreach (Spell spl in spells)
                    {
                        if (spl.ID == PoisonSpellID)
                        {
                            list.Add(" ");
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePoisonInfo.LevelRequired"));
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePoisonInfo.Level", spl.Level));
                            list.Add(" ");
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePoisonInfo.ProcAbility"));
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePoisonInfo.Charges", PoisonCharges));
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePoisonInfo.MaxCharges", PoisonMaxCharges));
                            list.Add(" ");

                            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(client.Player, spl, poisonLine);
                            if (spellHandler != null)
                            {
                                Util.AddRange(list, spellHandler.DelveInfo);
                            }
                            else
                            {
                                list.Add("-" + spl.Name + " (Not implemented yet)");
                            }
                            break;
                        }
                    }
                }
            }
        }


        protected virtual void WritePotionInfo(IList<string> list, GameClient client)
        {
            if (SpellID != 0)
            {
                SpellLine potionLine = SkillBase.GetSpellLine(GlobalSpellsLines.Potions_Effects);
                if (potionLine != null)
                {
                    List<Spell> spells = SkillBase.GetSpellList(potionLine.KeyName);

                    foreach (Spell spl in spells)
                    {
                        if (spl.ID == SpellID)
                        {
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.ChargedMagic"));
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.Charges", Charges));
                            list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.MaxCharges", MaxCharges));
                            list.Add(" ");
                            WritePotionSpellsInfos(list, client, spl, potionLine);
                            list.Add(" ");
                            long nextPotionAvailTime = client.Player.TempProperties.getProperty<long>("LastPotionItemUsedTick_Type" + spl.SharedTimerGroup);
                            // Satyr Update: Individual Reuse-Timers for Pots need a Time looking forward
                            // into Future, set with value of "itemtemplate.CanUseEvery" and no longer back into past
                            if (nextPotionAvailTime > client.Player.CurrentRegion.Time)
                            {
                                list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.UseItem3", Util.FormatTime((nextPotionAvailTime - client.Player.CurrentRegion.Time) / 1000)));
                            }
                            else
                            {
                                int minutes = CanUseEvery / 60;
                                int seconds = CanUseEvery % 60;

                                if (minutes == 0)
                                {
                                    list.Add(String.Format("Can use item every: {0} sec", seconds));
                                }
                                else
                                {
                                    list.Add(String.Format("Can use item every: {0}:{1:00} min", minutes, seconds));
                                }
                            }

                            if (spl.CastTime > 0)
                            {
                                list.Add(" ");
                                list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.NoUseInCombat"));
                            }
                            break;
                        }
                    }
                }
            }
        }


        protected static void WritePotionSpellsInfos(IList<string> list, GameClient client, Spell spl, NamedSkill line)
        {
            if (spl != null)
            {
                list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteMagicalBonuses.MagicAbility"));
                list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.Type", spl.SpellType));
                list.Add(" ");
                list.Add(spl.Description);
                list.Add(" ");
                if (spl.Value != 0)
                {
                    list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.Value", spl.Value));
                }
                list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.Target", spl.Target));
                if (spl.Range > 0)
                {
                    list.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WritePotionInfo.Range", spl.Range));
                }
                list.Add(" ");
                list.Add(" ");
                if (spl.SubSpellID > 0)
                {
                    List<Spell> spells = SkillBase.GetSpellList(line.KeyName);
                    foreach (Spell subSpell in spells)
                    {
                        if (subSpell.ID == spl.SubSpellID)
                        {
                            WritePotionSpellsInfos(list, client, subSpell, line);
                            break;
                        }
                    }
                }
            }
        }


        protected virtual void DelveShieldStats(IList<string> output, GameClient client)
        {
            double itemDPS = DPS_AF / 10.0;
            double clampedDPS = Math.Min(itemDPS, 1.2 + 0.3 * client.Player.Level);
            double itemSPD = SPD_ABS / 10.0;

            output.Add(" ");
            output.Add(" ");
            output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.DamageMod"));
            if (itemDPS != 0)
            {
                output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.BaseDPS", itemDPS.ToString("0.0")));
                output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.ClampDPS", clampedDPS.ToString("0.0")));
            }
            if (SPD_ABS >= 0)
            {
                output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.SPD", itemSPD.ToString("0.0")));
            }

            output.Add(" ");

            switch (Type_Damage)
            {
                case 1: output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.Small")); break;
                case 2: output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.Medium")); break;
                case 3: output.Add(LanguageMgr.GetTranslation(client.Account.Language, "DetailDisplayHandler.WriteClassicShieldInfos.Large")); break;
            }
        }


        protected virtual void DelveWeaponStats(List<String> delve, GamePlayer player)
        {
            double itemDPS = DPS_AF / 10.0;
            double clampedDPS = Math.Min(itemDPS, 1.2 + 0.3 * player.Level);
            double itemSPD = SPD_ABS / 10.0;
            double effectiveDPS = clampedDPS * Quality / 100.0 * Condition / MaxCondition;

            delve.Add(" ");
            delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.DamageMod"));

            if (itemDPS != 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.BaseDPS", itemDPS.ToString("0.0")));
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.ClampDPS", clampedDPS.ToString("0.0")));
            }

            if (SPD_ABS >= 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.SPD", itemSPD.ToString("0.0")));
            }

            if (Quality != 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.Quality", Quality));
            }

            if (Condition != 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.Condition", ConditionPercent));
            }

            delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language,
                                                 "DetailDisplayHandler.WriteClassicWeaponInfos.DamageType",
                                                 (Type_Damage == 0 ? "None" : GlobalConstants.WeaponDamageTypeToName(Type_Damage))));

            delve.Add(" ");

            delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicWeaponInfos.EffDamage"));

            if (itemDPS != 0)
            {
                delve.Add("- " + effectiveDPS.ToString("0.0") + " DPS");
            }
        }

        protected virtual void DelveArmorStats(List<String> delve, GamePlayer player)
        {
            delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.ArmorMod"));

            double af = 0;
            int afCap = player.Level + (player.RealmLevel > 39 ? 1 : 0);
            double effectiveAF = 0;

            if (DPS_AF != 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.BaseFactor", DPS_AF));

                if (Object_Type != (int)eObjectType.Cloth)
                {
                    afCap *= 2;
                }

                af = Math.Min(afCap, DPS_AF);

                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.ClampFact", (int)af));
            }

            if (SPD_ABS >= 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.Absorption", SPD_ABS));
            }

            if (Quality != 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.Quality", Quality));
            }

            if (Condition != 0)
            {
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.Condition", ConditionPercent));
            }

            delve.Add(" ");
            delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.EffArmor"));

            if (DPS_AF != 0)
            {
                effectiveAF = af * Quality / 100.0 * Condition / MaxCondition * (1 + SPD_ABS / 100.0);
                delve.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.WriteClassicArmorInfos.Factor", (int)effectiveAF));
            }
        }

        /// <summary>
        /// Write item technical info
        /// </summary>
        /// <param name="output"></param>
        /// <param name="item"></param>
        public virtual void WriteTechnicalInfo(List<String> delve, GameClient client)
        {
            delve.Add("");
            delve.Add("--- Technical Information ---");
            delve.Add("");

            if (Template is ItemUnique)
            {
                delve.Add("  Item Unique: " + Id_nb);
            }
            else
            {
                delve.Add("Item Template: " + Id_nb);
                delve.Add("Allow Updates: " + (Template as ItemTemplate).AllowUpdate);
            }

            delve.Add("");

            delve.Add("         Name: " + Name);
            delve.Add("    ClassType: " + this.GetType().FullName);
            delve.Add("");
            delve.Add(" SlotPosition: " + SlotPosition);
            if (OwnerLot != 0 || SellPrice != 0)
            {
                delve.Add("    Owner Lot: " + OwnerLot);
                delve.Add("   Sell Price: " + SellPrice);
            }
            delve.Add("");
            delve.Add("        Level: " + Level);
            delve.Add("       Object: " + GlobalConstants.ObjectTypeToName(client, Object_Type) + " (" + Object_Type + ")");
            delve.Add("         Type: " + GlobalConstants.SlotToName(client, Item_Type) + " (" + Item_Type + ")");
            delve.Add("");
            delve.Add("        Model: " + Model);
            delve.Add("    Extension: " + Extension);
            delve.Add("        Color: " + Color);
            delve.Add("       Emblem: " + Emblem);
            delve.Add("       Effect: " + Effect);
            delve.Add("");
            delve.Add("       DPS_AF: " + DPS_AF);
            delve.Add("      SPD_ABS: " + SPD_ABS);
            delve.Add("         Hand: " + Hand);
            delve.Add("  Type_Damage: " + Type_Damage);
            delve.Add("        Bonus: " + Bonus);

            if (GlobalConstants.IsWeapon(Object_Type))
            {
                delve.Add("");
                delve.Add("         Hand: " + GlobalConstants.ItemHandToName(client, Hand) + " (" + Hand + ")");
                delve.Add("Damage/Second: " + (DPS_AF / 10.0f));
                delve.Add("        Speed: " + (SPD_ABS / 10.0f));
                delve.Add("  Damage type: " + GlobalConstants.WeaponDamageTypeToName(Type_Damage) + " (" + Type_Damage + ")");
                delve.Add("        Bonus: " + Bonus);
            }
            else if (GlobalConstants.IsArmor(Object_Type))
            {
                delve.Add("");
                delve.Add("  Armorfactor: " + DPS_AF);
                delve.Add("   Absorption: " + SPD_ABS);
                delve.Add("        Bonus: " + Bonus);
            }
            else if (Object_Type == (int)eObjectType.Shield)
            {
                delve.Add("");
                delve.Add("Damage/Second: " + (DPS_AF / 10.0f));
                delve.Add("        Speed: " + (SPD_ABS / 10.0f));
                delve.Add("  Shield type: " + GlobalConstants.ShieldTypeToName(Type_Damage) + " (" + Type_Damage + ")");
                delve.Add("        Bonus: " + Bonus);
            }
            else if (Object_Type == (int)eObjectType.Arrow || Object_Type == (int)eObjectType.Bolt)
            {
                delve.Add("");
                delve.Add(" Ammunition #: " + DPS_AF);
                delve.Add("       Damage: " + GlobalConstants.AmmunitionTypeToDamageName(client, SPD_ABS));
                delve.Add("        Range: " + GlobalConstants.AmmunitionTypeToRangeName(client, SPD_ABS));
                delve.Add("     Accuracy: " + GlobalConstants.AmmunitionTypeToAccuracyName(client, SPD_ABS));
                delve.Add("        Bonus: " + Bonus);
            }
            else if (Object_Type == (int)eObjectType.Instrument)
            {
                delve.Add("");
                delve.Add("   Instrument: " + GlobalConstants.InstrumentTypeToName(DPS_AF));
            }

            if (OwnerLot != 0)
            {
                delve.Add("");
                delve.Add("   Owner Lot#: " + OwnerLot);
                delve.Add("   Sell Price: " + SellPrice);
            }

            delve.Add("");
            delve.Add("   Value/Price: " + Money.GetShortString(Price) + " / " + Money.GetShortString((long)(Price * (long)ServerProperties.Properties.ITEM_SELL_RATIO * .01)));
            delve.Add("Count/MaxCount: " + Count + " / " + MaxCount);
            delve.Add("        Weight: " + (Weight / 10.0f) + "lbs");
            delve.Add("       Quality: " + Quality + "%");
            delve.Add("    Durability: " + Durability + "/" + MaxDurability);
            delve.Add("     Condition: " + Condition + "/" + MaxCondition);
            delve.Add("         Realm: " + Realm);
            delve.Add("");
            delve.Add("   Is dropable: " + (IsDropable ? "yes" : "no"));
            delve.Add("   Is pickable: " + (IsPickable ? "yes" : "no"));
            delve.Add("	  CanUseInRvR: " + (CanUseInRvR ? "yes" : "no"));
            delve.Add("   Is tradable: " + (IsTradable ? "yes" : "no"));
            delve.Add("  Is alwaysDUR: " + (IsNotLosingDur ? "yes" : "no"));
            delve.Add(" Is Indestruct: " + (IsIndestructible ? "yes" : "no"));
            delve.Add("  Is stackable: " + (IsStackable ? "yes (" + MaxCount + ")" : "no"));
            delve.Add("");
            delve.Add("   ProcSpellID: " + ProcSpellID);
            delve.Add("  ProcSpellID1: " + ProcSpellID1);
            delve.Add("    ProcChance: " + ProcChance);
            delve.Add("       SpellID: " + SpellID + " (" + Charges + "/" + MaxCharges + ")");
            delve.Add("      SpellID1: " + SpellID1 + " (" + Charges1 + "/" + MaxCharges1 + ")");
            delve.Add(" PoisonSpellID: " + PoisonSpellID + " (" + PoisonCharges + "/" + PoisonMaxCharges + ") ");
            delve.Add("");
            delve.Add("AllowedClasses: " + AllowedClasses);
            delve.Add(" LevelRequired: " + LevelRequirement);
            delve.Add("    BonusLevel: " + BonusLevel);
            delve.Add(" ");
            delve.Add("              Flags: " + Flags);
            delve.Add("     SalvageYieldID: " + SalvageYieldID);
            delve.Add("          PackageID: " + PackageID);
            delve.Add("Requested ClassType: " + ClassType);
        }
    }
}
