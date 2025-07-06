using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Language;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
{
    [SpellHandler("TriggerBuff")]
    public class TriggerSpellHandler : SpellHandler
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public int TriggerHealth => (int)(Spell?.Value is null or 0 ? 100 : Spell.Value);
        public int TriggerSpellId => Spell?.SubSpellID ?? 0;
        public byte TriggerSpellLevel => (byte)(Caster?.Level ?? Spell?.Level ?? 0);
        
        public TriggerSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            CastSubSpellsWithSpell = false;
        }
        
        /* https://github.com/Mishura4/GondwanaServer/commit/a49cbd0050a2a640da0af3b63098c145288c877b#diff-27f99806169869d50b279201ea97faf27b9ed076e30b6bb22df043bc2ba8bd38
         * Since this commit ^ it seems this doesn't work anymore.
         * We'll disable this to eventually figure out what was the intent here
        public override bool CastSubSpells(GameLiving target)
        {
            List<int> subSpellList = new List<int>();
            if (m_spell.SubSpellID > 0)
                subSpellList.Add(m_spell.SubSpellID);

            foreach (int spellID in subSpellList.Union(m_spell.MultipleSubSpells))
            {
                Spell spell = SkillBase.GetSpellByID(spellID);
                //we need subspell ID to be 0, we don't want spells linking off the subspell
                if (target != null && spell != null)
                {
                    ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(m_caster, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    // TODO: And then?
                    spellhandler.Parent = this;
                }
            }
            return true;
        }
        */

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            if (Spell.SubSpellID == compare.Spell.SubSpellID)
                return true;
            
            return false;
        }

        class Effect : GameSpellEffect
        {
            /// <inheritdoc />
            public Effect(ISpellHandler handler, int duration, int pulseFreq) :
                base(handler, duration, pulseFreq)
            {
            }
            
            /// <inheritdoc />
            public Effect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness) :
                base(handler, duration, pulseFreq, effectiveness)
            {
            }

            internal void OnOwnerAttacked(DOLEvent e, object sender, EventArgs arguments)
            {
                if (IsDisabled || IsExpired || ImmunityState)
                    return;
                
                if (sender is not GameLiving owner
                    || SpellHandler is not TriggerSpellHandler triggerSpell
                    || arguments is not AttackedByEnemyEventArgs args)
                {
                    return;
                }
            
                double threshold = triggerSpell.TriggerHealth;
                if (threshold is > 0 and < 100)
                {
                    if (owner.HealthPercent < threshold)
                        return;
                }

                if (triggerSpell.CastSubSpells(args.AttackData!.Attacker))
                    Cancel(false);
            }
        }

        /// <inheritdoc />
        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            int freq = Spell != null ? Spell.Frequency : 0;
            return new Effect(this, CalculateEffectDuration(target, effectiveness), freq, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            if (effect is Effect triggerEffect && effect.Owner != null)
            {
                GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, triggerEffect.OnOwnerAttacked);
            }
        }

        /// <inheritdoc />
        public override void OnEffectRemove(GameSpellEffect effect, bool overwrite)
        {
            if (effect is Effect triggerEffect && effect.Owner != null)
            {
                GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, triggerEffect.OnOwnerAttacked);
            }
            
            base.OnEffectRemove(effect, overwrite);
        }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            var subSpell = ScriptMgr.CreateSpellHandler(m_caster, SkillBase.GetSpellByID((int)m_spell.SubSpellID), null);
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.Trigger.MainDescription", Spell.Name) + "\n\n" + subSpell.GetDelveDescription(delveClient);
        }
    }
}