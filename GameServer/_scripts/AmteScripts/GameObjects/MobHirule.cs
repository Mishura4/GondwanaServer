using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using System.Threading;
using log4net;
using DOL.AI.Brain;

namespace DOL.GS
{
    public class MobHirule : GameNPC
    {
        public ushort originalModel;
        public byte originalSize;
        private bool isDying;

        public MobHirule()
            : base()
        {
            LoadedFromScript = false;
            SetOwnBrain(new HiruleBrain());
        }
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Variables/Properties

        GameLiving m_hiruleTarget;

        public int stage = 0;

        //Glares Target
        public GameLiving HiruleTarget
        {
            get
            {
                return m_hiruleTarget;
            }
            set
            {
                m_hiruleTarget = value;
            }
        }

        ///public override int MaxHealth
        ///{
        ///	get
        ///	{
        ///		return base.MaxHealth * (Level/2);
        ///	}
        ///}

        ///public override double AttackDamage(InventoryItem weapon)
        ///{
        ///	return base.AttackDamage(weapon) * 2;
        ///}

        /// <summary>
        /// Gets or sets the base maxspeed of this living
        /// </summary>

        ///public override int MaxSpeedBase
        ///{
        ///	get
        ///	{
        ///return GamePlayer.PLAYER_BASE_SPEED + (Level * 2);
        ///	}
        ///	set
        ///	{
        ///		m_maxSpeedBase = value;
        ///	}
        ///}

        /// <summary>
        /// The Respawn Interval of this mob
        /// </summary>
        /// 
        ///public override int RespawnInterval
        ///{
        ///	get
        ///	{
        ///		int highmod = Level + 50;
        ///		int lowmod = Level / 3;
        ///		int result = Util.Random(lowmod, highmod);
        ///		return result * 60 * 1000;
        ///	}
        ///}
        /// <summary>
        /// Melee Attack Range.
        /// </summary>
        public override int AttackRange
        {
            get
            {
                //Normal mob attacks have 200 ...
                return 400;
            }
            set { }
        }

        #endregion

        #region Combat

