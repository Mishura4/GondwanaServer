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
        string subSpellDescription;
        public TriggerSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            subSpellDescription = ScriptMgr.CreateSpellHandler(m_caster, SkillBase.GetSpellByID((int)m_spell.SubSpellID), null).ShortDescription;
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            return 100;
        }
        public override void CastSubSpells(GameLiving target)
        {
            List<int> subSpellList = new List<int>();
            if (m_spell.SubSpellID > 0)
                subSpellList.Add(m_spell.SubSpellID);

            foreach (int spellID in subSpellList.Union(m_spell.MultipleSubSpells))
            {
                Spell spell = SkillBase.GetSpellByID(spellID);
                //we need subspell ID to be 0, we don't want spells linking off the subspell
                if (target != null && spell != null)
                {
                    ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(m_caster, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler.Parent = this;
                }
            }
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            if (Spell.SubSpellID == compare.Spell.SubSpellID)
                return true;
            return false;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            effect.Owner.TempProperties.setProperty("TriggerSpell", Spell.Value);
            effect.Owner.TempProperties.setProperty("TriggerSubSpell", Spell.SubSpellID);
            effect.Owner.TempProperties.setProperty("TriggerSpellLevel", effect.SpellHandler.Caster.Level);
        }

        public override string ShortDescription
            => $"Generates a magic Proc as a buff on the target. {Spell.Name} gets triggered when the target is hit.\n{subSpellDescription}";
    }
}