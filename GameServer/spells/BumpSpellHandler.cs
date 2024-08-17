using System;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS.Geometry;
using DOL.gameobjects.CustomNPC;
using DOL.GS.Effects;
using System.Collections;
using System.Linq;
using log4net;

namespace DOL.GS.Spells
{
    [SpellHandler("BumpSpell")]
    public class BumpSpellHandler : SpellHandler
    {
        public class BumpTrajectory
        {
            public record Point(Coordinate Coordinate, int Milliseconds);
            
            public double Gravity => -500.0f;

            public int NumSegments => 9;

            public int MaxSegments => NumSegments * 2;

            public List<Point> Points { get; } = new List<Point>();
            
            public int HorizontalSpeed { get; set; }

            public double InitialVerticalVelocity { get; set; }

            public Position Start { get; set; }

            public BumpTrajectory(Position start, short height, int distance)
            {
                Start = start;
                InitialVerticalVelocity = Math.Round(Math.Sqrt(2 * height * -Gravity));
                // https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
                // z = 0.5 g * t^2 + v * t
                // z = 0 <=> x = (-b +- sqrt(b^2 - 4ac) / 2a)
                //           with a = 0.5g, b = v, c = 0, x = t
                //
                // z = 0 <=> t = (-v +- sqrt(v^2 - 4 * 0.5g * 0)) / 2 * (0.5g)
                //           t = (-v +- sqrt(v^2)) / g
                //           t = (-v +- v) / g
                //           t = 0 and t = -2v / g
                double airTime = 2 * (InitialVerticalVelocity / -Gravity);
                HorizontalSpeed = (short)Math.Round(distance / airTime);
                int msIncrement = Math.Max(1, (int)Math.Round((airTime * 1000) / NumSegments));
                Coordinate currentPoint = Start.Coordinate;
                AddPoint(new Point(currentPoint, 0));
                int timeElapsed = 0;
                for (int i = 0; i < MaxSegments; ++i)
                {
                    RaycastStats stats = new RaycastStats();
                    Coordinate nextPoint = CalculateCoordinateAfter(timeElapsed + msIncrement);
                    if (nextPoint.Equals(currentPoint))
                        continue;
                    float collisionDist = LosCheckMgr.GetCollisionDistance(Start.Region, currentPoint, nextPoint, ref stats);
                    if (!float.IsInfinity(collisionDist))
                    {
                        var vector = (nextPoint - currentPoint);
                        double factor = (collisionDist / vector.Length) - double.Epsilon;
                        if (factor > 0)
                        {
                            timeElapsed = (int)(timeElapsed + msIncrement * factor);
                            nextPoint = currentPoint + (vector * factor);
                            AddPoint(new Point(nextPoint, timeElapsed));
                        }
                        break;
                    }
                    timeElapsed += msIncrement;
                    AddPoint(new Point(nextPoint, timeElapsed));
                    currentPoint = nextPoint;
                }
            }

            private void AddPoint(Point point)
            {
                Points.Add(point);
            }

            public double GetVerticalSpeedAt(int milliseconds)
            {
                double elapsedSeconds =milliseconds / 1000.0;
                return InitialVerticalVelocity + Gravity * elapsedSeconds;
            }

            public Coordinate CalculateCoordinateAfter(int elapsedMilliseconds)
            {
                if (elapsedMilliseconds <= 0)
                {
                    return Start.Coordinate;
                }
                double seconds = (double)elapsedMilliseconds / 1000;
                double z = 0.5 * Gravity * seconds * seconds + InitialVerticalVelocity * seconds;
                Vector v = Vector.Create(Start.Orientation, (int)(HorizontalSpeed * seconds)) + Vector.Create(0, 0, (int)Math.Round(z));
                return Start.Coordinate + v;
            }
        }

        public class Victim
        {
            public GameNPC Npc { get; init; }

            public BumpTrajectory Trajectory { get; init; }
            
            public int CurrentPoint { get; set; }
            
            public bool IsBumpNpc { get; init; }
            
            public BumpSpellHandler SpellHandler { get; init; }

            public RegionTimer Timer { get; init; }

            public Victim(BumpSpellHandler spellHandler, GameNPC npc, BumpTrajectory trajectory)
            {
                SpellHandler = spellHandler;
                Npc = npc;
                Trajectory = trajectory;
                CurrentPoint = 0;
                IsBumpNpc = npc is BumpNPC;
                Timer = new RegionTimer(npc, TimerCallback);
            }
            
            private int TimerCallback(RegionTimer callingtimer)
            {
                if (Npc.ObjectState != GameObject.eObjectState.Active)
                {
                    SpellHandler.Cleanup(this);
                    return 0;
                }
                int timeNow = Trajectory.Points[CurrentPoint].Milliseconds;
                if (!MoveToNext())
                {
                    SpellHandler.Finish(this, Trajectory.GetVerticalSpeedAt(Trajectory.Points.Last().Milliseconds));
                    return 0;
                }
                int next = Trajectory.Points[CurrentPoint].Milliseconds - timeNow;
                return next;
            }

            public void Start()
            {
                if (CurrentPoint + 1 >= Trajectory.Points.Count)
                    return;

                int delay = Trajectory.Points[CurrentPoint + 1].Milliseconds - Trajectory.Points[CurrentPoint].Milliseconds;
                if (!MoveToNext())
                    return;

                Timer.Start(delay);
            }

            private bool MoveToNext()
            {
                ++CurrentPoint;
                if (CurrentPoint >= Trajectory.Points.Count)
                {
                    return false;
                }
                Npc.WalkTo(Trajectory.Points[CurrentPoint].Coordinate, (short)Trajectory.HorizontalSpeed);
                //Npc.Motion = Motion.Create(Npc.Position, Trajectory.Points[CurrentPoint].Coordinate, (short)Trajectory.HorizontalSpeed);
                return true;
            }
        }

