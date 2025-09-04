using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Shears any active OffensiveProc effects from the target and, if configured,
    /// immediately applies a replacement OffensiveProc buff whose sub-spell heals the enemy.
    ///
    /// Usage/DB:
    /// - Create an OffensiveProc BUFF spell in DB (e.g. "Cursed Heal Enemy Proc") with:
    ///     SpellType = "OffensiveProc"
    ///     Frequency = your proc chance * 100 (e.g. 1000 = 10%)
    ///     Value     = SpellID of your "HealEnemy" sub-spell (see handler above)
    /// - In the OffProcShear spell row, set SubSpellID = SpellID of that OffensiveProc BUFF.
    /// - Cast OffProcShear on an enemy to rip their current offensive proc(s) and replace.
    /// </summary>
    [SpellHandler("OffProcShear")]
    public class OffProcShear : SpellHandler
    {
        public OffProcShear(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target, bool force = false)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target, force);
        }

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (!base.OnDirectEffect(target, effectiveness))
                return false;

            if (target == null || !target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active)
                return false;

            // Standard interrupt/aggro behavior similar to shear spells
            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
            if (target is GameNPC npc && npc.Brain is IOldAggressiveBrain aggro)
                aggro.AddToAggroList(Caster, 1);

            bool removedAny = false;
            var toCancel = new List<GameSpellEffect>();

            // Find any proc-buff effects with SpellType "OffensiveProc"
            foreach (GameSpellEffect eff in target.EffectList.GetAllOfType<GameSpellEffect>())
            {
                if (eff?.Spell == null) continue;

                // Prefer exact type check; also check SpellType name as a fallback.
                bool isOffProcType =
                    eff.SpellHandler is OffensiveProcSpellHandler ||
                    eff.SpellHandler is OffensiveProcPvESpellHandler ||
                    string.Equals(eff.Spell.SpellType, "OffensiveProc", StringComparison.OrdinalIgnoreCase);

                if (isOffProcType)
                    toCancel.Add(eff);
            }

            if (toCancel.Count > 0)
            {
                foreach (var eff in toCancel)
                {
                    SendEffectAnimation(target, 0, false, 1);
                    eff.Cancel(false);
                    removedAny = true;
                }

                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.OffProcShear.Removed"),
                                eChatType.CT_Spell);
                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.OffProcShear.YourProcRipped"),
                                eChatType.CT_Spell);
            }

            // Apply replacement cursed proc if we have one configured in SubSpellID
            bool appliedReplacement = false;
            if (removedAny && Spell.SubSpellID > 0)
            {
                Spell replacementBuff = SkillBase.GetSpellByID((int)Spell.SubSpellID);
                SpellLine offProcLine = SkillBase.GetSpellLine("OffensiveProc");

                if (replacementBuff != null && offProcLine != null)
                {
                    ISpellHandler h = ScriptMgr.CreateSpellHandler(Caster, replacementBuff, offProcLine);
                    if (h != null)
                    {
                        h.StartSpell(target);
                        appliedReplacement = true;

                        MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.OffProcShear.AppliedCursedProc"),
                                        eChatType.CT_Spell);
                        MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.OffProcShear.YouAreCursed"),
                                        eChatType.CT_Important);
                    }
                }
            }

            if (!removedAny)
            {
                SendEffectAnimation(target, 0, false, 0);
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.BuffShear.NoEnhancementFound"),
                                eChatType.CT_SpellResisted);
                return false;
            }

            // If we removed but couldn't apply replacement, still succeed as a shear
            return true;
        }

        public override void OnSpellResisted(GameLiving target)
        {
            base.OnSpellResisted(target);
            if (Spell.Damage == 0 && Spell.CastTime == 0)
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Function: Rip active Offensive Proc(s) from the target.");
                list.Add(" "); // empty line
                list.Add("On success: removes any active offensive proc buffs on the target.");
                list.Add("If configured, applies a cursed replacement proc that heals the target’s enemies on their hits.");
                if (Spell.Range != 0) list.Add("Range: " + Spell.Range);
                if (Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
                list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
                if (Spell.Radius != 0) list.Add("Radius: " + Spell.Radius);
                if (Spell.SubSpellID > 0) list.Add("Replacement proc spell ID: " + Spell.SubSpellID);
                return list;
            }
        }
    }
}