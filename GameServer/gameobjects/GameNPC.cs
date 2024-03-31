/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.GS.Utils;
using DOL.Language;
using DOL.GS.ServerProperties;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using DOL.GameEvents;
using DOL.MobGroups;
using DOL.Territory;
using static DOL.GS.ScriptMgr;
using DOL.GS.Finance;
using DOLDatabase.Tables;
using DOL.GS.Scripts;
using System.Timers;
using System.Text.RegularExpressions;

namespace DOL.GS
{
    /// <summary>
    /// This class is the baseclass for all Non Player Characters like
    /// Monsters, Merchants, Guards, Steeds ...
    /// </summary>
    public class GameNPC : GameLiving, ITranslatableObject
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constant for determining if already at a point
        /// </summary>
        /// <remarks>
        /// This helps to reduce the turning of an npc while fighting or returning to a spawn
        /// Tested - min distance for mob sticking within combat range to player is 25
        /// </remarks>
        public const int CONST_WALKTOTOLERANCE = 25;

        public ushort DamageTypeCounter { get; set; }
        public eDamageType LastDamageType { get; set; }
        public ushort DamageTypeLimit { get; set; }
        private RegionTimer ambientTextTimer;
        private bool hasImunity = false;
        public eDamageType ImunityDomage = eDamageType.GM;
        private Dictionary<MobXAmbientBehaviour, short> ambientXNbUse = new Dictionary<MobXAmbientBehaviour, short>();
        private eFlags tempoarallyFlags = 0;
        private ABrain temporallyBrain = null;
        private INpcTemplate temporallyTemplate = null;

        #region Debug
        private bool m_debugMode = false;
        public bool DebugMode
        {
            get { return m_debugMode; }
            set
            {
                m_debugMode = value;
                if (PathCalculator != null)
                    PathCalculator.VisualizePath = value;
            }
        }
        public virtual void DebugSend(string str, params object[] args)
        {
            if (!DebugMode)
                return;
            str = string.Format(str, args);
            Say("[DEBUG] " + str);
            log.Debug($"[pathing {Name}] {str}");
        }
        #endregion

        #region Formations/Spacing

        //Space/Offsets used in formations
        // Normal = 1
        // Big = 2
        // Huge = 3
        private byte m_formationSpacing = 1;

        /// <summary>
        /// The Minions's x-offset from it's commander
        /// </summary>
        public byte FormationSpacing
        {
            get { return m_formationSpacing; }
            set
            {
                //BD range values vary from 1 to 3.  It is more appropriate to just ignore the
                //incorrect values than throw an error since this isn't a very important area.
                if (value > 0 && value < 4)
                    m_formationSpacing = value;
            }
        }

        /// <summary>
        /// Used for that formation type if a GameNPC has a formation
        /// </summary>
        public enum eFormationType
        {
            // M = owner
            // x = following npcs
            //Line formation
            // M x x x
            Line,
            //Triangle formation
            //		x
            // M x
            //		x
            Triangle,
            //Protect formation
            //		 x
            // x  M
            //		 x
            Protect,
        }

        private eFormationType m_formation = eFormationType.Line;
        /// <summary>
        /// How the minions line up with the commander
        /// </summary>
        public eFormationType Formation
        {
            get { return m_formation; }
            set { m_formation = value; }
        }

        #endregion

