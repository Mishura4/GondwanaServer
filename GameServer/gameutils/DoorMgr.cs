/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

using DOL.Database;
using DOL.GS.Geometry;
using DOL.GS.Keeps;

using log4net;

namespace DOL.GS
{
    /// <summary>
    /// DoorMgr is manager of all door regular door and keep door
    /// </summary>
    public sealed class DoorMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly object Lock = new object();

        private static Dictionary<int, List<IDoor>> m_doors = new Dictionary<int, List<IDoor>>();

        public const string WANT_TO_ADD_DOORS = "WantToAddDoors";

        /// <summary>
        /// this function load all door from DB
        /// </summary>
        public static bool Init()
        {
            var dbdoors = GameServer.Database.SelectAllObjects<DBDoor>();
            foreach (DBDoor door in dbdoors)
            {
                if (!LoadDoor(door))
                {
                    log.Error("Unable to load door id " + door.ObjectId + ", correct your database");
                    // continue loading, no need to stop server for one bad door!
                }
            }
            return true;
        }

        public static bool LoadDoor(DBDoor door)
        {
            IDoor mydoor = null;
            ushort zone = (ushort)(door.InternalID / 1000000);

            Zone currentZone = WorldMgr.GetZone(zone);
            if (currentZone == null) return false;

            //check if the door is a keep door
            foreach (AbstractArea area in currentZone.GetAreasOfSpot(Coordinate.Create(door.X, door.Y, door.Z)))
            {
                if (area is KeepArea)
                {
                    mydoor = new GameKeepDoor();
                    mydoor.LoadFromDatabase(door);
                    break;
                }
            }

            //if the door is not a keep door, create a standard door
            if (mydoor == null)
            {
                mydoor = new GameDoor();
                mydoor.LoadFromDatabase(door);
            }

            //add to the list of doors
            if (mydoor != null)
            {
                RegisterDoor(mydoor);
            }

            return true;
        }

        public static void RegisterDoor(IDoor door)
        {
            lock (Lock)
            {
                if (!m_doors.ContainsKey(door.DoorID))
                {
                    List<IDoor> createDoorList = new List<IDoor>();
                    m_doors.Add(door.DoorID, createDoorList);
                }

                List<IDoor> addDoorList = m_doors[door.DoorID];
                addDoorList.Add(door);
            }
        }

        public static void UnRegisterDoor(int doorID)
        {
            if (m_doors.ContainsKey(doorID))
            {
                m_doors.Remove(doorID);
            }
        }

        /// <summary>
        /// This function get the door object by door index
        /// </summary>
        /// <returns>return the door with the index</returns>
        public static List<IDoor> getDoorByID(int id)
        {
            if (m_doors.ContainsKey(id))
            {
                return m_doors[id];
            }
            else
            {
                return new List<IDoor>();
            }
        }

        public static List<GameDoor> GetDoorsBySwitchFamily(string switchFamily)
        {
            List<GameDoor> doors = new List<GameDoor>();

            foreach (var doorList in m_doors.Values)
            {
                foreach (var door in doorList)
                {
                    if (door is GameDoor gameDoor && gameDoor.SwitchFamily == switchFamily)
                    {
                        doors.Add(gameDoor);
                    }
                }
            }

            return doors;
        }

        public static void UnlockDoorsBySwitchFamily(string switchFamily)
        {
            var doorsToUnlock = GetDoorsBySwitchFamily(switchFamily);
            foreach (var door in doorsToUnlock)
                if (door is GameDoor gameDoor)
                {
                    gameDoor.UnlockBySwitch();
                }
        }
    }
}
