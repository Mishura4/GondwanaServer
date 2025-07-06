using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("SpellShield")]
    public class SpellShieldHandler : SpellHandler
    {
        public SpellShieldHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override bool CanBeRightClicked => false;

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GameLiving living = effect.Owner;

            GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));
            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "SpellShield.Self.Message"), eChatType.CT_Spell);

                foreach (GamePlayer nearbyPlayer in casterPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (nearbyPlayer != casterPlayer)
                    {
                        nearbyPlayer.Out.SendMessage(LanguageMgr.GetTranslation(nearbyPlayer.Client, "SpellShield.Others.Message", nearbyPlayer.GetPersonalizedName(casterPlayer)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    }
                }
            }
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        private void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (arguments is not AttackedByEnemyEventArgs { AttackData: { AttackType: AttackData.eAttackType.Spell or AttackData.eAttackType.DoT } ad })
            {
                return;
            }

            if (sender is not GameLiving target)
            {
                return;
            }
            
            var newHealth = target.Health - ad.Damage - ad.CriticalDamage;
            if (newHealth > (int)(((double)target.MaxHealth * 20) / 100))
            {
                return;
            }

            int damageAbsorbed = 0;
            int critAbsorbed = 0;
            if (ad.AttackType == AttackData.eAttackType.Spell)
            {
                damageAbsorbed = ad.Damage;
                critAbsorbed = ad.CriticalDamage;
            }
            else if (ad.AttackType == AttackData.eAttackType.DoT)
            {
                damageAbsorbed = ((ad.Damage + 1) * 70) / 100;
                critAbsorbed = ((ad.CriticalDamage + 1) * 70) / 100;
            }

            if (damageAbsorbed == 0 && critAbsorbed == 0)
            {
                return;
            }

            ad.Damage -= damageAbsorbed;
            ad.CriticalDamage -= critAbsorbed;

            if (target is GamePlayer player)
            {
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellShield.Self.Absorb", damageAbsorbed), eChatType.CT_Spell);
            }
            if (ad.Attacker is GamePlayer attacker)
            {
                MessageToLiving(attacker, LanguageMgr.GetTranslation(attacker.Client, "SpellShield.Target.Absorbs", attacker.GetPersonalizedName(target), damageAbsorbed), eChatType.CT_Spell);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner;

            GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));
            return base.OnEffectExpires(effect, noMessages);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            int recastSeconds = Spell.RecastDelay / 1000;
            string mainDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.SpellShield.MainDescription", Spell.Name);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return mainDesc + "\n\n" + secondDesc;
            }

            return mainDesc;
        }
    }
}