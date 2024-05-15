using AmteScripts.Managers;
using DOL.Database;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.Territories
{
    public class TerritoryLord : GameNPC
    {
        public DateTime lastClaim = new DateTime(1);

        private RegionTimer _claimTimer;


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

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (CurrentTerritory == null)
            {
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

        public override bool WhisperReceive(GameLiving source, string text)
        {
            var player = source as GamePlayer;
            if (!base.WhisperReceive(source, text) || player == null)
                return false;
            if (CurrentTerritory == null)
                return false;
            if (player is not { Guild: { GuildType: not Guild.eGuildType.ServerGuild } })
            {
                return false;
            }

            if (text is not "oui" or "non" or "yes" or "no")
                return true;

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
                    if (ticks < Properties.TERRITORY_CLAIM_TIMER_SECONDS * 1000)
                        return 500;

                    player.Out.SendCloseTimerWindow();
                    TakeControl(player);
                    _claimTimer = null;
                    return 0;
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

        protected virtual void TakeControl(GamePlayer player)
        {
            if (CurrentTerritory == null || player is not { Guild: { GuildType: Guild.eGuildType.PlayerGuild } })
                return;

            lastClaim = DateTime.Now;
            if (CurrentTerritory.OwnerGuild != player.Guild)
            {
                CurrentTerritory.OwnerGuild = player.Guild;

                foreach (GamePlayer pl in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>())
                {
                    pl.SendTranslatedMessage("GameUtils.Guild.Territory.Capture.Captured", eChatType.CT_Important, eChatLoc.CL_SystemWindow, player.GuildName, CurrentTerritory.Name);
                }
                player.Guild.SendMessageToGuildMembersKey("GameUtils.Guild.Territory.Capture.Captured", eChatType.CT_Guild, eChatLoc.CL_SystemWindow, player.Name, CurrentTerritory.Name);
                player.GainGuildMeritPoints(800);
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