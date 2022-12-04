using System;
using System.Reflection;
using DOL.Events;
using log4net;
using DOL.GS;
using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "DeathLog")]
    public class DBDeathLog : DataObject
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool exitFromJail;

        public DBDeathLog()
        {

        }

        public DBDeathLog(GameObject killed, GameObject killer, bool isWanted)
        {
            Killer = "(null)";
            KillerClass = "(null)";
            if (killer != null)
            {
                Killer = killer.Name;
                KillerClass = killer.GetType().ToString();
                KillerId = killer.InternalID;
            }
            Killed = killed.Name;
            KilledClass = killed.GetType().ToString();
            X = (int)killed.Position.X;
            Y = (int)killed.Position.Y;
            Region = killed.CurrentRegionID;
            DeathDate = DateTime.Now;
            IsWanted = isWanted;
            KilledId = killed.InternalID;
        }

        [PrimaryKey(AutoIncrement = true)]
        public long Id { get; set; }

        [DataElement(AllowDbNull = false)]
        public String Killer { get; set; }

        [DataElement(AllowDbNull = false)]
        public String KillerClass { get; set; }

        [DataElement(AllowDbNull = false)]
        public String Killed { get; set; }

        [DataElement(AllowDbNull = false)]
        public String KillerId { get; set; }

        [DataElement(AllowDbNull = false, Index = true)]
        public String KilledId { get; set; }


        [DataElement(AllowDbNull = false)]
        public String KilledClass { get; set; }

        [DataElement(AllowDbNull = false)]
        public int X { get; set; }

        [DataElement(AllowDbNull = false)]
        public int Y { get; set; }

        [DataElement(AllowDbNull = false)]
        public int Region { get; set; }

        [DataElement(AllowDbNull = false, Index = true)]
        public DateTime DeathDate { get; set; }

        [DataElement(AllowDbNull = false)]
        public bool IsWanted { get; set; }

        [DataElement(AllowDbNull = false)]
        public bool ExitFromJail { get { return exitFromJail; } set { Dirty = true; exitFromJail = value; } }

        #region Init
        private static bool Loaded = false;

        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {
            if (Loaded)
                return;
            GameServer.Database.RegisterDataObject(typeof(DBDeathLog));
            Loaded = true;
            log.Info("DATABASE DBDeathLog LOADED");
        }
        #endregion
    }
}