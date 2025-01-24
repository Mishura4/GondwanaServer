using System;
using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("OmniHarm")]
    public class OmniHarmSpellHandler : DirectDamageSpellHandler
    {
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
            string baseDesc = base.GetDelveDescription(delveClient);
            int recastSeconds = Spell.RecastDelay / 1000;
            double endLoss = Spell.Damage * 0.15;
            double powerLoss = Spell.Damage * 0.55;
            string additionalLossDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.OmniHarm.MainDescription", endLoss, powerLoss);
            baseDesc += "\n\n" + additionalLossDesc;

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return baseDesc + "\n\n" + secondDesc;
            }

            return baseDesc;
        }
    }
}