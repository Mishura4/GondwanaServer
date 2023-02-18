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
using DOL.GS;
namespace DOL.Events
{
    /// <summary>
    /// Holds the arguments for the EnemyKilled event of GameLivings
    /// </summary>
    public class BringAFriendArgs : EventArgs
    {

        /// <summary>
        /// has the target entered or left the zone
        /// </summary>
        private readonly bool m_entered;

        /// <summary>
        /// the friendly mob
        /// </summary>
        private readonly GameLiving m_friend;
        /// <summary>
        /// has the target started following
        /// </summary>
        private readonly bool m_following;

        /// <summary>
        /// Constructs a new BringAFriendArgs
        /// </summary>
        public BringAFriendArgs(GameLiving friend, bool entered, bool following = false)
        {
            this.m_friend = friend;
            this.m_entered = entered;
            this.m_following = following;
        }

        /// <summary>
        /// Gets if the target has entered or left the zone
        /// </summary>
        public bool Entered
        {
            get { return m_entered; }
        }

        /// <summary>
        /// Gets if the target has started following
        /// </summary>
        public bool Following
        {
            get { return m_following; }
        }

        /// <summary>
        /// Gets the friendly mob
        /// </summary>
        public GameLiving Friend
        {
            get { return m_friend; }
        }

    }
}