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
using DOL.Language;
using log4net;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell handler for the Golden Spear Javelin spell.
    /// </summary>
    [SpellHandler("GoldenSpearJavelin")]
    public class GoldenSpearJavelin : SummonItemSpellHandler
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private ItemTemplate _artefJavelin;

        public GoldenSpearJavelin(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            _artefJavelin = GameServer.Database.FindObjectByKey<ItemTemplate>("Artef_Javelin") ?? CreateJavelinTemplate();
            items.Add(GameInventoryItem.Create(_artefJavelin));
        }

        private ItemTemplate CreateJavelinTemplate()
        {
            if (_artefJavelin == null)
            {
                if (log.IsWarnEnabled) log.Warn("Could not find Artef_Javelin, loading it ...");
                _artefJavelin = new ItemTemplate();
                _artefJavelin.Id_nb = "Artef_Javelin";
                _artefJavelin.Name = "Golden Javelin";
                _artefJavelin.Level = 50;
                _artefJavelin.MaxDurability = 50000;
                _artefJavelin.MaxCondition = 50000;
                _artefJavelin.Quality = 100;
                _artefJavelin.Object_Type = (int)eObjectType.Magical;
                _artefJavelin.Item_Type = 41;
                _artefJavelin.Model = 23;
                _artefJavelin.IsPickable = false;
                _artefJavelin.IsDropable = false;
                _artefJavelin.CanDropAsLoot = false;
                _artefJavelin.IsTradable = false;
                _artefJavelin.MaxCount = 1;
                _artefJavelin.PackSize = 1;
                _artefJavelin.Charges = 5;
                _artefJavelin.MaxCharges = 5;
                _artefJavelin.SpellID = 38076;
            }
            return _artefJavelin;
        }

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (!base.OnDirectEffect(target, effectiveness))
                return false;

            if (Caster is GamePlayer player)
            {
                GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, OnPlayerLeft);
                GameEventMgr.AddHandler(player, GamePlayerEvent.Linkdeath, OnPlayerLeft);
                GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, OnPlayerLeft);

                if (log.IsDebugEnabled)
                    log.Debug($"Event handlers added for player {player.Name}");

                SendEffectAnimation(player);
                player.TempProperties.setProperty("GoldenSpearJavelinHandler", this);
            }
            return true;
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
            if (!(sender is GamePlayer player))
                return;

            if (log.IsDebugEnabled)
                log.Debug($"OnPlayerLeft called for player {player.Name}");

            var items = player.Inventory.AllItems;
            foreach (InventoryItem invItem in items.ToList())
            {
                if (invItem.Id_nb.Equals("Artef_Javelin", StringComparison.OrdinalIgnoreCase))
                {
                    player.Inventory.RemoveItem(invItem);
                    if (log.IsDebugEnabled)
                        log.Debug($"Removed Artef_Javelin from {player.Name}'s inventory.");
                }
            }

            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, OnPlayerLeft);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.Linkdeath, OnPlayerLeft);
            GameEventMgr.RemoveHandler(player, GamePlayerEvent.RegionChanged, OnPlayerLeft);
            player.TempProperties.removeProperty("GoldenSpearJavelinHandler");
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string description = LanguageMgr.GetTranslation(delveClient, "SpellDescription.GoldenSpearJavelin.MainDescription");

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return description + "\n\n" + secondDesc;
            }

            return description;
        }
    }
}