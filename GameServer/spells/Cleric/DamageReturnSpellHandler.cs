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
    /// Percentage is Spell.Value (0..100). Reflected damage type is Spell.DamageType.
    /// Shows ClientLaunchEffect on the victim and ClientHitEffect on the attacker when it triggers.
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

            // Optional start messages (add LanguageMgr keys if you want localized strings)
            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer,
                    $"Damage Return active: reflecting {Math.Max(0, Math.Min(100, (int)Spell.Value))}% as {GlobalConstants.DamageTypeToName(Spell.DamageType)}.",
                    eChatType.CT_Spell);
            }

            // Normal “buff up” visual if you have ClientEffect set on the spell row
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));

            if (!noMessages && Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, "Damage Return has worn off.", eChatType.CT_SpellExpires);
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        private void EventHandler(DOLEvent e, object sender, EventArgs argsObj)
        {
            if (argsObj is not AttackedByEnemyEventArgs args)
                return;

            var ad = args.AttackData;
            var target = ad.Target;      // the victim who has the DamageReturn effect
            var attacker = ad.Attacker;  // the aggressor who will receive reflected damage

            if (target == null || attacker == null)
                return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active)
                return;
            if (ad.AttackResult is not (GameLiving.eAttackResult.HitUnstyled or GameLiving.eAttackResult.HitStyle))
                return;

            // Exclude DoT ticks; accept melee, ranged, direct spell
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

            // Prevent weird recursion if server emits AttackedByEnemy on TakeDamage (defensive)
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

            // Apply reflected damage as the spell's configured damage type
            var reflectType = Spell.DamageType;

            try
            {
                attacker.TempProperties.setProperty(DR_GUARD, true);
                attacker.TakeDamage(target, reflectType, reflectDamage, 0);

                // Optional feedback lines; swap to LanguageMgr keys if you maintain translations
                if (target is GamePlayer victimPlayer)
                {
                    MessageToLiving(victimPlayer,
                        $"Your Damage Return reflects {reflectDamage} {GlobalConstants.DamageTypeToName(reflectType)} damage.",
                        eChatType.CT_Spell);
                }

                if (attacker is GamePlayer attackerPlayer)
                {
                    MessageToLiving(attackerPlayer,
                        $"You are hurt by reflected damage ({reflectDamage} {GlobalConstants.DamageTypeToName(reflectType)}).",
                        eChatType.CT_Damaged);
                }
            }
            finally
            {
                attacker.TempProperties.removeProperty(DR_GUARD);
            }
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            // Simple, non-localized delve; replace with LanguageMgr.GetTranslation if you add keys.
            string typeName = GlobalConstants.DamageTypeToName(Spell.DamageType);
            int pct = Math.Max(0, Math.Min(100, (int)Spell.Value));
            int recastSeconds = Spell.RecastDelay / 1000;

            string text = $"Reflects {pct}% of incoming melee, ranged, and direct spell damage (excluding DoTs) " +
                          $"back to the attacker as {typeName} damage while active.";

            if (Spell.Duration > 0 && Spell.Duration < 65535)
            {
                string dur = (Spell.Duration >= 60000)
                    ? $"{(int)(Spell.Duration / 60000)}:{Spell.Duration % 60000} min"
                    : $"{Spell.Duration / 1000} sec";
                text += $"\n\nDuration: {dur}";
            }

            if (Spell.RecastDelay > 0)
                text += $"\n\nRecast: {recastSeconds} sec";

            return text;
        }
    }
}