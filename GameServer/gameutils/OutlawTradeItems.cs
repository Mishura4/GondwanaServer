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
            var pageEntries = Catalog.GetPage(page).GetAllEntries();
            var result = new HybridDictionary();
            foreach (var entry in pageEntries)
            {
                entry.Item.Price *= 2;
                result.Add((int)entry.SlotPosition, entry.Item);
            }
            return result;
        }

        public override IDictionary GetAllItems()
        {
            var items = new Hashtable();
            var catalogEntries = Catalog.GetAllEntries();
            foreach (var entry in catalogEntries)
            {
                if (items.Contains((entry.Page, entry.SlotPosition)))
                {
                    log.ErrorFormat($"two merchant items on same page/slot: listID={ItemsListID} page={entry.Page} slot={entry.SlotPosition}");
                    continue;
                }
                entry.Item.Price *= 2;
                items.Add((entry.Page, entry.SlotPosition), entry.Item);
            }
            return items;
        }

        public override ItemTemplate GetItem(int page, eMerchantWindowSlot slot)
        {
            var item = Catalog.GetPage(page).GetEntry((byte)slot).Item;
            item.Price *= 2;
            return item;
        }
    }
}