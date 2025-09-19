using System;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

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
        private const string BOD_FLAG_ACTIVE = "BOD_ACTIVE";
        private const string BOD_FLAG_UNINTERRUPTIBLE = "BOD_UNINTERRUPTIBLE";
        private const string BOD_FLAG_MOVECAST = "BOD_CAN_MOVECAST";
        private const string BOD_PREV_SPELL_ID = "BOD_PREV_SPELL_ID";
        private const string BOD_PREV_SPELL_LINE = "BOD_PREV_SPELL_LINE";

        private const int DISEASE_SUBSPELL_ID = 25296;
        private const int DISEASE_PROC_CHANCE = 50;

        private int _absorbPct;
        private float _regenMult;
        private int _meleeSpeedBonus;
        private int _spellDmgDelta;
        private int _dotDmgDelta;
        private bool _hookedEvents;

        public BringerOfDeath(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            Priority = 11;
            _absorbPct = Math.Max(0, (int)Spell.AmnesiaChance);
            _regenMult = ((float)Spell.ResurrectMana / 100f);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new BringerOfDeathSpellEffect(this, CalculateEffectDuration(target, effectiveness), 0);
        }

        public override bool CheckBeginCast(GameLiving target, bool quiet)
        {
            if (!base.CheckBeginCast(target, quiet))
                return false;

            GameSpellEffect prev = null;
            foreach (var eff in Caster.FindEffectsOnTarget(typeof(AbstractMorphSpellHandler)))
            {
                if (eff.SpellHandler is DecrepitShapeShift
                    || eff.SpellHandler is SpiritShapeShift
                    || eff.SpellHandler is ChtonicShapeShift)
                {
                    prev = eff;
                    break;
                }
            }

            if (prev != null)
            {
                Caster.TempProperties.setProperty(BOD_PREV_SPELL_ID, prev.Spell.ID);
                var lineKey = prev.SpellHandler.SpellLine?.KeyName ?? prev.SpellHandler.SpellLine?.Name;
                Caster.TempProperties.setProperty(BOD_PREV_SPELL_LINE, lineKey ?? string.Empty);
            }
            else
            {
                Caster.TempProperties.removeProperty(BOD_PREV_SPELL_ID);
                Caster.TempProperties.removeProperty(BOD_PREV_SPELL_LINE);
            }

            return true;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target != Caster)
            {
                MessageToCaster(
                    LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.SelfOnly")
                    ?? "You can only cast this on yourself.",
                    eChatType.CT_System);
                return false;
            }
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        // Morph model comes from DB
        public override ushort GetModelFor(GameLiving living) => (ushort)Spell.LifeDrainReturn;

        private int PotencyToNegativeBonus()
        {
            int potency = Math.Max(0, Math.Min(100, (int)Spell.Damage));
            return potency - 100;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            var o = effect.Owner;

            // Mark + rules (your move-cast code elsewhere already keys off these)
            o.TempProperties.setProperty(BOD_FLAG_ACTIVE, true);
            o.TempProperties.setProperty(BOD_FLAG_UNINTERRUPTIBLE, true);
            o.TempProperties.setProperty(BOD_FLAG_MOVECAST, true);

            // --- Melee speed (haste) from Spell.Value ---
            _meleeSpeedBonus = Math.Max(0, (int)Spell.Value);
            if (_meleeSpeedBonus > 0)
                o.BaseBuffBonusCategory[(int)eProperty.MeleeSpeed] += _meleeSpeedBonus;

            // --- Absorption from DB (Spell.AmnesiaChance) ---
            if (_absorbPct != 0)
            {
                o.SpecBuffBonusCategory[(int)eProperty.ArmorAbsorption] += _absorbPct;
                o.SpecBuffBonusCategory[(int)eProperty.MagicAbsorption] += _absorbPct;
            }

            // --- Regen multiplier (Decrepit-style) ---
            if (Math.Abs(_regenMult) > 0.0001f)
                o.BuffBonusMultCategory1.Set((int)eProperty.HealthRegenerationRate, this, _regenMult);

            // --- Magical-only potency: write NEGATIVE bonuses so calculators reduce output ---
            _spellDmgDelta = PotencyToNegativeBonus();
            _dotDmgDelta = _spellDmgDelta;
            if (_spellDmgDelta != 0)
            {
                o.BaseBuffBonusCategory[(int)eProperty.SpellDamage] += _spellDmgDelta;
                o.BaseBuffBonusCategory[(int)eProperty.DotDamageBonus] += _dotDmgDelta;
            }

            // --- Defensive disease proc on incoming physical hits ---
            GameEventMgr.AddHandler(o, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
            _hookedEvents = true;

            // --- Soft no-interrupt shim: clear any interrupt timeout the moment we take damage while casting ---
            GameEventMgr.AddHandler(o, GameLivingEvent.AttackedByEnemy, ClearInterruptIfCasting);

            // --- Client/state updates ---
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
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var o = effect.Owner;

            // Remove potency debuffs
            if (_spellDmgDelta != 0)
            {
                o.BaseBuffBonusCategory[(int)eProperty.SpellDamage] -= _spellDmgDelta;
                o.BaseBuffBonusCategory[(int)eProperty.DotDamageBonus] -= _dotDmgDelta;
            }

            // Remove regen multiplier
            o.BuffBonusMultCategory1.Remove((int)eProperty.HealthRegenerationRate, this);

            // Remove melee haste
            if (_meleeSpeedBonus > 0)
                o.BaseBuffBonusCategory[(int)eProperty.MeleeSpeed] -= _meleeSpeedBonus;

            // Remove ABS
            if (_absorbPct != 0)
            {
                o.SpecBuffBonusCategory[(int)eProperty.ArmorAbsorption] -= _absorbPct;
                o.SpecBuffBonusCategory[(int)eProperty.MagicAbsorption] -= _absorbPct;
            }

            // Unhook events
            if (_hookedEvents)
            {
                GameEventMgr.RemoveHandler(o, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                GameEventMgr.RemoveHandler(o, GameLivingEvent.AttackedByEnemy, ClearInterruptIfCasting);
                _hookedEvents = false;
            }

            // Clear flags
            o.TempProperties.removeProperty(BOD_FLAG_ACTIVE);
            o.TempProperties.removeProperty(BOD_FLAG_UNINTERRUPTIBLE);
            o.TempProperties.removeProperty(BOD_FLAG_MOVECAST);

            // ---- Restore previous morph (if any) ----
            int prevId = effect.Owner.TempProperties.getProperty<int>(BOD_PREV_SPELL_ID, 0);
            string prevLineName = effect.Owner.TempProperties.getProperty<string>(BOD_PREV_SPELL_LINE, string.Empty);

            // Clear the memory first to avoid loops if something goes wrong
            effect.Owner.TempProperties.removeProperty(BOD_PREV_SPELL_ID);
            effect.Owner.TempProperties.removeProperty(BOD_PREV_SPELL_LINE);

            if (prevId > 0)
            {
                new RegionTimerAction<GameLiving>(o, living =>
                {
                    try
                    {
                        var prevSpell = SkillBase.GetSpellByID(prevId);
                        SpellLine prevLine = null;

                        if (!string.IsNullOrEmpty(prevLineName))
                            prevLine = SkillBase.GetSpellLine(prevLineName);

                        prevLine ??= SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);

                        if (prevSpell != null && prevLine != null)
                        {
                            var h = ScriptMgr.CreateSpellHandler(living, prevSpell, prevLine);
                            h?.StartSpell(living);
                        }
                    }
                    catch
                    { }
                }).Start(1);
            }

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

        /// <summary>
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
                $"Your movement speed is increased by {meleeHaste}%, even in speedwarps and while in combat. Additionally, all regeneration bonuses & disease proc of Decrepit Form are active.";
        }
    }
}
