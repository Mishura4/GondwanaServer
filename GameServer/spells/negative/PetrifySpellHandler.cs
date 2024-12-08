using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("Petrify")]
    public class PetrifySpellHandler : AbstractMorphSpellHandler
    {
        public PetrifySpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            Priority = 100;
        }

        /// <inheritdoc />
        public override bool IsOverwritable(GameSpellEffect compare)
        {
            if (((SpellHandler)compare.SpellHandler).HasPositiveOrSpeedEffect() || compare.SpellHandler.Spell.Pulse > 0)
                return true;
            
            return base.IsOverwritable(compare);
        }

        /// <inheritdoc />
        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
            if (((SpellHandler)oldeffect.SpellHandler).HasPositiveOrSpeedEffect() || oldeffect.SpellHandler.Spell.Pulse > 0)
                return true;
            
            return base.IsNewEffectBetter(oldeffect, neweffect);
        }
        
        public override void OnBetterThan(GameLiving target, GameSpellEffect oldEffect, GameSpellEffect newEffect)
        {
            SpellHandler attempt = (SpellHandler)newEffect.SpellHandler;
            if (attempt.Caster.GetController() is GamePlayer player)
                player.SendTranslatedMessage("Petrify.Target.Resist", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, player.GetPersonalizedName(target));
            attempt.SendSpellResistAnimation(target);
        }

        /// <inheritdoc />
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is IllusionPet)
            {
                target.Die(Caster);
                SendHitAnimation(target, 0, false, 1);
                return true;
            }
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        /// <inheritdoc />
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = Spell.Duration;
            if (!(GameServer.ServerRules.IsPveOnlyBonus(eProperty.NegativeReduction) && GameServer.ServerRules.IsPvPAction(Caster, target)))
                duration *= (1.0 - target.GetModified(eProperty.NegativeReduction) * 0.01);
            duration *= (target.GetModified(eProperty.MythicalCrowdDuration) * 0.01);
            duration *= (target.GetModified(eProperty.StunDuration) * 0.01);

            if (duration <= 0)
                duration = 1;
            return (int)duration;
        }

        /// <inheritdoc />
        public override ushort GetModelFor(GameLiving living)
        {
            if (living is not GamePlayer)
                return 0;
            
            switch (living.Race)
            {
                case 1:
                    if (living.Gender == eGender.Female)
                        return 1316;
                    else
                        return 1315;
                    break;
                case 2:
                    if (living.Gender == eGender.Female)
                        return 1322;
                    else
                        return 1321;
                    break;
                case 3:
                    if (living.Gender == eGender.Female)
                        return 1318;
                    else
                        return 1317;
                    break;
                case 4:
                    if (living.Gender == eGender.Female)
                        return 1320;
                    else
                        return 1319;
                    break;
                case 5:
                    if (living.Gender == eGender.Female)
                        return 1328;
                    else
                        return 1327;
                    break;
                case 6:
                    if (living.Gender == eGender.Female)
                        return 1326;
                    else
                        return 1325;
                    break;
                case 7:
                    if (living.Gender == eGender.Female)
                        return 1332;
                    else
                        return 1331;
                    break;
                case 8:
                    if (living.Gender == eGender.Female)
                        return 1330;
                    else
                        return 1329;
                    break;
                case 9:
                    if (living.Gender == eGender.Female)
                        return 1340;
                    else
                        return 1339;
                    break;
                case 10:
                    if (living.Gender == eGender.Female)
                        return 1338;
                    else
                        return 1337;
                    break;
                case 11:
                    if (living.Gender == eGender.Female)
                        return 1344;
                    else
                        return 1343;
                    break;
                case 12:
                    if (living.Gender == eGender.Female)
                        return 1342;
                    else
                        return 1341;
                    break;
                case 13:
                    if (living.Gender == eGender.Female)
                        return 1314;
                    else
                        return 1313;
                    break;
                case 14:
                    if (living.Gender == eGender.Female)
                        return 1334;
                    else
                        return 1333;
                    break;
                case 15:
                    if (living.Gender == eGender.Female)
                        return 1346;
                    else
                        return 1345;
                    break;
                case 16:
                    if (living.Gender == eGender.Female)
                        return 1324;
                    else
                        return 1323;
                    break;
                case 17:
                    if (living.Gender == eGender.Female)
                        return 1336;
                    else
                        return 1335;
                    break;
                case 18:
                    if (living.Gender == eGender.Female)
                        return 1348;
                    else
                        return 1347;
                    break;
                case 19:
                    return 1574;
                    break;
                case 20:
                    return 1577;
                    break;
                case 21:
                    return 1580;
                    break;
                default:
                    break;
            }
            return base.GetModelFor(living);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameLiving living = effect.Owner;
            living.IsStunned = true;
            living.StopAttack();
            living.StopCurrentSpellcast();
            living.DisableTurning(true);

            if (living is GamePlayer player)
            {
                if (player.GuildBanner != null)
                {
                    player.GuildBanner.ForceBannerDrop();
                }
                player.Out.SendUpdateMaxSpeed();
            }
            GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));
            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "Petrify.Self.Message", casterPlayer.GetPersonalizedName(effect.Owner)), eChatType.CT_Spell);
            }
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (!(arguments is AttackedByEnemyEventArgs args))
            {
                return;
            }
            AttackData ad = args.AttackData;
            double absorbPercent = 0;
            if (args.AttackData.AttackResult == GameLiving.eAttackResult.HitUnstyled
                || args.AttackData.AttackResult == GameLiving.eAttackResult.HitStyle)
                absorbPercent = 50;
            if (args.AttackData.AttackType is AttackData.eAttackType.Spell or AttackData.eAttackType.DoT)
                absorbPercent = 75;

            int damageAbsorbed = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));

            ad.Damage -= damageAbsorbed;

            if (ad.Target is GamePlayer player)
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Petrify.Self.Absorb", damageAbsorbed), eChatType.CT_Spell);
            if (ad.Attacker is GamePlayer attacker)
                MessageToLiving(attacker, LanguageMgr.GetTranslation(attacker.Client, "Petrify.Target.Absorbs", attacker.GetPersonalizedName(ad.Target), damageAbsorbed), eChatType.CT_Spell);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner;

            living.IsStunned = false;
            living.DisableTurning(false);

            if (living is GamePlayer player)
            {
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Petrify.Self.Unpetrify"), eChatType.CT_Spell);
                player.Client.Out.SendUpdateMaxSpeed();
            }
            else if (living is GameNPC ncp && ncp.Brain is IOldAggressiveBrain aggroBrain)
                aggroBrain.AddToAggroList(Caster, 1);

            GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));
            return base.OnEffectExpires(effect, noMessages);
        }

        public override string ShortDescription
            => $"Petrifies the target and turns it into a statue. The target is completely paralysed and absorbs 50% of physical damages as well as 75% of magical damages it might suffer if attacked.";
    }
}