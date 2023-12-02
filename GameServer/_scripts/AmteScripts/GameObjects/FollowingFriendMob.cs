using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Timers;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOLDatabase.Tables;
using static DOL.GS.GameObject;

namespace DOL.GS.Scripts
{
    public class FollowingFriendMob : AmteMob, ITextNPC, IFollowingMob
    {

        public string MobID { get; set; }
        public string MobName { get; set; }
        public TextNPCPolicy TextNPCIdle { get; set; }
        public TextNPCPolicy TextNPCFollowing { get; set; }
        public Dictionary<string, string> ResponsesFollow { get; set; }
        public Dictionary<string, string> ResponsesUnfollow { get; set; }
        public ushort FollowingFromRadius { get; set; }
        public int AggroMultiplier { get; set; }
        public string LinkedGroupMob { get; set; }
        public string AreaToEnter { get; set; }
        public int TimerBeforeReset { get; set; }
        public bool WaitingInArea { get; set; }
        public string ungroupText;

        private double m_Angle;
        private int m_DistMob;
        public Timer ResetTimer { get; set; }

        private GamePlayer m_playerFollow;
        public GamePlayer PlayerFollow
        {
            get { return m_playerFollow; }
            set
            {
                m_playerFollow = value;
                if (value == null)
                    return;

                double DX = SpawnPoint.X - value.Position.X;
                double DY = SpawnPoint.Y - value.Position.Y;
                m_DistMob = (int)Math.Sqrt(DX * DX + DY * DY);

                if (m_DistMob > 0)
                {
                    m_Angle = Math.Asin(DX / m_DistMob);
                    if (DY > 0) m_Angle += (Math.PI / 2 - m_Angle) * 2;
                    m_Angle -= Math.PI / 2;

                    m_Angle = (m_Angle - value.Heading / GameMath.RADIAN_TO_HEADING) % (Math.PI * 2);
                }
                else m_Angle = 0;
            }
        }

        public TextNPCPolicy GetTextNPCPolicy(GameLiving target = null)
        {
            return PlayerFollow == target ? TextNPCFollowing : TextNPCIdle;
        }

