using DOL.Database;
using DOL.GS;
using DOL.GS.RealmAbilities;
using DOL.GS.Spells;
using DOL.Language;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// RR5: Call of Shadows (10 min reuse, 30s duration)
    /// </summary>
    public class CallOfShadowsAbility : RR5RealmAbility
    {
        private DBSpell _dbspell;
        private Spell _spell;
        private SpellLine _spellline;
        private GamePlayer _player;

        public CallOfShadowsAbility(DBAbility dba, int level) : base(dba, level)
        {
            BuildSpell();
        }

        private void BuildSpell()
        {
            _dbspell = new DBSpell
            {
                Name = "Call of Shadows",
                Icon = 7051,
                ClientEffect = 15184,
                Target = "self",
                Type = "CallOfShadows",
                Duration = 30,
                CastTime = 0,
                MoveCast = false,
                Uninterruptible = false,
                Range = 0
            };

            _spell = new Spell(_dbspell, 0);
            _spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            _player = living as GamePlayer;

            BuildSpell();

            var handler = ScriptMgr.CreateSpellHandler(_player, _spell, _spellline) as CallOfShadowsSpellHandler;
            handler?.StartSpell(_player);
        }

        public override int GetReUseDelay(int level) => 600;

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Become a demon of the void for 30 seconds.");
            list.Add("");
            list.Add("You gain the benefits of both Decrepit and Chthonic forms, with their bonuses increased by 10%.");
            list.Add("You inflict a wasting disease on the target, slowing its movement.");
            list.Add("Pets become more powerful, as if under Spirit form.");
            list.Add("");
            list.Add("Your spells cannot be interrupted.");
        }
    }
}
