using System;
using DOL.GS.Spells;
using DOL.Events;

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
                value += Math.Min(25, living.ItemBonus[(int)property]); // cap 25% from items
            }
            value -= living.DebuffCategory[eProperty.SpellShieldChance];
            return Math.Max(0, value); // Ensuring the gain is not negative
        }
    }

    public class ItemSpellShieldHandler
    {
        private static readonly int SpellID = 20629;
        private static readonly int CooldownTime = 2 * 60 * 1000;
        private static readonly string CooldownPropertyName = "SpellShieldCooldown";

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

                Spell spell = SkillBase.GetSpellByID(SpellID);
                SpellLine spellLine = SkillBase.GetSpellLine("Spell Shield Line");

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