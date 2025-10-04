using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using log4net;
using System;

namespace DOL.GS.Spells
{
    public class BringerOfDeathSpellEffect : GameSpellEffect
    {
        public BringerOfDeathSpellEffect(ISpellHandler handler, int duration, int pulseFreq)
            : base(handler, duration, pulseFreq) { }

        public BringerOfDeathSpellEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(handler, duration, pulseFreq, effectiveness) { }
    }

    [SpellHandler("BringerOfDeath")]
    public class BringerOfDeath : AbstractMorphSpellHandler
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private const string BOD_FLAG_ACTIVE = "BOD_ACTIVE";
        private const string BOD_FLAG_UNINTERRUPTIBLE = "BOD_UNINTERRUPTIBLE";
        private const string BOD_FLAG_MOVECAST = "BOD_CAN_MOVECAST";
        private const string BOD_PREV_SPELL = "BOD_PREV_SPELL";

        // TODO: Why not just use sub spell db field?
        private const int DISEASE_SUBSPELL_ID = 25296;
        private const int DISEASE_PROC_CHANCE = 50;

        private int _absorbPct;
        private int _meleeSpeedBonus;
        private bool _hookedEvents;
        private float _regenMult;
        private int _spellDmgPct;

        public BringerOfDeath(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            Priority = 11;

            // --- Melee speed (haste) from Spell.Value ---
            _meleeSpeedBonus = Math.Max(0, (int)Spell.Value);
            
            // --- Magical-only potency: write NEGATIVE bonuses so calculators reduce output ---
            _spellDmgPct = PotencyToNegativeBonus();
            
            // --- Regen multiplier (Decrepit-style) ---
            _regenMult = ((float)Spell.ResurrectMana / 100);

            _absorbPct = Math.Max(0, (int)Spell.AmnesiaChance);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new BringerOfDeathSpellEffect(this, CalculateEffectDuration(target, effectiveness), 0);
        }

        // Morph model comes from DB
        public override ushort GetModelFor(GameLiving living) => (ushort)Spell.LifeDrainReturn;

        private int PotencyToNegativeBonus()
        {
            int potency = Math.Max(0, Math.Min(100, (int)Spell.Damage));
            return potency - 100;
        }

        protected void SetFormProperties(GameLiving living, bool apply)
        {
            // Mark + rules (your move-cast code elsewhere already keys off these)
            if (apply)
            {
                living.TempProperties.setProperty(BOD_FLAG_ACTIVE, true);
                living.TempProperties.setProperty(BOD_FLAG_UNINTERRUPTIBLE, true);
                living.TempProperties.setProperty(BOD_FLAG_MOVECAST, true);
            }
            else
            {
                living.TempProperties.removeProperty(BOD_FLAG_ACTIVE);
                living.TempProperties.removeProperty(BOD_FLAG_UNINTERRUPTIBLE);
                living.TempProperties.removeProperty(BOD_FLAG_MOVECAST);
            }
        }