        public TextNPCPolicy GetOrCreateTextNPCPolicy(GameLiving target = null)
        {
            if (PlayerFollow == target)
            {
                return TextNPCFollowing ??= new TextNPCPolicy(this);
            }
            else
            {
                return TextNPCIdle ??= new TextNPCPolicy(this);
            }
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player) && (IsPeaceful || WaitingInArea ||
            (((StandardMobBrain)Brain).AggroLevel == 0 && ((StandardMobBrain)Brain).AggroRange == 0))
            && CurrentRegion.GetAreasOfSpot(Position).OfType<AbstractArea>().FirstOrDefault(a => a.DbArea != null && a.DbArea.ObjectId == AreaToEnter) != null)
                return false;
            if (PlayerFollow != null && PlayerFollow == player)
            {
                if (TextNPCFollowing == null)
                    return false;

                return TextNPCFollowing.Interact(player);
            }
            if (TextNPCIdle == null)
                return false;
            return TextNPCIdle.Interact(player);
        }
        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;
            if (source is not GamePlayer player)
                return false;

            TurnTo(player);
            if (PlayerFollow == player)
            {
                if (TextNPCFollowing != null && TextNPCFollowing.WhisperReceive(player, str) == false)
                    return false;
                string unfollowEntry = null;
                if (str == "ungroup" || (ResponsesUnfollow != null && ResponsesUnfollow.TryGetValue(str.ToLower(), out unfollowEntry)))
                {
                    if (unfollowEntry != null)
                    {
                        string text = string.Format(unfollowEntry, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                        player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    }
                    Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(player));
                    Reset();
                }
            }
            else
            {
                if (TextNPCIdle != null && TextNPCIdle.WhisperReceive(player, str) == false)
                    return false;
                if (ResponsesFollow != null && ResponsesFollow.TryGetValue(str.ToLower(), out var followEntry))
                {
                    if (followEntry != null)
                    {
                        string text = string.Format(followEntry, player.Name, player.LastName, player.GuildName, player.CharacterClass.Name, player.RaceName);
                        player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    }
                    Follow(player);
                }
            }
            return true;
        }
        public override bool AddToWorld()
        {
            WaitingInArea = false;
            if (!base.AddToWorld())
                return false;
            FollowingFriendMobBrain brain = new FollowingFriendMobBrain();
            if (Brain is IOldAggressiveBrain)
            {
                brain.AggroLevel = ((IOldAggressiveBrain)Brain).AggroLevel;
                brain.AggroRange = ((IOldAggressiveBrain)Brain).AggroRange;
            }
            SetOwnBrain(brain);
            return true;
        }

        #region DB
        public override void LoadFromDatabase(DataObject obj)
        {
            followingfriendmob data = null;
            data = GameServer.Database.SelectObject<followingfriendmob>(t => t.MobID == obj.ObjectId);
            if (data != null)
            {
                MobID = data.MobID;
                var textData = GameServer.Database.FindObjectByKey<DBTextNPC>(data.TextIdle);
                if (textData != null)
                {
                    TextNPCIdle = new TextNPCPolicy(this);
                    TextNPCIdle.LoadFromDatabase(textData);
                }
                textData = GameServer.Database.FindObjectByKey<DBTextNPC>(data.TextFollowing);
                if (textData != null)
                {
                    TextNPCFollowing = new TextNPCPolicy(this);
                    TextNPCFollowing.LoadFromDatabase(textData);
                }
                ResponsesFollow = new Dictionary<string, string>();
                if (data.ReponseFollow != null)
                {
                    foreach (string item in data.ReponseFollow.Split(';'))
                    {
                        string[] items = item.Split('|');
                        if (items.Length != 2)
                            continue;
                        ResponsesFollow.Add(items[0].ToLower(), items[1]);
                    }
                }
                ResponsesUnfollow = new Dictionary<string, string>();
                if (data.ReponseUnfollow != null)
                {
                    foreach (string item in data.ReponseUnfollow.Split(';'))
                    {
                        string[] items = item.Split('|');
                        if (items.Length != 2)
                            continue;
                        ResponsesUnfollow.Add(items[0].ToLower(), items[1]);
                    }
                }
                FollowingFromRadius = data.FollowingFromRadius;
                AggroMultiplier = data.AggroMultiplier;
                LinkedGroupMob = data.LinkedGroupMob;
                AreaToEnter = data.AreaToEnter;
                TimerBeforeReset = data.TimerBeforeReset;
                ResetTimer = new Timer();
                if (TimerBeforeReset == 0)
                    ResetTimer.Interval = 1;
                else
                    ResetTimer.Interval = TimerBeforeReset * 1000;
                ResetTimer.Elapsed += ResetTimer_Elapsed;
            }
            else
            {
                data = new followingfriendmob();
                data.MobID = obj.ObjectId;
                MobID = data.MobID;
                GameServer.Database.AddObject(data);
            }
            base.LoadFromDatabase(obj);
        }
        public override void DeleteFromDatabase()
        {
            followingfriendmob data = null;
            data = GameServer.Database.SelectObject<followingfriendmob>(t => t.MobID == MobID);
            if (data != null)
            {
                var textData = GameServer.Database.FindObjectByKey<DBTextNPC>(data.TextIdle);
                if (textData != null)
                {
                    GameServer.Database.DeleteObject(textData);
                }
                textData = GameServer.Database.FindObjectByKey<DBTextNPC>(data.TextFollowing);
                if (textData != null)
                {
                    GameServer.Database.DeleteObject(textData);
                }
                GameServer.Database.DeleteObject(data);
            }
            base.DeleteFromDatabase();
        }

        private void ResetTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ResetFriendMobs();
        }

        public void ResetFriendMobs()
        {
            if (CurrentGroupMob != null)
            {
                var mobs = WorldMgr.GetNPCsFromRegion(CurrentRegion.ID).Where(c => c is FollowingFriendMob &&
                    c.CurrentGroupMob != null && c.CurrentGroupMob == CurrentGroupMob).ToList();
                foreach (FollowingFriendMob mob in mobs)
                {
                    if (mob.PlayerFollow != null)
                        mob.ResetFriendMob();
                }
            }

            ResetFriendMob();
        }

        public void ResetFriendMob()
        {
            if (WaitingInArea == true && PlayerFollow != null)
            {
                ResetTimer.Stop();
                PlayerFollow.Notify(GameLivingEvent.BringAFriend, PlayerFollow, new BringAFriendArgs(this, false));
            }
            WaitingInArea = false;
            PlayerFollow = null;
            RemoveFromWorld();
            Health = MaxHealth;
            LoadFromDatabase(GameServer.Database.FindObjectByKey<Mob>(InternalID));
            AddToWorld();
        }

        public void Reset()
        {
            ResetFriendMobs();
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);
            ResetFriendMobs();
        }
        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();
            followingfriendmob data = null;
            MobID = InternalID;
            if (MobID == null)
                return;
            MobName = Name;
            data = GameServer.Database.SelectObject<followingfriendmob>(t => t.MobID == MobID);
            bool isNew = false;
            if (data == null)
            {
                data = new followingfriendmob();
                isNew = true;
            }
            data.MobID = MobID;
            data.ReponseFollow = ResponsesFollow is { Count: > 0 } ? string.Join(";", ResponsesFollow.Select(t => t.Key + "|" + t.Value).ToArray()) : null;
            data.ReponseUnfollow = ResponsesUnfollow is { Count: > 0 } ? string.Join(";", ResponsesUnfollow.Select(t => t.Key + "|" + t.Value).ToArray()) : null;
            data.FollowingFromRadius = FollowingFromRadius;
            data.AggroMultiplier = AggroMultiplier;
            data.LinkedGroupMob = LinkedGroupMob;
            data.AreaToEnter = AreaToEnter;
            data.TimerBeforeReset = TimerBeforeReset;
            if (TextNPCIdle != null)
            {
                TextNPCIdle.SaveIntoDatabase();
                data.TextIdle = TextNPCIdle.TextDB.ObjectId;
            }
            if (TextNPCFollowing != null)
            {
                TextNPCFollowing.SaveIntoDatabase();
                data.TextFollowing = TextNPCFollowing.TextDB.ObjectId;
            }
            if (isNew)
                GameServer.Database.AddObject(data);
            else
                GameServer.Database.SaveObject(data);
        }
        #endregion

        public override void StartAttack(GameObject attackTarget)
        {
            if (PlayerFollow != null)
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
                WalkToSpawn();
                return 0;
            }

            //Are we in range yet?
            if ((followTarget == PlayerFollow && distance - 5 <= m_DistMob && m_DistMob <= distance + 5)
                || (followTarget != PlayerFollow && distance <= m_followMinDist))
            {
                //StopMoving();
                if (followTarget != PlayerFollow) TurnTo(followTarget);
                else TurnTo(followTarget.Heading);

                if (!wasInRange)
                {
                    m_followTimer.Properties.setProperty(FOLLOW_TARGET_IN_RANGE, true);
                    FollowTargetInRange();
                }
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            //check area reached
            var area = followTarget.CurrentRegion.GetAreasOfSpot(Position).OfType<AbstractArea>().FirstOrDefault(a => a.DbArea != null && a.DbArea.ObjectId == AreaToEnter);
            if (area != null && !WaitingInArea)
            {
                StopFollowing();
                var targetPos = new Vector3(area.DbArea.X, area.DbArea.Y, area.DbArea.Z);
                var angle = Math.Atan2(targetPos.Y - Position.Y, targetPos.X - Position.X);
                targetPos.X = area.DbArea.X + (float)Math.Cos(angle) * Util.Random(200, 300);
                targetPos.Y = area.DbArea.Y + (float)Math.Sin(angle) * Util.Random(200, 300);
                WaitingInArea = true;
                followTarget.Notify(GameLivingEvent.BringAFriend, followTarget, new BringAFriendArgs(this, true));
                PathTo(targetPos, 130);
                return 0;
            }
            else if (WaitingInArea)
                return 0;

            // follow on distance
            diff = (diff / distance) * m_followMinDist;
            var newPos = followTarget.Position - diff;

            if (followTarget == PlayerFollow)
            {
                var speed = MaxSpeed;
                if (GameMath.GetDistance2D(Position, followTarget.Position) < 200)
                    speed = 0;
                PathTo(newPos, speed);
            }
            else
                PathTo(newPos, MaxSpeed);
            //foreach in visible distance
            foreach (GameNPC npc in GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (npc.Brain is StandardMobBrain brain)
                    brain.AggroMultiplier = 1 + AggroMultiplier;
            }

            return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        public override IList<string> DelveInfo()
        {
            var info = base.DelveInfo();
            info.Add("");
            info.Add("-- FollowingFriendMob --");
            info.Add(" + Mob à suivre: " + PlayerFollow.Name + " (Realm: " + PlayerFollow.Realm + ", Guilde: " + PlayerFollow.Guild.Name + ")");
            return info;
        }

        public override void CustomCopy(GameObject source)
        {
            var followingSource = source as FollowingFriendMob;
            MobName = followingSource.MobName;
            TextNPCIdle = followingSource.TextNPCIdle;
            TextNPCFollowing = followingSource.TextNPCFollowing;
            ResponsesFollow = followingSource.ResponsesFollow;
            ResponsesUnfollow = followingSource.ResponsesUnfollow;
            FollowingFromRadius = followingSource.FollowingFromRadius;
            AggroMultiplier = followingSource.AggroMultiplier;
            LinkedGroupMob = followingSource.LinkedGroupMob;
            AreaToEnter = followingSource.AreaToEnter;
            TimerBeforeReset = followingSource.TimerBeforeReset;
            ungroupText = followingSource.ungroupText;
            base.CustomCopy(source);
        }

        public void SayRandomPhrase()
        {
            throw new NotImplementedException();
        }

        public void Follow(GameObject obj)
        {
            if (!(Brain is FollowingFriendMobBrain followBrain) || !(obj is GamePlayer player))
            {
                return;
            }
            followBrain.Follow(player);
        }

        public override void StopFollowing()
        {
            PlayerFollow = null;
            base.StopFollowing();
        }
    }
}

