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
        /// <inheritdoc />
        public override GameLiving Owner
        {
            get => base.Owner;
            set
            {
                var owner = base.Owner;
                if (owner != null)
                {
                    RemoveOwner(owner);
                }
                owner = value;
                if (value != null)
                {
                    base.Owner = value;
                    AddOwner(owner);
                    if (value is GamePlayer player)
                    {
                        OwnerGuild = player.Guild;
                        Emblem = OwnerGuild?.Emblem ?? PvpManager.Instance.GetEmblemForPlayer(player);
                    }
                }
            }
        }

        public GamePlayer OwnerPlayer
        {
            get => Owner as GamePlayer;
            set => Owner = value;
        }

        public virtual void SetOwnership(GamePlayer? player)
        {
            OwnerPlayer = player!;
        }
    }
}