        protected void ApplyFormEffects(GameLiving living, bool apply)
        {
            var mult = (sbyte)(apply ? 1 : -1);
            
            if (_meleeSpeedBonus > 0)
                living.BaseBuffBonusCategory[(int)eProperty.MeleeSpeed] += _meleeSpeedBonus * mult;

            if (Math.Abs(_regenMult) > 0.0001f)
                living.BuffBonusMultCategory1.Set((int)eProperty.HealthRegenerationRate, this, _regenMult * mult);

            if (_spellDmgPct != 0)
            {
                living.BaseBuffBonusCategory[(int)eProperty.SpellDamage] += _spellDmgPct * mult;
                living.BaseBuffBonusCategory[(int)eProperty.DotDamageBonus] += _spellDmgPct * mult;
            }

            if (apply)
            {
                // --- Defensive disease proc on incoming physical hits ---
                GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);

                // --- Soft no-interrupt shim: clear any interrupt timeout the moment we take damage while casting ---
                GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, ClearInterruptIfCasting);
            }
            else
            {
                GameEventMgr.RemoveHandler(living, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                GameEventMgr.RemoveHandler(living, GameLivingEvent.AttackedByEnemy, ClearInterruptIfCasting);
            }
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            var o = effect.Owner;

            base.OnEffectStart(effect);

            SetFormProperties(o, true);
            ApplyFormEffects(o, true);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var o = effect.Owner;

            ApplyFormEffects(o, false);
            SetFormProperties(o, false);

            if (o is GamePlayer p)
            {
                p.Out.SendUpdateWeaponAndArmorStats();
                p.Out.SendCharStatsUpdate();
                p.Out.SendCharResistsUpdate();
                p.UpdatePlayerStatus();
                p.Out.SendUpdatePlayer();
            }
            else
            {
                o.UpdateHealthManaEndu();
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        public override void OnEffectRemove(GameSpellEffect effect, bool overwrite)
        {
            base.OnEffectRemove(effect, overwrite);
        }

        /// <summary>
        /// Reduce any incoming damage by Spell.AmnesiaChance%.
        /// Defensive proc: 50% chance to apply disease when owner is hit by a successful physical attack.
        /// </summary>
        private void OnAttackedByEnemy(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not AttackedByEnemyEventArgs { AttackData: { } ad })
                return;

            var owner = (GameLiving)sender;

            // Only on successful melee/ranged physical hits that actually dealt damage
            if (!ad.IsSuccessfulHit || (!ad.IsMeleeAttack && !ad.IsRangedAttack))
                return;

            if ((ad.Damage + ad.CriticalDamage) <= 0)
                return;
            
            // --- Absorption from DB (Spell.AmnesiaChance) ---
            if (_absorbPct > 0)
            {
                // Subtract from the hit
                var pct = (_absorbPct / 100.0);
                int reduceBase = (int)Math.Round(ad.Damage * pct);
                int reduceCrit = (int)Math.Round(ad.CriticalDamage * pct);
                ad.Damage -= reduceBase;
                ad.CriticalDamage -= reduceCrit;
            }

            if (!Util.Chance(DISEASE_PROC_CHANCE))
                return;

            Spell disease = SkillBase.GetSpellByID(DISEASE_SUBSPELL_ID);
            if (disease == null)
            {
                var db = new DBSpell
                {
                    SpellID = DISEASE_SUBSPELL_ID,
                    Name = "Bringer's Rot",
                    Description = "A rotting disease that slows, weakens, and inhibits heals.",
                    Type = "Disease",
                    Target = "enemy",
                    Damage = 30,
                    DamageType = (int)eDamageType.Body,
                    Duration = 20000
                };
                disease = new Spell(db, 50);
            }

            SpellLine line = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            ISpellHandler h = ScriptMgr.CreateSpellHandler(owner, disease, line);
            h?.StartSpell(ad.Attacker);
        }

        /// <summary>
        /// Best-effort: if the owner is casting and gets "soft" interrupted, immediately clear the timeout.
        /// This does not cancel a *hard* interrupt already applied by core. For iron-clad, add the 1-liner below in GamePlayer.
        /// </summary>
        private void ClearInterruptIfCasting(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer gp) return;
            if (!gp.IsCasting) return;
            if (!gp.TempProperties.getProperty(BOD_FLAG_ACTIVE, false)) return;

            gp.DisabledCastingTimeout = 0;
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int potency = Math.Max(0, Math.Min(100, (int)Spell.Damage));
            int meleeHaste = Math.Max(0, (int)Spell.Value);
            int absorb = Math.Max(0, (int)Spell.AmnesiaChance);

            return
                $"Become the Bringer of Death. Your spell casts ignore interruption but reach only {potency}% of their potency and you take {absorb}% less damage from all sources.\n" +
                $"Your melee attack speed is increased by {meleeHaste}%. Additionally, all regeneration bonuses & disease proc of Decrepit Form are active.";
        }
    }
}
