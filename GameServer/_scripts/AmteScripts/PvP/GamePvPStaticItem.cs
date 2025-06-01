#nullable enable
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
        private void SetOwner(GameLiving? newOwner, bool updateEmblem = true)
        {
            var prevOwner = base.Owner;
            if (prevOwner != null)
            {
                RemoveOwner(prevOwner);
            }
            if (newOwner != null)
            {
                base.Owner = newOwner;
                AddOwner(newOwner);
            }
            
            if (updateEmblem)
            {
                if (newOwner is GamePlayer player)
                {
                    Emblem = PvpManager.Instance.GetEmblemForPlayer(player);
                }
                else
                {
                    Emblem = 0;
                }
            }
                
        }
        private void SetOwnerGuild(Guild? newOwner, bool updateEmblem = true)
        {
            if (newOwner != null)
            {
                SetOwner(null, false);
                base.OwnerGuild = newOwner;
            }
            
            if (updateEmblem)
            {
                Emblem = newOwner?.Emblem ?? 0;
            }
                
        }
        
        /// <inheritdoc />
        public override GameLiving? Owner
        {
            get => base.Owner;
            set
            {
                SetOwner(value, true);
            }
        }

        /// <inheritdoc />
        public override Guild? OwnerGuild
        {
            get => base.OwnerGuild;
            set
            {
                SetOwnerGuild(value, true);
            }
        }

        public GamePlayer? OwnerPlayer
        {
            get => Owner as GamePlayer;
            set => Owner = value;
        }

        public virtual void SetOwnership(GamePlayer? player)
        {
            if (player?.Guild != null)
            {
                SetOwnerGuild(player.Guild);
            }
            else
            {
                SetOwner(player);
            }
        }
    }
}
