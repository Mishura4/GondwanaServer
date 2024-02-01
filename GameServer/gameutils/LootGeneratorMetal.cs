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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>
    /// MoneyLootGenerator
    /// At the moment this generaotr only adds money to the loot
    /// </summary>
    public class LootGeneratorMetal : LootGeneratorBase
    {
        private static readonly List<string> _metal_barTemplateIds = new List<string> { "bronze_metal_bars", "copper_metal_bars", "iron_metal_bars", "iron_metal_bars", "steel_metal_bars", "quartz_metal_bars", "alloy_metal_bars", "dolomite_metal_bars", "fine_alloy_metal_bars", "cobalt_metal_bars", "mithril_metal_bars", "carbide_metal_bars", "adamantium_metal_bars", "sapphire_metal_bars", "asterite_metal_bars", "diamond_metal_bars", "netherium_metal_bars", "netherite_metal_bars", "arcanium_metal_bars", "arcanite_metal_bars" };
        private static readonly Dictionary<string, float> _metal_barValue = new Dictionary<string, float> {
            { "bronze_metal_bars", 5}, { "copper_metal_bars", 5},
            { "iron_metal_bars", 8}, { "iron_metal_bars", 8},
            { "steel_metal_bars", 12}, { "quartz_metal_bars", 12},
            { "alloy_metal_bars", 20}, { "dolomite_metal_bars", 20},
            { "fine_alloy_metal_bars", 25}, { "cobalt_metal_bars", 25},
            { "mithril_metal_bars", 30}, { "carbide_metal_bars", 30},
            { "adamantium_metal_bars", 35}, { "sapphire_metal_bars", 35},
            { "asterite_metal_bars", 38}, { "diamond_metal_bars", 38},
            { "netherium_metal_bars", 42}, { "netherite_metal_bars", 42},
            { "arcanium_metal_bars", 45}, { "arcanite_metal_bars", 45},
        };
        private readonly ItemTemplate[] _metal_bars;

        public LootGeneratorMetal() : base()
        {
            this._metal_bars = GameServer.Database.SelectObjects<ItemTemplate>(it => _metal_barTemplateIds.Contains(it.Id_nb)).ToArray();
        }
        private int Chance(int mobLevel, string metal_bar)
        {
            if (!_metal_barValue.TryGetValue(metal_bar, out float value))
                return 0;
            return (int)(MathF.Sin((mobLevel + (50 - value)) / 45 * ((50 - value) / 50)) * 1000);
        }

        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);
            if (Util.Chance(90))
                return loot;

            var metal_bars = new LootList(1);
            foreach (var item in _metal_bars)
                metal_bars.AddRandom(Chance(mob.Level, item.Id_nb), item, 1);

            foreach (var item in metal_bars.GetLoot())
                loot.AddFixed(item, 1);
            return loot;
        }
    }
}
