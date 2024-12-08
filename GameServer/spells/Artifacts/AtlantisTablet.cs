using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("AtlantisTabletMorph")]
    public class AtlantisTabletMorph : AbstractMorphOffensiveProc
    {
        public AtlantisTabletMorph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
