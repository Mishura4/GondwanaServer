using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;

namespace DOL.GS
{
    public class LootChangerGenerator : LootGeneratorBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [GameServerStartedEvent]
        public static void Init(DOLEvent e, object sender, EventArgs args)
        {
            PreloadLootTemplates();
        }

        private readonly static Dictionary<string, DBMobXLootChanger> m_MobXDB = new Dictionary<string,DBMobXLootChanger>();
        private readonly static Dictionary<string, Dictionary<string, DBLootChangerTemplate>> m_MobNameXLootChangerTemplates = new Dictionary<string, Dictionary<string, DBLootChangerTemplate>>();

        /// <summary>
        /// Chargement des tables
        /// </summary>
        private static void PreloadLootTemplates()
        {
            var mXlc = GameServer.Database.SelectAllObjects<DBMobXLootChanger>();

            foreach (var obj in mXlc)
            {
                var templates = GameServer.Database.SelectObjects<DBLootChangerTemplate>(o => o.LootChangerTemplateName == obj.LootChangerTemplateName);
                if (templates == null)
                    continue;

                var dico = new Dictionary<string, DBLootChangerTemplate>();
                foreach (var tpl in templates)
                {
                    var template = GameServer.Database.FindObjectByKey<ItemTemplate>(tpl.ItemsTemplatesGives);
                    if (template == null)
                        log.Error("[LOOT CHANGER] item template (" + tpl.ItemsTemplatesRecvs + ") not found (mob name=" + obj.MobName + ") !");
                    else
                        dico.Add(tpl.ItemsTemplatesRecvs, tpl);
                }

                m_MobXDB.Add(obj.MobName, obj);
                m_MobNameXLootChangerTemplates.Add(obj.MobName, dico);

            }
            log.Info("[LOOT CHANGER] " + m_MobNameXLootChangerTemplates.Count + " mobs drops");
        }

        //TODO: Suppression d'un loot/mob
        public static int AddOrChangeLoot(GameNPC npc, GameClient client, string[] args)
        {
            //&lootchanger add <receive item> <give item> 
            //&lootchanger remove <receive item/all>
            //&lootchanger info
            if (npc == null || args.Length <= 1)
                return -1;
            string lootTemplate = npc.Name;
            switch (args[1].ToLower())
            {
                case "add":
                    try
                    {
                        var templates = GameServer.Database.SelectObjects<DBLootChangerTemplate>(o => o.LootChangerTemplateName == lootTemplate);
                        var template = templates.FirstOrDefault(tmp => tmp.ItemsTemplatesRecvs == args[2]);

                        var itemGive = GameServer.Database.FindObjectByKey<ItemTemplate>(args[3]);
                        if (itemGive == null)
                            return SendMsg(client, "L'item '" + args[3] + "' n'existe pas.");
                        var itemRecv = GameServer.Database.FindObjectByKey<ItemTemplate>(args[2]);
                        if (itemRecv == null)
                            return SendMsg(client, "L'item '" + args[2] + "' n'existe pas.");
                        string familyName = "";
                        short order = 0;
                        if (args.Length > 4)
                            if (args.Length < 6)
                                return -1;
                            else
                            {
                                familyName = args[4];
                                short.TryParse(args[5], out order);
                            }

                        #region template DB
                        //template DB
                        if (template != null)
                        {
                            template.ItemsTemplatesRecvs = itemRecv.Id_nb;
                            template.ItemsTemplatesGives = itemGive.Id_nb;
                            template.Order = order;
                            template.FamilyName = familyName;
                            GameServer.Database.SaveObject(template);
                        }
                        else
                        {
                            template = new DBLootChangerTemplate
                                           {
                                               LootChangerTemplateName = lootTemplate,
                                               ItemsTemplatesRecvs = itemRecv.Id_nb,
                                               ItemsTemplatesGives = itemGive.Id_nb,
                                               Order = order,
                                               FamilyName = familyName
                                           };
                            GameServer.Database.AddObject(template);
                        }
                        #endregion

                        #region mobXloot
                        if (m_MobNameXLootChangerTemplates.ContainsKey(npc.Name))
                        {
                            if (m_MobNameXLootChangerTemplates[npc.Name].ContainsKey(template.ItemsTemplatesRecvs))
                                m_MobNameXLootChangerTemplates[npc.Name][template.ItemsTemplatesRecvs] = template;
                            else
                                m_MobNameXLootChangerTemplates[npc.Name].Add(template.ItemsTemplatesRecvs, template);
                            GameServer.Database.SaveObject(m_MobXDB[npc.Name]);
                        }
                        else
                        {
                            // Fix 2nd point : add instantanly the loot changer without reload the server
                            //cache
                            var dic = new Dictionary<string, DBLootChangerTemplate>
                                          {
                                              {template.ItemsTemplatesRecvs, template}
                                          };
                            m_MobNameXLootChangerTemplates.Add(npc.Name, dic);

                            //DB
                            var mXlc = new DBMobXLootChanger
                                           {
                                               LootChangerTemplateName = lootTemplate,
                                               MobName = npc.Name,
                                               DropCount = 1
                                           };
                            GameServer.Database.AddObject(mXlc);
                            m_MobXDB.Add(npc.Name, mXlc);
                        }
                        #endregion

                        return SendMsg(client, "Le loot a été ajouté (" + args[2] + "=>" + args[3] + ").");
                    }
                    catch
                    {
                        return -1;
                    }

                case "remove":
                    try
                    {
                        if (args[2].ToLower() == "all")
                        {
                            foreach (var tmp in m_MobNameXLootChangerTemplates[npc.Name].Values)
                                GameServer.Database.DeleteObject(tmp);
                            m_MobNameXLootChangerTemplates.Remove(npc.Name);
                            GameServer.Database.DeleteObject(m_MobXDB[npc.Name]);
                            m_MobXDB.Remove(npc.Name);

                            return SendMsg(client, "Les loots de " + npc.Name + " ont été supprimés.");
                        }
                    	return SendMsg(client, "TODO, use \"/lootchanger remove all\"");
                    }
                    catch
                    {
                        return -1;
                    }

                case "info":
                    var infos = new List<string> {" ..:: " + npc.Name + " ::.."};
                    if (!m_MobXDB.ContainsKey(npc.Name))
                        infos.Add("Aucun loot changer.");
                    else
                    {
                        infos.Add("Item du joueur => item gagné");
                        infos.AddRange(m_MobNameXLootChangerTemplates[npc.Name].Values.Select(tmp => tmp.ItemsTemplatesRecvs + " => " + tmp.ItemsTemplatesGives));
                    }
                    client.Out.SendCustomTextWindow("Loot changer info", infos);
                    break;

                default:
                    return -1;
            }
            return 1;
        }

