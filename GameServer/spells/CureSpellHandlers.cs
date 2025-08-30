using System;
using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    public static class CureSpellConstants
    {
        public static readonly List<string> CureDiseaseSpellTypes = new List<string> { "Disease" };
        public static readonly List<string> CurePoisonSpellTypes = new List<string> { "DamageOverTime", "StyleBleeding" };
        public static readonly List<string> CureNearsightSpellTypes = new List<string> { "Nearsight", "Silence" };
        public static readonly List<string> CureMezzSpellTypes = new List<string> { "Mesmerize" };
        public static readonly List<string> CurePetrifySpellTypes = new List<string> { "Petrify" };
        public static readonly List<string> MaidenKissSpellTypes = new List<string> { "WarlockSpeedDecrease" };
        public static readonly List<string> ArawnCureSpellTypes = new List<string>
        {
            "DamageOverTime",
            "Disease",
            "RvrResurrectionIllness",
            "PveResurrectionIllness",
            "StyleBleeding"
        };
        public static readonly List<string> CureAllSpellTypes = new List<string>
        {
            "DamageOverTime",
            "Nearsight",
            "Silence",
            "Disease",
            "StyleBleeding",
            "Mesmerize",
            "Petrify",
            "WarlockSpeedDecrease"
        };
    }

    [SpellHandler("CureAll")]
    public class CureAllSpellHandler : RemoveSpellEffectHandler
    {
        public CureAllSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = CureSpellConstants.CureAllSpellTypes;
        }
    }

    [SpellHandler("CureMezz")]
    public class CureMezzSpellHandler : RemoveSpellEffectHandler
    {
        public CureMezzSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = CureSpellConstants.CureMezzSpellTypes;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.CureMezz.MainDescription");
        }
    }

    [SpellHandler("CureDisease")]
    public class CureDiseaseSpellHandler : RemoveSpellEffectHandler
    {
        public CureDiseaseSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = CureSpellConstants.CureDiseaseSpellTypes;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.CureDisease.MainDescription");
        }
    }

    [SpellHandler("CurePoison")]
    public class CurePoisonSpellHandler : RemoveSpellEffectHandler
    {
        public CurePoisonSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = CureSpellConstants.CurePoisonSpellTypes;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.CurePoison.MainDescription");
        }
    }

    [SpellHandler("CureNearsight")]
    public class CureNearsightSpellHandler : RemoveSpellEffectHandler
    {
        public CureNearsightSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = CureSpellConstants.CureNearsightSpellTypes;
        }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.CureNearsight.MainDescription");
        }
    }

    [SpellHandlerAttribute("ArawnCure")]
    public class ArawnCureSpellHandler : RemoveSpellEffectHandler
    {
        public ArawnCureSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = CureSpellConstants.ArawnCureSpellTypes;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.ArawnCure.MainDescription");
        }
    }

    [SpellHandler("MaidenKiss")]
    public class MaidenKissSpellHandler : RemoveSpellEffectHandler
    {
        public MaidenKissSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            m_spellTypesToRemove = CureSpellConstants.MaidenKissSpellTypes;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            var wsd = SpellHandler.FindEffectOnTarget(target, "WarlockSpeedDecrease");
            if (wsd == null)
            {
                return true;
            }

            // Only cure if the morph is FROG (ResurrectMana == 0).
            int morphType = wsd.Spell?.ResurrectMana ?? 0;
            if (morphType == 0)
            {
                wsd.Cancel(false);
                return true;
            }

            return true;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.MaidenKiss.MainDescription");
        }
    }

    [SpellHandler("Unpetrify")]
    public class UnpetrifySpellHandler : RemoveSpellEffectHandler
    {
        public UnpetrifySpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            m_spellTypesToRemove = CureSpellConstants.CurePetrifySpellTypes;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.Unpetrify.MainDescription");
        }
    }
}