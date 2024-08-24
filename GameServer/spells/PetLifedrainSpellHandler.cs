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
namespace DOL.spells
{
    using AI.Brain;
    using DOL.Language;
    using GS;
    using GS.PacketHandler;
    using GS.Spells;

    [SpellHandler("PetLifedrain")]
    public class PetLifedrainSpellHandler : LifedrainSpellHandler
    {
        public PetLifedrainSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (Caster == null || !(Caster is GamePet) || !(((GamePet)Caster).Brain is IControlledBrain))
                return;
            base.OnDirectEffect(target, effectiveness);
        }

        public override void StealLife(GameLiving target, AttackData ad)
        {
            if (ad == null) return;
            GamePlayer player = Caster.GetPlayerOwner();
            if (player is not { IsAlive: true }) return;
            int heal = ((ad.Damage + ad.CriticalDamage) * m_spell.LifeDrainReturn) / 100;
            if (player.IsDiseased)
            {
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Spell.LifeTransfer.TargetDiseased"), eChatType.CT_SpellResisted);
                heal >>= 1;
            }
            if (heal <= 0) return;

            heal = player.ChangeHealth(player, GameLiving.eHealthChangeType.Spell, heal);
            if (heal > 0)
            {
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.DamageSpeedDecrease.LifeSteal", heal, (heal == 1 ? "." : "s.")), eChatType.CT_Spell);
            }
            else
            {
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.DamageSpeedDecrease.NoMoreLife"), eChatType.CT_SpellResisted);
            }
        }
    }
}