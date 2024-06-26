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
using System.Numerics;
using DOL.Events;
using DOL.GS.Geometry;
using DOL.Language;

namespace DOL.GS
{
    /// <summary>
    /// Interface for areas within game, extend this or AbstractArea if you need to define a new area shape that isn't already defined.
    /// Defined ones:
    /// - Area.Cricle
    /// - Area.Square
    /// </summary>
    public interface IArea : ITranslatableObject
    {
        /// <summary>
        /// Returns the ID of this zone
        /// </summary>
        ushort ID { get; set; }

        int RealmPoints { get; set; }

        bool IsPvP { get; set; }

        void UnRegisterPlayerEnter(DOLEventHandler callback);
        void UnRegisterPlayerLeave(DOLEventHandler callback);
        void RegisterPlayerEnter(DOLEventHandler callback);
        void RegisterPlayerLeave(DOLEventHandler callback);

        string GetDescriptionForPlayer(GamePlayer player);

        /// <summary>
        /// Checks wether is intersects with given zone.
        /// This is needed to build an area.zone mapping cache for performance.
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        bool IsIntersectingZone(Zone zone);

        /// <summary>
        /// Checks wether given spot is within areas range or not
        /// </summary>
        /// <param name="spot"></param>
        /// <returns></returns>
        bool IsContaining(Coordinate spot, bool ignoreZ = false);
        
        [Obsolete("Use .IsContaining(Coordinate[,bool]) instead!")]
        bool IsContaining(int x, int y, int z);
        
        [Obsolete("Use .IsContaining(Coordinate[,bool]) instead!")]
        bool IsContaining(int x, int y, int z, bool checkZ);

        float DistanceSquared(Coordinate position, bool checkZ);

        /// <summary>
        /// Called whenever a player leaves the given area
        /// </summary>
        /// <param name="player"></param>
        void OnPlayerLeave(GamePlayer player);

        /// <summary>
        /// Called whenever a player enters the given area
        /// </summary>
        /// <param name="player"></param>
        void OnPlayerEnter(GamePlayer player);
    }
}