        public int HiruleNukeandStun(RegionTimer timer)
        {
            //AOE Stun
            CastSpell(Stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            //AOE Nuke
            CastSpell(Nuke, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }

        // What happens when the HiruleNuke timer is filled
        public int HiruleNuke(RegionTimer timer)
        {
            //AOE Nuke
            CastSpell(Nuke, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }

        public void HiruleGlare(object timer)
        {
            ActionHiruleGlare(m_hiruleTarget);
        }


        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            var mob = obj as Mob;

            if (mob != null)
            {
                originalModel = mob.Model;
                originalSize = mob.Size;
            }
        }

        public override void Die(GameObject killer)
        {
            int count = 0;
            lock (this.XPGainers)
            {
                foreach (var de in this.XPGainers)
                {
                    GameObject obj = de.Key;
                    if (obj is GamePlayer)
                    {
                        GamePlayer player = obj as GamePlayer;

                        if (player.Health >= 1)
                        {
                            player.Health = 1;
                            ///player.Out.SendSpellEffectAnimation(m_caster, target, 							///m_spell.ClientEffect, boltDuration, noSound, success);
                            player.Out.SendSpellEffectAnimation(this, player, 87, 0, false, 5);
                            player.Out.SendSpellEffectAnimation(this, this, 6141, 0, false, 5);
                            ///player.KillsDragon++;
                            count++;
                        }
                    }
                }
            }
            isDying = true;
            base.Die(killer);
            // dragon died message
            HiruleBroadcast(Name + " crie, \"Vous remportez cette bataille, mais la Guerre ne fait que commencer !\"");
            //Event dragons dont respawn
            if (RespawnInterval == -1)
            {
                Delete();
                DeleteFromDatabase();
            }

            isDying = false;
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            if (ObjectState != eObjectState.Active) return;
            GameLiving t = (GameLiving)source;
            int range = this.GetDistanceTo(t);
            if (range >= 1000) //�vite la technique du serpent
            {
                m_hiruleTarget = t;
                PickAction();
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            var healEvent = args as EnemyHealedEventArgs;
            if (healEvent != null)
            {
                OnEnemyHealed(healEvent.Enemy, healEvent.HealSource, healEvent.ChangeType, healEvent.HealAmount);
            }

            base.Notify(e, sender, args);
        }

        private void OnEnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType, int healAmount)
        {
            if (ObjectState != eObjectState.Active) return;
            if (healSource is GameLiving)
            {
                GameLiving t = (GameLiving)healSource;
                int range = this.GetDistanceTo(t);
                if (range >= ((StandardMobBrain)Brain).AggroRange)
                {
                    m_hiruleTarget = t;
                    PickAction();
                }
            }
        }

        public override bool AddToWorld()
        {
            bool added = false;
            isDying = false;
            added = base.AddToWorld();

            //If added by command create or copy save original values
            if (originalModel == 0 && originalSize == 0)
            {
                originalModel = this.Model;
                originalSize = this.Size;
            }
            //Otherwise restore original values from database load
            else
            {
                this.Model = originalModel;
                this.Size = originalSize;
            }

            return added;
        }

        public override void SaveIntoDatabase()
        {
            originalSize = Size;
            originalModel = Model;
            base.SaveIntoDatabase();
        }

        public override void StopAttack()
        {
            base.StopAttack();

            if (!isDying)
            {
                this.Model = originalModel;
                this.Size = originalSize;
            }
        }

        void PickAction()
        {
            if (Util.Random(1) < 1)
            {
                //Glare
                Timer timer = new Timer(new TimerCallback(HiruleGlare), null, 30, 0);
            }
            else
            {
                //Throw
                ActionHiruleThrow(m_hiruleTarget);
            }
        }

        public void HiruleBroadcast(string message)
        {
            foreach (GamePlayer players in this.GetPlayersInRadius((ushort)(WorldMgr.VISIBILITY_DISTANCE + 1500)))
                players.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
        }

        void BroadcastMain(string message)
        {
            foreach (GameClient players in WorldMgr.GetAllPlayingClients())
                players.Out.SendMessage("[main]: " + message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
        }

        void ActionHiruleGlare(GameLiving target)
        {
            // Let them know that they're about to die.
            HiruleBroadcast(Name + " incante un sort sur " + target.Name);
            TargetObject = m_hiruleTarget;
            //AOE Nuke			
            CastSpell(Glare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }

        void ActionHiruleThrow(GameLiving target)
        {
            if (!(target is GamePlayer)) return;
            if ((int)Realm == 5) return; // I don't want event dragons causing XP deaths
            HiruleBroadcast(Name + " jette " + m_hiruleTarget.Name + " dans les airs !");
            m_hiruleTarget.MoveTo(m_hiruleTarget.CurrentRegionID,
                m_hiruleTarget.X,
                m_hiruleTarget.Y,
                m_hiruleTarget.Z + 750,
                m_hiruleTarget.Heading);
        }

        #endregion

        #region Spells

        protected static Spell m_flash;
        public static Spell Flash
        {
            get
            {
                if (m_flash == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.CastTime = 0;
                    spell.ClientEffect = 6141;
                    spell.Description = "Flash";
                    spell.Name = "Hirule FLash";
                    spell.Range = 2500;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Heat;
                    spell.SpellID = 6003;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    m_flash = new Spell(spell, 70);
                    SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells).Add(m_flash);
                }
                return m_flash;
            }
        }

        protected static Spell m_glare;
        public static Spell Glare
        {
            get
            {
                if (m_glare == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.CastTime = 0;
                    spell.ClientEffect = 360;
                    spell.Description = "Glare";
                    spell.Name = "Hirule Glare";
                    spell.Range = 2500;
                    spell.Damage = 600;
                    spell.DamageType = (int)eDamageType.Heat;
                    spell.SpellID = 6001;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    m_glare = new Spell(spell, 70);
                    SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells).Add(m_glare);
                }
                return m_glare;
            }
        }

        protected static Spell m_nuke;
        public static Spell Nuke
        {
            get
            {
                if (m_nuke == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 2308;
                    spell.Description = "Nuke";
                    spell.Name = "Dragon Nuke";
                    spell.Range = 800;
                    spell.Radius = 800;
                    spell.Damage = 800;
                    spell.DamageType = (int)eDamageType.Heat;
                    spell.SpellID = 6000;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    m_nuke = new Spell(spell, 70);
                    SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells).Add(m_nuke);
                }
                return m_nuke;
            }
        }

        protected static Spell m_stun;
        public static Spell Stun
        {
            get
            {
                if (m_stun == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 4123;
                    spell.Duration = 10;
                    spell.Description = "Stun";
                    spell.Name = "Effroi d'Hirule";
                    spell.Range = 700;
                    spell.Radius = 700;
                    spell.Damage = 500;
                    spell.DamageType = 13;
                    spell.SpellID = 6002;
                    spell.Target = "Enemy";
                    spell.Type = "Stun";
                    spell.Message1 = "You cannot move!";
                    spell.Message2 = "{0} cannot seem to move!";
                    m_stun = new Spell(spell, 70);
                    SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells).Add(m_stun);
                }
                return m_stun;
            }
        }
        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class HiruleBrain : StandardMobBrain
    {
        public HiruleBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 650;
            ThinkInterval = 2000;
        }
        public override void Think()
        {
            MobHirule hirule = Body as MobHirule;
            //If at full HP we reset stages
            if (hirule.HealthPercent == 100 && hirule.stage != 0)
                hirule.stage = 0;


            //Stage 1 < 75%
            else if (hirule.HealthPercent < 75 && hirule.HealthPercent > 50 && hirule.stage == 0)
            {

                hirule.HiruleBroadcast(hirule.Name + " crie, \"La seule chose que vous atteindrez, c'est l'Enfer !\"");
                hirule.Model = 1189; //sale gorgone
                hirule.Size = 80;
                //ajouter un cast pour faire joli

                foreach (GamePlayer pl in hirule.GetPlayersInRadius(5000))
                {
                    pl.Out.SendSpellEffectAnimation(hirule, hirule, 14367, 0, false, 5);

                }

                hirule.HiruleTarget = CalculateNextAttackTarget();
                if (hirule.HiruleTarget != null)
                {
                    new RegionTimer(hirule, new RegionTimerCallback(hirule.HiruleNuke), 5000);
                    hirule.stage = 1;


                }
            }


            //Stage 2 < 50%
            else if (hirule.HealthPercent < 50 && hirule.HealthPercent > 25 && hirule.stage == 1)
            {

                hirule.Model = 919; ///Combattant Ogre
				hirule.Size = 200;
                //ajouter un cast pour faire joli

                foreach (GamePlayer pl in hirule.GetPlayersInRadius(5000))
                {
                    pl.Out.SendSpellEffectAnimation(hirule, hirule, 14367, 0, false, 5);

                }

                hirule.HiruleTarget = CalculateNextAttackTarget();
                if (hirule.HiruleTarget != null)
                {
                    new RegionTimer(hirule, new RegionTimerCallback(hirule.HiruleNuke), 5000);
                    hirule.stage = 2;
                }
            }



            //Stage 3 < 25%
            else if (hirule.HealthPercent < 25 && hirule.HealthPercent > 10 && hirule.stage == 2)
            {

                hirule.HiruleBroadcast(hirule.Name + " crie, \"La mort ne vous effraie donc pas ?\"");

                hirule.Model = 2175;///Azazael vraiment immense
				hirule.Size = 100;
                //ajouter un cast pour faire joli

                foreach (GamePlayer pl in hirule.GetPlayersInRadius(5000))
                {
                    pl.Out.SendSpellEffectAnimation(hirule, hirule, 14367, 0, false, 5);

                }


                hirule.HiruleTarget = CalculateNextAttackTarget();
                if (hirule.HiruleTarget != null)
                {
                    new RegionTimer(hirule, new RegionTimerCallback(hirule.HiruleNukeandStun), 5000);
                    hirule.stage = 3;
                }
            }
            //Stage 4 < 10%
            else if (hirule.HealthPercent < 10 && hirule.stage == 3)
            {

                hirule.Model = 957; ///vieille celte
				hirule.Size = 50;
                //ajouter un cast pour faire joli

                foreach (GamePlayer pl in hirule.GetPlayersInRadius(5000))
                {
                    pl.Out.SendSpellEffectAnimation(hirule, hirule, 14367, 0, false, 5);

                }

                hirule.HiruleTarget = CalculateNextAttackTarget();
                if (hirule.HiruleTarget != null)
                {
                    new RegionTimer(hirule, new RegionTimerCallback(hirule.HiruleNukeandStun), 5000);
                    hirule.stage = 4;
                    hirule.HiruleBroadcast(hirule.Name + " crie, \"Vous pensez vous en sortir aussi facilement ? AHAHA !\"");

                }
            }
            base.Think();
        }
    }
}