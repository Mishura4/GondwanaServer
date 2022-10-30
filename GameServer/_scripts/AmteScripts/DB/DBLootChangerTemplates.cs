using System;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;

namespace DOL.Database
{
    [DataTable(TableName = "LootChangerTemplate")]
    public class DBLootChangerTemplate : DataObject
    {
        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameServer.Database.RegisterDataObject(typeof(DBLootChangerTemplate));
        }

        protected string m_LootChangerTemplateName = "";
        protected string m_ItemsTemplatesRecvs = "";
        protected string m_ItemsTemplatesGives = "";
        protected int m_DropChance = 1;
        private string m_FamilyName;
        private short m_Order;

// ReSharper disable EmptyConstructor
        public DBLootChangerTemplate()
        {
        }
// ReSharper restore EmptyConstructor

        [DataElement(AllowDbNull = false)]
        public string LootChangerTemplateName
        {
            get { return m_LootChangerTemplateName; }
            set
            {
                Dirty = true;
                m_LootChangerTemplateName = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string ItemsTemplatesRecvs
        {
            get { return m_ItemsTemplatesRecvs; }
            set
            {
                Dirty = true;
                m_ItemsTemplatesRecvs = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public string ItemsTemplatesGives
        {
            get { return m_ItemsTemplatesGives; }
            set
            {
                Dirty = true;
                m_ItemsTemplatesGives = value;
            }
        }

        [DataElement(AllowDbNull = true)]
        public string FamilyName
        {
            get { return m_FamilyName; }
            set
            {
                Dirty = true;
                m_FamilyName = value;
            }
        }

        [DataElement(AllowDbNull = true)]
        public short Order
        {
            get { return m_Order; }
            set
            {
                Dirty = true;
                m_Order = value;
            }
        }
    }
}
