using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Geometry;

namespace DOL.GS.Scripts
{
    public class PlaceAssise : GameBoat
    {
        public override int MAX_PASSENGERS
        {
            get { return 1; }
        }

        public override int SLOT_OFFSET
        {
            get { return 1; }
        }

        public override ushort Type()
        {
            return 2;
        }

        public PlaceAssise()
        {
            Realm = eRealm.None;
            Flags = eFlags.PEACE;
            Model = 1293;
            MaxSpeedBase = 0;
            Level = 0;
            Name = "Place assise"; ;
            SetOwnBrain(new BlankBrain());
        }

        public override bool Interact(GamePlayer player)
        {
            if (!IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Items.Specialitems.PlaceAssiseTooFar"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (GetFreeArrayLocation() == -1)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,"Items.Specialitems.PlaceAssiseTaken"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else
                player.MountSteed(this, true);

            return true;
        }

        #region Database
        public override void LoadFromDatabase(DataObject obj)
        {
            InternalID = obj.ObjectId;

            if (!(obj is Mob)) return;
            Mob npc = (Mob)obj;
            Name = npc.Name;
            GuildName = npc.Guild;
            Position = Position.Create(npc.Region, npc.X, npc.Y, npc.Z, (ushort)(npc.Heading & 0xFFF));
            m_maxSpeedBase = (short)npc.Speed;  // TODO db has currently senseless information here, mob type db required
            if (m_maxSpeedBase == 0)
                m_maxSpeedBase = 600;
            m_currentSpeed = 0;
            CurrentRegionID = npc.Region;
            Realm = (eRealm)npc.Realm;
            Model = npc.Model;
            Size = npc.Size;
            Level = npc.Level;
            Flags = eFlags.PEACE;
        }

        public override void SaveIntoDatabase()
        {
            Mob mob = null;
            if (InternalID != null)
                mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);
            if (mob == null)
                mob = new Mob();

            mob.Name = Name;
            mob.Guild = GuildName;
            mob.X = (int)Position.X;
            mob.Y = (int)Position.Y;
            mob.Z = (int)Position.Z;
            mob.Heading = Position.Orientation.InHeading;
            mob.Speed = MaxSpeedBase;
            mob.Region = CurrentRegionID;
            mob.Realm = (byte)Realm;
            mob.Model = Model;
            mob.Size = Size;
            mob.Level = Level;
            mob.ClassType = GetType().ToString();

            if (InternalID == null)
            {
                GameServer.Database.AddObject(mob);
                InternalID = mob.ObjectId;
            }
            else
                GameServer.Database.SaveObject(mob);
        }
        #endregion
    }
}
