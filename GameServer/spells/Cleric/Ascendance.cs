using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("Ascendance")]
    public class AscendanceSpellHandler : SpellHandler
    {
        private const string ASC_BONUS_KEY = "Ascendance_EffectivenessBonus";

        public override bool HasPositiveEffect => true;

        public AscendanceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            if (effect.Owner is GamePlayer player)
            {
                double bonus = Spell.Value * 0.01;
                player.TempProperties.setProperty(ASC_BONUS_KEY, bonus);
                player.Effectiveness += bonus;

                var moc = player.EffectList.GetOfType<MasteryofConcentrationEffect>();
                if (moc != null && Spell.AmnesiaChance <= 0)
                {
                    moc.Cancel(false);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "SpellHandler.Cleric.Ascendance.CanceledMoC"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();

                MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.Cleric.Ascendance.OnStart"), eChatType.CT_Spell);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner is GamePlayer player)
            {
                double bonus = player.TempProperties.getProperty<double>(ASC_BONUS_KEY);
                if (bonus != 0)
                {
                    player.Effectiveness -= bonus;
                    player.TempProperties.removeProperty(ASC_BONUS_KEY);
                }

                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();

                if (!noMessages)
                {
                    MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.Cleric.Ascendance.OnEnd"), eChatType.CT_Spell);
                }
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string lang = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;

            string description = LanguageMgr.GetTranslation(lang, "SpellDescription.Ascendance.MainDescription", (int)Spell.Value);

            if (Spell.AmnesiaChance == 0)
            {
                string rest = LanguageMgr.GetTranslation(lang, "SpellDescription.Ascendance.Restrictions");
                string interact = LanguageMgr.GetTranslation(lang, "SpellDescription.Ascendance.Interaction");
                description += "\n\n" + rest + "\n\n" + interact;
            }

            if (Spell.RecastDelay > 0)
            {
                string secondDesc = LanguageMgr.GetTranslation(lang, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                return description + "\n\n" + secondDesc;
            }

            return description;
        }
    }
}