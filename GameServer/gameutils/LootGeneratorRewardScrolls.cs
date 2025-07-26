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
    public class LootGeneratorRewardScrolls : LootGeneratorBase
    {
        private static readonly List<string> _rewardscrollsTemplateIds = new List<string> { "arthur_parch_spelltest35", "arthur_parch_spelltest36", "arthur_parch_spelltest37", "arthur_parch_spelltest38", "arthur_parch_spelltest39", "arthur_parch_spelltest40", "arthur_parch_spelltest41", "arthur_parch_spelltest42", "arthur_parch_spelltest43", "arthur_parch_spelltest44", "arthur_parch_spelltest45" };
        private static readonly Dictionary<string, float> _rewardscrollsValue = new Dictionary<string, float> {
            { "arthur_parch_spelltest35", 1},
            { "arthur_parch_spelltest36", 1},
            { "arthur_parch_spelltest37", 1},
            { "arthur_parch_spelltest38", 1},
            { "arthur_parch_spelltest39", 1},
            { "arthur_parch_spelltest40", 1},
            { "arthur_parch_spelltest41", 1},
            { "arthur_parch_spelltest42", 1},
            { "arthur_parch_spelltest43", 1},
            { "arthur_parch_spelltest44", 1},
            { "arthur_parch_spelltest45", 1},
        };
        private readonly ItemTemplate[] _rewardscrolls;

        public LootGeneratorRewardScrolls() : base()
        {
            this._rewardscrolls = GameServer.Database.SelectObjects<ItemTemplate>(it => _rewardscrollsTemplateIds.Contains(it.Id_nb)).ToArray();
        }
        private int Chance(int mobLevel, string gem)
        {
            if (!_rewardscrollsValue.TryGetValue(gem, out float value))
                return 0;
            return (int)(MathF.Sin((mobLevel + (50 - value)) / 45 * ((50 - value) / 50)) * 1000);
        }

        public override LootList GenerateLoot(GameObject mob, GameObject killer)
        {
            LootList loot = base.GenerateLoot(mob, killer);
            if (Util.Chance(100))
                return loot;

            var gems = new LootList(1);
            foreach (var item in _rewardscrolls)
                gems.AddRandom(Chance(mob.Level, item.Id_nb), item, 1);

            foreach (var item in gems.GetLoot())
                loot.AddFixed(item, 1);
            return loot;
        }
    }
}
