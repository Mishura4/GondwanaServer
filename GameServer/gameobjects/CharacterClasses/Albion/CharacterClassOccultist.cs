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
using System.Collections.Generic;

using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS.Effects;
using DOL.AI.Brain;
using DOL.GS.PlayerClass;

namespace DOL.GS
{
    /// <summary>
    /// The occultist character class.
    /// </summary>
    public class CharacterClassOccultist : ClassDisciple
    {
        public static int ModTempParry(GamePlayer player, bool grant, int level)
        {
            if (grant)
            {
                if (player == null || player.HasSpecialization(Specs.Parry))
                    return 0;

                var parrySpec = SkillBase.GetSpecialization(Specs.Parry);
                if (parrySpec == null)
                    // Log?
                    return 0;

                int levelToGrant = Math.Max(1, level);

                parrySpec.Level = levelToGrant;
                parrySpec.AllowSave = false;
                parrySpec.Trainable = false;
                parrySpec.Hidden = true;
                player.AddSpecialization(parrySpec);

                player.Out.SendUpdatePlayerSkills();
                player.UpdatePlayerStatus();

                return level;
            }
            else
            {
                if (player == null || level == 0)
                    return 0;

                int ret = 0;
                var spec = player.GetSpecialization(Specs.Parry);
                if (spec is { Trainable: false, AllowSave: false } && spec.Level <= level)
                {
                    player.RemoveSpecialization(Specs.Parry);
                    ret = spec.Level;
                }

                player.Out.SendUpdatePlayerSkills();
                player.UpdatePlayerStatus();
                return ret;
            }
        }

    }
}
