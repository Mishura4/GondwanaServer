using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Effects
{
    /// <summary>
    /// 
    /// </summary>
    public class StaticEffect : IGameEffect
    {
        private ushort m_id;
        protected GameLiving m_owner = null;

        /// <summary>
        /// Cancel effect
        /// </summary>
        /// <param name="playerCanceled"></param>
        public virtual bool Cancel(bool playerCanceled, bool force = false)
        {
            if (!force && playerCanceled && HasNegativeEffect)
            {
                if (Owner is GamePlayer player)
                    player.SendTranslatedMessage("Effects.StaticEffect.YouCantRemoveThisEffect", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            Stop();
            return true;
        }

        /// <summary>
        /// Start the effect on target
        /// </summary>
        /// <param name="target">The effect target</param>
        public virtual void Start(GameLiving target)
        {
            m_owner = target;
            target.EffectList.Add(this);
        }

        /// <summary>
        /// Stop the effect on owner
        /// </summary>
        public virtual void Stop()
        {
            if (m_owner == null)
                return;

            m_owner.EffectList.Remove(this);
        }

        /// <summary>
        /// Name of the effect
        /// </summary>
        public virtual string Name { get { return "NoName"; } }

        /// <summary>
        /// Remaining Time of the effect in milliseconds
        /// </summary>
        public virtual int RemainingTime
        {
            get
            {
                return 0; // unlimited
            }
        }

        public GameLiving Owner
        {
            get { return m_owner; }
        }

        public virtual ushort Icon
        {
            get { return 0; }
        }

        /// <summary>
        /// unique id for identification in effect list
        /// </summary>
        public ushort InternalID
        {
            get { return m_id; }
            set { m_id = value; }
        }

        public virtual bool HasNegativeEffect
        {
            get { return false; }
        }

        public virtual IList<string> DelveInfo
        {
            get
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Get the save effect
        /// </summary>
        /// <returns></returns>
        public PlayerXEffect getSavedEffect()
        {
            return null;
        }
    }
}