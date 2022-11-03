using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
{
    [SpellHandler("TriggerBuff")]
    public class TriggerSpellHandler : SpellHandler
    {
        public TriggerSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override int CalculateToHitChance(GameLiving target)
        {
            return 100;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            effect.Owner.TempProperties.setProperty("TriggerSpell", Spell.Value);
            effect.Owner.TempProperties.setProperty("TriggerSubSpell", Spell.SubSpellID);
            effect.Owner.TempProperties.setProperty("TriggerSpellLevel", effect.SpellHandler.Caster.Level);
        }
    }
}