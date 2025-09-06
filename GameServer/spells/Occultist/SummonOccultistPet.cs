using System;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Occultist summoning handler: gives an OccultistPetBrain and enforces level scaling with owner.
    /// - Spell.Value can optionally cap the pet level (0 => no cap).
    /// - If you set Spell.Damage >= 0 in DB, that becomes a fixed pet level (override).
    ///   Otherwise we scale from owner level (-100% by default).
    /// </summary>
    [SpellHandler("SummonOccultistPet")]
    public class SummonOccultistPet : SummonSpellHandler
    {
        public SummonOccultistPet(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            // Remember the pet (if any) before summoning
            GamePet oldPet = (Caster as GamePlayer)?.ControlledBrain?.Body as GamePet;

            // Let base handler do the actual summoning
            bool result = base.OnDirectEffect(target, effectiveness);
            if (!result) return false; // summon failed/resisted/etc.

            var owner = Caster as GamePlayer ?? ((Caster as GameNPC)?.GetPlayerOwner());
            var brain = (owner as GamePlayer)?.ControlledBrain;
            var pet = brain?.Body as GamePet;

            // If we got a new pet, customize it
            if (pet != null && !ReferenceEquals(pet, oldPet))
            {
                // Install Occultist brain
                pet.SetOwnBrain(new OccultistPetBrain(owner ?? Caster));

                // Enforce level scaling knobs:
                // Damage >= 0 => fixed level; otherwise use -100 (scale from owner)
                double damageKnob = Spell.Damage >= 0 ? Spell.Damage : -100.0;
                pet.SummonSpellDamage = damageKnob;

                // Value > 0 => cap max level
                pet.SummonSpellValue = Spell.Value > 0 ? Spell.Value : 0.0;

                // Recalc level & stats
                pet.SetPetLevel();
                pet.AutoSetStats();
            }

            return true;
        }
    }
}
