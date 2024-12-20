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
/*
 * [Ganrod] Nidel 2008-07-08
 * - Corrections for Bomber actions.
 */
using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("Bomber")]
    public class BomberSpellHandler : SummonSpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        const string BOMBERTARGET = "bombertarget";

        public BomberSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { m_isSilent = true; }

        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (Spell.SubSpellID == 0)
            {
                MessageToCaster("SPELL NOT IMPLEMENTED: CONTACT GM", eChatType.CT_Important);
                return false;
            }

            return base.CheckBeginCast(selectedTarget, quiet);
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (!base.ApplyEffectOnTarget(target, effectiveness))
                return false;
            
            m_pet.Level = Caster.Level; // No bomber class to override SetPetLevel() in, so set level here
            m_pet.TempProperties.setProperty(BOMBERTARGET, target);
            m_pet.Name = Spell.Name;
            m_pet.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
            m_pet.FixedSpeed = true;
            m_pet.Follow(target, 5, Spell.Range * 5); // with Toa bonus, if the bomber was fired > Spell.Range base, it didnt move..
            return true;
        }

        protected override void AddHandlers()
        {
            GameEventMgr.AddHandler(m_pet, GameNPCEvent.ArriveAtTarget, BomberArriveAtTarget);
        }

        protected override void RemoveHandlers()
        {
            GameEventMgr.RemoveHandler(m_pet, GameNPCEvent.ArriveAtTarget, BomberArriveAtTarget);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new BomberBrain(owner);
        }
        protected override void SetBrainToOwner(IControlledBrain brain)
        {
        }
        protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments)
        {
        }

        private void BomberArriveAtTarget(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC bomber = sender as GameNPC;

            //[Ganrod] Nidel: Prevent NPE
            if (bomber == null || m_pet == null || bomber != m_pet)
                return;

            //[Ganrod] Nidel: Abort and delete bomber if Spell or Target is NULL
            Spell subspell = SkillBase.GetSpellByID(m_spell.SubSpellID);
            GameLiving living = m_pet.TempProperties.getProperty<object>(BOMBERTARGET, null) as GameLiving;

            if (subspell == null || living == null)
            {
                if (log.IsErrorEnabled && subspell == null)
                    log.Error("Bomber SubspellID for Bomber SpellID: " + m_spell.ID + " is not implemented yet");
                bomber.Health = 0;
                bomber.Delete();
                return;
            }

            //Andraste
            subspell.Level = m_spell.Level;
            if (living.IsWithinRadius(bomber, 350))
            {
                if (ReduceSubSpellDamage > 0)
                    subspell.Damage = subspell.Damage * ReduceSubSpellDamage / 100;
                ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(Caster, subspell, SkillBase.GetSpellLine(SpellLine.KeyName));
                spellhandler.Parent = this;
                spellhandler.StartSpell(living);
            }

            //[Ganrod] Nidel: Delete Bomber after all actions.
            bomber.Health = 0;
            bomber.Delete();
        }

        /// <summary>
        /// Do not trigger SubSpells
        /// </summary>
        /// <param name="target"></param>
        public override bool CastSubSpells(GameLiving target)
        {
            if (ServerProperties.Properties.ENABLE_SUB_SPELL_ALL_CLASS)
                return base.CastSubSpells(target);
            return false;
        }

        public int ReduceSubSpellDamage { get; set; }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Bomber.MainDescription");

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}