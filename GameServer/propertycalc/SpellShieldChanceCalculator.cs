using System;
using DOL.GS.Spells;
using DOL.Events;
using DOL.Database;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// Calculator for SpellShieldChance
    /// </summary>
    [PropertyCalculator(eProperty.SpellShieldChance)]
    public class SpellShieldChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int value = living.BuffBonusCategory4[eProperty.SpellShieldChance];
            if (living is GamePlayer)
            {
                value += Math.Min(20, living.ItemBonus[(int)property]); // cap 20% from items
            }
            value -= living.DebuffCategory[eProperty.SpellShieldChance];
            return Math.Max(0, value); // Ensuring the gain is not negative
        }
    }

    public class ItemSpellShieldHandler
    {
        private static readonly int CooldownTime = 2 * 60 * 1000;
        private static readonly string CooldownPropertyName = "SpellShieldCooldown";

        private static Spell _spellShieldSpell;

        public static Spell SpellShieldSpell
        {
            get
            {
                if (_spellShieldSpell == null)
                {
                    DBSpell spell = new DBSpell
                    {
                        CastTime = 0,
                        ClientEffect = 15217,
                        Icon = 12031,
                        Description = "Absorbs 100% of spell damage when the player's health is at 15% or below.",
                        Name = "Spell Shield",
                        Power = -5,
                        Range = 0,
                        Damage = 0,
                        DamageType = (int)eDamageType.Natural,
                        SpellID = 20629,
                        Target = "self",
                        Type = "SpellShield",
                        Duration = 90,
                        RecastDelay = 120, // 2 minutes
                    };

                    _spellShieldSpell = new Spell(spell, 70);
                    SkillBase.GetSpellList(GlobalSpellsLines.Item_Spells).Add(_spellShieldSpell);
                }
                return _spellShieldSpell;
            }
        }

        public static void ApplyEffect(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs args)
                return;

            GameLiving living = sender as GameLiving;
            if (living == null)
                return;

            if (living.HealthPercent > 30)
                return;

            if (living.TempProperties.getProperty(CooldownPropertyName, false))
                return;

            AttackData ad = args.AttackData;
            if (ad is { AttackType: AttackData.eAttackType.Spell or AttackData.eAttackType.DoT })
            {
                int chanceToShield = living.GetModified(eProperty.SpellShieldChance);

                if (!Util.Chance(chanceToShield))
                    return;

                // Cast the internally defined spell
                Spell spell = SpellShieldSpell;
                SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Spells);

                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(living, spell, spellLine);
                spellHandler.StartSpell(living);

                // Set the cooldown using RegionTimer
                living.TempProperties.setProperty(CooldownPropertyName, true);
                RegionTimer cooldownTimer = new RegionTimer(living, new RegionTimerCallback(CooldownExpired));
                cooldownTimer.Properties.setProperty("living", living);
                cooldownTimer.Start(CooldownTime);
            }
        }

        private static int CooldownExpired(RegionTimer timer)
        {
            GameLiving living = timer.Properties.getProperty<GameLiving>("living");
            if (living != null)
            {
                living.TempProperties.removeProperty(CooldownPropertyName);
            }
            return 0;
        }
    }
}