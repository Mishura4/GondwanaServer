using DOL.Database;
using DOL.GameEvents;
using DOL.GS.PacketHandler;
using DOLDatabase.Tables;
using System;
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

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            if (this.CheckEventValidity() == null)
                return false;


            TurnTo(player, 5000);
            string currentMoney = Money.GetString(Money.GetMoney(this.CurrentMithril, this.CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper));
            string text;
            if (!string.IsNullOrEmpty(Resource4))
                text = Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreResource4TextDefault, Money.GetString(this.RequiredMoney), currentMoney, RequiredResource1, Resource1, RequiredResource2, Resource2, RequiredResource3, Resource3, RequiredResource4, Resource4, CurrentResource1, CurrentResource2, CurrentResource3, CurrentResource4);
            else if (!string.IsNullOrEmpty(Resource3))
                text = Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreResource3TextDefault, Money.GetString(this.RequiredMoney), currentMoney, RequiredResource1, Resource1, RequiredResource2, Resource2, RequiredResource3, Resource3, CurrentResource1, CurrentResource2, CurrentResource3);
            else if (!string.IsNullOrEmpty(Resource2))
                text = Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreResource2TextDefault, Money.GetString(this.RequiredMoney), currentMoney, RequiredResource1, Resource1, RequiredResource2, Resource2, CurrentResource1, CurrentResource2);
            if (!string.IsNullOrEmpty(Resource1))
                text = Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreResource1TextDefault, Money.GetString(this.RequiredMoney), currentMoney, RequiredResource1, Resource1, CurrentResource1);
            else
                text = Language.LanguageMgr.GetTranslation(player.Client.Account.Language, InteractDefault, Money.GetString(this.RequiredMoney), currentMoney);
            player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var player = source as GamePlayer;

            if (player == null)
                return base.ReceiveItem(source, item);

            var ev = this.CheckEventValidity();

            if (ev == null)
                return base.ReceiveItem(source, item);

            if (item.Template.Id_nb == Resource1)
                CurrentResource1 += item.Count;
            if (item.Template.Id_nb == Resource2)
                CurrentResource2 += item.Count;
            if (item.Template.Id_nb == Resource3)
                CurrentResource3 += item.Count;
            if (item.Template.Id_nb == Resource4)
                CurrentResource4 += item.Count;
            else
                return false;

            if (CheckRequiredResources())
            {
                var text = ValidateText ?? Language.LanguageMgr.GetTranslation(player.Client.Account.Language, ValidateTextDefault);
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                player.Inventory.RemoveItem(item);
                this.SaveIntoDatabase();
                Task.Run(() => GameEventManager.Instance.StartEvent(ev));
            }
            else
            {
                string text = Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreMoneyTextDefault);
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                player.Inventory.RemoveItem(item);
                this.SaveIntoDatabase();
            }

            return true;
        }

        private bool CheckRequiredResources()
        {
            return CurrentMoney >= RequiredMoney && CurrentResource1 >= RequiredResource1 && CurrentResource2 >= RequiredResource2 && CurrentResource3 >= RequiredResource3 && CurrentResource4 >= CurrentResource4;
        }

        public override bool ReceiveMoney(GameLiving source, long money)
        {
            var player = source as GamePlayer;

            if (player == null)
                return base.ReceiveMoney(source, money);

            var ev = this.CheckEventValidity();

            if (ev == null)
                return base.ReceiveMoney(source, money);


            this.CurrentGold += Money.GetGold(money);
            this.CurrentPlatinum += Money.GetPlatinum(money);
            this.CurrentMithril += Money.GetMithril(money);
            this.CurrentSilver += Money.GetSilver(money);
            this.CurrentCopper += Money.GetCopper(money);

            if (CheckRequiredResources())
            {
                var text = ValidateText ?? Language.LanguageMgr.GetTranslation(player.Client.Account.Language, ValidateTextDefault);
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                player.RemoveMoney(money);
                this.SaveIntoDatabase();
                Task.Run(() => GameEventManager.Instance.StartEvent(ev));
            }
            else
            {
                var text = NeedMoreMoneyText ?? Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreMoneyTextDefault);
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                player.RemoveMoney(money);
                this.SaveIntoDatabase();
            }

            return true;
        }

        private GameEvent CheckEventValidity()
        {
            if (ServingEventID == null)
                return null;

            var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(ServingEventID));

            if (ev == null || ev.StartConditionType != StartingConditionType.Money || ev.StartedTime.HasValue)
            {
                return null;
            }

            return ev;
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
            {
                return false;
            }

            this.ReloadMoneyValues();
            return true;
        }

        public override void LoadFromDatabase(Database.DataObject obj)
        {
            base.LoadFromDatabase(obj);
            var mob = GameServer.Database.SelectObjects<MoneyNpcDb>("`MobID` = @MobID", new Database.QueryParameter("MobID", obj.ObjectId))?.FirstOrDefault();

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
                id = db.ObjectId;
            }
            else
            {
                GameServer.Database.SaveObject(db);
            }
        }

    }
}