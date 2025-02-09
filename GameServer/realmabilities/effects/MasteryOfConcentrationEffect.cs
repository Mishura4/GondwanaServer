using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Events;

namespace DOL.GS.Effects
{
    /// <summary>
    /// 
    /// </summary>
    public class MasteryofConcentrationEffect : TimedEffect, IGameEffect
    {
        /// <summary>
        /// Default constructor for MasteryofConcentrationEffect
        /// </summary>
        public MasteryofConcentrationEffect()
            : base(RealmAbilities.MasteryofConcentrationAbility.Duration)
        {
        }

        /// <summary>
        /// Called when effect is to be cancelled
        /// </summary>
        /// <param name="playerCancel">Whether or not effect is player cancelled</param>
        public override void Cancel(bool playerCancel, bool force = false)
        {
            //uncancable by player
            if (playerCancel)
                return;

            base.Cancel(playerCancel, force);
        }

        /// <summary>
        /// Name of the effect
        /// </summary>
        public override string Name
        {
            get
            {
                return "Mastery of Concentration";
            }
        }

        /// <summary>
        /// Icon ID
        /// </summary>
        public override UInt16 Icon
        {
            get
            {
                return 3006;
            }
        }

        /// <summary>
        /// Delve information
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>(4);
                delveInfoList.Add("This ability allows a player to cast uninterrupted, even while sustaining attacks, through melee or spell for 30 seconds.");

                int seconds = (int)(RemainingTime / 1000);
                if (seconds > 0)
                {
                    delveInfoList.Add(" ");
                    delveInfoList.Add("- " + seconds + " seconds remaining.");
                }

                return delveInfoList;
            }
        }
    }
}
