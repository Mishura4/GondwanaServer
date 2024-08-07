﻿using DOL.GameEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Language;

namespace DOL.GS.Commands
{

    [CmdAttribute(
        "&event",
        ePrivLevel.Player,
        "Liste les événements en cours")]
    public class Event :
        AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            ShowEvents(client);
        }

        private void ShowEvents(GameClient client)
        {
            client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.Info.WindowTitle"), GameEventManager.Instance.GetEventsInfos(true, false));
        }
    }
}