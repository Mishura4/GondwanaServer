using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Finance;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Scripts
{
    public class PrisonGardian : GameNPC
    {
        public static bool Activate = true;

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            if (Activate)
            {
                if (player.Client.Account.PrivLevel >= (int)ePrivLevel.GM)
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.GMMenuDeactivate"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

                var objs = from p in JailMgr.PlayerXPrisoner
                           where p.Value.RP
                           select p;
                if (objs.Count() > 0)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.FreePrisonersPart1"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.FreePrisonersPart2"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

                    int nb_prisoners = 0;
                    foreach (KeyValuePair<GamePlayer, Prisoner> kp in objs)
                    {
                        string textePrisonier = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.PrisonerList", kp.Key.Name, kp.Value.Cost);
                        player.Out.SendMessage(textePrisonier, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        nb_prisoners++;
                    }
                    if (nb_prisoners == 0)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.NoPrisonersCurrently"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                }
                else
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.NoPrisoners"), eChatType.CT_System, eChatLoc.CL_PopupWindow);

            }
            else
            {
                if (player.Client.Account.PrivLevel >= (int)ePrivLevel.GM)
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.GMMenuActivate"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.WantToEnter"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            GamePlayer player = source as GamePlayer;
            if (!base.WhisperReceive(source, text) || player == null)
                return false;

            if (player.Client.Account.PrivLevel >= (int)ePrivLevel.GM)
            {
                switch (text)
                {
                    case "Activation Gardien":
                        Activate = true;
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.GuardianActive"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return true;
                    case "Désactivation Gardien":
                    case "Desactivation Gardien":
                        Activate = false;
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.GuardianInactive"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        return true;
                }
            }

            GamePlayer gameprisoner = WorldMgr.GetClientByPlayerName(text, true, true).Player;
            if (gameprisoner == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.PrisonerNotFound"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            Prisoner prisoner = JailMgr.GetPrisoner(gameprisoner);
            if (prisoner == null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.PrisonerNotFound"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            int PrixTotal = prisoner.Cost;
            if (player.Client.Account.PrivLevel == 1 && !player.RemoveMoney(Currency.Copper.Mint(PrixTotal * 10000)))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.NotEnoughMoney"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            JailMgr.Relacher(gameprisoner);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameJail.PrisonGardian.PrisonerReleased", text, PrixTotal), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }
    }
}