        public override LootList GenerateLoot(GameNPC mob, GameObject killer)
        {
            LootList loots = base.GenerateLoot(mob, killer);
            if (killer is GameNPC && ((GameNPC)killer).Brain is IControlledBrain)
                killer = ((IControlledBrain)((GameNPC)killer).Brain).Owner;
            if (!m_MobXDB.ContainsKey(mob.Name) || !(killer is GamePlayer))
                return loots;
            var pl = killer as GamePlayer;

            var randLoot = new Dictionary<DBLootChangerTemplate, InventoryItem>();

            // Add LootChanger's families to fix the issue when there are several receiver for one LootChanger's family
            Dictionary<string, KeyValuePair<DBLootChangerTemplate, InventoryItem>> familiesLootChanger = new Dictionary<string, KeyValuePair<DBLootChangerTemplate, InventoryItem>>();
            foreach (var tmp in m_MobNameXLootChangerTemplates[mob.Name])
            {
                var obj = pl.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack).Where(item => tmp.Key == item.Id_nb).FirstOrDefault();
                if(obj != null)
                {
                    if (string.IsNullOrEmpty(tmp.Value.FamilyName))
                        randLoot.Add(tmp.Value, obj);
                    else if (!familiesLootChanger.ContainsKey(tmp.Value.FamilyName))
                        familiesLootChanger.Add(tmp.Value.FamilyName, new KeyValuePair<DBLootChangerTemplate, InventoryItem>(tmp.Value, obj));
                    else if (familiesLootChanger[tmp.Value.FamilyName].Key.Order < tmp.Value.Order)
                        familiesLootChanger[tmp.Value.FamilyName] = new KeyValuePair<DBLootChangerTemplate, InventoryItem>(tmp.Value, obj);
                }
            }
            foreach (KeyValuePair<DBLootChangerTemplate, InventoryItem> familyLootChanger in familiesLootChanger.Values)
                randLoot.Add(familyLootChanger.Key, familyLootChanger.Value);

            foreach (var tmp in randLoot)
            {
                if (!pl.Inventory.RemoveCountFromStack(tmp.Value, 1))
                    continue;
                InventoryLogging.LogInventoryAction(pl, mob, eInventoryActionType.Loot, tmp.Value.Template);
                var item = GameServer.Database.FindObjectByKey<ItemTemplate>(tmp.Key.ItemsTemplatesGives);
                if (!pl.Inventory.AddTemplate(GameInventoryItem.Create(item), 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                    loots.AddFixed(item, 1);
                else
                    InventoryLogging.LogInventoryAction(mob, pl, eInventoryActionType.Loot, item);
            }

            return loots;
        }

        #region Util
        /// <summary>
        /// Envoie un msg au client
        /// </summary>
        private static int SendMsg(GameClient cl, string msg)
        {
            cl.Out.SendMessage(msg, PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
            return 1;
        }
        #endregion
    }
}
