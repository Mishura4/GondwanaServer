using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.Language;
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
        ISpellHandler _subSpell;
        
        public TriggerSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            _subSpell = ScriptMgr.CreateSpellHandler(m_caster, SkillBase.GetSpellByID((int)m_spell.SubSpellID), null);
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            return 100;
        }
        public override bool CastSubSpells(GameLiving target)
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
                    // TODO: And then?
                    spellhandler.Parent = this;
                }
            }
            return true;
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

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.Trigger.MainDescription", Spell.Name) + "\n\n" + _subSpell.GetDelveDescription(delveClient);
        }
    }
}