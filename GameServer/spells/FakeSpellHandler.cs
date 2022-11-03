using DOL.GS;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Use it to call only subspell and dislay additional information in delv info
    /// </summary>
    [SpellHandler("AvaloniaFake")]
    public class FakeSpellHandler : SpellHandler
    {
        public FakeSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override bool StartSpell(GameLiving target)
        {
            CastSubSpells(target);
            return true;
        }
    }

    [SpellHandler("AllStatsBuffItem")]
    public class AllStatsBuffItemSpellHandler : FakeSpellHandler
    {
        public AllStatsBuffItemSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }

    [SpellHandler("AllStatsDebuff")]
    public class AllStatsDebuffSpellHandler : FakeSpellHandler
    {
        public AllStatsDebuffSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }

    [SpellHandler("AllResistsBuff")]
    public class AllResistsBuffSpellHandler : FakeSpellHandler
    {
        public AllResistsBuffSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }

    [SpellHandler("AllResistsDebuff")]
    public class AllResistsDebuffSpellHandler : FakeSpellHandler
    {
        public AllResistsDebuffSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }

    [SpellHandler("Supremacy")]
    public class SupremacySpellHandler : FakeSpellHandler
    {
        public SupremacySpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }

    [SpellHandler("Omniregen")]
    public class OmniregenSpellHandler : FakeSpellHandler
    {
        public OmniregenSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }
}