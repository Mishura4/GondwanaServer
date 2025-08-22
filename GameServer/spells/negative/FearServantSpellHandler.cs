using System;
using System.Collections.Generic;
using System.Linq;

using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Fear for SERVANTS (player pets / controlled NPCs / charmed mobs).
    /// Regular free NPCs are NOT valid targets.
    /// Illusion pets die on hit.
    /// While active, a PetFearBrain is layered to make the pet flee the caster and ignore owner commands.
    /// </summary>
    [SpellHandler("FearServant")]
    public class FearServantSpellHandler : SpellHandler
    {
        private readonly ReaderWriterDictionary<GameNPC, PetFearBrain> _petFearBrains = new ReaderWriterDictionary<GameNPC, PetFearBrain>();

        private const string PET_FEAR_FLAG = "FEAR_SERVANT_ACTIVE";

        private static bool HasEffect(GameLiving target, string spellType)
            => SpellHandler.FindEffectOnTarget(target, spellType) != null;

        /// <summary>
        /// Is the NPC currently charmed or befriended?
        /// </summary>
        private static bool IsCharmedOrFriend(GameNPC npc)
            => HasEffect(npc, "Charm") || HasEffect(npc, "BeFriend") || (npc.Brain is FriendBrain);

        /// <summary>
        /// True if this NPC is a valid "servant" (player-owned/controlled) target.
        /// </summary>
        private static bool IsEligiblePetTarget(GameNPC npc)
        {
            if (npc.Brain is ControlledNpcBrain) return true;
            if (npc.Brain is TheurgistPetBrain) return true;
            if (npc.Brain is NoveltyPetBrain) return true;

            // Charmed followers (friend/following brains or status)
            if (npc.Brain is FollowingFriendMobBrain) return true;
            if (IsCharmedOrFriend(npc)) return true;

            // Illusion pets are also "servants" for this spell; they’ll be killed on hit.
            if (npc.Brain is IllusionPetBrain) return true;

            return false;
        }

        public override void FinishSpellCast(GameLiving target, bool force = false)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target, force);
        }

        /// <summary>
        /// Only target NPCs that are actually pets/controlled/charmed/illusion.
        /// </summary>
        public override IList<GameLiving> SelectTargets(GameObject castTarget, bool force = false)
        {
            return base.SelectTargets(castTarget, force)
                       .Where(t => t is GameNPC npc && IsEligiblePetTarget(npc))
                       .ToList();
        }

        /// <summary>
        /// Apply: kill illusion pets; otherwise attach PetFearBrain if level check passes.
        /// </summary>
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            var npcTarget = target as GameNPC;
            if (npcTarget == null) return false;

            if (!IsEligiblePetTarget(npcTarget))
                return false;

            if (npcTarget.Brain is IllusionPetBrain || npcTarget.Brain is TurretBrain)
            {
                if (npcTarget.IsAlive)
                    npcTarget.Die(m_caster);
                return true;
            }

            // Let "regular controlled pet" through as well (the condition you asked to reuse)
            //   regular controlled pet = ControlledNpcBrain AND not charmed/followingfriend/illusion
            // We also allow charmed followers through (FollowingFriend/Friend/Charm effects).
            // Resist by level like Fear
            if (npcTarget.Level > Spell.Value)
            {
                OnSpellResisted(target);
                return true;
            }

            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        /// <summary>
        /// On start: set a temp-flag so command methods can ignore owner orders;
        /// add a PetFearBrain that specifically flees from the caster.
        /// </summary>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            var npcTarget = effect.Owner as GameNPC;
            if (npcTarget == null) return;

            npcTarget.TempProperties.setProperty(PET_FEAR_FLAG, true);

            // layer a caster-aware fear brain
            var fearBrain = new PetFearBrain(m_caster);
            _petFearBrains.AddOrReplace(npcTarget, fearBrain);

            npcTarget.AddBrain(fearBrain);
            fearBrain.Think();

            base.OnEffectStart(effect);
        }

        /// <summary>
        /// On expire: remove the PetFearBrain and clear flag.
        /// </summary>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var npcTarget = effect.Owner as GameNPC;

            if (npcTarget != null && _petFearBrains.TryRemove(npcTarget, out var fearBrain))
            {
                // let the brain clean itself (clear flags, stop fleeing)
                fearBrain.RemoveEffect();
                npcTarget.RemoveBrain(fearBrain);
            }

            npcTarget?.TempProperties.removeProperty(PET_FEAR_FLAG);

            if (npcTarget != null && npcTarget.Brain == null)
                npcTarget.AddBrain(new StandardMobBrain());

            return base.OnEffectExpires(effect, noMessages);
        }

        public override void OnSpellResisted(GameLiving target)
        {
            SendSpellResistAnimation(target);
            SendSpellResistMessages(target);
            StartSpellResistLastAttackTimer(target);
        }

        public FearServantSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc;
            if (Spell.Radius > 0)
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.FearServant.AreaTarget", Spell.Value, Spell.Radius);
            else
                mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.FearServant.SingleTarget", Spell.Value);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}