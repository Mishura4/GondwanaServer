using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    public abstract class AbstractMorphSpellHandler : SpellHandler
    {
        /// <inheritdoc />
        protected AbstractMorphSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public int Priority
        {
            get;
            set;
        } = 0;

        public bool OverwritesMorphs
        {
            get;
            set;
        } = true;

        /// <inheritdoc />
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is IllusionPet)
                return false;
            
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        public virtual ushort GetModelFor(GameLiving living)
        {
            return (ushort)Spell.LifeDrainReturn;
        }

        /// <inheritdoc />
        public override bool IsOverwritable(GameSpellEffect compare)
        {
            if (compare.SpellHandler is AbstractMorphSpellHandler otherMorph && (OverwritesMorphs || otherMorph.OverwritesMorphs))
                return true;

            return base.IsOverwritable(compare);
        }
        
        public override void OnBetterThan(GameLiving target, GameSpellEffect oldEffect, GameSpellEffect newEffect)
        {
            SpellHandler attempt = (SpellHandler)newEffect.SpellHandler;
            if (attempt.Caster.GetController() is GamePlayer player)
                player.SendTranslatedMessage("Morphed.Target.Resist", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, player.GetPersonalizedName(target));
            attempt.SendSpellResistAnimation(target);
        }

        /// <inheritdoc />
        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
            if (oldeffect.SpellHandler is AbstractMorphSpellHandler otherMorph && neweffect.SpellHandler is AbstractMorphSpellHandler newMorph)
            {
                if (otherMorph.Priority >= newMorph.Priority)
                    return false;
                else if (otherMorph.Priority < newMorph.Priority)
                    return true;
            }
            return base.IsNewEffectBetter(oldeffect, neweffect);
        }

        /// <inheritdoc />
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            ushort model = GetModelFor(effect.Owner);
            if (model != 0)
            {
                effect.Owner.Model = model;
                effect.Owner.BroadcastUpdate();
                if (effect.Owner is GamePlayer ownerPlayer)
                {
                    ownerPlayer.Out.SendUpdatePlayer();
                    if (ownerPlayer.Group != null)
                    {
                        ownerPlayer.Group.UpdateMember(ownerPlayer, false, false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameSpellEffect bestEffect = null;
            ushort bestModel = 0;
            foreach (var otherEffect in effect.Owner.FindEffectsOnTarget(typeof(AbstractMorphSpellHandler)))
            {
                if (otherEffect == effect)
                    continue;
                
                var morph = otherEffect.SpellHandler as AbstractMorphSpellHandler;
                var model = morph.GetModelFor(effect.Owner);
                if (bestEffect == null)
                {
                    bestEffect = otherEffect;
                    bestModel = model;
                    continue;
                }

                if (model != 0 && IsNewEffectBetter(bestEffect, otherEffect))
                {
                    bestEffect = otherEffect;
                    bestModel = model;
                }
            }
            if (bestModel == 0)
            {
                if (effect.Owner is GamePlayer playerOwner)
                {
                    bestModel = playerOwner.CreationModel;
                }
                else if (effect.Owner is GameNPC livingOwner)
                {
                    bestModel = livingOwner.ModelDb;
                }
            }
            if (bestModel != 0 && bestModel != effect.Owner.Model)
            {
                effect.Owner.Model = bestModel;
                effect.Owner.BroadcastUpdate();
                if (effect.Owner is GamePlayer ownerPlayer)
                {
                    ownerPlayer.Out.SendUpdatePlayer();
                    if (ownerPlayer.Group != null)
                    {
                        ownerPlayer.Group.UpdateMember(ownerPlayer, false, false);
                    }
                }
            }
            return base.OnEffectExpires(effect, noMessages);
        }
    }

    public abstract class AbstractMorphOffensiveProc : OffensiveProcSpellHandler
    {
        /// <inheritdoc />
        protected AbstractMorphOffensiveProc(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}
