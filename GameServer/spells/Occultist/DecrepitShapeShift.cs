using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("DecrepitShapeShift")]
    public class DecrepitShapeShift : AbstractMorphSpellHandler
    {
        // Temp keys
        private const string KEY_SPELL_DMG = "DECREPIT_SPELL_DMG";
        private const string KEY_DOT_DMG = "DECREPIT_DOT_DMG";
        private const string KEY_REGEN_FLAT = "DECREPIT_REGEN_FLAT";     // flat extra HP regen per tick
        private const string KEY_HANDLER_FLAG = "DECREPIT_HANDLER_ATTACHED";
        private const string KEY_ABS_UNIFIED = "DECREPIT_ABS_UNIFIED";    // percent absorb applied to all attack types

        // Constants
        private const int DISEASE_SUBSPELL_ID = 25296; // "Decrepit's Disease"
        private const int DISEASE_PROC_CHANCE = 50;    // 50%

        public DecrepitShapeShift(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            Priority = 1;
        }

        public override bool CheckBeginCast(GameLiving target, bool quiet)
        {
            if (Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_SPIRIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_DECREPIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_CHTONIC, false))
            {
                if (!quiet)
                    MessageToCaster("You must end your current form first.", eChatType.CT_System);
                return false;
            }
            return base.CheckBeginCast(target, quiet);
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target != Caster)
            {
                MessageToCaster(LanguageMgr.GetTranslation((m_caster as GamePlayer)?.Client, "SpellHandler.SelfOnly")
                                ?? "You can only cast this on yourself.",
                                eChatType.CT_System);
                return false;
            }

            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        public override ushort GetModelFor(GameLiving living)
        {
            return (ushort)Spell.LifeDrainReturn;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            var owner = effect.Owner;
            int spellDmgPct = (int)Spell.Value;               // % to direct spell damage and DoT damage
            int absorbPercent = Math.Max(0, (int)Spell.AmnesiaChance); // unified absorb %
            int regenPct = Spell.ResurrectMana;   // % of Level -> flat HP regen bonus
            owner.TempProperties.setProperty(OccultistForms.KEY_DECREPIT, true);

            // --- Spell Damage (direct) ---
            if (spellDmgPct != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.SpellDamage] += spellDmgPct;
                owner.TempProperties.setProperty(KEY_SPELL_DMG, spellDmgPct);
            }

            // --- DoT Damage ---
            if (spellDmgPct != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.DotDamageBonus] += spellDmgPct;
                owner.TempProperties.setProperty(KEY_DOT_DMG, spellDmgPct);
            }

            // --- Armor Absorption (pool) ---
            if (absorbPercent > 0)
                owner.TempProperties.setProperty(KEY_ABS_UNIFIED, absorbPercent);

            if (!owner.TempProperties.getProperty(KEY_HANDLER_FLAG, false))
            {
                GameEventMgr.AddHandler(owner, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                owner.TempProperties.setProperty(KEY_HANDLER_FLAG, true);
            }

            // --- Extra Health Regen (flat), scaling by Level and ResurrectMana% ---
            // extra = round(Level * ResurrectMana / 100)
            if (regenPct != 0)
            {
                int extraRegen = (int)Math.Round(owner.Level * (regenPct / 100.0));
                if (extraRegen > 0)
                    owner.TempProperties.setProperty(KEY_REGEN_FLAT, extraRegen);
            }

            // --- Defensive Disease Proc (native event hook; 50% on melee hits) ---
            if (!owner.TempProperties.getProperty(KEY_HANDLER_FLAG, false))
            {
                GameEventMgr.AddHandler(owner, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                owner.TempProperties.setProperty(KEY_HANDLER_FLAG, true);
            }

            if (owner is GamePlayer gp)
            {
                gp.Out.SendUpdateWeaponAndArmorStats();
                gp.Out.SendCharStatsUpdate();
                gp.UpdatePlayerStatus();
                gp.Out.SendUpdatePlayer();
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var owner = effect.Owner;
            owner.TempProperties.removeProperty(OccultistForms.KEY_DECREPIT);

            int spellDmgPct = owner.TempProperties.getProperty<int>(KEY_SPELL_DMG, 0);
            int dotDmgPct = owner.TempProperties.getProperty<int>(KEY_DOT_DMG, 0);

            if (spellDmgPct != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.SpellDamage] -= spellDmgPct;
                owner.TempProperties.removeProperty(KEY_SPELL_DMG);
            }

            if (dotDmgPct != 0)
            {
                owner.SpecBuffBonusCategory[(int)eProperty.DotDamageBonus] -= dotDmgPct;
                owner.TempProperties.removeProperty(KEY_DOT_DMG);
            }

            owner.TempProperties.removeProperty(KEY_REGEN_FLAT);
            owner.TempProperties.removeProperty(KEY_ABS_UNIFIED);

            if (owner.TempProperties.getProperty(KEY_HANDLER_FLAG, false))
            {
                GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                owner.TempProperties.removeProperty(KEY_HANDLER_FLAG);
            }

            if (owner is GamePlayer gp)
            {
                gp.Out.SendUpdateWeaponAndArmorStats();
                gp.Out.SendCharStatsUpdate();
                gp.UpdatePlayerStatus();
                gp.Out.SendUpdatePlayer();
            }
            else
            {
                owner.UpdateHealthManaEndu();
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        /// <summary>
        /// Defensive: disease proc (50%) + unified absorption on every damaging hit (melee, ranged, spell, DoT).
        /// </summary>
        private void OnAttackedByEnemy(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs { AttackData: { } ad })
                return;

            var owner = (GameLiving)sender;

            // Only apply on actual damaging hits
            if ((ad.Damage + ad.CriticalDamage) <= 0)
                return;

            // --- Unified absorption ---
            int absorbPercent = owner.TempProperties.getProperty<int>(KEY_ABS_UNIFIED, 0);
            if (absorbPercent > 0 && IsAnyCombatHit(ad))
            {
                int total = ad.Damage + ad.CriticalDamage;
                int absorbed = Math.Min(ad.Damage, (int)Math.Round(total * (absorbPercent / 100.0)));
                ad.Damage -= absorbed;
            }

            // --- 50% chance to disease the attacker on melee/ranged physical hits only ---
            // (keep your original intent: proc on weapon hits; skip for spells/DoTs)
            if (ad.AttackType is AttackData.eAttackType.MeleeOneHand
                                  or AttackData.eAttackType.MeleeTwoHand
                                  or AttackData.eAttackType.MeleeDualWield
                                  or AttackData.eAttackType.Ranged)
            {
                if (ad.AttackResult is GameLiving.eAttackResult.HitUnstyled or GameLiving.eAttackResult.HitStyle)
                {
                    if (Util.Chance(DISEASE_PROC_CHANCE))
                    {
                        Spell disease = SkillBase.GetSpellByID(DISEASE_SUBSPELL_ID);
                        if (disease == null)
                        {
                            var db = new DOL.Database.DBSpell
                            {
                                SpellID = DISEASE_SUBSPELL_ID,
                                Name = "Decrepit's Disease",
                                Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.",
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
                }
            }
        }

        private static bool IsAnyCombatHit(AttackData ad)
        {
            // Cover melee/ranged (need Hit result), and spell/DoT (damage already implies a hit)
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

        public override string GetDelveDescription(GameClient delveClient)
        {
            int dmgPct = (int)Spell.Value;
            int absPct = Spell.AmnesiaChance;
            double regenPct = Spell.ResurrectMana / 10;

            return
                $"Become a Decrepit Magus. Your magic damage is increased by {dmgPct}%. " +
                $"Your ABS is {(absPct > 0 ? $"increased by {absPct}%" : "slightly increased")}. " +
                $"Melee attackers have a 50% chance to become diseased when they strike you, " +
                $"and your rotted flesh regenerates, increasing health regeneration by {regenPct}% per level.";
        }
    }
}