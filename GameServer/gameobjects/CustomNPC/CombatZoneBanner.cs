using DOL.Database;
using DOL.Events;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class CombatZoneBanner
        : GameStaticItem
    {
        public int OriginalEmblem { get; private set; }

        public Guild OwningGuild { get; private set; }

        public AbstractArea CombatArea { get; private set; }
        public List<CombatZoneMiniBanner> MiniBanners { get; } = new List<CombatZoneMiniBanner>();

        public CombatZoneBanner()
            : base()
        {
        }

        public static CombatZoneBanner Create(Guild owningGuild, AbstractArea area)
        {
            CombatZoneBanner ret = new CombatZoneBanner();

            ret.Model = 679;
            ret.Name = "Banner of " + owningGuild.Name;
            ret.OwningGuild = owningGuild;
            ret.CombatArea = area;
            ret.OriginalEmblem = owningGuild.Emblem;
            ret.Emblem = owningGuild.Emblem;
            return ret;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            this.OriginalEmblem = this.Emblem;
        }

        public override void Delete()
        {
            if (MiniBanners != null)
            {
                foreach (var mini in MiniBanners)
                {
                    try
                    {
                        mini.RemoveFromWorld(0);
                        mini.ObjectState = eObjectState.Deleted;
                    }
                    catch { /* ignore */ }
                }
                MiniBanners.Clear();
            }

            Notify(GameObjectEvent.Delete, this);
            RemoveFromWorld(0); // will not respawn
            ObjectState = eObjectState.Deleted;
            CurrentRegion.RemoveArea(CombatArea);
        }
    }
}
