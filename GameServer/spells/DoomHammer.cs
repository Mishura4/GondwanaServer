//Eden - Darwin

using System;
using System.Reflection;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;
using System.Collections;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.ServerProperties;
using DOL.Language;
using System.Text;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("DoomHammer")]
    public class DoomHammerSpellHandler : DirectDamageSpellHandler
    {
        public override bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
        {
            if (Caster.IsDisarmed)
            {
                MessageToCaster("You are disarmed and can't use this spell!", eChatType.CT_SpellResisted);
                return false;
            }
            return base.CheckBeginCast(selectedTarget, quiet);
        }
        public override double CalculateDamageBase(GameLiving target) { return Spell.Damage; }
        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GamePlayer player = target as GamePlayer;
            if (!base.ApplyEffectOnTarget(Caster, effectiveness))
                return false;

            Caster.StopAttack();
            foreach (GamePlayer visPlayer in Caster.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
                visPlayer.Out.SendCombatAnimation(Caster, target, 0x0000, 0x0000, (ushort)408, 0, 0x00, target.HealthPercent);
            if (Spell.ResurrectMana > 0) foreach (GamePlayer visPlayer in target.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
                    visPlayer.Out.SendSpellEffectAnimation(Caster, target, (ushort)Spell.ResurrectMana, 0, false, 0x01);

            if ((Spell.Duration > 0 && Spell.Target != "Area") || Spell.Concentration > 0)
                return OnDirectEffect(target, effectiveness);
            return false;
        }
        /// <inheritdoc />
        public override void OnEffectStart(GameSpellEffect effect)
        {
            effect.Owner.DisarmedCount++;
            base.OnEffectStart(effect);
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.DisarmedCount--;
            return base.OnEffectExpires(effect, noMessages);
        }
        public DoomHammerSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            string damageTypeName = Spell.DamageType.ToString();
            int recastSeconds = Spell.RecastDelay / 1000;

            string mainDesc = LanguageMgr.GetTranslation(language, "SpellDescription.DoomHammer.MainDescription", Spell.Damage, damageTypeName);

            StringBuilder sb = new StringBuilder();
            sb.Append(mainDesc);

            if (Spell.SubSpellID > 0)
            {
                Spell subSpell = SkillBase.GetSpellByID((int)Spell.SubSpellID);
                if (subSpell != null)
                {
                    ISpellHandler subHandler = ScriptMgr.CreateSpellHandler(Caster, subSpell, SpellLine);
                    if (subHandler != null)
                    {
                        sb.Append("\n\n");
                        sb.Append(subHandler.GetDelveDescription(delveClient));
                    }
                }
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                sb.Append("\n\n");
                sb.Append(secondDesc);
            }

            return sb.ToString().TrimEnd();
        }
    }
}