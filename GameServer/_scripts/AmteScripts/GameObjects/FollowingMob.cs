using System;
using System.Collections.Generic;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.GS.Scripts
{
    public class FollowingMob : AmteMob, IFollowingMob
    {
        private DBBrainsParam m_param;
        public string FollowMobID = "";

        private double m_Angle;
        private int m_DistMob;

        private GameNPC m_mobFollow;
        public GameNPC MobFollow
        {
            get { return m_mobFollow; }
            set
            {
                m_mobFollow = value;
                if (value == null)
                    return;

                double DX = SpawnPosition.X - value.SpawnPosition.X;
                double DY = SpawnPosition.Y - value.SpawnPosition.Y;
                m_DistMob = (int)Math.Sqrt(DX * DX + DY * DY);

                if (m_DistMob > 0)
                {
                    m_Angle = Math.Asin(DX / m_DistMob);
                    if (DY > 0) m_Angle += (Math.PI / 2 - m_Angle) * 2;
                    m_Angle -= Math.PI / 2;

                    m_Angle = (m_Angle - value.SpawnHeading / GameMath.RADIAN_TO_HEADING) % (Math.PI * 2);
                }
                else m_Angle = 0;
            }
        }

        public override bool IsVisibleToPlayers => true;

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;
            FollowingBrain brain = new FollowingBrain();
            if (Brain is IOldAggressiveBrain)
            {
                brain.AggroLevel = ((IOldAggressiveBrain)Brain).AggroLevel;
                brain.AggroRange = ((IOldAggressiveBrain)Brain).AggroRange;
                //brain.AggroLink = ((IAggressiveBrain)Brain).AggroLink;
            }
            SetOwnBrain(brain);
            return true;
        }

        #region DB
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            m_param = GameServer.Database.SelectObject<DBBrainsParam>(b => b.MobID == obj.ObjectId);
            if (m_param != null && m_param.Param == "MobIDToFollow")
                FollowMobID = m_param.Value;
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();
            if (FollowMobID != "" && m_param == null)
            {
                m_param = new DBBrainsParam { MobID = InternalID, Param = "MobIDToFollow", Value = FollowMobID };
                GameServer.Database.AddObject(m_param);
            }
            else if (FollowMobID != "" && m_param.Value != FollowMobID)
            {
                m_param.Value = FollowMobID;
                GameServer.Database.SaveObject(m_param);
            }
        }

        public override void DeleteFromDatabase()
        {
            base.DeleteFromDatabase();
            if (m_param != null)
                GameServer.Database.DeleteObject(m_param);
        }
        #endregion

        public override void Reset()
        {
            StopFollowing();
            base.Reset();
        }

        public override void StartAttack(GameObject attackTarget)
        {
            if (MobFollow != null)
                StopFollowing();
            base.StartAttack(attackTarget);
        }

        protected override int FollowTimerCallback(RegionTimer callingTimer)
        {
            if (IsCasting)
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
            bool wasInRange = m_followTimer.Properties.getProperty(FOLLOW_TARGET_IN_RANGE, false);
            m_followTimer.Properties.removeProperty(FOLLOW_TARGET_IN_RANGE);

            GameObject followTarget = (GameObject)m_followTarget.Target;
            GameLiving followLiving = followTarget as GameLiving;
            if (followLiving != null && !followLiving.IsAlive)
            {
                StopFollowing();
                Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
                return 0;
            }

            //Stop following if we have no target
            if (followTarget == null || followTarget.ObjectState != eObjectState.Active || CurrentRegionID != followTarget.CurrentRegionID)
            {
                StopFollowing();
                Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
                return 0;
            }

            //Calculate the difference between our position and the players position
            var diff = followTarget.Position - Position;

            //SH: Removed Z checks when one of the two Z values is zero(on ground)
            float distance;
            if (followTarget.Position.Z == 0 || Position.Z == 0)
                distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
            else
                distance = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);

            //if distance is greater then the max follow distance, stop following and return home
            if (distance > m_followMaxDist)
            {
                StopFollowing();
                Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
                Reset();
                return 0;
            }

            //if the npc hasn't hit or been hit in a while, stop following and return home
            if (Brain is StandardMobBrain && Brain is IControlledBrain == false)
            {
                StandardMobBrain brain = Brain as StandardMobBrain;
                if (AttackState && brain != null && followLiving != null)
                {
                    // Amtenael MODIF : Aggro entre 30 et 45 secondes
                    //long seconds = Util.Random(30, 45);
                    long seconds = 20 + ((brain.GetAggroAmountForLiving(followLiving) / (MaxHealth + 1)) * 100);
                    long lastattacked = LastAttackTick;
                    long lasthit = LastAttackedByEnemyTick;
                    if (CurrentRegion.Time - lastattacked > seconds * 1000 && CurrentRegion.Time - lasthit > seconds * 1000)
                    {
                        StopFollowing();
                        Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
                        brain.ClearAggroList();
                        Reset();
                        return 0;
                    }
                }
            }

            //Are we in range yet?
            if ((followTarget == MobFollow && distance - 5 <= m_DistMob && m_DistMob <= distance + 5)
                || (followTarget != MobFollow && distance <= m_followMinDist))
            {
                //StopMoving();
                if (followTarget != MobFollow) TurnTo(followTarget);
                else TurnTo(followTarget.Heading);

                if (!wasInRange)
                {
                    m_followTimer.Properties.setProperty(FOLLOW_TARGET_IN_RANGE, true);
                    FollowTargetInRange();
                }
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            // follow on distance
            diff = (diff / distance) * m_followMinDist;
            var newPos = followTarget.Position - diff;

            if (followTarget == MobFollow)
            {
                var angle = (m_Angle + followTarget.Heading / GameMath.RADIAN_TO_HEADING) % (Math.PI * 2);

                newPos.X = followTarget.Position.X + (float)Math.Cos(angle) * m_DistMob;
                newPos.Y = followTarget.Position.Y + (float)Math.Sin(angle) * m_DistMob;

                var speed = MaxSpeed;
                if (GameMath.GetDistance2D(Position, newPos) < 500)
                    speed = (short)Math.Min(MaxSpeed, MobFollow.CurrentSpeed + 6);
                PathTo(newPos, speed);
            }
            else
                PathTo(newPos, MaxSpeed);
            return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        public override IList<string> DelveInfo()
        {
            var info = base.DelveInfo();
            info.Add("");
            info.Add("-- FollowingMob --");
            info.Add(" + Mob à suivre: " + MobFollow.Name + " (Realm: " + MobFollow.Realm + ", Guilde: " + MobFollow.GuildName + ")");
            return info;
        }

        public override void CustomCopy(GameObject source)
        {
            FollowMobID = (source as FollowingMob).FollowMobID;
            base.CustomCopy(source);
        }

        public void Follow(GameObject obj)
        {
            if (!(obj is GameNPC npc))
            {
                return;
            }

            MobFollow = npc;
            FollowMobID = npc.InternalID;
            Follow(npc, 10, 3000);
        }
    }
}

