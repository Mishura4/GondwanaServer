using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.Geometry;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.GS.Utils;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler("SummonMultiTemplatePets")]
    public class SummonMultiTemplatePetsHandler : SpellHandler
    {
        public SummonMultiTemplatePetsHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override bool HasPositiveEffect => true;

        public override bool OnDirectEffect(GameLiving target, double effectiveness)
            => ApplyEffectOnTarget(target, effectiveness);

        private static int GuardTplId(int v) => v > 0 ? v : 0;
        private static int ToTplId(double v)
        {
            var i = (int)Math.Round(v);
            return i > 0 ? i : 0;
        }

        private enum TemplateKind
        {
            LifeDrainReturn,
            Value,
            AmnesiaChance,
            ResurrectMana
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            bool isSelf = Spell.Target.Equals("self", StringComparison.OrdinalIgnoreCase);
            if (!isSelf && target != m_spellTarget) return true;
            if (isSelf && target != Caster) return true;

            int tplLifeDrain = GuardTplId(Spell.LifeDrainReturn);
            int tplValue = GuardTplId(ToTplId(Spell.Value));
            int tplAmnesia = GuardTplId(Spell.AmnesiaChance);
            int tplResMana = GuardTplId(Spell.ResurrectMana);

            if (tplLifeDrain == 0 && tplValue == 0 && tplAmnesia == 0 && tplResMana == 0)
            {
                return false;
            }

            int resH = Math.Max(0, Spell.ResurrectHealth);
            int countLifeValue = resH;
            int countResMana = resH / 2;
            int countAmnesia = (int)Math.Ceiling((resH / 2.0) * 3.0);

            GameLiving attackTarget = null;
            if (target != null && GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
                attackTarget = target;

            int spawned = 0;

            if (tplLifeDrain > 0 && countLifeValue > 0)
                spawned += SpawnMany(tplLifeDrain, countLifeValue, attackTarget, effectiveness, TemplateKind.LifeDrainReturn);

            if (tplValue > 0 && countLifeValue > 0)
                spawned += SpawnMany(tplValue, countLifeValue, attackTarget, effectiveness, TemplateKind.Value);

            if (tplAmnesia > 0 && countAmnesia > 0)
                spawned += SpawnMany(tplAmnesia, countAmnesia, attackTarget, effectiveness, TemplateKind.AmnesiaChance);

            if (tplResMana > 0 && countResMana > 0)
                spawned += SpawnMany(tplResMana, countResMana, attackTarget, effectiveness, TemplateKind.ResurrectMana);

            return spawned > 0;
        }

        private int SpawnMany(int templateId, int count, GameLiving attackTarget, double effectiveness, TemplateKind kind)
        {
            int done = 0;
            for (int i = 0; i < count; i++)
            {
                if (TrySummonMinionFromTemplate(templateId, attackTarget, effectiveness, kind) != null)
                    done++;
            }
            return done;
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect?.Owner is GameNPC npc)
            {
                if (npc.ObjectState == GameObject.eObjectState.Active)
                    npc.Die(Caster);
            }
            return 0;
        }

        private GamePet TrySummonMinionFromTemplate(int templateId, GameLiving attackTarget, double effectiveness, TemplateKind kind)
        {
            var tpl = NpcTemplateMgr.GetTemplate(templateId);
            if (tpl == null)
            {
                var lang = (Caster as GamePlayer)?.Client?.Account?.Language ?? Properties.SERV_LANGUAGE;
                MessageToCaster(LanguageMgr.GetTranslation(lang, "SpellHandler.Occultist.ArawnsLegion.NoTemplateFound", templateId), eChatType.CT_System);
                return null;
            }

            var pet = new GamePet(tpl);

            var brain = new ControlledNpcBrain(Caster) { IsMainPet = false };
            pet.SetOwnBrain(brain as DOL.AI.ABrain);

            pet.Owner = Caster;
            pet.Realm = Caster.Realm;
            pet.Position = GetSummonPosition();
            pet.Heading = Caster.Heading;
            pet.CurrentRegion = Caster.CurrentRegion;

            // --- level scaling by template kind ---
            double dmg = Spell.Damage;
            if (dmg < 0)
            {
                double factor = kind switch
                {
                    TemplateKind.ResurrectMana => 1.00,
                    TemplateKind.LifeDrainReturn or TemplateKind.Value => 0.95,
                    TemplateKind.AmnesiaChance => 0.90,
                    _ => 1.00
                };
                pet.SummonSpellDamage = dmg * factor;
            }
            else
            {
                pet.SummonSpellDamage = dmg;
            }

            pet.SummonSpellValue = Spell.Value;
            pet.SetPetLevel();
            pet.Health = pet.MaxHealth;
            pet.AutoSetStats();
            pet.VisibleActiveWeaponSlots = tpl.VisibleActiveWeaponSlot;

            if (!pet.AddToWorld())
                return null;

            // Aggro radius: use Spell.Radius if provided, else default 2000
            int aggroRadius = Spell.Radius > 0 ? Spell.Radius : 2000;
            brain.AggroRange = aggroRadius;
            brain.AggressionState = eAggressionState.Aggressive;     // proactively seek targets
            brain.WalkState = eWalkState.Follow;               // still follow owner when idle

            if (attackTarget != null && GameServer.ServerRules.IsAllowedToAttack(pet as GameLiving, attackTarget as GameLiving, true))
                (pet.Brain as IOldAggressiveBrain)?.AddToAggroList(attackTarget, 1);

            try
            {
                var pool = new List<GameLiving>();

                foreach (var obj in pet.GetPlayersInRadius((ushort)aggroRadius))
                {
                    if (obj is GamePlayer gp && GameServer.ServerRules.IsAllowedToAttack(pet as GameLiving, gp as GameLiving, true))
                        pool.Add(gp);
                }

                foreach (var obj in pet.GetNPCsInRadius((ushort)aggroRadius))
                {
                    if (obj is GameLiving gl && GameServer.ServerRules.IsAllowedToAttack(pet as GameLiving, gl, true))
                        pool.Add(gl);
                }

                if (pool.Count > 0)
                {
                    Util.Shuffle(pool);
                    int seedCount = Math.Min(3, pool.Count);
                    for (int i = 0; i < seedCount; i++)
                        (pet.Brain as IOldAggressiveBrain)?.AddToAggroList(pool[i], Util.Random(1, 5));
                }

            (pet.Brain as ControlledNpcBrain)?.Think();

            }
            catch { }

            if (Spell.Duration > 0)
            {
                var eff = CreateSpellEffect(pet, effectiveness: 1.0);
                eff.Start(pet);
            }

            var casterplayer = Caster as GamePlayer;
            if (casterplayer != null)
            {
                var lang = casterplayer.Client?.Account?.Language ?? Properties.SERV_LANGUAGE;
                casterplayer.Out.SendMessage( LanguageMgr.GetTranslation(lang, "SpellHandler.Occultist.ArawnsLegion.PetAnswer", pet.Name), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            }

            return pet;
        }

        private Position GetSummonPosition()
        {
            return Caster.Position.TurnedAround() + Vector.Create(Caster.Orientation, length: 64);
        }

        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            string description;

            if (string.Equals(Spell.PackageID, "Occultist", StringComparison.OrdinalIgnoreCase))
            {
                string mainDesc1 = LanguageMgr.GetTranslation(language, "SpellDescription.ArawnsLegion.MainDescription1");
                string mainDesc2 = LanguageMgr.GetTranslation(language, "SpellDescription.ArawnsLegion.MainDescription2", Spell.Radius, Spell.Duration / 1000);
                string mainDesc3 = LanguageMgr.GetTranslation(language, "SpellDescription.ArawnsLegion.MainDescription3");
                description = mainDesc1 + "\n\n" + mainDesc2 + "\n\n" + mainDesc3;
            }
            else
            {
                string mainDesc1 = LanguageMgr.GetTranslation(language, "SpellDescription.SummonMultiTemplatePets.MainDescription1");
                string mainDesc2 = LanguageMgr.GetTranslation(language, "SpellDescription.ArawnsLegion.MainDescription2", Spell.Radius, Spell.Duration / 1000);
                description = mainDesc1 + "\n\n" + mainDesc2;
            }

            if (Spell.RecastDelay > 0)
            {
                int recastSeconds = Spell.RecastDelay / 1000;
                string subDesc3 = LanguageMgr.GetTranslation(language, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                description += "\n\n" + subDesc3;
            }

            return description.TrimEnd();
        }
    }
}
