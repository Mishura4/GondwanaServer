using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("SpellReflectionDebuff")]
    public class SpellReflectionDebuff : RemoveSpellEffectHandler
    {
        public SpellReflectionDebuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            m_spellTypesToRemove = new List<string> { "SpellReflection" };
        }

        /// <inheritdoc />
        public override bool HasPositiveEffect => false;

        public override string ShortDescription
        {
            get
            {
                string language = Properties.SERV_LANGUAGE;
                return LanguageMgr.GetTranslation(language, "SpellDescription.SpellReflectionDebuff.MainDescription");
            }
        }
    }
}