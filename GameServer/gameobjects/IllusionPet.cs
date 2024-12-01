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
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;
using DOL.GS.Scripts;
using System.Collections.Generic;

namespace DOL.GS
{
    public class IllusionPet : GamePet
    {
        [Flags]
        public enum eIllusionFlags
        {
            None = 0,
            RandomizePositions = 1 << 0,
        }
        
        public IllusionPet(GamePlayer owner, eIllusionFlags mode) : base(new IllusionPetBrain(owner, mode))
        {
            PlayerCloner.ClonePlayer(owner, this, false, false, false);
        }

        /// <inheritdoc />
        public override long ExperienceValue
        {
            get => 0;
        }

        /// <inheritdoc />
        public override int RealmPointsValue
        {
            get => 0;
        }

        public override void Die(GameObject killer)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(this, this, 7202, 0, false, 1);
            }

            base.Die(killer);
        }
    }
}