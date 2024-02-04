using DOL.Database;
using DOL.GS.Finance;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOL.GS.Scripts
{
    public class GuildCaptainGuard : AmteMob
    {
        public const long CLAIM_COST = 500 * 100 * 100; // 500g
        public const ushort AREA_RADIUS = 4096;
        public const ushort NEUTRAL_EMBLEM = 256;

        public static readonly List<GuildCaptainGuard> allCaptains = new List<GuildCaptainGuard>();

        private Guild _guild;

        public List<string> safeGuildIds = new List<string>();
        private readonly AmteCustomParam _safeGuildParam;

        public GuildCaptainGuard()
        {
            _safeGuildParam = new AmteCustomParam(
                "safeGuildIds",
                () => string.Join(";", safeGuildIds),
                v => safeGuildIds = v.Split(';').ToList(),
                "");
        }

        public GuildCaptainGuard(INpcTemplate npc)
            : base(npc)
        {
            _safeGuildParam = new AmteCustomParam(
                "safeGuildIds",
                () => string.Join(";", safeGuildIds),
                v => safeGuildIds = v.Split(';').ToList(),
                "");
        }

        public override AmteCustomParam GetCustomParam()
        {
            var param = base.GetCustomParam();
            param.next = _safeGuildParam;
            return param;
        }

        public override string GuildName
        {
            get
            {
                return base.GuildName;
            }
            set
            {
                base.GuildName = value;
                _guild = GuildMgr.GetGuildByName(value);

                if (_guild != null)
                {
                    ResetArea(_guild.Emblem);
                }
                else
                {
                    ResetArea(NEUTRAL_EMBLEM);
                }
            }
        }

        public override bool AddToWorld()
        {
            var r = base.AddToWorld();
            _guild = GuildMgr.GetGuildByName(GuildName);
            allCaptains.Add(this);
            return r;
        }

        public override bool RemoveFromWorld()
        {
            allCaptains.Remove(this);
            return base.RemoveFromWorld();
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player) || player.Guild == null)
                return false;

            if (player.Client.Account.PrivLevel == 1 && !player.GuildRank.Claim)
            {
                player.Out.SendMessage(string.Format("Bonjour {0}, je ne discute pas avec les bleus, circulez.", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }
            string title = string.Empty;

            if (player.GuildID != null && _guild.GuildID != null)
            {
                if (player.GuildRank != null)
                {
                    title = player.GuildRank.Title;
                }

                player.Out.SendMessage(
                    string.Format("Bonjour {0} {1} que puis-je faire pour vous ?\n[capturer le territoire] ({2})", title, player.Name, Money.GetShortString(CLAIM_COST)),
                    eChatType.CT_System,
                    eChatLoc.CL_PopupWindow
                );
                return true;
            }

            player.Out.SendMessage(string.Format("Bonjour {0} {1}, que puis-je faire pour vous ?\n\n[modifier les alliances]\n", title, player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text) || _guild == null)
                return false;
            var player = source as GamePlayer;
            if (player == null)
                return false;
            if (player.GuildID != _guild.GuildID)
            {
                if (player.GuildRank.Claim && text == "capturer le territoire")
                {
                    Claim(player);
                    return true;
                }
                if (player.Client.Account.PrivLevel == 1)
                    return false;
            }

            switch (text)
            {
                case "default":
                case "modifier les alliances":
                    var guilds = GuildMgr.GetAllGuilds()
                        .Where(g => g.IsSystemGuild == false && g.GuildID != _guild.GuildID)
                        .OrderBy(g => g.Name)
                        .Select(g =>
                        {
                            var safe = safeGuildIds.Contains(g.GuildID);
                            if (safe)
                                return string.Format("{0}: [{1}. attaquer à vue]", g.Name, g.ID);
                            return string.Format("{0}: [{1}. ne plus attaquer à vue]", g.Name, g.ID);
                        })
                        .Aggregate((a, b) => string.Format("{0}\n{1}", a, b));
                    var safeNoGuild = safeGuildIds.Contains("NOGUILD");
                    guilds += "\nLes sans guildes: [256. ";
                    guilds += (safeNoGuild ? "" : "ne plus ") + "attaquer à vue]";
                    player.Out.SendMessage(string.Format("Voici la liste des guildes et leurs paramètres :\n{0}", guilds), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                case "acheter un garde":
                    BuyGuard(player);
                    return true;
            }

            var dotIdx = text.IndexOf('.');
            ushort id;
            if (dotIdx > 0 && ushort.TryParse(text.Substring(0, dotIdx), out id))
            {
                var guild = GuildMgr.GetAllGuilds().FirstOrDefault(g => g.ID == id);
                if (guild == null && id != 256)
                    return false;
                var guildID = guild == null ? "NOGUILD" : guild.GuildID;
                if (safeGuildIds.Contains(guildID))
                    safeGuildIds.Remove(guildID);
                else
                    safeGuildIds.Add(guildID);
                SaveIntoDatabase();
                return WhisperReceive(source, "default");
            }
            return false;
        }

        public IEnumerable<SimpleGvGGuard> GetGuardsInRadius(ushort radius = AREA_RADIUS)
        {
            foreach (var npc in GetNPCsInRadius(radius).OfType<SimpleGvGGuard>())
            {
                if (npc.Captain != this)
                    continue;
                yield return npc;
            }
        }

        public void ResetArea(int newemblem, int oldemblem = NEUTRAL_EMBLEM)
        {
            foreach (var guard in GetGuardsInRadius())
                guard.Captain = this;
            foreach (var obj in GetItemsInRadius(AREA_RADIUS))
            {
                var item = obj as GameStaticItem;
                if (item != null && item.Emblem == oldemblem)
                    item.Emblem = newemblem;
            }
        }

        public void BuyGuard(GamePlayer player)
        {
            player.Out.SendMessage("Vous devez prendre contact avec un Game Master d'Avalonia.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }

        public void Claim(GamePlayer player)
        {
            if (!Name.StartsWith("Capitaine"))
            {
                player.Out.SendMessage(
                    "Vous devez demander à un GM pour ce type de territoire.",
                    eChatType.CT_System,
                    eChatLoc.CL_PopupWindow
                );
                return;
            }

            if (DateTime.Now.DayOfWeek != DayOfWeek.Monday || DateTime.Now.Hour < 21 || DateTime.Now.Hour > 23)
            {
                player.Out.SendMessage(
                    "Il n'est pas possible de capturer des territoires aujourd'hui à cette heure-ci.\n" +
                    "Pour le moment, les territoires ne sont prenables que le lundi entre 21h et 23h.\n",
                    eChatType.CT_System,
                    eChatLoc.CL_PopupWindow
                );
                return;
            }

            if (GetGuardsInRadius(AREA_RADIUS).Any(g => g.IsAlive))
            {
                player.Out.SendMessage(
                    "Vous devez tuer tous les gardes avant de pouvoir prendre possession du territoire.",
                    eChatType.CT_System,
                    eChatLoc.CL_PopupWindow
                );
                return;
            }

            if (!player.RemoveMoney(Currency.Copper.Mint(CLAIM_COST)))
            {
                player.Out.SendMessage(
                    "Vous n'avez pas assez d'argent pour prendre possession du territoire.",
                    eChatType.CT_System,
                    eChatLoc.CL_PopupWindow
                );
                return;
            }

            var oldguild = GuildMgr.GetGuildByName(GuildName);
            GuildName = player.GuildName;
            SaveIntoDatabase();
            ushort oldEmblem = oldguild != null ? (ushort)oldguild.Emblem : NEUTRAL_EMBLEM;
            if (player.Guild != null)
            {
                ResetArea(player.Guild.Emblem, oldEmblem);
            }
            else
            {
                ResetArea(NEUTRAL_EMBLEM, oldEmblem);
            }

            player.Out.SendMessage(
                "Le territoire appartient maintenant à votre guilde, que voulez-vous faire ?\n\n[modifier les alliances]\n",
                eChatType.CT_System,
                eChatLoc.CL_PopupWindow
            );
        }
    }
}