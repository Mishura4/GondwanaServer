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
    public class TerritoryGuard : AmteMob, IGuardNPC
    {
        public TerritoryGuard()
        {
            var brain = new TerritoryBrain();
            brain.AggroLink = 3;
            brain.AggroRange = 500;
            SetOwnBrain(brain);
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
            {
                if (string.IsNullOrWhiteSpace(GuildName) && player.Client.Account.PrivLevel >= 2)
                {
                    player.Out.SendMessage("Message aux GM+: Donnez-moi dans une guilde pour fonctionner.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                return false;
            }

            if (player.Client.Account.PrivLevel == 1 && player.GuildName != GuildName)
                return false;
            if (!player.GuildRank.Claim)
            {
                player.Out.SendMessage(string.Format("Bonjour {0}, je ne discute pas avec les bleus, circulez.", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            var cloaks = GameServer.Database.SelectObjects<NPCEquipment>("TemplateID like @guard AND Slot = @slot", new QueryParameter[] { new QueryParameter("guard", "gvg_guard_%"), new QueryParameter("slot", 26) });
            if (cloaks != null)
            {
                player.Out.SendMessage(
                   string.Format("Bonjour {0}, vous pouvez modifier l'équippement que je porte, sélectionner l'ensemble que vous souhaitez :\n", player.Name) +
                   string.Join("\n", cloaks.Select(c => string.Format("[{0}]", c.TemplateID.Substring(10)))),
                   eChatType.CT_System,
                   eChatLoc.CL_PopupWindow
               );
            }
            else
            {
                player.Out.SendMessage("Umh.. j'ai beau chercher mais je ne trouve pas de capes dans mon inventaire.. Tu devrais dire ça à un GM..", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text) || string.IsNullOrWhiteSpace(GuildName))
                return false;
            var player = source as GamePlayer;
            if (player == null || player.Guild == null)
                return false;
            if (player.Client.Account.PrivLevel == 1 && player.GuildName != GuildName)
                return false;
            if (player.Client.Account.PrivLevel == 1 && !player.GuildRank.Claim)
            {
                player.Out.SendMessage(string.Format("Bonjour {0}, je ne discute pas avec les bleus, circulez.", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            var cloaks = GameServer.Database.SelectObjects<NPCEquipment>("TemplateID like @guard AND Slot = @slot", new QueryParameter[] { new QueryParameter("guard", "gvg_guard_%"), new QueryParameter("slot", 26) });
            text = string.Format("gvg_guard_{0}", text);
            if (cloaks != null && cloaks.Any(c => c.TemplateID == text))
                TerritoryManager.Instance.ChangeGuildOwner(this.InternalID, player.Guild, text);
            return true;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            var brain = this.Brain as TerritoryBrain;
            Mob mob = obj as Mob;

            if (brain != null && mob != null)
            {
                if (mob.AggroRange > 0)
                    brain.AggroRange = mob.AggroRange;
            }
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);

            var plKiller = killer as GamePlayer;
            var npc = killer as GameNPC;
            if (plKiller == null && npc != null && npc.ControlledBrain != null)
                plKiller = npc.ControlledBrain.GetPlayerOwner();
            if (plKiller != null && !string.IsNullOrEmpty(GuildName))
            {
                var guild = GuildMgr.GetGuildByName(GuildName);
                if (guild == null)
                    return;
                var name = "un inconnu";
                if (!string.IsNullOrEmpty(plKiller.GuildName))
                    name = string.Format("un membre de la guilde {0}", plKiller.GuildName);


                guild.SendMessageToGuildMembers(
                    string.Format("un garde vient d'être tué par {0}.", name),
                    eChatType.CT_Guild,
                    eChatLoc.CL_ChatWindow
                );

                if (guild.alliance != null)
                    foreach (Guild guildAlly in guild.alliance.Guilds)
                    {
                        guildAlly.SendMessageToGuildMembers(
                         string.Format("un garde vient d'être tué par {0}.", name),
                         eChatType.CT_Guild,
                         eChatLoc.CL_ChatWindow
                        );
                    }
            }
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;
            //if (Captain != null)
            //    RefreshEmblem();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class TerritoryBrain : AmteMobBrain
    {

        public override int AggroLevel
        {
            get { return 100; }
            set { }
        }

        protected override void CheckPlayerAggro()
        {
            if (Body.AttackState)
                return;
            foreach (GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
                    continue;

                int aggro = CalculateAggroLevelToTarget(pl);
                if (aggro <= 0)
                    continue;
                AddToAggroList(pl, aggro);
                if (pl.Level > Body.Level - 20 || (pl.Group != null && pl.Group.MemberCount > 2))
                    // Use new BAF system
                    BringFriends(pl);
            }
        }

        protected override void CheckNPCAggro()
        {
            if (Body.AttackState)
                return;
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange, Body.CurrentRegion.IsDungeon ? false : true))
            {
                bool isTaxi = npc as GameTaxi != null;
                if (npc.Realm != 0 || (npc.Flags & GameNPC.eFlags.PEACE) != 0 ||
                    !npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active ||
                    isTaxi ||
                    m_aggroTable.ContainsKey(npc) ||
                    !GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
                    continue;

                int aggro = CalculateAggroLevelToTarget(npc);
                if (aggro <= 0)
                    continue;
                AddToAggroList(npc, aggro);
                if (npc.Level > Body.Level)
                    // Use new BAF system
                    BringFriends(npc);
            }
        }

        private void BringReinforcements(GameNPC target)
        {
            int count = (int)Math.Log(target.Level - Body.Level, 2) + 1;
            foreach (GameNPC npc in Body.GetNPCsInRadius(WorldMgr.YELL_DISTANCE))
            {
                if (count <= 0)
                    return;
                var brain = npc.Brain as TerritoryBrain;
                if (brain == null)
                    continue;
                brain.AddToAggroList(target, 1);
                brain.AttackMostWanted();
            }
        }

        public override int CalculateAggroLevelToTarget(GameLiving target)
        {
            var player = target as AmtePlayer;
            if (player != null)
            {
                //if (Captain != null)
                //{
                //    var plGuildId = player.Guild != null ? player.GuildID : "NOGUILD";
                //    if (target.GuildName == Body.GuildName || Captain.safeGuildIds.Contains(plGuildId))
                //        return 0;
                //    return 100;
                //}
                return target.GuildName == Body.GuildName ? 0 : 100;
            }
            if (target.Realm == 0)
                return 0;
            return base.CalculateAggroLevelToTarget(target);
        }
    }
}

