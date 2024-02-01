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
    public class LootGeneratorWood : LootGeneratorBase
    {
        private static readonly List<string> _wooden_boardTemplateIds = new List<string> { "rowan_wooden_boards", "elm_wooden_boards", "oak_wooden_boards", "ironwood_wooden_boards", "heartwood_wooden_boards", "runewood_wooden_boards", "stonewood_wooden_boards", "ebonwood_wooden_boards", "dyrwood_wooden_boards", "duskwood_wooden_boards" };
        private static readonly Dictionary<string, float> _wooden_boardValue = new Dictionary<string, float> {
            { "rowan_wooden_boards", 5},
            { "elm_wooden_boards", 8},
            { "oak_wooden_boards", 12},
            { "ironwood_wooden_boards", 20},
            { "heartwood_wooden_boards", 25},
            { "runewood_wooden_boards", 30},
            { "stonewood_wooden_boards", 35},
            { "ebonwood_wooden_boards", 38},
            { "dyrwood_wooden_boards", 42},
            { "duskwood_wooden_boards", 45},
        };
        private readonly ItemTemplate[] _wooden_boards;

        public LootGeneratorWood() : base()
        {
            this._wooden_boards = GameServer.Database.SelectObjects<ItemTemplate>(it => _wooden_boardTemplateIds.Contains(it.Id_nb)).ToArray();
        }
        private int Chance(int mobLevel, string wooden_board)
        {
            if (!_wooden_boardValue.TryGetValue(wooden_board, out float value))
                return 0;
            return (int)(MathF.Sin((mobLevel + (50 - value)) / 45 * ((50 - value) / 50)) * 1000);
        }

        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);
            if (Util.Chance(90))
                return loot;

            var wooden_boards = new LootList(1);
            foreach (var item in _wooden_boards)
                wooden_boards.AddRandom(Chance(mob.Level, item.Id_nb), item, 1);

            foreach (var item in wooden_boards.GetLoot())
                loot.AddFixed(item, 1);
            return loot;
        }
    }
}
