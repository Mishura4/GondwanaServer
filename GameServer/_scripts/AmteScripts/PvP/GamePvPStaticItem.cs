#nullable enable
using AmteScripts.Managers;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AmteScripts.PvP.CTF
{
    public class GamePvPStaticItem : GameStaticItem
    {
        private void SetOwner(GameLiving? newOwner, bool updateEmblem = true)
        {
            var prevOwner = base.Owner;
            int newEmblem = 0;
            if (newOwner != prevOwner)
            {
                if (prevOwner != null)
                {
                    RemoveOwner(prevOwner);
                }
                base.Owner = newOwner;
                if (newOwner != null)
                {
                    AddOwner(newOwner);
                }
                else
                {
                    base.OwnerGuild = null;
                }
            }
            
            if (newOwner is GamePlayer playerOwner)
            {
                base.OwnerGuild = playerOwner.Guild;
                newEmblem = PvpManager.Instance.GetEmblemForPlayer(playerOwner);
            }
            
            if (updateEmblem)
            {
                Emblem = newEmblem;
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

        public GamePlayer? OwnerPlayer
        {
            get => Owner as GamePlayer;
            set => Owner = value;
        }

        public virtual void SetOwnership(GamePlayer? player, bool updateEmblem = true)
        {
            SetOwner(player, updateEmblem);
        }
    }
}
