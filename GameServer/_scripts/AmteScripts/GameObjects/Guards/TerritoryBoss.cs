using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Territory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Beware, Changing this class Name or Namespace breaks TerritoryManager
    /// </summary>
    public class TerritoryBoss
        : AmteMob, IGuardNPC
    {
        private string originalGuildName;

        public TerritoryBoss()
        {
            var brain = new TerritoryBrain();
            brain.AggroLink = 3;
            brain.AggroRange = 500;
            SetOwnBrain(brain);
        }


        public override bool AddToWorld()
        {
            bool added = base.AddToWorld();

            if (!added)
            {
                return false;
            }

            var territory = TerritoryManager.Instance.Territories.FirstOrDefault(t => t.BossId.Equals(this.InternalID));

            if (territory != null && territory.GuildOwner != null)
            {
                this.GuildName = territory.GuildOwner;
            }

            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (player.Client.Account.PrivLevel == 1 && !IsWithinRadius(player, InteractDistance))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.Interact.TooFarAway", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
                return false;
            }
            Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
            player.Notify(GameObjectEvent.InteractWith, player, new InteractWithEventArgs(this));

            if (string.IsNullOrWhiteSpace(GuildName) || player.Guild == null)
                return false;
            if (player.Client.Account.PrivLevel == 1 && player.GuildName != GuildName)
                return false;
            if (!player.GuildRank.Claim)
            {
                player.Out.SendMessage(string.Format("Bonjour {0}, je ne discute pas avec les bleus, circulez.", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            return true;
        }


        public override void Die(GameObject killer)
        {
            base.Die(killer);
            GamePlayer player = killer as GamePlayer;

            if (killer.GuildName != null && player != null)
            {
                this.GuildName = killer.GuildName;
                TerritoryManager.Instance.ChangeGuildOwner(this, player.Guild);
            }
        }


        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            TerritoryBrain brain = this.Brain as TerritoryBrain;
            Mob mob = obj as Mob;
            if (mob != null)
            {
                this.originalGuildName = mob.Guild;
            }

            if (brain != null && mob != null)
            {
                if (mob.AggroRange > 0)
                    brain.AggroRange = mob.AggroRange;
            }
        }

        public override void RestoreOriginalGuildName()
        {
            this.GuildName = originalGuildName;
        }
    }
}