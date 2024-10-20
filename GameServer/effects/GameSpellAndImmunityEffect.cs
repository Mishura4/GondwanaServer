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
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using System.Threading;

namespace DOL.GS.Effects
{
    /// <summary>
    /// Spell Effect assists SpellHandler with duration spells with immunity
    /// </summary>
    public class GameSpellAndImmunityEffect : GameSpellEffect
    {
        /// <summary>
        /// The amount of times this effect started
        /// </summary>
        protected int m_startedCount;

        /// <summary>
        /// Creates a new game spell effect
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="duration"></param>
        /// <param name="pulseFreq"></param>
        public GameSpellAndImmunityEffect(ISpellHandler handler, int duration, int pulseFreq) : this(handler, duration, pulseFreq, 1)
        {
        }

        /// <summary>
        /// Creates a new game spell effect
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="duration"></param>
        /// <param name="pulseFreq"></param>
        /// <param name="effectiveness"></param>
        public GameSpellAndImmunityEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness) : base(handler, duration, pulseFreq, effectiveness)
        {
            m_startedCount = 0;
        }

        /// <summary>
        /// Starts the timers for this effect
        /// </summary>
        protected override void StartDurationTimer()
        {
            // Duration => 0 = endless until explicit stop
            if (Duration == 0)
                return;
            
            int duration = Duration;
            if (!IsExpired)
            {
                int startcount = Interlocked.Add(ref m_startedCount, 1) - 1;
                if (startcount > 0)
                {
                    duration /= Math.Min(20, startcount * 2);
                    if (duration < 1) duration = 1;
                }
            }
            var now = GameTimer.GetTickCount();
            var endTick = m_startedTick + duration;
            var timeLeft = endTick - now;
            if (m_startedTick != 0 && timeLeft > 0)
            {
                m_effectTimer = new RegionTimer(m_owner, ExpiredCallback);
                m_effectTimer.Start((int)timeLeft);
            }
        }

        /// <summary>
        /// Gets the amount of times this effect started
        /// </summary>
        public int StartedCount
        {
            get { return m_startedCount; }
        }
    }
}
