using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;
using DOL.Language;
using System;
using System.Linq;
using static DOL.GS.Region;

namespace DOL.GS.Spells
{
    [SpellHandler("Earthquake")]
    public class EarthquakeSpellHandler : SpellHandler
    {
        uint unk1 = 0;
        float radius, intensity, duration, delay = 0;
        private Position Position
        {
            get;
            set;
        }

        public EarthquakeSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            radius = 1200.0f;
            intensity = 50.0f;
            duration = 1000.0f;
        }

        private void SendPacketTo(GamePlayer player, float newIntensity)
        {
            GSTCPPacketOut pak = new GSTCPPacketOut(0x47);
            pak.WriteIntLowEndian(unk1);
            pak.WriteIntLowEndian((uint)Position.X);
            pak.WriteIntLowEndian((uint)Position.Y);
            pak.WriteIntLowEndian((uint)Position.Z);
            pak.Write(BitConverter.GetBytes(radius), 0, sizeof(float));
            pak.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(float));
            pak.Write(BitConverter.GetBytes(duration), 0, sizeof(float));
            pak.Write(BitConverter.GetBytes(delay), 0, sizeof(float));
            player.Out.SendTCP(pak);
        }

        private void SendPacketToAll()
        {
            // Explicitly handle the caster
            if (Caster is GamePlayer caster)
            {
                SendPacketTo(caster, intensity);
            }

            foreach (PlayerDistanceEnumerator enumerator in Position.Region.GetPlayersInRadius(Position.Coordinate, (ushort)radius, true, true))
            {
                var entry = (PlayerDistEntry)enumerator.Current;
                var player = entry.Player;
                int distance = (int)entry.Distance;

                if (player == Caster)
                    continue; // Skip caster, we already sent
                
                if (distance > radius)
                {
                    continue;
                }
                
                float newIntensity = intensity * (1 - distance / radius);

                SendPacketTo(player, newIntensity);
            }
        }

        private double elapsedTime = 0;

        public override void OnSpellPulse(PulsingSpellEffect effect)
        {
            elapsedTime += Spell.Frequency; // Assuming Spell.Frequency is the time between pulses

            // Check if the total duration has been exceeded
            if (elapsedTime >= duration)
            {
                effect.Cancel(false);
                MessageToCaster("The earthquake effect has ended.", eChatType.CT_SpellExpires);
                return;
            }

            if (Caster.ObjectState != GameObject.eObjectState.Active || Caster.IsMoving && Spell.IsFocus || Caster.IsStunned || Caster.IsMezzed || !Caster.IsAlive)
            {
                effect.Cancel(false);
                MessageToCaster("Your spell was cancelled.", eChatType.CT_SpellExpires);
                return;
            }

            if (Caster.Mana < Spell.PulsePower)
            {
                if (Spell.IsFocus)
                {
                    FocusSpellAction(null, Caster, null);
                }
                effect.Cancel(false);
                MessageToCaster("You do not have enough mana and your spell was cancelled.", eChatType.CT_SpellExpires);
                return;
            }

            Caster.Mana -= Spell.PulsePower;
            ApplyEffectOnTarget(Caster, 1.0);  // Caster experiences the effect
            SendPacketToAll();  // Send packet to all affected players

            foreach (GameLiving living in SelectTargets(m_spellTarget))
            {
                if (living == Caster) continue; // Skip the caster
                var chances = CalculateToHitChance(living);
                if (chances <= 0 || Util.Random(100) > chances)
                {
                    continue;
                }

                float distSq = living.GetDistanceSquaredTo(Position, 1.0f);
                if (distSq > radius * radius)
                    continue;

                double dist = Math.Sqrt(distSq);
                var effectiveness = (1 - dist / radius);
                ApplyEffectOnTarget(living, effectiveness);
            }
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (elapsedTime >= duration)
            {
                return;  // Stop applying damage once the duration is over
            }

            if (IsImmune(target))
            {
                return;
            }

            int distance = (int)target.Coordinate.DistanceTo(Position.Coordinate, true);
            if (distance > radius)
            {
                CancelPulsingSpell(target, Spell.SpellType);
                return;
            }

            float newIntensity = intensity * (1 - distance / radius);
            int damage = (int)newIntensity;
            if (target is GamePlayer player)
            {
                SendPacketTo(player, newIntensity);  // Update the intensity sent to players
            }

            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.Spell;
            ad.SpellHandler = this;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.IsSpellResisted = false;
            ad.Damage = damage;

            m_lastAttackData = ad;
            SendDamageMessages(ad);
            DamageTarget(ad, true);
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
        }

        private bool IsImmune(GameLiving living)
        {
            if (living == null)
            {
                return true;
            }

            if (living == Caster)
            {
                return true; // The caster is immune to their own earthquake
            }

            GameNPC npc = living as GameNPC;
            if (npc is { Brain: IControlledBrain controlledBrain })
            {
                return IsImmune(controlledBrain.Owner);
            }

            GamePlayer ownerPlayer = Caster as GamePlayer;
            if (ownerPlayer == null)
            {
                return false;
            }

            GamePlayer targetPlayer = living as GamePlayer;

            if (ownerPlayer.Group?.IsInTheGroup(living) == true)
            {
                return true; // Members of the caster's group are immune
            }

            if (ownerPlayer.Guild != null)
            {
                if (npc != null)
                {
                    if (npc.CurrentTerritory?.IsOwnedBy(ownerPlayer.Guild) == true)
                    {
                        return true;
                    }

                    if (!string.IsNullOrEmpty(npc.GuildName) && string.Equals(npc.GuildName, ownerPlayer.Guild.Name))
                    {
                        return true;
                    }
                }
                else if (targetPlayer != null)
                {
                    if (targetPlayer.Guild == ownerPlayer.Guild)
                    {
                        return true; // Members of the caster's guild are immune
                    }
                }
            }

            if (ownerPlayer.BattleGroup != null && targetPlayer?.BattleGroup == ownerPlayer.BattleGroup)
            {
                return true; // Members of the caster's battle group are immune
            }

            return false; // Not immune
        }

        /// <inheritdoc />
        public override void OnDurationEffectApply(GameLiving target, double effectiveness)
        {
            var hitIntensity = intensity * effectiveness;
            int damage = Math.Max(0, (int)(hitIntensity));
            if (target is GamePlayer player)
                SendPacketTo(player, (float)hitIntensity);

            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.Spell;
            ad.SpellHandler = this;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.IsSpellResisted = false;
            ad.Damage = damage;
            target.TakeDamage(ad);

            foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>())
            {
                p.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, 0, false, 1);
            }

            base.OnDirectEffect(target, effectiveness);
        }

        /// <inheritdoc />
        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            var hitIntensity = intensity * effectiveness;
            int damage = Math.Max(0, (int)(hitIntensity));
            if (target is GamePlayer player)
                SendPacketTo(player, (float)hitIntensity);

            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.Spell;
            ad.SpellHandler = this;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.IsSpellResisted = false;
            ad.Damage = damage;
            target.TakeDamage(ad);

            foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Cast<GamePlayer>())
            {
                p.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, 0, false, 1);
            }

            base.OnDirectEffect(target, effectiveness);
        }

        public override bool StartSpell(GameLiving target, bool force)
        {
            if (Spell.Pulse != 0 && !Spell.IsFocus && CancelPulsingSpell(Caster, Spell.SpellType))
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PulsingSpellCancelled"), eChatType.CT_SpellExpires);
            }

            var targetType = Spell.Target.ToLowerInvariant();
            if (targetType == "area")
            {
                if (Caster.GroundTargetPosition == Position.Nowhere)
                    Position = Caster.Position;
                else
                    Position = Caster.GroundTargetPosition;
            }
            else
            {
                Position = target.Position;
            }
            /*if (args.Length > 1)
            {
                try
                {
                    unk1 = (uint)Convert.ToSingle(args[1]);
                }
                catch { }
            }*/
            if (Spell.Radius > 0)
            {
                radius = Spell.Radius;
            }
            if (Spell.Damage > 0)
            {
                intensity = (float)Spell.Damage;
            }
            if (Spell.Duration > 0)
            {
                duration = Spell.Duration;
            }
            if (Spell.CastTime > 0)
            {
                delay = Spell.CastTime;
            }
            
            if (!base.StartSpell(target, force))
                return false;

            if (Caster is GamePlayer caster)
                SendPacketTo(caster, intensity);

            if (m_spell.Pulse != 0 && m_spell.Frequency > 0)
            {
                CancelAllPulsingSpells(Caster);
                PulsingSpellEffect pulseeffect = new PulsingSpellEffect(this);
                pulseeffect.Start();
            }
            return true;
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            if (target is GameNPC npc)
            {
                if (npc.Flags.HasFlag(GameNPC.eFlags.FLYING) || npc.Flags.HasFlag(GameNPC.eFlags.SWIMMING))
                {
                    return 0;
                }
                else
                {
                    return 100;
                }
            }
            else
            {
                GamePlayer player = target as GamePlayer;
                if (player.IsSwimming ||
                    (player.CharacterClass is ClassVampiir && player.IsSprinting && player.CurrentSpeed == player.MaxSpeed) ||
                    (player.CharacterClass is ClassBainshee && (player.Model == 1883 || player.Model == 1884 || player.Model == 1885)))
                {
                    return 0;
                }
                else
                {
                    return 100;
                }
            }
        }

        public override string ShortDescription
            => $"Creates an earthquake having an intensity of {Spell.Value / 50} on the Richter scale, causing from {Spell.Damage} damages near the epicenter to {Spell.AmnesiaChance} damages at the farthest points. Earthquakes have no effect on flying mobs, swimming mobs, bainshees or floating vampiirs.";
    }
}
