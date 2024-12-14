using DOL.AI.Brain;
using DOL.GS.PlayerClass;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("DeathClaw")]
    public class DeathClawSpellHandler : SpellHandler
    {
        public DeathClawSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.Spell;
            ad.SpellHandler = this;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.IsSpellResisted = false;

            if (target is GameNPC npc)
            {
                if (npc.Flags.HasFlag(GameNPC.eFlags.GHOST))
                {
                    // No effect on ghosts
                    ad.Damage = 0;
                }
                else if (npc.BodyType == (ushort)NpcTemplateMgr.eBodyType.Undead)
                {
                    if (npc.IsBoss)
                    {
                        // Reduce undead health by 20%
                        ad.Damage = (int)(npc.MaxHealth * 0.2 - (npc.MaxHealth - npc.Health));
                    }
                    else
                    {
                        // Reduce undead health by 50%
                        ad.Damage = (int)(npc.MaxHealth * 0.5 - (npc.MaxHealth - npc.Health));
                    }
                }
                else if (npc.IsBoss)
                {
                    if (npc.BodyType == (ushort)NpcTemplateMgr.eBodyType.Undead)
                    {
                        // Reduce undead health by 20%
                        ad.Damage = (int)(npc.MaxHealth * 0.2 - (npc.MaxHealth - npc.Health));
                    }
                    else
                    {
                        // Reduce boss health by 30%
                        ad.Damage = (int)(npc.MaxHealth * 0.3 - (npc.MaxHealth - npc.Health));
                    }
                }
                else
                {
                    // Reduce health by 90% for other NPCs
                    ad.Damage = (int)(npc.MaxHealth * 0.9 - (npc.MaxHealth - npc.Health));
                }
            }
            else
            {
                // Reduce health by 90% for players
                ad.Damage = (int)(target.MaxHealth * 0.9 - (target.MaxHealth - target.Health));
            }

            m_lastAttackData = ad;
            SendDamageMessages(ad);
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            DamageTarget(ad, true);
            return true;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            if (Spell.AmnesiaChance > 0 && target.Level > Spell.AmnesiaChance)
                return 100;

            var ResistChanceFactor = 2.6;
            bool isGhost = target is GameNPC npc && npc.Flags.HasFlag(GameNPC.eFlags.GHOST);
            bool isBoss = target is GameNPC gameNPC && gameNPC.IsBoss;

            if (isBoss || isGhost)
                return base.CalculateSpellResistChance(target) * (int)ResistChanceFactor;

            return base.CalculateSpellResistChance(target);
        }

        public override string ShortDescription
        {
            get
            {
                string language = Properties.SERV_LANGUAGE;
                return LanguageMgr.GetTranslation(language, "SpellDescription.DeathClaw.MainDescription", Spell.Name, Spell.AmnesiaChance);
            }
        }
    }
}