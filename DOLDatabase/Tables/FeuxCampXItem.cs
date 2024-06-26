﻿using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Database
{
    [DataTable(TableName = "FeuxCampXItem")]
    public class FeuxCampXItem : DataObject
    {
        private string m_feuxCampXItemId_nb;
        private int m_realm;
        private int m_radius;
        private int m_enduranceRatePercent;
        private int m_lifetime;
        private bool m_isHealthType;
        private bool m_isManaType;
        private bool m_isHealthTrapType;
        private bool m_isEnduranceType;
        private bool m_isManaTrapType;
        private int m_manaTrapDamagePercent;
        private int m_healthTrapDamagePercent;
        private int m_healthRatePercent;
        private int m_manaRatePercent;
        private bool m_ownerImmuneToTrap;

        public FeuxCampXItem()
        {
            AllowAdd = true;
        }

        [PrimaryKey(AutoIncrement = true)]
        public string FeuxCampXItem_ID
        {
            get;
            set;
        }

        /// <summary>
        /// the index
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public string FeuxCampItemId_nb
        {
            get
            {
                return m_feuxCampXItemId_nb;
            }

            set
            {
                Dirty = true;
                m_feuxCampXItemId_nb = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Realm // New property for Realm
        {
            get
            {
                return m_realm;
            }

            set
            {
                Dirty = true;
                m_realm = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Radius
        {
            get
            {
                return m_radius;
            }

            set
            {
                Dirty = true;
                m_radius = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int EnduranceRatePercent
        {
            get
            {
                return m_enduranceRatePercent;
            }

            set
            {
                Dirty = true;
                m_enduranceRatePercent = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int HealthRatePercent
        {
            get
            {
                return m_healthRatePercent;
            }

            set
            {
                Dirty = true;
                m_healthRatePercent = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int ManaRatePercent
        {
            get
            {
                return m_manaRatePercent;
            }

            set
            {
                Dirty = true;
                m_manaRatePercent = value;
            }
        }


        [DataElement(AllowDbNull = false)]
        public int Lifetime
        {
            get
            {
                return m_lifetime;
            }

            set
            {
                Dirty = true;
                m_lifetime = value;
            }
        }


        [DataElement(AllowDbNull = true)]
        public int HealthTrapDamagePercent
        {
            get
            {
                return m_healthTrapDamagePercent;
            }

            set
            {
                Dirty = true;
                m_healthTrapDamagePercent = value;
            }
        }


        [DataElement(AllowDbNull = true)]
        public int ManaTrapDamagePercent
        {
            get
            {
                return m_manaTrapDamagePercent;
            }

            set
            {
                Dirty = true;
                m_manaTrapDamagePercent = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool OwnerImmuneToTrap
        {
            get
            {
                return m_ownerImmuneToTrap;
            }

            set
            {
                Dirty = true;
                m_ownerImmuneToTrap = value;
            }
        }
    }
}