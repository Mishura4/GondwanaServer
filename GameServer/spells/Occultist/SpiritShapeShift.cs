using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Numerics;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Self-morph: Ghostly Spirit form.
    /// - Model chosen from Spell.LifeDrainReturn
    /// - Absorb Spell.Value% of incoming damage, convert absorbed damage into Power
    /// - Extra Power regen per tick: round(Level * (Spell.ResurrectMana / 100))
    /// - Stealth detection increased by Spell.AmnesiaChance
    /// </summary>
    [SpellHandler("SpiritShapeShift")]
    public class SpiritShapeShift : AbstractMorphSpellHandler
    {
        // Temp keys
        private const string KEY_HANDLER_FLAG = "SPIRIT_HANDLER_ATTACHED";
        private const string KEY_ABSORB_PCT = "SPIRIT_ABSORB_PCT";
        private const string KEY_REGEN_FLAT = "SPIRIT_REGEN_FLAT";      // flat extra Power regen per tick

        public SpiritShapeShift(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            Priority = 10;
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

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            // Self-only
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
            // Model taken from Spell.LifeDrainReturn
            return (ushort)Spell.LifeDrainReturn;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            var owner = effect.Owner;

            owner.TempProperties.setProperty(OccultistForms.KEY_SPIRIT, true);

            if (effect.Owner is GamePlayer petowner && owner.ControlledBrain?.Body is GamePet pet)
            {
                owner.TempProperties.setProperty(OccultistForms.KEY_SPIRIT, true);
                var spiritTpl = pet.TempProperties.getProperty<int>(OccultistForms.PET_SPIRIT_TPL, 0);
                if (spiritTpl > 0) SpellHandler.OccultistForms.ApplyTemplate(pet, spiritTpl);
            }

            // if caster already has pets, flip them to Spirit template
            if (owner is GamePlayer occOwner)
            {
                OccultistForms.SetOccultistPetForm(occOwner, true);
            }

            // --- Store absorb % (Spell.Value) ---
            int storedabsorbPct = Math.Max(0, (int)Spell.Value);
            if (storedabsorbPct > 0)
                owner.TempProperties.setProperty(KEY_ABSORB_PCT, storedabsorbPct);

            // --- Extra Power Regen (flat), scaling by Level and ResurrectMana% ---
            // extra = round(Level * ResurrectMana / 100)
            int regenPct = Spell.ResurrectMana;
            if (regenPct != 0)
            {
                int extraRegen = (int)Math.Round(owner.Level * (regenPct / 100.0));
                if (extraRegen > 0)
                    owner.TempProperties.setProperty(KEY_REGEN_FLAT, extraRegen);
            }

            // --- Stealth Detection bonus (Spell.AmnesiaChance) ---
            if (Spell.AmnesiaChance != 0)
            {
                owner.BaseBuffBonusCategory[(int)eProperty.StealthDetectionBonus] += Spell.AmnesiaChance;
            }

            // --- Unified event hook for incoming damage to perform absorb->mana conversion ---
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
            else
            {
                owner.UpdateHealthManaEndu();
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var owner = effect.Owner;

            owner.TempProperties.removeProperty(OccultistForms.KEY_SPIRIT);

            // if caster has pets, flip them back to base template
            if (owner is GamePlayer occOwner)
            {
                OccultistForms.SetOccultistPetForm(occOwner, false);
            }

            // Remove flat power regen
            owner.TempProperties.removeProperty(KEY_REGEN_FLAT);

            // Remove stored absorb %
            owner.TempProperties.removeProperty(KEY_ABSORB_PCT);

            // Remove event handler
            if (owner.TempProperties.getProperty(KEY_HANDLER_FLAG, false))
            {
                GameEventMgr.RemoveHandler(owner, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                owner.TempProperties.removeProperty(KEY_HANDLER_FLAG);
            }

            // Remove Stealth Detection bonus
            if (Spell.AmnesiaChance != 0)
            {
                owner.BaseBuffBonusCategory[(int)eProperty.StealthDetectionBonus] -= Spell.AmnesiaChance;
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

            if (effect.Owner is GamePlayer petowner && owner.ControlledBrain?.Body is GamePet pet)
            {
                owner.TempProperties.removeProperty(OccultistForms.KEY_SPIRIT);
                var baseTpl = pet.TempProperties.getProperty<int>(OccultistForms.PET_BASE_TPL, 0);
                if (baseTpl > 0) SpellHandler.OccultistForms.ApplyTemplate(pet, baseTpl);
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        /// <summary>
        /// Absorb Spell.Value% of the incoming hit and convert it to Power (Mana).
        /// Applies to all damaging hits (melee, ranged, spells, DoTs).
        /// </summary>
        private void OnAttackedByEnemy(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs { AttackData: { } ad })
                return;

            var owner = (GameLiving)sender;

            // Only apply on actual damaging hits
            int total = ad.Damage + ad.CriticalDamage;
            if (total <= 0)
                return;

            int absorbPct = owner.TempProperties.getProperty<int>(KEY_ABSORB_PCT, 0);
            if (absorbPct <= 0)
                return;

            // absorb = round(total * absorbPct / 100), cap by actual base damage portion
            int absorbed = (int)Math.Round(total * (absorbPct / 100.0));
            if (absorbed <= 0)
                return;

            // Subtract from the hit (use ad.Damage; don't touch crit directly to avoid negatives)
            int reduceFromDamage = Math.Min(ad.Damage, absorbed);
            ad.Damage -= reduceFromDamage;

            // Any remainder (if crit > 0 and absorbed > base damage) is effectively not applied
            // to final damage; we don't need to modify ad.CriticalDamage as the engine sums them.

            // Convert absorbed amount into power (positive change)
            if (reduceFromDamage > 0)
            {
                // Use ChangeMana to respect caps and propagate properly
                int changed = owner.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, reduceFromDamage);

                // Optional feedback
                if (owner is GamePlayer p && changed > 0)
                {
                    p.SendTranslatedMessage("SpiritShapeShift.Self.AbsorbToPower",
                        eChatType.CT_Spell, eChatLoc.CL_SystemWindow, changed);
                }
            }
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int absorb = (int)Spell.Value;
            int stealthDet = Spell.AmnesiaChance;
            double perLevel = Spell.ResurrectMana / 10.0;

            string baseText = "Become a Ghostly Spirit. " +
                              $"{absorb}% of all damage you take is absorbed and instead restores your power. " +
                              $"Your power regeneration is increased by {perLevel}% per level, " +
                              $"and your chance to uncover stealther enemies is {stealthDet}% greater. " +
                              $"Additionally, your servants become highly resistant to damage and gain new abilities.";

            return baseText;
        }
    }
}
