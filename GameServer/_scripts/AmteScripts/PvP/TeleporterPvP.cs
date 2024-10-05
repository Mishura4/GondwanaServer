using AmteScripts.Managers;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Spells;

namespace DOL.GS.Scripts
{
    public class TeleporterPvP : GameNPC
    {
        private bool _isBusy;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            SetOwnBrain(new BlankBrain());
            return true;
        }

        private bool _BaseSay(GamePlayer player, string str = "Partir")
        {
            if (_isBusy)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.Busy"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            TurnTo(player);

            if (SpellHandler.FindEffectOnTarget(player, "Damnation") != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterRvR.DamnationRefusal1", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterRvR.DamnationRefusal2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (!PvpManager.Instance.IsIn(player) &&
                (!PvpManager.Instance.IsOpen || player.Level < 20 || (str != "Pret" && str != "Prêt" && str != "Partir")))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.CannotHelpPart1", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.CannotHelpPart2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            return false;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (!PvpManager.Instance.IsOpen && PvpManager.Instance.IsPvPRegion(player.CurrentRegionID))
            {
                _Teleport(player);
                return true;
            }

            if (_BaseSay(player)) return true;

            if (PvpManager.Instance.IsIn(player))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.ChickenOutPart1"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.ChickenOutPart2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            else
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.SendToCombat", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str) || !(source is GamePlayer)) return false;
            GamePlayer player = source as GamePlayer;

            if (_BaseSay(player, str)) return true;

            _Teleport(player);
            return true;
        }

        private void _Teleport(GamePlayer player)
        {
            _isBusy = true;
            RegionTimer TimerTL = new RegionTimer(this, _Teleportation);
            TimerTL.Properties.setProperty("player", player);
            TimerTL.Start(2000);
            foreach (GamePlayer players in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                players.Out.SendSpellCastAnimation(this, 1, 20);
                players.Out.SendEmoteAnimation(player, eEmote.Bind);
            }
        }

        private int _Teleportation(RegionTimer timer)
        {
            _isBusy = false;
            GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);
            if (player == null) return 0;
            if (player.InCombat)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TeleporterPvP.InCombat"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            else
            {
                if (!PvpManager.Instance.IsOpen || PvpManager.Instance.IsIn(player))
                    PvpManager.Instance.RemovePlayer(player);
                else
                    PvpManager.Instance.AddPlayer(player);
            }
            return 0;
        }
    }
}