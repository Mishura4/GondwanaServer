using AmteScripts.Managers;
using DOL.Database;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;

namespace Amte
{
    public class LordRvR : GameNPC
    {
        public static int CLAIM_TIME_SECONDS = 30;
        public static int CLAIM_TIME_BETWEEN_SECONDS = 120;
        public string originalGuildName;

        public DateTime lastClaim = new DateTime(1);

        private RegionTimer _claimTimer;

        private RegionTimer _scoreTimer;
        private Dictionary<eRealm, TimeSpan> _scores = new Dictionary<eRealm, TimeSpan>
        {
            { eRealm.Albion, new TimeSpan(0) },
            { eRealm.Midgard, new TimeSpan(0) },
            { eRealm.Hibernia, new TimeSpan(0) },
        };


        public double timeBeforeClaim
        {
            get
            {
                return CLAIM_TIME_BETWEEN_SECONDS - (DateTime.Now - lastClaim).TotalSeconds;
            }
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (RvrManager.Instance.IsOpen)
            {
                if (timeBeforeClaim > 0)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Open.UnClaimble", Math.Round(timeBeforeClaim, 1)), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Open.Claimable"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            else
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Closed"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            this.originalGuildName = this.GuildName;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            var player = source as GamePlayer;
            if (!base.WhisperReceive(source, text) || player == null)
                return false;
            if (text != "prendre le contrôle" && text != "seize control")
                return true;
            if (player.InCombat)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.In.Combat"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            if (timeBeforeClaim > 0)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Open.UnClaimble", Math.Round(timeBeforeClaim, 1)), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            if (_claimTimer != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Occupied"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            var startTime = DateTime.Now;
            _claimTimer = new RegionTimer(
                this,
                timer =>
                {
                    if (player.InCombat)
                    {
                        _claimTimer = null;
                        player.Out.SendCloseTimerWindow();
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.Interrupt"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        return 0;
                    }
                    if (player.GetDistanceTo(this) > WorldMgr.GIVE_ITEM_DISTANCE)
                    {
                        _claimTimer = null;
                        player.Out.SendCloseTimerWindow();
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Too.Far"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        return 0;
                    }
                    var passedTime = DateTime.Now - startTime;
                    if (passedTime.TotalSeconds < CLAIM_TIME_SECONDS)
                        return 500;

                    player.Out.SendCloseTimerWindow();
                    TakeControl(player);
                    _claimTimer = null;
                    return 0;
                },
                500
            );

            player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.GamePlayer.RvR.Capture.Timer"), CLAIM_TIME_SECONDS);

            foreach (var obj in GetPlayersInRadius(ushort.MaxValue - 1))
            {
                var pl = obj as GamePlayer;
                if (pl != null)
                    pl.Out.SendMessage(LanguageMgr.GetTranslation(pl.Client.Account.Language, "GameObjects.GamePlayer.RvR.Capture.Start", player.GuildName), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }

            return true;
        }

        public override void RestoreOriginalGuildName()
        {
            this.GuildName = this.originalGuildName;
        }

        public virtual void TakeControl(GamePlayer player)
        {
            lastClaim = DateTime.Now;
            GuildName = player.GuildName;
            var rvr = RvrManager.Instance.GetRvRTerritory(this.CurrentRegionID);
            var defaultAreaName = string.Empty;
            if (rvr != null)
            {
                defaultAreaName = ((AbstractArea)rvr.Area).Description;
            }

            string fortName = string.IsNullOrEmpty(this.originalGuildName) ? defaultAreaName : this.originalGuildName;

            foreach (GameClient client in WorldMgr.GetAllPlayingClients())
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GameObjects.GamePlayer.RvR.Control", player.GuildName, fortName), eChatType.CT_Help, eChatLoc.CL_SystemWindow);
            }
            player.GainGuildMeritPoints(800);
            NewsMgr.CreateNews("GameObjects.GamePlayer.RvR.Control", 0, eNewsType.RvRGlobal, false, true, player.GuildName, fortName);

            RvrManager.Instance.OnControlChange(this.InternalID, player.Guild);
        }

        public virtual void StartRvR()
        {
            var lastTime = DateTime.Now;
            if (_scoreTimer != null)
                _scoreTimer.Stop();

            _scoreTimer = new RegionTimer(
                this,
                timer =>
                {
                    switch (GuildName)
                    {
                        case "Albion": _scores[eRealm.Albion] += (DateTime.Now - lastTime); break;
                        case "Midgard": _scores[eRealm.Midgard] += (DateTime.Now - lastTime); break;
                        case "Hibernia": _scores[eRealm.Hibernia] += (DateTime.Now - lastTime); break;
                    }
                    lastTime = DateTime.Now;
                    return 1000;
                },
                1000
            );
        }

        public virtual void StopRvR()
        {
            if (_scoreTimer != null)
                _scoreTimer.Stop();
            _scoreTimer = null;
        }

        public virtual string GetScores()
        {
            var str = " - Temps de détention du fort :\n";
            foreach (var kvp in _scores)
                str += GlobalConstants.RealmToName(kvp.Key) + ": " + Math.Round(kvp.Value.TotalSeconds, 1) + " secondes\n";
            return str;
        }
    }
}