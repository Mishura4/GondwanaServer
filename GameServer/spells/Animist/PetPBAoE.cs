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
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Summary description for TurretPBAoESpellHandler.
    /// </summary>
    [SpellHandler("TurretPBAoE")]
    public class PetPBAoE : DirectDamageSpellHandler
    {
        public PetPBAoE(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override bool HasPositiveEffect
        {
            get { return false; }
        }

        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (!(Caster is GamePlayer))
            {
                return false;
            }
            /*
			 * [Ganrod]Nidel: Like 1.90 EU off servers
			 * -Need Main Turret under our controle before casting.
			 * -Select automatically Main controlled Turret if player don't have target or Turret target.
			 * -Cast only on our turrets.
			 */
            if (Caster.ControlledBrain == null || Caster.ControlledBrain.Body == null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)!.Client, "PetPBAOE.CheckBeginCast.NoPet"), eChatType.CT_System);
                return false;
            }
            TurretPet target = selectedTarget as TurretPet;

            if (target == null || !Caster.IsControlledNPC(target))
            {
                target = Caster.ControlledBrain.Body as TurretPet;
            }
            return base.CheckBeginCast(target, quiet);
        }

        public override void DamageTarget(AttackData ad, bool showEffectAnimation)
        {
            if (ad.Damage > 0 && ad.Target is GameNPC)
            {
                if (!(Caster is GamePlayer)) return;
                IOldAggressiveBrain aggroBrain = ((GameNPC)ad.Target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                {
                    TurretPet turret = null;
                    if (Caster.TargetObject == null || !Caster.IsControlledNPC(Caster.TargetObject as TurretPet))
                    {
                        if (Caster.ControlledBrain != null && Caster.ControlledBrain.Body != null)
                        {
                            turret = Caster.ControlledBrain.Body as TurretPet;
                        }
                    }
                    else if (Caster.IsControlledNPC(Caster.TargetObject as TurretPet))
                    {
                        turret = Caster.TargetObject as TurretPet;
                    }

                    if (turret != null)
                    {
                        //pet will take aggro
                        AttackData turretAd = ad;
                        turretAd.Attacker = turret;
                        ad.Target.OnAttackedByEnemy(turretAd);

                        aggroBrain.AddToAggroList(turret, (ad.Damage + ad.CriticalDamage) * 3);
                    }
                    aggroBrain.AddToAggroList(Caster, ad.Damage);
                }
            }
            base.DamageTarget(ad, showEffectAnimation);
        }
    }
}