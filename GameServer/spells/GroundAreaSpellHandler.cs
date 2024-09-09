using DOL.AI;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
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
            public GroundAreaSpellHandler MasterSpellHandler { get; init; }

            private List<Spell> m_spells;

            /// <inheritdoc />
            public override int ThinkInterval => MasterSpellHandler.Spell.Pulse;

            public GroundAreaTurretBrain(GroundAreaSpellHandler handler)
            {
                MasterSpellHandler = handler;
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
                Body.GroundTargetPosition = Body.Position;
                var caster = MasterSpellHandler.Caster;

                if (!MasterSpellHandler.TryPulse())
                    return;
                
                foreach (Spell spell in Body.Spells)
                {
                    ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(Body, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler.Parent = MasterSpellHandler;
                    if (MasterSpellHandler.Spell.SubSpellDelay > 0)
                    {
                        new SubSpellTimer(caster, spellhandler, caster).Start(MasterSpellHandler.Spell.SubSpellDelay * 1000);
                    }
                    else
                        spellhandler.StartSpell(caster);
                }
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
            pet.Model = Spell.ClientEffect;
            pet.Name = Spell.Name;
            pet.Realm = Caster.Realm;
            pet.Owner = Caster;
            pet.Level = Caster.Level;
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
        
        private void CleanupTurret()
        {
            GameLiving trueCaster = Caster.GetController();
            if (Turret == null)
                return;

            if (trueCaster != null)
            {
                GameEventMgr.RemoveHandler(trueCaster, GameLivingEvent.RemoveFromWorld, OnRemoveFromWorld);
                trueCaster.TempProperties.removeProperty(LIVING_GROUNDEFFECT_PROPERTY);
            }
            Turret.RemoveFromWorld();
            Turret.Delete();
            DurationTimer?.Stop();
            DurationTimer = null;
        }
        private void OnRemoveFromWorld(DOLEvent e, object sender, EventArgs arguments)
        {
            if (e != GameObjectEvent.RemoveFromWorld)
                return;
                
            CleanupTurret();
        }

        /// <inheritdoc />
        public override bool StartSpell(GameLiving target, bool force = false)
        {
            m_spellTarget = Caster;
            GameLiving trueCaster = Caster.GetController();
            GameNPC turret = CreateTurret();
            turret.Position = (Caster.GroundTargetPosition == Position.Nowhere ? Caster.Position : Caster.GroundTargetPosition);
            turret.LoadedFromScript = true;
            trueCaster.TempProperties.setProperty(LIVING_GROUNDEFFECT_PROPERTY, turret);
            Turret = turret;

            GameEventMgr.AddHandler(trueCaster, GameLivingEvent.RemoveFromWorld, OnRemoveFromWorld);

            if (Spell.Duration > 0)
            {
                DurationTimer = new RegionTimer(turret, _ =>
                {
                    CleanupTurret();
                    return 0;
                }, Spell.Duration /* "Duration does not increase the duration of ground spells." https://darkageofcamelot.com/content/1122b-live-patch-notes */);
            }
            
            turret.AddToWorld();
            
            SendLaunchAnimation(Caster, 0, false, 1);
            SendHitAnimation(turret, 0, false, 1);

            return true;
        }
        
        public bool TryPulse()
        {
            if (Caster.Mana < Spell.PulsePower)
            {
                if (Spell.IsFocus)
                {
                    FocusSpellAction(null, Caster, null);
                }
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.PulsingSpellNoMana"), eChatType.CT_SpellExpires);
                CleanupTurret();
                return false;
            }
            Caster.Mana -= Spell.PulsePower;
            return true;
        }

        /// <inheritdoc />
        public override void CastSubSpells(GameLiving target)
        {
        }
    }
}
