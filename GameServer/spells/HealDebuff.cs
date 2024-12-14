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

using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.Language;
using DOL.GS.Effects;
using DOL.GS.ServerProperties;

namespace DOL.GS.Spells
{
    [SpellHandler("HealDebuff")]
    public class HealDebuffSpellHandler : SpellHandler
    {
        public HealDebuffSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        /// <inheritdoc />
        public override int CalculateSpellResistChance(GameLiving target)
        {
            int totalResistChance = base.CalculateSpellResistChance(target);

            // 1. DebuffImmunity
            var immunityEffect = SpellHandler.FindEffectOnTarget(target, "DebuffImmunity") as DebuffImmunityEffect;
            if (immunityEffect != null)
            {
                totalResistChance += immunityEffect.AdditionalResistChance;
            }

            // 2. MythicalDebuffResistChance
            int mythicalResistChance = 0;
            if (target is GamePlayer gamePlayer)
            {
                mythicalResistChance = gamePlayer.GetModified(eProperty.MythicalDebuffResistChance);
                totalResistChance += mythicalResistChance;
            }
            return totalResistChance;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (SpellHandler.FindEffectOnTarget(target, "Damnation") != null)
            {
                if (Caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "Damnation.Target.Resist", player.GetPersonalizedName(target)), eChatType.CT_SpellResisted);

                SendSpellResistAnimation(target);
                return true;
            }
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GameLiving target = effect.Owner;

            if (SpellHandler.FindEffectOnTarget(target, "Damnation") != null)
            {
                return;
            }

            base.OnEffectStart(effect);

            MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "HealDebuffSpellHandler.EffectStart.Target", effect.Spell.Name), eChatType.CT_Important);
            MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "HealDebuffSpellHandler.EffectStart.Area", Caster.GetPersonalizedName(target), effect.Spell.Name), eChatType.CT_SpellExpires);

            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = target.CurrentRegion.Time;
                Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;
                Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
            }
            if (target is GameNPC)
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (!noMessages)
            {
                GameLiving target = effect.Owner;

                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "HealDebuffSpellHandler.EffectExpires.Target"), eChatType.CT_Spell);
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "HealDebuffSpellHandler.EffectExpires.Area", Caster.GetPersonalizedName(target)), eChatType.CT_SpellExpires);
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public override string ShortDescription
        {
            get
            {
                string language = Properties.SERV_LANGUAGE;
                string description = LanguageMgr.GetTranslation(language, "SpellDescription.HealDebuff.MainDescription", Spell.Value);
                return description;
            }
        }
    }
}