namespace DOL.AI.Brain
{
    public class FollowingFriendMobBrain : AmteMobBrain
    {
        private string FollowMobID => (Body as FollowingMob)?.FollowMobID;

        public override bool Start()
        {
            if (!base.Start())
                return false;
            if (Body is not FollowingFriendMob)
                return false;
            ScanForPlayers();
            if ((Body as FollowingFriendMob)?.PlayerFollow != null)
                Body.Follow(((FollowingFriendMob)Body).PlayerFollow, 10, 3000);
            return true;
        }

        public override void Think()
        {
            //if player quits the game
            if (((FollowingFriendMob)Body).PlayerFollow != null && ((FollowingFriendMob)Body).PlayerFollow.ObjectState == eObjectState.Deleted)
            {
                ((FollowingFriendMob)Body).ResetFriendMobs();
                return;
            }
            if (!Body.IsCasting && CheckSpells(eCheckSpellType.Defensive))
                return;

            if (AggroLevel > 0)
            {
                CheckPlayerAggro();
                CheckNPCAggro();
            }

            if (!Body.AttackState && !Body.IsCasting && !Body.IsMoving
                && Body.Heading != Body.SpawnHeading && Body.Position == Body.SpawnPoint)
                Body.TurnTo(Body.SpawnHeading);

            if (!Body.InCombat)
                Body.TempProperties.removeProperty(GameLiving.LAST_ATTACK_DATA);

            if (Body.CurrentSpellHandler != null || Body.IsMoving || Body.AttackState ||
                Body.InCombat || Body.IsMovingOnPath || Body.CurrentFollowTarget != null)
                return;
            if ((Body as FollowingFriendMob)?.PlayerFollow == null)
                ScanForPlayers();
        }

        private void ScanForPlayers()
        {
            var followingMob = (FollowingFriendMob)Body;
            if (followingMob.FollowingFromRadius > 0 && !followingMob.WaitingInArea)
            {
                var players = Body.GetPlayersInRadius(followingMob.FollowingFromRadius);
                foreach (var player in players)
                {
                    Follow((GamePlayer)player);
                    break;
                }
            }
        }

        public void Follow(GamePlayer player)
        {
            ((FollowingFriendMob)Body).PlayerFollow = player;
            player.Notify(GameLivingEvent.BringAFriend, player, new BringAFriendArgs((FollowingFriendMob)Body, true, true));
            Body.Follow(player, 10, 3000);
        }

        public void StopFollowing()
        {
            Body.Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(Body.CurrentFollowTarget));
            Body.StopFollowing();
        }
    }
}