        #region Sizes/Properties
        /// <summary>
        /// Holds the size of the NPC
        /// </summary>
        protected byte m_size;
        /// <summary>
        /// Gets or sets the size of the npc
        /// </summary>
        public byte Size
        {
            get { return m_size; }
            set
            {
                m_size = value;
                if (ObjectState == eObjectState.Active)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendModelAndSizeChange(this, Model, value);
                    //					BroadcastUpdate();
                }
            }
        }

        public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier
        {
            get { return LanguageDataObject.eTranslationIdentifier.eNPC; }
        }

        /// <summary>
        /// Holds the translation id.
        /// </summary>
        protected string m_translationId = "";

        /// <summary>
        /// Gets or sets the translation id.
        /// </summary>
        public string TranslationId
        {
            get { return m_translationId; }
            set { m_translationId = (value == null ? "" : value); }
        }

        public string EventID
        {
            get;
            set;
        }

        public int ExperienceEventFactor
        {
            get;
            set;
        } = 1;

        /// <summary>
        /// Original Race from Database
        /// </summary>
        public int RaceDb
        {
            get;
            set;
        }

        /// <summary>
        /// Original Model from Database
        /// </summary>
        public ushort ModelDb
        {
            get;
            set;
        }

        /// <summary>
        /// Original VisibleWeapons from Database
        /// </summary>
        public byte VisibleWeaponsDb
        {
            get;
            set;
        }

        /// <summary>
        /// Original Flags from Database
        /// </summary>
        public uint FlagsDb
        {
            get;
            set;
        }

        /// <summary>
        /// If this mob is a Member of GroupMob
        /// </summary>
        public MobGroup CurrentGroupMob
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the model of this npc
        /// </summary>
        public override ushort Model
        {
            get { return base.Model; }
            set
            {
                base.Model = value;
                if (ObjectState == eObjectState.Active)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendModelChange(this, Model);
                }
            }
        }

        public bool IsRenaissance
        {
            get;
            set;
        }

        protected override void GainTension(AttackData source)
        {
            if (source.Attacker == null || MaxTension <= 0)
            {
                return;
            }

            int level_difference = source.Attacker.EffectiveLevel - this.EffectiveLevel;

            if (level_difference <= -5)
            {
                return;
            }

            int tension = level_difference switch
            {
                <= -2 => 1,
                <= 2 => 2,
                <= 6 => 3,
                <= 15 => 5,
                > 15 => 8
            };

            if (IsRenaissance)
            {
                tension = (int)(Properties.MOB_TENSION_RATE * tension * source.TensionRate * 1.10f);
            }
            else
            {
                tension = (int)(Properties.MOB_TENSION_RATE * tension * source.TensionRate);
            }


            Tension += tension;
        }

        /// <summary>
        /// Gets or sets the heading of this NPC
        /// </summary>
        public override ushort Heading
        {
            get { return base.Heading; }
            set
            {
                if (IsTurningDisabled)
                    return;
                ushort oldHeading = base.Heading;
                base.Heading = value;
                if (base.Heading != oldHeading)
                    BroadcastUpdate();
            }
        }

        /// <summary>
        /// Gets or sets the level of this NPC
        /// </summary>
        public override byte Level
        {
            get { return base.Level; }
            set
            {
                bool bMaxHealth = (m_health == MaxHealth);

                if (Level != value)
                {
                    if (Level < 1 && ObjectState == eObjectState.Active)
                    {
                        // This is a newly created NPC, so notify nearby players of its creation
                        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            player.Out.SendNPCCreate(this);
                            if (m_inventory != null)
                                player.Out.SendLivingEquipmentUpdate(this);
                        }
                    }

                    base.Level = value;
                    AutoSetStats();  // Recalculate stats when level changes
                }
                else
                    base.Level = value;

                if (bMaxHealth)
                    m_health = MaxHealth;
            }
        }

        /// <summary>
        /// Auto set stats based on DB entry, npcTemplate, and level.
        /// </summary>
        public virtual void AutoSetStats()
        {
            AutoSetStats(null);
        }

        /// <summary>
        /// Auto set stats based on DB entry, npcTemplate, and level.
        /// </summary>
        /// <param name="dbMob">Mob DB entry to load stats from, retrieved from DB if null</param>
        public virtual void AutoSetStats(Mob dbMob = null)
        {
            // Don't set stats for mobs until their level is set
            if (Level < 1)
                return;

            // We have to check both the DB and template values to account for mobs changing levels.
            // Otherwise, high level mobs retain their stats when their level is lowered by a GM.
            if (NPCTemplate != null && NPCTemplate.ReplaceMobValues)
            {
                Strength = NPCTemplate.Strength;
                Constitution = NPCTemplate.Constitution;
                Quickness = NPCTemplate.Quickness;
                Dexterity = NPCTemplate.Dexterity;
                Intelligence = NPCTemplate.Intelligence;
                Empathy = NPCTemplate.Empathy;
                Piety = NPCTemplate.Piety;
                Charisma = NPCTemplate.Charisma;
                WeaponDps = NPCTemplate.WeaponDps;
                WeaponSpd = NPCTemplate.WeaponSpd;
                ArmorFactor = NPCTemplate.ArmorFactor;
                ArmorAbsorb = NPCTemplate.ArmorAbsorb;
            }
            else
            {
                Mob mob = dbMob;

                if (mob == null && !String.IsNullOrEmpty(InternalID))
                    // This should only happen when a GM command changes level on a mob with no npcTemplate,
                    mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);

                if (mob != null)
                {
                    Strength = mob.Strength;
                    Constitution = mob.Constitution;
                    Quickness = mob.Quickness;
                    Dexterity = mob.Dexterity;
                    Intelligence = mob.Intelligence;
                    Empathy = mob.Empathy;
                    Piety = mob.Piety;
                    Charisma = mob.Charisma;
                }
                else
                {
                    // This is usually a mob about to be loaded from its DB entry,
                    //	but it could also be a new mob created by a GM command, so we need to assign stats.
                    Strength = 0;
                    Constitution = 0;
                    Quickness = 0;
                    Dexterity = 0;
                    Intelligence = 0;
                    Empathy = 0;
                    Piety = 0;
                    Charisma = 0;
                    WeaponDps = 0;
                    WeaponSpd = 0;
                    ArmorFactor = 0;
                    ArmorAbsorb = 0;
                }
            }

            if (Strength < 1)
            {
                Strength = (Properties.MOB_AUTOSET_STR_BASE > 0) ? Properties.MOB_AUTOSET_STR_BASE : (short)1;
                if (Level > 1)
                    Strength += (short)((Level - 1) * Properties.MOB_AUTOSET_STR_MULTIPLIER);
            }

            if (Constitution < 1)
            {
                Constitution = (Properties.MOB_AUTOSET_CON_BASE > 0) ? Properties.MOB_AUTOSET_CON_BASE : (short)1;
                if (Level > 1)
                    Constitution += (short)((Level - 1) * Properties.MOB_AUTOSET_CON_MULTIPLIER);
            }

            if (Quickness < 1)
            {
                Quickness = (Properties.MOB_AUTOSET_QUI_BASE > 0) ? Properties.MOB_AUTOSET_QUI_BASE : (short)1;
                if (Level > 1)
                    Quickness += (short)((Level - 1) * Properties.MOB_AUTOSET_QUI_MULTIPLIER);
            }

            if (Dexterity < 1)
            {
                Dexterity = (Properties.MOB_AUTOSET_DEX_BASE > 0) ? Properties.MOB_AUTOSET_DEX_BASE : (short)1;
                if (Level > 1)
                    Dexterity += (short)((Level - 1) * Properties.MOB_AUTOSET_DEX_MULTIPLIER);
            }

            if (Intelligence < 1)
            {
                Intelligence = (Properties.MOB_AUTOSET_INT_BASE > 0) ? Properties.MOB_AUTOSET_INT_BASE : (short)1;
                if (Level > 1)
                    Intelligence += (short)((Level - 1) * Properties.MOB_AUTOSET_INT_MULTIPLIER);
            }

            if (Empathy < 1)
                Empathy = (short)(29 + Level);

            if (Piety < 1)
                Piety = (short)(29 + Level);

            if (Charisma < 1)
                Charisma = (short)(29 + Level);

            if (WeaponDps < 1)
                WeaponDps = (int)((1.4 + 0.3 * Level + Level * Level * 0.002) * 10);
            if (WeaponSpd < 1)
                WeaponSpd = 30;
            if (ArmorFactor < 1)
                ArmorFactor = (int)((1.0 + (Level / 100.0)) * Level * 1.8);
            if (ArmorAbsorb < 1)
                ArmorAbsorb = (int)((Level - 10) * 0.5 - (Level - 60) * Level * 0.0015).Clamp(0, 75);
        }

        /// <summary>
        /// Gets or Sets the effective level of the Object
        /// </summary>
        public override int EffectiveLevel
        {
            get
            {
                IControlledBrain brain = Brain as IControlledBrain;
                if (brain != null)
                    return brain.Owner.EffectiveLevel;
                return base.EffectiveLevel;
            }
        }

        /// <summary>
        /// Gets or sets the Realm of this NPC
        /// </summary>
        public override eRealm Realm
        {
            get
            {
                IControlledBrain brain = Brain as IControlledBrain;
                if (brain != null)
                    return brain.Owner.Realm; // always realm of the owner
                return base.Realm;
            }
            set
            {
                base.Realm = value;
                if (ObjectState == eObjectState.Active)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendNPCCreate(this);
                        if (m_inventory != null)
                            player.Out.SendLivingEquipmentUpdate(this);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of this npc
        /// </summary>
        public override string Name
        {
            get { return base.Name; }
            set
            {
                base.Name = value;
                if (ObjectState == eObjectState.Active)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendNPCCreate(this);
                        if (m_inventory != null)
                            player.Out.SendLivingEquipmentUpdate(this);
                    }
                }
            }
        }

        /// <summary>
        /// Holds the suffix.
        /// </summary>
        private string m_suffix = string.Empty;
        /// <summary>
        /// Gets or sets the suffix.
        /// </summary>
        public string Suffix
        {
            get { return m_suffix; }
            set
            {
                if (value == null)
                    m_suffix = string.Empty;
                else
                {
                    if (value == m_suffix)
                        return;
                    else
                        m_suffix = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the guild name
        /// </summary>
        public override string GuildName
        {
            get { return base.GuildName; }
            set
            {
                base.GuildName = value;
                if (ObjectState == eObjectState.Active)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendNPCCreate(this);
                        if (m_inventory != null)
                            player.Out.SendLivingEquipmentUpdate(this);
                    }
                }
            }
        }

        /// <summary>
        /// Holds the examine article.
        /// </summary>
        private string m_examineArticle = string.Empty;
        /// <summary>
        /// Gets or sets the examine article.
        /// </summary>
        public string ExamineArticle
        {
            get { return m_examineArticle; }
            set
            {
                if (value == null)
                    m_examineArticle = string.Empty;
                else
                {
                    if (value == m_examineArticle)
                        return;
                    else
                        m_examineArticle = value;
                }
            }
        }

        /// <summary>
        /// Holds the message article.
        /// </summary>
        private string m_messageArticle = string.Empty;
        /// <summary>
        /// Gets or sets the message article.
        /// </summary>
        public string MessageArticle
        {
            get { return m_messageArticle; }
            set
            {
                if (value == null)
                    m_messageArticle = string.Empty;
                else
                {
                    if (value == m_messageArticle)
                        return;
                    else
                        m_messageArticle = value;
                }
            }
        }

        private Faction m_faction = null;
        /// <summary>
        /// Gets the Faction of the NPC
        /// </summary>
        public Faction Faction
        {
            get { return m_faction; }
            set
            {
                m_faction = value;
            }
        }

        private ArrayList m_linkedFactions;
        /// <summary>
        /// The linked factions for this NPC
        /// </summary>
        public ArrayList LinkedFactions
        {
            get { return m_linkedFactions; }
            set { m_linkedFactions = value; }
        }

        private bool m_isConfused;
        /// <summary>
        /// Is this NPC currently confused
        /// </summary>
        public bool IsConfused
        {
            get { return m_isConfused; }
            set { m_isConfused = value; }
        }

        private ushort m_bodyType;
        /// <summary>
        /// The NPC's body type
        /// </summary>
        public ushort BodyType
        {
            get { return m_bodyType; }
            set { m_bodyType = value; }
        }

        private ushort m_houseNumber;
        /// <summary>
        /// The NPC's current house
        /// </summary>
        public ushort HouseNumber
        {
            get { return m_houseNumber; }
            set { m_houseNumber = value; }
        }
        #endregion

        #region Stats


        /// <summary>
        /// Change a stat value
        /// (delegate to GameNPC)
        /// </summary>
        /// <param name="stat">The stat to change</param>
        /// <param name="val">The new value</param>
        public override void ChangeBaseStat(eStat stat, short val)
        {
            int oldstat = GetBaseStat(stat);
            base.ChangeBaseStat(stat, val);
            int newstat = GetBaseStat(stat);
            GameNPC npc = this;
            if (this != null && oldstat != newstat)
            {
                switch (stat)
                {
                    case eStat.STR: npc.Strength = (short)newstat; break;
                    case eStat.DEX: npc.Dexterity = (short)newstat; break;
                    case eStat.CON: npc.Constitution = (short)newstat; break;
                    case eStat.QUI: npc.Quickness = (short)newstat; break;
                    case eStat.INT: npc.Intelligence = (short)newstat; break;
                    case eStat.PIE: npc.Piety = (short)newstat; break;
                    case eStat.EMP: npc.Empathy = (short)newstat; break;
                    case eStat.CHR: npc.Charisma = (short)newstat; break;
                }
            }
        }

        /// <summary>
        /// Gets NPC's constitution
        /// </summary>
        public virtual short Constitution
        {
            get
            {
                return m_charStat[eStat.CON - eStat._First];
            }
            set { m_charStat[eStat.CON - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's dexterity
        /// </summary>
        public virtual short Dexterity
        {
            get { return m_charStat[eStat.DEX - eStat._First]; }
            set { m_charStat[eStat.DEX - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's strength
        /// </summary>
        public virtual short Strength
        {
            get { return m_charStat[eStat.STR - eStat._First]; }
            set { m_charStat[eStat.STR - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's quickness
        /// </summary>
        public virtual short Quickness
        {
            get { return m_charStat[eStat.QUI - eStat._First]; }
            set { m_charStat[eStat.QUI - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's intelligence
        /// </summary>
        public virtual short Intelligence
        {
            get { return m_charStat[eStat.INT - eStat._First]; }
            set { m_charStat[eStat.INT - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's piety
        /// </summary>
        public virtual short Piety
        {
            get { return m_charStat[eStat.PIE - eStat._First]; }
            set { m_charStat[eStat.PIE - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's empathy
        /// </summary>
        public virtual short Empathy
        {
            get { return m_charStat[eStat.EMP - eStat._First]; }
            set { m_charStat[eStat.EMP - eStat._First] = value; }
        }

        /// <summary>
        /// Gets NPC's charisma
        /// </summary>
        public virtual short Charisma
        {
            get { return m_charStat[eStat.CHR - eStat._First]; }
            set { m_charStat[eStat.CHR - eStat._First] = value; }
        }

        public virtual int WeaponDps { get; set; }
        public virtual int WeaponSpd { get; set; }
        public virtual int ArmorFactor { get; set; }
        public virtual int ArmorAbsorb { get; set; }
        #endregion

        #region Flags/Position/SpawnPosition/UpdateTick/Tether
        /// <summary>
        /// Various flags for this npc
        /// </summary>
        [Flags]
        public enum eFlags : uint
        {
            /// <summary>
            /// The npc is translucent (like a ghost)
            /// </summary>
            GHOST = 0x01,
            /// <summary>
            /// The npc is stealthed (nearly invisible, like a stealthed player; new since 1.71)
            /// </summary>
            STEALTH = 0x02,
            /// <summary>
            /// The npc doesn't show a name above its head but can be targeted
            /// </summary>
            DONTSHOWNAME = 0x04,
            /// <summary>
            /// The npc doesn't show a name above its head and can't be targeted
            /// </summary>
            CANTTARGET = 0x08,
            /// <summary>
            /// Not in nearest enemyes if different vs player realm, but can be targeted if model support this
            /// </summary>
            PEACE = 0x10,
            /// <summary>
            /// The npc is flying (z above ground permitted)
            /// </summary>
            FLYING = 0x20,
            /// <summary>
            /// npc's torch is lit
            /// </summary>
            TORCH = 0x40,
            /// <summary>
            /// npc is a statue (no idle animation, no target...)
            /// </summary>
            STATUE = 0x80,
            /// <summary>
            /// npc is swimming
            /// </summary>
            SWIMMING = 0x100
        }

        /// <summary>
        /// Holds various flags of this npc
        /// </summary>
        protected eFlags m_flags;
        /// <summary>
        /// Spawn point
        /// </summary>
        protected Vector3 m_spawnPoint;
        /// <summary>
        /// Spawn Heading
        /// </summary>
        protected ushort m_spawnHeading;


        /// <summary>
        /// package ID defined form this NPC
        /// </summary>
        protected string m_packageID;

        public string PackageID
        {
            get { return m_packageID; }
            set { m_packageID = value; }
        }

        /// <summary>
        /// The last time this NPC sent the 0x09 update packet
        /// </summary>
        protected volatile uint m_lastUpdateTickCount = uint.MinValue;
        /// <summary>
        /// The last time this NPC was actually updated to at least one player
        /// </summary>
        protected volatile uint m_lastVisibleToPlayerTick = uint.MinValue;

        /// <summary>
        /// Gets or Sets the flags of this npc
        /// </summary>
        public virtual eFlags Flags
        {
            get
            {
                if (tempoarallyFlags != 0)
                    return tempoarallyFlags;
                return m_flags;
            }
            set
            {
                eFlags oldflags = m_flags;
                m_flags = value;

                if (ObjectState == eObjectState.Active)
                {
                    if (oldflags != m_flags)
                    {
                        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        {
                            player.Out.SendNPCCreate(this);
                            if (m_inventory != null)
                                player.Out.SendLivingEquipmentUpdate(this);
                        }
                    }
                }
            }
        }

        public bool IsGhost => Flags.HasFlag(eFlags.GHOST);
        public override bool IsStealthed => Flags.HasFlag(eFlags.STEALTH);
        public bool IsDontShowName => Flags.HasFlag(eFlags.DONTSHOWNAME);
        public bool IsCannotTarget => Flags.HasFlag(eFlags.CANTTARGET);
        public bool IsPeaceful => Flags.HasFlag(eFlags.PEACE);
        public bool IsFlying => Flags.HasFlag(eFlags.FLYING);
        public bool IsTorchLit => Flags.HasFlag(eFlags.TORCH);
        public bool IsStatue => Flags.HasFlag(eFlags.STATUE);
        public override bool IsUnderwater
            => Flags.HasFlag(eFlags.SWIMMING) || base.IsUnderwater;

        /// <summary>
        /// Set the NPC to stealth or unstealth
        /// </summary>
        /// <param name="goStealth">True to stealth, false to unstealth</param>
        public override void Stealth(bool goStealth)
        {
            if (goStealth != IsStealthed)
            {
                if (goStealth)
                    Flags |= eFlags.STEALTH;
                else
                    Flags &= ~eFlags.STEALTH;

                if (!goStealth && Brain is IControlledBrain brain && brain.Owner is GameLiving living && living.IsStealthed)
                    living.Stealth(false);
            }
        }

        /// <summary>
        /// Shows wether any player sees that mob
        /// we dont need to calculate things like AI if mob is in no way
        /// visible to at least one player
        /// </summary>
        public virtual bool IsVisibleToPlayers => GameTimer.GetTickCount() - m_lastVisibleToPlayerTick < 60000;

        /// <summary>
        /// Gets or sets the spawnposition of this npc
        /// </summary>
        public virtual Vector3 SpawnPoint
        {
            get { return m_spawnPoint; }
            set { m_spawnPoint = value; }
        }

        /// <summary>
        /// Gets or sets the spawnposition of this npc
        /// </summary>
        [Obsolete("Use GameNPC.SpawnPoint")]
        public float SpawnX
        {
            get { return m_spawnPoint.X; }
            set { m_spawnPoint.X = value; }
        }
        /// <summary>
        /// Gets or sets the spawnposition of this npc
        /// </summary>
        [Obsolete("Use GameNPC.SpawnPoint")]
        public float SpawnY
        {
            get { return m_spawnPoint.Y; }
            set { m_spawnPoint.Y = value; }
        }
        /// <summary>
        /// Gets or sets the spawnposition of this npc
        /// </summary>
        [Obsolete("Use GameNPC.SpawnPoint")]
        public float SpawnZ
        {
            get { return m_spawnPoint.Z; }
            set { m_spawnPoint.Z = value; }
        }

        /// <summary>
        /// Gets or sets the spawnheading of this npc
        /// </summary>
        public virtual ushort SpawnHeading
        {
            get { return m_spawnHeading; }
            set { m_spawnHeading = value; }
        }

        /// <summary>
        /// Stores the currentwaypoint that npc has to wander to
        /// </summary>
        protected PathPoint m_currentWayPoint = null;

        /// <summary>
        /// Gets sets the speed for traveling on path
        /// </summary>
        public short PathingNormalSpeed
        {
            get { return m_pathingNormalSpeed; }
            set { m_pathingNormalSpeed = value; }
        }
        /// <summary>
        /// Stores the speed for traveling on path
        /// </summary>
        protected short m_pathingNormalSpeed;

        protected int m_maxdistance;
        /// <summary>
        /// The Mob's max distance from its spawn before return automatically
        /// if MaxDistance > 0 ... the amount is the normal value
        /// if MaxDistance = 0 ... no maxdistance check
        /// if MaxDistance less than 0 ... the amount is calculated in procent of the value and the aggrorange (in StandardMobBrain)
        /// </summary>
        public int MaxDistance
        {
            get { return m_maxdistance; }
            set { m_maxdistance = value; }
        }

        protected int m_roamingRange;
        /// <summary>
        /// radius for roaming
        /// </summary>
        public int RoamingRange
        {
            get { return m_roamingRange; }
            set { m_roamingRange = value; }
        }

        protected int m_tetherRange;

        /// <summary>
        /// The mob's tether range; if mob is pulled farther than this distance
        /// it will return to its spawn point.
        /// if TetherRange > 0 ... the amount is the normal value
        /// if TetherRange less or equal 0 ... no tether check
        /// </summary>
        public int TetherRange
        {
            get { return m_tetherRange; }
            set { m_tetherRange = value; }
        }

        /// <summary>
        /// True, if NPC is out of tether range, false otherwise; if no tether
        /// range is specified, this will always return false.
        /// </summary>
        public bool IsOutOfTetherRange
        {
            get
            {
                if (TetherRange > 0)
                {
                    if (this.IsWithinRadius(this.SpawnPoint, TetherRange))
                        return false;
                    else
                        return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Is this object visible to another?
        /// </summary>
        /// <param name="checkObject"></param>
        /// <returns></returns>
        public override bool IsVisibleTo(GameObject checkObject)
        {
            if (base.IsVisibleTo(checkObject))
            {
                if (EventID != null && EventID != "" && checkObject is GamePlayer player)
                {
                    var gameEvents = GameEventManager.Instance.Events.Where(e => e.ID.Equals(EventID));
                    switch (gameEvents.FirstOrDefault().InstancedConditionType)
                    {
                        case InstancedConditionTypes.All:
                            return true;
                        case InstancedConditionTypes.Player:
                            return gameEvents.Where(e => e.Owner != null && e.Owner == player && e.Mobs.Contains(this)).Any();
                        case InstancedConditionTypes.Group:
                            return gameEvents.Where(e => e.Owner != null && e.Owner.Group != null && e.Owner.Group.IsInTheGroup(player) && e.Mobs.Contains(this)).Any();
                        case InstancedConditionTypes.Guild:
                            return gameEvents.Where(e => e.Owner != null && e.Owner.Guild != null && e.Owner.Guild == player.Guild && e.Mobs.Contains(this)).Any();
                        case InstancedConditionTypes.Battlegroup:
                            return gameEvents.Where(e => e.Owner != null && e.Owner.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null) != null &&
                            e.Owner.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null) ==
                            player.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null) && e.Mobs.Contains(this)).Any();
                        default:
                            break;
                    }
                }
                else
                {
                    return true;
                }

            }
            return false;
        }
        #endregion

        #region Movement
        /// <summary>
        /// Timer to be set if an OnArriveAtTarget
        /// handler is set before calling the WalkTo function
        /// </summary>
        protected ArriveAtTargetAction m_arriveAtTargetAction;

        /// <summary>
        /// Is the mob roaming towards a target?
        /// </summary>
        public bool IsRoaming => m_arriveAtTargetAction != null && m_arriveAtTargetAction.IsAlive;

        /// <summary>
        /// Object that this npc is following as weakreference
        /// </summary>
        protected WeakReference m_followTarget;
        /// <summary>
        /// Max range to keep following
        /// </summary>
        protected int m_followMaxDist;
        /// <summary>
        /// Min range to keep to the target
        /// </summary>
        protected int m_followMinDist;
        /// <summary>
        /// Timer with purpose of follow updating
        /// </summary>
        protected RegionTimer m_followTimer;
        /// <summary>
        /// Property entry on follow timer, wether the follow target is in range
        /// </summary>
        protected const string FOLLOW_TARGET_IN_RANGE = "FollowTargetInRange";
        /// <summary>
        /// Minimum allowed attacker follow distance to avoid issues with client / server resolution (herky jerky motion)
        /// </summary>
        protected const int MIN_ALLOWED_FOLLOW_DISTANCE = 100;
        /// <summary>
        /// Minimum allowed pet follow distance
        /// </summary>
        protected const int MIN_ALLOWED_PET_FOLLOW_DISTANCE = 90;
        /// <summary>
        /// At what health percent will npc give up range attack and rush the attacker
        /// </summary>
        protected const int MINHEALTHPERCENTFORRANGEDATTACK = 70;

        private string m_pathID;
        public string PathID
        {
            get { return m_pathID; }
            set { m_pathID = value; }
        }

        private Vector3 _basePosition = Vector3.Zero;
        public override Vector3 Position
        {
            set => _basePosition = value;
            get
            {
                if (!IsMoving || TargetPosition == Vector3.Zero)
                    return _basePosition;
                if (MovementElapsedTicks > (Vector2.Distance(_basePosition.ToVector2(), TargetPosition.ToVector2()) * 1000 / CurrentSpeed))
                {
                    return TargetPosition;
                }
                _basePosition.Z = TargetPosition.Z;
                return _basePosition + MovementElapsedTicks * Velocity;
            }
        }
        /// <summary>
        /// The target position.
        /// </summary>
        public Vector3 TargetPosition
        {
            get;
            private set;
        }

        /// <summary>
        /// Is allowed to attack anyone?
        /// </summary>
        public virtual bool ApplyAttackRules
        {
            get;
            set;
        } = true;


        /// <summary>
        /// The target object.
        /// </summary>
        public override GameObject TargetObject
        {
            get
            {
                return base.TargetObject;
            }
            set
            {
                GameObject previousTarget = TargetObject;
                GameObject newTarget = value;

                base.TargetObject = newTarget;

                if (previousTarget != null && newTarget != previousTarget)
                    previousTarget.Notify(GameNPCEvent.SwitchedTarget, this,
                                          new SwitchedTargetEventArgs(previousTarget, newTarget));
            }
        }

        /// <summary>
        /// True if the mob is at its target position, else false.
        /// </summary>
        public bool IsAtTargetPosition => Vector3.DistanceSquared(Position, TargetPosition) < 1.0f;

        /// <summary>
        /// Turns the npc towards a specific heading
        /// optionally sends update to client
        /// </summary>
        /// <param name="newHeading">the new heading</param>
        public virtual void TurnTo(ushort heading, bool sendUpdate)
        {
            if (IsStunned || IsMezzed) return;

            Notify(GameNPCEvent.TurnToHeading, this, new TurnToHeadingEventArgs(heading));

            if (sendUpdate && Heading != heading)
                Heading = heading;
            else
                base.Heading = heading;
        }

        public void TurnTo(float tx, float ty) => TurnTo(tx, ty, true);
        public void TurnTo(float tx, float ty, bool sendUpdate) => TurnTo(GameMath.GetHeading(Position, new Vector2(tx, ty)), sendUpdate);
        public void TurnTo(ushort heading) => TurnTo(heading, true);
        public void TurnTo(GameObject target) => TurnTo(target, true);
        public void TurnTo(GameObject target, bool sendUpdate)
        {
            if (target == null || target.CurrentRegion != CurrentRegion)
                return;

            TurnTo(target.Position.X, target.Position.Y, sendUpdate);
        }

        /// <summary>
        /// Turns the NPC towards a specific gameObject
        /// which can be anything ... a player, item, mob, npc ...
        /// and turn back after specified duration
        /// </summary>
        /// <param name="target">GameObject to turn towards</param>
        /// <param name="duration">restore heading after this duration</param>
        public virtual void TurnTo(GameObject target, int duration)
        {
            if (target == null || target.CurrentRegion != CurrentRegion)
                return;

            // Store original heading if not set already.

            RestoreHeadingAction restore = (RestoreHeadingAction)TempProperties.getProperty<object>(RESTORE_HEADING_ACTION_PROP, null);

            if (restore == null)
            {
                restore = new RestoreHeadingAction(this);
                TempProperties.setProperty(RESTORE_HEADING_ACTION_PROP, restore);
            }

            TurnTo(target);
            restore.Start(duration);
        }

        /// <summary>
        /// The property used to store the NPC heading restore action
        /// </summary>
        protected const string RESTORE_HEADING_ACTION_PROP = "NpcRestoreHeadingAction";

        /// <summary>
        /// Restores the NPC heading after some time
        /// </summary>
        protected class RestoreHeadingAction : RegionAction
        {
            /// <summary>
            /// The NPCs old heading
            /// </summary>
            protected readonly ushort m_oldHeading;

            /// <summary>
            /// The NPCs old position
            /// </summary>
            protected readonly Vector3 m_oldPosition;

            /// <summary>
            /// Creates a new TurnBackAction
            /// </summary>
            /// <param name="actionSource">The source of action</param>
            public RestoreHeadingAction(GameNPC actionSource)
                : base(actionSource)
            {
                m_oldHeading = actionSource.Heading;
                m_oldPosition = actionSource.Position;
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override void OnTick()
            {
                GameNPC npc = (GameNPC)m_actionSource;

                npc.TempProperties.removeProperty(RESTORE_HEADING_ACTION_PROP);

                if (npc.ObjectState != eObjectState.Active) return;
                if (!npc.IsAlive) return;
                if (npc.AttackState) return;
                if (npc.IsMoving) return;
                if (npc.Equals(m_oldPosition)) return;
                if (npc.Heading == m_oldHeading) return; // already set? oO

                npc.TurnTo(m_oldHeading);
            }
        }

        /// <summary>
        /// Gets the last time this mob was updated
        /// </summary>
        public uint LastUpdateTickCount
        {
            get { return m_lastUpdateTickCount; }
        }

        /// <summary>
        /// Gets the last this this NPC was actually update to at least one player.
        /// </summary>
        public uint LastVisibleToPlayersTickCount
        {
            get { return m_lastVisibleToPlayerTick; }
        }

        /// <summary>
        /// Delayed action that fires an event when an NPC arrives at its target
        /// </summary>
        protected class ArriveAtTargetAction : RegionAction
        {
            /// <summary>
            /// Constructs a new ArriveAtTargetAction
            /// </summary>
            /// <param name="actionSource">The action source</param>
            public ArriveAtTargetAction(GameNPC actionSource) : base(actionSource)
            {
            }

            /// <summary>
            /// This function is called when the Mob arrives at its target spot
            /// This time was estimated using walking speed and distance.
            /// It fires the ArriveAtTarget event
            /// </summary>
            protected override void OnTick()
            {
                GameNPC npc = (GameNPC)m_actionSource;
                npc._OnArrivedAtTarget();
            }
        }

        protected void _OnArrivedAtTarget()
        {
            var arriveAtSpawnPoint = IsReturningToSpawnPoint;
            StopMoving();

            Notify(GameNPCEvent.ArriveAtTarget, this);
            if (arriveAtSpawnPoint)
                Notify(GameNPCEvent.ArriveAtSpawnPoint, this);
        }

        public void CancelWalkToTimer()
        {
            _arriveAtPathNodeAction?.Stop();
            _arriveAtPathNodeAction = null;
            m_arriveAtTargetAction?.Stop();
            m_arriveAtTargetAction = null;
        }

        /// <summary>
        /// Ticks required to arrive at a given spot.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public int GetTicksToArriveAt(Vector3 target, short speed)
        {
            return (int)(Vector2.Distance(Position.ToVector2(), target.ToVector2()) * 1000 / speed);
        }

        /// <summary>
        /// Walk to a certain spot at a given speed.
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="tz"></param>
        /// <param name="speed"></param>
        [Obsolete("Use .PathTo instead")]
        public void WalkTo(float targetX, float targetY, float targetZ, short speed)
        {
            WalkTo(new Vector3(targetX, targetY, targetZ), speed);
        }

        /// <summary>
        /// Walk to a certain spot at a given speed.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="speed"></param>
        public virtual void WalkTo(Vector3 target, short speed)
        {
            if (IsTurningDisabled)
                return;

            if (speed > MaxSpeed)
                speed = MaxSpeed;

            if (speed <= 0)
                return;

            if (IsWithinRadius(target, CONST_WALKTOTOLERANCE))
            {

                // No need to start walking.
                TargetPosition = target;
                Position = target;
                _OnArrivedAtTarget();
                return;
            }

            _StartWalk(target, speed);
        }

        private void StartArriveAtTargetAction(int requiredTicks)
        {
            m_arriveAtTargetAction = new ArriveAtTargetAction(this);
            m_arriveAtTargetAction.Start((requiredTicks > 1) ? requiredTicks : 1);
        }

        /// <summary>
        /// Walk to the spawn point
        /// </summary>
        public virtual void WalkToSpawn()
        {
            WalkToSpawn((short)(MaxSpeed / 2.5));
        }

        /// <summary>
        /// Walk to the spawn point
        /// </summary>
        public virtual void CancelWalkToSpawn()
        {
            CancelWalkToTimer();
            IsReturningHome = false;
            IsReturningToSpawnPoint = false;
        }

        /// <summary>
        /// Walk to the spawn point with specified speed
        /// </summary>
        public virtual void WalkToSpawn(short speed)
        {
            StopAttack();
            StopFollowing();

            if (Brain is StandardMobBrain brain && brain.HasAggro)
                brain.ClearAggroList();


            TargetObject = null;
            if (Brain is IControlledBrain)
                return;

            IsReturningHome = true;
            IsReturningToSpawnPoint = true;
            if (TPPoint != null)
                PathTo(new Vector3((float)TPPoint.Position.X, (float)TPPoint.Position.Y, (float)TPPoint.Position.Z), speed);
            else
                PathTo(SpawnPoint, speed);
        }

        /// <summary>
        /// Helper component for efficiently calculating paths
        /// </summary>
        public PathCalculator PathCalculator { get; protected set; } // Only visible for debugging

        /// <summary>
        /// Finds a valid path to the destination (or picks the direct path otherwise). Uses WalkTo for each of the pathing nodes.
        /// </summary>
        /// <returns>true if a path was found</returns>
        public void PathTo(float destX, float destY, float destZ, short? speed = null)
        {
            PathTo(new Vector3(destX, destY, destZ), speed);
        }
        /// <summary>
        /// Finds a valid path to the destination (or picks the direct path otherwise). Uses WalkTo for each of the pathing nodes.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="speed"></param>
        /// <returns>true if a path was found</returns>
        public void PathTo(Vector3 dest, short? speed = null)
        {
            if (dest == Position)
                return;
            if (IsTurningDisabled)
                return;

            short walkSpeed = speed ?? MaxSpeed;
            if (walkSpeed > MaxSpeed)
                walkSpeed = MaxSpeed;
            if (walkSpeed <= 0)
                return;
            if (!PathCalculator.ShouldPath(this, dest))
            {
                WalkTo(dest, walkSpeed);
                return;
            }

            if (PathCalculator == null)
            {
                if (!PathCalculator.IsSupported(this))
                {
                    WalkTo(dest, walkSpeed);
                    return;
                }

                // TODO: Only make this check once on spawn since it internally calls .CurrentZone + hashtable lookup?
                PathCalculator = new PathCalculator(this);
                PathCalculator.VisualizePath = DebugMode;
            }

            DebugSend("PathTo({0}, {1})", dest, walkSpeed);

            Interlocked.Increment(ref Statistics.PathToCalls);

            // Pick the next pathing node, and walk towards it
            var (nextNode, reason) = PathCalculator.CalculateNextTarget(dest);
            var shouldUseAirPath = reason == NoPathReason.RECAST_FOUND_NO_PATH;

            if (!nextNode.HasValue)
            {
                // Directly walk towards the target (or call the customly provided action)
                if (shouldUseAirPath)
                    WalkTo(dest, walkSpeed);
                return;
            }

            Notify(GameNPCEvent.WalkTo, this, new WalkToEventArgs(dest, walkSpeed));
            // Do the actual pathing bit: Walk towards the next pathing node
            _WalkToPathNode(nextNode.Value, walkSpeed);
        }

        private void _WalkToPathNode(Vector3 node, short speed)
        {
            if (IsTurningDisabled)
                return;

            if (speed > MaxSpeed)
                speed = MaxSpeed;

            if (speed <= 0)
                return;

            _StartWalk(node, speed);

            _StartArriveAtPathNodeAction(GetTicksToArriveAt(node, speed));

        }

        private void _StartWalk(Vector3 target, short speed)
        {
            CancelWalkToTimer();

            if (IsMoving)
            {
                Position = Position;
            }
            m_Heading = GetHeading(target);
            TargetPosition = target;
            CurrentSpeed = speed;
            MovementStartTick = GameTimer.GetTickCount();

            UpdateTickSpeed();
            StartArriveAtTargetAction(GetTicksToArriveAt(TargetPosition, speed));
            BroadcastUpdate();
        }

        private ArriveAtPathNodeAction _arriveAtPathNodeAction;
        private void _StartArriveAtPathNodeAction(int requiredTicks)
        {
            CancelWalkToTimer();
            var action = new ArriveAtPathNodeAction(this);
            action.Start(Math.Max(1,requiredTicks));
            _arriveAtPathNodeAction = action;
        }
        private class ArriveAtPathNodeAction : RegionAction
        {
            public ArriveAtPathNodeAction(GameObject actionSource) : base(actionSource)
            {
            }
            protected override void OnTick()
            {
                var npc = (GameNPC)m_actionSource;
                npc.DebugSend("calculate next node..." + npc.MovementElapsedTicks + " / " + (uint)(Vector3.Distance(npc._basePosition, npc.TargetPosition) * 1000 / npc.CurrentSpeed));
                // Pick the next pathing node, and walk towards it
                var (nextNode, _reason) = npc.PathCalculator.CalculateNextTarget();
                if (!nextNode.HasValue)
                {
                    // Directly walk towards the target (or call the customly provided action)
                    npc.WalkTo(npc.TargetPosition, npc.CurrentSpeed);
                    return;
                }

                npc.DebugSend("Next target for {0} is {1}", npc.TargetPosition, nextNode.Value);
                // Do the actual pathing bit: Walk towards the next pathing node
                npc._WalkToPathNode(nextNode.Value, npc.CurrentSpeed);
            }
        }

        /// <summary>
        /// Clears all remaining elements in our pathing cache.
        /// </summary>
        public void ClearPathingCache()
        {
            PathCalculator?.Clear();
        }


        /// <summary>
        /// Gets the NPC current follow target
        /// </summary>
        public GameObject CurrentFollowTarget
        {
            get { return m_followTarget.Target as GameObject; }
        }

        /// <summary>
        /// Stops the movement of the mob.
        /// </summary>
        public void StopMoving()
        {
            CancelWalkToSpawn();

            if (!IsMoving)
                return;
            if (MovementElapsedTicks > (Vector3.Distance(_basePosition, TargetPosition) * 1000 / CurrentSpeed))
            {
                Position = TargetPosition;
            }
            else
                Position = _basePosition + MovementElapsedTicks * Velocity;
            MovementStartTick = GameTimer.GetTickCount();
            TargetPosition = Vector3.Zero;
            CurrentSpeed = 0;
            BroadcastUpdate();
        }

        protected override void UpdateTickSpeed(Vector3? target = null)
        {
            if (CurrentSpeed == 0)
            {
                base.UpdateTickSpeed(target);
                return;
            }

            if ((target ?? TargetPosition) == Vector3.Zero)
            {
                CurrentSpeed = 0;
                return;
            }

            Heading = GetHeading(target ?? TargetPosition);
            base.UpdateTickSpeed(target ?? TargetPosition);
        }

        public override void UpdateMaxSpeed()
        {
            if (!IsMoving)
                return;

            if (CurrentSpeed > MaxSpeed || !IsReturningToSpawnPoint)
            {
                Position = Position;
                CurrentSpeed = MaxSpeed;
                BroadcastUpdate();
            }
        }

        public const int STICKMINIMUMRANGE = 100;
        public const int STICKMAXIMUMRANGE = 5000;

        /// <summary>
        /// Follow given object
        /// </summary>
        /// <param name="target">Target to follow</param>
        /// <param name="minDistance">Min distance to keep to the target</param>
        /// <param name="maxDistance">Max distance to keep following</param>
        public virtual void Follow(GameObject target, int minDistance, int maxDistance)
        {
            if (m_followTimer.IsAlive)
                m_followTimer.Stop();

            if (target == null || target.ObjectState != eObjectState.Active)
                return;

            m_followMaxDist = maxDistance;
            m_followMinDist = minDistance;
            m_followTarget.Target = target;
            m_followTimer.Start(100);
        }

        /// <summary>
        /// Stop following
        /// </summary>
        public virtual void StopFollowing()
        {
            lock (m_followTimer)
            {
                if (m_followTimer.IsAlive)
                    m_followTimer.Stop();

                m_followTarget.Target = null;
                StopMoving();
            }
        }

        /// <summary>
        /// Will be called if follow mode is active
        /// and we reached the follow target
        /// </summary>
        public virtual void FollowTargetInRange()
        {
            if (AttackState)
            {
                // if in last attack the enemy was out of range, we can attack him now immediately
                AttackData ad = (AttackData)TempProperties.getProperty<object>(LAST_ATTACK_DATA, null);
                if (ad != null && ad.AttackResult == eAttackResult.OutOfRange)
                {
                    m_attackAction.Start(1);// schedule for next tick
                }
            }
            //sirru
            else if (m_attackers.Count == 0 && this.Spells.Count > 0 && this.TargetObject != null && GameServer.ServerRules.IsAllowedToAttack(this, (this.TargetObject as GameLiving), true))
            {
                if (TargetObject.Realm == 0 || Realm == 0)
                    m_lastAttackTickPvE = m_CurrentRegion.Time;
                else m_lastAttackTickPvP = m_CurrentRegion.Time;
                if (this.CurrentRegion.Time - LastAttackedByEnemyTick > 5 * 1000)
                {
                    // Aredhel: Erm, checking for spells in a follow method, what did we create
                    // brain classes for again?

                    //Check for negatively casting spells
                    StandardMobBrain stanBrain = (StandardMobBrain)Brain;
                    if (stanBrain != null)
                        ((StandardMobBrain)stanBrain).CheckSpells(StandardMobBrain.eCheckSpellType.Offensive);
                }
            }
        }

        /// <summary>
        /// Keep following a specific object at a max distance
        /// </summary>
        protected virtual int FollowTimerCallback(RegionTimer callingTimer)
        {
            if (IsCasting)
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;

            bool wasInRange = m_followTimer.Properties.getProperty(FOLLOW_TARGET_IN_RANGE, false);
            m_followTimer.Properties.removeProperty(FOLLOW_TARGET_IN_RANGE);

            GameObject followTarget = (GameObject)m_followTarget.Target;
            GameLiving followLiving = followTarget as GameLiving;

            //Stop following if target living is dead
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
            //Tolakram: a Z of 0 does not indicate on the ground.  Z varies based on terrain  Removed 0 Z check
            float distance = diff.Length();

            //if distance is greater then the max follow distance, stop following and return home
            if ((int)distance > m_followMaxDist)
            {
                StopFollowing();
                Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
                this.WalkToSpawn();
                return 0;
            }
            float newX, newY, newZ;

            if (this.Brain is StandardMobBrain)
            {
                StandardMobBrain brain = this.Brain as StandardMobBrain;

                //if the npc hasn't hit or been hit in a while, stop following and return home
                if (!(Brain is IControlledBrain))
                {
                    if (AttackState && brain != null && followLiving != null)
                    {
                        long seconds = 20 + ((brain.GetAggroAmountForLiving(followLiving) / (MaxHealth + 1)) * 100);
                        long lastattacked = LastAttackTick;
                        long lasthit = LastAttackedByEnemyTick;
                        if (CurrentRegion.Time - lastattacked > seconds * 1000 && CurrentRegion.Time - lasthit > seconds * 1000)
                        {
                            //StopFollow();
                            Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
                            //brain.ClearAggroList();
                            this.WalkToSpawn();
                            return 0;
                        }
                    }
                }

                //If we're part of a formation, we can get out early.
                newX = followTarget.Position.X;
                newY = followTarget.Position.Y;
                newZ = followTarget.Position.Z;
                if (brain.CheckFormation(ref newX, ref newY, ref newZ))
                {
                    PathTo(newX, newY, (ushort)newZ, MaxSpeed);
                    return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
                }
            }

            // Tolakram - Distances under 100 do not calculate correctly leading to the mob always being told to walkto
            int minAllowedFollowDistance = MIN_ALLOWED_FOLLOW_DISTANCE;

            // pets can follow closer.  need to implement /fdistance command to make this adjustable
            if (this.Brain is IControlledBrain)
                minAllowedFollowDistance = MIN_ALLOWED_PET_FOLLOW_DISTANCE;

            //Are we in range yet?
            if ((int)distance <= (m_followMinDist < minAllowedFollowDistance ? minAllowedFollowDistance : m_followMinDist))
            {
                StopMoving();
                TurnTo(followTarget);
                if (!wasInRange)
                {
                    m_followTimer.Properties.setProperty(FOLLOW_TARGET_IN_RANGE, true);
                    FollowTargetInRange();
                }
                return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
            }

            // follow on distance
            diff = (diff / distance) * m_followMinDist;

            //Subtract the offset from the target's position to get
            //our target position
            if (InCombat || Brain is BomberBrain || !IsWithinRadius(followTarget, MaxSpeed))
                PathTo(followTarget.Position - diff, MaxSpeed);
            else
                PathTo(followTarget.Position - diff, (short)((followLiving?.CurrentSpeed ?? 0) + 50));
            return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
        }

        /// <summary>
        /// Disables the turning for this living
        /// </summary>
        /// <param name="add"></param>
        public override void DisableTurning(bool add)
        {
            bool old = IsTurningDisabled;
            base.DisableTurning(add);
            if (old != IsTurningDisabled)
                BroadcastUpdate();
        }

        #endregion

        #region Path (Movement)
        /// <summary>
        /// Gets sets the currentwaypoint that npc has to wander to
        /// </summary>
        public PathPoint CurrentWayPoint
        {
            get { return m_currentWayPoint; }
            set { m_currentWayPoint = value; }
        }

        /// <summary>
        /// Is the NPC returning home, if so, we don't want it to think
        /// </summary>
        public bool IsReturningHome
        {
            get { return m_isReturningHome; }
            set { m_isReturningHome = value; }
        }

        protected bool m_isReturningHome = false;

        /// <summary>
        /// Whether or not the NPC is on its way back to the spawn point.
        /// [Aredhel: I decided to add this property in order not to mess
        /// with SMB and IsReturningHome. Also, to prevent outside classes
        /// from interfering the setter is now protected.]
        /// </summary>
        public bool IsReturningToSpawnPoint { get; protected set; }

        /// <summary>
        /// Gets if npc moving on path
        /// </summary>
        public bool IsMovingOnPath
        {
            get { return m_IsMovingOnPath; }
        }
        /// <summary>
        /// Stores if npc moving on path
        /// </summary>
        protected bool m_IsMovingOnPath = false;

        /// <summary>
        /// let the npc travel on its path
        /// </summary>
        /// <param name="speed">Speed on path</param>
        public void MoveOnPath(short speed)
        {
            if (IsMovingOnPath)
                StopMovingOnPath();

            if (CurrentWayPoint == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("No path to travel on for " + Name);
                return;
            }

            PathingNormalSpeed = speed;

            if (this.IsWithinRadius(CurrentWayPoint.Position, 100))
            {
                // reaching a waypoint can start an ambient sentence
                FireAmbientSentence(eAmbientTrigger.moving);

                if (CurrentWayPoint.Type == ePathType.Path_Reverse && CurrentWayPoint.FiredFlag)
                    CurrentWayPoint = CurrentWayPoint.Prev;
                else
                {
                    if ((CurrentWayPoint.Type == ePathType.Loop) && (CurrentWayPoint.Next == null))
                        CurrentWayPoint = MovementMgr.FindFirstPathPoint(CurrentWayPoint);
                    else
                        CurrentWayPoint = CurrentWayPoint.Next;
                }
            }

            if (CurrentWayPoint != null)
            {
                GameEventMgr.AddHandler(this, GameNPCEvent.ArriveAtTarget, OnArriveAtWaypoint);
                PathTo(CurrentWayPoint.Position, Math.Min(speed, (short)CurrentWayPoint.MaxSpeed));
                m_IsMovingOnPath = true;
                Notify(GameNPCEvent.PathMoveStarts, this);
            }
            else
            {
                StopMovingOnPath();
            }
        }

        /// <summary>
        /// Stop moving on path.
        /// </summary>
        public void StopMovingOnPath()
        {
            if (!IsMovingOnPath)
                return;

            GameEventMgr.RemoveHandler(this, GameNPCEvent.ArriveAtTarget, OnArriveAtWaypoint);
            Notify(GameNPCEvent.PathMoveEnds, this);
            m_IsMovingOnPath = false;
        }

        /// <summary>
        /// decides what to do on reached waypoint in path
        /// </summary>
        /// <param name="e"></param>
        /// <param name="n"></param>
        /// <param name="args"></param>
        protected void OnArriveAtWaypoint(DOLEvent e, object n, EventArgs args)
        {
            if (!IsMovingOnPath || n != this)
                return;

            if (CurrentWayPoint != null)
            {
                WaypointDelayAction waitTimer = new WaypointDelayAction(this);
                waitTimer.Start(Math.Max(1, CurrentWayPoint.WaitTime * 100));
            }
            else
                StopMovingOnPath();
        }

        /// <summary>
        /// Delays movement to the next waypoint
        /// </summary>
        protected class WaypointDelayAction : RegionAction
        {
            /// <summary>
            /// Constructs a new WaypointDelayAction
            /// </summary>
            /// <param name="actionSource"></param>
            public WaypointDelayAction(GameObject actionSource)
                : base(actionSource)
            {
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override void OnTick()
            {
                GameNPC npc = (GameNPC)m_actionSource;
                if (!npc.IsMovingOnPath)
                    return;
                PathPoint oldPathPoint = npc.CurrentWayPoint;
                PathPoint nextPathPoint = npc.CurrentWayPoint.Next;
                if ((npc.CurrentWayPoint.Type == ePathType.Path_Reverse) && (npc.CurrentWayPoint.FiredFlag))
                    nextPathPoint = npc.CurrentWayPoint.Prev;

                if (nextPathPoint == null)
                {
                    switch (npc.CurrentWayPoint.Type)
                    {
                        case ePathType.Loop:
                            {
                                npc.CurrentWayPoint = MovementMgr.FindFirstPathPoint(npc.CurrentWayPoint);
                                npc.Notify(GameNPCEvent.PathMoveStarts, npc);
                                break;
                            }
                        case ePathType.Once:
                            npc.CurrentWayPoint = null;//to stop
                            break;
                        case ePathType.Path_Reverse://invert sens when go to end of path
                            if (oldPathPoint.FiredFlag)
                                npc.CurrentWayPoint = npc.CurrentWayPoint.Next;
                            else
                                npc.CurrentWayPoint = npc.CurrentWayPoint.Prev;
                            break;
                    }
                }
                else
                {
                    if ((npc.CurrentWayPoint.Type == ePathType.Path_Reverse) && (npc.CurrentWayPoint.FiredFlag))
                        npc.CurrentWayPoint = npc.CurrentWayPoint.Prev;
                    else
                        npc.CurrentWayPoint = npc.CurrentWayPoint.Next;
                }
                oldPathPoint.FiredFlag = !oldPathPoint.FiredFlag;

                if (npc.CurrentWayPoint != null)
                {
                    npc.PathTo(npc.CurrentWayPoint.Position, (short)Math.Min(npc.PathingNormalSpeed, npc.CurrentWayPoint.MaxSpeed));
                }
                else
                {
                    npc.StopMovingOnPath();
                }
            }
        }
        #endregion

        #region Inventory/LoadfromDB
        private NpcTemplate m_npcTemplate = null;
        /// <summary>
        /// The NPC's template
        /// </summary>
        public NpcTemplate NPCTemplate
        {
            get
            {
                if (temporallyTemplate != null)
                    return temporallyTemplate as NpcTemplate;
                return m_npcTemplate;
            }
            set
            {
                if (temporallyTemplate == null)
                    m_npcTemplate = value;
            }
        }
        /// <summary>
        /// Loads the equipment template of this npc
        /// </summary>
        /// <param name="equipmentTemplateID">The template id</param>
        public virtual void LoadEquipmentTemplateFromDatabase(string equipmentTemplateID)
        {
            EquipmentTemplateID = equipmentTemplateID;
            if (EquipmentTemplateID != null && EquipmentTemplateID.Length > 0)
            {
                GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                if (template.LoadFromDatabase(EquipmentTemplateID))
                {
                    m_inventory = template.CloseTemplate();
                }
                else
                {
                    //if (log.IsDebugEnabled)
                    //{
                    //    //log.Warn("Error loading NPC inventory: InventoryID="+EquipmentTemplateID+", NPC name="+Name+".");
                    //}
                }
                if (Inventory != null)
                {
                    //if the distance slot isnt empty we use that
                    //Seems to always
                    if (Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
                        SwitchWeapon(eActiveWeaponSlot.Distance);
                    else
                    {
                        InventoryItem twohand = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
                        InventoryItem onehand = Inventory.GetItem(eInventorySlot.RightHandWeapon);

                        if (twohand != null && onehand != null)
                            //Let's add some random chance
                            SwitchWeapon(Util.Chance(50) ? eActiveWeaponSlot.TwoHanded : eActiveWeaponSlot.Standard);
                        else if (twohand != null)
                            //Hmm our right hand weapon may have been null
                            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        else if (onehand != null)
                            //Hmm twohand was null lets default down here
                            SwitchWeapon(eActiveWeaponSlot.Standard);
                    }
                }
            }
        }

        private bool m_loadedFromScript = true;
        public bool LoadedFromScript
        {
            get { return m_loadedFromScript; }
            set { m_loadedFromScript = value; }
        }

        public virtual void RestoreOriginalGuildName() { }


        public bool IsInTerritory
        {
            get;
            set;
        }

        /// <summary>
        /// Load a npc from the npc template
        /// </summary>
        /// <param name="obj">template to load from</param>
        public override void LoadFromDatabase(DataObject obj)
        {
            if (obj == null) return;
            base.LoadFromDatabase(obj);
            if (!(obj is Mob)) return;
            m_loadedFromScript = false;
            Mob dbMob = (Mob)obj;
            NPCTemplate = NpcTemplateMgr.GetTemplate(dbMob.NPCTemplateID);

            TranslationId = dbMob.TranslationId;
            Name = dbMob.Name;
            Suffix = dbMob.Suffix;
            GuildName = dbMob.Guild;
            ExamineArticle = dbMob.ExamineArticle;
            MessageArticle = dbMob.MessageArticle;
            Position = new Vector3(dbMob.X, dbMob.Y, dbMob.Z);
            m_Heading = (ushort)(dbMob.Heading & 0xFFF);
            m_maxSpeedBase = (short)dbMob.Speed;
            m_currentSpeed = 0;
            m_tension = 0;
            CurrentRegionID = dbMob.Region;
            Realm = (eRealm)dbMob.Realm;
            Model = dbMob.Model;
            Size = dbMob.Size;
            Flags = (eFlags)dbMob.Flags;
            CanStealth = IsStealthed;
            m_packageID = dbMob.PackageID;
            IsRenaissance = dbMob.IsRenaissance;
            EventID = dbMob.EventID;

            ModelDb = dbMob.Model;
            RaceDb = dbMob.Race;
            VisibleWeaponsDb = dbMob.VisibleWeaponSlots;
            FlagsDb = dbMob.Flags;

            // Skip Level.set calling AutoSetStats() so it doesn't load the DB entry we already have
            m_level = dbMob.Level;
            AutoSetStats(dbMob);
            Level = dbMob.Level;

            MeleeDamageType = (eDamageType)dbMob.MeleeDamageType;
            if (MeleeDamageType == 0)
            {
                MeleeDamageType = eDamageType.Slash;
            }
            m_activeWeaponSlot = eActiveWeaponSlot.Standard;
            ActiveQuiverSlot = eActiveQuiverSlot.None;

            m_faction = FactionMgr.GetFactionByID(dbMob.FactionID);
            LoadEquipmentTemplateFromDatabase(dbMob.EquipmentTemplateID);

            if (dbMob.RespawnInterval == -1)
            {
                dbMob.RespawnInterval = 0;
            }
            m_respawnInterval = dbMob.RespawnInterval * 1000;

            m_pathID = dbMob.PathID;

            if (dbMob.Brain != "")
            {
                try
                {
                    ABrain brain = null;
                    foreach (Assembly asm in ScriptMgr.GameServerScripts)
                    {
                        brain = (ABrain)asm.CreateInstance(dbMob.Brain, false);
                        if (brain != null)
                            break;
                    }
                    if (brain != null)
                        SetOwnBrain(brain);
                }
                catch
                {
                    log.ErrorFormat("GameNPC error in LoadFromDatabase: can not instantiate brain of type {0} for npc {1}, name = {2}.", dbMob.Brain, dbMob.ClassType, dbMob.Name);
                }
            }

            IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;
            if (aggroBrain != null)
            {
                aggroBrain.AggroLevel = dbMob.AggroLevel;
                aggroBrain.AggroRange = dbMob.AggroRange;
                if (aggroBrain.AggroRange == Constants.USE_AUTOVALUES)
                {
                    if (Realm == eRealm.None)
                    {
                        aggroBrain.AggroRange = 400;
                        if (Name != Name.ToLower())
                        {
                            aggroBrain.AggroRange = 500;
                        }
                        if (CurrentRegion.IsDungeon)
                        {
                            aggroBrain.AggroRange = 300;
                        }
                    }
                    else
                    {
                        aggroBrain.AggroRange = 500;
                    }
                }
                if (aggroBrain.AggroLevel == Constants.USE_AUTOVALUES)
                {
                    aggroBrain.AggroLevel = 0;
                    if (Level > 5)
                    {
                        aggroBrain.AggroLevel = 30;
                    }
                    if (Name != Name.ToLower())
                    {
                        aggroBrain.AggroLevel = 30;
                    }
                    if (Realm != eRealm.None)
                    {
                        aggroBrain.AggroLevel = 60;
                    }
                }
            }

            m_race = (short)dbMob.Race;
            m_bodyType = (ushort)dbMob.BodyType;
            m_houseNumber = (ushort)dbMob.HouseNumber;
            m_maxdistance = dbMob.MaxDistance;
            m_roamingRange = dbMob.RoamingRange;
            m_isCloakHoodUp = dbMob.IsCloakHoodUp;
            m_visibleActiveWeaponSlots = dbMob.VisibleWeaponSlots;

            Gender = (eGender)dbMob.Gender;
            OwnerID = dbMob.OwnerID;

            LoadTemplate(NPCTemplate);
            /*
                if (Inventory != null)
                    SwitchWeapon(ActiveWeaponSlot);
            */
        }

        /// <summary>
        /// Deletes the mob from the database
        /// </summary>
        public override void DeleteFromDatabase()
        {
            if (Brain != null && Brain is IControlledBrain)
            {
                return;
            }

            if (InternalID != null)
            {
                Mob mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);
                if (mob != null)
                    GameServer.Database.DeleteObject(mob);
            }
        }

        /// <summary>
        /// Saves a mob into the db if it exists, it is
        /// updated, else it creates a new object in the DB
        /// </summary>
        public override void SaveIntoDatabase()
        {
            // do not allow saving in an instanced region
            if (CurrentRegion.IsInstance)
            {
                LoadedFromScript = true;
                return;
            }

            if (Brain != null && Brain is IControlledBrain)
            {
                // do not allow saving of controlled npc's
                return;
            }

            Mob mob = null;
            if (InternalID != null)
            {
                mob = GameServer.Database.FindObjectByKey<Mob>(InternalID);
            }

            if (mob == null)
            {
                if (LoadedFromScript == false)
                {
                    mob = new Mob();
                }
                else
                {
                    return;
                }
            }
            mob.TranslationId = TranslationId;
            mob.Name = Name;
            mob.Suffix = Suffix;
            mob.Guild = GuildName;
            mob.ExamineArticle = ExamineArticle;
            mob.MessageArticle = MessageArticle;
            mob.X = (int)Position.X;
            mob.Y = (int)Position.Y;
            mob.Z = (int)Position.Z;
            mob.Heading = Heading;
            mob.Speed = MaxSpeedBase;
            mob.Region = CurrentRegionID;
            mob.Realm = (byte)Realm;

            //If mob is part of GroupMob we need to save the changing properties from PropertyDb which contains original value from db or new values from Commands
            if (this.CurrentGroupMob == null)
            {
                mob.Model = Model;
                mob.Race = Race;
                mob.Flags = (uint)m_flags;
                mob.VisibleWeaponSlots = this.m_visibleActiveWeaponSlots;
            }
            else
            {
                mob.Model = this.ModelDb;
                mob.Flags = this.FlagsDb;
                mob.Race = this.RaceDb;
                mob.VisibleWeaponSlots = this.VisibleWeaponsDb;
            }

            mob.Size = Size;
            mob.Level = Level;
            mob.IsRenaissance = IsRenaissance;
            mob.EventID = EventID;

            // Stats
            mob.Constitution = Constitution;
            mob.Dexterity = Dexterity;
            mob.Strength = Strength;
            mob.Quickness = Quickness;
            mob.Intelligence = Intelligence;
            mob.Piety = Piety;
            mob.Empathy = Empathy;
            mob.Charisma = Charisma;

            mob.ClassType = this.GetType().ToString();
            mob.Speed = MaxSpeedBase;
            mob.RespawnInterval = m_respawnInterval / 1000;
            mob.HouseNumber = HouseNumber;
            mob.RoamingRange = RoamingRange;
            if (Brain.GetType().FullName != typeof(StandardMobBrain).FullName)
                mob.Brain = Brain.GetType().FullName;
            IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;
            if (aggroBrain != null)
            {
                mob.AggroLevel = aggroBrain.AggroLevel;
                mob.AggroRange = aggroBrain.AggroRange;
            }
            mob.EquipmentTemplateID = EquipmentTemplateID;

            if (m_faction != null)
                mob.FactionID = m_faction.ID;

            mob.MeleeDamageType = (int)MeleeDamageType;

            if (NPCTemplate != null)
            {
                mob.NPCTemplateID = NPCTemplate.TemplateId;
            }
            else
            {
                mob.NPCTemplateID = -1;
            }

            mob.BodyType = BodyType;
            mob.PathID = PathID;
            mob.MaxDistance = m_maxdistance;
            mob.IsCloakHoodUp = m_isCloakHoodUp;
            mob.Gender = (byte)Gender;
            mob.PackageID = PackageID;
            mob.OwnerID = OwnerID;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(mob);
                InternalID = mob.ObjectId;
            }
            else
            {
                GameServer.Database.SaveObject(mob);
            }
        }

        /// <summary>
        /// Load a NPC template onto this NPC
        /// </summary>
        /// <param name="template"></param>
        public virtual void LoadTemplate(INpcTemplate template)
        {
            if (template == null)
                return;

            // Save the template for later
            NPCTemplate = template as NpcTemplate;

            // These stats aren't found in the mob table, so always get them from the template
            this.TetherRange = template.TetherRange;
            this.ParryChance = template.ParryChance;
            this.EvadeChance = template.EvadeChance;
            this.BlockChance = template.BlockChance;
            this.LeftHandSwingChance = template.LeftHandSwingChance;
            this.MaxTension = template.MaxTension;

            // We need level set before assigning spells to scale pet spells
            if (template.ReplaceMobValues)
            {
                if (!Util.IsEmpty(template.Level))
                {
                    byte choosenLevel = 1;
                    var split = Util.SplitCSV(template.Level, true);
                    byte.TryParse(split[Util.Random(0, split.Count - 1)], out choosenLevel);
                    this.Level = choosenLevel; // Also calls AutosetStats()
                }
            }

            if (template.Spells != null) this.Spells = template.Spells;
            if (template.Styles != null) this.Styles = template.Styles;
            if (template.Abilities != null)
            {
                lock (m_lockAbilities)
                {
                    foreach (Ability ab in template.Abilities)
                        m_abilities[ab.KeyName] = ab;
                }
            }

            if (template.AdrenalineSpellID != 0)
            {
                if (template.MaxTension == 0)
                {
                    log.Warn($"NPCTemplate {template.TemplateId} has Adrenaline spell {template.AdrenalineSpellID} but MaxTension is 0, it will never be called");
                }
                Spell sp = SkillBase.GetSpellByID(template.AdrenalineSpellID);
                if (sp != null)
                {
                    AdrenalineSpell = sp;
                }
                else
                {
                    log.Error($"NPCTemplate {template.TemplateId} has unknown Adrenaline spell {template.AdrenalineSpellID} ");
                }
            }
            else if (template.MaxTension > 0) /* && data.AdrenalineSpellID == 0 */
            {
                AdrenalineSpell = this is MageMob ? SkillBase.GetSpellByID(AdrenalineSpellHandler.MAGE_ADRENALINE_SPELL_ID) : SkillBase.GetSpellByID(AdrenalineSpellHandler.TANK_ADRENALINE_SPELL_ID);
                if (AdrenalineSpell == null)
                {
                    log.Error($"Could not load default adrenaline spell for NPCTemplate {template.TemplateId}");
                }
            }

            // Everything below this point is already in the mob table
            if (!template.ReplaceMobValues && !LoadedFromScript)
                return;

            var m_templatedInventory = new List<string>();
            this.TranslationId = template.TranslationId;
            this.Name = template.Name;
            this.Suffix = template.Suffix;
            this.GuildName = template.GuildName;
            this.ExamineArticle = template.ExamineArticle;
            this.MessageArticle = template.MessageArticle;

            #region Models, Sizes, Levels, Gender
            // Dre: don't change an attribute if it's not set in the template

            // Grav: this.Model/Size/Level accessors are triggering SendUpdate()
            // so i must use them, and not directly use private variables
            if (!Util.IsEmpty(template.Model))
            {
                ushort choosenModel = 1;
                var splitModel = Util.SplitCSV(template.Model, true);
                ushort.TryParse(splitModel[Util.Random(0, splitModel.Count - 1)], out choosenModel);
                this.Model = choosenModel;
            }

            // Graveen: template.Gender is 0,1 or 2 for respectively eGender.Neutral("it"), eGender.Male ("he"), 
            // eGender.Female ("she"). Any other value is randomly choosing a gender for current GameNPC
            int choosenGender = template.Gender > 2 ? Util.Random(0, 2) : template.Gender;

            switch (choosenGender)
            {
                default:
                case 0: this.Gender = eGender.Neutral; break;
                case 1: this.Gender = eGender.Male; break;
                case 2: this.Gender = eGender.Female; break;
            }

            if (!Util.IsEmpty(template.Size))
            {
                byte choosenSize = 50;
                var split = Util.SplitCSV(template.Size, true);
                byte.TryParse(split[Util.Random(0, split.Count - 1)], out choosenSize);
                this.Size = choosenSize;
            }
            #endregion

            #region Misc Stats
            this.MaxDistance = template.MaxDistance;
            this.Race = (short)template.Race;
            this.BodyType = (ushort)template.BodyType;
            this.MaxSpeedBase = template.MaxSpeed;
            this.Flags = (eFlags)template.Flags;
            CanStealth = IsStealthed;
            this.MeleeDamageType = template.MeleeDamageType;
            #endregion

            #region Inventory
            //Ok lets start loading the npc equipment - only if there is a value!
            if (!Util.IsEmpty(template.Inventory))
            {
                bool equipHasItems = false;
                GameNpcInventoryTemplate equip = new GameNpcInventoryTemplate();
                //First let's try to reach the npcequipment table and load that!
                //We use a ';' split to allow npctemplates to support more than one equipmentIDs
                var equipIDs = Util.SplitCSV(template.Inventory);
                if (!template.Inventory.Contains(":"))
                {

                    foreach (string str in equipIDs)
                    {
                        m_templatedInventory.Add(str);
                    }

                    string equipid = "";

                    if (m_templatedInventory.Count > 0)
                    {
                        if (m_templatedInventory.Count == 1)
                            equipid = template.Inventory;
                        else
                            equipid = m_templatedInventory[Util.Random(m_templatedInventory.Count - 1)];
                    }
                    if (equip.LoadFromDatabase(equipid))
                        equipHasItems = true;
                }

                #region Legacy Equipment Code
                //Nope, nothing in the npcequipment table, lets do the crappy parsing
                //This is legacy code
                if (!equipHasItems && template.Inventory.Contains(":"))
                {
                    //Temp list to store our models
                    List<int> tempModels = new List<int>();

                    //Let's go through all of our ';' seperated slots
                    foreach (string str in equipIDs)
                    {
                        tempModels.Clear();
                        //Split the equipment into slot and model(s)
                        string[] slotXModels = str.Split(':');
                        //It should only be two in length SLOT : MODELS
                        if (slotXModels.Length == 2)
                        {
                            int slot;
                            //Let's try to get our slot
                            if (Int32.TryParse(slotXModels[0], out slot))
                            {
                                //Now lets go through and add all the models to the list
                                string[] models = slotXModels[1].Split('|');
                                foreach (string strModel in models)
                                {
                                    //We'll add it to the list if we successfully parse it!
                                    int model;
                                    if (Int32.TryParse(strModel, out model))
                                        tempModels.Add(model);
                                }

                                //If we found some models let's randomly pick one and add it the equipment
                                if (tempModels.Count > 0)
                                    equipHasItems |= equip.AddNPCEquipment((eInventorySlot)slot, tempModels[Util.Random(tempModels.Count - 1)]);
                            }
                        }
                    }
                }
                #endregion

                //We added some items - let's make it the new inventory
                if (equipHasItems)
                {
                    this.Inventory = new GameNPCInventory(equip);
                    if (this.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
                        this.SwitchWeapon(eActiveWeaponSlot.Distance);
                }

                if (template.VisibleActiveWeaponSlot > 0)
                    this.VisibleActiveWeaponSlots = template.VisibleActiveWeaponSlot;
            }
            #endregion

            // Dre: don't change the brain if it's already a StandardMobBrain
            if (Brain is StandardMobBrain brain)
            {
                brain.AggroLevel = template.AggroLevel;
                brain.AggroRange = template.AggroRange;
            }
            else
            {
                m_ownBrain = new StandardMobBrain
                {
                    Body = this,
                    AggroLevel = template.AggroLevel,
                    AggroRange = template.AggroRange
                };
            }

            if (template.Spells != null) Spells = template.Spells;
            if (template.Styles != null) Styles = template.Styles;
            if (template.Abilities != null)
            {
                lock (m_lockAbilities)
                {
                    foreach (Ability ab in template.Abilities)
                        m_abilities[ab.KeyName] = ab;
                }
            }
        }

        /// <summary>
        /// Switches the active weapon to another one
        /// </summary>
        /// <param name="slot">the new eActiveWeaponSlot</param>
        public override void SwitchWeapon(eActiveWeaponSlot slot)
        {
            base.SwitchWeapon(slot);
            if (ObjectState == eObjectState.Active)
            {
                // Update active weapon appearence
                BroadcastLivingEquipmentUpdate();
            }
        }
        /// <summary>
        /// Equipment templateID
        /// </summary>
        protected string m_equipmentTemplateID;
        /// <summary>
        /// The equipment template id of this npc
        /// </summary>
        public string EquipmentTemplateID
        {
            get { return m_equipmentTemplateID; }
            set { m_equipmentTemplateID = value; }
        }

        #endregion

        #region Quest
        /// <summary>
        /// Holds all the quests this npc can give to players
        /// </summary>
        protected readonly List<ushort> m_questIdListToGive = new();

        /// <summary>
        /// Gets the questlist of this player
        /// </summary>
        public IReadOnlyList<ushort> QuestIdListToGive
        {
            get
            {
                lock (m_questIdListToGive)
                    return m_questIdListToGive.ToList();
            }
        }

        /// <summary>
        /// Adds a scripted quest type to the npc questlist
        /// </summary>
        /// <param name="quest">The quest type to add</param>
        /// <returns>true if added, false if the npc has already the quest!</returns>
        public void AddQuestToGive(DataQuestJson quest)
        {
            lock (m_questIdListToGive)
                if (!m_questIdListToGive.Contains(quest.Id))
                    m_questIdListToGive.Add(quest.Id);
        }

        /// <summary>
        /// removes a scripted quest from this npc
        /// </summary>
        /// <param name="quest">The questType to remove</param>
        /// <returns>true if added, false if the npc has already the quest!</returns>
        public bool RemoveQuestToGive(DataQuestJson quest)
        {
            lock (m_questIdListToGive)
                return m_questIdListToGive.Remove(quest.Id);
        }

        /// <summary>
        /// Check if the npc can give the specified quest to a player
        /// Used for scripted quests
        /// </summary>
        /// <param name="quest">The type of the quest</param>
        /// <param name="player">The player who search a quest</param>
        public bool CanGiveQuest(DataQuestJson quest, GamePlayer player)
        {
            if (!quest.CheckQuestQualification(player))
                return false;
            if (player.HasFinishedQuest(quest) >= quest.MaxCount)
                return false;
            return true;
        }

        protected GameNPC m_teleporterIndicator = null;

        /// <summary>
        /// Should this NPC have an associated teleporter indicator
        /// </summary>
        public virtual bool ShowTeleporterIndicator
        {
            get { return false; }
        }

        /// <summary>
        /// Should the NPC show a quest indicator, this can be overriden for custom handling
        /// Checks both scripted and data quests
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if the NPC should show quest indicator, false otherwise</returns>
        public virtual eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            // Available one?
            if (CanShowOneQuest(player))
                return eQuestIndicator.Available;

            // Finishing one?
            if (CanFinishOneQuest(player))
                return eQuestIndicator.Finish;

            // Interact one?
            if (CanInteractOneQuest(player))
                return eQuestIndicator.Pending;

            return eQuestIndicator.None;
        }

        /// <summary>
        /// Check if the npc can show a quest iteract goal indicator to a player
        /// Checks both scripted and data quests
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if yes, false if the npc can progress any quest by interaction</returns>
        public bool CanInteractOneQuest(GamePlayer player)
        {
                foreach (var quest in player.QuestList)
                {
                    foreach (var goal in quest.Quest.Goals.Values.Where(g => g.hasInteraction && g.Target == this))
                        if (goal.IsActive(quest))
                            return true;
                }

            return false;
        }
        /// <summary>
        /// Check if the npc can show a quest indicator to a player
        /// Checks both scripted and data quests
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if yes, false if the npc can give any quest</returns>
        public bool CanShowOneQuest(GamePlayer player)
        {
            foreach (var id in QuestIdListToGive)
            {
                var quest = DataQuestJsonMgr.GetQuest(id);
                if (quest == null)
                    continue;

                var doingQuest = (player.IsDoingQuest(quest) != null ? 1 : 0);
                if (quest.CheckQuestQualification(player) && player.HasFinishedQuest(quest) + doingQuest < quest.MaxCount)
                    return true;
            }
            return false;
        }

        public bool CanFinishOneQuest(GamePlayer player)
        {
                foreach (var id in QuestIdListToGive)
                {
                    var quest = DataQuestJsonMgr.GetQuest(id);
                    if (quest == null)
                        continue;

                    var pq = player.IsDoingQuest(quest);
                    if (pq != null && pq.CanFinish())
                        return true;
                }

            return false;
        }

        /// <summary>
        /// Checks if this npc already has a specified quest
        /// used for scripted quests
        /// </summary>
        /// <param name="questType">The quest type</param>
        /// <returns>the quest if the npc have the quest or null if not</returns>
        protected bool HasQuest(DataQuestJson quest)
        {
            lock (m_questIdListToGive)
                return m_questIdListToGive.Contains(quest.Id);
        }

        #endregion

        #region Riding
        //NPC's can have riders :-)
        /// <summary>
        /// Holds the rider of this NPC as weak reference
        /// </summary>
        public GamePlayer[] Riders;

        /// <summary>
        /// This function is called when a rider mounts this npc
        /// Since only players can ride NPC's you should use the
        /// GamePlayer.MountSteed function instead to make sure all
        /// callbacks are called correctly
        /// </summary>
        /// <param name="rider">GamePlayer that is the rider</param>
        /// <param name="forced">if true, mounting can't be prevented by handlers</param>
        /// <returns>true if mounted successfully</returns>
        public virtual bool RiderMount(GamePlayer rider, bool forced)
        {
            int exists = RiderArrayLocation(rider);
            if (exists != -1)
                return false;

            rider.MoveTo(CurrentRegionID, Position, Heading);

            Notify(GameNPCEvent.RiderMount, this, new RiderMountEventArgs(rider, this));
            int slot = GetFreeArrayLocation();
            Riders[slot] = rider;
            rider.Steed = this;
            return true;
        }

        /// <summary>
        /// This function is called when a rider mounts this npc
        /// Since only players can ride NPC's you should use the
        /// GamePlayer.MountSteed function instead to make sure all
        /// callbacks are called correctly
        /// </summary>
        /// <param name="rider">GamePlayer that is the rider</param>
        /// <param name="forced">if true, mounting can't be prevented by handlers</param>
        /// <param name="slot">The desired slot to mount</param>
        /// <returns>true if mounted successfully</returns>
        public virtual bool RiderMount(GamePlayer rider, bool forced, int slot)
        {
            int exists = RiderArrayLocation(rider);
            if (exists != -1)
                return false;

            if (Riders[slot] != null)
                return false;

            //rider.MoveTo(CurrentRegionID, X, Y, Z, Heading);

            Notify(GameNPCEvent.RiderMount, this, new RiderMountEventArgs(rider, this));
            Riders[slot] = rider;
            rider.Steed = this;
            return true;
        }

        /// <summary>
        /// Called to dismount a rider from this npc.
        /// Since only players can ride NPC's you should use the
        /// GamePlayer.MountSteed function instead to make sure all
        /// callbacks are called correctly
        /// </summary>
        /// <param name="forced">if true, the dismounting can't be prevented by handlers</param>
        /// <param name="player">the player that is dismounting</param>
        /// <returns>true if dismounted successfully</returns>
        public virtual bool RiderDismount(bool forced, GamePlayer player)
        {
            if (Riders.Length <= 0)
                return false;

            int slot = RiderArrayLocation(player);
            if (slot < 0)
            {
                return false;
            }
            Riders[slot] = null;

            Notify(GameNPCEvent.RiderDismount, this, new RiderDismountEventArgs(player, this));
            player.Steed = null;

            return true;
        }

        /// <summary>
        /// Get a free array location on the NPC
        /// </summary>
        /// <returns></returns>
        public int GetFreeArrayLocation()
        {
            for (int i = 0; i < MAX_PASSENGERS; i++)
            {
                if (Riders[i] == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Get the riders array location
        /// </summary>
        /// <param name="player">the player to get location of</param>
        /// <returns></returns>
        public int RiderArrayLocation(GamePlayer player)
        {
            for (int i = 0; i < MAX_PASSENGERS; i++)
            {
                if (Riders[i] == player)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Get the riders slot on the npc
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public int RiderSlot(GamePlayer player)
        {
            int location = RiderArrayLocation(player);
            if (location == -1)
                return location;
            return location + SLOT_OFFSET;
        }

        /// <summary>
        /// The maximum passengers the NPC can take
        /// </summary>
        public virtual int MAX_PASSENGERS
        {
            get { return 1; }
        }

        /// <summary>
        /// The minimum number of passengers required to move
        /// </summary>
        public virtual int REQUIRED_PASSENGERS
        {
            get { return 1; }
        }

        /// <summary>
        /// The slot offset for this NPC
        /// </summary>
        public virtual int SLOT_OFFSET
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets a list of the current riders
        /// </summary>
        public GamePlayer[] CurrentRiders
        {
            get
            {
                List<GamePlayer> list = new List<GamePlayer>(MAX_PASSENGERS);
                for (int i = 0; i < MAX_PASSENGERS; i++)
                {
                    if (Riders == null || i >= Riders.Length)
                        break;

                    GamePlayer player = Riders[i];
                    if (player != null)
                        list.Add(player);
                }
                return list.ToArray();
            }
        }
        #endregion

        #region Add/Remove/Create/Remove/Update

        /// <summary>
        /// Broadcasts the NPC Update to all players around
        /// </summary>
        public override void BroadcastUpdate()
        {
            base.BroadcastUpdate();

            m_lastUpdateTickCount = GameTimer.GetTickCount();
        }

        /// <summary>
        /// callback that npc was updated to the world
        /// so it must be visible to at least one player
        /// </summary>
        public void NPCUpdatedCallback()
        {
            m_lastVisibleToPlayerTick = GameTimer.GetTickCount();
            lock (BrainSync)
            {
                ABrain brain = Brain;
                if (brain != null)
                    brain.Start();
            }
        }
        /// <summary>
        /// Adds the npc to the world
        /// </summary>
        /// <returns>true if the npc has been successfully added</returns>
        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;

            if (MAX_PASSENGERS > 0)
                Riders = new GamePlayer[MAX_PASSENGERS];

            if (temporallyBrain != null)
            {
                RemoveBrain(temporallyBrain);
                temporallyBrain = null;
            }
            tempoarallyFlags = 0;
            if (temporallyTemplate != null)
            {
                LoadFromDatabase(GameServer.Database.FindObjectByKey<Mob>(InternalID));
            }
            if (hasImunity)
            {
                ImunityDomage = eDamageType.GM;
                DamageTypeCounter = 0;
                LastDamageType = eDamageType.GM;
                hasImunity = false;
            }

            bool anyPlayer = false;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null) continue;
                player.Out.SendNPCCreate(this);
                if (m_inventory != null)
                    player.Out.SendLivingEquipmentUpdate(this);

                // If any player was initialized, update last visible tick to enable brain
                anyPlayer = true;
            }

            if (anyPlayer)
                m_lastVisibleToPlayerTick = GameTimer.GetTickCount();

            m_spawnPoint = Position;
            m_spawnHeading = Heading;
            lock (BrainSync)
            {
                ABrain brain = Brain;
                if (brain != null)
                    brain.Start();
            }

            if (Mana <= 0 && MaxMana > 0)
                Mana = MaxMana;
            else if (Mana > 0 && MaxMana > 0)
                StartPowerRegeneration();
            StartEnduranceRegeneration();

            //If the Mob has a Path assigned he will now walk on it!
            if (MaxSpeedBase > 0 && CurrentSpellHandler == null && !IsMoving
                && !AttackState && !InCombat && !IsMovingOnPath && !IsReturningHome
                //Check everything otherwise the Server will crash
                && PathID != null && PathID != "" && PathID != "NULL")
            {
                PathPoint path = MovementMgr.LoadPath(PathID);
                if (path != null)
                {
                    var p = path.GetNearestNextPoint(Position);
                    CurrentWayPoint = p;
                    MoveOnPath((short)p.MaxSpeed);
                }
            }

            if (m_houseNumber > 0 && !(this is GameConsignmentMerchant))
            {
                log.Info("NPC '" + Name + "' added to house " + m_houseNumber);
                CurrentHouse = HouseMgr.GetHouse(m_houseNumber);
                if (CurrentHouse == null)
                    log.Warn("House " + CurrentHouse + " for NPC " + Name + " doesn't exist !!!");
                else
                    log.Info("Confirmed number: " + CurrentHouse.HouseNumber.ToString());
            }

            // [Ganrod] Nidel: spawn full life
            if (!InCombat && IsAlive && base.Health < MaxHealth)
            {
                base.Health = MaxHealth;
            }

            // create the ambiant text list for this NPC
            BuildAmbientTexts();
            if (GameServer.Instance.ServerStatus == eGameServerStatus.GSS_Open)
                FireAmbientSentence(eAmbientTrigger.spawning);


            if (ShowTeleporterIndicator)
            {
                if (m_teleporterIndicator == null)
                {
                    m_teleporterIndicator = new GameNPC();
                    m_teleporterIndicator.Name = "";
                    m_teleporterIndicator.Model = 1923;
                    m_teleporterIndicator.Flags ^= eFlags.PEACE;
                    m_teleporterIndicator.Flags ^= eFlags.CANTTARGET;
                    m_teleporterIndicator.Flags ^= eFlags.DONTSHOWNAME;
                    m_teleporterIndicator.Flags ^= eFlags.FLYING;
                    m_teleporterIndicator.Position = Position + Vector3.UnitZ;
                    m_teleporterIndicator.CurrentRegionID = CurrentRegionID;
                }

                m_teleporterIndicator.AddToWorld();
            }

            //On Groupmob repop handle slave status
            if (this.CurrentGroupMob?.SlaveGroupId != null)
            {
                if (MobGroupManager.Instance.Groups.ContainsKey(this.CurrentGroupMob.SlaveGroupId))
                {
                    MobGroupManager.Instance.Groups[this.CurrentGroupMob.SlaveGroupId].ResetGroupInfo();
                }
            }

            var territory = TerritoryManager.Instance.GetCurrentTerritory(this.CurrentAreas);
            if (territory?.GuildOwner != null)
            {
                this.GuildName = territory.GuildOwner;
            }

            return true;
        }

        public virtual bool Spawn()
        {
            int dummy;
            CurrentRegion.MobsRespawning.TryRemove(this, out dummy);

            lock (m_respawnTimerLock)
            {
                if (m_respawnTimer != null)
                {
                    m_respawnTimer.Stop();
                    m_respawnTimer = null;
                }
            }

            if (IsAlive || ObjectState == eObjectState.Active) return false;

            Health = MaxHealth;
            Mana = MaxMana;
            Endurance = MaxEndurance;
            Position = m_spawnPoint;
            Heading = m_spawnHeading;
            ambientXNbUse = new Dictionary<MobXAmbientBehaviour, short>();

            return AddToWorld();
        }

        /// <summary>
        /// Fill the ambient text list for this NPC
        /// </summary>
        public virtual void BuildAmbientTexts()
        {
            // list of ambient texts
            if (!string.IsNullOrEmpty(Name))
                ambientTexts = GameServer.Instance.NpcManager.AmbientBehaviour[Name];
        }

        /// <summary>
        /// Removes the npc from the world
        /// </summary>
        /// <returns>true if the npc has been successfully removed</returns>
        public override bool RemoveFromWorld()
        {
            if (IsMovingOnPath)
                StopMovingOnPath();
            if (MAX_PASSENGERS > 0)
            {
                foreach (GamePlayer player in CurrentRiders)
                {
                    player.DismountSteed(true);
                }
            }

            if (ObjectState == eObjectState.Active)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendObjectRemove(this);
            }
            if (!base.RemoveFromWorld()) return false;

            lock (BrainSync)
            {
                ABrain brain = Brain;
                brain.Stop();
            }
            EffectList.CancelAll();

            if (ShowTeleporterIndicator && m_teleporterIndicator != null)
            {
                m_teleporterIndicator.RemoveFromWorld();
                m_teleporterIndicator = null;
            }

            return true;
        }

        /// <summary>
        /// Move an NPC within the same region without removing from world
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="heading"></param>
        /// <param name="forceMove">Move regardless of combat check</param>
        /// <returns>true if npc was moved</returns>
        public virtual bool MoveInRegion(ushort regionID, float x, float y, float z, ushort heading, bool forceMove)
        {
            if (m_ObjectState != eObjectState.Active)
                return false;

            // pets can't be moved across regions
            if (regionID != CurrentRegionID)
                return false;

            if (forceMove == false)
            {
                // do not move a pet in combat, player can passive / follow to bring pet to them
                if (InCombat)
                    return false;

                ControlledNpcBrain controlledBrain = Brain as ControlledNpcBrain;

                // only move pet if it's following the owner
                if (controlledBrain != null && controlledBrain.WalkState != eWalkState.Follow)
                    return false;
            }

            Region rgn = WorldMgr.GetRegion(regionID);

            if (rgn == null || rgn.GetZone(x, y) == null)
                return false;

            // For a pet move simple erase the pet from all clients and redraw in the new location

            Notify(GameObjectEvent.MoveTo, this, new MoveToEventArgs(regionID, x, y, z, heading));

            if (ObjectState == eObjectState.Active)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendObjectRemove(this);
                }
            }

            Position = new Vector3(x, y, z);
            m_Heading = heading;

            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null) continue;

                player.Out.SendNPCCreate(this);

                if (m_inventory != null)
                {
                    player.Out.SendLivingEquipmentUpdate(this);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets or Sets the current Region of the Object
        /// </summary>
        public override Region CurrentRegion
        {
            get { return base.CurrentRegion; }
            set
            {
                Region oldRegion = CurrentRegion;
                base.CurrentRegion = value;
                Region newRegion = CurrentRegion;
                if (oldRegion != newRegion && newRegion != null)
                {
                    if (m_followTimer != null) m_followTimer.Stop();
                    m_followTimer = new RegionTimer(this);
                    m_followTimer.Callback = new RegionTimerCallback(FollowTimerCallback);
                }
            }
        }

        /// <summary>
        /// Marks this object as deleted!
        /// </summary>
        public override void Delete()
        {
            lock (m_respawnTimerLock)
            {
                if (m_respawnTimer != null)
                {
                    m_respawnTimer.Stop();
                    m_respawnTimer = null;
                }
            }
            lock (BrainSync)
            {
                ABrain brain = Brain;
                brain.Stop();
            }
            StopFollowing();
            TempProperties.removeProperty(CHARMED_TICK_PROP);
            base.Delete();
        }

        #endregion

        #region AI

        /// <summary>
        /// Holds the own NPC brain
        /// </summary>
        protected ABrain m_ownBrain;

        /// <summary>
        /// Holds the all added to this npc brains
        /// </summary>
        private ArrayList m_brains = new ArrayList(1);

        /// <summary>
        /// The sync object for brain changes
        /// </summary>
        private readonly object m_brainSync = new object();

        /// <summary>
        /// Gets the brain sync object
        /// </summary>
        public object BrainSync
        {
            get { return m_brainSync; }
        }

        /// <summary>
        /// Gets the current brain of this NPC
        /// </summary>
        public ABrain Brain
        {
            get
            {
                ArrayList brains = m_brains;
                if (brains.Count > 0)
                    return (ABrain)brains[brains.Count - 1];
                return m_ownBrain;
            }
        }

        /// <summary>
        /// Sets the NPC own brain
        /// </summary>
        /// <param name="brain">The new brain</param>
        /// <returns>The old own brain</returns>
        public virtual ABrain SetOwnBrain(ABrain brain)
        {
            if (brain == null)
                return null;
            if (brain.IsActive)
                throw new ArgumentException("The new brain is already active.", "brain");

            lock (BrainSync)
            {
                ABrain oldBrain = m_ownBrain;
                bool activate = oldBrain.IsActive;
                if (activate)
                    oldBrain.Stop();
                m_ownBrain = brain;
                m_ownBrain.Body = this;
                if (activate)
                    m_ownBrain.Start();

                return oldBrain;
            }
        }

        /// <summary>
        /// Adds a temporary brain to Npc, last added brain is active
        /// </summary>
        /// <param name="newBrain"></param>
        public virtual void AddBrain(ABrain newBrain)
        {
            if (newBrain == null)
                throw new ArgumentNullException("newBrain");
            if (newBrain.IsActive)
                throw new ArgumentException("The new brain is already active.", "newBrain");

            lock (BrainSync)
            {
                Brain.Stop();
                ArrayList brains = new ArrayList(m_brains);
                brains.Add(newBrain);
                m_brains = brains; // make new array list to avoid locks in the Brain property
                newBrain.Body = this;
                newBrain.Start();
            }
        }

        /// <summary>
        /// Removes a temporary brain from Npc
        /// </summary>
        /// <param name="removeBrain">The brain to remove</param>
        /// <returns>True if brain was found</returns>
        public virtual bool RemoveBrain(ABrain removeBrain)
        {
            if (removeBrain == null) return false;

            lock (BrainSync)
            {
                ArrayList brains = new ArrayList(m_brains);
                int index = brains.IndexOf(removeBrain);
                if (index < 0) return false;
                bool active = brains[index] == Brain;
                if (active)
                    removeBrain.Stop();
                brains.RemoveAt(index);
                m_brains = brains;
                if (active)
                    Brain.Start();

                return true;
            }
        }
        #endregion

        #region GetAggroLevelString

        /// <summary>
        /// How friendly this NPC is to player
        /// </summary>
        /// <param name="player">GamePlayer that is examining this object</param>
        /// <param name="firstLetterUppercase"></param>
        /// <returns>aggro state as string</returns>
        public virtual string GetAggroLevelString(GamePlayer player, bool firstLetterUppercase)
        {
            // "aggressive", "hostile", "neutral", "friendly"
            // TODO: correct aggro strings
            // TODO: some merchants can be aggressive to players even in same realm
            // TODO: findout if trainers can be aggro at all

            //int aggro = CalculateAggroLevelToTarget(player);

            // "aggressive towards you!", "hostile towards you.", "neutral towards you.", "friendly."
            // TODO: correct aggro strings
            string aggroLevelString = "";
            int aggroLevel;
            if (Faction != null)
            {
                aggroLevel = Faction.GetAggroToFaction(player);
                if (aggroLevel > 75)
                    aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive1");
                else if (aggroLevel > 50)
                    aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Hostile1");
                else if (aggroLevel > 25)
                    aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral1");
                else
                    aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
            }
            else
            {
                IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;
                if (GameServer.ServerRules.IsSameRealm(this, player, true))
                {
                    if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly2");
                    else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Friendly1");
                }
                else if (aggroBrain != null && aggroBrain.AggroLevel > 0)
                {
                    if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive2");
                    else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Aggressive1");
                }
                else
                {
                    if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral2");
                    else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.Neutral1");
                }
            }
            return LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetAggroLevelString.TowardsYou", aggroLevelString);
        }

        public string GetPronoun(int form, bool capitalize, string lang)
        {
            switch (Gender)
            {
                case eGender.Male:
                    switch (form)
                    {
                        case 1:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Male.Possessive"));
                        case 2:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Male.Objective"));
                        default:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Male.Subjective"));
                    }

                case eGender.Female:
                    switch (form)
                    {
                        case 1:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Female.Possessive"));
                        case 2:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Female.Objective"));
                        default:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Female.Subjective"));
                    }
                default:
                    switch (form)
                    {
                        case 1:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Neutral.Possessive"));
                        case 2:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Neutral.Objective"));
                        default:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(lang, "GameLiving.Pronoun.Neutral.Subjective"));
                    }
            }
        }

        /// <summary>
        /// Gets the proper pronoun including capitalization.
        /// </summary>
        /// <param name="form">1=his; 2=him; 3=he</param>
        /// <param name="capitalize"></param>
        /// <returns></returns>
        public override string GetPronoun(int form, bool capitalize)
        {
            String language = ServerProperties.Properties.DB_LANGUAGE;

            switch (Gender)
            {
                case eGender.Male:
                    switch (form)
                    {
                        case 1:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Male.Possessive"));
                        case 2:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Male.Objective"));
                        default:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Male.Subjective"));
                    }

                case eGender.Female:
                    switch (form)
                    {
                        case 1:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Female.Possessive"));
                        case 2:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Female.Objective"));
                        default:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Female.Subjective"));
                    }
                default:
                    switch (form)
                    {
                        case 1:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Neutral.Possessive"));
                        case 2:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Neutral.Objective"));
                        default:
                            return Capitalize(capitalize, LanguageMgr.GetTranslation(language,
                                                                                     "GameLiving.Pronoun.Neutral.Subjective"));
                    }
            }
        }

        /// <summary>
        /// Adds messages to ArrayList which are sent when object is targeted
        /// </summary>
        /// <param name="player">GamePlayer that is examining this object</param>
        /// <returns>list with string messages</returns>
        public override IList GetExamineMessages(GamePlayer player)
        {
            switch (player.Client.Account.Language)
            {
                case "EN":
                    {
                        IList list = base.GetExamineMessages(player);
                        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetExamineMessages.YouExamine",
                                                            GetName(0, false), GetPronoun(0, true), GetAggroLevelString(player, false)));
                        return list;
                    }
                default:
                    {
                        IList list = new ArrayList(4);
                        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.GetExamineMessages.YouTarget",
                                                            player.GetPersonalizedName(this)));
                        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.GetExamineMessages.YouExamine",
                                                            GetName(0, false, player.Client.Account.Language, this),
                                                            GetPronoun(0, true, player.Client.Account.Language), GetAggroLevelString(player, false)));
                        return list;
                    }
            }
        }

        /*		/// <summary>
                /// Pronoun of this NPC in case you need to refer it in 3rd person
                /// http://webster.commnet.edu/grammar/cases.htm
                /// </summary>
                /// <param name="firstLetterUppercase"></param>
                /// <param name="form">0=Subjective, 1=Possessive, 2=Objective</param>
                /// <returns>pronoun of this object</returns>
                public override string GetPronoun(bool firstLetterUppercase, int form)
                {
                    // TODO: when mobs will get gender
                    if(PlayerCharacter.Gender == 0)
                        // male
                        switch(form)
                        {
                            default: // Subjective
                                if(firstLetterUppercase) return "He"; else return "he";
                            case 1:	// Possessive
                                if(firstLetterUppercase) return "His"; else return "his";
                            case 2:	// Objective
                                if(firstLetterUppercase) return "Him"; else return "him";
                        }
                    else
                        // female
                        switch(form)
                        {
                            default: // Subjective
                                if(firstLetterUppercase) return "She"; else return "she";
                            case 1:	// Possessive
                                if(firstLetterUppercase) return "Her"; else return "her";
                            case 2:	// Objective
                                if(firstLetterUppercase) return "Her"; else return "her";
                        }

                    // it
                    switch(form)
                    {
                        // Subjective
                        default: if(firstLetterUppercase) return "It"; else return "it";
                        // Possessive
                        case 1:	if(firstLetterUppercase) return "Its"; else return "its";
                        // Objective
                        case 2: if(firstLetterUppercase) return "It"; else return "it";
                    }
                }*/
        #endregion

        #region Interact/WhisperReceive/SayTo

        /// <summary>
        /// The possible triggers for GameNPC ambient actions
        /// </summary>
        public enum eAmbientTrigger
        {
            spawning,
            dieing,
            aggroing,
            fighting,
            roaming,
            killing,
            moving,
            interact,
            seeing,
            hurting,
            immunised,
        }

        /// <summary>
        /// The ambient texts
        /// </summary>
        public IList<MobXAmbientBehaviour> ambientTexts;

        /// <summary>
        /// This function is called from the ObjectInteractRequestHandler
        /// </summary>
        /// <param name="player">GamePlayer that interacts with this object</param>
        /// <returns>false if interaction is prevented</returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            if (!GameServer.ServerRules.IsSameRealm(this, player, true) && (Faction?.GetAggroToFaction(player) ?? 0) > 25)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.DirtyLook",
                    GetName(0, true, player.Client.Account.Language, this)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
                return false;
            }

            //NPC Renaissance don't talk to not renaissance players
            if (this.IsRenaissance && !player.IsRenaissance)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.NotRenaissance"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (MAX_PASSENGERS > 1)
            {
                string name = "";
                if (this is GameTaxiBoat)
                    name = "boat";
                if (this is GameSiegeRam)
                    name = "ram";

                if (RiderSlot(player) != -1)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.AlreadyRiding", name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (GetFreeArrayLocation() == -1)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Interact.IsFull", name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (player.IsRiding)
                {
                    player.DismountSteed(true);
                }

                if (player.IsOnHorse)
                {
                    player.IsOnHorse = false;
                }

                player.MountSteed(this, true);
            }

            FireAmbientSentence(eAmbientTrigger.interact, player);
            return true;
        }

        /// <summary>
        /// ToDo
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;
            if (source is GamePlayer == false)
                return true;

            GamePlayer player = (GamePlayer)source;

            //TODO: Guards in rvr areas doesn't need check
            if (text == "task")
            {
                if (source.TargetObject == null)
                    return false;
                if (KillTask.CheckAvailability(player, (GameLiving)source.TargetObject))
                {
                    KillTask.BuildTask(player, (GameLiving)source.TargetObject);
                    return true;
                }
                else if (MoneyTask.CheckAvailability(player, (GameLiving)source.TargetObject))
                {
                    MoneyTask.BuildTask(player, (GameLiving)source.TargetObject);
                    return true;
                }
                else if (CraftTask.CheckAvailability(player, (GameLiving)source.TargetObject))
                {
                    CraftTask.BuildTask(player, (GameLiving)source.TargetObject);
                    return true;
                }
            }
            return true;
        }

        /// <summary>
        /// Format "say" message and send it to target in popup window
        /// </summary>
        /// <param name="target"></param>
        /// <param name="message"></param>
        public virtual void SayTo(GamePlayer target, string message, bool announce = true)
        {
            SayTo(target, eChatLoc.CL_PopupWindow, message, announce);
        }

        /// <summary>
        /// Format "say" message and send it to target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="loc">chat location of the message</param>
        /// <param name="message"></param>
        public virtual void SayTo(GamePlayer target, eChatLoc loc, string message, bool announce = true)
        {
            if (target == null)
                return;

            TurnTo(target);
            string resultText = LanguageMgr.GetTranslation(target.Client.Account.Language, "GameNPC.SayTo.Says", GetName(0, true, target.Client.Account.Language, this), message);
            switch (loc)
            {
                case eChatLoc.CL_PopupWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    if (announce)
                    {
                        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
                        {
                            if (!(target == player))
                            {
                                player.MessageFromArea(this, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.SayTo.SpeaksTo",
                                player.GetPersonalizedName(this), player.GetPersonalizedName(target)
                                ), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                            }
                        }
                    }
                    break;
                case eChatLoc.CL_ChatWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                    break;
                case eChatLoc.CL_SystemWindow:
                    target.Out.SendMessage(resultText, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
            }
        }
        #endregion

        #region Combat

        /// <summary>
        /// The property that holds charmed tick if any
        /// </summary>
        public const string CHARMED_TICK_PROP = "CharmedTick";

        /// <summary>
        /// The duration of no exp after charmed, in game ticks
        /// </summary>
        public const int CHARMED_NOEXP_TIMEOUT = 60000;

        public const string LAST_LOS_TARGET_PROPERTY = "last_LOS_checkTarget";
        public const string LAST_LOS_TICK_PROPERTY = "last_LOS_checkTick";
        public const string NUM_LOS_CHECKS_INPROGRESS = "num_LOS_progress";

        protected object LOS_LOCK = new object();

        protected GameObject m_targetLOSObject = null;

        /// <summary>
        /// Starts a melee attack on a target
        /// </summary>
        /// <param name="target">The object to attack</param>
        public override void StartAttack(GameObject target)
        {
            if (target == null)
                return;

            TargetObject = target;

            long lastTick = this.TempProperties.getProperty<long>(LAST_LOS_TICK_PROPERTY);

            if (ServerProperties.Properties.ALWAYS_CHECK_PET_LOS &&
                Brain != null &&
                Brain is IControlledBrain &&
                (target is GamePlayer || (target is GameNPC && (target as GameNPC).Brain != null && (target as GameNPC).Brain is IControlledBrain)))
            {
                GameObject lastTarget = (GameObject)this.TempProperties.getProperty<object>(LAST_LOS_TARGET_PROPERTY, null);
                if (lastTarget != null && lastTarget == target)
                {
                    if (lastTick != 0 && CurrentRegion.Time - lastTick < ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        return;
                }

                GamePlayer losChecker = null;
                if (target is GamePlayer)
                {
                    losChecker = target as GamePlayer;
                }
                else if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
                {
                    losChecker = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
                }
                else
                {
                    // try to find another player to use for checking line of site
                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        losChecker = player;
                        break;
                    }
                }

                if (losChecker == null)
                {
                    return;
                }

                lock (LOS_LOCK)
                {
                    int count = TempProperties.getProperty<int>(NUM_LOS_CHECKS_INPROGRESS, 0);

                    if (count > 10)
                    {
                        log.DebugFormat("{0} LOS count check exceeds 10, aborting LOS check!", Name);

                        // Now do a safety check.  If it's been a while since we sent any check we should clear count
                        if (lastTick == 0 || CurrentRegion.Time - lastTick > ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        {
                            log.Debug("LOS count reset!");
                            TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, 0);
                        }

                        return;
                    }

                    count++;
                    TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, count);

                    TempProperties.setProperty(LAST_LOS_TARGET_PROPERTY, target);
                    TempProperties.setProperty(LAST_LOS_TICK_PROPERTY, CurrentRegion.Time);
                    m_targetLOSObject = target;

                }

                losChecker.Out.SendCheckLOS(this, target, new CheckLOSResponse(this.NPCStartAttackCheckLOS));
                return;
            }

            ContinueStartAttack(target);
        }

        /// <summary>
        /// We only attack if we have LOS
        /// </summary>
        /// <param name="player"></param>
        /// <param name="response"></param>
        /// <param name="targetOID"></param>
        public void NPCStartAttackCheckLOS(GamePlayer player, ushort response, ushort targetOID)
        {
            lock (LOS_LOCK)
            {
                int count = TempProperties.getProperty<int>(NUM_LOS_CHECKS_INPROGRESS, 0);
                count--;
                TempProperties.setProperty(NUM_LOS_CHECKS_INPROGRESS, Math.Max(0, count));
            }

            if ((response & 0x100) == 0x100)
            {
                // make sure we didn't switch targets
                if (TargetObject != null && m_targetLOSObject != null && TargetObject == m_targetLOSObject)
                    ContinueStartAttack(m_targetLOSObject);
            }
            else
            {
                if (m_targetLOSObject != null && m_targetLOSObject is GameLiving && Brain != null && Brain is IOldAggressiveBrain)
                {
                    // there will be a think delay before mob attempts to attack next target
                    (Brain as IOldAggressiveBrain).RemoveFromAggroList(m_targetLOSObject as GameLiving);
                }
            }
        }


        public virtual void ContinueStartAttack(GameObject target)
        {
            StopMoving();
            StopMovingOnPath();

            if (Brain is IControlledBrain brain && brain.AggressionState == eAggressionState.Passive)
                return;

            //if (target != TargetObject)
            SetLastMeleeAttackTick();
            StartMeleeAttackTimer();

            base.StartAttack(target);

            if (AttackState)
            {

                if (ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // Archer mobs sometimes bug and keep trying to fire at max range unsuccessfully so force them to get just a tad closer.
                    Follow(target, AttackRange - 30, STICKMAXIMUMRANGE);
                }
                else
                {
                    Follow(target, STICKMINIMUMRANGE, STICKMAXIMUMRANGE);
                }
            }

        }


        public override void RangedAttackFinished()
        {
            base.RangedAttackFinished();

            if (ServerProperties.Properties.ALWAYS_CHECK_PET_LOS &&
                Brain is IControlledBrain &&
                (TargetObject is GamePlayer || (TargetObject is GameNPC && (TargetObject as GameNPC).Brain != null && (TargetObject as GameNPC).Brain is IControlledBrain)))
            {
                GamePlayer player = null;

                if (TargetObject is GamePlayer)
                {
                    player = TargetObject as GamePlayer;
                }
                else if (TargetObject is GameNPC && (TargetObject as GameNPC).Brain != null && (TargetObject as GameNPC).Brain is IControlledBrain)
                {
                    if (((TargetObject as GameNPC).Brain as IControlledBrain).Owner is GamePlayer)
                    {
                        player = ((TargetObject as GameNPC).Brain as IControlledBrain).Owner as GamePlayer;
                    }
                }

                if (player != null)
                {
                    player.Out.SendCheckLOS(this, TargetObject, new CheckLOSResponse(NPCStopRangedAttackCheckLOS));
                    if (ServerProperties.Properties.ENABLE_DEBUG)
                    {
                        log.Debug(Name + " sent LOS check to player " + player.Name);
                    }
                }
            }
        }


        /// <summary>
        /// If we don't have LOS we stop attack
        /// </summary>
        /// <param name="player"></param>
        /// <param name="response"></param>
        /// <param name="targetOID"></param>
        public void NPCStopRangedAttackCheckLOS(GamePlayer player, ushort response, ushort targetOID)
        {
            if ((response & 0x100) != 0x100)
            {
                if (ServerProperties.Properties.ENABLE_DEBUG)
                {
                    log.Debug(Name + " FAILED stop ranged attack LOS check to player " + player.Name);
                }

                StopAttack();
            }
        }


        public void SetLastMeleeAttackTick()
        {
            if (TargetObject.Realm == 0 || Realm == 0)
                m_lastAttackTickPvE = m_CurrentRegion.Time;
            else
                m_lastAttackTickPvP = m_CurrentRegion.Time;
        }

        private void StartMeleeAttackTimer()
        {
            if (m_attackers.Count == 0)
            {
                if (SpellTimer == null)
                    SpellTimer = new SpellAction(this);

                if (!SpellTimer.IsAlive)
                    SpellTimer.Start(1);
            }
        }

        /// <summary>
        /// Returns the Damage this NPC does on an attack, adding 2H damage bonus if appropriate
        /// </summary>
        /// <param name="weapon">the weapon used for attack</param>
        /// <returns></returns>
        public override double AttackDamage(InventoryItem weapon)
        {
            double damage = base.AttackDamage(weapon);

            if (ActiveWeaponSlot == eActiveWeaponSlot.TwoHanded && m_blockChance > 0)
                switch (this)
                {
                    case Keeps.GameKeepGuard guard:
                        if (ServerProperties.Properties.GUARD_2H_BONUS_DAMAGE)
                            damage *= (100 + m_blockChance) / 100.00;
                        break;
                    case GamePet pet:
                        if (ServerProperties.Properties.PET_2H_BONUS_DAMAGE)
                            damage *= (100 + m_blockChance) / 100.00;
                        break;
                    default:
                        if (ServerProperties.Properties.MOB_2H_BONUS_DAMAGE)
                            damage *= (100 + m_blockChance) / 100.00;
                        break;
                }

            return damage;
        }

        private int AmbientTextTypeCallback(RegionTimer regionTimer)
        {
            if (hasImunity)
            {
                ImunityDomage = eDamageType.GM;
                DamageTypeCounter = 0;
                LastDamageType = eDamageType.GM;
                hasImunity = false;
            }
            if (tempoarallyFlags != 0)
            {
                tempoarallyFlags = 0;
                // Send flag update to the players
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendNPCCreate(this);
                    if (m_inventory != null)
                        player.Out.SendLivingEquipmentUpdate(this);
                }
            }
            if (temporallyBrain != null)
            {
                RemoveBrain(temporallyBrain);
                temporallyBrain = null;
            }
            if (temporallyTemplate != null)
            {
                temporallyTemplate = null;
                LoadTemplate(NPCTemplate);
                BroadcastLivingEquipmentUpdate();
            }
            ambientTextTimer.Stop();
            ambientTextTimer = null;
            return 1000;
        }

        public override void TakeDamage(AttackData ad)
        {
            GamePlayer gamePlayer = ad.Attacker as GamePlayer;
            GamePet pet = ad.Attacker as GamePet;
            if ((gamePlayer != null || (pet != null && pet.Owner is GamePlayer)) && (ad.AttackType != AttackData.eAttackType.MeleeDualWield && ad.AttackType != AttackData.eAttackType.MeleeOneHand && ad.AttackType != AttackData.eAttackType.MeleeTwoHand))
            {
                eDamageType damageType = ad.DamageType;
                MobXAmbientBehaviour ambientText = ambientTexts.Where(mobXAmbient => mobXAmbient.DamageTypeRepeat > 0).FirstOrDefault();
                if (ambientText != null && (ambientText.Chance == 100 || ambientText.Chance == 0) && (ambientText.HP == 0 || HealthPercent < ambientText.HP))
                {
                    if (hasImunity && ImunityDomage == damageType)
                    {
                        LastDamageType = damageType;
                        FireAmbientSentence(eAmbientTrigger.immunised, pet != null ? pet.Owner : gamePlayer);
                        ad.CriticalDamage = 0;
                        ad.Damage = 0;
                    }
                    else
                    {
                        if (damageType != LastDamageType)
                        {
                            DamageTypeCounter = 1;
                            LastDamageType = damageType;
                        }
                        else
                        {
                            DamageTypeCounter++;
                        }
                        if (DamageTypeCounter >= ambientText.DamageTypeRepeat)
                        {
                            FireAmbientSentence(eAmbientTrigger.immunised, pet != null ? pet.Owner : gamePlayer);
                            ad.CriticalDamage = 0;
                            ad.Damage = 0;
                        }
                    }
                }
            }
            base.TakeDamage(ad);
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GameLiving living)
            {
                Territory.Territory currentTerritory = TerritoryManager.Instance.GetCurrentTerritory(living.CurrentAreas);
                if (currentTerritory != null && currentTerritory.GuildOwner == GuildName && ((living is GamePlayer player && player.GuildName != GuildName) || (living.ControlledBrain != null && living.ControlledBrain.Owner.GuildName != GuildName)))
                    TerritoryManager.Instance.TerritoryAttacked(currentTerritory);

                if (damageAmount > 0)
                    FireAmbientSentence(eAmbientTrigger.hurting, living);
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        /// <summary>
        /// Gets/sets the object health
        /// </summary>
        public override int Health
        {
            get
            {
                return base.Health;
            }
            set
            {
                if (value > base.Health)
                {
                    List<MobXAmbientBehaviour> ambientText = ambientTexts.Where(mobXAmbient => mobXAmbient.HP > 0).ToList();
                    if (ambientText.Count > 0)
                    {
                        MobXAmbientBehaviour changeBrainAmbient = ambientText.Where(ambient => !string.IsNullOrEmpty(ambient.ChangeBrain)).FirstOrDefault();
                        if (changeBrainAmbient != null && changeBrainAmbient.HP < (byte)(value * 100 / MaxHealth))
                        {
                            if (temporallyBrain != null)
                            {
                                RemoveBrain(temporallyBrain);
                                temporallyBrain = null;
                            }
                        }
                        MobXAmbientBehaviour changeFlagAmbient = ambientText.Where(ambient => ambient.ChangeFlag > 0).FirstOrDefault();
                        if (changeFlagAmbient != null && changeFlagAmbient.HP < (byte)(value * 100 / MaxHealth))
                        {
                            tempoarallyFlags = 0;
                            // Send flag update to the players
                            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            {
                                player.Out.SendNPCCreate(this);
                                if (m_inventory != null)
                                    player.Out.SendLivingEquipmentUpdate(this);
                            }
                        }
                    }
                }
                base.Health = value;
                //Slow mobs down when they are hurt!
                short maxSpeed = MaxSpeed;
                if (CurrentSpeed > maxSpeed)
                    CurrentSpeed = maxSpeed;
            }
        }

        /// <summary>
        /// Tests if this MOB should give XP and loot based on the XPGainers
        /// </summary>
        /// <returns>true if it should deal XP and give loot</returns>
        public virtual bool IsWorthReward
        {
            get
            {
                if (CurrentRegion == null || CurrentRegion.Time - CHARMED_NOEXP_TIMEOUT < TempProperties.getProperty<long>(CHARMED_TICK_PROP))
                    return false;
                if (Brain is IControlledBrain)
                    return false;

                if (XPGainers.IsEmpty)
                    return false;

                foreach (var de in XPGainers)
                {
                    GameObject obj = de.Key;
                    // If a player to which we are gray killed up we
                    // aren't worth anything either
                    if (obj is GameLiving living && living.IsObjectGreyCon(this))
                        return false;

                    //If a gameplayer with privlevel > 1 attacked the
                    //mob, then the players won't gain xp ...
                    if (obj is GamePlayer player && player.Client.Account.PrivLevel > 1)
                        return false;
                }
                return true;
            }
            set
            {
            }
        }

        protected void ControlledNPC_Release()
        {
            if (this.ControlledBrain != null)
            {
                //log.Info("On tue le pet !");
                this.Notify(GameLivingEvent.PetReleased, ControlledBrain.Body);
            }
        }

        /// <summary>
        /// Called when this living dies
        /// </summary>
        public override void Die(GameObject killer)
        {
            FireAmbientSentence(eAmbientTrigger.dieing, killer as GameLiving);

            if (ControlledBrain != null)
                ControlledNPC_Release();

            if (killer != null)
            {
                if (IsWorthReward)
                    DropLoot(killer);

                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    if (!(killer == player))
                    {
                        player.MessageFromArea(this, player.GetPersonalizedName(this) + " dies!", eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow);
                    }
                }
                if (killer is GamePlayer killerPlayer)
                    killerPlayer.Out.SendMessage(killerPlayer.GetPersonalizedName(this) + " dies!", eChatType.CT_PlayerDied, eChatLoc.CL_SystemWindow);
            }
            StopFollowing();

            if (Group != null)
                Group.RemoveMember(this);

            if (killer != null)
            {
                // Handle faction alignement changes // TODO Review
                if ((Faction != null) && (killer is GamePlayer))
                {
                    // Get All Attackers. // TODO check if this shouldn't be set to Attackers instead of XPGainers ?
                    foreach (var de in XPGainers)
                    {
                        var player = de.Key as GamePlayer;

                        // Get Pets Owner (// TODO check if they are not already treated as attackers ?)
                        if (de.Key is GameNPC npc && npc.Brain is IControlledBrain brain)
                            player = brain.GetPlayerOwner();

                        if (player != null && player.ObjectState == eObjectState.Active && player.IsAlive && player.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                        {
                            Faction.KillMember(player);
                        }
                    }
                }

                // deal out exp and realm points based on server rules
                GameServer.ServerRules.OnNPCKilled(this, killer);
                base.Die(killer);
            }

            bool isStoppingEvent = false;
            if (this.EventID != null)
            {
                var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(this.EventID));

                if (ev != null)
                {
                    //Check if mob is a mob to kill in event
                    if (ev.Mobs.Contains(this) && ev.IsKillingEvent && ev.MobNamesToKill?.Contains(this.Name) == true)
                    {
                        ev.WantedMobsCount--;

                        if (ev.WantedMobsCount == 0)
                        {
                            GamePlayer player = killer as GamePlayer;
                            GamePlayer controller = null;
                            if (killer is GameLiving living && living.ControlledBrain != null && living.ControlledBrain.Owner is GamePlayer)
                                controller = living.ControlledBrain.Owner as GamePlayer;
                            if (player != null || controller != null)
                                ev.Owner = player ?? controller;
                            isStoppingEvent = true;
                            Task.Run(() => GameEventManager.Instance.StopEvent(ev, EndingConditionType.Kill));
                        }
                    }
                }
            }

            //Check if killed mob starts event
            if (this.CurrentGroupMob != null)
            {
                bool isAllOthersGroupMobDead = MobGroupManager.Instance.IsAllOthersGroupMobDead(this);
                var mobGroupEvent = GameEventManager.Instance.Events.FirstOrDefault(e =>
                e.KillStartingGroupMobId?.Equals(this.CurrentGroupMob.GroupId) == true &&
               !e.StartedTime.HasValue &&
                e.Status == EventStatus.NotOver &&
                e.StartConditionType == StartingConditionType.Kill);

                if (isAllOthersGroupMobDead && mobGroupEvent != null)
                {
                    Task.Run(() => GameEventManager.Instance.StartEvent(mobGroupEvent, null, killer as GamePlayer));
                }
            }

            Delete();

            // remove temp properties
            TempProperties.removeAllProperties();

            if (!(this is GamePet) && (this.EventID == null || (CanRespawnWithinEvent && !isStoppingEvent)))
                StartRespawn();
        }

        /// <summary>
        /// Stores the melee damage type of this NPC
        /// </summary>
        protected eDamageType m_meleeDamageType = eDamageType.Slash;

        /// <summary>
        /// Gets or sets the melee damage type of this NPC
        /// </summary>
        public virtual eDamageType MeleeDamageType
        {
            get { return m_meleeDamageType; }
            set { m_meleeDamageType = value; }
        }

        /// <summary>
        /// Returns the damage type of the current attack
        /// </summary>
        /// <param name="weapon">attack weapon</param>
        public override eDamageType AttackDamageType(InventoryItem weapon)
        {
            return m_meleeDamageType;
        }

        /// <summary>
        /// Stores the NPC evade chance
        /// </summary>
        protected byte m_evadeChance;
        /// <summary>
        /// Stores the NPC block chance
        /// </summary>
        protected byte m_blockChance;
        /// <summary>
        /// Stores the NPC parry chance
        /// </summary>
        protected byte m_parryChance;
        /// <summary>
        /// Stores the NPC left hand swing chance
        /// </summary>
        protected byte m_leftHandSwingChance;

        /// <summary>
        /// Gets or sets the NPC evade chance
        /// </summary>
        public virtual byte EvadeChance
        {
            get { return m_evadeChance; }
            set { m_evadeChance = value; }
        }

        /// <summary>
        /// Gets or sets the NPC block chance
        /// </summary>
        public virtual byte BlockChance
        {
            get
            {
                //When npcs have two handed weapons, we don't want them to block
                if (ActiveWeaponSlot != eActiveWeaponSlot.Standard)
                    return 0;

                return m_blockChance;
            }
            set
            {
                m_blockChance = value;
            }
        }

        /// <summary>
        /// Gets or sets the NPC parry chance
        /// </summary>
        public virtual byte ParryChance
        {
            get { return m_parryChance; }
            set { m_parryChance = value; }
        }

        /// <summary>
        /// Gets or sets the NPC left hand swing chance
        /// </summary>
        public byte LeftHandSwingChance
        {
            get { return m_leftHandSwingChance; }
            set { m_leftHandSwingChance = value; }
        }

        /// <summary>
        /// Calculates how many times left hand swings
        /// </summary>
        /// <returns></returns>
        public override int CalculateLeftHandSwingCount()
        {
            if (Util.Chance(m_leftHandSwingChance))
                return 1;
            return 0;
        }

        /// <summary>
        /// Checks whether Living has ability to use lefthanded weapons
        /// </summary>
        public override bool CanUseLefthandedWeapon
        {
            get { return m_leftHandSwingChance > 0; }
        }

        /// <summary>
        /// Method to switch the npc to Melee attacks
        /// </summary>
        /// <param name="target"></param>
        public void SwitchToMelee(GameObject target)
        {
            // Tolakram: Order is important here.  First StopAttack, then switch weapon
            StopFollowing();
            StopAttack();

            InventoryItem twohand = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
            InventoryItem righthand = Inventory.GetItem(eInventorySlot.RightHandWeapon);

            if (twohand != null && righthand == null)
                SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            else if (twohand != null && righthand != null)
            {
                if (Util.Chance(50))
                    SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                else SwitchWeapon(eActiveWeaponSlot.Standard);
            }
            else
                SwitchWeapon(eActiveWeaponSlot.Standard);

            StartAttack(target);
        }

        /// <summary>
        /// Method to switch the guard to Ranged attacks
        /// </summary>
        /// <param name="target"></param>
        public void SwitchToRanged(GameObject target)
        {
            StopFollowing();
            StopAttack();
            SwitchWeapon(eActiveWeaponSlot.Distance);
            StartAttack(target);
        }

        /// <summary>
        /// Draw the weapon, but don't actually start a melee attack.
        /// </summary>		
        public virtual void DrawWeapon()
        {
            if (!AttackState)
            {
                AttackState = true;

                BroadcastUpdate();

                AttackState = false;
            }
        }

        /// <summary>
        /// If npcs cant move, they cant be interupted from range attack
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attackType"></param>
        /// <returns></returns>
        protected override bool OnInterruptTick(GameLiving attacker, AttackData.eAttackType attackType)
        {
            if (this.MaxSpeedBase == 0)
            {
                if (attackType == AttackData.eAttackType.Ranged || attackType == AttackData.eAttackType.Spell)
                {
                    if (this.IsWithinRadius(attacker, 150) == false)
                        return false;
                }
            }

            // Experimental - this prevents interrupts from causing ranged attacks to always switch to melee
            if (AttackState)
            {
                if (ActiveWeaponSlot == eActiveWeaponSlot.Distance && HealthPercent < MINHEALTHPERCENTFORRANGEDATTACK)
                {
                    SwitchToMelee(attacker);
                }
                else if (ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
                         Inventory != null &&
                         Inventory.GetItem(eInventorySlot.DistanceWeapon) != null &&
                         GetDistanceTo(attacker) > 500)
                {
                    SwitchToRanged(attacker);
                }
            }

            return base.OnInterruptTick(attacker, attackType);
        }

        /// <summary>
        /// The time to wait before each mob respawn
        /// </summary>
        protected int m_respawnInterval;
        /// <summary>
        /// A timer that will respawn this mob
        /// </summary>
        protected RegionTimer m_respawnTimer;
        /// <summary>
        /// The sync object for respawn timer modifications
        /// </summary>
        protected readonly object m_respawnTimerLock = new object();
        /// <summary>
        /// The Respawn Interval of this mob in milliseconds
        /// </summary>
        public virtual int RespawnInterval
        {
            get
            {
                if (m_respawnInterval > 0 || m_respawnInterval < 0)
                    return m_respawnInterval;

                int minutes = Util.Random(ServerProperties.Properties.NPC_MIN_RESPAWN_INTERVAL, ServerProperties.Properties.NPC_MIN_RESPAWN_INTERVAL + 5);

                if (Name != Name.ToLower())
                {
                    minutes += 5;
                }

                if (Level <= 65 && Realm == 0)
                {
                    return minutes * 60000;
                }
                else if (Realm != 0)
                {
                    // 5 to 10 minutes for realm npc's
                    return Util.Random(5 * 60000, 10 * 60000);
                }
                else
                {
                    int add = (Level - 65) + ServerProperties.Properties.NPC_MIN_RESPAWN_INTERVAL;
                    return (minutes + add) * 60000;
                }
            }
            set
            {
                m_respawnInterval = value;
            }
        }

        /// <summary>
        /// True if NPC is alive, else false.
        /// </summary>
        public override bool IsAlive
        {
            get
            {
                bool alive = base.IsAlive;
                if (alive && IsRespawning)
                    return false;
                return alive;
            }
        }

        /// <summary>
        /// True, if the mob is respawning, else false.
        /// </summary>
        public bool IsRespawning
        {
            get
            {
                if (m_respawnTimer == null)
                    return false;
                return m_respawnTimer.IsAlive;
            }
        }

        /// <summary>
        /// Starts the Respawn Timer
        /// </summary>
        public virtual void StartRespawn()
        {
            if (IsAlive) return;

            if (this.Brain is IControlledBrain)
                return;

            int respawnInt = RespawnInterval;
            if (respawnInt > 0)
            {
                lock (m_respawnTimerLock)
                {
                    if (m_respawnTimer == null)
                    {
                        m_respawnTimer = new RegionTimer(this);
                        m_respawnTimer.Callback = new RegionTimerCallback(RespawnTimerCallback);
                    }
                    else if (m_respawnTimer.IsAlive)
                    {
                        m_respawnTimer.Stop();
                    }
                    // register Mob as "respawning"
                    CurrentRegion.MobsRespawning.TryAdd(this, respawnInt);

                    m_respawnTimer.Start(respawnInt);
                }
            }
        }
        protected virtual int RespawnTimerCallback(RegionTimer respawnTimer)
        {
            Spawn();
            return 0;
        }

        /// <summary>
        /// Callback timer for health regeneration
        /// </summary>
        /// <param name="selfRegenerationTimer">the regeneration timer</param>
        /// <returns>the new interval</returns>
        protected override int HealthRegenerationTimerCallback(RegionTimer selfRegenerationTimer)
        {
            int period = m_healthRegenerationPeriod;
            if (!InCombat)
            {
                int oldPercent = HealthPercent;
                period = base.HealthRegenerationTimerCallback(selfRegenerationTimer);
                if (oldPercent != HealthPercent)
                    BroadcastUpdate();
            }
            return (Health < MaxHealth) ? period : 0;
        }

        /// <summary>
        /// The chance for a critical hit
        /// </summary>
        public override int AttackCriticalChance(InventoryItem weapon)
        {
            if (m_activeWeaponSlot == eActiveWeaponSlot.Distance)
            {
                if (RangedAttackType == eRangedAttackType.Critical)
                    return 0; // no crit damage for crit shots
                else
                    return GetModified(eProperty.CriticalArcheryHitChance);
            }

            return GetModified(eProperty.CriticalMeleeHitChance);
        }

        /// <summary>
        /// Stop attacking and following, but stay in attack mode (e.g. in
        /// order to cast a spell instead).
        /// </summary>
        public virtual void HoldAttack()
        {
            if (m_attackAction != null)
                m_attackAction.Stop();
            StopFollowing();
        }

        /// <summary>
        /// Continue a previously started attack.
        /// </summary>
        public virtual void ContinueAttack(GameObject target)
        {
            if (m_attackAction != null && target != null)
            {
                Follow(target, STICKMINIMUMRANGE, MaxDistance);
                m_attackAction.Start(1);
            }
        }

        /// <summary>
        /// Stops all attack actions, including following target
        /// </summary>
        public override void StopAttack()
        {
            base.StopAttack();
            StopFollowing();

            // Tolakram: If npc has a distance weapon it needs to be made active after attack is stopped
            if (Inventory != null && Inventory.GetItem(eInventorySlot.DistanceWeapon) != null && ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                SwitchWeapon(eActiveWeaponSlot.Distance);
        }

        /// <summary>
        /// This method is called to drop loot after this mob dies
        /// </summary>
        /// <param name="killer">The killer</param>
        public virtual void DropLoot(GameObject killer)
        {
            // TODO: mobs drop "a small chest" sometimes
            ArrayList droplist = new ArrayList();
            ArrayList autolootlist = new ArrayList();
            ArrayList aplayer = new ArrayList();

            var gainers = XPGainers.ToArray();
            if (gainers.Length == 0)
                return;

            ItemTemplate[] lootTemplates = LootMgr.GetLoot(this, killer);

            foreach (ItemTemplate lootTemplate in lootTemplates)
            {
                if (lootTemplate == null) continue;
                GameStaticItem loot = null;
                if (GameMoney.IsItemMoney(lootTemplate.Name))
                {
                    long value = lootTemplate.Price;
                    //GamePlayer killerPlayer = killer as GamePlayer;

                    //[StephenxPimentel] - Zone Bonus XP Support
                    if (ServerProperties.Properties.ENABLE_ZONE_BONUSES)
                    {
                        GamePlayer killerPlayer = killer as GamePlayer;
                        if (killer is GameNPC)
                        {
                            if (killer is GameNPC && ((killer as GameNPC).Brain is IControlledBrain))
                                killerPlayer = ((killer as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
                            else return;
                        }

                        int zoneBonus = (((int)value * ZoneBonus.GetCoinBonus(killerPlayer) / 100));
                        if (zoneBonus > 0)
                        {
                            long amount = (long)(zoneBonus * ServerProperties.Properties.MONEY_DROP);
                            killerPlayer.AddMoney(Currency.Copper.Mint(amount));
                            killerPlayer.SendMessage(ZoneBonus.GetBonusMessage(killerPlayer, (int)(zoneBonus * ServerProperties.Properties.MONEY_DROP), ZoneBonus.eZoneBonusType.COIN),
                                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            InventoryLogging.LogInventoryAction(this, killerPlayer, eInventoryActionType.Loot, amount);
                        }
                    }
                    if (Keeps.KeepBonusMgr.RealmHasBonus(DOL.GS.Keeps.eKeepBonusType.Coin_Drop_5, (eRealm)killer.Realm))
                        value += (value / 100) * 5;
                    else if (Keeps.KeepBonusMgr.RealmHasBonus(DOL.GS.Keeps.eKeepBonusType.Coin_Drop_3, (eRealm)killer.Realm))
                        value += (value / 100) * 3;

                    //this will need to be changed when the ML for increasing money is added
                    if (value != lootTemplate.Price)
                    {
                        GamePlayer killerPlayer = killer as GamePlayer;
                        if (killerPlayer != null)
                            killerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(killerPlayer.Client, "GameNPC.DropLoot.AdditionalMoney", Money.GetString(value - lootTemplate.Price)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                    }
                    //Mythical Coin bonus property (Can be used for any equipped item, bonus 235)
                    if (killer is GamePlayer)
                    {
                        GamePlayer killerPlayer = killer as GamePlayer;
                        if (killerPlayer.GetModified(eProperty.MythicalCoin) > 0)
                        {
                            ((WorldInventoryItem)loot).Item.Count = ((WorldInventoryItem)loot).Item.PackSize;
                            value += (value * killerPlayer.GetModified(eProperty.MythicalCoin)) / 100;
                            killerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(killerPlayer.Client,
                                                                                    "GameNPC.DropLoot.ItemAdditionalMoney", Money.GetString(value - lootTemplate.Price)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);

                        }
                    }

                    loot = new GameMoney(value, this);
                    loot.Name = lootTemplate.Name;
                    loot.Model = (ushort)lootTemplate.Model;
                }
                else
                {
                    InventoryItem invitem;

                    if (lootTemplate is ItemUnique)
                    {
                        GameServer.Database.AddObject(lootTemplate);
                        invitem = GameInventoryItem.Create(lootTemplate as ItemUnique);
                    }
                    else
                        invitem = GameInventoryItem.Create(lootTemplate);

                    if (lootTemplate is GeneratedUniqueItem)
                    {
                        invitem.IsROG = true;
                    }

                    loot = new WorldInventoryItem(invitem);
                    loot.Position = Position;
                    loot.Heading = Heading;
                    loot.CurrentRegion = CurrentRegion;
                    (loot as WorldInventoryItem).Item.IsCrafted = false;
                    (loot as WorldInventoryItem).Item.Creator = Name;

                    // This may seem like an odd place for this code, but loot-generating code further up the line
                    // is dealing strictly with ItemTemplate objects, while you need the InventoryItem in order
                    // to be able to set the Count property.
                    // Converts single drops of loot with PackSize > 1 (and MaxCount >= PackSize) to stacks of Count = PackSize
                    if (((WorldInventoryItem)loot).Item.PackSize > 1 && ((WorldInventoryItem)loot).Item.MaxCount >= ((WorldInventoryItem)loot).Item.PackSize)
                    {
                        ((WorldInventoryItem)loot).Item.Count = ((WorldInventoryItem)loot).Item.PackSize;
                    }
                }

                GamePlayer playerAttacker = null;
                foreach (var (gainer, _dmg) in gainers)
                {
                    if (gainer is GamePlayer player)
                    {
                        playerAttacker = player;
                        if (loot.Realm == 0)
                            loot.Realm = player.Realm;
                    }
                    loot.AddOwner(gainer);
                    if (gainer is GameNPC npc && npc.Brain is IControlledBrain brain)
                    {
                        playerAttacker = brain.GetPlayerOwner();
                        loot.AddOwner(brain.GetPlayerOwner());
                    }
                }
                if (playerAttacker == null)
                    return; // no loot if mob kills another mob

                droplist.Add(loot.GetName(1, false));
                loot.AddToWorld();

                foreach (var (gainer, _dmg) in gainers)
                {
                    if (gainer is GamePlayer player && player.Autoloot &&
                        loot.IsWithinRadius(player, 1500)) // should be large enough for most casters to autoloot
                    {
                        if (player.Group == null || (player.Group != null && player == player.Group.Leader))
                            aplayer.Add(player);
                        autolootlist.Add(loot);
                    }
                }
            }

            BroadcastLoot(droplist);

            if (autolootlist.Count > 0)
            {
                foreach (GameObject obj in autolootlist)
                {
                    foreach (GamePlayer player in aplayer)
                    {
                        player.PickupObject(obj, true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The enemy is healed, so we add to the xp gainers list
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="healSource"></param>
        /// <param name="changeType"></param>
        /// <param name="healAmount"></param>
        public override void EnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType, int healAmount)
        {
            base.EnemyHealed(enemy, healSource, changeType, healAmount);

            if (changeType != eHealthChangeType.Spell)
                return;
            if (enemy == healSource)
                return;
            if (!IsAlive)
                return;

            var attackerLiving = healSource as GameLiving;
            if (attackerLiving == null)
                return;

            Group attackerGroup = attackerLiving.Group;
            if (attackerGroup != null)
            {
                // collect "helping" group players in range
                var xpGainers = attackerGroup.GetMembersInTheGroup()
                    .Where(l => IsWithinRadius(l, WorldMgr.MAX_EXPFORKILL_DISTANCE) && l.IsAlive && l.ObjectState == eObjectState.Active)
                    .ToArray();

                float damageAmount = (float)healAmount / xpGainers.Length;

                foreach (GameLiving living in xpGainers)
                {
                    // add players in range for exp to exp gainers
                    this.AddXPGainer(living, damageAmount);
                }
            }
            else
            {
                this.AddXPGainer(healSource, (float)healAmount);
            }
            //DealDamage needs to be called after addxpgainer!
        }

        public override double WeaponDamage(InventoryItem weapon)
        {
            double dps;
            var spd = WeaponSpd * 0.1;
            var twohand_bonus = 1.0;

            if (weapon == null || weapon.DPS_AF <= 1)
            {
                dps = WeaponDps * 0.1;
                dps *= 1.0 + (GetModified(eProperty.DPS) * 0.01);
                dps *= 0.98;

                if (ActiveWeaponSlot == eActiveWeaponSlot.TwoHanded)
                {
                    var wp_spec = GetModifiedSpecLevel("Staff");
                    twohand_bonus = 1.1 + 0.005 * wp_spec;
                }
            }
            else
            {
                dps = weapon.DPS_AF * 0.1;
                spd = weapon.SPD_ABS * 0.1;
                var cap = 1.5 + 0.3 * Level;
                dps = dps.Clamp(0.1, cap);
                dps *= 1.0 + (GetModified(eProperty.DPS) * 0.01);
                // beware to use always ConditionPercent, because Condition is abolute value
                dps *= weapon.Quality * 0.01 * weapon.ConditionPercent * 0.01;

                if (weapon.Item_Type == Slot.TWOHAND)
                {
                    var wp_spec = GetModifiedSpecLevel("Staff");
                    twohand_bonus = 1.1 + 0.005 * wp_spec;
                }
            }
            var weapon_dps = dps * spd
                * (0.94 + spd * 0.03)
                * twohand_bonus
                * (1 + 0.01 * GetModified(eProperty.MeleeDamage));
            return 2.0 + weapon_dps;
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return Math.Min(5, (int)Level) + ArmorFactor + GetModified(eProperty.ArmorFactor) / 5;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            return ArmorAbsorb / 100.0 + GetModified(eProperty.ArmorAbsorption) * 0.01;
        }
        #endregion

        #region Styles
        /// <summary>
        /// Styles for this NPC
        /// </summary>
        private List<Style> m_styles = new List<Style>(0);
        public List<Style> Styles
        {
            get { return m_styles; }
            set
            {
                m_styles = value;
                this.SortStyles();
            }
        }

        /// <summary>
        /// Stealth styles for this NPC
        /// </summary>
        public List<Style> StylesStealth { get; protected set; } = null;

        /// <summary>
        /// Chain styles for this NPC
        /// </summary>
        public List<Style> StylesChain { get; protected set; } = null;

        /// <summary>
        /// Defensive styles for this NPC
        /// </summary>
        public List<Style> StylesDefensive { get; protected set; } = null;

        /// <summary>
        /// Back positional styles for this NPC
        /// </summary>
        public List<Style> StylesBack { get; protected set; } = null;

        /// <summary>
        /// Side positional styles for this NPC
        /// </summary>
        public List<Style> StylesSide { get; protected set; } = null;

        /// <summary>
        /// Front positional styles for this NPC
        /// </summary>
        public List<Style> StylesFront { get; protected set; } = null;

        /// <summary>
        /// Anytime styles for this NPC
        /// </summary>
        public List<Style> StylesAnytime { get; protected set; } = null;

        /// <summary>
        /// Sorts styles by type for more efficient style selection later
        /// </summary>
        public virtual void SortStyles()
        {
            if (StylesStealth != null)
                StylesStealth.Clear();

            if (StylesChain != null)
                StylesChain.Clear();

            if (StylesDefensive != null)
                StylesDefensive.Clear();

            if (StylesBack != null)
                StylesBack.Clear();

            if (StylesSide != null)
                StylesSide.Clear();

            if (StylesFront != null)
                StylesFront.Clear();

            if (StylesAnytime != null)
                StylesAnytime.Clear();

            if (m_styles == null)
                return;

            foreach (Style s in m_styles)
            {
                if (s == null)
                {
                    if (log.IsWarnEnabled)
                    {
                        String sError = $"GameNPC.SortStyles(): NULL style for NPC named {Name}";
                        if (m_InternalID != null)
                            sError += $", InternalID {this.m_InternalID}";
                        if (m_npcTemplate != null)
                            sError += $", NPCTemplateID {m_npcTemplate.TemplateId}";
                        log.Warn(sError);
                    }
                    continue; // Keep sorting, as a later style may not be null
                }// if (s == null)

                if (s.StealthRequirement)
                {
                    if (StylesStealth == null)
                        StylesStealth = new List<Style>(1);
                    StylesStealth.Add(s);
                }

                switch (s.OpeningRequirementType)
                {
                    case Style.eOpening.Defensive:
                        if (StylesDefensive == null)
                            StylesDefensive = new List<Style>(1);
                        StylesDefensive.Add(s);
                        break;
                    case Style.eOpening.Positional:
                        switch ((Style.eOpeningPosition)s.OpeningRequirementValue)
                        {
                            case Style.eOpeningPosition.Back:
                                if (StylesBack == null)
                                    StylesBack = new List<Style>(1);
                                StylesBack.Add(s);
                                break;
                            case Style.eOpeningPosition.Side:
                                if (StylesSide == null)
                                    StylesSide = new List<Style>(1);
                                StylesSide.Add(s);
                                break;
                            case Style.eOpeningPosition.Front:
                                if (StylesFront == null)
                                    StylesFront = new List<Style>(1);
                                StylesFront.Add(s);
                                break;
                            default:
                                log.Warn($"GameNPC.SortStyles(): Invalid OpeningRequirementValue for positional style {s.Name}, ID {s.ID}, ClassId {s.ClassID}");
                                break;
                        }
                        break;
                    default:
                        if (s.OpeningRequirementValue > 0)
                        {
                            if (StylesChain == null)
                                StylesChain = new List<Style>(1);
                            StylesChain.Add(s);
                        }
                        else
                        {
                            if (StylesAnytime == null)
                                StylesAnytime = new List<Style>(1);
                            StylesAnytime.Add(s);
                        }
                        break;
                }// switch (s.OpeningRequirementType)
            }// foreach
        }// SortStyles()

        /// <summary>
        /// Can we use this style without spamming a stun style?
        /// </summary>
        /// <param name="style">The style to check.</param>
        /// <returns>True if we should use the style, false if it would be spamming a stun effect.</returns>
        protected bool CheckStyleStun(Style style)
        {
            if (TargetObject is GameLiving living && style.Procs.Count > 0)
                foreach (Tuple<Spell, int, int> t in style.Procs)
                    if (t != null && t.Item1 is Spell spell
                        && spell.SpellType.ToUpper() == "STYLESTUN" && living.HasEffect(t.Item1))
                        return false;

            return true;
        }

        /// <summary>
        /// Picks a style, prioritizing reactives an	d chains over positionals and anytimes
        /// </summary>
        /// <returns>Selected style</returns>
        protected override Style GetStyleToUse()
        {
            if (m_styles == null || m_styles.Count < 1 || TargetObject == null)
                return null;

            if (StylesStealth != null && StylesStealth.Count > 0 && IsStealthed)
                foreach (Style s in StylesStealth)
                    if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
                        return s;

            // Chain and defensive styles skip the GAMENPC_CHANCES_TO_STYLE,
            //	or they almost never happen e.g. NPC blocks 10% of the time,
            //	default 20% style chance means the defensive style only happens
            //	2% of the time, and a chain from it only happens 0.4% of the time.
            if (StylesChain != null && StylesChain.Count > 0)
                foreach (Style s in StylesChain)
                    if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
                        return s;

            if (StylesDefensive != null && StylesDefensive.Count > 0)
                foreach (Style s in StylesDefensive)
                    if (StyleProcessor.CanUseStyle(this, s, AttackWeapon)
                        && CheckStyleStun(s)) // Make sure we don't spam stun styles like Brutalize
                        return s;

            if (Util.Chance(Properties.GAMENPC_CHANCES_TO_STYLE))
            {
                // Check positional styles
                // Picking random styles allows mobs to use multiple styles from the same position
                //	e.g. a mob with both Pincer and Ice Storm side styles will use both of them.
                if (StylesBack != null && StylesBack.Count > 0)
                {
                    Style s = StylesBack[Util.Random(0, StylesBack.Count - 1)];
                    if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
                        return s;
                }

                if (StylesSide != null && StylesSide.Count > 0)
                {
                    Style s = StylesSide[Util.Random(0, StylesSide.Count - 1)];
                    if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
                        return s;
                }

                if (StylesFront != null && StylesFront.Count > 0)
                {
                    Style s = StylesFront[Util.Random(0, StylesFront.Count - 1)];
                    if (StyleProcessor.CanUseStyle(this, s, AttackWeapon))
                        return s;
                }

                // Pick a random anytime style
                if (StylesAnytime != null && StylesAnytime.Count > 0)
                    return StylesAnytime[Util.Random(0, StylesAnytime.Count - 1)];
            }

            return null;
        } // GetStyleToUse()
        #endregion

        /// <summary>
        /// The Abilities for this NPC
        /// </summary>
        public Dictionary<string, Ability> Abilities
        {
            get
            {
                Dictionary<string, Ability> tmp = new Dictionary<string, Ability>();

                lock (m_lockAbilities)
                {
                    tmp = new Dictionary<string, Ability>(m_abilities);
                }

                return tmp;
            }
        }

        #region Spell
        private IList m_spells = new List<Spell>(0);
        /// <summary>
        /// property of spell array of NPC
        /// </summary>
        public virtual IList Spells
        {
            get { return m_spells; }
            set
            {
                if (value == null || value.Count < 1)
                {
                    m_spells.Clear();
                    InstantHarmfulSpells = null;
                    HarmfulSpells = null;
                    InstantHealSpells = null;
                    HealSpells = null;
                    InstantMiscSpells = null;
                    MiscSpells = null;
                }
                else
                {
                    m_spells = value.Cast<Spell>().ToList();
                    SortSpells();
                }
            }
        }

        /// <summary>
        /// Harmful spell list and accessor
        /// </summary>
        public List<Spell> HarmfulSpells { get; set; } = null;

        /// <summary>
        /// Whether or not the NPC can cast harmful spells with a cast time.
        /// </summary>
        public bool CanCastHarmfulSpells
        {
            get { return (HarmfulSpells != null && HarmfulSpells.Count > 0); }
        }

        /// <summary>
        /// Instant harmful spell list and accessor
        /// </summary>
        public List<Spell> InstantHarmfulSpells { get; set; } = null;

        /// <summary>
        /// Whether or not the NPC can cast harmful instant spells.
        /// </summary>
        public bool CanCastInstantHarmfulSpells
        {
            get { return (InstantHarmfulSpells != null && InstantHarmfulSpells.Count > 0); }
        }

        /// <summary>
        /// Healing spell list and accessor
        /// </summary>
        public List<Spell> HealSpells { get; set; } = null;

        /// <summary>
        /// Whether or not the NPC can cast heal spells with a cast time.
        /// </summary>
        public bool CanCastHealSpells
        {
            get { return (HealSpells != null && HealSpells.Count > 0); }
        }

        /// <summary>
        /// Instant healing spell list and accessor
        /// </summary>
        public List<Spell> InstantHealSpells { get; set; } = null;

        /// <summary>
        /// Whether or not the NPC can cast instant healing spells.
        /// </summary>
        public bool CanCastInstantHealSpells
        {
            get { return (InstantHealSpells != null && InstantHealSpells.Count > 0); }
        }

        /// <summary>
        /// Miscellaneous spell list and accessor
        /// </summary>
        public List<Spell> MiscSpells { get; set; } = null;

        /// <summary>
        /// Whether or not the NPC can cast miscellaneous spells with a cast time.
        /// </summary>
        public bool CanCastMiscSpells
        {
            get { return (MiscSpells != null && MiscSpells.Count > 0); }
        }

        /// <summary>
        /// Instant miscellaneous spell list and accessor
        /// </summary>
        public List<Spell> InstantMiscSpells { get; set; } = null;

        /// <summary>
        /// Whether or not the NPC can cast miscellaneous instant spells.
        /// </summary>
        public bool CanCastInstantMiscSpells
        {
            get { return (InstantMiscSpells != null && InstantMiscSpells.Count > 0); }
        }

        /// <summary>
        /// Sort spells into specific lists
        /// </summary>
        public virtual void SortSpells()
        {
            if (Spells.Count < 1)
                return;

            // Clear the lists
            if (InstantHarmfulSpells != null)
                InstantHarmfulSpells.Clear();
            if (HarmfulSpells != null)
                HarmfulSpells.Clear();

            if (InstantHealSpells != null)
                InstantHealSpells.Clear();
            if (HealSpells != null)
                HealSpells.Clear();

            if (InstantMiscSpells != null)
                InstantMiscSpells.Clear();
            if (MiscSpells != null)
                MiscSpells.Clear();

            // Sort spells into lists
            foreach (Spell spell in m_spells)
            {
                if (spell == null)
                    continue;


                if (spell.IsHarmful)
                {
                    if (spell.IsInstantCast)
                    {
                        if (InstantHarmfulSpells == null)
                            InstantHarmfulSpells = new List<Spell>(1);
                        InstantHarmfulSpells.Add(spell);
                    }
                    else
                    {
                        if (HarmfulSpells == null)
                            HarmfulSpells = new List<Spell>(1);
                        HarmfulSpells.Add(spell);
                    }
                }
                else if (spell.IsHealing)
                {
                    if (spell.IsInstantCast)
                    {
                        if (InstantHealSpells == null)
                            InstantHealSpells = new List<Spell>(1);
                        InstantHealSpells.Add(spell);
                    }
                    else
                    {
                        if (HealSpells == null)
                            HealSpells = new List<Spell>(1);
                        HealSpells.Add(spell);
                    }
                }
                else
                {
                    if (spell.IsInstantCast)
                    {
                        if (InstantMiscSpells == null)
                            InstantMiscSpells = new List<Spell>(1);
                        InstantMiscSpells.Add(spell);
                    }
                    else
                    {
                        if (MiscSpells == null)
                            MiscSpells = new List<Spell>(1);
                        MiscSpells.Add(spell);
                    }
                }
            } // foreach
        }

        private SpellAction m_spellaction = null;
        /// <summary>
        /// The timer that controls an npc's spell casting
        /// </summary>
        public SpellAction SpellTimer
        {
            get { return m_spellaction; }
            set { m_spellaction = value; }
        }

        /// <summary>
        /// Callback after spell execution finished and next spell can be processed
        /// </summary>
        /// <param name="handler"></param>
        public override void OnAfterSpellCastSequence(ISpellHandler handler)
        {
            if (SpellTimer != null)
            {
                if (this == null || this.ObjectState != eObjectState.Active || !this.IsAlive || this.TargetObject == null || (this.TargetObject is GameLiving && this.TargetObject.ObjectState != eObjectState.Active || !(this.TargetObject as GameLiving).IsAlive))
                    SpellTimer.Stop();
                else
                {
                    int interval = 1500;

                    if (Brain != null)
                    {
                        interval = Math.Min(interval, Brain.ThinkInterval);
                    }

                    SpellTimer.Start(interval);
                }
            }

            if (m_runningSpellHandler != null)
            {
                //prevent from relaunch
                base.OnAfterSpellCastSequence(handler);
            }

            // Notify Brain of Cast Finishing.
            if (Brain != null)
                Brain.Notify(GameNPCEvent.CastFinished, this, new CastingEventArgs(handler));
        }

        /// <summary>
        /// The spell action of this living
        /// </summary>
        public class SpellAction : RegionAction
        {
            /// <summary>
            /// Constructs a new attack action
            /// </summary>
            /// <param name="owner">The action source</param>
            public SpellAction(GameLiving owner)
                : base(owner)
            {
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override void OnTick()
            {
                GameNPC owner = null;
                if (m_actionSource != null && m_actionSource is GameNPC)
                    owner = (GameNPC)m_actionSource;
                else
                {
                    Stop();
                    return;
                }

                if (owner.TargetObject == null || !owner.AttackState)
                {
                    Stop();
                    return;
                }

                //If we started casting a spell, stop the timer and wait for
                //GameNPC.OnAfterSpellSequenceCast to start again
                if (owner.Brain is StandardMobBrain && ((StandardMobBrain)owner.Brain).CheckSpells(StandardMobBrain.eCheckSpellType.Offensive))
                {
                    Stop();
                    return;
                }
                else
                {
                    //If we aren't a distance NPC, lets make sure we are in range to attack the target!
                    if (owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance && !owner.IsWithinRadius(owner.TargetObject, STICKMINIMUMRANGE))
                        ((GameNPC)owner).Follow(owner.TargetObject, STICKMINIMUMRANGE, STICKMAXIMUMRANGE);
                }

                if (owner.Brain != null)
                {
                    Interval = Math.Min(1500, owner.Brain.CastInterval);
                }
                else
                {
                    Interval = 1500;
                }
            }
        }

        private const string LOSTEMPCHECKER = "LOSTEMPCHECKER";
        private const string LOSCURRENTSPELL = "LOSCURRENTSPELL";
        private const string LOSCURRENTLINE = "LOSCURRENTLINE";
        private const string LOSSPELLTARGET = "LOSSPELLTARGET";


        /// <summary>
        /// Cast a spell, with optional LOS check
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="line"></param>
        /// <param name="checkLOS"></param>
        public virtual void CastSpell(Spell spell, SpellLine line, bool checkLOS)
        {
            if (IsIncapacitated)
                return;

            if (checkLOS)
            {
                CastSpell(spell, line);
            }
            else
            {
                Spell spellToCast = null;

                if (line.KeyName == GlobalSpellsLines.Mob_Spells)
                {
                    // NPC spells will get the level equal to their caster
                    spellToCast = (Spell)spell.Clone();
                    spellToCast.Level = Level;
                }
                else
                {
                    spellToCast = spell;
                }

                base.CastSpell(spellToCast, line);
            }
        }

        /// <summary>
        /// Cast a spell with LOS check to a player
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="line"></param>
        /// <returns>Whether the spellcast started successfully</returns>
        public override bool CastSpell(Spell spell, SpellLine line)
        {
            if (IsIncapacitated)
                return false;

            if ((m_runningSpellHandler != null && !spell.IsInstantCast) || TempProperties.getProperty<Spell>(LOSCURRENTSPELL, null) != null)
                return false;

            bool casted = false;
            Spell spellToCast = null;

            if (line.KeyName == GlobalSpellsLines.Mob_Spells)
            {
                // NPC spells will get the level equal to their caster
                spellToCast = (Spell)spell.Clone();
                spellToCast.Level = Level;
            }
            else
            {
                spellToCast = spell;
            }

            // Let's do a few checks to make sure it doesn't just wait on the LOS check
            int tempProp = TempProperties.getProperty<int>(LOSTEMPCHECKER);

            if (tempProp <= 0)
            {
                GamePlayer LOSChecker = TargetObject as GamePlayer;

                if (LOSChecker == null && this is GamePet pet)
                {
                    if (pet.Owner is GamePlayer player)
                        LOSChecker = player;
                    else if (pet.Owner is CommanderPet petComm && petComm.Owner is GamePlayer owner)
                        LOSChecker = owner;
                }

                if (LOSChecker == null)
                {
                    foreach (GamePlayer ply in GetPlayersInRadius(350))
                    {
                        if (ply != null)
                        {
                            LOSChecker = ply;
                            break;
                        }
                    }
                }

                if (LOSChecker == null)
                {
                    TempProperties.setProperty(LOSTEMPCHECKER, 0);
                    casted = base.CastSpell(spellToCast, line);
                }
                else
                {
                    TempProperties.setProperty(LOSTEMPCHECKER, 10);
                    TempProperties.setProperty(LOSCURRENTSPELL, spellToCast);
                    TempProperties.setProperty(LOSCURRENTLINE, line);
                    TempProperties.setProperty(LOSSPELLTARGET, TargetObject);
                    LOSChecker.Out.SendCheckLOS(LOSChecker, this, new CheckLOSResponse(StartSpellAttackCheckLOS));
                    casted = true;
                }
            }
            else
                TempProperties.setProperty(LOSTEMPCHECKER, tempProp - 1);

            return casted;
        }

        public void StartSpellAttackCheckLOS(GamePlayer player, ushort response, ushort targetOID)
        {
            SpellLine line = TempProperties.getProperty<SpellLine>(LOSCURRENTLINE, null);
            Spell spell = TempProperties.getProperty<Spell>(LOSCURRENTSPELL, null);
            GameObject target = TempProperties.getProperty<GameObject>(LOSSPELLTARGET, null);
            GameObject lasttarget = TargetObject;

            TempProperties.removeProperty(LOSSPELLTARGET);
            TempProperties.removeProperty(LOSTEMPCHECKER);
            TempProperties.removeProperty(LOSCURRENTLINE);
            TempProperties.removeProperty(LOSCURRENTSPELL);
            TempProperties.setProperty(LOSTEMPCHECKER, 0);

            if ((response & 0x100) == 0x100 && line != null && spell != null)
            {
                TargetObject = target;

                GameLiving living = TargetObject as GameLiving;

                if (living != null && living.EffectList.GetOfType<NecromancerShadeEffect>() != null)
                {
                    if (living is GamePlayer && (living as GamePlayer).ControlledBrain != null)
                    {
                        TargetObject = (living as GamePlayer).ControlledBrain.Body;
                    }
                }

                base.CastSpell(spell, line);
                TargetObject = lasttarget;
            }
            else
            {
                Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.TargetNotInView));
            }
        }

        #endregion

        #region Notify

        /// <summary>
        /// Handle event notifications
        /// </summary>
        /// <param name="e">The event</param>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguements</param>
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            ABrain brain = Brain;
            if (brain != null)
                brain.Notify(e, sender, args);

            if (e == GameNPCEvent.ArriveAtTarget)
            {
                if (IsReturningToSpawnPoint)
                {
                    TurnTo(SpawnHeading);
                    IsReturningToSpawnPoint = false;
                }
            }
        }

        /// <inheritdoc />
        public override string GetPersonalizedName(GameObject obj)
        {
            if (Brain is IControlledBrain { Owner: not null } controlledBrain)
                return controlledBrain.Owner.GetPersonalizedName(obj);
            return base.GetPersonalizedName(obj);
        }

        System.Timers.Timer TriggerPlayerLostTimer = new System.Timers.Timer(20000);

        /// <summary>
        /// Cooldown for triggers, triggers cannot run while this is not finished
        /// </summary>
        private System.Timers.Timer TriggerCooldownTimer = new();
        GamePlayer TriggerPlayer;

        string BeforeTriggerPathID = "";

        private void TriggerPlayerLostTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TriggerPlayer == null)
                return;
            if (TriggerPlayer.IsWithinRadius(this, 1500))
            {
                return;
            }
            // reset if player lost
            foreach (MobXAmbientBehaviour ambientText in ambientTexts)
            {
                if (ambientText.InteractTriggerTimer != null)
                    ambientText.InteractTriggerTimer.Stop();
            }
            RemoveFromWorld();
            LoadFromDatabase(GameServer.Database.FindObjectByKey<Mob>(InternalID));
            AddToWorld();
            CurrentWayPoint = null;
            PathID = BeforeTriggerPathID;
            TriggerPlayer = null;
        }

        /// <summary>
        /// Handle ResponseTriggers and launch all ambient sentences that countain it 
        /// </summary>
        /// <param name="action">The trigger action</param>
        /// <param name="npc">The NPC to handle the trigger for</param>
        /// <param name="preChosenID">The ID of the sentence to fire</param>
        /// <param name="responseTrigger">The response trigger of the sentence to fire</param>
        /// <param name="useTimer">Whether to use the timer or not</param>
        public void FireAllResponseTriggers(eAmbientTrigger trigger, GameLiving living = null, string responseTrigger = null, bool useTimer = true)
        {
            if (IsSilent || ambientTexts == null || ambientTexts.Count == 0)
            {
                return;
            }

            if (responseTrigger != null)
            {
                List<MobXAmbientBehaviour> mxa = (from i in ambientTexts where i.ResponseTrigger == responseTrigger select i).ToList();
                foreach (MobXAmbientBehaviour m in mxa)
                {
                    FireAmbientSentence(trigger, living, m, useTimer);
                }
            }
            else
            {
                FireAmbientSentence(trigger, living, null, useTimer);
            }
        }
        /// <summary>
        /// Handle triggers for ambient sentences
        /// </summary>
        /// <param name="action">The trigger action</param>
        /// <param name="npc">The NPC to handle the trigger for</param>
        /// <param name="preChosen">The sentence to fire</param>
        /// <param name="useTimer">Whether to use the timer or not</param>
        public void FireAmbientSentence(eAmbientTrigger trigger, GameLiving living = null, MobXAmbientBehaviour preChosen = null, bool useTimer = true)
        {
            // TODO: clean this up
            if (IsSilent)
            {
                return;
            }
            if (TriggerCooldownTimer is { Enabled: true })
            {
                return;
            }
            if (trigger == eAmbientTrigger.interact && living == null)
            {
                return;
            }
            TriggerPlayerLostTimer.Stop();
            MobXAmbientBehaviour chosen;
            if (preChosen != null)
            {
                chosen = preChosen;
            }
            else
            {
                if (ambientTexts == null || ambientTexts.Count == 0)
                    return;
                // select all triggers with no response associated
                List<MobXAmbientBehaviour> mxa;
                if (trigger == eAmbientTrigger.interact)
                {
                    mxa = ambientTexts.Where(i => i.Trigger == trigger.ToString() && string.IsNullOrEmpty(i.ResponseTrigger)).ToList();
                }
                else
                {
                    mxa = ambientTexts.Where(i => i.Trigger == trigger.ToString()).ToList();
                }
                if (mxa.Count == 0) return;

                // grab random
                chosen = mxa[Util.Random(mxa.Count - 1)];
            }

            if (chosen.TimerBetweenTriggers > 0)
            {
                TriggerCooldownTimer.Interval = chosen.TimerBetweenTriggers;
                TriggerCooldownTimer.AutoReset = false;
                TriggerCooldownTimer.Start();
            }
            if (useTimer && trigger == eAmbientTrigger.interact && chosen.InteractTimerDelay > 0)
            {
                if (chosen.InteractTriggerTimer != null)
                {
                    chosen.InteractTriggerTimer.Stop();
                    chosen.InteractTriggerTimer.Dispose();
                }
                chosen.InteractTriggerTimer = new System.Timers.Timer();
                chosen.InteractTriggerTimer.Interval = chosen.InteractTimerDelay * 1000;
                chosen.InteractTriggerTimer.Start();
                chosen.InteractTriggerTimer.Elapsed += (sender, e) =>
                {
                    if (chosen.InteractTriggerTimer != null)
                    {
                        chosen.InteractTriggerTimer.Stop();
                        chosen.InteractTriggerTimer.Dispose();
                    }
                    FireAmbientSentence(trigger, living, chosen, false);
                };
                TriggerPlayer = living as GamePlayer;
                TriggerPlayerLostTimer.Start();
                return;
            }

            if (chosen.HP < 1 && chosen.Chance > 0)
                if (!Util.Chance(chosen.Chance))
                    return;
                else if (chosen.HP > 0 && HealthPercent > chosen.HP)
                    return;
                else if (chosen.HP > 0 && chosen.Chance > 0 && !Util.Chance(chosen.Chance))
                    return;

            //WalkToPath
            if (!string.IsNullOrEmpty(chosen.WalkToPath))
            {
                CurrentWayPoint = null;
                DBPathPoint pathPoint = DOLDB<DBPathPoint>.SelectObject(DB.Column(nameof(DBPathPoint.PathID)).IsEqualTo(chosen.WalkToPath));
                if (pathPoint == null)
                    return;
                CurrentWayPoint = new PathPoint(pathPoint.X, pathPoint.Y, pathPoint.Z, pathPoint.MaxSpeed, ePathType.Once);
                BeforeTriggerPathID = PathID;
                PathID = pathPoint.PathID;
                MoveOnPath(MaxSpeed);
                TriggerPlayer = living as GamePlayer;
                TriggerPlayerLostTimer.Start();
            }

            //Yell
            if (chosen.Yell > 0)
            {
                foreach (GameNPC npc in GetNPCsInRadius(chosen.Yell))
                {
                    if (!(npc is GameNPC))
                        continue;
                    if ((string.IsNullOrEmpty(GuildName) && (Faction == null || string.IsNullOrEmpty(Faction.Name)))
                    || (string.IsNullOrEmpty(GuildName) && npc.Faction != null && Faction != null && npc.Faction.Name == Faction.Name)
                    || (GuildName != null && npc.GuildName == GuildName && (Faction == null || string.IsNullOrEmpty(Faction.Name)))
                    || (GuildName != null && npc.GuildName == GuildName && npc.Faction != null && Faction != null && npc.Faction.Name == Faction.Name))
                    {
                        npc.StartAttack(living);
                    }
                }
            }

            //NbUse
            if (chosen.NbUse > 0 && (chosen.Spell == 0 || (chosen.Spell > 0 && !IsCasting)))
            {
                if (ambientXNbUse.ContainsKey(chosen))
                {
                    ambientXNbUse[chosen]++;
                }
                else
                {
                    ambientXNbUse.Add(chosen, 1);
                }
                if (ambientXNbUse[chosen] > chosen.NbUse)
                    return;
            }

            // DamageTypeRepeate
            if (trigger == eAmbientTrigger.immunised)
            {
                hasImunity = true;
                if (LastDamageType != eDamageType.GM)
                    ImunityDomage = LastDamageType;
                LastDamageType = eDamageType.GM;
                DamageTypeCounter = 0;
            }

            //TriggerTimer
            if (chosen.TriggerTimer > 0)
            {
                if (ambientTextTimer != null)
                    ambientTextTimer.Stop();
                ambientTextTimer = new RegionTimer(this, new RegionTimerCallback(AmbientTextTypeCallback));
                ambientTextTimer.Start(chosen.TriggerTimer * 1000);
            }

            string controller = string.Empty;
            if (Brain is IControlledBrain)
            {
                GamePlayer playerOwner = (Brain as IControlledBrain).GetPlayerOwner();
                if (playerOwner != null)
                    controller = playerOwner.Name;
            }

            if (chosen.Spell > 0)
            {
                DBSpell dbspell = GameServer.Database.SelectObject<DBSpell>(DB.Column("SpellID").IsEqualTo(chosen.Spell));
                if (dbspell != null)
                {
                    /*foreach (GamePlayer pl in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if(!(living is GamePlayer) || (living as GamePlayer != pl))
                            pl.Out.SendSpellEffectAnimation(this, living, (ushort)dbspell.ClientEffect, 0, false, 1);
                    }
                    if (living is GamePlayer player)
                        player.Out.SendSpellEffectAnimation(this, player, (ushort)dbspell.ClientEffect, 0, false, 1);
                    living.TakeDamage(this, eDamageType.Energy, (int)dbspell.Damage, 0);*/
                    Spell spell = new Spell(dbspell, Level);
                    ISpellHandler dd = CreateSpellHandler(this, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    dd.IgnoreDamageCap = true;
                    dd.StartSpell(living, true);
                }
            }

            // ChangeFlag
            if (chosen.ChangeFlag > 0 && !Flags.HasFlag((eFlags)chosen.ChangeFlag))
            {
                tempoarallyFlags = Flags | (eFlags)chosen.ChangeFlag;
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendNPCCreate(this);
                    if (m_inventory != null)
                        player.Out.SendLivingEquipmentUpdate(this);
                }
            }

            // ChangeBrain
            if (!string.IsNullOrEmpty(chosen.ChangeBrain))
            {
                foreach (Assembly script in ScriptMgr.GameServerScripts)
                {
                    ABrain newBrain = (ABrain)script.CreateInstance(chosen.ChangeBrain, false);
                    if (newBrain != null && newBrain.GetType() != Brain.GetType())
                    {
                        temporallyBrain = newBrain;
                        AddBrain(temporallyBrain);
                        break;
                    }
                }
            }

            // ChangeNPCTemplate
            if (chosen.ChangeNPCTemplate > 0 && (temporallyTemplate == null || chosen.ChangeNPCTemplate != temporallyTemplate.TemplateId))
            {
                foreach (Assembly script in ScriptMgr.GameServerScripts)
                {
                    INpcTemplate newTemplate = NpcTemplateMgr.GetTemplate(chosen.ChangeNPCTemplate);
                    if (newTemplate != null)
                    {
                        temporallyTemplate = newTemplate;
                        if (chosen.ChangeEffect > 0)
                        {
                            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                player.Out.SendSpellEffectAnimation(this, this, (ushort)chosen.ChangeEffect, 0, false, 1);
                        }
                        LoadTemplate(temporallyTemplate);
                        BroadcastLivingEquipmentUpdate();
                        break;
                    }
                }
            }

            // CallAreaeffect
            if (chosen.CallAreaeffectID > 0)
            {
                GameCommand command = ScriptMgr.GuessCommand("&areaeffect");
                string[] param = new string[]
                {
                    "/areaeffect",
                    "callareaeffect",
                    chosen.CallAreaeffectID.ToString()
                };
                command.m_cmdHandler.OnCommand(null, param);
            }

            // MobtoTpPoint
            if (chosen.MobtoTPpoint > 0)
            {
                if (chosen.TPeffect > 0)
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendSpellEffectAnimation(this, this, (ushort)chosen.TPeffect, 0, false, 1);
                    }
                if (TPPoint != null)
                {
                    TPPoint newTPPoint = TPPoint.GetNextTPPoint();
                    if (TPPoint.DbTPPoint.ObjectId != newTPPoint.DbTPPoint.ObjectId)
                    {
                        TPPoint = newTPPoint;
                        MoveTo(TPPoint.Region, (float)TPPoint.Position.X, (float)TPPoint.Position.Y, (float)TPPoint.Position.Z, TPPoint.GetHeading(TPPoint));
                    }
                }
                else
                {
                    TPPoint = TeleportMgr.LoadTP(chosen.MobtoTPpoint);
                    MoveTo(TPPoint.Region, (float)TPPoint.Position.X, (float)TPPoint.Position.Y, (float)TPPoint.Position.Z, TPPoint.GetHeading(TPPoint));
                }
            }

            // PlayertoTpPoint
            if (chosen.PlayertoTPpoint > 0)
            {
                if (chosen.TPeffect > 0)
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendSpellEffectAnimation(this, player, (ushort)chosen.TPeffect, 0, false, 1);
                if (TPPoint != null)
                {
                    TPPoint newTPPoint = TPPoint.GetNextTPPoint();
                    if (TPPoint.DbTPPoint.ObjectId != newTPPoint.DbTPPoint.ObjectId)
                    {
                        TPPoint = newTPPoint;
                        living.MoveTo(TPPoint.Region, (float)TPPoint.Position.X, (float)TPPoint.Position.Y, (float)TPPoint.Position.Z, TPPoint.GetHeading(TPPoint));
                    }
                }
                else
                {
                    TPPoint = TeleportMgr.LoadTP(chosen.PlayertoTPpoint);
                    living.MoveTo(TPPoint.Region, (float)TPPoint.Position.X, (float)TPPoint.Position.Y, (float)TPPoint.Position.Z, TPPoint.GetHeading(TPPoint));
                }
            }

            string text = chosen.Text.Replace("{sourcename}", Name).Replace("{targetname}", living == null ? string.Empty : living.Name).Replace("{controller}", controller);

            if (chosen.Emote != 0)
            {
                Emote((eEmote)chosen.Emote);
            }

            // issuing text
            if (living is GamePlayer)
                text = text.Replace("{class}", (living as GamePlayer).CharacterClass.Name).Replace("{race}", (living as GamePlayer).RaceName);
            if (living is GameNPC)
                text = text.Replace("{class}", "NPC").Replace("{race}", "NPC");

            if (trigger == eAmbientTrigger.interact)
            {
                // for interact text we pop up a window
                (living as GamePlayer).Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return;
            }

            // broadcasted , yelled or talked ?
            if (chosen.Voice.StartsWith("b"))
            {
                foreach (GamePlayer player in CurrentRegion.GetPlayersInRadius(Position, 25000, false, false))
                {
                    player.Out.SendMessage(text, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                }
                return;
            }
            if (chosen.Voice.StartsWith("y"))
            {
                Yell(text);
                return;
            }
            Say(text);
        }
        #endregion

        #region ControlledNPCs

        public override void SetControlledBrain(IControlledBrain controlledBrain)
        {
            if (ControlledBrain == null)
                InitControlledBrainArray(1);

            ControlledBrain = controlledBrain;
        }
        /// <summary>
        /// Gets the controlled object of this NPC
        /// </summary>
        public override IControlledBrain ControlledBrain
        {
            get
            {
                if (m_controlledBrain == null) return null;
                return m_controlledBrain[0];
            }
        }

        /// <summary>
        /// Gets the controlled array of this NPC
        /// </summary>
        public IControlledBrain[] ControlledNpcList
        {
            get { return m_controlledBrain; }
        }

        /// <summary>
        /// Adds a pet to the current array of pets
        /// </summary>
        /// <param name="controlledNpc">The brain to add to the list</param>
        /// <returns>Whether the pet was added or not</returns>
        public virtual bool AddControlledNpc(IControlledBrain controlledNpc)
        {
            return true;
        }

        /// <summary>
        /// Removes the brain from
        /// </summary>
        /// <param name="controlledNpc">The brain to find and remove</param>
        /// <returns>Whether the pet was removed</returns>
        public virtual bool RemoveControlledNpc(IControlledBrain controlledNpc)
        {
            return true;
        }

        #endregion

        /// <summary>
        /// Whether this NPC is available to add on a fight.
        /// </summary>
        public virtual bool IsAvailable
        {
            get { return !(Brain is IControlledBrain) && !InCombat; }
        }

        /// <summary>
        /// Whether this NPC is aggressive.
        /// </summary>
        public virtual bool IsAggressive
        {
            get
            {
                ABrain brain = Brain;
                return (brain == null) ? false : (brain is IOldAggressiveBrain);
            }
        }

        /// <summary>
        /// Whether this NPC is a friend or not.
        /// </summary>
        /// <param name="npc">The NPC that is checked against.</param>
        /// <returns></returns>
        public bool IsFriend(GameNPC npc)
        {
            if (npc.Brain is IControlledBrain)
                return GameServer.ServerRules.IsSameRealm(this, npc, true);
            if (Faction == null && npc.Faction == null)
                return npc.Name == Name || (!string.IsNullOrEmpty(npc.GuildName) && npc.GuildName == GuildName);
            return npc.Faction == Faction || (Faction?.FriendFactions?.Contains(npc.Faction) ?? false);
        }

        /// <summary>
        /// Broadcast loot to the raid.
        /// </summary>
        /// <param name="dropMessages">List of drop messages to broadcast.</param>
        protected virtual void BroadcastLoot(ArrayList droplist)
        {
            if (droplist.Count > 0)
            {
                String lastloot;
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
                    lastloot = "";
                    foreach (string str in droplist)
                    {
                        // Suppress identical messages (multiple item drops).
                        if (str != lastloot)
                        {
                            player.Out.SendMessage(String.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.DropLoot.Drops",
                                GetName(0, true, player.Client.Account.Language, this), str)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
                            lastloot = str;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Gender of this NPC.
        /// </summary>
        public override eGender Gender { get; set; }

        public GameNPC Copy()
        {
            return Copy(null);
        }


        /// <summary>
        /// Create a copy of the GameNPC
        /// </summary>
        /// <param name="copyTarget">A GameNPC to copy this GameNPC to (can be null)</param>
        /// <returns>The GameNPC this GameNPC was copied to</returns>
        public GameNPC Copy(GameNPC copyTarget)
        {
            if (copyTarget == null)
                copyTarget = new GameNPC();

            copyTarget.InternalID = InternalID;
            copyTarget.TranslationId = TranslationId;
            copyTarget.BlockChance = BlockChance;
            copyTarget.BodyType = BodyType;
            copyTarget.CanUseLefthandedWeapon = CanUseLefthandedWeapon;
            copyTarget.Charisma = Charisma;
            copyTarget.Constitution = Constitution;
            copyTarget.CurrentRegion = CurrentRegion;
            copyTarget.Dexterity = Dexterity;
            copyTarget.Empathy = Empathy;
            copyTarget.Endurance = Endurance;
            copyTarget.EquipmentTemplateID = EquipmentTemplateID;
            copyTarget.EvadeChance = EvadeChance;
            copyTarget.Faction = Faction;
            copyTarget.Flags = Flags;
            copyTarget.GuildName = GuildName;
            copyTarget.ExamineArticle = ExamineArticle;
            copyTarget.MessageArticle = MessageArticle;
            copyTarget.Heading = Heading;
            copyTarget.Intelligence = Intelligence;
            copyTarget.IsCloakHoodUp = IsCloakHoodUp;
            copyTarget.IsCloakInvisible = IsCloakInvisible;
            copyTarget.IsHelmInvisible = IsHelmInvisible;
            copyTarget.LeftHandSwingChance = LeftHandSwingChance;
            copyTarget.Level = Level;
            copyTarget.LoadedFromScript = LoadedFromScript;
            copyTarget.MaxSpeedBase = MaxSpeedBase;
            copyTarget.MeleeDamageType = MeleeDamageType;
            copyTarget.Model = Model;
            copyTarget.Name = Name;
            copyTarget.Suffix = Suffix;
            copyTarget.NPCTemplate = NPCTemplate;
            copyTarget.ParryChance = ParryChance;
            copyTarget.PathID = PathID;
            copyTarget.PathingNormalSpeed = PathingNormalSpeed;
            copyTarget.Quickness = Quickness;
            copyTarget.Piety = Piety;
            copyTarget.Race = Race;
            copyTarget.Realm = Realm;
            copyTarget.RespawnInterval = RespawnInterval;
            copyTarget.RoamingRange = RoamingRange;
            copyTarget.Size = Size;
            copyTarget.SaveInDB = SaveInDB;
            copyTarget.Strength = Strength;
            copyTarget.TetherRange = TetherRange;
            copyTarget.MaxDistance = MaxDistance;
            copyTarget.Position = Position;
            copyTarget.OwnerID = OwnerID;
            copyTarget.PackageID = PackageID;

            if (Abilities != null && Abilities.Count > 0)
            {
                foreach (Ability targetAbility in Abilities.Values)
                {
                    if (targetAbility != null)
                        copyTarget.AddAbility(targetAbility);
                }
            }

            ABrain brain = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                brain = (ABrain)assembly.CreateInstance(Brain.GetType().FullName, true);
                if (brain != null)
                    break;
            }

            if (brain == null)
            {
                log.Warn("GameNPC.Copy():  Unable to create brain:  " + Brain.GetType().FullName + ", using StandardMobBrain.");
                brain = new StandardMobBrain();
            }

            StandardMobBrain newBrainSMB = brain as StandardMobBrain;
            StandardMobBrain thisBrainSMB = this.Brain as StandardMobBrain;

            if (newBrainSMB != null && thisBrainSMB != null)
            {
                newBrainSMB.AggroLevel = thisBrainSMB.AggroLevel;
                newBrainSMB.AggroRange = thisBrainSMB.AggroRange;
            }

            copyTarget.SetOwnBrain(brain);

            if (Inventory != null && Inventory.Count > 0)
            {
                GameNpcInventoryTemplate inventoryTemplate = Inventory as GameNpcInventoryTemplate;

                if (inventoryTemplate != null)
                    copyTarget.Inventory = inventoryTemplate.CloneTemplate();
            }

            if (Spells != null && Spells.Count > 0)
                copyTarget.Spells = new List<Spell>(Spells.Cast<Spell>());

            if (Styles != null && Styles.Count > 0)
                copyTarget.Styles = new List<Style>(Styles);

            if (copyTarget.Inventory != null)
                copyTarget.SwitchWeapon(ActiveWeaponSlot);

            return copyTarget;
        }

        /// <summary>
        /// Constructs a NPC
        /// NOTE: Most npcs are generated as GameLiving objects and then used as GameNPCs when needed.
        /// 	As a result, this constructor is rarely called.
        /// </summary>
        public GameNPC()
            : this(new StandardMobBrain())
        {
            TriggerPlayerLostTimer.Elapsed += TriggerPlayerLostTimer_Elapsed;
        }

        public GameNPC(ABrain defaultBrain) : base()
        {
            Level = 1;
            m_health = MaxHealth;
            m_Realm = 0;
            m_name = "new mob";
            m_model = 408;
            //Fill the living variables
            //			CurrentSpeed = 0; // cause position addition recalculation
            MaxSpeedBase = 200;
            GuildName = "";

            m_brainSync = m_brains.SyncRoot;
            m_followTarget = new WeakRef(null);

            m_size = 50; //Default size
            TargetPosition = Vector3.Zero;
            m_followMinDist = 100;
            m_followMaxDist = 3000;
            m_flags = 0;
            m_maxdistance = 0;
            m_roamingRange = 0; // default to non roaming - tolakram
            m_ownerID = "";

            //m_factionName = "";
            LinkedFactions = new ArrayList(1);
            if (m_ownBrain == null)
            {
                m_ownBrain = defaultBrain;
                m_ownBrain.Body = this;
            }
            TriggerPlayerLostTimer.Elapsed += TriggerPlayerLostTimer_Elapsed;
        }

        /// <summary>
        /// create npc from template
        /// NOTE: Most npcs are generated as GameLiving objects and then used as GameNPCs when needed.
        /// 	As a result, this constructor is rarely called.
        /// </summary>
        /// <param name="template">template of generator</param>
        public GameNPC(INpcTemplate template)
            : this()
        {
            if (template == null) return;

            // When creating a new mob from a template, we have to get all values from the template
            if (template is NpcTemplate npcTemplate)
                npcTemplate.ReplaceMobValues = true;

            TriggerPlayerLostTimer.Elapsed += TriggerPlayerLostTimer_Elapsed;
            LoadTemplate(template);
        }

        // camp bonus
        private double m_campBonus = 1;
        /// <summary>
        /// gets/sets camp bonus experience this gameliving grants
        /// </summary>
        public virtual double CampBonus
        {
            get
            {
                return m_campBonus;
            }
            set
            {
                m_campBonus = value;
            }
        }

        public virtual List<string> CustomInfo()
        {
            return new List<string>();
        }
    }
}
