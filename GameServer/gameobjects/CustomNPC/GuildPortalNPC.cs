using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Quests;
using DOL.GS.Scripts;
using log4net;
using DOL.GS.Spells;
using DOL.Territories;
using DOL.GS.Geometry;

namespace DOL.GS
{
    public class GuildPortalNPC : GameNPC
    {
        public Guild OwningGuild { get; init; }

        public Territories.Territory LinkedTerritory { get; init; }

        private readonly object m_coordinatesLockObject = new();

        private readonly Dictionary<GamePlayer, Position> m_savedCoordinates = new();

        private GuildPortalNPC(Territories.Territory territory, GamePlayer spawner) : base()
        {
            OwningGuild = spawner.Guild;
            LinkedTerritory = territory;
        }

        public static GuildPortalNPC Create(Territories.Territory territory, GamePlayer spawner)
        {
            GuildPortalNPC portalNpc = new GuildPortalNPC(territory, spawner);
            portalNpc.LoadedFromScript = true;
            portalNpc.Position = Position.Create(territory.RegionId, territory.PortalCoordinate.Value, Angle.Zero);
            portalNpc.CurrentRegionID = territory.RegionId;
            portalNpc.Heading = 1000;
            portalNpc.Model = 1438;
            portalNpc.Size = 70;
            portalNpc.Realm = 0;
            portalNpc.Name = "Guild Portal";

            portalNpc.Flags |= GameNPC.eFlags.PEACE;
            return portalNpc;
        }

        public void SummonPlayer(GamePlayer player)
        {
            foreach (GamePlayer pl in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                pl.Out.SendSpellEffectAnimation(player, player, 4310, 0, false, 1);
            lock (m_coordinatesLockObject)
            {
                m_savedCoordinates.TryAdd(player, player.Position);
            }
            player.MoveTo(CurrentRegionID, Position.X, Position.Y, Position.Z, Heading);
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.TerritoryPortal.Summoned", LinkedTerritory.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public override bool Interact(GamePlayer player)
        {
            Position returnCoordinates;

            lock (m_coordinatesLockObject)
            {
                if (!m_savedCoordinates.TryGetValue(player, out returnCoordinates))
                {
                    return false;
                }
            }
            foreach (GamePlayer pl in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                pl.Out.SendSpellEffectAnimation(player, player, 4310, 0, false, 1);
            player.MoveTo(returnCoordinates);
            return true;
        }
    }
}
