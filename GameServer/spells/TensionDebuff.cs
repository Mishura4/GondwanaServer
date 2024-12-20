
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("TensionDebuff")]
    public class TensionDebuff : SingleStatDebuff
    {
        /// <inheritdoc />
        public TensionDebuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
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
            get => eBuffBonusCategory.Debuff;
        }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.TensionDebuff.MainDescription", LanguageMgr.GetProperty(delveClient, Property1), Spell.Value);
        }
    }
}
