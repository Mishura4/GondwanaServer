using AmteScripts.Managers;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmteScripts.PvP.CTF
{
    public class GamePvPStaticItem : GameStaticItem
    {
        protected Guild? m_ownerGuild = null;
        protected GamePlayer? m_ownerPlayer = null;
        
        /// <summary>
        /// Gets the guild that owns this item, if any.
        /// </summary>
        public virtual Guild? OwnerGuild
        {
            get => m_ownerGuild;
            set
            {
                m_ownerGuild = value;
            }
        }

        public virtual GamePlayer? OwnerPlayer
        {
            get => m_ownerPlayer;
            set => m_ownerPlayer = value;
        } 
        
        public virtual void SetOwnership(GamePlayer? player)
        {
            if (player != null)
            {
                OwnerPlayer = player;
                OwnerGuild = player.Guild;
                Emblem = OwnerGuild?.Emblem ?? PvpManager.Instance.GetEmblemForPlayer(player);
            }
            else
            {
                OwnerPlayer = null;
                OwnerGuild = null;
                Emblem = 0;
            }
        }

        public bool IsOwnedBy(GamePlayer p)
        {
            return IsOwnedByGuild(p.Guild) || (OwnerPlayer != null && OwnerPlayer == p);
        }
        
        /// <summary>
        /// Does a given guild own me?
        /// (Optional convenience method)
        /// </summary>
        public new bool IsOwnedByGuild(Guild g)
        {
            return (OwnerGuild != null && OwnerGuild == g);
        }
    }
}
