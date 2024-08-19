using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("CraftingSpeedBuff")]
    public class CraftingSpeedBuff : SingleStatBuff
    {
        public override bool CanBeRightClicked => false;

        public CraftingSpeedBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override eProperty Property1
        {
            get => eProperty.CraftingSpeed;
        }

        public override eBuffBonusCategory BonusCategory1
        {
            get => eBuffBonusCategory.Other;
        }

        public override string ShortDescription => $"Increases the target's crafting speed by {Spell.Value}%.";

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Out.SendUpdateCraftingSkills();
                player.Out.SendStatusUpdate();
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Out.SendUpdateCraftingSkills();
                player.Out.SendStatusUpdate();
            }
            return 0;
        }

        public override PlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            PlayerXEffect eff = new PlayerXEffect();
            eff.Var1 = Spell.ID;
            eff.Duration = e.RemainingTime;
            eff.IsHandler = true;
            eff.SpellLine = SpellLine.KeyName;
            return eff;
        }

        public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
        {
            OnEffectStart(effect);
        }

        public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
        {
            return OnEffectExpires(effect, noMessages);
        }
    }
}