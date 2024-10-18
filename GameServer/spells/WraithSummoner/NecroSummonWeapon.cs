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
*/

using System;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using log4net;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell handler for summoning the Nethersbane weapon.
    /// </summary>
    [SpellHandler("SummonNethersbane")]
    public class SummonNethersbane : SummonItemSpellHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SummonNethersbane));

        private ItemTemplate _nethersbaneTemplate;

        public SummonNethersbane(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            _nethersbaneTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>("Necro_Nethersbane") ?? CreateNethersbaneTemplate();
            items.Add(GameInventoryItem.Create(_nethersbaneTemplate));
        }

        private ItemTemplate CreateNethersbaneTemplate()
        {
            if (log.IsWarnEnabled) log.Warn("Could not find Necro_Nethersbane, creating it...");

            var template = new ItemTemplate
            {
                Id_nb = "Necro_Nethersbane",
                Name = "Nethersbane",
                Level = 50,
                Durability = 50000,
                MaxDurability = 50000,
                Condition = 50000,
                MaxCondition = 50000,
                Quality = 100,
                DPS_AF = 165,
                SPD_ABS = 60,
                Hand = 1,
                Type_Damage = 11,
                Object_Type = 6, // Assuming 6 corresponds to the correct object type
                Item_Type = 12,
                Color = 87,
                Effect = 26,
                Weight = 45,
                Model = 6,
                IsPickable = false,
                IsDropable = false,
                CanDropAsLoot = false,
                IsTradable = false,
                MaxCount = 1,
                PackSize = 1,
                Charges = 0,
                MaxCharges = 0,
                Charges1 = 0,
                MaxCharges1 = 0,
                SpellID = 0,
                SpellID1 = 0,
                ProcSpellID = 25131,
                ProcSpellID1 = 25133,
                ProcChance = 25,
            };

            GameServer.Database.AddObject(template);

            return template;
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            base.OnDirectEffect(target, effectiveness);

            if (Caster is GamePlayer player)
            {
                GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, OnPlayerLeft);
                GameEventMgr.AddHandler(player, GamePlayerEvent.Linkdeath, OnPlayerLeft);
                GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, OnPlayerLeft);

                if (log.IsDebugEnabled)
                    log.Debug($"Event handlers added for player {player.Name}");

                MessageToCaster(LanguageMgr.GetTranslation(player.Client, "Spells.NecroSumonWeap.Nethersbane.You"), eChatType.CT_Spell);
                SendEffectAnimation(player);

                foreach (GamePlayer nearbyPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (nearbyPlayer != player)
                    {
                        nearbyPlayer.Out.SendMessage(LanguageMgr.GetTranslation(nearbyPlayer.Client, "Spells.NecroSumonWeap.Nethersbane.Target", nearbyPlayer.GetPersonalizedName(player)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }

        private void SendEffectAnimation(GamePlayer player)
        {
            foreach (GamePlayer nearbyPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                nearbyPlayer.Out.SendSpellEffectAnimation(player, player, Spell.ClientEffect, 0, false, 1);
            }
        }

        private void OnPlayerLeft(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is not GamePlayer player)
                return;

            if (log.IsDebugEnabled)
                log.Debug($"OnPlayerLeft called for player {player.Name}");

            var items = player.Inventory.AllItems;
            foreach (InventoryItem invItem in items.ToList())
            {
                if (invItem.Id_nb.Equals("Necro_Nethersbane", StringComparison.OrdinalIgnoreCase))
                {
                    player.Inventory.RemoveItem(invItem);
                    if (log.IsDebugEnabled)
                        log.Debug($"Removed Necro_Nethersbane from {player.Name}'s inventory.");
                }
            }

            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, OnPlayerLeft);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Linkdeath, OnPlayerLeft);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.RegionChanged, OnPlayerLeft);
        }
    }

    /// <summary>
    /// Spell handler for summoning the Icebrand weapon.
    /// </summary>
    [SpellHandler("SummonIcebrand")]
    public class SummonIcebrand : SummonItemSpellHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SummonIcebrand));

        private ItemTemplate _icebrandTemplate;

        public SummonIcebrand(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            _icebrandTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>("Necro_Icebrand") ?? CreateIcebrandTemplate();
            items.Add(GameInventoryItem.Create(_icebrandTemplate));
        }

        private ItemTemplate CreateIcebrandTemplate()
        {
            if (log.IsWarnEnabled) log.Warn("Could not find Necro_Icebrand, creating it...");

            var template = new ItemTemplate
            {
                Id_nb = "Necro_Icebrand",
                Name = "Icebrand",
                Level = 50,
                Durability = 50000,
                MaxDurability = 50000,
                Condition = 50000,
                MaxCondition = 50000,
                Quality = 100,
                DPS_AF = 165,
                SPD_ABS = 57,
                Hand = 1,
                Type_Damage = 11,
                Object_Type = 6, // Assuming 6 corresponds to the correct object type
                Item_Type = 12,
                Color = 79,
                Effect = 27,
                Weight = 42,
                Model = 1981,
                IsPickable = false,
                IsDropable = false,
                CanDropAsLoot = false,
                IsTradable = false,
                MaxCount = 1,
                PackSize = 1,
                Charges = 10,
                MaxCharges = 0,
                Charges1 = 0,
                MaxCharges1 = 0,
                SpellID = 25135,
                SpellID1 = 0,
                ProcSpellID = 25134,
                ProcChance = 25,
            };

            GameServer.Database.AddObject(template);

            return template;
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            base.OnDirectEffect(target, effectiveness);

            if (Caster is GamePlayer player)
            {
                GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, OnPlayerLeft);
                GameEventMgr.AddHandler(player, GamePlayerEvent.Linkdeath, OnPlayerLeft);
                GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, OnPlayerLeft);

                if (log.IsDebugEnabled)
                    log.Debug($"Event handlers added for player {player.Name}");

                MessageToCaster(LanguageMgr.GetTranslation(player.Client, "Spells.NecroSumonWeap.Icebrand.You"), eChatType.CT_Spell);
                SendEffectAnimation(player);

                foreach (GamePlayer nearbyPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (nearbyPlayer != player)
                    {
                        nearbyPlayer.Out.SendMessage(LanguageMgr.GetTranslation(nearbyPlayer.Client, "Spells.NecroSumonWeap.Icebrand.Target", nearbyPlayer.GetPersonalizedName(player)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }
        }

        private void SendEffectAnimation(GamePlayer player)
        {
            foreach (GamePlayer nearbyPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                nearbyPlayer.Out.SendSpellEffectAnimation(player, player, Spell.ClientEffect, 0, false, 1);
            }
        }

        private void OnPlayerLeft(DOLEvent e, object sender, EventArgs arguments)
        {
            if (sender is not GamePlayer player)
                return;

            if (log.IsDebugEnabled)
                log.Debug($"OnPlayerLeft called for player {player.Name}");

            var items = player.Inventory.AllItems;
            foreach (InventoryItem invItem in items.ToList())
            {
                if (invItem.Id_nb.Equals("Necro_Icebrand", StringComparison.OrdinalIgnoreCase))
                {
                    player.Inventory.RemoveItem(invItem);
                    if (log.IsDebugEnabled)
                        log.Debug($"Removed Necro_Icebrand from {player.Name}'s inventory.");
                }
            }

            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, OnPlayerLeft);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Linkdeath, OnPlayerLeft);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.RegionChanged, OnPlayerLeft);
        }
    }
}