using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using System;
using static DOL.GS.Spells.SpellHandler.OccultistForms;

namespace DOL.GS.Spells
{
    public class CallOfShadowsSpellEffect : GameSpellEffect
    {
        public CallOfShadowsSpellEffect(ISpellHandler handler, int duration, int pulseFreq)
            : base(handler, duration, pulseFreq) { }

        public CallOfShadowsSpellEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(handler, duration, pulseFreq, effectiveness) { }
    }

    /// </summary>
    [SpellHandler("CallOfShadows")]
    public class CallOfShadowsSpellHandler : AbstractMorphSpellHandler
    {
        private const string FLAG_ACTIVE = "COS_ACTIVE";
        private const string FLAG_UNINTERRUPTIBLE = "COS_UNINTERRUPTIBLE";

        private const int MODEL = 1695;
        private const int DISEASE_ID = 25296;
        private const int DISEASE_PCT = 50;

        private const int DEC_SPELL_DMG_PCT = 10;
        private const int DEC_ABSORB_PCT = 5;
        private const float DEC_REGEN_MULT = 5.5f;

        private const int CHT_VALUE_CORE = 30;
        private const int CHT_PER_LVL_AF = 4;
        private const int CHT_SEC_RES_PCT = 15;
        private static readonly int CHT_ABSORB_PCT = (int)Math.Round(CHT_VALUE_CORE * (2.0 / 3.0));
        private const int SPI_STEALTH_DET = 20;

        // ---- Global booster ----
        private const double BOOST = 1.10;

        // Snapshots we apply/undo
        private int _spellDmgBonus;
        private int _absorbAll;
        private float _regenMult;
        private int _hpFlat;
        private int _wsPct;
        private int _secResPct;
        private int _afFlat;
        private int _tempParryLevel;
        private int _stealthDet;

        private bool _hookedEvents;

        public CallOfShadowsSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            Priority = 700;
            OverwritesMorphs = true;
        }

        public override ushort GetModelFor(GameLiving living) => MODEL;

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
            => new GameSpellEffect(this, CalculateEffectDuration(target, effectiveness), 0);

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            // Note: THIS WILL NOT BE ABLE TO HANDLE A RADIUS ON THIS SPELL.

            var o = effect.Owner;
            var gp = o as GamePlayer;

            // Flags: uninterruptible (no move-cast flag set)
            o.TempProperties.setProperty(FLAG_ACTIVE, true);
            o.TempProperties.setProperty(FLAG_UNINTERRUPTIBLE, true);

            // Decrepit:
            _spellDmgBonus = (int)Math.Round(DEC_SPELL_DMG_PCT * BOOST);
            int decrepitAbs = (int)Math.Round(DEC_ABSORB_PCT * BOOST);
            _regenMult = (float)(DEC_REGEN_MULT * BOOST);

            // Chtonic:
            _wsPct = (int)Math.Round(CHT_VALUE_CORE * BOOST);
            _secResPct = (int)Math.Round(CHT_SEC_RES_PCT * BOOST);
            int chtAbs = (int)Math.Round(CHT_ABSORB_PCT * BOOST);

            // Spirit:
            _stealthDet = (int)Math.Round(SPI_STEALTH_DET * BOOST);

            // HP flat from % of current MaxHealth: 30% -> 33%
            _hpFlat = (int)Math.Round(o.MaxHealth * (_wsPct / 100.0));

            // AF flat from per-level and %AF component in your Chtonic logic, then +10%
            // base AF calc (see Chtonic handler)
            int level = o.Level;
            int flatAFBase = (CHT_PER_LVL_AF * level) * 5;
            int afProp = o.GetModified(eProperty.ArmorFactor);
            int pctAFComp = (int)Math.Round(afProp * (CHT_VALUE_CORE / 100.0));
            _afFlat = (int)Math.Round((flatAFBase + pctAFComp) * BOOST);

            // Total absorb pool (stack Decrepit + Chtonic flavors)
            _absorbAll = decrepitAbs + chtAbs;

            // Decrepit bonuses:
            o.SpecBuffBonusCategory[(int)eProperty.SpellDamage] += _spellDmgBonus;
            o.SpecBuffBonusCategory[(int)eProperty.DotDamageBonus] += _spellDmgBonus;
            o.SpecBuffBonusCategory[(int)eProperty.MagicAbsorption] += _absorbAll;

            // Regen (multiplicative bucket)
            if (Math.Abs(_regenMult) > 0.0001f)
                o.BuffBonusMultCategory1.Set((int)eProperty.HealthRegenerationRate, this, _regenMult);

