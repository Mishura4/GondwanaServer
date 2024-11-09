﻿
using DOL.Database.Attributes;


namespace DOL.Database
{
    /// <summary>
    /// ItemchargeXRaces
    /// </summary>
    [DataTable(TableName = "ItemchargeXRaces")]
    public class ItemchargeXRaces : DataObject
    {
        private string m_itemTemplate;
        private double m_unknownRace;
        private double m_britonRace;
        private double m_avalonianRace;
        private double m_highlanderRace;
        private double m_saracenRace;
        private double m_norsemanRace;
        private double m_trollRace;
        private double m_dwarfRace;
        private double m_koboldRace;
        private double m_celtRace;
        private double m_firbolgRace;
        private double m_elfRace;
        private double m_lurikeenRace;
        private double m_inconnuRace;
        private double m_valkynRace;
        private double m_sylvanRace;
        private double m_halfOgreRace;
        private double m_frostalfRace;
        private double m_sharRace;
        private double m_albionMinotaurRace;
        private double m_midgardMinotaurRace;
        private double m_hiberniaMinotaurRace;
        private bool m_isDurationMultiplied;
        private bool m_useStyleInsteadOfSpell;

        [DataElement(AllowDbNull = false)]
        public string ItemTemplate
        {
            get { return m_itemTemplate; }
            set
            {
                Dirty = true;
                m_itemTemplate = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double UnknownRace
        {
            get { return m_unknownRace; }
            set
            {
                Dirty = true;
                m_unknownRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double BritonRace
        {
            get { return m_britonRace; }
            set
            {
                Dirty = true;
                m_britonRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double AvalonianRace
        {
            get { return m_avalonianRace; }
            set
            {
                Dirty = true;
                m_avalonianRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double HighlanderRace
        {
            get { return m_highlanderRace; }
            set
            {
                Dirty = true;
                m_highlanderRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double SaracenRace
        {
            get { return m_saracenRace; }
            set
            {
                Dirty = true;
                m_saracenRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double NorsemanRace
        {
            get { return m_norsemanRace; }
            set
            {
                Dirty = true;
                m_norsemanRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double TrollRace
        {
            get { return m_trollRace; }
            set
            {
                Dirty = true;
                m_trollRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double DwarfRace
        {
            get { return m_dwarfRace; }
            set
            {
                Dirty = true;
                m_dwarfRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double KoboldRace
        {
            get { return m_koboldRace; }
            set
            {
                Dirty = true;
                m_koboldRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double CeltRace
        {
            get { return m_celtRace; }
            set
            {
                Dirty = true;
                m_celtRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double FirbolgRace
        {
            get { return m_firbolgRace; }
            set
            {
                Dirty = true;
                m_firbolgRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double ElfRace
        {
            get { return m_elfRace; }
            set
            {
                Dirty = true;
                m_elfRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double LurikeenRace
        {
            get { return m_lurikeenRace; }
            set
            {
                Dirty = true;
                m_lurikeenRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double InconnuRace
        {
            get { return m_inconnuRace; }
            set
            {
                Dirty = true;
                m_inconnuRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double ValkynRace
        {
            get { return m_valkynRace; }
            set
            {
                Dirty = true;
                m_valkynRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double SylvanRace
        {
            get { return m_sylvanRace; }
            set
            {
                Dirty = true;
                m_sylvanRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double HalfOgreRace
        {
            get { return m_halfOgreRace; }
            set
            {
                Dirty = true;
                m_halfOgreRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double FrostalfRace
        {
            get { return m_frostalfRace; }
            set
            {
                Dirty = true;
                m_frostalfRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double SharRace
        {
            get { return m_sharRace; }
            set
            {
                Dirty = true;
                m_sharRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double AlbionMinotaurRace
        {
            get { return m_albionMinotaurRace; }
            set
            {
                Dirty = true;
                m_albionMinotaurRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double MidgardMinotaurRace
        {
            get { return m_midgardMinotaurRace; }
            set
            {
                Dirty = true;
                m_midgardMinotaurRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double HiberniaMinotaurRace
        {
            get { return m_hiberniaMinotaurRace; }
            set
            {
                Dirty = true;
                m_hiberniaMinotaurRace = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsDurationMultiplied
        {
            get { return m_isDurationMultiplied; }
            set
            {
                Dirty = true;
                m_isDurationMultiplied = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool UseStyleInsteadOfSpell
        {
            get { return m_useStyleInsteadOfSpell; }
            set
            {
                Dirty = true;
                m_useStyleInsteadOfSpell = value;
            }
        }
    }
}