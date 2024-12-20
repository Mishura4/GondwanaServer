using DOL.gameobjects.CustomNPC;
using DOL.GS.ServerProperties;
using DOL.Language;


namespace DOL.GS.Spells
{
    [SpellHandler("Teleport")]
    public class TeleportSpellHandler : SpellHandler
    {
        string zoneName;
        public TeleportSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            TPPoint tPPoint = TeleportMgr.LoadTP((ushort)Spell.LifeDrainReturn);
            zoneName = WorldMgr.GetRegion(tPPoint.Region).GetZone(tPPoint.Position.Coordinate).Description;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is ShadowNPC)
                return false;

            if (Spell.LifeDrainReturn <= 0)
            {
                return false;
            }
            
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
            target.MoveTo(tPPoint.Position.With(target.Orientation));
            return true;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Teleport.MainDescription", Spell.Name, zoneName);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }

    }
}