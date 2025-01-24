using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "eventXMoneyNpc")]
    public class MoneyNpcDb
        : DataObject
    {
        private string eventId;
        private long currentAmount;
        private long requiredMoney;
        private string mobId;
        private string mobName;
        private string needMoreMoneyText;
        private string validateText;
        private string interactText;
        private string requirementsReachedEmote;
        private int requirementsReachedSpellId;
        private string resource1;
        private string resource2;
        private string resource3;
        private string resource4;
        private int requiredResource1;
        private int requiredResource2;
        private int requiredResource3;
        private int requiredResource4;
        private int currentResource1;
        private int currentResource2;
        private int currentResource3;
        private int currentResource4;

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string MobID
        {
            get => mobId;

            set
            {
                mobId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string MobName
        {
            get => mobName;

            set
            {
                mobName = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string EventID
        {
            get => eventId;

            set
            {
                eventId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long RequiredMoney
        {
            get => requiredMoney;

            set
            {
                requiredMoney = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string NeedMoreMoneyText
        {
            get => needMoreMoneyText;

            set
            {
                needMoreMoneyText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string ValidateText
        {
            get => validateText;

            set
            {
                validateText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string RequirementsReachedEmote
        {
            get => requirementsReachedEmote;
            set
            {
                requirementsReachedEmote = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int RequirementsReachedSpellId
        {
            get => requirementsReachedSpellId;
            set
            {
                requirementsReachedSpellId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long CurrentAmount
        {
            get => currentAmount;

            set
            {
                currentAmount = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string InteractText
        {
            get => interactText;

            set
            {
                interactText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Resource1
        {
            get => resource1;

            set
            {
                resource1 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Resource2
        {
            get => resource2;

            set
            {
                resource2 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Resource3
        {
            get => resource3;

            set
            {
                resource3 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Resource4
        {
            get => resource4;

            set
            {
                resource4 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int RequiredResource1
        {
            get => requiredResource1;

            set
            {
                requiredResource1 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int RequiredResource2
        {
            get => requiredResource2;

            set
            {
                requiredResource2 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int RequiredResource3
        {
            get => requiredResource3;

            set
            {
                requiredResource3 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int RequiredResource4
        {
            get => requiredResource4;

            set
            {
                requiredResource4 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int CurrentResource1
        {
            get => currentResource1;

            set
            {
                currentResource1 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int CurrentResource2
        {
            get => currentResource2;

            set
            {
                currentResource2 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int CurrentResource3
        {
            get => currentResource3;

            set
            {
                currentResource3 = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public int CurrentResource4
        {
            get => currentResource4;

            set
            {
                currentResource4 = value;
                Dirty = true;
            }
        }
    }
}