        private List<Victim> _npcVictims = new();

        class BumpNPC : GameNPC
        {
            public List<GamePlayer> Victims { get; init; } = new List<GamePlayer>();
            
            public BumpSpellHandler SpellHandler { get; init; }

            public BumpNPC(BumpSpellHandler spellHandler)
            {
                SpellHandler = spellHandler;
            }

            public void Grab(GamePlayer victim)
            {
                Victims.Add(victim);
                
                victim.IsStunned = true;
                victim.DebuffCategory[(int)eProperty.SpellFumbleChance] += 100;
                victim.StopAttack();
                victim.StopCurrentSpellcast();
                victim.MountSteed(this, true);
                victim.Emote(eEmote.Stagger);

                SpellHandler.BroadcastMessage(victim, "is hurled into the air!");
            }
        }

        public BumpSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            Height = (int)Spell.Value;
            MinDistance = Spell.LifeDrainReturn;
            MaxDistance = Spell.AmnesiaChance;
        }

        public int Height
        {
            get;
            set;
        }

        public int MinDistance
        {
            get;
            set;
        }

        public int MaxDistance
        {
            get;
            set;
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target == Caster || !target.IsAlive || target is ShadowNPC || target.EffectList.GetOfType<NecromancerShadeEffect>() != null)
                return;

            if (IsSwimming(target) || IsPeaceful(target))
                return;

            // Calculate the bump distance
            int bumpDistance = Util.Random(MinDistance, MaxDistance);
            BumpTrajectory trajectory;
            if (target is GamePlayer player)
            {
                BumpNPC npc = (BumpNPC)_npcVictims.FirstOrDefault(n => n.IsBumpNpc && n.Npc.IsWithinRadius2D(target, 8.0f))?.Npc;
                if (npc == null)
                {
                    trajectory = _npcVictims.Find(t => GameMath.IsWithinRadius2D(t.Trajectory.Start.Coordinate, target.Coordinate, 8.0f))?.Trajectory;
                    if (trajectory == null || trajectory.Points.Count <= 1)
                    {
                        trajectory = new BumpTrajectory(target.Position.With(Caster.Orientation), (short)Height, bumpDistance);
                        if (trajectory.Points.Count <= 1)
                            return;
                    }
                    npc = new BumpNPC(this)
                    {
                        Realm = Caster.Realm,
                        Model = 667,
                        Position = target.Position.With(Caster.Orientation),
                        Name = "Bump",
                        Level = Caster.Level,
                        Flags = GameNPC.eFlags.PEACE | GameNPC.eFlags.DONTSHOWNAME | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.FLYING,
                        MaxSpeedBase = 4000
                    };
                    var victim = new Victim(this, npc, trajectory);
                    npc.AddToWorld();
                    _npcVictims.Add(victim);
                    victim.Start();
                }
                npc.Grab(player);
            }
            else if (target is GameNPC npc)
            {
                trajectory = _npcVictims.Find(t => GameMath.IsWithinRadius2D(t.Trajectory.Start.Coordinate, target.Coordinate, 8.0f))?.Trajectory;
                if (trajectory == null || trajectory.Points.Count <= 1)
                {
                    trajectory = new BumpTrajectory(target.Position.With(Caster.Orientation), (short)Height, bumpDistance);
                    if (trajectory.Points.Count <= 1)
                        return;
                }
                var victim = new Victim(this, npc, trajectory);
                _npcVictims.Add(victim);
                npc.StopMoving();
                npc.Brain.Stop();
                npc.Flags |= GameNPC.eFlags.FLYING;
                npc.IsFrozen = true;
                npc.DebuffCategory[(int)eProperty.SpellFumbleChance] += 100;
                npc.StopAttack();
                npc.StopCurrentSpellcast();
                npc.Emote(eEmote.Stagger);
                victim.Start();

                BroadcastMessage(npc, "is hurled into the air!");
            }
        }

        private void BroadcastMessage(GameLiving target, string message)
        {
            if (Caster is GamePlayer casterPlayer)
            {
                casterPlayer.Out.SendMessage($"{target.GetName(0, false)} {message}", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }
        }

        private void DoFinalEffects(GameLiving living, double speed)
        {
            living.DebuffCategory[(int)eProperty.SpellFumbleChance] -= 100;
            
            int safeFallLevel = living.GetAbilityLevel(Abilities.SafeFall);
            
            var fallSpeed = -speed - (100 * safeFallLevel);

            int fallDivide = 15;
            int fallMinSpeed = 500;

            var fallPercent = (int)Math.Min(99, (fallSpeed - (fallMinSpeed + 1)) / fallDivide);

            if (fallSpeed > fallMinSpeed)
            {
                fallPercent = Math.Max(0, fallPercent - safeFallLevel);
                living.TakeFallDamage(fallPercent);
            }
        }

        private void Finish(Victim victim, double speed)
        {
            if (victim.IsBumpNpc)
            {
                foreach (GamePlayer player in ((BumpNPC)victim.Npc).Victims)
                {
                    player.IsStunned = false;
                    player.DismountSteed(true);
                    DoFinalEffects(player, speed);
                }
            }
            else
            {
                victim.Npc.Flags &= ~GameNPC.eFlags.FLYING;
                victim.Npc.IsFrozen = false;
                DoFinalEffects(victim.Npc, speed);
            }
            Cleanup(victim);
        }

        private void Cleanup(Victim victim)
        {
            _npcVictims.Remove(victim);
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