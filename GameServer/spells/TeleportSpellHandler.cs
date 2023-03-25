using DOL.gameobjects.CustomNPC;


namespace DOL.GS.Spells
{
    [SpellHandler("Teleport")]
    public class TeleportSpellHandler : SpellHandler
    {
        string zoneName;
        public TeleportSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            TPPoint tPPoint = TeleportMgr.LoadTP((ushort)Spell.LifeDrainReturn);
            zoneName = WorldMgr.GetRegion(tPPoint.Region).GetZone((float)tPPoint.Position.X, (float)tPPoint.Position.Z).Description;
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is ShadowNPC)
                return;
            if (Spell.LifeDrainReturn > 0)
            {
                TPPoint tPPoint = TeleportMgr.LoadTP((ushort)Spell.LifeDrainReturn);
                if (target.TPPoint != null && target.TPPoint.DbTPPoint.TPID == tPPoint.DbTPPoint.TPID)
                {
                    tPPoint = target.TPPoint.GetNextTPPoint();
                }
                else
                {
                    switch (tPPoint.Type)
                    {
                        case Database.eTPPointType.Random:
                            tPPoint = tPPoint.GetNextTPPoint();
                            break;
                        case Database.eTPPointType.Smart:
                            tPPoint = tPPoint.GetSmarttNextPoint();
                            break;
                    }
                }
                target.TPPoint = tPPoint;
                target.MoveTo(tPPoint.Region, (float)tPPoint.Position.X, (float)tPPoint.Position.Y, (float)tPPoint.Position.Z, target.GetHeading(target));
            }
        }
        public override string ShortDescription
            => $"{Spell.Name} Teleports the target to {zoneName}.";

    }
}