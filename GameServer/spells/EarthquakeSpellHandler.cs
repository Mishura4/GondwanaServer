using DOL.Geometry;
using DOL.GS.Effects;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("Earthquake")]
    public class EarthquakeSpellHandler : SpellHandler
    {
        uint unk1 = 0;
        float radius, intensity, duration, delay = 0;
        private Coordinate Coordinate
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

        public override bool StartSpell(GameLiving targetObject)
        {
            if (Spell.Pulse != 0 && !Spell.IsFocus && CancelPulsingSpell(Caster, Spell.SpellType))
            {
                MessageToCaster("Your spell was cancelled.", eChatType.CT_SpellExpires);
                return false;
            }
            if (Caster.GroundTargetPosition == Position.Nowhere)
            {
                Coordinate = Caster.Coordinate;
            }
            else
            {
                Coordinate = Caster.GroundTargetPosition.Coordinate;
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
                try
                {
                    radius = Spell.Radius;
                }
                catch { }
            }
            if (Spell.Damage > 0)
            {
                try
                {
                    intensity = (float)Spell.Damage;
                }
                catch { }
            }
            if (Spell.Duration > 0)
            {
                try
                {
                    duration = Spell.Duration;
                }
                catch { }
            }
            if (Spell.CastTime > 0)
            {
                try
                {
                    delay = Spell.CastTime;
                }
                catch { }
            }

            if (Caster is GamePlayer player)
            {
                int distance = (int)player.Coordinate.DistanceTo(Coordinate, true);
                float newIntensity = intensity * (1 - distance / radius);
                GSTCPPacketOut pak = new GSTCPPacketOut(0x47);
                pak.WriteIntLowEndian(unk1);
                pak.WriteIntLowEndian((uint)Coordinate.X);
                pak.WriteIntLowEndian((uint)Coordinate.Y);
                pak.WriteIntLowEndian((uint)Coordinate.Z);
                pak.Write(BitConverter.GetBytes(radius), 0, sizeof(float));
                pak.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(float));
                pak.Write(BitConverter.GetBytes(duration), 0, sizeof(float));
                pak.Write(BitConverter.GetBytes(delay), 0, sizeof(float));
                player.Out.SendTCP(pak);
            }

            if (m_spell.Pulse != 0 && m_spell.Frequency > 0)
            {
                CancelAllPulsingSpells(Caster);
                PulsingSpellEffect pulseeffect = new PulsingSpellEffect(this);
                pulseeffect.Start();
            }

            return base.StartSpell(targetObject);
        }

        public override void OnSpellPulse(PulsingSpellEffect effect)
        {
            if (Caster.IsMoving && Spell.IsFocus)
            {
                MessageToCaster("Your spell was cancelled.", eChatType.CT_SpellExpires);
                effect.Cancel(false);
                return;
            }
            if (Caster.IsAlive == false)
            {
                effect.Cancel(false);
                return;
            }
            if (Caster.ObjectState != GameObject.eObjectState.Active)
                return;
            if (Caster.IsStunned || Caster.IsMezzed)
                return;


            if (Caster.Mana >= Spell.PulsePower)
            {
                Caster.Mana -= Spell.PulsePower;

                if (Caster is GamePlayer player)
                {
                    int distance = (int)player.Coordinate.DistanceTo(Coordinate, true);
                    float newIntensity = intensity * (1 - distance / radius);
                    GSTCPPacketOut pak = new GSTCPPacketOut(0x47);
                    pak.WriteIntLowEndian(unk1);
                    pak.WriteIntLowEndian((uint)Coordinate.X);
                    pak.WriteIntLowEndian((uint)Coordinate.Y);
                    pak.WriteIntLowEndian((uint)Coordinate.Z);
                    pak.Write(BitConverter.GetBytes(radius), 0, sizeof(float));
                    pak.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(float));
                    pak.Write(BitConverter.GetBytes(duration), 0, sizeof(float));
                    pak.Write(BitConverter.GetBytes(delay), 0, sizeof(float));
                    player.Out.SendTCP(pak);
                }
                base.StartSpell(m_spellTarget);
            }
            else
            {
                if (Spell.IsFocus)
                {
                    FocusSpellAction(null, Caster, null);
                }
                MessageToCaster("You do not have enough mana and your spell was cancelled.", eChatType.CT_SpellExpires);
                effect.Cancel(false);
            }
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            int distance = (int)target.Coordinate.DistanceTo(Coordinate, true);
            if (distance > radius)
            {
                CancelPulsingSpell(target, Spell.SpellType);
                return;
            }

            float newIntensity = intensity * (1 - distance / radius);
            int damage = (int)newIntensity;
            if (target is GamePlayer player)
            {
                if (player == Caster as GamePlayer)
                    return;
                GSTCPPacketOut pakBis = new GSTCPPacketOut(0x47);
                pakBis.WriteIntLowEndian(unk1);
                pakBis.WriteIntLowEndian((uint)Coordinate.X);
                pakBis.WriteIntLowEndian((uint)Coordinate.Y);
                pakBis.WriteIntLowEndian((uint)Coordinate.Z);
                pakBis.Write(BitConverter.GetBytes(radius), 0, sizeof(float));
                pakBis.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(float));
                pakBis.Write(BitConverter.GetBytes(duration), 0, sizeof(float));
                pakBis.Write(BitConverter.GetBytes(delay), 0, sizeof(float));
                player.Out.SendTCP(pakBis);
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