using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("Damnation")]
    public class DamnationSpellHandler : AbstractMorphSpellHandler
    {
        private const int HeatDebuff = -15;
        private const int ColdDebuff = -10;
        private const int ThrustDebuff = -10;
        private const int SpiritDebuff = -5;
        private const int EnergyDebuff = -5;

        private const int BodyBuff = 15;
        private const int MatterBuff = 15;
        private const int SlashBuff = 5;
        private const int CrushBuff = 5;

        public DamnationSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            Priority = 666;
            OverwritesMorphs = true;
        }

        /// <inheritdoc />
        public override ushort GetModelFor(GameLiving living)
        {
            if (living is not GamePlayer)
                return 0;
            
            switch (living.Race)
            {
                case 1:
                case 3:
                case 9:
                case 16:
                    if (living.Gender == eGender.Female)
                        return 443;
                    else
                        return 442;
                    break;
                case 2:
                case 11:
                    if (living.Gender == eGender.Female)
                        return 451;
                    else
                        return 452;
                    break;
                case 4:
                case 7:
                    if (living.Gender == eGender.Female)
                        return 468;
                    else
                        return 467;
                    break;
                case 5:
                case 17:
                    if (living.Gender == eGender.Female)
                        return 445;
                    else
                        return 446;
                    break;
                case 6:
                    return 444;
                    break;
                case 8:
                    if (living.Gender == eGender.Female)
                        return 682;
                    else
                        return 683;
                    break;
                case 10:
                    if (living.Gender == eGender.Female)
                        return 681;
                    else
                        return 680;
                    break;
                case 12:
                    if (living.Gender == eGender.Female)
                        return 1380;
                    else
                        return 1379;
                    break;
                case 13:
                    if (living.Gender == eGender.Female)
                        return 1352;
                    else
                        return 1351;
                    break;
                case 14:
                    return 1210;
                    break;
                case 15:
                    if (living.Gender == eGender.Female)
                        return 890;
                    else
                        return 889;
                    break;
                case 18:
                    if (living.Gender == eGender.Female)
                        return 1386;
                    else
                        return 1385;
                    break;
                case 19:
                    return 1576;
                    break;
                case 20:
                    return 1579;
                    break;
                case 21:
                    return 1582;
                    break;
                default:
                    break;
            }
            return 0;
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

        public override void OnBetterThan(GameLiving target, GameSpellEffect oldEffect, GameSpellEffect newEffect)
        {
            SpellHandler attempt = (SpellHandler)newEffect.SpellHandler;
            if (attempt.Caster.GetController() is GamePlayer player)
                player.SendTranslatedMessage("Damnation.Target.Resist", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow, player.GetPersonalizedName(target));
            attempt.SendSpellResistAnimation(target);
        }

        /// <inheritdoc />
        public override bool PreventsApplication(GameSpellEffect self, GameSpellEffect other)
        {
            var spellHandler = other.SpellHandler as SpellHandler;

            if (spellHandler != null)
            {
                if (spellHandler.HasPositiveEffect || spellHandler.Spell.SpellType == "Disease" || spellHandler.Spell.SpellType == "HealDebuff" || spellHandler.Spell.Pulse > 0)
                    return true;
            }
            
            if (spellHandler.HasPositiveOrSpeedEffect() || spellHandler.Spell.Pulse > 0)
                return true;
            
            return base.PreventsApplication(self, other);
        }

        /// <inheritdoc />
        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
            if (neweffect.SpellHandler is DamnationSpellHandler)
            {
                if (oldeffect.SpellHandler is not DamnationSpellHandler)
                    return true;

                if (oldeffect.Duration > neweffect.Duration)
                    return true;
            }
            return false;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GameLiving living = effect.Owner;

            int harmvalue = (int)Spell.Value;
            living.TempProperties.setProperty("DamnationValue", harmvalue);
            
            ApplyDebuffs(living);
            ApplyBuffs(living);
            
            living.IsDamned = true;
            if (living is GamePlayer player)
            {
                if (player.GuildBanner != null)
                {
                    player.GuildBanner.ForceBannerDrop();
                }

                living.TempProperties.setProperty("OriginalModel", living.Model);
                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Damnation.Self.Message2"), eChatType.CT_Spell);
            }
            else if (living is GameNPC ncp && ncp.Brain is IOldAggressiveBrain aggroBrain)
                aggroBrain.AddToAggroList(Caster, 1);
            
            base.OnEffectStart(effect);

            GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(DamnationEventHandler));

            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "Damnation.Self.Message"), eChatType.CT_Spell);
            }
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        private void ApplyDebuffs(GameLiving living)
        {
            living.DebuffCategory[eProperty.Resist_Heat] += HeatDebuff;
            living.DebuffCategory[eProperty.Resist_Cold] += ColdDebuff;
            living.DebuffCategory[eProperty.Resist_Thrust] += ThrustDebuff;
            living.DebuffCategory[eProperty.Resist_Spirit] += SpiritDebuff;
            living.DebuffCategory[eProperty.Resist_Energy] += EnergyDebuff;

            if (living is GamePlayer player)
            {
                player.Out.SendCharResistsUpdate();
            }
        }

        private void RemoveDebuffs(GameLiving living)
        {
            living.DebuffCategory[eProperty.Resist_Heat] -= HeatDebuff;
            living.DebuffCategory[eProperty.Resist_Cold] -= ColdDebuff;
            living.DebuffCategory[eProperty.Resist_Thrust] -= ThrustDebuff;
            living.DebuffCategory[eProperty.Resist_Spirit] -= SpiritDebuff;
            living.DebuffCategory[eProperty.Resist_Energy] -= EnergyDebuff;

            if (living is GamePlayer player)
            {
                player.Out.SendCharResistsUpdate();
            }
        }

        private void ApplyBuffs(GameLiving living)
        {
            living.BaseBuffBonusCategory[eProperty.Resist_Body] += BodyBuff;
            living.BaseBuffBonusCategory[eProperty.Resist_Matter] += MatterBuff;
            living.BaseBuffBonusCategory[eProperty.Resist_Slash] += SlashBuff;
            living.BaseBuffBonusCategory[eProperty.Resist_Crush] += CrushBuff;

            if (living is GamePlayer player)
            {
                player.Out.SendCharResistsUpdate();
            }
        }

        private void RemoveBuffs(GameLiving living)
        {
            living.BaseBuffBonusCategory[eProperty.Resist_Body] -= BodyBuff;
            living.BaseBuffBonusCategory[eProperty.Resist_Matter] -= MatterBuff;
            living.BaseBuffBonusCategory[eProperty.Resist_Slash] -= SlashBuff;
            living.BaseBuffBonusCategory[eProperty.Resist_Crush] -= CrushBuff;

            if (living is GamePlayer player)
            {
                player.Out.SendCharResistsUpdate();
            }
        }

        public void DamnationEventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (!(arguments is AttackedByEnemyEventArgs args))
            {
                return;
            }
            AttackData ad = args.AttackData;
            GameLiving target = ad.Target;
            int damnationEnhancement = target.GetModified(eProperty.DamnationEffectEnhancement);

            if (ad.AttackType == AttackData.eAttackType.DoT)
            {
                ad.Damage = 0;

                if (ad.Target is GamePlayer player)
                    MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Damnation.Self.Resist"), eChatType.CT_SpellResisted);
                if (ad.Attacker is GamePlayer attacker)
                    MessageToLiving(attacker, LanguageMgr.GetTranslation(attacker.Client, "Damnation.Target.Resist", attacker.GetPersonalizedName(ad.Target)), eChatType.CT_SpellResisted);
            }
            else if (ad.AttackType == AttackData.eAttackType.MeleeOneHand ||
                     ad.AttackType == AttackData.eAttackType.MeleeTwoHand ||
                     ad.AttackType == AttackData.eAttackType.MeleeDualWield ||
                     ad.AttackType == AttackData.eAttackType.Ranged)
            {
                int meleeAbsorbPercent = Spell.AmnesiaChance + damnationEnhancement;
                meleeAbsorbPercent = Math.Min(100, meleeAbsorbPercent);

                int damageAbsorbed = (int)(0.01 * meleeAbsorbPercent * (ad.Damage + ad.CriticalDamage));
                ad.Damage -= damageAbsorbed;

                if (damageAbsorbed != 0)
                {
                    if (ad.Target is GamePlayer player)
                        MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Damnation.Self.Absorb", damageAbsorbed), eChatType.CT_Spell);
                    if (ad.Attacker is GamePlayer attacker)
                        MessageToLiving(attacker, LanguageMgr.GetTranslation(attacker.Client, "Damnation.Target.Absorbs", attacker.GetPersonalizedName(ad.Target), damageAbsorbed), eChatType.CT_Spell);
                }
            }
            else if (ad.AttackType == AttackData.eAttackType.Spell)
            {
                if (damnationEnhancement > 0)
                {
                    int spellAbsorbPercent = damnationEnhancement;
                    spellAbsorbPercent = Math.Min(100, spellAbsorbPercent);

                    int damageAbsorbed = (int)(0.01 * spellAbsorbPercent * (ad.Damage + ad.CriticalDamage));
                    ad.Damage -= damageAbsorbed;

                    if (damageAbsorbed != 0)
                    {
                        if (ad.Target is GamePlayer player)
                            MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "Damnation.Self.SpellAbsorb", damageAbsorbed), eChatType.CT_Spell);
                        if (ad.Attacker is GamePlayer attacker)
                            MessageToLiving(attacker, LanguageMgr.GetTranslation(attacker.Client, "Damnation.Target.SpellAbsorbs", attacker.GetPersonalizedName(ad.Target), damageAbsorbed), eChatType.CT_Spell);
                    }
                }
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner;
            GameEventMgr.RemoveHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(DamnationEventHandler));

            RemoveDebuffs(living);
            RemoveBuffs(living);

            living.Die(Caster);
            living.IsDamned = false;

            return base.OnEffectExpires(effect, noMessages);
        }

        public override int CalculateToHitChance(GameLiving target)
        {
            var HitChanceFactor = 3.3;

            if (target is GameNPC npc && npc.BodyType == (ushort)NpcTemplateMgr.eBodyType.Undead)
                return 0;

            if (target is GameNPC gameNPC && gameNPC.IsBoss)
                return base.CalculateToHitChance(target) / (int)HitChanceFactor;

            return base.CalculateToHitChance(target);
        }

        public override string ShortDescription
        {
            get
            {
                string description = $"The target is condemned, turned into a zombie and loses all its spell enhancements. The target will be more resilient against melee attacks by {Spell.AmnesiaChance}% but will inevitably die after {Spell.Duration / 1000} seconds. No cure can reverse this effect.";

                if (Spell.Value < 0)
                {
                    description += $" Healing is severely reduced to only {Math.Abs(Spell.Value)}%.";
                }
                else if (Spell.Value == 0)
                {
                    description += " Healing will have no effect on the target.";
                }
                else if (Spell.Value > 0)
                {
                    description += $" Healing will be converted by {Spell.Value}% into damages.";
                }

                description += " Undead monsters are not affected by this spell.";

                return description;
            }
        }
    }
}