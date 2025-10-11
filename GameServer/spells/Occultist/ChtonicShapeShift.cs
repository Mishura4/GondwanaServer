using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Self-morph: Chthonic Knight form with layered defensive/offensive stat boosts.
    /// </summary>
    [SpellHandler("ChtonicShapeShift")]
    public class ChtonicShapeShift : AbstractMorphSpellHandler
    {
        //private double m_afBonusPct;     // % points added to eProperty.ArmorFactor (Category4)
        private int m_afBonusFlat;    // flat points added to eProperty.ArmorFactor (Category4)
        private int m_hpBonus;        // absolute HP added (BaseBuff)
        private int m_wsBonus;        // percent added to eProperty.WeaponSkill (SpecBuff)
        private int m_resBonus;       // percent added to each secondary magic resist (SpecBuff)
        private int m_absBonus;
        private int m_tempParryLevel = 0;

        private const string TP_ADDED  = "ChtonicShapeShift.ParryAdded";
        private const string TP_GVL    = "ChtonicShapeShift.ParryGivenLevel";
        private const string KEY_HANDLER_FLAG = "CHTONIC_HANDLER_ATTACHED";
        private const string KEY_ABS_UNIFIED  = "CHTONIC_ABS_UNIFIED"; // percent absorb applied to all attack types

        public ChtonicShapeShift(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            // Priority = 10;
            OverwritesMorphs = true;
            
            // -------------------------
            // 1) ARMOR FACTOR (AF)
            // -------------------------
            int level = caster.Level;
            int perLevelAF = (int)Spell.AmnesiaChance; // AF per level dial
            int flatAFBase = (perLevelAF * level);     // "AF" units
            int flatAFTotal = flatAFBase * 5;          // convert to eProperty points
            double pctAF = Spell.Value * 0.01;
            int afPropBefore = caster.GetModified(eProperty.ArmorFactor);
            int finalAFBonus = flatAFTotal + (int)Math.Round(afPropBefore * pctAF);
            m_afBonusFlat = finalAFBonus;
            
            // -------------------------
            // 2) MAX HEALTH (+% via Spell.Value)
            // -------------------------
            double pctHealth = Spell.Value * 0.01;
            m_hpBonus = (int)Math.Round(caster.MaxHealth * pctHealth);
            
            // -------------------------
            // 3) WEAPONSKILL (+% via Spell.Value)
            // -------------------------
            m_wsBonus = (int)Math.Round(Spell.Value);

            m_absBonus = (int)Math.Round(Spell.Value * (2.0 / 3.0));
            
            // -------------------------
            // 5) SECONDARY MAGIC RESISTS (+% via Spell.ResurrectMana)
            // -------------------------
            m_resBonus = Spell.ResurrectMana;
        }

        public override bool HasPositiveEffect => true;

        public override bool HasPositiveOrSpeedEffect() => true;

        public override bool IsCancellable(GameSpellEffect compare)
        {
            if (compare.SpellHandler is BringerOfDeath)
                return true;

            return base.IsCancellable(compare);
        }

        /// <summary>Model is driven by Spell.LifeDrainReturn (0 -> no model change).</summary>
        public override ushort GetModelFor(GameLiving living)
        {
            return (ushort)Spell.LifeDrainReturn;
        }

        public override bool CheckBeginCast(GameLiving target, bool quiet)
        {
            if (Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_SPIRIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_DECREPIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_CHTONIC, false))
            {
                if (!quiet)
                    MessageToCaster(LanguageMgr.GetTranslation(m_caster as GamePlayer, "SpellHandler.Occultist.CastCondition4"), eChatType.CT_System);
                return false;
            }
            return base.CheckBeginCast(target, quiet);
        }

        /// <summary>Self-only safety: refuse if target isn't the caster.</summary>
        /// <todo>^ why? Also that's not how you make a self-target spell</todo>
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target != Caster)
            {
                ErrorTranslationToCaster("SpellHandler.TargetSelfOnly");
                return false;
            };
            
            // and ignore effectiveness?
            return base.ApplyEffectOnTarget(target, 1.0);
        }

        private void ApplyStats(GameSpellEffect effect, bool apply)
        {
            var owner = effect.Owner;
            int mult = apply ? 1 : -1;
            owner.BuffBonusCategory4[(int)eProperty.ArmorFactor] += mult * m_afBonusFlat;
            owner.BaseBuffBonusCategory[(int)eProperty.MaxHealth] += mult * m_hpBonus;
            owner.SpecBuffBonusCategory[(int)eProperty.WeaponSkill] += mult * m_wsBonus;
            owner.SpecBuffBonusCategory[(int)eProperty.ArmorAbsorption] += mult * m_absBonus;

            if (m_hpBonus != 0 && apply)
                owner.Health += m_hpBonus;

            if (m_resBonus != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Heat]   += mult * m_resBonus;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Cold]   += mult * m_resBonus;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Matter] += mult * m_resBonus;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Body]   += mult * m_resBonus;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Spirit] += mult * m_resBonus;
                owner.SpecBuffBonusCategory[(int)eProperty.Resist_Energy] += mult * m_resBonus;
            }

            if (owner is GamePlayer player)
            {
                // TODO: Why is this a timer?
                new RegionTimerAction<GamePlayer>(player, p => ModTempParry(p, apply)).Start(1);
            }
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            var owner  = effect.Owner;
            var player = owner as GamePlayer;
            owner.TempProperties.setProperty(OccultistForms.KEY_CHTONIC, true);

            // (C) <<< NEW: release/kill all controlled pets on cast
            // We do an explicit release, which will stop brains and despawn them cleanly.
            if (owner is GameNPC npc)
                npc.ControlledNpcList.Foreach(b => b.Body.Die(owner));
            else if (player != null)
                player.CommandNpcRelease(); // TODO: Does this work? Are there pets we can't manually release?
            
            ApplyStats(effect, true);

            // Push UI updates
            player!.Out.SendCharStatsUpdate();
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
            
            ApplyStats(effect, false);

            if (player != null)
            {
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

        private void ModTempParry(GamePlayer player, bool grant)
        {
            if (grant)
            {
                if (player == null || player.HasSpecialization(Specs.Parry))
                    return;
                
                var parrySpec = SkillBase.GetSpecialization(Specs.Parry);
                if (parrySpec == null)
                    // Log?
                    return;

                int levelToGrant = Math.Max(1, (int)player.Level);

                parrySpec.Level = levelToGrant;
                parrySpec.AllowSave = false;
                parrySpec.Trainable = false;
                parrySpec.Hidden = true;
                player.AddSpecialization(parrySpec);

                m_tempParryLevel = levelToGrant;

                player.Out.SendUpdatePlayerSkills();
                player.UpdatePlayerStatus();
            }
            else
            {
                if (player == null || m_tempParryLevel == 0)
                    return;
                
                var spec = player.GetSpecialization(Specs.Parry);
                if (spec is { Trainable: false, AllowSave: false } && spec.Level <= m_tempParryLevel)
                    player.RemoveSpecialization(Specs.Parry);

                player.Out.SendUpdatePlayerSkills();
                player.UpdatePlayerStatus();
            }
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int perLevelAF = (int)Spell.AmnesiaChance;
            int pctCore = (int)Spell.Value;
            int pctAbsExtra = (int)Math.Round(Spell.Value - (Spell.Value / 3.0));
            int pctSecRes = (int)Spell.ResurrectMana;

            string line1 = $"Become a Chthonic Knight.";
            string line2 = $"AF raises by an additional {perLevelAF} per level.";
            string line3 = $"Your health, your armor factor and weaponskills are each increased by {m_wsBonus}%,";
            string line4 = $"your melee absorption by {m_absBonus}%, secondary magic resistances by {m_resBonus}%,";
            string line5 = $"and you gain the ability to parry attacks.";

            return $"{line1} {line2} {line3} {line4} {line5}";
        }
    }
}
