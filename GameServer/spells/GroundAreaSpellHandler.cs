using DOL.AI;
using DOL.Database;
using DOL.Events;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("GroundArea")]
    public class GroundAreaSpellHandler : SpellHandler
    {
        internal class GroundAreaTurret : GameMovingObject
        {
            public GroundAreaTurret(ABrain brain) : base()
            {
                SetOwnBrain(brain);
            }
        }
        
        internal class GroundAreaTurretBrain : ABrain
        {
            public SpellHandler MasterSpellHandler { get; init; }

            private List<Spell> m_spells;

            public override int ThinkInterval => MasterSpellHandler.Spell.Frequency;

            public GroundAreaTurretBrain(GroundAreaSpellHandler handler)
            {
                MasterSpellHandler = handler;
                m_spells = handler.GetTurretSpells().ToList();
            }


            /// <inheritdoc />
            public override bool Start()
            {
                if (!base.Start())
                    return false;

                if (Body is GamePet pet)
                {
                    foreach (Spell spell in Body.Spells)
                    {
                        pet.ScalePetSpell(spell);
                    }
                }

                return true;
            }

            /// <inheritdoc />
            public override void Think()
            {
                Body.GroundTargetPosition = Body.Position; // Set the turret's ground position
                CastSubSpellsOnTargets(); // Cast spells on targets within range
            }

            private void CastSubSpellsOnTargets()
            {
                var newTargets = new List<GameLiving>();
                var oldTargets = new List<GameLiving>();

                // Gather players in radius
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)MasterSpellHandler.Spell.Radius))
                {
                    if (!GameServer.ServerRules.IsAllowedToAttack(Body, player, true) || player.IsInvulnerableToAttack)
                        continue;

                    if (player.IsAlive && !player.IsMezzed && !player.IsStealthed)
                    {
                        if (HasEffect(player))
                        {
                            oldTargets.Add(player);
                        }
                        else
                        {
                            newTargets.Add(player);
                        }
                    }
                }

                // Gather NPCs in radius
                foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)MasterSpellHandler.Spell.Radius))
                {
                    if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
                        continue;

                    if (npc.IsAlive && !npc.IsMezzed && !npc.IsStealthed)
                    {
                        if (HasEffect(npc))
                        {
                            oldTargets.Add(npc);
                        }
                        else
                        {
                            newTargets.Add(npc);
                        }
                    }
                }

                GameLiving target = newTargets.Count > 0 ? newTargets[Util.Random(newTargets.Count - 1)] : oldTargets.Count > 0 ? oldTargets[Util.Random(oldTargets.Count - 1)] : null;

                if (target != null)
                {
                    foreach (Spell spell in m_spells)
                    {
                        CastSpellOnTarget(spell, target);
                    }
                }
            }

            private bool HasEffect(GameLiving target)
            {
                foreach (var spell in m_spells)
                {
                    if (SpellHandler.FindEffectOnTarget(target, spell.SpellType) != null)
                        return true;
                }
                return false;
            }

            private bool CastSpellOnTarget(Spell spell, GameLiving target)
            {
                if (spell == null || target == null || !target.IsAlive)
                    return false;

                if (spell.Range > 0)
                {
                    if (Body.IsWithinRadius(target, spell.Range))
                    {
                        Body.TargetObject = target;
                        if (spell.CastTime > 0)
                        {
                            Body.TurnTo(target);
                        }
                        Body.CastSpell(spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                        return true;
                    }
                }
                else // For AoE spells or non-targeted spells
                {
                    Body.CastSpell(spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    return true;
                }

                return false;
            }
        }

        public GroundAreaSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public GameNPC Turret
        {
            get;
            private set;
        }

        public RegionTimer DurationTimer
        {
            get;
            private set;
        }

        protected virtual GameNPC CreateTurret()
        {
            var brain = new GroundAreaTurretBrain(this);

            GroundAreaTurret pet = new GroundAreaTurret(brain);
            pet.Model = 2588;
            pet.Name = Spell.Name;
            pet.Realm = Caster.Realm;
            pet.Owner = Caster;
            pet.Flags = GameNPC.eFlags.CANTTARGET;
            pet.Effectiveness = Caster.Effectiveness;
            pet.Spells = GetTurretSpells().ToList();
            return pet;
        }

        protected virtual IEnumerable<Spell> GetTurretSpells()
        {
            List<int> subSpellList = new List<int>();
            if (m_spell.SubSpellID > 0)
                subSpellList.Add(m_spell.SubSpellID);

            return subSpellList.Union(m_spell.MultipleSubSpells).Select(SkillBase.GetSpellByID).Where(s => s != null);
        }
        
        public const string LIVING_GROUNDEFFECT_PROPERTY = "LIVING_GROUND_EFFECT";

        /// <inheritdoc />
        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            GameLiving trueCaster = Caster.GetController();
            
            object casterEffect = trueCaster.TempProperties.getProperty<object>(LIVING_GROUNDEFFECT_PROPERTY, null);
            if (casterEffect != null)
            {
                if (!quiet && trueCaster is GamePlayer casterPlayer)
                {
                    casterPlayer.SendTranslatedMessage("SpellHandler.GroundAreaEffect.AlreayActive", eChatType.CT_SpellResisted);
                }
                return false;
            }
            return base.CheckBeginCast(selectedTarget, quiet);
        }

        /// <inheritdoc />
        public override bool StartSpell(GameLiving target, bool force = false)
        {
            m_spellTarget = Caster;
            GameLiving trueCaster = Caster.GetController();
            GameNPC turret = CreateTurret();
            turret.Position = (Caster.GroundTargetPosition == Position.Nowhere ? Caster.Position : Caster.GroundTargetPosition);
            turret.LoadedFromScript = true;
            turret.AddToWorld();
            if (m_spell.ClientEffect > 0)
            {
                foreach (GamePlayer player in turret.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendSpellEffectAnimation(Caster, turret, m_spell.ClientEffect, 0, false, 1);
                }

                foreach (GamePlayer player in Caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendSpellEffectAnimation(Caster, Caster, m_spell.ClientEffect, 0, false, 1);
                }
            }
            trueCaster.TempProperties.setProperty(LIVING_GROUNDEFFECT_PROPERTY, turret);
            Turret = turret;

            void CleanupTurret()
            {
                GameEventMgr.RemoveHandler(trueCaster, GameLivingEvent.RemoveFromWorld, OnRemoveFromWorld);
                trueCaster.TempProperties.removeProperty(LIVING_GROUNDEFFECT_PROPERTY);
                turret.RemoveFromWorld();
                turret.Delete();
                DurationTimer?.Stop();
                DurationTimer = null;
            }
            GameEventMgr.AddHandler(trueCaster, GameLivingEvent.RemoveFromWorld, OnRemoveFromWorld);
            void OnRemoveFromWorld(DOLEvent e, object sender, EventArgs arguments)
            {
                if (e != GameObjectEvent.RemoveFromWorld && sender != trueCaster)
                    return;
                
                CleanupTurret();
            }

            if (Spell.Duration > 0)
            {
                DurationTimer = new RegionTimer(turret, _ =>
                {
                    CleanupTurret();
                    return 0;
                }, Spell.Duration /* "Duration does not increase the duration of ground spells." https://darkageofcamelot.com/content/1122b-live-patch-notes */);
            }

            return true;
        }

        /// <inheritdoc />
        public override void CastSubSpells(GameLiving target)
        {
        }
    }
}
