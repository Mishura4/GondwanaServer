// Dawn of Light::GameServer::CommandSource.cs
// Created 2023/11/16 by miuna for Gondwana

using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    public class CommandSource
    {
        public CommandSource(GameClient client)
        {
            Client = client;
        }

        public GameClient Client { get; }

        public bool IsClient() => Client != null;

        public GamePlayer GetPlayer() => Client?.Player;

        public string GetName() => Client != null ? Client.Player.Name : "[SERVER]";

        public void SendMessage(string message, eChatType type = eChatType.CT_System,
            eChatLoc loc = eChatLoc.CL_SystemWindow)
        {
            if (Client != null)
            {
                Client.Out.SendMessage(message, type, loc);
            }
            else
            {
                GameServer.Instance.Logger.Info(message);
            }
        }
    }
}