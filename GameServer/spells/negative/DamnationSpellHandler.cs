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
    public class DamnationSpellHandler : SpellHandler
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
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            return base.ApplyEffectOnTarget(target, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GameLiving living = effect.Owner;

            int harmvalue = (int)Spell.Value;
            living.TempProperties.setProperty("DamnationValue", harmvalue);
            living.DamnationCancelBuffEffects();
            living.CancelMorphSpellEffects();
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
                switch (living.Race)
                {
                    case 1:
                    case 3:
                    case 9:
                    case 16:
                        if (living.Gender == eGender.Female)
                            living.Model = 443;
                        else
                            living.Model = 442;
                        break;
                    case 2:
                    case 11:
                        if (living.Gender == eGender.Female)
                            living.Model = 451;
                        else
                            living.Model = 452;
                        break;
                    case 4:
                    case 7:
                        if (living.Gender == eGender.Female)
                            living.Model = 468;
                        else
                            living.Model = 467;
                        break;
                    case 5:
                    case 17:
                        if (living.Gender == eGender.Female)
                            living.Model = 445;
                        else
                            living.Model = 446;
                        break;
                    case 6:
                        living.Model = 444;
                        break;
                    case 8:
                        if (living.Gender == eGender.Female)
                            living.Model = 682;
                        else
                            living.Model = 683;
                        break;
                    case 10:
                        if (living.Gender == eGender.Female)
                            living.Model = 681;
                        else
                            living.Model = 680;
                        break;
                    case 12:
                        if (living.Gender == eGender.Female)
                            living.Model = 1380;
                        else
                            living.Model = 1379;
                        break;
                    case 13:
                        if (living.Gender == eGender.Female)
                            living.Model = 1352;
                        else
                            living.Model = 1351;
                        break;
                    case 14:
                        living.Model = 1210;
                        break;
                    case 15:
                        if (living.Gender == eGender.Female)
                            living.Model = 890;
                        else
                            living.Model = 889;
                        break;
                    case 18:
                        if (living.Gender == eGender.Female)
                            living.Model = 1386;
                        else
                            living.Model = 1385;
                        break;
                    case 19:
                        living.Model = 1576;
                        break;
                    case 20:
                        living.Model = 1579;
                        break;
                    case 21:
                        living.Model = 1582;
                        break;
                    default:
                        break;
                }
                player.Out.SendUpdatePlayer();
                if (player.Group != null)
                {
                    player.Group.UpdateMember(player, false, false);
                }
            }
            else if (living is GameNPC ncp && ncp.Brain is IOldAggressiveBrain aggroBrain)
                aggroBrain.AddToAggroList(Caster, 1);

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
            if (living is GamePlayer player)
            {
                living.Model = living.TempProperties.getProperty<ushort>("OriginalModel");
                player.Out.SendUpdatePlayer();
                if (player.Group != null)
                {
                    player.Group.UpdateMember(player, false, false);
                }
            }

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