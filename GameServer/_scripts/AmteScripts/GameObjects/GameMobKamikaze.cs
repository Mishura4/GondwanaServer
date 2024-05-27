using System;
using System.Reflection;
using System.Collections;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;
using DOL.GS.Scripts;
using DOL.GS.Spells;
using DOL.Events;

namespace DOL.GS
{
    namespace Spells
    {
        // Ce mob, une fois pris en aggro, cours vers le premier joueur vu
        // et le premier coup donné par ce mob est synonyme de lancement
        // d'un sort puissant
        [SpellHandler("KamiBomb")]
        public class KamiBomb : DirectDamageSpellHandler
        {
            public const float MIN_DISTANCE_HIT = 300.0f;
            public const float MAX_DISTANCE_HIT = 1000.0f;
            public const float MAX_PERC_HIT = 0.75f;

            public KamiBomb(GameLiving caster, Spell spell, SpellLine spellLine)
                : base(caster, spell, spellLine)
            {
            }

            public override AttackData CalculateDamageToTarget(GameLiving target, double effectiveness)
            {
                // Quand on est à moins de 100 unité du mob : On perd 75% de sa vie
                // Maximum à 999 unité ou on ne perd que 1% de sa vie.

                float Factor = Caster.GetDistanceTo(target);
                if (Factor <= MIN_DISTANCE_HIT)
                    Factor = MAX_PERC_HIT;
                else if (Factor > MAX_DISTANCE_HIT)
                    // Ne devrait PAS arriver
                    Factor = 0.0f;
                else
                    Factor = 0.5f;

                AttackData ad = new AttackData();
                ad.Attacker = Caster;
                ad.Target = target;
                ad.AttackType = AttackData.eAttackType.Spell;
                ad.SpellHandler = this;
                ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;

                ad.Damage = (int)(target.MaxHealth * Factor);
                ad.CriticalDamage = 0;
                ad.DamageType = DetermineSpellDamageType();
                ad.Modifier = 0;
                m_lastAttackData = ad;

                return ad;
            }

            public override void OnAfterSpellCastSequence()
            {
                base.OnAfterSpellCastSequence();
                var mob = Caster as GameMobKamikaze;
                if (mob != null)
                {
                    mob.Health = 0;
                    mob.Die(null);
                }
            }
        }
    }

    public class GameMobKamikaze : GameNPC
    {
        static private Spell m_KamiSpell = null;
        static private Spell KamiSpell
        {
            get
            {
                if (m_KamiSpell == null)
                {
                    DBSpell TheSpell = new DBSpell();
                    TheSpell.CastTime = 0.0;
                    TheSpell.MoveCast = true;
                    TheSpell.Uninterruptible = true;
                    TheSpell.ClientEffect = 4567;
                    // Besoin d'un bon effet !
                    TheSpell.Icon = 4567; // Inutile mais nécessaire je pense

                    TheSpell.Duration = 0;

                    TheSpell.Value = 161;
                    TheSpell.Power = 0;
                    TheSpell.Radius = (int)KamiBomb.MAX_DISTANCE_HIT;
                    TheSpell.Name = "Tricheur";
                    TheSpell.Description = "Si quelqu'un voit ça, c'est qu'il triche :D";
                    TheSpell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    TheSpell.Radius = 0;
                    TheSpell.SpellID = 25668;
                    TheSpell.Target = "enemy";
                    TheSpell.Type = "KamiBomb";
                    m_KamiSpell = new Spell(TheSpell, 1);
                }
                return m_KamiSpell;
            }
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object obj, EventArgs args)
        {
            GameEventMgr.AddHandler(GameLivingEvent.AttackFinished,
            new DOLEventHandler(MobAttackFinished));
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object obj, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GameLivingEvent.AttackFinished,
            new DOLEventHandler(MobAttackFinished));
        }

        public GameMobKamikaze() :
            base()
        {
            LoadedFromScript = false;
        }

        public static void MobAttackFinished(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is GameMobKamikaze)
            {
                var finishArgs = args as AttackFinishedEventArgs;

                if (finishArgs != null)
                {
                    var attacker = finishArgs.AttackData.Attacker as GameNPC;

                    if (attacker != null)
                    {
                        attacker.CastSpell(KamiSpell,
                        SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
            }
        }

        public override void DealDamage(AttackData ad)
        {
            //Le mob kamikaze ne peut pas faire de dommage de manière 'normale'
            // Ca signifirait simplement qu'il est buggé..

            //On ne permet que les dégats de spell
            if (ad.AttackType == AttackData.eAttackType.Spell)
            {
                base.DealDamage(ad);
            }
        }
    }
}