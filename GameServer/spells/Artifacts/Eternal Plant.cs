using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("ShatterIllusions")]
    public class ShatterIllusions : SpellHandler
    {
        //Shatter Illusions 
        //(returns the enemy from their shapeshift forms 
        //causing 200 body damage to the enemy. Range: 1500) 
        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (!base.OnDirectEffect(target, effectiveness))
                return false;
            
            AttackData ad = CalculateDamageToTarget(target, effectiveness);
            foreach (GameSpellEffect effect in target.EffectList.GetAllOfType(typeof(AbstractMorphSpellHandler)))
            {
                if (effect.SpellHandler is not PetrifySpellHandler or DamnationSpellHandler)
                {
                    ad.Damage = (int)Spell.Damage;
                    effect.Cancel(false);

                    // Attacked living may modify the attack data.
                    ad.Target.ModifyAttack(ad);

                    SendEffectAnimation(target, 0, false, 1);
                    SendDamageMessages(ad);
                    DamageTarget(ad);
                    return false;
                }
            }
            return true;
        }

        public virtual void DamageTarget(AttackData ad)
        {
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.Target.ModifyAttack(ad);
            ad.Target.OnAttackedByEnemy(ad);
            ad.Attacker.DealDamage(ad);
            foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
            }
        }
        public ShatterIllusions(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}