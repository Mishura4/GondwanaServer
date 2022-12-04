using DOL.Database;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class OutlawTradeItems
        : MerchantTradeItems
    {

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public OutlawTradeItems(string itemsListId)
            : base(itemsListId)
        {
        }


        public override IDictionary GetItemsInPage(int page)
        {
            try
            {
                HybridDictionary itemsInPage = new HybridDictionary(MAX_ITEM_IN_TRADEWINDOWS);
                if (m_itemsListID != null && m_itemsListID.Length > 0)
                {
                    var itemList = GameServer.Database.SelectObjects<MerchantItem>("`ItemListID` = @ItemListID AND `PageNumber` = @PageNumber", new[] { new QueryParameter("@ItemListID", m_itemsListID), new QueryParameter("@PageNumber", page) });
                    foreach (MerchantItem merchantitem in itemList)
                    {
                        //Force query to execute without precache
                        ItemTemplate item = GameServer.Database.SelectObjects<ItemTemplate>("Id_nb = @Id_nb", new QueryParameter("Id_nb", merchantitem.ItemTemplateID))?.FirstOrDefault();
                        if (item != null)
                        {
                            ItemTemplate slotItem = (ItemTemplate)itemsInPage[merchantitem.SlotPosition];
                            if (slotItem == null)
                            {
                                item.Price *= 2;
                                itemsInPage.Add(merchantitem.SlotPosition, item);
                            }
                            else
                            {
                                log.ErrorFormat("two merchant items on same page/slot: listID={0} page={1} slot={2}", m_itemsListID, page, merchantitem.SlotPosition);
                            }
                        }
                        else
                        {
                            log.ErrorFormat(
                                "Item template with ID = '{0}' not found for merchant item list '{1}'",
                                merchantitem.ItemTemplateID, ItemsListID);
                        }
                    }
                }

                lock (m_usedItemsTemplates.SyncRoot)
                {
                    foreach (DictionaryEntry de in m_usedItemsTemplates)
                    {
                        if ((int)de.Key >= (MAX_ITEM_IN_TRADEWINDOWS * page) && (int)de.Key < (MAX_ITEM_IN_TRADEWINDOWS * page + MAX_ITEM_IN_TRADEWINDOWS))
                        {
                            itemsInPage[(int)de.Key % MAX_ITEM_IN_TRADEWINDOWS] = (ItemTemplate)de.Value;
                        }
                    }
                }

                return itemsInPage;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Loading merchant items list (" + m_itemsListID + ") page (" + page + "): ", e);
                }

                return new HybridDictionary();
            }
        }

        public override IDictionary GetAllItems()
        {
            try
            {
                Hashtable allItems = new Hashtable();
                if (m_itemsListID != null && m_itemsListID.Length > 0)
                {
                    var itemList = GameServer.Database.SelectObjects<MerchantItem>("`ItemListID` = @ItemListID", new QueryParameter("@ItemListID", m_itemsListID));
                    foreach (MerchantItem merchantitem in itemList)
                    {
                        ItemTemplate item = GameServer.Database.SelectObjects<ItemTemplate>("Id_nb = @Id_nb", new QueryParameter("Id_nb", merchantitem.ItemTemplateID))?.FirstOrDefault();
                        if (item != null)
                        {
                            ItemTemplate slotItem = (ItemTemplate)allItems[merchantitem.SlotPosition];
                            if (slotItem == null)
                            {
                                item.Price *= 2;
                                allItems.Add(merchantitem.SlotPosition, item);
                            }
                            else
                            {
                                log.ErrorFormat("two merchant items on same page/slot: listID={0} page={1} slot={2}", m_itemsListID, merchantitem.PageNumber, merchantitem.SlotPosition);
                            }
                        }
                    }
                }

                lock (m_usedItemsTemplates.SyncRoot)
                {
                    foreach (DictionaryEntry de in m_usedItemsTemplates)
                    {
                        allItems[(int)de.Key] = (ItemTemplate)de.Value;
                    }
                }

                return allItems;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Loading merchant items list (" + m_itemsListID + "):", e);
                }

                return new HybridDictionary();
            }
        }

        public override ItemTemplate GetItem(int page, eMerchantWindowSlot slot)
        {
            try
            {
                slot = GetValidSlot(page, slot);
                if (slot == eMerchantWindowSlot.Invalid)
                {
                    return null;
                }

                ItemTemplate item;
                lock (m_usedItemsTemplates.SyncRoot)
                {
                    item = m_usedItemsTemplates[(int)slot + (page * MAX_ITEM_IN_TRADEWINDOWS)] as ItemTemplate;
                    if (item != null)
                    {
                        return item;
                    }
                }

                if (m_itemsListID != null && m_itemsListID.Length > 0)
                {
                    var itemToFind = GameServer.Database.SelectObjects<MerchantItem>(
                        "`ItemListID` = @ItemListID AND `PageNumber` = @PageNumber AND `SlotPosition` = @SlotPosition",
                                                                                     new[] { new QueryParameter("@ItemListID", m_itemsListID), new QueryParameter("@PageNumber", page), new QueryParameter("@SlotPosition", (int)slot) }).FirstOrDefault();
                    if (itemToFind != null)
                    {
                        //Prevent precache by calling SelectObjects
                        item = GameServer.Database.SelectObjects<ItemTemplate>("Id_nb = @Id_nb", new QueryParameter("Id_nb", itemToFind.ItemTemplateID))?.FirstOrDefault();

                        if (item != null)
                        {
                            item.Price *= 2;
                        }
                    }
                }

                return item;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Loading merchant items list (" + m_itemsListID + ") page (" + page + ") slot (" + slot + "): ", e);
                }

                return null;
            }
        }
    }
}