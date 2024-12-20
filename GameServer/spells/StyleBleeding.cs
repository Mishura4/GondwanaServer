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
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("StyleBleeding")]
    public class StyleBleeding : SpellHandler
    {
        protected const string BLEED_VALUE_PROPERTY = "BleedValue";

        private string GetFormattedMessage(GamePlayer player, string messageKey, params object[] args)
        {
            if (messageKey.StartsWith("Languages.DBSpells."))
            {
                string translationKey = messageKey;
                string translation;

                if (LanguageMgr.TryGetTranslation(out translation, player.Client.Account.Language, translationKey, args))
                {
                    return translation;
                }
                else
                {
                    return "(Translation not found)";
                }
            }
            return string.Format(messageKey, args);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            SendEffectAnimation(effect.Owner, 0, false, 1);
            effect.Owner.TempProperties.setProperty(BLEED_VALUE_PROPERTY, (int)Spell.Damage + (int)Spell.Damage * Util.Random(25) / 100);  // + random max 25%
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            effect.Owner.TempProperties.removeProperty(BLEED_VALUE_PROPERTY);
            return 0;
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);

            string casterLanguage = (m_caster as GamePlayer)?.Client?.Account?.Language ?? "EN";
            GamePlayer ownerPlayer = effect.Owner as GamePlayer;

            if (ownerPlayer != null)
            {
                string message1 = string.IsNullOrEmpty(Spell.Message1) ? string.Empty : Spell.GetFormattedMessage1(ownerPlayer);
                MessageToLiving(effect.Owner, message1, eChatType.CT_YouWereHit);
            }
            else
            {
                string message1 = string.IsNullOrEmpty(Spell.Message1) ? string.Empty : LanguageMgr.GetTranslation(casterLanguage, Spell.Message1, effect.Owner.GetName(0, false));
                MessageToLiving(effect.Owner, message1, eChatType.CT_YouWereHit);
            }

            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (!(effect.Owner == player))
                {
                    string personalizedTargetName = player.GetPersonalizedName(effect.Owner);
                    string message2 = string.IsNullOrEmpty(Spell.Message2) ? string.Empty : Spell.GetFormattedMessage2(player, personalizedTargetName);
                    player.MessageFromArea(effect.Owner, message2, eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }
            }

            int bleedValue = effect.Owner.TempProperties.getProperty<int>(BLEED_VALUE_PROPERTY);

            AttackData ad = CalculateDamageToTarget(effect.Owner, 1.0);

            // Attacked living may modify the attack data.
            ad.Target.ModifyAttack(ad);

            SendDamageMessages(ad);

            // attacker must be null, attack result is 0x0A
            foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
            }
            // send animation before dealing damage else dead livings show no animation
            ad.Target.OnAttackedByEnemy(ad);
            ad.Attacker.DealDamage(ad);

            if (--bleedValue <= 0 || !effect.Owner.IsAlive)
                effect.Cancel(false);
            else effect.Owner.TempProperties.setProperty(BLEED_VALUE_PROPERTY, bleedValue);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new GameSpellEffect(this, CalculateEffectDuration(target, effectiveness), Spell.Frequency, effectiveness);
        }

        public override AttackData CalculateDamageToTarget(GameLiving target, double effectiveness)
        {
            int bleedValue = target.TempProperties.getProperty<int>(BLEED_VALUE_PROPERTY);

            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.DoT;
            ad.Modifier = bleedValue * ad.Target.GetResist(Spell.DamageType) / -100;
            ad.Damage = bleedValue + ad.Modifier;
            ad.DamageType = Spell.DamageType;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.SpellHandler = this;
            ad.CausesCombat = false;

            return ad;
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
            return oldeffect.Spell.SpellType == neweffect.Spell.SpellType;
        }

        public StyleBleeding(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override string GetDelveDescription(GameClient client)
        {
            string language = client?.Account?.Language ?? Properties.SERV_LANGUAGE;
            string damageTypeName = LanguageMgr.GetDamageOfType(language, Spell.DamageType);
            double freqSeconds = Spell.Frequency / 1000.0;
            return LanguageMgr.GetTranslation(language, "SpellDescription.StyleBleeding.MainDescription", Spell.Damage, damageTypeName, freqSeconds.ToString("0.##"));
        }
    }
}
