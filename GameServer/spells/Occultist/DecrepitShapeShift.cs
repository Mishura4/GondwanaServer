using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("DecrepitShapeShift")]
    public class DecrepitShapeShift : AbstractMorphSpellHandler
    {
        protected int m_spellDmgPct;
        protected int m_absorbPct;
        protected float m_regenPct;
        
        // Constants
        private const int DISEASE_SUBSPELL_ID = 25296; // "Decrepit's Disease"
        private const int DISEASE_PROC_CHANCE = 50;    // 50%

        public DecrepitShapeShift(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            // Priority = 10;
            
            // --- Spell Damage (direct) ---
            m_spellDmgPct = (int)Spell.Value; // % to direct spell damage and DoT damage
            // --- Armor Absorption (pool) ---
            m_absorbPct = Math.Max(0, (int)Spell.AmnesiaChance); // unified absorb %

            // --- Extra Health Regen (%), ResurrectMana% ---
            m_regenPct = ((float)Spell.ResurrectMana / 100);
            // extra = round(Level * ResurrectMana / 100)
        }

        public override bool HasPositiveEffect => true;

        public override bool HasPositiveOrSpeedEffect() => true;

        public override bool IsCancellable(GameSpellEffect compare)
        {
            if (compare.SpellHandler is BringerOfDeath)
                return true;

            return base.IsCancellable(compare);
        }

        protected virtual bool CheckFormConditions(GameLiving target, bool quiet)
        {
            if (Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_SPIRIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_DECREPIT, false) ||
                Caster.TempProperties.getProperty<bool>(OccultistForms.KEY_CHTONIC, false))
            {
                if (!quiet)
                    MessageToCaster(LanguageMgr.GetTranslation(m_caster as GamePlayer, "SpellHandler.Occultist.CastCondition4"), eChatType.CT_System);
                return false;
            }
            return true;
        }

        public override bool CheckBeginCast(GameLiving target, bool quiet)
        {
            if (!CheckFormConditions(target, quiet))
                return false;
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

        protected virtual void SetFormProperties(GameLiving living, bool apply)
        {
            if (apply)
                living.TempProperties.setProperty(OccultistForms.KEY_DECREPIT, true);
            else
                living.TempProperties.removeProperty(OccultistForms.KEY_DECREPIT);
        }

        protected virtual void ApplyBonuses(GameLiving living, bool apply)
        {
            var mult = (sbyte)(apply ? 1 : -1);

            if (m_regenPct != 0)
            {
                living.BuffBonusMultCategory1.Set((int)eProperty.HealthRegenerationRate, this, m_regenPct * mult);
            }

            // --- Spell Damage (direct) ---
            living.SpecBuffBonusCategory[(int)eProperty.SpellDamage] += m_spellDmgPct * mult;
            living.SpecBuffBonusCategory[(int)eProperty.DotDamageBonus] += m_spellDmgPct * mult;

            // --- Defensive Disease Proc (native event hook; 50% on melee hits) ---
            // --- Armor absorption logic ---
            living.SpecBuffBonusCategory[(int)eProperty.ArmorAbsorption] += m_absorbPct * mult;

            if (apply)
                GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
            else
                GameEventMgr.RemoveHandler(living, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            var owner = effect.Owner;

            SetFormProperties(owner, true);
            ApplyBonuses(owner, true);

            if (owner is GamePlayer gp)
            {
                gp.Out.SendUpdateWeaponAndArmorStats();
                gp.Out.SendCharStatsUpdate();
                gp.Out.SendCharResistsUpdate();
                gp.UpdatePlayerStatus();
                gp.Out.SendUpdatePlayer();
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var owner = effect.Owner;

            SetFormProperties(owner, false);
            ApplyBonuses(owner, false);

            if (owner is GamePlayer gp)
            {
                gp.Out.SendUpdateWeaponAndArmorStats();
                gp.Out.SendCharStatsUpdate();
                gp.Out.SendCharResistsUpdate();
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
        protected virtual void OnAttackedByEnemy(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs { AttackData: { } ad })
                return;

            var owner = (GameLiving)sender;

            // Only apply on actual damaging hits
            if ((ad.Damage + ad.CriticalDamage) <= 0)
                return;

            // --- 50% chance to disease the attacker on melee/ranged physical hits only ---
            // (keep your original intent: proc on weapon hits; skip for spells/DoTs)
            if (ad.IsSuccessfulHit && (ad.IsMeleeAttack || ad.IsRangedAttack))
            {
                if (Util.Chance(DISEASE_PROC_CHANCE))
                {
                    Spell disease = SkillBase.GetSpellByID(DISEASE_SUBSPELL_ID);
                    if (disease == null)
                    {
                        // TODO: Why not use subspells here, and set this.CastSubSpellsWithSpell to false?
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

        public override string GetDelveDescription(GameClient delveClient)
        {
            string abs = (m_absorbPct != 0 ? $"Your ABS is increased by {m_absorbPct}%" : string.Empty);
            return
                $"Become a Decrepit Magus. Your magic damage is increased by {m_spellDmgPct}%. " +
                abs + 
                $"Melee attackers have a {DISEASE_PROC_CHANCE}% chance to become diseased when they strike you, " +
                $"and your rotted flesh regenerates, increasing health regeneration by {m_regenPct}%.";
        }
    }
}