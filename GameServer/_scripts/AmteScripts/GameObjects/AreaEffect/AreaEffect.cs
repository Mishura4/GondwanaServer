using System;
using DOL.AI.Brain;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.MobGroups;
using System.Collections.Generic;
using DOL.GS.Spells;
using System.Reflection.Metadata.Ecma335;
using DOL.GS.Geometry;
using System.Numerics;

namespace DOL.GS.Scripts
{
    public class AreaEffect : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public int SpellEffect;
        public int IntervalMin;
        public int IntervalMax;
        public int HealHarm;
        public int AddMana;
        public int AddEndurance;
        public int Radius;
        public int MissChance;
        public int SpellID;
        public string Message = "";
        public string Group_Mob_Id = "";
        public bool Group_Mob_Turn;
        public ushort AreaEffectFamily;
        public ushort OrderInFamily;
        public bool OneUse;

        private long LastApplyEffectTick;
        private int Interval;
        private DBAreaEffect AreaEffectDB;
        private bool enable;
        private bool disable;

        public Spell _spell;

        protected static SpellLine m_mobSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);

        private GameLiving m_implicitTarget;

        public bool Disable
        {
            get { return disable; }
            set
            {
                enable = !value;
                disable = value;
            }
        }

        public void CallAreaEffect()
        {
            enable = true;
            Brain.Think();
            if (_spell != null)
            {
                if (_spell.Target == "enemy")
                {
                    m_implicitTarget = GetController()?.TargetObject as GameLiving;
                }
                else if (_spell.Target == "realm")
                {
                    m_implicitTarget = _spell.Radius > 0 ? this : GetController()?.TargetObject as GameLiving;
                }
            }
        }

        public bool IsImmune(GameLiving living)
        {
            if (living == null)
            {
                return true;
            }

            if (living.GetLivingOwner() is GameLiving owner)
                living = owner;

            if (this.IsOwner(living))
                return true;
            
            var myOwner = GetLivingOwner();

            if (myOwner == null)
                return false;

            if (myOwner.Group?.IsInTheGroup(living) == true)
            {
                return true;
            }

            if (myOwner is GamePlayer ownerPlayer)
            {
                if (living is GamePlayer targetPlayer)
                {
                    if (targetPlayer.Guild == ownerPlayer.Guild)
                    {
                        return true;
                    }

                    if (ownerPlayer.BattleGroup != null && targetPlayer.BattleGroup == ownerPlayer.BattleGroup)
                    {
                        return true;
                    }
                }
                else if (living is GameNPC targetNpc)
                {
                    if (targetNpc.CurrentTerritory?.IsOwnedBy(ownerPlayer) == true)
                    {
                        return true;
                    }
                    
                    if (!string.IsNullOrEmpty(targetNpc.GuildName) && string.Equals(targetNpc.GuildName, ownerPlayer.Guild.Name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #region AddToWorld
        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            if (!(Brain is AreaEffectBrain))
                SetOwnBrain(new AreaEffectBrain());
            enable = !Disable;
            return true;
        }
        #endregion

        private void ApplyEffect(GameLiving living)
        {
            if (!living.IsAlive) return;
            if (Util.Chance(MissChance)) return;

            var health = HealHarm + (HealHarm * Util.Random(-5, 5) / 100);
            bool affected = false;

            if (health < 0)
            {
                if (!IsImmune(living) && GameServer.ServerRules.ShouldAOEHitTarget(null, Owner ?? this, living))
                {
                    AttackData ad = new AttackData
                    {
                        Attacker = Owner ?? this,
                        AttackResult = eAttackResult.HitUnstyled,
                        AttackType = AttackData.eAttackType.Spell,
                        CausesCombat = false,
                        Target = living,
                        Damage = -health
                    };
                    living.TakeDamage(ad);
                    affected = true;
                }
            }
            if (GameServer.ServerRules.IsAllowedToHelp(this, living, true))
            {
                if (health > 0)
                {
                    living.Health += health;
                    affected = true;
                }
                if (AddMana > 0)
                {
                    living.Mana += AddMana;
                    affected = true;
                }
                if (AddEndurance > 0)
                {
                    affected = true;
                    living.Endurance += AddEndurance;
                }
            }

            if (affected)
            {
                if (!string.IsNullOrEmpty(Message) && living is GamePlayer player)
                {
                    player.SendMessage(
                        string.Format(Message, Math.Abs(health), Math.Abs(AddMana), Math.Abs(AddEndurance)),
                        eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                foreach (GamePlayer plr in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    plr.Out.SendSpellEffectAnimation(this, living, (ushort)SpellEffect, 0, false, 1);
            }
        }

        #region ApplyEffect
        public void ApplyEffect()
        {
            if (!enable) return;
            if (Radius == 0 || SpellEffect == 0) return;
            if (LastApplyEffectTick > CurrentRegion.Time - Interval) return;

            GetPlayersInRadius((ushort)Radius).Cast<GamePlayer>().ForEach(ApplyEffect);
            GetNPCsInRadius((ushort)Radius).Cast<GameNPC>().ForEach(ApplyEffect);

            foreach (GamePlayer plr in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                plr.Out.SendSpellEffectAnimation(this, this, (ushort)SpellEffect, 0, false, 1);

            LastApplyEffectTick = CurrentRegion.Time;
            Interval = Util.Random(IntervalMin, Math.Max(IntervalMax, IntervalMin)) * 1000;
        }
        
        GameLiving SelectSpellEnemy()
        {
            if (m_implicitTarget != null && (Radius == 0 || m_implicitTarget.IsWithinRadius(this, Radius)) && GameServer.ServerRules.IsAllowedToAttack(this, m_implicitTarget, false))
            {
                return m_implicitTarget;
            }
            
            GameLiving target = null;
            float minDist = float.MaxValue;
            foreach (PlayerDistEntry entry in GetPlayersInRadius(true, (ushort)Radius, true, false))
            {
                if (entry.Distance < minDist && GameServer.ServerRules.IsAllowedToAttack(this, entry.Player, true))
                {
                    target = entry.Player;
                    minDist = entry.Distance;
                }
            }
            foreach (NPCDistEntry entry in GetNPCsInRadius(true, (ushort)Radius, true, false))
            {
                if (entry.Distance < minDist && GameServer.ServerRules.IsAllowedToAttack(this, entry.NPC, true))
                {
                    target = entry.NPC;
                    minDist = entry.Distance;
                }
            }
            return target;
        }
        
        GameLiving SelectSpellFriend()
        {
            if (m_implicitTarget != null && (Radius == 0 || m_implicitTarget.IsWithinRadius(this, Radius)) && GameServer.ServerRules.IsAllowedToHelp(this, m_implicitTarget, false))
            {
                return m_implicitTarget;
            }
            
            GameLiving target = null;
            float minDist = float.MaxValue;
            foreach (PlayerDistEntry entry in GetPlayersInRadius(true, (ushort)Radius, true, false))
            {
                if (entry.Distance < minDist && GameServer.ServerRules.IsAllowedToHelp(this, entry.Player, true))
                {
                    target = entry.Player;
                    minDist = entry.Distance;
                }
            }
            foreach (NPCDistEntry entry in GetNPCsInRadius(true, (ushort)Radius, true, false))
            {
                if (entry.Distance < minDist && GameServer.ServerRules.IsAllowedToHelp(this, entry.NPC, true))
                {
                    target = entry.NPC;
                    minDist = entry.Distance;
                }
            }
            return target;
        }

        GameLiving SelectSpellTarget()
        {
            switch (_spell.Target)
            {
                case "enemy":
                    return SelectSpellEnemy();
                
                case "realm":
                    return SelectSpellFriend();
                
                case "group":
                case "pet":
                    return GetController();
                
                case "area":
                case "self":
                case "ground":
                default:
                    return this;
            }
        }

        public void ApplySpell()
        {
            if (!enable) return;
            if (SpellID == 0 || _spell == null) return;
            if (LastApplyEffectTick > CurrentRegion.Time - Interval) return;

            TargetObject = SelectSpellTarget();
            GroundTargetPosition = (TargetObject ?? this).Position;
            if (TargetObject != this)
                TurnTo(TargetObject);
            this.CastSpell(_spell, m_mobSpellLine, true);
            LastApplyEffectTick = CurrentRegion.Time;
            Interval = Util.Random(IntervalMin, Math.Max(IntervalMax, IntervalMin)) * 1000;
        }

        public override bool CastSpell(Spell spell, SpellLine line)
        {
            ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(this, spell, line);
            if (spellhandler != null)
            {
                if (spell.CastTime > 0)
                {
                    m_runningSpellHandler = spellhandler;
                    spellhandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
                }
                return spellhandler.CastSpell();
            }
            return false;
        }

        #endregion

        public void CheckGroupMob()
        {
            if (!String.IsNullOrEmpty(Group_Mob_Id) && MobGroupManager.Instance.Groups.ContainsKey(Group_Mob_Id))
            {
                bool allDead = MobGroupManager.Instance.Groups[Group_Mob_Id].NPCs.All(m => !m.IsAlive);
                if (!allDead)
                    enable = Group_Mob_Turn;
                else
                    enable = !Group_Mob_Turn;
            }
        }

        public AreaEffect CheckFamily()
        {
            if (enable && AreaEffectFamily != 0)
            {
                List<DBAreaEffect> areaList = GameServer.Database.SelectObjects<DBAreaEffect>(DB.Column("AreaEffectFamily").IsEqualTo(AreaEffectFamily)).OrderBy((area) => area.OrderInFamily).ToList();
                // search the next 
                foreach (DBAreaEffect area in areaList)
                    if (area.OrderInFamily > OrderInFamily)
                    {
                        Mob mob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(area.MobID)).FirstOrDefault();
                        if (mob != null)
                        {
                            if (OneUse)
                                enable = false;
                            return WorldMgr.GetNPCsByName(mob.Name, (eRealm)mob.Realm).FirstOrDefault((npc) => npc.InternalID == mob.ObjectId) as AreaEffect;
                        }

                    }

            }
            if (OneUse)
                enable = false;
            return null;
        }

        #region Database
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            DBAreaEffect data = null;
            try
            {
                data = GameServer.Database.SelectObject<DBAreaEffect>(a => a.MobID == obj.ObjectId);
                if (data == null)
                    return;
            }
            catch
            {
                DBAreaEffect.Init();
            }
            if (data == null)
                data = GameServer.Database.SelectObject<DBAreaEffect>(a => a.MobID == obj.ObjectId);
            if (data == null)
                return;

            AreaEffectDB = data;
            SpellEffect = AreaEffectDB.Effect;
            IntervalMin = AreaEffectDB.IntervalMin;
            IntervalMax = AreaEffectDB.IntervalMax;
            HealHarm = AreaEffectDB.HealHarm;
            AddMana = AreaEffectDB.Mana;
            AddEndurance = AreaEffectDB.Endurance;
            Radius = AreaEffectDB.Radius;
            MissChance = AreaEffectDB.MissChance;
            Message = AreaEffectDB.Message;
            SpellID = AreaEffectDB.SpellID;
            Group_Mob_Id = AreaEffectDB.Group_Mob_Id;
            Group_Mob_Turn = AreaEffectDB.Group_Mob_Turn;
            AreaEffectFamily = AreaEffectDB.AreaEffectFamily;
            Disable = AreaEffectDB.Disable;
            OrderInFamily = AreaEffectDB.OrderInFamily;
            OneUse = AreaEffectDB.OnuUse;
            
            _spell = null;
            if (SpellID != 0)
            {
                DBSpell dbSpell = GameServer.Database.SelectObject<DBSpell>(DB.Column("SpellID").IsEqualTo(SpellID));

                if (dbSpell == null)
                {
                    log.Error($"DBSpell {SpellID} not found for AreaEffect {data.ObjectId}");
                }
                else
                {
                    _spell = new Spell(dbSpell, 0);
                }
            }
        }

        public static AreaEffect CreateTemporary(GameLiving owner, DBAreaEffect areaEffect, GameLiving target, Position position)
        {
            Mob mob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(areaEffect.MobID)).FirstOrDefault();
            if (mob == null)
                return null;

            // Ensure that the region is correctly set
            position = position.With(regionID: target.CurrentRegion.ID);

            Spell spell = null;
            if (areaEffect.SpellID != 0)
            {
                DBSpell dbSpell = GameServer.Database.SelectObject<DBSpell>(DB.Column("SpellID").IsEqualTo(areaEffect.SpellID));

                if (dbSpell == null)
                {
                    log.Error($"DBSpell {areaEffect.SpellID} not found for AreaEffect {areaEffect.ObjectId}");
                }
                else
                {
                    spell = new Spell(dbSpell, 0);
                }
            }

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
                OneUse = areaEffect.OnuUse,
                Owner = owner,
                _spell = spell
            };

            if (owner is GameNPC npc)
            {
                mob.FactionID = mob.FactionID;
            }

            AreaEffectBrain brain = new AreaEffectBrain();
            newAreaEffect.SetOwnBrain(brain);
            newAreaEffect.Flags = GameNPC.eFlags.DONTSHOWNAME | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.FLYING;
            return newAreaEffect;
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();
            bool New = false;
            if (AreaEffectDB == null)
            {
                AreaEffectDB = new DBAreaEffect();
                AreaEffectDB.MobID = InternalID;
                New = true;
            }

            AreaEffectDB.Effect = SpellEffect;
            AreaEffectDB.IntervalMin = IntervalMin;
            AreaEffectDB.IntervalMax = IntervalMax;
            AreaEffectDB.HealHarm = HealHarm;
            AreaEffectDB.Mana = AddMana;
            AreaEffectDB.Endurance = AddEndurance;
            AreaEffectDB.Radius = Radius;
            AreaEffectDB.MissChance = MissChance;
            AreaEffectDB.Message = Message;
            AreaEffectDB.SpellID = SpellID;
            AreaEffectDB.Group_Mob_Id = Group_Mob_Id;
            AreaEffectDB.Group_Mob_Turn = Group_Mob_Turn;
            AreaEffectDB.AreaEffectFamily = AreaEffectFamily;
            AreaEffectDB.Disable = Disable;
            AreaEffectDB.OrderInFamily = OrderInFamily;
            AreaEffectDB.OnuUse = OneUse;

            if (New)
                GameServer.Database.AddObject(AreaEffectDB);
            else
                GameServer.Database.SaveObject(AreaEffectDB);
        }
        #endregion

        public override List<string> CustomInfo()
        {
            List<string> text = base.CustomInfo();
            text.Add("");
            text.Add("-- AreaEffect --");
            text.Add(" + Effet: " + (HealHarm > 0 ? "heal " : "harm ") + HealHarm + " points de vie (+/- 10%).");
            text.Add(" + Rayon: " + Radius);
            text.Add(" + effect: " + SpellEffect);
            text.Add(" + Interval: " + IntervalMin + " à " + IntervalMax + " secondes");
            text.Add(" + Chance de miss: " + MissChance + "%");
            text.Add(" + Message: " + Message);
            text.Add(" + Spell: " + SpellID);
            text.Add(" + Family: " + AreaEffectFamily);
            text.Add(" + Order in family: " + OrderInFamily);
            text.Add(" + Groupmob: " + Group_Mob_Id);
            text.Add(" + Groupmob ON/OFF: " + Group_Mob_Turn);
            text.Add(" + Enable: " + !Disable);
            text.Add(" + OneUse: " + OneUse);
            return text;
        }

        public override void CustomCopy(GameObject source)
        {
            base.CustomCopy(source);
            AreaEffect areaSource = source as AreaEffect;
            if (areaSource != null)
            {
                DBAreaEffect lastarea = GameServer.Database.SelectObjects<DBAreaEffect>(DB.Column("AreaEffectFamily").IsEqualTo(areaSource.AreaEffectFamily)).OrderBy((area) => area.OrderInFamily).ToList().LastOrDefault();
                ushort order = 0;
                if (lastarea != null)
                    order = (ushort)(lastarea.OrderInFamily + 1);
                HealHarm = areaSource.HealHarm;
                Radius = areaSource.Radius;
                SpellEffect = areaSource.SpellEffect;
                IntervalMin = areaSource.IntervalMin;
                IntervalMax = areaSource.IntervalMax;
                MissChance = areaSource.MissChance;
                Message = areaSource.Message;
                SpellID = areaSource.SpellID;
                AreaEffectFamily = areaSource.AreaEffectFamily;
                OrderInFamily = order;
                Group_Mob_Id = areaSource.Group_Mob_Id;
                Group_Mob_Turn = areaSource.Group_Mob_Turn;
                Disable = areaSource.Disable;
                OneUse = areaSource.OneUse;
                _spell = areaSource._spell?.Copy();
            }
        }
    }
}