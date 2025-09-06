using System;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using DOL.Events;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Self-morph: Chthonic Knight form with layered defensive/offensive stat boosts.
    /// </summary>
    [SpellHandler("ChtonicShapeShift")]
    public class ChtonicShapeShift : AbstractMorphSpellHandler
    {
        // TempProperties key is the effect object itself, we store a Snapshot to cleanly revert
        private class Snapshot
        {
            public int AfAddProp;          // total points added to eProperty.ArmorFactor (Category4)
            public int HpAddAbs;           // absolute HP added (BaseBuff)
            public int WsAddPct;           // percent added to eProperty.WeaponSkill (SpecBuff)
            public int ParryAddPct;        // percent added to eProperty.ParryChance (SpecBuff)
            public int SecResAddPct;       // percent added to each secondary magic resist (SpecBuff)
        }

        private const string TP_ADDED  = "ChtonicShapeShift.ParryAdded";
        private const string TP_GVL    = "ChtonicShapeShift.ParryGivenLevel";
        private const string KEY_HANDLER_FLAG = "CHTONIC_HANDLER_ATTACHED";
        private const string KEY_ABS_UNIFIED  = "CHTONIC_ABS_UNIFIED"; // percent absorb applied to all attack types

        public ChtonicShapeShift(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            Priority = 1;           // same priority level as WarlockSpeedDecrease
            OverwritesMorphs = true;
        }

        public override bool HasPositiveOrSpeedEffect() => true;

        /// <summary>Model is driven by Spell.LifeDrainReturn (0 -> no model change).</summary>
        public override ushort GetModelFor(GameLiving living)
        {
            return (ushort)Spell.LifeDrainReturn;
        }

        public override bool CheckBeginCast(GameLiving target, bool quiet)
        {
            // Do not allow casting if any form is already active (including this one)
            if (Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_SPIRIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_DECREPIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_CHTONIC, false))
            {
                if (!quiet) ErrorTranslationToCaster("You must end your current form first.");
                return false;
            }
            return base.CheckBeginCast(target, quiet);
        }

        /// <summary>Self-only safety: refuse if target isn't the caster.</summary>
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target != Caster)
            {
                ErrorTranslationToCaster("SpellHandler.TargetSelfOnly");
                return false;
            }
            return base.ApplyEffectOnTarget(target, 1.0);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            var owner  = effect.Owner;
            var snap   = new Snapshot();
            var player = owner as GamePlayer;
            owner.TempProperties.setProperty(OccultistForms.KEY_CHTONIC, true);

            if (player == null) return;

            // (C) <<< NEW: release/kill all controlled pets on cast
            // We do an explicit release, which will stop brains and despawn them cleanly.
            try
            {
                if (player?.ControlledBrain != null)
                {
                    // GamePlayer has CommandNpcRelease(); GameLiving does not.
                    player.CommandNpcRelease();
                }
            }
            catch
            {
                // keep cast safe
            }

            // -------------------------
            // 1) ARMOR FACTOR (AF)
            // -------------------------
            int level = owner.Level;
            int perLevelAF = (int)Spell.AmnesiaChance; // AF per level dial
            int flatAF = 0;
            int baseAFTotal = (perLevelAF * level) + flatAF; // "AF" units
            int basePropAdd = baseAFTotal * 5;               // convert to eProperty points

            int afPropBefore = owner.GetModified(eProperty.ArmorFactor);
            int afPctPropAdd = (int)Math.Round(afPropBefore * Spell.Value * 0.01);
            int afPropTotalAdd = basePropAdd + afPctPropAdd;

            owner.BuffBonusCategory4[(int)eProperty.ArmorFactor] += afPropTotalAdd;
            snap.AfAddProp = afPropTotalAdd;

            // -------------------------
            // 2) MAX HEALTH (+% via Spell.Value)
            // -------------------------
            int hpBaseNow = player!.MaxHealth;
            int hpAddAbs  = (int)Math.Round(hpBaseNow * Spell.Value * 0.01);
            if (hpAddAbs > 0)
            {
                owner.BaseBuffBonusCategory[(int)eProperty.MaxHealth] += hpAddAbs;
                snap.HpAddAbs = hpAddAbs;
            }

            // -------------------------
            // 3) WEAPONSKILL (+% via Spell.Value)
            // -------------------------
            int wsAddPct = (int)Spell.Value;
            if (wsAddPct != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.WeaponSkill] += wsAddPct;
                snap.WsAddPct = wsAddPct;
            }

            // -------------------------
            // 4) UNIFIED DAMAGE ABSORPTION (ALL attack types)
            //    absorbPercent = two-thirds of Spell.Value (i.e., Value - Value/3)
            // -------------------------
            int absorbPercent = Math.Max(0, (int)Math.Round(Spell.Value * (2.0 / 3.0)));
            if (absorbPercent > 0)
                owner.TempProperties.setProperty(KEY_ABS_UNIFIED, absorbPercent);

            // -------------------------
            // 5) SECONDARY MAGIC RESISTS (+% via Spell.ResurrectMana)
            // -------------------------
            int secResAdd = Spell.ResurrectMana;
            if (secResAdd != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Heat]   += secResAdd;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Cold]   += secResAdd;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Matter] += secResAdd;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Body]   += secResAdd;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Spirit] += secResAdd;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Energy] += secResAdd;
                snap.SecResAddPct = secResAdd;
            }

            // Optional: grant Parry specialization temporarily (client-visible)
            new RegionTimerAction<GamePlayer>(player, p => GrantTempParry(p)).Start(1);

            int parryPct = Spell.Damage > 0 ? (int)Spell.Damage : 5;
            if (parryPct > 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.ParryChance] += parryPct;
                snap.ParryAddPct = parryPct;
            }

            // Save snapshot for clean removal
            owner.TempProperties.setProperty(effect, snap);

            // Attach unified absorb handler
            if (!owner.TempProperties.getProperty(KEY_HANDLER_FLAG, false))
            {
                GameEventMgr.AddHandler(owner, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                owner.TempProperties.setProperty(KEY_HANDLER_FLAG, true);
            }

            // Push UI updates
            player.Out.SendCharStatsUpdate();
            player.Out.SendCharResistsUpdate();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();
            player.Out.SendUpdateWeaponAndArmorStats();
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var owner  = effect.Owner;
            var player = owner as GamePlayer;
            owner.TempProperties.removeProperty(OccultistForms.KEY_CHTONIC);

            // Revert everything we added
            var snapObj = owner.TempProperties.getProperty<object>(effect, null);
            owner.TempProperties.removeProperty(effect);

            if (snapObj is Snapshot snap)
            {
                owner.BuffBonusCategory4[(int)eProperty.ArmorFactor] -= snap.AfAddProp;

                if (snap.HpAddAbs != 0)
                    owner.BaseBuffBonusCategory[(int)eProperty.MaxHealth] -= snap.HpAddAbs;

                if (snap.WsAddPct != 0)
                    owner.SpecBuffBonusCategory[(int)eProperty.WeaponSkill] -= snap.WsAddPct;

                if (snap.ParryAddPct != 0)
                    owner.SpecBuffBonusCategory[(int)eProperty.ParryChance] -= snap.ParryAddPct;

                if (snap.SecResAddPct != 0)
                {
                    owner.SpecBuffBonusCategory[(int)eProperty.Resist_Heat]   -= snap.SecResAddPct;
                    owner.SpecBuffBonusCategory[(int)eProperty.Resist_Cold]   -= snap.SecResAddPct;
                    owner.SpecBuffBonusCategory[(int)eProperty.Resist_Matter] -= snap.SecResAddPct;
                    owner.SpecBuffBonusCategory[(int)eProperty.Resist_Body]   -= snap.SecResAddPct;
                    owner.SpecBuffBonusCategory[(int)eProperty.Resist_Spirit] -= snap.SecResAddPct;
                    owner.SpecBuffBonusCategory[(int)eProperty.Resist_Energy] -= snap.SecResAddPct;
                }
            }

            // Remove unified absorption + handler
            owner.TempProperties.removeProperty(KEY_ABS_UNIFIED);
            if (owner.TempProperties.getProperty(KEY_HANDLER_FLAG, false))
            {
                GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                owner.TempProperties.removeProperty(KEY_HANDLER_FLAG);
            }

            if (player != null)
            {
                if (player.TempProperties.getProperty<bool>(TP_ADDED, false))
                {
                    int granted = player.TempProperties.getProperty<int>(TP_GVL, 0);
                    var spec = player.GetSpecialization(Specs.Parry);
                    if (spec != null && spec.Level <= Math.Max(1, granted))
                        player.RemoveSpecialization(Specs.Parry);

                    player.Out.SendUpdatePlayerSkills();
                    player.UpdatePlayerStatus();

                    player.TempProperties.removeProperty(TP_ADDED);
                    player.TempProperties.removeProperty(TP_GVL);
                }

                player.Out.SendCharStatsUpdate();
                player.Out.SendCharResistsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
                player.Out.SendUpdateWeaponAndArmorStats();
            }
            else
            {
                owner.UpdateHealthManaEndu();
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        /// <summary>
        /// Unified damage absorption for *all* attack types (melee, ranged, spells, DoTs).
        /// </summary>
        private void OnAttackedByEnemy(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs { AttackData: { } ad })
                return;

            var owner = (GameLiving)sender;

            // Only apply on actual damaging hits
            if ((ad.Damage + ad.CriticalDamage) <= 0)
                return;

            int absorbPercent = owner.TempProperties.getProperty<int>(KEY_ABS_UNIFIED, 0);
            if (absorbPercent <= 0)
                return;

            if (!IsAnyCombatHit(ad))
                return;

            int total = ad.Damage + ad.CriticalDamage;
            int absorbed = Math.Min(ad.Damage, (int)Math.Round(total * (absorbPercent / 100.0)));
            ad.Damage -= absorbed;
        }

        private static bool IsAnyCombatHit(AttackData ad)
        {
            if (ad.AttackType is AttackData.eAttackType.MeleeOneHand
                              or AttackData.eAttackType.MeleeTwoHand
                              or AttackData.eAttackType.MeleeDualWield
                              or AttackData.eAttackType.Ranged)
            {
                return ad.AttackResult is GameLiving.eAttackResult.HitUnstyled or GameLiving.eAttackResult.HitStyle;
            }
            if (ad.AttackType is AttackData.eAttackType.Spell or AttackData.eAttackType.DoT)
                return true;

            return false;
        }

        private void GrantTempParry(GamePlayer player)
        {
            if (player == null || player.HasSpecialization(Specs.Parry)) return;

            int levelToGrant = Math.Max(1, (int)player.Level);
            var parrySpec = SkillBase.GetSpecialization(Specs.Parry);
            if (parrySpec == null) return;

            parrySpec.Level = levelToGrant;
            player.AddSpecialization(parrySpec);

            player.TempProperties.setProperty(TP_ADDED, true);
            player.TempProperties.setProperty(TP_GVL, levelToGrant);

            player.Out.SendUpdatePlayerSkills();
            player.UpdatePlayerStatus();
        }


        public override string GetDelveDescription(GameClient delveClient)
        {
            int perLevelAF = (int)Spell.AmnesiaChance;
            int pctCore = (int)Spell.Value;
            int pctAbsExtra = (int)Math.Round(Spell.Value - (Spell.Value / 3.0));
            int pctSecRes = (int)Spell.ResurrectMana;
            int parryPct = Spell.Damage > 0 ? (int)Spell.Damage : 5;

            string line1 = $"Become a Chthonic Knight.";
            string line2 = $"AF raises by an additional {perLevelAF} per level.";
            string line3 = $"Your health, your armor factor and weaponskills are each increased by {pctCore}%,";
            string line4 = $"your melee absorption by {pctAbsExtra}%, secondary magic resistances by {pctSecRes}%,";
            string line5 = $"and you gain the ability to parry attacks.";

            return $"{line1} {line2} {line3} {line4} {line5}";
        }
    }
}
