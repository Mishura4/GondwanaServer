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
using DOL.GS.Effects;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("AllStatsPercentDebuff")]
    public class AllStatsPercentDebuff : SpellHandler
    {
        protected int StrDebuff = 0;
        protected int DexDebuff = 0;
        protected int ConDebuff = 0;
        protected int EmpDebuff = 0;
        protected int QuiDebuff = 0;
        protected int IntDebuff = 0;
        protected int ChaDebuff = 0;
        protected int PieDebuff = 0;
        
        /// <inheritdoc />
        public override bool HasPositiveEffect => false;


        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            //effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += (int)m_spell.Value;
            double percentValue = (m_spell.Value) / 100;
            StrDebuff = (int)((double)effect.Owner.GetModified(eProperty.Strength) * percentValue);
            DexDebuff = (int)((double)effect.Owner.GetModified(eProperty.Dexterity) * percentValue);
            ConDebuff = (int)((double)effect.Owner.GetModified(eProperty.Constitution) * percentValue);
            EmpDebuff = (int)((double)effect.Owner.GetModified(eProperty.Empathy) * percentValue);
            QuiDebuff = (int)((double)effect.Owner.GetModified(eProperty.Quickness) * percentValue);
            IntDebuff = (int)((double)effect.Owner.GetModified(eProperty.Intelligence) * percentValue);
            ChaDebuff = (int)((double)effect.Owner.GetModified(eProperty.Charisma) * percentValue);
            PieDebuff = (int)((double)effect.Owner.GetModified(eProperty.Piety) * percentValue);


            effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += DexDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Strength] += StrDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Constitution] += ConDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Piety] += PieDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Empathy] += EmpDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Quickness] += QuiDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Intelligence] += IntDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Charisma] += ChaDebuff;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player!.Out.SendCharStatsUpdate();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            double percentValue = (m_spell.Value) / 100;

            effect.Owner.DebuffCategory[(int)eProperty.Dexterity] -= DexDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Strength] -= StrDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Constitution] -= ConDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Piety] -= PieDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Empathy] -= EmpDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Quickness] -= QuiDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Intelligence] -= IntDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Charisma] -= ChaDebuff;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player!.Out.SendCharStatsUpdate();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target.EffectList.GetOfType<AdrenalineSpellEffect>() != null)
            {
                (m_caster as GamePlayer)?.SendTranslatedMessage("Adrenaline.Target.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, m_caster.GetPersonalizedName(target));
                (target as GamePlayer)?.SendTranslatedMessage("Adrenaline.Self.Immune", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return true;
            }

            if (!base.ApplyEffectOnTarget(target, effectiveness))
                return false;
            
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
            return true;
        }
        
        public AllStatsPercentDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            return LanguageMgr.GetTranslation(delveClient, "SpellDescription.AllStatsPercentDebuff.MainDescription", Spell.Value);
        }
    }
}