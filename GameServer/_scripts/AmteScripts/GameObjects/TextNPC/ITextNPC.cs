/**
 * Created by Virant "Dre" Jérémy for Amtenael
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Scripts
{
    public interface ITextNPC
    {
        TextNPCPolicy TextNPCData { get; set; }
        void SayRandomPhrase();
    }

    public class TextNPCPolicy
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private long _lastPhrase;
        private readonly GameNPC _body;

        public readonly Dictionary<string, DBEchangeur> EchangeurDB = new Dictionary<string, DBEchangeur>();
        public Dictionary<string, string> Reponses { get; private set; }
        public Dictionary<string, eEmote> EmoteReponses { get; private set; }
        public Dictionary<string, ushort> SpellReponses { get; private set; }
        public Dictionary<string, eEmote> RandomPhrases { get; private set; }
        public string Interact_Text { get; set; }
        public int PhraseInterval { get; set; }
        public TextNPCCondition Condition { get; private set; }
        public DBTextNPC TextDB { get; set; }
        bool? IsOutlawFriendly { get; set; }

        public Dictionary<string, EchangeurInfo> PlayerReferences;

        public TextNPCPolicy(GameNPC body)
        {
            Condition = new TextNPCCondition("");
            Reponses = new Dictionary<string, string>();
            EmoteReponses = new Dictionary<string, eEmote>();
            SpellReponses = new Dictionary<string, ushort>();
            _body = body;
            _lastPhrase = 0;
            Interact_Text = "";
            PhraseInterval = 0;
            PlayerReferences = new Dictionary<string, EchangeurInfo>();
        }

        public bool Interact(GamePlayer player)
        {
            if (string.IsNullOrEmpty(Interact_Text) || !CheckAccess(player))
                return false;

            _body.TurnTo(player);        

            if (this.IsOutlawFriendly.HasValue)
            {
                if (this.IsOutlawFriendly.Value)
                {
                    if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
                    {
                        player.Out.SendMessage("...", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return true;
                    }
                }
                else
                {
                    if (player.Reputation < 0 && player.Client.Account.PrivLevel == 1)
                    {
                        player.Out.SendMessage("...", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return true;
                    }
                }
            }

            //Message
            string text = string.Format(Interact_Text, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
            if (text != "")
                player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);

            //Spell
            if (SpellReponses != null && SpellReponses.ContainsKey("INTERACT"))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendSpellEffectAnimation(_body, player, SpellReponses["INTERACT"], 0, false, 1);

            //Emote
            if (EmoteReponses != null && EmoteReponses.ContainsKey("INTERACT"))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendEmoteAnimation(_body, EmoteReponses["INTERACT"]);

            return true;
        }

        public bool WhisperReceive(GameLiving source, string str)
        {
            if (!(source is GamePlayer))
                return false;
            GamePlayer player = source as GamePlayer;
            if (!CheckAccess(player))
                return false;

            _body.TurnTo(player, 10000);

            if (this.IsOutlawFriendly.HasValue)
            {
                if (this.IsOutlawFriendly.Value)
                {
                    if (player.Reputation >= 0 && player.Client.Account.PrivLevel == 1)
                    {
                        return true;
                    }
                }
                else
                {
                    if (player.Reputation <= 0 && player.Client.Account.PrivLevel == 1)
                    {
                        return true;
                    }
                }
            }

            //Message
            if (Reponses != null && Reponses.ContainsKey(str))
            {
                string text = string.Format(Reponses[str], player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                if (text != "")
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            //Spell
            if (SpellReponses != null && SpellReponses.ContainsKey(str))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendSpellEffectAnimation(_body, player, SpellReponses[str], 0, false, 1);

            //Emote
            if (EmoteReponses != null && EmoteReponses.ContainsKey(str))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendEmoteAnimation(_body, EmoteReponses[str]);


            return true;
        }

        public bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source == null || item == null)
                return false;
            _body.Notify(GameObjectEvent.ReceiveItem, _body, new ReceiveItemEventArgs(source, _body, item));

            if (!(source is GamePlayer) || !EchangeurDB.ContainsKey(item.Id_nb))
                return false;

            GamePlayer player = source as GamePlayer;
            if (!CheckAccess(player)) return false;
            _body.TurnTo(player);

            DBEchangeur EchItem = EchangeurDB[item.Id_nb];
            if (EchItem.ItemRecvCount > item.Count)
            {
                player.Out.SendMessage("Vous devez donner " + EchItem.ItemRecvCount + " " + item.Name + ".", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (EchItem.MoneyPrice > 0 && player.GetCurrentMoney() < EchItem.MoneyPrice)
            {
                player.Out.SendMessage(string.Format("Vous avez besoin de {0} pour échanger cet objet", Money.GetString(EchItem.MoneyPrice)), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            var requireditems = this.GetRequireItems(EchItem);          

            if (requireditems.Any())
            {
                var playerItems = this.GetPlayerRequiredItems(player, requireditems, item.Id_nb);
                if (playerItems.HasAllRequiredItems)
                { 
                    player.Client.Out.SendCustomDialog(string.Format("Afin de procéder à l'échange, il va falloir payer {0} et me donner en plus {1}", 
                       Money.GetString(EchItem.MoneyPrice),
                       string.Join(", ", requireditems.Select(r => string.Format("{0} {1}", r.Count, r.Name)))
                        ), this.HandleClientResponse);
                }
                else
                {
                    player.Client.Out.SendMessage(string.Format("Il va te manquer \n{0}\n pour procéder à l'échange.", string.Join("\n", playerItems.Items.Select(i => string.Format("{0} {1}", i.Value, i.Key)))), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                }

                //Handle references for callback
                if (PlayerReferences.ContainsKey(player.InternalID))
                {
                    this.PlayerReferences[player.InternalID] = new EchangeurInfo() { requireInfos = requireditems, GiveItem = item as GameInventoryItem };
                }
                else
                {
                    this.PlayerReferences.Add(player.InternalID, new EchangeurInfo() { requireInfos = requireditems, GiveItem = item as GameInventoryItem });
                }

                return false;
            }
            else
            {
                if (EchItem.MoneyPrice > 0)
                {
                //Handle references for callback
                if (PlayerReferences.ContainsKey(player.InternalID))
                {
                    this.PlayerReferences[player.InternalID] = new EchangeurInfo() { requireInfos = requireditems, GiveItem = item as GameInventoryItem };
                }
                else
                {
                    this.PlayerReferences.Add(player.InternalID, new EchangeurInfo() { requireInfos = requireditems, GiveItem = item as GameInventoryItem });
                }

                    player.Client.Out.SendCustomDialog(string.Format("J'aurais besoin de {0} pour échanger ça. Valider l'échange ?", Money.GetString(EchItem.MoneyPrice)), this.HandleClientResponse);
                    return false;
                }
                else
                {
                    return ProcessExchange(item, player, EchItem, requireditems);
                }  
            }
        }

        public void HandleClientResponse(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                if (this.PlayerReferences.ContainsKey(player.InternalID) && EchangeurDB.ContainsKey(this.PlayerReferences[player.InternalID].GiveItem.Id_nb))
                {
                    var echangeur = EchangeurDB[this.PlayerReferences[player.InternalID].GiveItem.Id_nb];

                    this.ProcessExchange(this.PlayerReferences[player.InternalID].GiveItem, player, echangeur, this.PlayerReferences[player.InternalID].requireInfos);
                }
                else
                {
                    log.Error("Impossible to get player reference from callback in Echangeur from player id" + player.InternalID);
                }
            }

            this.PlayerReferences.Remove(player.InternalID);
        }

        private IEnumerable<RequireItemInfo> GetRequireItems(DBEchangeur ech)
        {
            List<RequireItemInfo> items = new List<RequireItemInfo>();

            var val1 = this.ParseItem(ech.PriceRessource1);

            if (val1 != null)
            {
                items.Add(val1);
            }

            var val2 = this.ParseItem(ech.PriceRessource2);

            if (val2 != null)
            {
                items.Add(val2);
            }

            var val3 = this.ParseItem(ech.PriceRessource3);

            if (val3 != null)
            {
                items.Add(val3);
            }

            return items;
        }

        private RequireItemInfo ParseItem(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            int count = 0;
            var item =  raw.Split(new char[] { '|' });

            if (item.Length == 2 && int.TryParse(item[1], out count))
            {
                var itemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(item[0]);
                string name = string.Empty;

                if (itemTemplate != null)
                {
                    name = itemTemplate.Name;
                }

                return new RequireItemInfo()
                {
                    ItemId = item[0],
                    Count = count,
                    Name = name
                };
            }

            return null;
        }


        private void RemoveItemsFromPlayer(GamePlayer player, IEnumerable<RequireItemInfo> requireItems, InventoryItem gaveItem)
        {
            List<GameInventoryItem> items = new List<GameInventoryItem>();
            var playerItems = new Dictionary<string, int>();

            foreach (var val in requireItems)
            {
                if (!playerItems.ContainsKey(val.ItemId))
                    playerItems.Add(val.ItemId, 0);
            }

            foreach (GameInventoryItem item in player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
            {
                var requireItem = requireItems.FirstOrDefault(i => i.ItemId.Equals(item.Id_nb));

                if (requireItem != null)
                {
                    if (item.Id_nb.Equals(gaveItem))
                    {
                        continue;
                    }

                    if (item.Count >= requireItem.Count)
                    {
                        player.Inventory.RemoveCountFromStack(item, requireItem.Count);
                    }
                    else
                    {
                        items.Add(item);
                    }
                    requireItem.Name = item.Name;
                }
            }

            foreach (var item in items)
            {
                if (item.OwnerID == null)
                    item.OwnerID = player.InternalID;

                player.Inventory.RemoveItem(item);
            }
        }

        private EchangeurPlayerItemsCount GetPlayerRequiredItems(GamePlayer player, IEnumerable<RequireItemInfo> requireItems, string gaveItem)
        {
            var playerItems = new Dictionary<string, int>();
            var playerItemsCount = new Dictionary<string, int>();
            bool hasAllRequiredItems = true;

            foreach (var val in requireItems)
            {
                if (!playerItems.ContainsKey(val.ItemId))
                    playerItems.Add(val.ItemId, 0);
            }

            bool hasRemovedgaveItem = false;

            foreach (var item in player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
            { 
                var requireItem = requireItems.FirstOrDefault(i => i.ItemId.Equals(item.Id_nb));

                if (requireItem != null)
                {
                    if (item.Id_nb.Equals(gaveItem) && !hasRemovedgaveItem)
                    {
                        hasRemovedgaveItem = true;
                        continue;
                    }

                    if (item.Count >= requireItem.Count)
                    {
                        playerItems[item.Id_nb] = item.Count;
                    }
                    else
                    {
                        playerItems[item.Id_nb] += item.Count;
                    }

                    requireItem.Name = item.Name;
                }
            }


            foreach (var reqItem in requireItems)
            {
                int missingCount = reqItem.Count - playerItems[reqItem.ItemId];
                if (missingCount > 0)
                {
                    hasAllRequiredItems = false;
                    playerItemsCount.Add(reqItem.Name, missingCount);
                }
            }

            return new EchangeurPlayerItemsCount() { HasAllRequiredItems = hasAllRequiredItems, Items = playerItemsCount  };
        }


        private bool ProcessExchange(InventoryItem item, GamePlayer player, DBEchangeur echItem, IEnumerable<RequireItemInfo> requireItems)
        {
            if (!player.Inventory.RemoveCountFromStack(item, echItem.ItemRecvCount))
                return false;
            InventoryLogging.LogInventoryAction(player, _body, eInventoryActionType.Quest, item.Template, echItem.ItemRecvCount);

            if (echItem.GiveTemplate != null)
                if (!player.Inventory.AddTemplate(GameInventoryItem.Create(echItem.GiveTemplate), echItem.ItemGiveCount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                {
                    player.Out.SendMessage("Votre inventaire est plein, l'objet est déposé au sol.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    for (int i = 0; i < echItem.ItemGiveCount; i++)
                        player.CreateItemOnTheGround(GameInventoryItem.Create(echItem.GiveTemplate));
                }
                else
                    InventoryLogging.LogInventoryAction(_body, player, eInventoryActionType.Quest, echItem.GiveTemplate, echItem.ItemGiveCount);

            if (echItem.GainMoney > 0)
            {
                player.AddMoney(echItem.GainMoney);
                InventoryLogging.LogInventoryAction(_body, player, eInventoryActionType.Quest, echItem.GainMoney);
            }
            if (echItem.GainXP > 0)
                player.GainExperience(GameLiving.eXPSource.Quest, echItem.GainXP);
            else if (echItem.GainXP < 0)
            {
                long xp = (player.ExperienceForNextLevel - player.ExperienceForCurrentLevel ) * echItem.GainXP / -1000;
                player.GainExperience(GameLiving.eXPSource.Quest, xp);
            }

            if (echItem.MoneyPrice > 0)
                player.RemoveMoney(echItem.MoneyPrice, "Vous avez payé {0}");

            if (requireItems.Any())
                this.RemoveItemsFromPlayer(player, requireItems, item);  


            echItem.ChangedItemCount++;
            GameServer.Database.SaveObject(echItem);

            if (Reponses != null && Reponses.ContainsKey(echItem.ItemRecvID))
            {
                string text = string.Format(Reponses[echItem.ItemRecvID], player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                if (text != "")
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            return true;
        }

        public bool CheckAccess(GamePlayer player)
        {
            return Condition.CheckAccess(player);
        }

        public virtual void SayRandomPhrase()
        {
            if (RandomPhrases == null || RandomPhrases.Count < 1)
                return;
            if (_lastPhrase > DateTime.Now.Ticks / 10000 - PhraseInterval * 1000)
                return;
            _lastPhrase = DateTime.Now.Ticks / 10000;

            //Heure
            int heure = (int)(WorldMgr.GetCurrentGameTime() / 1000 / 60 / 54);
            if (Condition.Heure_max < Condition.Heure_min && (Condition.Heure_min > heure || heure <= Condition.Heure_max))
                return;
            if (Condition.Heure_max > Condition.Heure_min && (Condition.Heure_min > heure || heure >= Condition.Heure_max))
                return;
            if (Condition.Heure_max == Condition.Heure_min && heure != Condition.Heure_min)
                return;

            int phrase = Util.Random(0, RandomPhrases.Count - 1);
            int i = 0;
            string text = "";
            eEmote emote = 0;
            foreach (var de in RandomPhrases)
            {
                if (i == phrase)
                {
                    text = de.Key;
                    emote = de.Value;
                    break;
                }
                i++;
            }

            //Phrase
            if (text.StartsWith("say:"))
                _body.Say(text.Substring(4));
            else if (text.StartsWith("yell:"))
                _body.Yell(text.Substring(5));
            else if (text.StartsWith("em:"))
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.YELL_DISTANCE))
                    plr.Out.SendMessage(_body.Name + " " + text.Substring(3), eChatType.CT_Emote, eChatLoc.CL_ChatWindow);

            //Emote
            if (emote != 0)
                foreach (GamePlayer plr in _body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendEmoteAnimation(_body, emote);

        }

        public void LoadFromDatabase(DataObject obj)
        {
            var objs = GameServer.Database.SelectObjects<DBEchangeur>(e => e.NpcID == obj.ObjectId);
            foreach (DBEchangeur echangeur in objs)
                if (!EchangeurDB.ContainsKey(echangeur.ItemRecvID))
                    EchangeurDB.Add(echangeur.ItemRecvID, echangeur);

            DBTextNPC data = null;
            try
            {
                data = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == obj.ObjectId);
            }
            catch
            {
                DBTextNPC.Init();
            }
            if (data == null)
                data = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == obj.ObjectId);
            if (data == null)
                return;

            //Chargement des textes
            TextDB = data;
            Interact_Text = TextDB.Text;

            //Set this value only when OR Exclusive
            if (data.IsOutlawFriendly ^ data.IsRegularFriendly)
            {
                if (data.IsRegularFriendly)
                {
                    IsOutlawFriendly = false;
                }

                if (data.IsOutlawFriendly)
                {
                    IsOutlawFriendly = true;
                }
            }
            else if (data.IsOutlawFriendly && data.IsRegularFriendly)
            {
                log.Error("Cannot load IsOutlawFriendly Status because both values are set. Update database (DbTextNPC) for id: " + data.ObjectId + " npc: " + data.MobName);
            }


            var table = new Dictionary<string, string>();
            if (TextDB.Reponse != "")
            {
                foreach (string item in TextDB.Reponse.Split(';'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    table.Add(items[0], items[1]);
                }
            }
            Reponses = table;

            //Chargement des spells réponses
            var table2 = new Dictionary<string, ushort>();
            if (TextDB.ReponseSpell != "")
            {
                foreach (string item in TextDB.ReponseSpell.Split('\n'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    try
                    {
                        table2.Add(items[0], ushort.Parse(items[1]));
                    }
                    catch { }
                }
            }
            SpellReponses = table2;

            //Chargement des emotes réponses
            var table3 = new Dictionary<string, eEmote>();
            if (TextDB.ReponseEmote != "")
            {
                foreach (string item in TextDB.ReponseEmote.Split('\n'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    try
                    {
                        table3.Add(items[0], (eEmote)Enum.Parse(typeof(eEmote), items[1], false));
                    }
                    catch { }
                }
            }
            EmoteReponses = table3;

            //phrase/emote aléatoire
            var table4 = new Dictionary<string, eEmote>();
            if (TextDB.RandomPhraseEmote != "")
            {
                foreach (string item in TextDB.RandomPhraseEmote.Split('\n'))
                {
                    string[] items = item.Split('|');
                    if (items.Length != 2)
                        continue;
                    try
                    {
                        if (items[1] == "0")
                            table4.Add(items[0], 0);
                        else
                            table4.Add(items[0], (eEmote)Enum.Parse(typeof(eEmote), items[1], false));
                    }
                    catch (Exception e)
                    {
                        log.Error("ERROR :", e);
                    }
                }
            }
            RandomPhrases = table4;
            PhraseInterval = TextDB.PhraseInterval;



            //Chargement des conditions
            Condition = new TextNPCCondition(TextDB.Condition);
        }

        public void SaveIntoDatabase()
        {
            if (TextDB == null)
                TextDB = new DBTextNPC();
            TextDB.MobID = _body.InternalID;
            TextDB.MobName = _body.Name;
            TextDB.MobRealm = (byte)_body.Realm;
            TextDB.Text = Interact_Text;

            //Sauve les réponses
            string reponse = "";
            if (Reponses != null && Reponses.Count > 0)
            {
                foreach (var de in Reponses)
                {
                    if (reponse.Length > 1)
                        reponse += ";";
                    reponse += de.Key.Trim('|', ';') + "|" + de.Value.Trim('|', ';');
                }
            }
            TextDB.Reponse = reponse;

            //Sauve les spell réponses
            reponse = "";
            if (SpellReponses != null && SpellReponses.Count > 0)
            {
                foreach (var de in SpellReponses)
                {
                    if (reponse.Length > 1)
                        reponse += "\n";
                    reponse += de.Key.Trim('|', ';') + "|" + de.Value;
                }
            }
            TextDB.ReponseSpell = reponse;

            //Sauve les emote réponses
            reponse = "";
            if (EmoteReponses != null && EmoteReponses.Count > 0)
            {
                foreach (var de in EmoteReponses)
                {
                    if (reponse.Length > 1)
                        reponse += "\n";
                    reponse += de.Key.Trim('|', ';') + "|" + de.Value;
                }
            }
            TextDB.ReponseEmote = reponse;

            //Sauve les phrase/emote aléatoire
            reponse = "";
            if (RandomPhrases != null && RandomPhrases.Count > 0)
            {
                foreach (var de in RandomPhrases)
                {
                    if (reponse.Length > 1)
                        reponse += "\n";
                    reponse += de.Key.Trim('|', '\n') + "|" + de.Value;
                }
            }
            TextDB.RandomPhraseEmote = reponse;
            TextDB.PhraseInterval = PhraseInterval;            

            //Sauve les conditions
            if (Condition != null)
                TextDB.Condition = Condition.GetConditionString();

            var data = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == _body.InternalID);
            if (data == null)
                GameServer.Database.AddObject(TextDB);
            else
            {
                TextDB.ObjectId = data.ObjectId;
                GameServer.Database.SaveObject(TextDB);
            }

            foreach (KeyValuePair<string, DBEchangeur> pair in EchangeurDB)
            {
                pair.Value.NpcID = _body.InternalID;
                if (pair.Value.IsPersisted)
                    GameServer.Database.SaveObject(pair.Value);
                else
                    GameServer.Database.AddObject(pair.Value);
            }
        }

        public void DeleteFromDatabase()
        {
            if (TextDB != null && TextDB.IsPersisted)
                GameServer.Database.DeleteObject(TextDB);
            foreach (KeyValuePair<string, DBEchangeur> pair in EchangeurDB)
                if (pair.Value.IsPersisted)
                    GameServer.Database.DeleteObject(pair.Value);
        }

        public IList<string> DelveInfo()
        {
            List<string> text = new List<string>
				{
					" + OID: " + _body.ObjectID,
					" + Class: " + _body.GetType(),
					" + Position: " + _body.Position + " Heading=" + _body.Heading,
					" + Realm: " + _body.Realm,
					" + Model: " + _body.Model,
					"",
					"-- Echangeur (Items) --"
				};
            foreach (KeyValuePair<string, DBEchangeur> pair in EchangeurDB)
            {
                text.Add(" - " + pair.Value.ItemRecvID + " (" + pair.Value.ItemRecvCount + "):");
                if (pair.Value.ItemGiveCount > 0)
                    text.Add(" . " + pair.Value.ItemGiveCount + " " + pair.Value.ItemGiveID);
                if (pair.Value.GainMoney > 0)
                    text.Add(" . " + Money.GetString(pair.Value.GainMoney));
                if (pair.Value.GainXP > 0)
                    text.Add(" . " + pair.Value.GainXP + "xp");
                if (pair.Value.GainXP < 0)
                    text.Add(" . " + (-pair.Value.GainXP) + "/1000 du niveau en cours");
                if (Reponses.ContainsKey(pair.Value.ItemRecvID))
                    text.Add(" . Réponse: " + Reponses[pair.Value.ItemRecvID]);
                text.Add(" . " + pair.Value.ChangedItemCount + " Items échangés");
            }
            return text;
        }
    }
}
