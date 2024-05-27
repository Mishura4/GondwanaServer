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
        public DamnationSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GameSpellEffect damnationEffect = SpellHandler.FindEffectOnTarget(target, "Damnation");
            if (damnationEffect != null)
            {
                if (Caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "Damnation.Target.Resist", player.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                return;
            }
            base.ApplyEffectOnTarget(target, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GameLiving living = effect.Owner;

            living.CancelAllSpeedOrPulseEffects();
            if (living is GamePlayer player)
            {
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
                    player.IsDamned = true;
                    player.Group.UpdateMember(player, false, false);
                }
            }
            else if (living is GameNPC ncp && ncp.Brain is IOldAggressiveBrain aggroBrain)
                aggroBrain.AddToAggroList(Caster, 1);
            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "Damnation.Self.Message"), eChatType.CT_Spell);
            }
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner;
            living.Die(Caster);
            if (living is GamePlayer player)
            {
                player.IsDamned = false;
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
            if (target is GameNPC npc && npc.BodyType == (ushort)NpcTemplateMgr.eBodyType.Undead)
                return 0;
            return base.CalculateToHitChance(target);
        }

        public override string ShortDescription
            => $"The target is condemned, turned into a zombie and loses all its spell enhancements. The target will inevitably die after {Spell.Duration / 1000} seconds. No cure can reverse this effect... Undead monsters are not affected by this spell.";
    }
}