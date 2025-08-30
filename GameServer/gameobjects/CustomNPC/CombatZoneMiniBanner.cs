using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class CombatZoneMiniBanner : GameStaticItem
    {
        public Guild OwningGuild { get; set; }
        public int OriginalEmblem { get; set; }

        public static CombatZoneMiniBanner Create(Guild owningGuild, int emblem)
        {
            return new CombatZoneMiniBanner
            {
                Model = 3223,
                Name = "Boundary Banner of " + owningGuild.Name,
                OwningGuild = owningGuild,
                OriginalEmblem = emblem,
                Emblem = emblem
            };
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            OriginalEmblem = Emblem;
        }
    }
}
