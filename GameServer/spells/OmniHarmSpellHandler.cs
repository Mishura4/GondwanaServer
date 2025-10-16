using System;
using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.ServerProperties;

namespace DOL.GS.Spells
{
    [SpellHandler("OmniHarm")]
    public class OmniHarmSpellHandler : DirectDamageSpellHandler
    {
        const string MYTH_REFLECT_ABSORB_TICK = "MYTH_REFLECT_ABSORB_TICK";
        const string MYTH_REFLECT_ABSORB_FLAG = "MYTH_REFLECT_ABSORB_PCT_THIS_HIT";

        public OmniHarmSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        { }

        /// <summary>
        /// After normal HP damage, also subtract Endurance and Power based on Spell.Damage.
        /// </summary>
        protected override void DealDamage(GameLiving target, double effectiveness)
        {
            base.DealDamage(target, effectiveness);

            if (target == null || !target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active)
                return;

            double endDrainAmount = Spell.Damage * 0.15;
            int endDrain = (int)Math.Round(endDrainAmount);

            double powerDrainAmount = Spell.Damage * 0.55;
            int powerDrain = (int)Math.Round(powerDrainAmount);

            if (endDrain <= 0 && powerDrain <= 0)
                return;

            var reflectEff = FindEffectOnTarget(target, "SpellReflection");
            if (reflectEff != null)
            {
                double absorbPct = Math.Max(0, Math.Min(100, reflectEff.Spell.LifeDrainReturn));

                if (absorbPct > 0)
                {
                    if (endDrain > 0)
                    {
                        int absorbedEnd = (int)Math.Round(endDrain * (absorbPct / 100.0));
                        endDrain = Math.Max(0, endDrain - absorbedEnd);
                    }
                    if (powerDrain > 0)
                    {
                        int absorbedPow = (int)Math.Round(powerDrain * (absorbPct / 100.0));
                        powerDrain = Math.Max(0, powerDrain - absorbedPow);
                    }
                }
            }

            int mythAbsorb = target.TempProperties.getProperty<int>(MYTH_REFLECT_ABSORB_FLAG, 0);
            if (mythAbsorb > 0)
            {
                endDrain = Math.Max(0, endDrain - (int)Math.Round(endDrain * mythAbsorb / 100.0));
                powerDrain = Math.Max(0, powerDrain - (int)Math.Round(powerDrain * mythAbsorb / 100.0));
                target.TempProperties.removeProperty(MYTH_REFLECT_ABSORB_FLAG);
                target.TempProperties.removeProperty(MYTH_REFLECT_ABSORB_TICK);
            }

            int changedEnd = 0;
            int changedMana = 0;

            if (endDrain > 0)
                changedEnd = target.ChangeEndurance(Caster, GameLiving.eEnduranceChangeType.Spell, -endDrain);

            if (powerDrain > 0)
                changedMana = target.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, -powerDrain);

            if (changedEnd == 0 && changedMana == 0)
                return;

            if (Caster is GamePlayer casterPlayer)
            {
                int endLost = Math.Abs(changedEnd);
                int manaLost = Math.Abs(changedMana);

                string msg = LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.OmniHarm.InflictLossesToTarget", endLost, manaLost, target.GetPersonalizedName(Caster));
                MessageToCaster(msg, eChatType.CT_Spell);
            }

            if (target is GamePlayer targetPlayer)
            {
                int endLost = Math.Abs(changedEnd);
                int manaLost = Math.Abs(changedMana);

                string msgTarget = LanguageMgr.GetTranslation(targetPlayer.Client, "SpellHandler.OmniHarm.InflictLossesToYou", Caster.GetPersonalizedName(target), endLost, manaLost);
                targetPlayer.Out.SendMessage(msgTarget, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            string baseDesc = LanguageMgr.GetTranslation(language, "SpellDescription.DirectDamage.MainDescription", Spell.Damage, LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType));
            int recastSeconds = Spell.RecastDelay / 1000;
            double endLoss = Spell.Damage * 0.15;
            double powerLoss = Spell.Damage * 0.55;
            string additionalLossDesc = LanguageMgr.GetTranslation(language, "SpellDescription.OmniHarm.MainDescription", endLoss, powerLoss);
            baseDesc += "\n\n" + additionalLossDesc;

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return baseDesc + "\n\n" + secondDesc;
            }

            return baseDesc;
        }
    }
}