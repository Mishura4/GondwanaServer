using DOL.AI.Brain;
using System;
using System.Reflection;
using System.Collections;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Database;
using log4net;
using DOL.GS.Effects;
using DOL.GS.ServerProperties;
using System.Linq;

namespace DOL.GS
{
    public abstract class AbstractBanner
    {
        public virtual GamePlayer OwningPlayer
        {
            get;
            set;
        }

        protected GamePlayer m_carryingPlayer;

        public virtual GamePlayer CarryingPlayer
        {
            get => m_carryingPlayer;
            protected set
            {
                if (value == m_carryingPlayer)
                    return;

                if (m_carryingPlayer != null)
                    m_carryingPlayer.ActiveBanner = null;
                m_carryingPlayer = value;
                if (m_carryingPlayer != null)
                    m_carryingPlayer.ActiveBanner = this;
            }
        }
        
        public virtual int Emblem => OwningPlayer?.Guild?.Emblem ?? 0;

        /// <summary>
        /// Drop the banner.
        /// </summary>
        /// <param name="forced">Whether this is an involuntary drop (true) or a voluntary drop (false).</param>
        public virtual void Drop(bool forced)
        {
            CarryingPlayer = null;
        }
    }
    
    public class GuildBanner : AbstractBanner
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        /// <summary>
        /// Tick timer
        /// </summary>
        private RegionTimer m_timer;

        /// <summary>
        /// Banner expiration timer
        /// </summary>
        private RegionTimer m_expireTimer;

        private GuildBannerItem m_item;

        public GuildBanner(GamePlayer player)
        {
            Guild = player.Guild;
            OwningPlayer = player;
            if (Properties.GUILD_BANNER_DURATION > 0)
            {
                m_expireTimer = new RegionTimer(OwningPlayer, new RegionTimerCallback(BannerExpireCallback), Properties.GUILD_BANNER_DURATION * 1000);
            }
        }

        protected GamePlayer m_owningPlayer;

        /// <summary>
        /// Player who summoned or recovered the banner
        /// </summary>
        public override GamePlayer OwningPlayer
        {
            get => m_owningPlayer;
            set
            {
                if (CarryingPlayer != null)
                {
                    throw new InvalidOperationException("Guild banner is already started");
                }
                m_owningPlayer = value;
                if (value != null)
                {
                    Start();
                }
                else
                {
                    if (m_timer != null)
                    {
                        m_timer.Stop();
                        m_timer = null;
                    }
                }
            }
        }

        public Guild Guild { get; set; }

        /// <inheritdoc />
        public override int Emblem => Guild?.Emblem ?? 0;

        public string BannerEffectType { get; set; }

        public GuildBannerItem BannerItem
        {
            get { return m_item; }
        }