namespace DOL.AI.Brain
{
    public class FollowingBrain : AmteMobBrain
    {
        private string FollowMobID => (Body as FollowingMob)?.FollowMobID;

        public override bool Start()
        {
            if (!base.Start())
                return false;
            if (Body is not FollowingMob)
                return false;
            if (FollowMobID != "")
                SetMobByMobID();
            if ((Body as FollowingMob)?.MobFollow != null)
                Body.Follow(((FollowingMob)Body).MobFollow, 10, 3000);
            return true;
        }

        public override void Think()
        {
            if (!Body.IsCasting && CheckSpells(eCheckSpellType.Defensive))
                return;

            if (AggroLevel > 0)
            {
                CheckPlayerAggro();
                CheckNPCAggro();
            }

            if (!Body.AttackState && !Body.IsCasting && !Body.IsMoving
                && Body.Heading != Body.SpawnHeading && Body.Position == Body.SpawnPosition)
                Body.TurnTo(Body.SpawnHeading);

            if (!Body.InCombat)
                Body.TempProperties.removeProperty(GameLiving.LAST_ATTACK_DATA);

            if (Body.CurrentSpellHandler != null || Body.IsMoving || Body.AttackState ||
                Body.InCombat || Body.IsMovingOnPath || Body.CurrentFollowTarget != null)
                return;
            if ((Body as FollowingMob)?.MobFollow == null && FollowMobID != "")
                SetMobByMobID();

            if ((Body as FollowingMob)?.MobFollow != null)
                Body.Follow(((FollowingMob)Body).MobFollow, 10, 3000);
            else
                Body.Reset();
        }

        private void SetMobByMobID()
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
                if (npc.InternalID == FollowMobID)
                {
                    ((FollowingMob)Body).MobFollow = npc;
                    break;
                }
        }
    }
}
