using AmteScripts.Managers;
using DOL.Database;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.GS.ServerProperties;
using DOL.Language;
using log4net;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DOL.Territories
{
    public class TerritoryLord : GameNPC
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public DateTime lastClaim = new DateTime(1);

        private RegionTimer _claimTimer;

        private readonly object _lockObject = new object();

        private readonly HashSet<string> _playersAuthorized = new();


        public TimeSpan TimeBeforeClaim
        {
            get
            {
                return lastClaim.AddSeconds(Properties.TERRITORY_CLAIM_COOLDOWN_SECONDS) - DateTime.Now;
            }
        }

        public bool CanClaim
        {
            get => TimeBeforeClaim.Ticks > 0;
        }

        private sealed class LazyTerritory
        {
            string _territoryID;
            Territory? _territory;

            public LazyTerritory(string ID)
            {
                _territoryID = ID;
            }

            public Territory Get()
            {
                if (_territory != null)
                    return _territory;

                _territory = TerritoryManager.GetTerritoryByID(_territoryID);
                return _territory;
            }
        }

        /// <summary>
        /// Condition for capture. Can have parameters, see ParseParameters
        /// </summary>
        public enum eCaptureCondition
        {
            None = 0, // No parameters
            MoneyBribe, // 1 parameter: amount of money
            BountyPointsBribe, // 1 parameter: amount of BPs
            ItemBribe, // 2 parameters: ItemTemplateID, (optional) amount
            QuestCompletion, // 2 parameters: QuestID, (optional) amount
            TerritoryOwned // 1 parameter: TerritoryID
        }

        public eCaptureCondition CaptureCondition
        {
            get;
            private set;
        }

        public object CaptureParam1
        {
            get;
            private set;
        }

        public object CaptureParam2
        {
            get;
            private set;
        }

        public override bool AddToWorld()
        {
            if (CaptureCondition == eCaptureCondition.TerritoryOwned)
            {
                ((LazyTerritory)CaptureParam1).Get();
            }
            return base.AddToWorld();
        }

        /// <summary>
        /// Parse the capture parameters from the DB.
        /// </summary>
        /// <param name="args">Parameters from the DB. [0] is mobID, [1] is condition, [2] is param1, [3] is param2</param>
        public void ParseParameters(string[] args)
        {
            void SetNoCondition()
            {
                CaptureCondition = eCaptureCondition.None;
                CaptureParam1 = null;
                CaptureParam2 = null;
            }

            void EnforceParamLength(int amount)
            {
                if (args.Length < amount)
                {
                    throw new ArgumentException($"Bad parameters -- expected {amount}, got {args.Length}");
                }
            }

            if (args.Length < 2)
            {
                SetNoCondition();
                return;
            }

            eCaptureCondition condition = (eCaptureCondition)Enum.Parse(typeof(eCaptureCondition), args[1]);
            if (condition == eCaptureCondition.None)
            {
                SetNoCondition();
                return;
            }

            switch (condition)
            {
                case eCaptureCondition.None:
                    SetNoCondition();
                    break;

                case eCaptureCondition.MoneyBribe:
                case eCaptureCondition.BountyPointsBribe:
                // Amount
                    {
                        EnforceParamLength(3);
                        long amount = long.Parse(args[2]);
                        CaptureCondition = condition;
                        CaptureParam1 = amount;
                    }
                    break;

                case eCaptureCondition.ItemBribe:
                    {
                        EnforceParamLength(3);
                        long amount = 1;
                        if (args.Length > 3)
                        {
                            amount = long.Parse(args[3]);
                        }
                        ItemTemplate tpl = DOLDB<ItemTemplate>.SelectObject(DB.Column("Id_nb").IsEqualTo(args[2]));
                        if (tpl == null)
                        {
                            log.Error($"ItemTemplate {args[2]} not found for TerritoryLord {Name} ({InternalID})");
                            SetNoCondition();
                            return;
                        }
                        CaptureCondition = condition;
                        CaptureParam1 = tpl;
                        CaptureParam2 = amount;
                    }
                    break;

                case eCaptureCondition.QuestCompletion:
                    {
                        EnforceParamLength(3);
                        ushort questID = ushort.Parse(args[2]);
                        long amount = 1;
                        if (args.Length > 3)
                        {
                            amount = long.Parse(args[3]);
                        }
                        DataQuestJson quest = DataQuestJsonMgr.GetQuest(questID);
                        if (quest == null)
                        {
                            log.Error($"DataQuestJson {args[2]} not found for TerritoryLord {Name} ({InternalID})");
                            SetNoCondition();
                            return;
                        }
                        CaptureCondition = condition;
                        CaptureParam1 = quest;
                        CaptureParam2 = amount;
                    }
                    break;

                case eCaptureCondition.TerritoryOwned:
                // String
                    {
                        EnforceParamLength(3);
                        CaptureCondition = condition;
                        // Because this is called while loading territories we cannot check here,
                        // as the territory requested might not be loaded yet. Instead we defer to AddToWorld.
                        // This is not ideal but the alternative is to change how territories are loaded
                        CaptureParam1 = new LazyTerritory(args[2]);
                    }
                    break;

                default:
                    log.Error("Unknown capture condition " + args[1]);
                    SetNoCondition();
                    return;
            }
        }

        private bool CanPlayerClaim(GamePlayer player)
        {
            try
            {
                switch (CaptureCondition)
                {
                    case eCaptureCondition.None:
                        return true;

                    case eCaptureCondition.MoneyBribe:
                    case eCaptureCondition.BountyPointsBribe:
                    case eCaptureCondition.ItemBribe:
                        return _playersAuthorized.Contains(player.InternalID);


                    case eCaptureCondition.QuestCompletion:
                        return player.HasFinishedQuest((DataQuestJson)CaptureParam1) >= ((long)CaptureParam2);

                    case eCaptureCondition.TerritoryOwned:
                        return ((LazyTerritory)CaptureParam1).Get()?.IsOwnedBy(player) == true;

                    default:
                        log.Warn($"TerritoryLord {Name} ({InternalID}) has unknown capture condition {CaptureCondition} and will always refuse players");
                        return false;
                }
            }
            catch (Exception ex)
            {
                log.Warn($"TerritoryLord {Name} ({InternalID}) has broken capture condition {CaptureCondition}:\n{ex}");
                return false;
            }
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            lock (_lockObject)
            {
                if (CurrentTerritory == null)
                {
                    log.Warn($"TerritoryLord {Name} ({InternalID}) is not part of a territory");
                    if (player.Client.Account.PrivLevel > 1)
                    {
                        Whisper(player, "(GM) I am not part of a territory!");
                    }
                    return false;
                }

                if (player is not { Guild: { GuildType: not Guild.eGuildType.ServerGuild } })
                {
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.NoGuild");
                    return true;
                }

                if (CurrentTerritory.OwnerGuild != player.Guild)
                {
                    if (CurrentTerritory.OwnerGuild != null)
                    {
                        TimeSpan cooldown = TimeBeforeClaim;
                        if (cooldown.Ticks > 0)
                        {
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.NotClaimable", CurrentTerritory.OwnerGuild.Name, LanguageMgr.TranslateTimeLong(player, cooldown)), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            return true;
                        }
                    }
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.Claimable"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                }
                else
                {
                    var renewAvailable = CurrentTerritory.RenewAvailableTime;
                    if (renewAvailable != null && renewAvailable <= DateTime.Now)
                    {
                        string timeStr = LanguageMgr.TranslateTimeLong(player, CurrentTerritory.ClaimedTime.Value.AddMinutes(CurrentTerritory.Expiration) - DateTime.Now);
                        player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.Renewable", eChatType.CT_System, eChatLoc.CL_PopupWindow, timeStr);
                    }
                    else
                    {
                        player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.Hello", eChatType.CT_System, eChatLoc.CL_PopupWindow, player.Name, player.Guild.Name);
                    }
                }
                return true;
            }
        }

        protected bool AskCondition(GamePlayer player)
        {
            switch (CaptureCondition)
            {
                case eCaptureCondition.None:
                    // This wouldn't happen normally
                    throw new InvalidOperationException("TerritoryLord.AskCondition called with no capture condition");

                case eCaptureCondition.MoneyBribe:
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.MoneyBribe", eChatType.CT_System, eChatLoc.CL_PopupWindow, (long)CaptureParam1);
                    return true;

                case eCaptureCondition.BountyPointsBribe:
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.BountyPoints", eChatType.CT_System, eChatLoc.CL_PopupWindow, (long)CaptureParam1);
                    return true;

                case eCaptureCondition.ItemBribe:
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.ItemBribe", eChatType.CT_System, eChatLoc.CL_PopupWindow, (long)CaptureParam2, ((ItemTemplate)CaptureParam1).Name);
                    return true;

                case eCaptureCondition.QuestCompletion:
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.QuestCondition", eChatType.CT_System, eChatLoc.CL_PopupWindow, ((DataQuestJson)CaptureParam1).Name);
                    return true;

                case eCaptureCondition.TerritoryOwned:
                    string name = ((LazyTerritory)CaptureParam1).Get()?.Name;
                    if (name == null)
                        throw new ArgumentException($"Unknown territory");
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.QuestCondition", eChatType.CT_System, eChatLoc.CL_PopupWindow, name);
                    return true;

                default:
                    // This wouldn't happen normally
                    throw new InvalidOperationException("TerritoryLord.AskCondition called with no capture condition");
            }
        }

        protected virtual bool AskToJoin(GamePlayer player, bool isSecondAsk)
        {
            if (player.InCombat)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.InCombat"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (CurrentTerritory.OwnerGuild != null && CurrentTerritory.OwnerGuild != player.Guild)
            {
                TimeSpan cooldown = TimeBeforeClaim;
                if (cooldown.Ticks > 0)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.NotClaimable", CurrentTerritory.OwnerGuild.Name, LanguageMgr.TranslateTimeLong(player, cooldown)), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }
            }

            if (!CanPlayerClaim(player))
            {
                if (isSecondAsk)
                {
                    Whisper(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.ConditionChanged"));
                    return true;
                }
                return AskCondition(player);
            }

            if (_claimTimer != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.Occupied"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            int ticks = 0;
            _claimTimer = new RegionTimer(
                this,
                timer =>
                {
                    lock (_lockObject)
                    {
                        ticks += 500;
                        if (player.InCombat)
                        {
                            _claimTimer = null;
                            player.Out.SendCloseTimerWindow();
                            Whisper(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.Interrupted"));
                            return 0;
                        }
                        if (player.GetDistanceTo(this) > WorldMgr.GIVE_ITEM_DISTANCE)
                        {
                            _claimTimer = null;
                            player.Out.SendCloseTimerWindow();
                            Whisper(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.TooFar"));
                            return 0;
                        }
                        if (!CanPlayerClaim(player))
                        {
                            _claimTimer = null;
                            player.Out.SendCloseTimerWindow();
                            Whisper(player, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.ConditionChanged"));
                            return 0;
                        }
                        if (ticks < Properties.TERRITORY_CLAIM_TIMER_SECONDS * 1000)
                            return 500;

                        player.Out.SendCloseTimerWindow();
                        TakeControl(player);
                        _claimTimer = null;
                        return 0;
                    }
                },
                500
            );

            player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Capture.Timer"), Properties.TERRITORY_CLAIM_TIMER_SECONDS);

            foreach (GamePlayer pl in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>())
            {
                pl.SendTranslatedMessage("GameUtils.Guild.Territory.Capture.Start", eChatType.CT_Important, eChatLoc.CL_SystemWindow, player.GuildName, CurrentTerritory.Name);
            }
            return true;
        }

        protected bool TakeMoney(GamePlayer player)
        {
            if (CaptureCondition != eCaptureCondition.MoneyBribe)
            {
                player.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.ConditionChanged"), eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                return true;
            }
            player.SendMessage("This is where I take your money! (not implemented)", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.MoneyBribe.Accept", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            return true;
        }

        protected bool TakeItems(GamePlayer player)
        {
            if (CaptureCondition != eCaptureCondition.ItemBribe)
            {
                player.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.ConditionChanged"), eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                return true;
            }
            player.SendMessage("This is where I take your items! (not implemented)", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.ItemBribe.Accept", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            return true;
        }

        protected bool TakeBP(GamePlayer player)
        {
            if (CaptureCondition != eCaptureCondition.BountyPointsBribe)
            {
                player.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameUtils.Guild.Territory.Lord.ConditionChanged"), eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                return true;
            }
            player.SendMessage("This is where I take your BPs! (not implemented)", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.BountyPoints.Accept", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            var player = source as GamePlayer;

            if (!base.WhisperReceive(source, text) || player == null)
                return false;

            lock (_lockObject)
            {
                if (CurrentTerritory == null)
                    return false;

                if (player is not { Guild: { GuildType: not Guild.eGuildType.ServerGuild } })
                {
                    player.SendTranslatedMessage("GameUtils.Guild.Territory.Lord.NoGuild");
                    return true;
                }

                switch (text.ToLower())
                {
                    case "oui":
                    case "yes":
                        return AskToJoin(player, false);

                    case "alliance":
                        return AskToJoin(player, true);

                    case "pay":
                    case "payer":
                        return TakeMoney(player);

                    case "exchange":
                    case "échanger":
                        return TakeBP(player);

                    case "give":
                    case "donner":
                        return TakeItems(player);

                    case "no":
                    case "non":
                        return true;

                    default:
                        return false;
                }
            }
        }

        protected virtual void TakeControl(GamePlayer player)
        {
            if (CurrentTerritory == null || player is not { Guild: { GuildType: Guild.eGuildType.PlayerGuild } })
                return;

            lastClaim = DateTime.Now;
            if (CurrentTerritory.OwnerGuild != player.Guild)
            {
                player.Guild.SendMessageToGuildMembersKey("GameUtils.Guild.Territory.Capture.Captured", eChatType.CT_Guild, eChatLoc.CL_SystemWindow, player.Name, CurrentTerritory.Name);
                CurrentTerritory.OwnerGuild = player.Guild;

                foreach (GamePlayer pl in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>())
                {
                    pl.SendTranslatedMessage("GameUtils.Guild.Territory.Capture.Captured", eChatType.CT_Important, eChatLoc.CL_SystemWindow, player.GuildName, CurrentTerritory.Name);
                }
                NewsMgr.CreateNews("GameUtils.Guild.Territory.Capture.Captured", 0, eNewsType.PvE, false, true, player.GuildName, CurrentTerritory.Name);
            }
            else
            {
                player.Guild.SendMessageToGuildMembersKey("GameUtils.Guild.Territory.Capture.Renewed", eChatType.CT_Guild, eChatLoc.CL_SystemWindow, player.Name, CurrentTerritory.Name);
                CurrentTerritory.ClaimedTime = lastClaim;
                CurrentTerritory.SaveIntoDatabase();
            }
        }
    }
}