using System;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.GS.Geometry;
using DOL.gameobjects.CustomNPC;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandler("BumpSpell")]
    public class BumpSpellHandler : SpellHandler
    {
        public BumpSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target == null || target == Caster || !target.IsAlive || target.IsStunned || target is ShadowNPC || target.EffectList.GetOfType<NecromancerShadeEffect>() != null)
                return;

            if (IsSwimming(target) || IsPeaceful(target))
                return;

            Position newPosition = GetBumpPosition(target);
            MoveTarget(target, newPosition);
        }

        private Position GetBumpPosition(GameLiving target)
        {
            int bumpHeight = (int)Spell.Value;
            int bumpMinDistance = Spell.LifeDrainReturn;
            int bumpMaxDistance = Spell.AmnesiaChance;

            // Calculate the bump distance
            int bumpDistance = Util.Random(bumpMinDistance, bumpMaxDistance);
            var bumpVector = Vector.Create(Caster.Orientation, bumpDistance) + Vector.Create(z: bumpHeight);

            // Calculate the new position by adding the bump vector to the target's current position
            Coordinate newCoordinate = target.Position.Coordinate + bumpVector;
            return target.Position.With(x: newCoordinate.X, y: newCoordinate.Y, z: newCoordinate.Z);
        }

        private void MoveTarget(GameLiving target, Position newPosition)
        {
            if (target is GamePlayer player)
            {
                player.MoveTo(newPosition);
                player.Out.SendEmoteAnimation(player, eEmote.Stagger);
            }
            else if (target is GameNPC npc)
            {
                npc.MoveWithoutRemovingFromWorld(newPosition, true);
                npc.Emote(eEmote.Stagger);
            }

            BroadcastMessage(target, "is hurled into the air!");
        }

        private void BroadcastMessage(GameLiving target, string message)
        {
            if (Caster is GamePlayer casterPlayer)
            {
                casterPlayer.Out.SendMessage($"{target.GetName(0, false)} {message}", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }
        }

        public override IList<GameLiving> SelectTargets(GameObject castTarget, bool force = false)
        {
            var targets = new List<GameLiving>();
            if (Spell.Target == "enemy")
            {
                if (castTarget is GameLiving livingTarget && livingTarget.IsAlive && livingTarget != Caster)
                {
                    targets.Add(livingTarget);
                }
            }
            else if (Spell.Target == "cone" || Spell.Target == "area")
            {
                targets.AddRange(GetTargetsInArea());
            }
            return targets;
        }

        private IEnumerable<GameLiving> GetTargetsInArea()
        {
            var targets = new List<GameLiving>();
            foreach (GamePlayer player in Caster.GetPlayersInRadius((ushort)Spell.Radius))
            {
                if (player.IsAlive && player != Caster && GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
                {
                    targets.Add(player);
                }
            }

            foreach (GameNPC npc in Caster.GetNPCsInRadius((ushort)Spell.Radius))
            {
                if (npc.IsAlive && npc != Caster && GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
                {
                    targets.Add(npc);
                }
            }
            return targets;
        }

        private bool IsSwimming(GameLiving target)
        {
            if (target is GamePlayer player)
            {
                return player.IsSwimming;
            }
            if (target is GameNPC npc)
            {
                return npc.Flags.HasFlag(GameNPC.eFlags.SWIMMING);
            }
            return false;
        }

        private bool IsPeaceful(GameLiving target)
        {
            if (target is GameNPC npc)
            {
                return npc.Flags.HasFlag(GameNPC.eFlags.PEACE);
            }
            return false;
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            return 100; // Always hit
        }
    }
}