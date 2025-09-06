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
                // +X% effectiveness → +X * 0.01 to the internal multiplier
                double bonus = Spell.Value * 0.01;
                player.TempProperties.setProperty(ASC_BONUS_KEY, bonus);
                player.Effectiveness += bonus;

                // If MoC is up and Ascendance isn't “permissive” (AmnesiaChance <= 0), cancel MoC now.
                var moc = player.EffectList.GetOfType<MasteryofConcentrationEffect>();
                if (moc != null && Spell.AmnesiaChance <= 0)
                {
                    moc.Cancel(false);
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client, "SpellDescription.Ascendance.CanceledMoC")
                        ?? "Your Mastery of Concentration fades as Ascendance takes over.",
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendStatusUpdate();

                MessageToLiving(player,
                    LanguageMgr.GetTranslation(player.Client, "SpellDescription.Ascendance.OnStart", (int)Spell.Value)
                    ?? $"You ascend, increasing effectiveness by {(int)Spell.Value}%.",
                    eChatType.CT_Spell);
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
                    MessageToLiving(player,
                        LanguageMgr.GetTranslation(player.Client, "SpellDescription.Ascendance.OnEnd")
                        ?? "Your ascended state fades.",
                        eChatType.CT_Spell);
                }
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string lang = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;

            // Suggested keys:
            // "SpellDescription.Ascendance.MainDescription" = "Greatly increases your spell and ability effectiveness by {0}%."
            // "SpellDescription.Ascendance.Restrictions"    = "While Ascendance is active: you cannot cast Heal or Resurrect. If this spell’s AmnesiaChance is above 0, those restrictions are lifted."
            // "SpellDescription.Ascendance.Interaction"     = "Casting Mastery of Concentration cancels Ascendance, and vice versa, unless this spell’s AmnesiaChance is above 0."
            string main = LanguageMgr.GetTranslation(lang, "SpellDescription.Ascendance.MainDescription", (int)Spell.Value)
                          ?? $"Greatly increases your effectiveness by {(int)Spell.Value}%.";
            string rest = LanguageMgr.GetTranslation(lang, "SpellDescription.Ascendance.Restrictions")
                          ?? "While Ascendance is active: you cannot cast Heal or Resurrect. If this spell’s AmnesiaChance is above 0, those restrictions are lifted.";
            string interact = LanguageMgr.GetTranslation(lang, "SpellDescription.Ascendance.Interaction")
                          ?? "Casting Mastery of Concentration cancels Ascendance, and vice versa, unless this spell’s AmnesiaChance is above 0.";
            return main + "\n\n" + rest + "\n\n" + interact;
        }
    }
}