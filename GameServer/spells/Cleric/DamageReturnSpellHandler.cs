using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Reflects a portion of non-DoT incoming damage back to the attacker.
    /// </summary>
    [SpellHandler("DamageReturn")]
    public class DamageReturnSpellHandler : SpellHandler
    {
        private const string DR_GUARD = "DAMAGE_RETURN_NO_RECURSION";

        public DamageReturnSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine) { }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            GameLiving living = effect.Owner;
            GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));

            if (Caster is GamePlayer casterPlayer)
            {
                string typeName = LanguageMgr.GetDamageOfType(casterPlayer.Client, Spell.DamageType);
                int pct = Math.Max(0, Math.Min(100, (int)Spell.Value));
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.Cleric.DamageReturn.Active"), eChatType.CT_Spell);
            }

            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));

            if (!noMessages && Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "SpellHandler.Cleric.DamageReturn.WornOff"), eChatType.CT_SpellExpires);
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        private void EventHandler(DOLEvent e, object sender, EventArgs argsObj)
        {
            if (argsObj is not AttackedByEnemyEventArgs args)
                return;

            var ad = args.AttackData;
            var target = ad.Target;
            var attacker = ad.Attacker;

            if (target == null || attacker == null)
                return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active)
                return;
            if (ad.AttackResult is not (GameLiving.eAttackResult.HitUnstyled or GameLiving.eAttackResult.HitStyle))
                return;

            if (ad.AttackType == AttackData.eAttackType.DoT)
                return;

            bool allowedAttackType =
                ad.AttackType == AttackData.eAttackType.MeleeOneHand ||
                ad.AttackType == AttackData.eAttackType.MeleeTwoHand ||
                ad.AttackType == AttackData.eAttackType.MeleeDualWield ||
                ad.AttackType == AttackData.eAttackType.Ranged ||
                ad.AttackType == AttackData.eAttackType.Spell;

            if (!allowedAttackType)
                return;

            if (attacker.TempProperties.getProperty<bool>(DR_GUARD, false))
                return;

            int inflicted = Math.Max(0, ad.Damage + ad.CriticalDamage);
            if (inflicted <= 0)
                return;

            // Reflection percent from Spell.Value
            double pct = Spell.Value;
            if (pct <= 0)
                return;

            int reflectDamage = (int)Math.Round(inflicted * (pct / 100.0));
            if (reflectDamage <= 0)
                return;

            // Show trigger visuals:
            // - launch effect on the victim who owns the buff
            if (Spell.ClientLaunchEffect > 0)
            {
                foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    p.Out.SendSpellEffectAnimation(target, target, Spell.ClientLaunchEffect, 0, false, 1);
            }

            // - hit effect on the attacker who gets the reflected damage
            if (Spell.ClientHitEffect > 0)
            {
                foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    p.Out.SendSpellEffectAnimation(target, attacker, Spell.ClientHitEffect, 0, false, 1);
            }

            var reflectType = Spell.DamageType;

            try
            {
                attacker.TempProperties.setProperty(DR_GUARD, true);
                attacker.TakeDamage(target, reflectType, reflectDamage, 0);

                if (target is GamePlayer victimPlayer)
                {
                    string victimTypeName = LanguageMgr.GetDamageOfType(victimPlayer.Client, reflectType);
                    MessageToLiving(victimPlayer, LanguageMgr.GetTranslation(victimPlayer.Client,"SpellHandler.Cleric.DamageReturn.VictimReflect", reflectDamage, victimTypeName), eChatType.CT_Spell);
                }

                if (attacker is GamePlayer attackerPlayer)
                {
                    string attackerTypeName = LanguageMgr.GetDamageOfType(attackerPlayer.Client, reflectType);
                    MessageToLiving(attackerPlayer, LanguageMgr.GetTranslation(attackerPlayer.Client, "SpellHandler.Cleric.DamageReturn.AttackerHurt", reflectDamage, attackerTypeName), eChatType.CT_Damaged);
                }
            }
            finally
            {
                attacker.TempProperties.removeProperty(DR_GUARD);
            }
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string typeName = LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType);
            int pct = Math.Max(0, Math.Min(100, (int)Spell.Value));
            int recastSeconds = Spell.RecastDelay / 1000;

            string description = LanguageMgr.GetTranslation(delveClient, "SpellDescription.DamageReturn.MainDescription", pct, typeName);

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(delveClient, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return description + "\n\n" + secondDesc;
            }

            return description;
        }
    }
}