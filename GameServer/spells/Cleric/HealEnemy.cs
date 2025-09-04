using System;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.AI.Brain;
using DOL.Language;
using DOL.GS.ServerProperties;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Heals the TARGET even if it is an ENEMY. Designed for use as a sub-spell in Offensive procs.
    /// Keeps all the base Heal math (variance, disease, heal debuffs, Damnation), but
    /// bypasses same-realm checks and avoids RP bonuses by routing via ProcHeal(...).
    /// Configure your sub-spell with Target="Enemy".
    /// </summary>
    [SpellHandler("HealEnemy")]
    public class HealEnemySpellHandler : HealSpellHandler
    {
        public HealEnemySpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Override the final per-target heal to allow hostile healing and avoid RP awards.
        /// We deliberately use ProcHeal(...) instead of HealTarget(...) to skip realm gating and RP gain.
        /// ExecuteSpell in the base class still computes heal variance, disease reductions, Damnation, crit, etc.
        /// </summary>
        public override bool HealTarget(GameLiving target, double amount)
        {
            return ProcHeal(target, amount);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string lang = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            // Reuse generic Heal description but mention hostile-heal intent.
            string baseDesc = LanguageMgr.GetTranslation(lang, "SpellDescription.Heal.MainDescription", Spell.Value);
            return baseDesc + "\n\n" + LanguageMgr.GetTranslation(lang, "SpellDescription.Custom.HostileHeal", "Heals your enemy on hit (proc).");
        }
    }
}