using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS
{
    class AdrenalineAbilityHandler : Ability
    {
        public AdrenalineAbilityHandler(DBAbility ability, int level) : base(ability, level)
        {
        }

        private SpellLine m_spellLine;

        public SpellLine SpellLine
        {
            get
            {
                m_spellLine ??= SkillBase.GetSpellLine("Adrenaline");
                return m_spellLine;
            }
        }

        /// <inheritdoc />
        public override void Execute(GameLiving living)
        {
            var player = living as GamePlayer;
            if (player == null) return;

            // Dynamically pick the adrenaline spell NOW (respects ChtonicShapeShift)
            Spell sp = ResolveAdrenalineSpell(player);
            if (sp == null) return;

            player.CastSpell(sp, SpellLine);
        }

        private static Spell ResolveAdrenalineSpell(GamePlayer player)
        {
            if (player.CharacterClass is CharacterClassBase ccb)
            {
                var dyn = ccb.GetAdrenalineSpell(player);
                if (dyn != null) return dyn;

                if (ccb.AdrenalineSpell != null) return ccb.AdrenalineSpell;
            }

            return player.CharacterClass.AdrenalineSpell;
        }
    }
}
