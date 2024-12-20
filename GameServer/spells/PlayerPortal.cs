/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 *///made by DeMAN
using System;
using System.Reflection;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.GS.Effects;
using DOL.Events;
using log4net;
using System.Numerics;
using DOL.Language;
using DOL.GS.ServerProperties;
using DOL.GS.Geometry;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("PlayerPortal")]
    public class PlayerPortal : SpellHandler
    {
        public PlayerPortal(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        private List<GamePlayer> PlayersWithAccess;
        private List<GamePlayer> PlayersUsedFirstPortal;
        private List<GamePlayer> PlayersUsedSecondPortal;
        private GameNPC firstPortal;
        private PlayerPortalNPC firstPortalNPC;
        private GameNPC secondPortal;
        private PlayerPortalNPC secondPortalNPC;
        private Group group;
        GameSpellEffect portalEffect;

        GameNPC CreatePortal(GameNPC portal, GamePlayer player)
        {
            portal = new GameNPC();
            portal.LoadedFromScript = false;
            portal.Position = player.Position;
            portal.CurrentRegion = player.CurrentRegion;
            portal.Heading = player.Heading;
            portal.Model = 1393;
            portal.Size = 50;
            portal.Realm = 0;

            //not selectable and peace flags
            portal.Flags |= GameNPC.eFlags.PEACE;
            portal.Flags |= GameNPC.eFlags.CANTTARGET;

            return portal;
        }
        PlayerPortalNPC CreatePortalNPC(PlayerPortalNPC portalNPC, GamePlayer player)
        {
            portalNPC = new PlayerPortalNPC();
            portalNPC.LoadedFromScript = false;
            portalNPC.Position = player.Position;
            portalNPC.CurrentRegion = player.CurrentRegion;
            portalNPC.Heading = player.Heading;
            portalNPC.Model = 928;
            portalNPC.Size = 12;
            portalNPC.Realm = 0;
            portalNPC.PortalSpell = this;

            portalNPC.Flags |= GameNPC.eFlags.PEACE;

            return portalNPC;
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            DeletePortals();
            GamePlayer player = effect.Owner as GamePlayer;

            PlayersWithAccess = new List<GamePlayer>();
            PlayersUsedFirstPortal = new List<GamePlayer>();
            PlayersUsedSecondPortal = new List<GamePlayer>();

            firstPortal = CreatePortal(firstPortal, player);
            firstPortalNPC = CreatePortalNPC(firstPortalNPC, player);

            secondPortal = CreatePortal(secondPortal, player);
            secondPortalNPC = CreatePortalNPC(secondPortalNPC, player);

            // set to player bind location
            secondPortal.Position = player!.BindPosition;
            secondPortalNPC.Position = player.BindPosition;

            if (player == null)
                return;
            String targetType = m_spell.Target.ToLower();

            if (targetType == "self")
            {
                PlayersWithAccess.Add(player);
                firstPortalNPC.Name = player.Name + "- Portal";
                secondPortalNPC.Name = player.Name + "- Portal";
            }
            else if (targetType == "group")
            {
                if (player.Group == null)
                    PlayersWithAccess.Add(player);
                else
                {
                    group = player.Group;
                    foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
                        PlayersWithAccess.Add(p);
                }

                firstPortalNPC.Name = player.Name + "- Group Portal";
                secondPortalNPC.Name = player.Name + "- Group Portal";
            }

            firstPortal.AddToWorld();
            firstPortalNPC.AddToWorld();
            secondPortal.AddToWorld();
            secondPortalNPC.AddToWorld();

            portalEffect = effect;
            base.OnEffectStart(effect);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            DeletePortals();
            return base.OnEffectExpires(effect, noMessages);
        }

        public void TryTeleport(PlayerPortalNPC portalUsed, GamePlayer player)
        {
            if (player == null)
                return;

            if (PlayersWithAccess.Contains(player) == false)
                return;

            if (portalUsed == firstPortalNPC)
            {
                if (PlayersUsedFirstPortal.Contains(player))
                    return;

                PlayersUsedFirstPortal.Add(player);
                ApplyTeleportEffect(player);
                Position secondPortalPos = Position.Create(secondPortal.CurrentRegionID, secondPortal.Position.X, secondPortal.Position.Y, secondPortal.Position.Z, secondPortal.Heading);
                player.MoveTo(secondPortalPos);
            }
            else if (portalUsed == secondPortalNPC)
            {
                if (PlayersUsedSecondPortal.Contains(player))
                    return;

                PlayersUsedSecondPortal.Add(player);
                ApplyTeleportEffect(player);
                Position firstPortalPos = Position.Create(firstPortal.CurrentRegionID, firstPortal.Position.X, firstPortal.Position.Y, firstPortal.Position.Z, firstPortal.Heading);
                player.MoveTo(firstPortalPos);
            }

            CheckFinished();
        }
        void ApplyTeleportEffect(GamePlayer player)
        {
            foreach (GamePlayer pl in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                pl.Out.SendSpellEffectAnimation(player, player, 4310, 0, false, 1);
        }

        public void CheckFinished()
        {
            if (PlayersUsedFirstPortal.Count == PlayersWithAccess.Count && PlayersUsedSecondPortal.Count == PlayersWithAccess.Count)
            {
                DeletePortals();
                portalEffect.Cancel(false);
            }
        }

        void DeletePortals()
        {
            if (firstPortal != null)
            {
                firstPortal.RemoveFromWorld();
                firstPortal.Delete();
            }

            if (secondPortal != null)
            {
                secondPortal.RemoveFromWorld();
                secondPortal.Delete();
            }

            if (firstPortalNPC != null)
            {
                firstPortalNPC.RemoveFromWorld();
                firstPortalNPC.Delete();
            }

            if (secondPortalNPC != null)
            {
                secondPortalNPC.RemoveFromWorld();
                secondPortalNPC.Delete();
            }
        }
        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            string spellTarget = LanguageMgr.GetTargetOfType(language, m_spell.Target.ToString());
            return LanguageMgr.GetTranslation(language, "SpellDescription.PlayerPortal.MainDescription", spellTarget);
        }
    }
}
