using DOL.GS;
using DOL.GS.Styles;
using DOL.GS.Spells;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler("StyleSpell")]
    public class StyleSpellHandler : SpellHandler
    {
        public StyleSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine) { }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            var target = effect.Owner;
            if (target == null || !(target is GameLiving living))
                return;

            var player = living as GamePlayer;
            if (player == null)
                return;

            var styleId = (int)Spell.LifeDrainReturn;
            var dbStyle = GamePlayer.GetDBStyleByID(styleId, player.CharacterClass.ID); // Use player's character class ID
            if (dbStyle == null)
            {
                player.Out.SendMessage($"Style with ID {styleId} not found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            var style = new Style(dbStyle); // Convert DBStyle to Style
            InventoryItem weapon = living.AttackWeapon;

            if (StyleProcessor.CanUseStyle(living, style, weapon))
            {
                // Enter combat mode if not already in combat
                if (!player.AttackState)
                {
                    player.StartAttack(player.TargetObject);
                }

                // Execute the style
                StyleProcessor.ExecuteStyle(living, new AttackData { Style = style, Target = living.TargetObject as GameLiving }, weapon);
                player.Out.SendMessage($"You use {style.Name}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                player.Out.SendMessage("Cannot use the style with the current weapon.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
            // Create a new GameSpellEffect instance with correct arguments
            var effect = new GameSpellEffect(this, Spell.Duration, Spell.Frequency, Spell.Value); // Pass effectiveness as a double
            effect.Start(target); // Apply the effect to the target
        }
    }
}