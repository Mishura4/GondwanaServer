
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("TensionBuff")]
    public class TensionBuff : SingleStatBuff
    {
        /// <inheritdoc />
        public TensionBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        /// <inheritdoc />
        public override eProperty Property1
        {
            get => eProperty.MythicalTension;
        }

        /// <inheritdoc />
        public override eBuffBonusCategory BonusCategory1
        {
            get => eBuffBonusCategory.Other;
        }

        public override string ShortDescription
        {
            get
            {
                string language = Properties.SERV_LANGUAGE;
                string propName = ConvertPropertyToText(Property1).ToLower();
                return LanguageMgr.GetTranslation(language, "SpellDescription.TensionBuff.MainDescription", propName, Spell.Value);
            }
        }
    }
}