            // Chtonic bonuses:
            o.BuffBonusCategory4[(int)eProperty.ArmorFactor] += _afFlat;
            o.BaseBuffBonusCategory[(int)eProperty.MaxHealth] += _hpFlat;
            o.SpecBuffBonusCategory[(int)eProperty.WeaponSkill] += _wsPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Heat] += _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Cold] += _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Matter] += _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Body] += _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Spirit] += _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Energy] += _secResPct;

            // Spirit Bonus:
            o.BaseBuffBonusCategory[(int)eProperty.StealthDetectionBonus] += _stealthDet;

            // Parry specialization (temporary) + parry chance
            if (gp != null)
                new RegionTimerAction<GamePlayer>(gp, p => _tempParryLevel = GS.CharacterClassOccultist.ModTempParry(p, true, (int)p.Level)).Start(1);

            GameEventMgr.AddHandler(o, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
            _hookedEvents = true;

            GameEventMgr.AddHandler(o, GameLivingEvent.AttackedByEnemy, ClearInterruptIfCasting);

            // Pet “spirit-like” template swap
            o.TempProperties.setProperty(KEY_COS, true);

            if (o is GamePlayer cosOwner)
                SetOccultistPetForm(cosOwner, true);

            if (gp != null)
            {
                gp.Out.SendUpdateWeaponAndArmorStats();
                gp.Out.SendCharStatsUpdate();
                gp.Out.SendCharResistsUpdate();
                gp.UpdatePlayerStatus();
                gp.Out.SendUpdatePlayer();
            }
            else
            {
                o.UpdateHealthManaEndu();
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            var o = effect.Owner;
            var gp = o as GamePlayer;

            o.SpecBuffBonusCategory[(int)eProperty.SpellDamage] -= _spellDmgBonus;
            o.SpecBuffBonusCategory[(int)eProperty.DotDamageBonus] -= _spellDmgBonus;

            o.SpecBuffBonusCategory[(int)eProperty.MagicAbsorption] -= _absorbAll;

            o.BuffBonusMultCategory1.Remove((int)eProperty.HealthRegenerationRate, this);

            o.BuffBonusCategory4[(int)eProperty.ArmorFactor] -= _afFlat;
            o.BaseBuffBonusCategory[(int)eProperty.MaxHealth] -= _hpFlat;
            o.SpecBuffBonusCategory[(int)eProperty.WeaponSkill] -= _wsPct;

            o.SpecBuffBonusCategory[(int)eProperty.Resist_Heat] -= _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Cold] -= _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Matter] -= _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Body] -= _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Spirit] -= _secResPct;
            o.SpecBuffBonusCategory[(int)eProperty.Resist_Energy] -= _secResPct;

            o.BaseBuffBonusCategory[(int)eProperty.StealthDetectionBonus] -= _stealthDet;
            _stealthDet = 0;

            if (gp != null && _tempParryLevel != 0)
                new RegionTimerAction<GamePlayer>(gp, p => GS.CharacterClassOccultist.ModTempParry(p, false, _tempParryLevel)).Start(1);

            if (_hookedEvents)
            {
                GameEventMgr.RemoveHandler(o, GameLivingEvent.AttackedByEnemy, OnAttackedByEnemy);
                GameEventMgr.RemoveHandler(o, GameLivingEvent.AttackedByEnemy, ClearInterruptIfCasting);
                _hookedEvents = false;
            }

            o.TempProperties.removeProperty(FLAG_ACTIVE);
            o.TempProperties.removeProperty(FLAG_UNINTERRUPTIBLE);

            // Pets: flip back to base templates
            o.TempProperties.removeProperty(KEY_COS);

            if (o is GamePlayer cosOwner)
                SetOccultistPetForm(cosOwner, false);

            if (gp != null)
            {
                gp.Out.SendUpdateWeaponAndArmorStats();
                gp.Out.SendCharStatsUpdate();
                gp.Out.SendCharResistsUpdate();
                gp.UpdatePlayerStatus();
                gp.Out.SendUpdatePlayer();
            }
            else
            {
                o.UpdateHealthManaEndu();
            }

            return base.OnEffectExpires(effect, noMessages);
        }

        private void OnAttackedByEnemy(DOLEvent e, object sender, EventArgs args)
        {
            if (args is not AttackedByEnemyEventArgs { AttackData: { } ad })
                return;

            if (!ad.IsSuccessfulHit || (!ad.IsMeleeAttack && !ad.IsRangedAttack))
                return;

            if ((ad.Damage + ad.CriticalDamage) <= 0)
                return;

            // --- Armor Absorption ---
            if (_absorbAll > 0)
            {
                var pct = (_absorbAll / 100.0);
                int reduceBase = (int)Math.Round(ad.Damage * pct);
                int reduceCrit = (int)Math.Round(ad.CriticalDamage * pct);
                ad.Damage -= reduceBase;
                ad.CriticalDamage -= reduceCrit;
            }

            if (!Util.Chance(DISEASE_PCT))
                return;

            var owner = (GameLiving)sender;

            var disease = SkillBase.GetSpellByID(DISEASE_ID) ?? new Spell(new DBSpell
            {
                SpellID = DISEASE_ID,
                Name = "Shadow Rot",
                Description = "A rotting disease that slows, weakens, and inhibits heals.",
                Type = "Disease",
                Target = "enemy",
                Damage = 30,
                DamageType = (int)eDamageType.Body,
                Duration = 20000
            }, 50);

            var line = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            ScriptMgr.CreateSpellHandler(owner, disease, line)?.StartSpell(ad.Attacker);
        }

        private void ClearInterruptIfCasting(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer gp) return;
            if (!gp.IsCasting) return;
            if (!gp.TempProperties.getProperty(FLAG_ACTIVE, false)) return;

            gp.DisabledCastingTimeout = 0;
        }
    }
}
