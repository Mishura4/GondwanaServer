using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
{
    [SpellHandler("CombineItem")]
    public class CombineItemSpellHandler
        : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly int[] modelForge = { 478, 1495 };
        private static readonly int[] modelTannery = { 479 };
        private static readonly int[] modelWeaver = { 480 };
        private static readonly int[] modelcarpentryWorkshop = { 481 };
        private static readonly int[] modelCauldron = { 632, 2607, 3475, 4203 };
        private static readonly int[] modelAlchemyTable = { 820, 1494 };
        private static readonly int[] modelFire = { 2656, 3460, 3470, 3549 };
        private static readonly int[] modelChest = { 1596, 4182, 4183, 4184 };

        private Combinable match;
        private WorldInventoryItem combined;
        // Transfor the list to a dictionary to count the number of items to remove
        private Dictionary<InventoryItem, int> removeItems;
        InventoryItem useItem;


        public CombineItemSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
        }

        /// <summary>
        /// Check whether it's actually possible to do the combine.
        /// </summary>
        /// <param name="selectedTarget"></param>
        /// <returns></returns>
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!base.CheckBeginCast(selectedTarget))
            {
                return false;
            }

            if (!(Caster is GamePlayer player))
            {
                return false;
            }

            useItem = player.UseItem;
            if (useItem == null)
            {
                return false;
            }

            var neededItems = this.GetCombinableItems(useItem.Id_nb);

            if (neededItems == null || !neededItems.Any())
            {
                return false;
            }

            removeItems = new Dictionary<InventoryItem, int>();

            var backpack = player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            match = null;

            foreach (var combinable in neededItems)
            {
                List<string> ids = new List<string>();
                Dictionary<string, int> countIterator = new Dictionary<string, int>();
                foreach (InventoryItem item in backpack)
                {
                    // check if the number of item is greater or equal than needed
                    if (item != null && combinable.Items.ContainsKey(item.Id_nb) && (item.Count >= combinable.Items[item.Id_nb] || (countIterator.ContainsKey(item.Id_nb) && (countIterator[item.Id_nb] + item.Count) >= combinable.Items[item.Id_nb])))
                    {
                        if (!ids.Contains(item.Id_nb))
                        {
                            ids.Add(item.Id_nb);
                            // If items have already added in the remove list, substract the total of removed items, else add the number of items to delete
                            if (countIterator.ContainsKey(item.Id_nb))
                                removeItems.Add(item, combinable.Items[item.Id_nb] - countIterator[item.Id_nb]);
                            else
                                removeItems.Add(item, combinable.Items[item.Id_nb]);
                        }
                    }
                    else if (item != null && combinable.Items.ContainsKey(item.Id_nb))
                    {
                        if (countIterator.ContainsKey(item.Id_nb))
                            countIterator[item.Id_nb] += item.Count;
                        else
                            countIterator.Add(item.Id_nb, item.Count);
                        // fix the issue when items take several slots
                        removeItems.Add(item, item.Count);
                    }
                }

                if (ids.Count == combinable.Items.Count())
                {
                    match = combinable;
                    break;
                }

                removeItems.Clear();
            }

            if (match == null)
            {
                // It missed a message to inform the player
                player.Out.SendMessage("Aucune combinaison possible n'a été trouvée", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (match.CraftingSkill != eCraftingSkill.NoCrafting && match.CraftValue > 0)
            {
                if (!player.CraftingSkills.ContainsKey(match.CraftingSkill))
                {
                    log.Warn($"Combine Item: Crafting skill {match.CraftingSkill.ToString()}  not found in Player {player.InternalID}  Crafting Skill");
                    return false;
                }
                else
                {
                    if (player.CraftingSkills[match.CraftingSkill] < match.CraftValue)
                    {
                        player.Out.SendMessage($"Votre niveau en {match.CraftingSkill.ToString()} doit etre au moins de {match.CraftValue} ", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                        return false;
                    }
                }
            }

            // should not pass here but keep it in case where I'm wrong
            if (!removeItems.ContainsKey(useItem))
                removeItems.Add(useItem, match.Items[useItem.Id_nb]);

            combined = WorldInventoryItem.CreateFromTemplate(match.TemplateId);

            if (combined == null)
            {
                log.Warn($"Missing item in ItemTemplate table '{match.TemplateId}' for CombineItem spell");
                return false;
            }
            combined.Item.Count = match.ItemsCount;

            // check if player is in the area if it needed
            if (match.AreaId != null)
            {
                bool checkAreaId = false;
                foreach (IArea iarea in player.CurrentAreas)
                    if (iarea is AbstractArea area && area.DbArea != null && area.DbArea.ObjectId == match.AreaId)
                        checkAreaId = true;
                if (!checkAreaId)
                {
                    player.Out.SendMessage("Vous ne pouvez pas combiner ces ingrédients à cet endroit", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (match.CombinexObjectModel != null && !CheckForTools(player, match.CombinexObjectModel.Split(new char[] { '|' }))
                && (string.IsNullOrEmpty(match.ToolKit) || !CheckToolKit(player, match.ToolKit)))
                return false;
            if (!CheckToolKit(player, match.ToolKit))
                return false;

            if (match.Duration > 0)
            {
                player.Out.SendTimerWindow("Combinaison en cours: " + combined.Item.Name, match.Duration);
                GameEventMgr.RemoveHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
                GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
                GameEventMgr.AddHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
                GameEventMgr.AddHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
            }

            return true;
        }

        private bool CheckToolKit(GamePlayer player, string toolKit)
        {
            if (string.IsNullOrEmpty(toolKit))
                return true;
            List<InventoryItem> items = player.Inventory.GetItemRange(eInventorySlot.MinEquipable, eInventorySlot.LastBackpack).ToList();
            foreach (InventoryItem item in items)
            {
                if (item.Id_nb == toolKit)
                    return true;
            }
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Spell.CombineItemSpellHandler.Toolkit", toolKit), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }

        public override bool CastSpell(GameLiving targetObject)
        {
            if (match.Duration > 0)
            {
                StartCastTimer(m_spellTarget);

                if ((Caster is GamePlayer && (Caster as GamePlayer).IsStrafing) || Caster.IsMoving)
                    CasterMoves();
            }
            else
            {
                SendCastAnimation(0);

                FinishSpellCast(m_spellTarget);
            }
            if (!IsCasting)
                OnAfterSpellCastSequence();
            return true;
        }

        public override int CalculateCastingTime()
        {
            return match.Duration * 1000;
        }

        private void EventManager(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
            if (e == GameLivingEvent.Moving)
                MessageToCaster(LanguageMgr.GetTranslation(((GamePlayer)Caster).Client.Account.Language, "AssassinateAbility.Moving"), eChatType.CT_Important);
            if (e == GameLivingEvent.AttackedByEnemy)
                MessageToCaster(LanguageMgr.GetTranslation(((GamePlayer)Caster).Client.Account.Language, "AssassinateAbility.Attacked"), eChatType.CT_Important);
            InterruptCasting();
        }

        public override void InterruptCasting()
        {
            base.InterruptCasting();
            if (match.Duration > 0)
                ((GamePlayer)Caster).Out.SendCloseTimerWindow();
        }

        /// <summary>
        /// Check if the player is near the needed tools (forge, lathe, etc)
        /// </summary>
        /// <param name="player">the crafting player</param>
        /// <param name="models">model for tools needed</param>
        /// <returns>true if required tools are found</returns>
        private bool CheckForTools(GamePlayer player, IList<string> models)
        {
            int modelCase = 0;
            bool differentModels = false;

            string toolName;
            string endString = "combiner cet objet";

            List<int> mergeModelList = new List<int>(models.Select(model => int.Parse(model)));

            if (mergeModelList.Intersect(modelForge).Count() > 0)
            {
                modelCase = 1;
                mergeModelList = new List<int>(mergeModelList.Union(modelForge));
            }
            if (mergeModelList.Intersect(modelTannery).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 2;
                mergeModelList = new List<int>(mergeModelList.Union(modelTannery));
            }
            if (mergeModelList.Intersect(modelWeaver).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 3;
                mergeModelList = new List<int>(mergeModelList.Union(modelWeaver));
            }
            if (mergeModelList.Intersect(modelcarpentryWorkshop).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 4;
                mergeModelList = new List<int>(mergeModelList.Union(modelcarpentryWorkshop));
            }
            if (mergeModelList.Intersect(modelCauldron).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 5;
                mergeModelList = new List<int>(mergeModelList.Union(modelCauldron));
            }
            if (mergeModelList.Intersect(modelAlchemyTable).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 6;
                mergeModelList = new List<int>(mergeModelList.Union(modelAlchemyTable));
            }
            if (mergeModelList.Intersect(modelFire).Count() > 0)
            {
                if (modelCase > 0)
                    differentModels = true;
                modelCase = 7;
                mergeModelList = new List<int>(mergeModelList.Union(modelFire));
            }
            if (mergeModelList.Intersect(modelChest).Count() > 0)
            {
                if (modelCase > 0)
                    modelCase = 0;
                else
                    modelCase = 8;
                mergeModelList = new List<int>(mergeModelList.Union(modelChest));
            }

            IEnumerable<GameObject> itemsInRadius = player.GetItemsInRadius(350).Cast<GameObject>();
            IEnumerable<GameObject> ncpInRadius = player.GetNPCsInRadius(350).Cast<GameObject>();
            foreach (GameObject item in new List<GameObject>(itemsInRadius.Concat(ncpInRadius)))
            {
                if (mergeModelList.Contains(item.Model))
                {
                    return true;
                }
            }
            if (differentModels)
            {
                modelCase = 0;
            }
            switch (modelCase)
            {
                case 1:
                    toolName = "une forge";
                    break;
                case 2:
                    toolName = "une tannerie";
                    break;
                case 3:
                    toolName = "une fileuse";
                    break;
                case 4:
                    toolName = "un atelier de menuiserie";
                    break;
                case 5:
                    toolName = "un chaudron";
                    break;
                case 6:
                    toolName = "une table d'alchimie";
                    break;
                case 7:
                    toolName = "un feu de camp";
                    break;
                case 8:
                    toolName = "un coffre";
                    break;
                default:
                    toolName = "un objet";
                    endString = "effectuer la combinaison";
                    break;
            }

            player.Out.SendMessage("Il faut vous rapprocher d'" + toolName + " pour pouvoir " + endString, eChatType.CT_System, eChatLoc.CL_SystemWindow);

#if (RELEASE)
            if (player.Client.Account.PrivLevel > 1)
            {
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Do the combine.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GamePlayer player = Caster as GamePlayer;
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.Moving, new DOLEventHandler(EventManager));
            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventManager));
            player.Out.SendCloseTimerWindow();

            if (!string.IsNullOrEmpty(match.CombinationId))
            {
                CharacterXCombineItem characterXCombineItem = (CharacterXCombineItem)GameServer.Database.SelectObjects<CharacterXCombineItem>(
                    DB.Column("CombinationId").IsEqualTo(player.InternalID)
                    .And(DB.Column("Character_ID").IsEqualTo(match.CombinationId)));
                if (characterXCombineItem == null)
                {
                    characterXCombineItem = new CharacterXCombineItem(player.InternalID, match.CombinationId);
                    GameServer.Database.AddObject(characterXCombineItem);
                }
            }

            // Check if player fail to combine
            if (Util.Chance(match.ChanceFailCombine))
            {
                player.Out.SendMessage("Vous échouez à combiner les objets", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
                if (match.PunishSpell != 0)
                {
                    var punishSpell = GameServer.Database.SelectObject<DBSpell>(DB.Column("SpellID").IsEqualTo(match.PunishSpell));

                    // check if the player is punished
                    if (punishSpell != null)
                    {
                        foreach (GamePlayer pl in player.GetPlayersInRadius(5000))
                        {
                            pl.Out.SendSpellEffectAnimation(player, player, (ushort)match.PunishSpell, 0, false, 5);
                        }
                        player.Out.SendSpellEffectAnimation(player, player, (ushort)match.PunishSpell, 0, false, 5);
                        player.TakeDamage(player, eDamageType.Energy, (int)punishSpell.Damage, 0);
                    }
                }
                RemoveItems(player, removeItems);
                return;
            }
            InventoryItem newItem = combined.Item;
            if (match.IsUnique)
            {
                ItemUnique unique = new ItemUnique(combined.Item.Template);
                unique.IsTradable = true;
                GameServer.Database.AddObject(unique);
                newItem = GameInventoryItem.Create(unique);
                int randomQuality = Util.Random(99);
                if (randomQuality < 16)
                {
                    newItem.Quality = 95;
                }
                else if (randomQuality < 36)
                {
                    newItem.Quality = 96;
                }
                else if (randomQuality < 56)
                {
                    newItem.Quality = 97;
                }
                else if (randomQuality < 76)
                {
                    newItem.Quality = 98;
                }
                else if (randomQuality < 96)
                {
                    newItem.Quality = 99;
                }
                else
                {
                    newItem.Quality = 100;
                }
                newItem.IsCrafted = true;
                newItem.Creator = player.Name;
                newItem.Count = match.ItemsCount;
            }

            player.Out.SendSpellEffectAnimation(player, player, (ushort)match.SpellEfect, 0, false, 1);
            player.Out.SendMessage($"Vous avez créé {newItem.Name} en combinant {useItem.Name} ainsi que {match.Items.Count() - 1} autres objects.", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            RemoveItems(player, removeItems);

            if (match.RewardCraftingSkills > 0)
            {
                int gain = match.RewardCraftingSkills;
                if (match.ApplyRewardCraftingSkillsSystem && player.CraftingSkills.ContainsKey(match.CraftingSkill))
                    // round the result
                    gain = (int)Math.Round(gain * ((double)match.CraftValue / (double)player.CraftingSkills[match.CraftingSkill]));
                player.GainCraftingSkill(match.CraftingSkill, gain);
                player.Out.SendUpdateCraftingSkills();
            }

            if (!string.IsNullOrEmpty(match.ToolKit) && match.ToolLoseDur > 0)
            {

                InventoryItem toolkit = player.Inventory.GetItemRange(eInventorySlot.MinEquipable, eInventorySlot.LastBackpack).Where(item => item.Id_nb == match.ToolKit).FirstOrDefault();
                if (toolkit != null)
                {
                    toolkit.Durability -= match.ToolLoseDur;
                    if (toolkit.Durability <= 0)
                        player.Inventory.RemoveItem(toolkit);
                    else
                        player.Out.SendInventoryItemsUpdate(new InventoryItem[] { toolkit });
                }
            }


            if (!player.ReceiveItem(player, newItem))
            {
                player.CreateItemOnTheGround(newItem);
                player.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.BuildCraftedItem.BackpackFull", newItem.Name));
            }
        }

        private IEnumerable<Combinable> GetCombinableItems(string usedItemId)
        {
            var cbitems = GameServer.Database.SelectAllObjects<CombineItemDb>();

            if (cbitems == null || !cbitems.Any())
            {
                return null;
            }

            var neededItems = cbitems.Select(
                c =>
                {
                    // Add the count of used items for a specific template to combine it with others templates
                    string[] itemsNameCount = c.ItemsIds.Split(new char[] { ';' });
                    Dictionary<string, int> items = new Dictionary<string, int>();
                    foreach (string item in itemsNameCount)
                    {
                        string[] itemNameCount = item.Split(new char[] { '|' });
                        int count;
                        if (int.TryParse(itemNameCount[1], out count))
                            items.Add(itemNameCount[0], count);
                    }
                    return new Combinable()
                    {
                        Items = items,
                        TemplateId = c.ItemTemplateId.Split(new char[] { '|' })[0],
                        ItemsCount = int.Parse(c.ItemTemplateId.Split(new char[] { '|' })[1]),
                        SpellEfect = c.SpellEffect,
                        CraftingSkill = (eCraftingSkill)c.CraftingSkill,
                        CraftValue = c.CraftingValue,
                        RewardCraftingSkills = c.RewardCraftingSkills,
                        AreaId = c.AreaId,
                        ChanceFailCombine = c.ChanceFailCombine,
                        PunishSpell = c.PunishSpell,
                        CombinexObjectModel = c.CombinexObjectModel,
                        Duration = c.Duration,
                        IsUnique = c.IsUnique,
                        ApplyRewardCraftingSkillsSystem = c.ApplyRewardCraftingSkillsSystem,
                        ToolKit = c.ToolKit,
                        ToolLoseDur = c.ToolLoseDur,
                        CombinationId = c.CombinationId
                    };
                });

            return neededItems.Where(i => i.Items.ContainsKey(usedItemId));
        }

        private Dictionary<eCraftingSkill, int> BuildRewardSkills(string rawSkills)
        {
            var skills = new Dictionary<eCraftingSkill, int>();

            if (rawSkills == null)
            {
                return skills;
            }

            var skillRaws = rawSkills.Split('|');

            foreach (var skillRaw in skillRaws)
            {
                var values = skillRaw.Split(';');

                if (values.Length == 2)
                {
                    if (Enum.TryParse(values[0], out eCraftingSkill skill) && int.TryParse(values[1], out int val))
                    {
                        if (skill != eCraftingSkill.NoCrafting)
                        {
                            if (!skills.ContainsKey(skill))
                            {
                                skills.Add(skill, val);
                            }
                            else
                            {
                                skills[skill] += val;
                            }
                        }
                    }
                }
            }

            return skills;
        }

        /// <summary>
        /// Remove the needed items to combine
        /// </summary>
        /// <param name="player"></param>
        /// <param name="removeItems"></param>
        private void RemoveItems(GamePlayer player, Dictionary<InventoryItem, int> removeItems)
        {
            foreach (InventoryItem item in removeItems.Keys)
            {
                if (item.OwnerID == null)
                    item.OwnerID = player.InternalID;

                // Replace remove item by RemoveCountFromStack
                player.Inventory.RemoveCountFromStack(item, removeItems[item]);
            }
        }
    }


    public class Combinable
    {
        /// <summary>
        /// List of Keys/Values with key = template id and value = number of items needed for this template
        /// </summary>
        public Dictionary<string, int> Items { get; set; }

        public string TemplateId { get; set; }

        /// <summary>
        /// Number of item to generate
        /// </summary>
        public int ItemsCount { get; set; }

        public int SpellEfect { get; set; }

        public eCraftingSkill CraftingSkill { get; set; }

        public int CraftValue { get; set; }

        public int RewardCraftingSkills { get; set; }

        /// <summary>
        /// Area accesseur
        /// </summary>
        public string AreaId { get; set; }

        /// <summary>
        /// Chance to faill combinaiton
        /// </summary>
        public int ChanceFailCombine { get; set; }

        /// <summary>
        /// Punish Spell if fail
        /// </summary>
        public int PunishSpell { get; set; }

        /// <summary>
        /// Time to combine
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Tool Models to Combine the Object
        /// </summary>
        public string CombinexObjectModel { get; set; }

        /// <summary>
        /// If is unique item
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// If is unique item
        /// </summary>
        public bool ApplyRewardCraftingSkillsSystem { get; set; }

        /// <summary>
        /// Toolkit template to Combine the Object
        /// </summary>
        public string ToolKit { get; set; }

        /// <summary>
        /// Point of durability lost per combination
        /// </summary>
        public short ToolLoseDur { get; set; }

        /// <summary>
        /// Combination id to reference it in player list
        /// </summary>
        public string CombinationId { get; set; }
    }

}