        protected bool Start()
        {
            if (OwningPlayer == null)
            {
                if (m_timer != null)
                {
                    m_timer.Stop();
                    m_timer = null;
                }
                return false;
            }

            if (OwningPlayer.Group != null)
            {
                foreach (GamePlayer groupPlayer in OwningPlayer.Group.GetPlayersInTheGroup())
                {
                    if (groupPlayer.ActiveBanner != null)
                    {
                        OwningPlayer.SendTranslatedMessage("GameUtils.Guild.Banner.BannerInGroup", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                        if (m_timer != null)
                        {
                            m_timer.Stop();
                            m_timer = null;
                        }
                        return false;
                    }
                }
            }
            else if (!Properties.GUILD_BANNER_ALLOW_SOLO && OwningPlayer.Client.Account.PrivLevel <= (int)ePrivLevel.Player)
            {
                OwningPlayer.SendTranslatedMessage("GameUtils.Guild.Banner.BannerNoGroup", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                if (m_timer != null)
                {
                    m_timer.Stop();
                    m_timer = null;
                }
                return false;
            }

            if (m_item == null)
            {
                GuildBannerItem item = new GuildBannerItem(GuildBannerTemplate);

                item.Banner = this;
                m_item = item;
            }

            CarryingPlayer = OwningPlayer;
            CarryingPlayer.Stealth(false);
            AddHandlers();

            if (m_timer != null)
            {
                m_timer.Stop();
            }

            m_timer = new RegionTimer(CarryingPlayer, new RegionTimerCallback(TimerTick));
            m_timer.Start(1);
            return true;
        }

        public void Stop()
        {
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer = null;
            }
            if (CarryingPlayer != null)
            {
                RemoveHandlers();
                CarryingPlayer = null;
            }
        }

        private void ApplyBannerEffect(GamePlayer player)
        {
            GuildBannerEffect effect = GuildBannerEffect.CreateEffectOfClass(CarryingPlayer, player);

            if (effect != null)
            {
                IGameEffect oldEffect = player.EffectList.GetOfType(effect.GetType());
                (oldEffect as GuildBannerEffect)?.Stop();
                effect.Start(player);
            }
        }

        private int TimerTick(RegionTimer timer)
        {
            if (CarryingPlayer != null)
            {
                if (CarryingPlayer.Group == null)
                {
                    if (!Properties.GUILD_BANNER_ALLOW_SOLO)
                        return 9000;
                }
                else
                {
                    foreach (GamePlayer player in CarryingPlayer.Group.GetPlayersInTheGroup().Where(p => p.Guild != Guild && p is { ObjectState: GameObject.eObjectState.Active, IsAlive: true } && p.GetDistanceSquaredTo(CarryingPlayer) < 1500 * 1500))
                    {
                        ApplyBannerEffect(player);
                    }
                }
                foreach (GamePlayer player in Guild.GetListOfOnlineMembers().Where(p => p is { ObjectState: GameObject.eObjectState.Active, IsAlive: true } && p.GetDistanceSquaredTo(CarryingPlayer) < 1500 * 1500))
                {
                    ApplyBannerEffect(player);
                }
            }
            return 9000; // Pulsing every 9 seconds with a duration of 9 seconds - Tolakram
        }

        protected virtual void AddHandlers()
        {
            GameEventMgr.AddHandler(CarryingPlayer, GamePlayerEvent.LeaveGroup, new DOLEventHandler(PlayerLeaveGroup));
            GameEventMgr.AddHandler(CarryingPlayer, GroupEvent.MemberJoined, new DOLEventHandler(PlayerJoinGroup));
            GameEventMgr.AddHandler(CarryingPlayer, GamePlayerEvent.Quit, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.AddHandler(CarryingPlayer, GamePlayerEvent.StealthStateChanged, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.AddHandler(CarryingPlayer, GamePlayerEvent.Linkdeath, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.AddHandler(CarryingPlayer, GamePlayerEvent.RegionChanging, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.AddHandler(CarryingPlayer, GamePlayerEvent.Dying, new DOLEventHandler(PlayerDied));
        }

        protected virtual void RemoveHandlers()
        {
            GameEventMgr.RemoveHandler(CarryingPlayer, GamePlayerEvent.LeaveGroup, new DOLEventHandler(PlayerLeaveGroup));
            GameEventMgr.RemoveHandler(CarryingPlayer, GroupEvent.MemberJoined, new DOLEventHandler(PlayerJoinGroup));
            GameEventMgr.RemoveHandler(CarryingPlayer, GamePlayerEvent.Quit, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.RemoveHandler(CarryingPlayer, GamePlayerEvent.StealthStateChanged, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.RemoveHandler(CarryingPlayer, GamePlayerEvent.Linkdeath, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.RemoveHandler(CarryingPlayer, GamePlayerEvent.RegionChanging, new DOLEventHandler(PlayerPutAwayBanner));
            GameEventMgr.RemoveHandler(CarryingPlayer, GamePlayerEvent.Dying, new DOLEventHandler(PlayerDied));
        }

        protected void PlayerPutAwayBanner(DOLEvent e, object sender, EventArgs args)
        {
            Drop(true);
        }

        /// <inheritdoc />
        public override void Drop(bool forced)
        {
            if (forced && Guild != null)
            {
                Guild.ActiveGuildBanner = null;
                Guild.SendPlayerActionTranslationToGuildMembers(CarryingPlayer, "GameUtils.Guild.Banner.PutAway", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
            }
            Stop();
        }

        protected int LeaveGroupCallback(RegionTimer timer)
        {
            timer.Stop();
            m_expireTimer = null;
            if (!Properties.GUILD_BANNER_ALLOW_SOLO)
            {
                CarryingPlayer?.SendTranslatedMessage("GameUtils.Guild.Banner.BannerNoGroup", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                PutAway();
            }
            return 0;
        }

        protected int BannerExpireCallback(RegionTimer timer)
        {
            timer.Stop();
            m_expireTimer = null;
            PutAway();
            return 0;
        }

        protected void PutAway(bool notifyGroup = true)
        {
            if (CarryingPlayer != null)
            {
                Guild.SendPlayerActionTranslationToGuildMembers(CarryingPlayer, "GameUtils.Guild.Banner.PutAway", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                if (notifyGroup)
                {
                    CarryingPlayer.Group?.SendPlayerActionTranslationToGroupMembers(CarryingPlayer, "GameUtils.Guild.Banner.PutAway.OtherGuild", eChatType.CT_Group, eChatLoc.CL_SystemWindow, Guild.Name);
                }
                CarryingPlayer = null;
            }
            Stop();
            Guild.ActiveGuildBanner = null;
        }

        protected void PlayerLeaveGroup(DOLEvent e, object sender, EventArgs args)
        {
            if (!Properties.GUILD_BANNER_ALLOW_SOLO)
            {
                CarryingPlayer.SendTranslatedMessage("GameUtils.Guild.Banner.LeavesGroup", eChatType.CT_Group, eChatLoc.CL_SystemWindow, Guild.Name);
                (args as LeaveGroupEventArgs)?.Group.SendTranslatedMessageToGroupMembers("GameUtils.Guild.Banner.LeavesGroup", eChatType.CT_Group, eChatLoc.CL_SystemWindow, Guild.Name);
                m_expireTimer?.Stop();
                m_expireTimer = new RegionTimer(CarryingPlayer, LeaveGroupCallback, 30000);
            }
            else
            {
                (args as LeaveGroupEventArgs)?.Group.SendTranslatedMessageToGroupMembers("GameUtils.Guild.Banner.LeavesGroup", eChatType.CT_Group, eChatLoc.CL_SystemWindow, Guild.Name);
            }
        }

        protected void PlayerJoinGroup(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer { Group: not null } player)
            {
                return;
            }

            if (player.Group.Leader == player)
            {
                foreach (GamePlayer groupPlayer in OwningPlayer.Group.GetPlayersInTheGroup())
                {
                    if (groupPlayer.ActiveBanner is GuildBanner gBanner)
                    {
                        groupPlayer.SendTranslatedMessage("GameUtils.Guild.Banner.BannerInGroup", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                        gBanner.PutAway(false);
                    }
                }
            }
            else
            {
                foreach (GamePlayer groupPlayer in OwningPlayer.Group.GetPlayersInTheGroup())
                {
                    if (groupPlayer.ActiveBanner != null)
                    {
                        player.SendTranslatedMessage("GameUtils.Guild.Banner.BannerInGroup", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                        PutAway();
                        break;
                    }
                }
            }
        }

        protected void PlayerDied(DOLEvent e, object sender, EventArgs args)
        {
            DyingEventArgs arg = args as DyingEventArgs;
            if (arg == null) return;

            m_item.OnPlayerKilled(arg.Killer);
        }

        protected ItemTemplate m_guildBannerTemplate;
        public ItemTemplate GuildBannerTemplate
        {
            get
            {
                if (m_guildBannerTemplate == null)
                {
                    string guildIDNB = "GuildBanner_" + Guild.GuildID;

                    m_guildBannerTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(guildIDNB) ?? new ItemTemplate();
                    m_guildBannerTemplate.CanDropAsLoot = false;
                    m_guildBannerTemplate.Id_nb = guildIDNB;
                    m_guildBannerTemplate.IsDropable = false;
                    m_guildBannerTemplate.IsPickable = true;
                    m_guildBannerTemplate.IsTradable = false;
                    m_guildBannerTemplate.IsIndestructible = true;
                    m_guildBannerTemplate.Item_Type = 41;
                    m_guildBannerTemplate.Level = 1;
                    m_guildBannerTemplate.MaxCharges = 1;
                    m_guildBannerTemplate.MaxCount = 1;
                    m_guildBannerTemplate.Emblem = Guild.Emblem;
                    switch (OwningPlayer.Realm)
                    {
                        case eRealm.Albion:
                            m_guildBannerTemplate.Model = 3223;
                            break;
                        case eRealm.Midgard:
                            m_guildBannerTemplate.Model = 3224;
                            break;
                        case eRealm.Hibernia:
                            m_guildBannerTemplate.Model = 3225;
                            break;
                    }
                    m_guildBannerTemplate.Name = Guild.Name + "'s Banner";
                    m_guildBannerTemplate.Object_Type = (int)eObjectType.HouseWallObject;
                    m_guildBannerTemplate.Realm = 0;
                    m_guildBannerTemplate.Quality = 100;
                    m_guildBannerTemplate.ClassType = "DOL.GS.GuildBannerItem";
                    m_guildBannerTemplate.PackageID = "GuildBanner";
                }

                return m_guildBannerTemplate;
            }
        }

    }
}



