using System;
using DOL.GS;
using DOL.GS.Effects;
using DOL.Database;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("Dread")]
    public class DreadSpellHandler : DoTSpellHandler
    {
        public DreadSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    }

    [SpellHandler("Anguish")]
    public class AnguishSpellHandler : DoTSpellHandler
    {
        public AnguishSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    }

    [SpellHandler("Agony")]
    public class AgonySpellHandler : DoTSpellHandler
    {
        public AgonySpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string description = LanguageMgr.GetTranslation(delveClient, "SpellDescription.DoT.MainDescription", Spell.Damage, LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType), Spell.Frequency / 1000.0) + "\n\n" + LanguageMgr.GetTranslation(delveClient, "SpellDescription.Agony.ConditionDescription");

            if (Spell.SubSpellID != 0)
            {
                Spell subSpell = SkillBase.GetSpellByID((int)Spell.SubSpellID);
                if (subSpell != null)
                {
                    ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(m_caster, subSpell, null);
                    if (subSpellHandler != null)
                    {
                        string subspelldesc = subSpellHandler.GetDelveDescription(delveClient);
                        description += "\n\n" + subspelldesc;
                    }
                }
            }

            if (Spell.IsSecondary)
            {
                string secondaryMessage = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Warlock.SecondarySpell");
                if (!string.IsNullOrEmpty(secondaryMessage))
                    description += "\n\n" + secondaryMessage;
            }

            if (Spell.IsPrimary)
            {
                string secondaryMessage = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Warlock.PrimarySpell");
                description += "\n\n" + secondaryMessage;
            }

            return description;
        }
    }

    [SpellHandler("Doom")]
    public class DoomSpellHandler : DoTSpellHandler
    {
        private const int DoomEndSpellID = 25251;

        public DoomSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            int result = base.OnEffectExpires(effect, noMessages);

            if (effect.CancelledByPurge)
                return result;

            Spell subSpell = LoadSubSpellFromDatabase() ?? CreateFallbackSubSpell();

            SpellLine tempSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            ISpellHandler subspellHandler = ScriptMgr.CreateSpellHandler(Caster, subSpell, tempSpellLine);

            if (subspellHandler != null)
            {
                subspellHandler.StartSpell(effect.Owner);
            }

            return result;
        }

        /// <summary>
        /// Attempts to load the Doom's End spell from the database first
        /// </summary>
        private Spell LoadSubSpellFromDatabase()
        {
            DBSpell dbSpell = GameServer.Database.SelectObject<DBSpell>(s => s.SpellID == DoomEndSpellID);
            if (dbSpell != null)
            {
                return new Spell(dbSpell, Spell.Level);
            }

            return null;
        }

        /// <summary>
        /// Fallback: Creates Doom's End spell directly from script if database entry is missing
        /// </summary>
        private Spell CreateFallbackSubSpell()
        {
            var fallbackDbSpell = new DBSpell
            {
                SpellID = DoomEndSpellID,
                ClientEffect = 3487,
                Icon = 3487,
                Name = "Doom's End",
                Description = "Damages the target.",
                Target = "enemy",
                Range = 1500,
                Power = 0,
                CastTime = 0,
                Damage = 475,
                DamageType = (int)eDamageType.Spirit,
                Type = "DirectDamage",
                Duration = 0,
                Frequency = 0,
                Pulse = 0,
                Radius = 0,
                RecastDelay = 0,
                Value = 0
            };

            Spell fallbackSpell = new Spell(fallbackDbSpell, Spell.Level);
            SkillBase.GetSpellList(GlobalSpellsLines.Reserved_Spells).Add(fallbackSpell);

            return fallbackSpell;
        }
        public override string GetDelveDescription(GameClient delveClient)
        {
            string description = LanguageMgr.GetTranslation(delveClient, "SpellDescription.DoT.MainDescription", Spell.Damage, LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType), Spell.Frequency / 1000.0) + "\n\n" + LanguageMgr.GetTranslation(delveClient, "SpellDescription.Doom.ConditionDescription");

            Spell doomEndSpell = LoadSubSpellFromDatabase() ?? CreateFallbackSubSpell();
            string doomEndDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.DirectDamage.MainDescription", doomEndSpell.Damage, LanguageMgr.GetDamageOfType(delveClient, doomEndSpell.DamageType));

            description += "\n\n" + LanguageMgr.GetTranslation(delveClient, "SpellDescription.Doom.Expires") + "\n" + doomEndDesc;

            return description;
        }
    }
}
