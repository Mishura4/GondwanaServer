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
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Pet summon spell handler
    ///
    /// Spell.LifeDrainReturn is used for pet ID.
    ///
    /// Spell.Value is used for hard pet level cap
    /// Spell.Damage is used to set pet level:
    /// less than zero is considered as a percent (0 .. 100+) of target level;
    /// higher than zero is considered as level value.
    /// Resulting value is limited by the Byte field type.
    /// Spell.DamageType is used to determine which type of pet is being cast:
    /// 0 = melee
    /// 1 = healer
    /// 2 = mage
    /// 3 = debuffer
    /// 4 = Buffer
    /// 5 = Range
    /// </summary>
    [SpellHandler("SummonMinion")]
    public class SummonMinionHandler : SummonSpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public SummonMinionHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (Caster is GamePlayer && ((GamePlayer)Caster).ControlledBrain == null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "SummonMinionHandler.CheckBeginCast.Text1"), eChatType.CT_SpellResisted);
                return false;
            }

            if (Caster is GamePlayer && (((GamePlayer)Caster).ControlledBrain.Body.ControlledNpcList == null || ((GamePlayer)Caster).ControlledBrain.Body.PetCount >= ((GamePlayer)Caster).ControlledBrain.Body.ControlledNpcList.Length))
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "SummonMinionHandler.CheckBeginCast.Text2"), eChatType.CT_SpellResisted);

                return false;
            }
            return base.CheckBeginCast(selectedTarget, quiet);
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (Caster == null || Caster.ControlledBrain == null)
                return false;

            GameNPC temppet = Caster.ControlledBrain.Body;
            //Lets let NPC's able to cast minions.  Here we make sure that the Caster is a GameNPC
            //and that m_controlledNpc is initialized (since we aren't thread safe).
            if (temppet == null)
            {
                if (Caster is GameNPC)
                {
                    temppet = (GameNPC)Caster;
                    //We'll give default NPCs 2 minions!
                    if (temppet.ControlledNpcList == null)
                        temppet.InitControlledBrainArray(2);
                }
                else
                    return false;
            }
            
            if (!base.ApplyEffectOnTarget(target, effectiveness))
                return false;

            if (m_pet.Brain is BDPetBrain brain && !brain.MinionsAssisting)
                brain.SetAggressionState(eAggressionState.Passive);

            // Assign weapons
            if (m_pet is BDSubPet subPet)
                switch (subPet.Brain)
                {
                    case BDArcherBrain archer:
                        subPet.MinionGetWeapon(CommanderPet.eWeaponType.OneHandSword);
                        subPet.MinionGetWeapon(CommanderPet.eWeaponType.Bow);
                        break;
                    case BDDebufferBrain debuffer:
                        subPet.MinionGetWeapon(CommanderPet.eWeaponType.OneHandHammer);
                        break;
                    case BDBufferBrain buffer:
                    case BDCasterBrain caster:
                        subPet.MinionGetWeapon(CommanderPet.eWeaponType.Staff);
                        break;
                    case BDMeleeBrain melee:
                        subPet.MinionGetWeapon((CommanderPet.eWeaponType)Util.Random((int)CommanderPet.eWeaponType.TwoHandAxe, (int)CommanderPet.eWeaponType.TwoHandSword));
                        break;
                }
            return true;
        }

        protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments)
        {
            GameNPC pet = sender as GameNPC;
            if (pet == null)
                return;

            GameEventMgr.RemoveHandler(pet, GameLivingEvent.PetReleased, new DOLEventHandler(OnNpcReleaseCommand));

            GameSpellEffect effect = FindEffectOnTarget(pet, this);
            if (effect != null)
                effect.Cancel(false);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if ((effect.Owner is BDPet) && ((effect.Owner as BDPet)!.Brain is IControlledBrain) && (((effect.Owner as BDPet)!.Brain as IControlledBrain)!.Owner is CommanderPet))
            {
                BDPet pet = effect.Owner as BDPet;
                CommanderPet commander = (pet!.Brain as IControlledBrain)!.Owner as CommanderPet;
                commander!.RemoveControlledNpc(pet.Brain as IControlledBrain);
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            IControlledBrain controlledBrain = null;
            BDSubPet.SubPetType type = (BDSubPet.SubPetType)(byte)this.Spell.DamageType;
            owner = owner.ControlledBrain.Body;

            switch (type)
            {
                case BDSubPet.SubPetType.Melee:
                    controlledBrain = new BDMeleeBrain(owner);
                    break;
                case BDSubPet.SubPetType.Healer:
                    controlledBrain = new BDHealerBrain(owner);
                    break;
                case BDSubPet.SubPetType.Caster:
                    controlledBrain = new BDCasterBrain(owner);
                    break;
                case BDSubPet.SubPetType.Debuffer:
                    controlledBrain = new BDDebufferBrain(owner);
                    break;
                case BDSubPet.SubPetType.Buffer:
                    controlledBrain = new BDBufferBrain(owner);
                    break;
                case BDSubPet.SubPetType.Archer:
                    controlledBrain = new BDArcherBrain(owner);
                    break;
                default:
                    controlledBrain = new ControlledNpcBrain(owner);
                    break;
            }

            return controlledBrain;
        }

        protected override GamePet GetGamePet(INpcTemplate template)
        {
            return new BDSubPet(template);
        }

        protected override void SetBrainToOwner(IControlledBrain brain)
        {
            Caster.ControlledBrain.Body.AddControlledNpc(brain);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var delve = new List<string>();
                delve.Add(String.Format(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "SummonMinionHandler.DelveInfo.Text1", Spell.Target)));
                delve.Add(String.Format(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "SummonMinionHandler.DelveInfo.Text2", Math.Abs(Spell.Power))));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "SummonMinionHandler.DelveInfo.Text3", (Spell.CastTime / 1000).ToString("0.0## " + LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "Effects.DelveInfo.Seconds"))));
                return delve;
            }
        }
    }
}
