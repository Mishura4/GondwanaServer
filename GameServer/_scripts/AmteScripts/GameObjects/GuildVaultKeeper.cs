using System.Collections.Generic;
using System.Numerics;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class GuildVaultKeeper : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            /*if (player.HCFlag)
            {
                SayTo(player,$"I'm sorry {player.Name}, my vault is not Hardcore enough for you.");
                return false;
            }*/

            // if (player.Level <= 1)
            // {
            //     SayTo(player,$"I'm sorry {player.Name}, come back if you are venerable to use my services.");
            //     return false;
            // }

            if (player.Guild == null)
            {
                player.Out.SendMessage($"I'm sorry {player.Name}, I cannot do anything for you without a guild.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (player.Guild.IsSystemGuild)
            {
                player.Out.SendMessage($"I'm sorry {player.Name}, your guild does not have a vault.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return true;
            }

            string message = $"Greetings {player.Name}, nice meeting you.\n";

            message += "I am happy to offer you my services.\n\n";

            message += "You can browse the [first], [second] or [third] page of " + player.Guild.Name + "'s vault.";
            player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_PopupWindow);

            ItemTemplate vaultItem = GetDummyVaultItem(player);
            GuildVault vault = new GuildVault(player, 0, vaultItem);
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;

            GamePlayer player = source as GamePlayer;

            if (player?.Guild == null)
                return false;

            int index;

            switch (text)
            {
                case "first":
                case "premi�re":
                    index = 0;
                    break;

                case "second":
                case "deuxi�me":
                    index = 1;
                    break;

                case "third":
                case "troisi�me":
                    index = 2;
                    break;

                default:
                    return true;
            }

            GuildVault vault = new GuildVault(player, index, GetDummyVaultItem(player));
            player.ActiveInventoryObject = vault;
            player.Out.SendInventoryItemsUpdate(vault.GetClientInventory(player), eInventoryWindowType.HouseVault);

            return true;
        }

        private static ItemTemplate GetDummyVaultItem(GamePlayer player)
        {
            ItemTemplate vaultItem = new ItemTemplate();
            vaultItem.Object_Type = (int)eObjectType.HouseVault;
            vaultItem.Name = "Vault";
            vaultItem.ObjectId = player.Guild?.GuildID;
            switch (player.Realm)
            {
                case eRealm.Albion:
                    vaultItem.Id_nb = "housing_alb_vault";
                    vaultItem.Model = 1489;
                    break;
                case eRealm.Hibernia:
                    vaultItem.Id_nb = "housing_hib_vault";
                    vaultItem.Model = 1491;
                    break;
                case eRealm.Midgard:
                    vaultItem.Id_nb = "housing_mid_vault";
                    vaultItem.Model = 1493;
                    break;
            }

            return vaultItem;
        }
    }

    public sealed class GuildVault : CustomVault
    {
        private readonly int m_vaultNumber = 0;

        public Guild Guild { get; init; }

        /// <summary>
        /// A guild vault that masquerades as a house vault to the game client
        /// </summary>
        /// <param name="player">Player who owns the vault</param>
        /// <param name="vaultNPC">NPC controlling the interaction between player and vault</param>
        /// <param name="vaultNumber">Valid vault IDs are 0-1</param>
        /// <param name="dummyTemplate">An ItemTemplate to satisfy the base class's constructor</param>
        public GuildVault(GamePlayer player, int vaultNumber, ItemTemplate dummyTemplate)
            : base(player, player.Guild?.GuildID ?? "", vaultNumber, dummyTemplate)
        {
            Guild = player.Guild;
            m_vaultNumber = vaultNumber;
            Name = (player.Guild?.Name ?? "unknown") + "'s Vault";
        }

        public override string GetOwner(GamePlayer player)
        {
            return (player.Guild?.GuildID);
        }

        public override int FirstDBSlot
        {
            get
            {
                switch (m_vaultNumber)
                {
                    case 0:
                        return (int)2900;
                    case 1:
                        return (int)3000;
                    case 2:
                        return (int)3100;
                    default: return 0;
                }
            }
        }

        public override int LastDBSlot
        {
            get
            {
                switch (m_vaultNumber)
                {
                    case 0:
                        return (int)2999;
                    case 1:
                        return (int)3099;
                    case 2:
                        return (int)3199;
                    default: return 0;
                }
            }
        }

        /// <summary>
        /// List of items in the vault.
        /// </summary>
        public override IList<InventoryItem> DBItems(GamePlayer player = null)
        {
            return GameServer.Database.SelectObjects<InventoryItem>(DB.Column("OwnerID").IsEqualTo(Guild.GuildID).And(DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDBSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDBSlot))));
        }
    }
}