using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.Scripts;
using DOL.GS.Geometry;

namespace DOL.GS.Spells
{
    [SpellHandler("CallAreaEffect")]
    public class CallAreaEffectSpellHandler : SpellHandler
    {
        public CallAreaEffectSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!(Caster is GamePlayer))
            {
                return false;
            }

            if (selectedTarget == null)
            {
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "CallAreaEffect.CheckBeginCast.NoSelectedTarget"), eChatType.CT_SpellResisted);
                return false;
            }

            if (!Caster.IsWithinRadius(selectedTarget, Spell.Range))
            {
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "CallAreaEffect.CheckBeginCast.TargetTooFarAway"), eChatType.CT_SpellResisted);
                return false;
            }
            return base.CheckBeginCast(selectedTarget);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target != null && target.CurrentRegion != null)
            {
                int areaEffectFamily = Spell.LifeDrainReturn;
                if (areaEffectFamily > 0)
                {
                    List<DBAreaEffect> areaEffects = GameServer.Database.SelectObjects<DBAreaEffect>(
                        DB.Column("AreaEffectFamily").IsEqualTo(areaEffectFamily)).OrderBy(ae => ae.OrderInFamily).ToList();

                    if (areaEffects.Count > 0)
                    {
                        Position initialPosition = target.Position;
                        Mob initialMob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(areaEffects.First().MobID)).FirstOrDefault();
                        if (initialMob != null)
                        {
                            SpawnAreaEffect(areaEffects.First(), target, initialPosition, Spell.Duration);

                            // Use initialPosition for subsequent AreaEffects
                            foreach (var areaEffect in areaEffects.Skip(1))
                            {
                                Position calculatedPosition = CalculateRelativePosition(initialPosition, areaEffects.First().MobID, areaEffect.MobID);
                                SpawnAreaEffect(areaEffect, target, calculatedPosition, Spell.Duration);
                            }
                        }
                    }
                }
            }
        }

        private Position CalculateRelativePosition(Position basePosition, string firstMobID, string currentMobID)
        {
            Mob firstMob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(firstMobID)).FirstOrDefault();
            Mob currentMob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(currentMobID)).FirstOrDefault();

            if (firstMob == null || currentMob == null)
                return basePosition;

            int deltaX = currentMob.X - firstMob.X;
            int deltaY = currentMob.Y - firstMob.Y;
            int deltaZ = currentMob.Z - firstMob.Z;

            return basePosition.With(
                x: basePosition.X + deltaX,
                y: basePosition.Y + deltaY,
                z: basePosition.Z + deltaZ
            );
        }

        private void SpawnAreaEffect(DBAreaEffect areaEffect, GameLiving target, Position position, int duration)
        {
            Mob mob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(areaEffect.MobID)).FirstOrDefault();
            if (mob != null)
            {
                // Ensure that the region is correctly set
                position = position.With(regionID: target.CurrentRegion.ID);

                AreaEffect newAreaEffect = new AreaEffect
                {
                    Model = mob.Model,
                    Name = mob.Name,
                    Level = mob.Level,
                    Position = position,
                    Heading = target.Heading,
                    CurrentRegion = target.CurrentRegion,

                    // Copy properties from DBAreaEffect to AreaEffect
                    SpellEffect = areaEffect.Effect,
                    IntervalMin = areaEffect.IntervalMin,
                    IntervalMax = areaEffect.IntervalMax,
                    HealHarm = areaEffect.HealHarm,
                    MissChance = areaEffect.MissChance,
                    Radius = areaEffect.Radius,
                    Message = areaEffect.Message,
                    Mana = areaEffect.Mana,
                    Endurance = areaEffect.Endurance,
                    SpellID = areaEffect.SpellID,
                    OrderInFamily = areaEffect.OrderInFamily,
                    OneUse = areaEffect.OnuUse
                };

                if (Caster is GamePlayer player)
                {
                    newAreaEffect.OwnerID = player.ObjectId;
                }
                else if (Caster is GameNPC npc)
                {
                    mob.FactionID = mob.FactionID;
                }

                newAreaEffect.AddToWorld();
                newAreaEffect.TempProperties.setProperty("AreaEffectDuration", duration);

                AreaEffectBrain brain = new AreaEffectBrain();
                newAreaEffect.SetOwnBrain(brain);
                newAreaEffect.Flags = GameNPC.eFlags.DONTSHOWNAME | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.FLYING;

                // Set a timer to remove the AreaEffect after the duration
                GameEventMgr.AddHandler(newAreaEffect, GameLivingEvent.RemoveFromWorld, new DOLEventHandler(OnRemoveFromWorld));
                new RemoveAreaEffectTimer(newAreaEffect, duration).Start(duration);
            }
        }

        private void OnRemoveFromWorld(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is AreaEffect areaEffect)
            {
                GameEventMgr.RemoveHandler(areaEffect, GameLivingEvent.RemoveFromWorld, new DOLEventHandler(OnRemoveFromWorld));
                areaEffect.Delete();
            }
        }

        protected class RemoveAreaEffectTimer : RegionAction
        {
            public RemoveAreaEffectTimer(AreaEffect areaEffect, int delay) : base(areaEffect)
            {
            }

            protected override void OnTick()
            {
                AreaEffect areaEffect = (AreaEffect)m_actionSource;
                if (areaEffect != null && areaEffect.ObjectState == GameObject.eObjectState.Active)
                {
                    areaEffect.RemoveFromWorld();
                }
            }
        }
    }
}