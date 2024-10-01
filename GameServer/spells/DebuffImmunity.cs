using System;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("DebuffImmunity")]
    public class DebuffImmunitySpellHandler : SpellHandler
    {
        public DebuffImmunitySpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine) { }

        /// <summary>
        /// Applies the Debuff Immunity effect to the target.
        /// </summary>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target == null)
                return;

            var existingImmunity = FindEffectOnTarget(target, "DebuffImmunity") as DebuffImmunityEffect;
            if (existingImmunity != null)
            {
                existingImmunity.Refresh(CalculateEffectDuration(target, effectiveness));
                MessageToLiving(target, "Your Debuff Immunity has been refreshed.", eChatType.CT_Spell);
                return;
            }

            var immunityEffect = new DebuffImmunityEffect(this, (int)Spell.Value, CalculateEffectDuration(target, effectiveness));
            immunityEffect.Start(target);

            MessageToLiving(target, "You are now immune to all debuffs for the duration of the spell!", eChatType.CT_Spell);
            SendEffectAnimation(target, 0, false, 1);
        }

        /// <summary>
        /// Called when the effect starts.
        /// </summary>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            var immunityEffect = effect as DebuffImmunityEffect;
            if (immunityEffect != null)
            {
                MessageToLiving(effect.Owner, $"You gain immunity to all debuffs, increasing your resist chance by {immunityEffect.AdditionalResistChance}%.", eChatType.CT_Spell);
            }
        }

        /// <summary>
        /// Called when the effect expires.
        /// </summary>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (!noMessages)
            {
                var immunityEffect = effect as DebuffImmunityEffect;
                if (immunityEffect != null)
                {
                    MessageToLiving(effect.Owner, "Your Debuff Immunity has worn off.", eChatType.CT_Spell);
                }
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        /// <summary>
        /// Calculates the chance for the spell to be resisted.
        /// Includes existing ResistDebuff effects and DebuffImmunity.
        /// </summary>
        public override int CalculateSpellResistChance(GameLiving target)
        {
            int baseResistChance = base.CalculateSpellResistChance(target);

            var resistDebuff = FindEffectOnTarget(target, typeof(AbstractResistDebuff));
            if (resistDebuff != null)
            {
                baseResistChance += (int)resistDebuff.Spell.Value;
            }

            var immunityEffect = FindEffectOnTarget(target, "DebuffImmunity") as DebuffImmunityEffect;
            if (immunityEffect != null)
            {
                baseResistChance += immunityEffect.AdditionalResistChance;
            }

            return Math.Min(100, baseResistChance);
        }

        /// <summary>
        /// Calculates the duration of the Debuff Immunity effect.
        /// </summary>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = Spell.Duration * effectiveness;
            duration *= (1.0 + Caster.GetModified(eProperty.SpellDuration) * 0.01);

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = (Spell.Duration * 4);

            return (int)duration;
        }

        /// <summary>
        /// Provides a brief description of the Debuff Immunity spell.
        /// </summary>
        public override string ShortDescription => "Grants immunity to all debuffs, increasing resist chance by {Spell.Value}% for {Spell.Duration / 1000} seconds.";
    }

    /// <summary>
    /// Represents the Debuff Immunity effect, increasing resist chance against all debuffs.
    /// </summary>
    public class DebuffImmunityEffect : GameSpellEffect
    {
        public int AdditionalResistChance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the DebuffImmunityEffect class.
        /// </summary>
        /// <param name="spellHandler">The spell handler applying this effect.</param>
        /// <param name="resistChance">Additional resist chance provided by this effect.</param>
        /// <param name="duration">Duration of the effect in milliseconds.</param>
        public DebuffImmunityEffect(ISpellHandler spellHandler, int resistChance, int duration)
            : base(spellHandler, duration, 0, 0)
        {
            AdditionalResistChance = resistChance;
        }

        /// <summary>
        /// Refreshes the duration of the effect.
        /// </summary>
        /// <param name="duration">New duration in milliseconds.</param>
        public void Refresh(int duration)
        {
            Duration = duration;
        }
    }
}