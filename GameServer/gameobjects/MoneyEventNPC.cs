using DOL.Database;
using DOL.GameEvents;
using DOL.GS.Finance;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class MoneyEventNPC
        : GameNPC
    {
        private string id;
        private string mobId;
        public readonly string InteractDefault = "MoneyEventNPC.InteractTextDefault";
        public readonly string ValidateTextDefault = "MoneyEventNPC.ValidateTextDefault";
        public readonly string NeedMoreMoneyTextDefault = "MoneyEventNPC.NeedMoreMoneyTextDefault";
        public readonly string NeedMoreResource1TextDefault = "MoneyEventNPC.NeedMoreResource1TextDefault";
        public readonly string NeedMoreResource2TextDefault = "MoneyEventNPC.NeedMoreResource2TextDefault";
        public readonly string NeedMoreResource3TextDefault = "MoneyEventNPC.NeedMoreResource3TextDefault";
        public readonly string NeedMoreResource4TextDefault = "MoneyEventNPC.NeedMoreResource4TextDefault";

        public MoneyEventNPC()
        : base()
        {
        }

        public long CurrentMoney => Money.GetMoney(this.CurrentMithril, CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper);

        public int CurrentMithril
        {
            get;
            set;
        }

        public int CurrentGold
        {
            get;
            set;
        }

        public int CurrentPlatinum
        {
            get;
            set;
        }

        public int CurrentSilver
        {
            get;
            set;
        }

        public int CurrentCopper
        {
            get;
            set;
        }

        public string ServingEventID
        {
            get;
            set;
        }

        public long RequiredMoney
        {
            get;
            set;
        }

        public string NeedMoreMoneyText
        {
            get;
            set;
        }

        public string ValidateText
        {
            get;
            set;
        }

        public string InteractText
        {
            get;
            set;
        }

        public string Resource1
        {
            get;

            set;
        }

        public string Resource2
        {
            get;

            set;
        }

        public string Resource3
        {
            get;

            set;
        }

        public string Resource4
        {
            get;

            set;
        }

        public int RequiredResource1
        {
            get;

            set;
        }

        public int RequiredResource2
        {
            get;

            set;
        }

        public int RequiredResource3
        {
            get;

            set;
        }

        public int RequiredResource4
        {
            get;

            set;
        }

        public int CurrentResource1
        {
            get;

            set;
        }

        public int CurrentResource2
        {
            get;

            set;
        }

        public int CurrentResource3
        {
            get;

            set;
        }

        public int CurrentResource4
        {
            get;

            set;
        }

        public bool ShouldSaveProgress => Event == null || Event.IsInstanceMaster;

        private bool _hasShownRequirementsReached = false;
        public string RequirementsReachedText { get; set; }
        public string RequirementsReachedEmoteName { get; set; }
        public int RequirementsReachedSpellId { get; set; }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            if (this.GetStartedEvent() == null)
                return false;

            TurnTo(player, 5000);
            string currentMoney = Money.GetString(Money.GetMoney(this.CurrentMithril, this.CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper));
            if (!string.IsNullOrEmpty(InteractText))
            {
                List<object> args = new List<object> { Money.GetString(RequiredMoney), currentMoney };

                if (!string.IsNullOrEmpty(Resource1))
                {
                    args.Add(RequiredResource1);
                    args.Add(ResourceName1);
                    args.Add(CurrentResource1);
                }
                if (!string.IsNullOrEmpty(Resource2))
                {
                    args.Add(RequiredResource2);
                    args.Add(ResourceName2);
                    args.Add(CurrentResource2);
                }
                if (!string.IsNullOrEmpty(Resource3))
                {
                    args.Add(RequiredResource3);
                    args.Add(ResourceName3);
                    args.Add(CurrentResource3);
                }
                if (!string.IsNullOrEmpty(Resource4))
                {
                    args.Add(RequiredResource4);
                    args.Add(ResourceName4);
                    args.Add(CurrentResource4);
                }

                string text = LanguageMgr.GetMoneyNPCMessage(player.Client.Account.Language, InteractText, args.ToArray());

                player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            string text2;
            List<object> fallbackArgs = new List<object> { Money.GetString(RequiredMoney), currentMoney };

            if (!string.IsNullOrEmpty(Resource4))
            {
                fallbackArgs.AddRange(new object[] { RequiredResource1, ResourceName1, RequiredResource2, ResourceName2, RequiredResource3, ResourceName3, RequiredResource4, ResourceName4, CurrentResource1, CurrentResource2, CurrentResource3, CurrentResource4 });
                text2 = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NeedMoreResource4TextDefault", fallbackArgs.ToArray());
            }
            else if (!string.IsNullOrEmpty(Resource3))
            {
                fallbackArgs.AddRange(new object[] { RequiredResource1, ResourceName1, RequiredResource2, ResourceName2, RequiredResource3, ResourceName3, CurrentResource1, CurrentResource2, CurrentResource3 });
                text2 = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NeedMoreResource3TextDefault", fallbackArgs.ToArray());
            }
            else if (!string.IsNullOrEmpty(Resource2))
            {
                fallbackArgs.AddRange(new object[] { RequiredResource1, ResourceName1, RequiredResource2, ResourceName2, CurrentResource1, CurrentResource2 });
                text2 = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NeedMoreResource2TextDefault", fallbackArgs.ToArray());
            }
            else if (!string.IsNullOrEmpty(Resource1))
            {
                fallbackArgs.AddRange(new object[] { RequiredResource1, ResourceName1, CurrentResource1 });
                text2 = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NeedMoreResource1TextDefault", fallbackArgs.ToArray());
            }
            else
            {
                text2 = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.InteractTextDefault", fallbackArgs.ToArray());
            }

            player.Out.SendMessage(text2, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var player = source as GamePlayer;

            if (player == null)
                return base.ReceiveItem(source, item);

            var ev = this.GetStartedEvent();

            if (ev == null)
                return base.ReceiveItem(source, item);

            bool matched = false;
            int newValue = 0;
            if (item.Template.Id_nb == Resource1)
            {
                if (AddResource(player, CurrentResource1, RequiredResource1, ResourceName1, item, out newValue))
                {
                    CurrentResource1 = newValue;
                    matched = true;
                }
            }
            else if (item.Template.Id_nb == Resource2)
            {
                if (AddResource(player, CurrentResource2, RequiredResource2, ResourceName2, item, out newValue))
                {
                    CurrentResource2 = newValue;
                    matched = true;
                }
            }
            else if (item.Template.Id_nb == Resource3)
            {
                if (AddResource(player, CurrentResource3, RequiredResource3, ResourceName3, item, out newValue))
                {
                    CurrentResource3 = newValue;
                    matched = true;
                }
            }
            else if (item.Template.Id_nb == Resource4)
            {
                if (AddResource(player, CurrentResource4, RequiredResource4, ResourceName4, item, out newValue))
                {
                    CurrentResource4 = newValue;
                    matched = true;
                }
            }

            if (!matched) return false;

            if (CheckRequiredResources())
            {
                if (!_hasShownRequirementsReached)
                {
                    string resourceName = string.Join(", ", new[] { ResourceName1, ResourceName2, ResourceName3, ResourceName4 }.Where(name => !string.IsNullOrEmpty(name)));
                    string reachedText = RequirementsReachedText ?? LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.ResourceRequirementsReached", resourceName);
                    player.Out.SendMessage(reachedText, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);

                    eEmote finalEmote = ParseEmoteString(this.RequirementsReachedEmoteName);
                    if (finalEmote != 0)
                    {
                        foreach (GamePlayer plr in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            plr.Out.SendEmoteAnimation(this, finalEmote);
                        }
                    }

                    if (RequirementsReachedSpellId != 0)
                    {
                        ushort effectId = (ushort)RequirementsReachedSpellId;
                        foreach (GamePlayer plr in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            plr.Out.SendSpellEffectAnimation(this, this, effectId, 0, false, 1);
                        }
                    }
                }

                string text;
                if (string.IsNullOrEmpty(ValidateText))
                {
                    text = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.ValidateTextDefault");
                }
                else
                {
                    text = LanguageMgr.GetMoneyNPCMessage(player.Client.Account.Language, ValidateText);
                }
                player.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);

                this.SaveResources();
                Task.Run(() => GameEventManager.Instance.StartEvent(ev, player));
            }
            else
            {
                string text;
                if (string.IsNullOrEmpty(NeedMoreMoneyText))
                {
                    text = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NeedMoreMoneyTextDefault");
                }
                else
                {
                    text = LanguageMgr.GetMoneyNPCMessage(player.Client.Account.Language, NeedMoreMoneyText);
                }
                player.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);

                this.SaveResources();
            }
            return true;
        }

        private bool AddResource(GamePlayer player, int current, int required, string resourceName, InventoryItem item, out int newValue)
        {
            newValue = current;

            if (newValue >= required)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NoNeedMoreResources", resourceName), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            int needed = required - newValue;
            if (item.Count > needed)
            {
                newValue += needed;
                player.Inventory.RemoveCountFromStack(item, needed);
            }
            else
            {
                newValue += item.Count;
                player.Inventory.RemoveItem(item);
            }

            return true;
        }

        private bool CheckRequiredResources()
        {
            return CurrentMoney >= RequiredMoney
                && CurrentResource1 >= RequiredResource1
                && CurrentResource2 >= RequiredResource2
                && CurrentResource3 >= RequiredResource3
                && CurrentResource4 >= RequiredResource4;
        }

        private string GetItemName(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
                return string.Empty;

            var itemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(resourceId);
            return itemTemplate?.Name ?? resourceId;
        }

        public string ResourceName1 => GetItemName(Resource1);
        public string ResourceName2 => GetItemName(Resource2);
        public string ResourceName3 => GetItemName(Resource3);
        public string ResourceName4 => GetItemName(Resource4);

        public override bool ReceiveMoney(GameLiving source, long money)
        {
            var player = source as GamePlayer;

            if (player == null)
                return base.ReceiveMoney(source, money);

            var ev = this.GetStartedEvent();

            if (ev == null)
                return base.ReceiveMoney(source, money);


            if (CurrentMoney >= RequiredMoney)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NoNeedMoreMoney"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            long needed = RequiredMoney - CurrentMoney;

            if (money > needed)
            {
                AddMoney(needed);
                player.RemoveMoney(Currency.Copper.Mint(needed));
                long leftover = money - needed;
            }
            else
            {
                AddMoney(money);
                player.RemoveMoney(Currency.Copper.Mint(money));
            }

            if (CheckRequiredResources())
            {
                if (!_hasShownRequirementsReached)
                {
                    string reachedText = RequirementsReachedText ?? LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.MoneyRequirementsReached");
                    player.Out.SendMessage(reachedText, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);

                    eEmote finalEmote = ParseEmoteString(this.RequirementsReachedEmoteName);
                    if (finalEmote != 0)
                    {
                        foreach (GamePlayer plr in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            plr.Out.SendEmoteAnimation(this, finalEmote);
                        }
                    }

                    if (RequirementsReachedSpellId != 0)
                    {
                        ushort effectId = (ushort)RequirementsReachedSpellId;
                        foreach (GamePlayer plr in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            plr.Out.SendSpellEffectAnimation(this, this, effectId, 0, false, 1);
                        }
                    }
                }

                string text;
                if (string.IsNullOrEmpty(ValidateText))
                {
                    text = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.ValidateTextDefault");
                }
                else
                {
                    text = LanguageMgr.GetMoneyNPCMessage(player.Client.Account.Language, ValidateText);
                }
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);

                this.SaveIntoDatabase();
                Task.Run(() => GameEventManager.Instance.StartEvent(ev, player));
            }
            else
            {
                string text;
                if (string.IsNullOrEmpty(NeedMoreMoneyText))
                {
                    text = LanguageMgr.GetTranslation(player.Client.Account.Language, "MoneyEventNPC.NeedMoreMoneyTextDefault");
                }
                else
                {
                    text = LanguageMgr.GetMoneyNPCMessage(player.Client.Account.Language, NeedMoreMoneyText);
                }
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);

                this.SaveIntoDatabase();
            }

            return true;
        }

        private void AddMoney(long copper)
        {
            CurrentGold += Money.GetGold(copper);
            CurrentPlatinum += Money.GetPlatinum(copper);
            CurrentMithril += Money.GetMithril(copper);
            CurrentSilver += Money.GetSilver(copper);
            CurrentCopper += Money.GetCopper(copper);
        }

        private GameEvent GetStartedEvent()
        {
            if (ServingEventID == null)
                return null;

            var ev = GameEventManager.Instance.GetEventByID(ServingEventID);
            if (ev == null || ev.StartConditionType != StartingConditionType.Money || ev.StartedTime.HasValue)
            {
                return null;
            }

            return ev;
        }

        private eEmote ParseEmoteString(string emoteStr)
        {
            if (string.IsNullOrEmpty(emoteStr))
                return 0;

            switch (emoteStr.ToLower())
            {
                case "angry": return eEmote.Angry;
                case "bang": return eEmote.BangOnShield;
                case "beckon": return eEmote.Beckon;
                case "beg": return eEmote.Beg;
                case "blush": return eEmote.Blush;
                case "bow": return eEmote.Bow;
                case "cheer": return eEmote.Cheer;
                case "clap": return eEmote.Clap;
                case "cry": return eEmote.Cry;
                case "dance": return eEmote.Dance;
                case "kiss": return eEmote.BlowKiss;
                case "laugh": return eEmote.Laugh;
                case "no": return eEmote.No;
                case "point": return eEmote.Point;
                case "ponder": return eEmote.Ponder;
                case "pray": return eEmote.Pray;
                case "roar": return eEmote.Roar;
                case "salute": return eEmote.Salute;
                case "shrug": return eEmote.Shrug;
                case "slap": return eEmote.Slap;
                case "slit": return eEmote.Slit;
                case "smile": return eEmote.Smile;
                case "taunt": return eEmote.Taunt;
                case "victory": return eEmote.Victory;
                case "wave": return eEmote.Wave;
                case "yawn": return eEmote.Yawn;
                case "yes": return eEmote.Yes;

                default:
                    return 0;
            }
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
            {
                return false;
            }

            var ev = this.GetStartedEvent();
            if (ev != null && CheckRequiredResources() && !ev.StartedTime.HasValue)
            {
                this.SaveIntoDatabase();
                Task.Run(() => GameEventManager.Instance.StartEvent(ev, null));
            }
            this.ReloadMoneyValues();
            return true;
        }

        public override void LoadFromDatabase(Database.DataObject obj)
        {
            base.LoadFromDatabase(obj);
            var mob = GameServer.Database.SelectObjects<MoneyNpcDb>(DB.Column("MobID").IsEqualTo(obj.ObjectId))?.FirstOrDefault();

            if (mob != null)
            {
                id = mob.ObjectId;
                mobId = mob.MobID;
                ReloadMoneyValues(mob);
            }

        }

        private void ReloadMoneyValues(MoneyNpcDb eventNpc = null)
        {
            if (eventNpc == null)
            {
                eventNpc = GameServer.Database.FindObjectByKey<MoneyNpcDb>(this.id);
            }

            if (eventNpc != null)
            {
                this.CurrentGold = Money.GetGold(eventNpc.CurrentAmount);
                this.CurrentCopper = Money.GetCopper(eventNpc.CurrentAmount);
                this.CurrentMithril = Money.GetMithril(eventNpc.CurrentAmount);
                this.CurrentPlatinum = Money.GetPlatinum(eventNpc.CurrentAmount);
                this.CurrentSilver = Money.GetSilver(eventNpc.CurrentAmount);
                this.ServingEventID = eventNpc.EventID;
                this.RequiredMoney = eventNpc.RequiredMoney;
                this.NeedMoreMoneyText = eventNpc.NeedMoreMoneyText;
                this.ValidateText = eventNpc.ValidateText;
                this.InteractText = eventNpc.InteractText;
                this.RequirementsReachedEmoteName = eventNpc.RequirementsReachedEmote;
                this.RequirementsReachedSpellId = eventNpc.RequirementsReachedSpellId;
                CurrentResource1 = eventNpc.CurrentResource1;
                CurrentResource2 = eventNpc.CurrentResource2;
                CurrentResource3 = eventNpc.CurrentResource3;
                CurrentResource4 = eventNpc.CurrentResource4;
                RequiredResource1 = eventNpc.RequiredResource1;
                RequiredResource2 = eventNpc.RequiredResource2;
                RequiredResource3 = eventNpc.RequiredResource3;
                RequiredResource4 = eventNpc.RequiredResource4;
                Resource1 = eventNpc.Resource1;
                Resource2 = eventNpc.Resource2;
                Resource3 = eventNpc.Resource3;
                Resource4 = eventNpc.Resource4;
            }
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            return eQuestIndicator.Lesson;
        }

        private void SaveResources(MoneyNpcDb db)
        {
            if (db == null)
                return;
            
            db.CurrentResource1 = CurrentResource1;
            db.CurrentResource2 = CurrentResource2;
            db.CurrentResource3 = CurrentResource3;
            db.CurrentResource4 = CurrentResource4;
            GameServer.Database.SaveObject(db);
        }

        private void SaveResources()
        {
            if (!ShouldSaveProgress)
            {
                return;
            }

            SaveResources(GameServer.Database.FindObjectByKey<MoneyNpcDb>(this.id));
        }


        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();

            MoneyNpcDb db = null;

            if (id == null)
            {
                db = new MoneyNpcDb();
            }
            else
            {
                db = GameServer.Database.FindObjectByKey<MoneyNpcDb>(this.id);
            }

            if (db != null)
            {
                db.CurrentAmount = Money.GetMoney(CurrentMithril, CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper);
                db.EventID = ServingEventID ?? string.Empty;
                db.RequiredMoney = RequiredMoney;
                db.MobID = this.InternalID;
                db.MobName = this.Name;
                db.RequirementsReachedEmote = this.RequirementsReachedEmoteName;
                db.RequirementsReachedSpellId = this.RequirementsReachedSpellId;
                db.CurrentResource1 = CurrentResource1;
                db.CurrentResource2 = CurrentResource2;
                db.CurrentResource3 = CurrentResource3;
                db.CurrentResource4 = CurrentResource4;
                db.RequiredResource1 = RequiredResource1;
                db.RequiredResource2 = RequiredResource2;
                db.RequiredResource3 = RequiredResource3;
                db.RequiredResource4 = RequiredResource4;
                db.Resource1 = Resource1;
                db.Resource2 = Resource2;
                db.Resource3 = Resource3;
                db.Resource4 = Resource4;

                if (InteractText != null)
                    db.InteractText = InteractText;

                if (NeedMoreMoneyText != null)
                    db.NeedMoreMoneyText = NeedMoreMoneyText;

                if (ValidateText != null)
                    db.ValidateText = ValidateText;
            }

            if (id == null)
            {
                GameServer.Database.AddObject(db);
                id = db!.ObjectId;
            }
            else
            {
                GameServer.Database.SaveObject(db);
            }
        }
    }
}