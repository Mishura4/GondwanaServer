using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("Unpetrify")]
    public class UnpetrifySpellHandler : RemoveSpellEffectHandler
    {
        public UnpetrifySpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            m_spellTypesToRemove = new List<string> { "Petrify" };
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.Unpetrify.MainDescription");
        }
    }
}