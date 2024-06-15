using DOL.Database;
using DOL.GS;
using DOL.GS.Geometry;
using DOL.GS.Keeps;
using DOL.MobGroups;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Territories
{
    public class RvRTerritory
        : Territory
    {
        /// <inheritdoc />
        public RvRTerritory(Zone zone, List<IArea> areas, string name, GameNPC boss, Coordinate? portalCoordinate, ushort regionID, MobGroup group) : base(eType.Normal, zone, areas, name, boss, portalCoordinate, regionID, group)
        {
            //add new areas to region
            //only in memory
            if (WorldMgr.Regions.TryGetValue(this.RegionId, out Region region))
            {
                areas.ForEach(a => region.AddArea(a));
            }
        }

        protected override void SaveOriginalGuilds()
        {
            if (this.Mobs != null)
            {
                this.Mobs.ForEach(m => this.SaveMobOriginalGuildname(m));
            }
        }

        public override void SaveIntoDatabase()
        {
            //In memory RvR
            //No save allowed
        }

        protected override void SaveIntoDatabaseUnsafe()
        {
            //In memory RvR
            //No save allowed
        }

        public void Reset()
        {
            OwnerGuild = null;
            // reset keep
            AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(Boss.Position, 100000);
            keep.TempRealm = eRealm.None;
            keep.Reset(keep.TempRealm);
            // reset all doors
            foreach (GameKeepDoor door in keep.Doors.Values)
            {
                door.Reset((eRealm)6);
            }
            // reset all mobs
            foreach (GameNPC mob in Mobs)
            {
                mob.Realm = eRealm.None;
            }
        }
    }
}