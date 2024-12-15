using DOL.Database;
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

        protected override bool ExecuteSpell(GameLiving target, bool force = false)
        {
            return CastSubSpells(target);;
        }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            string shortDescription = "";
            var spell = Spell;
            while (spell.SubSpellID != 0)
            {
                spell = SkillBase.GetSpellByID((int)spell.SubSpellID);
                shortDescription += ScriptMgr.CreateSpellHandler(m_caster, spell, null).GetDelveDescription(delveClient) + "\n";
            }
            return base.GetDelveDescription(delveClient);
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