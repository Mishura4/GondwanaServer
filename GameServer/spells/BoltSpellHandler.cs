/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.SkillHandler;
using DOL.Language;
using DOL.GS.ServerProperties;

namespace DOL.GS.Spells
{
    [SpellHandler("Bolt")]
    public class BoltSpellHandler : SpellHandler
    {
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            if ((target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent) && Spell.SpellType != "SiegeArrow")
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "SpellHandler.NoEffectOnTarget", m_caster.GetPersonalizedName(target)), eChatType.CT_SpellResisted);
                return;
            }
            base.FinishSpellCast(target);
        }

        #region LOS Checks for Keeps
        /// <summary>
        /// called when spell effect has to be started and applied to targets
        /// </summary>
        protected override bool ExecuteSpell(GameLiving target, bool force = false)
        {
            foreach (GameLiving targ in SelectTargets(target, force))
            {
                if (targ is GamePlayer && Spell.Target.ToLower() == "cone" && CheckLOS(Caster))
                {
                    GamePlayer player = targ as GamePlayer;
                    player!.Out.SendCheckLOS(Caster, player, new CheckLOSResponse(DealDamageCheckLOS));
                }
                else
                {
                    DealDamage(targ);
                }
            }

            return true;
        }

        private bool CheckLOS(GameLiving living)
        {
            foreach (AbstractArea area in living.CurrentAreas)
            {
                if (area.CheckLOS)
                    return true;
            }
            return false;
        }

        private void DealDamageCheckLOS(GamePlayer player, ushort response, ushort targetOID)
        {
            if ((response & 0x100) == 0x100)
            {
                GameLiving target = (GameLiving)(Caster.CurrentRegion.GetObject(targetOID));
                if (target != null)
                    DealDamage(target);
            }
        }

        private void DealDamage(GameLiving target)
        {
            int ticksToTarget = (int)(m_caster.GetDistanceTo(target) * 100 / 85); // 85 units per 1/10s
            int delay = 1 + ticksToTarget / 100;
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, (ushort)(delay), false, 1);
            }
            BoltOnTargetAction bolt = new BoltOnTargetAction(Caster, target, this);
            bolt.Start(1 + ticksToTarget);
        }
        #endregion

        protected class BoltOnTargetAction : RegionAction
        {
            protected readonly GameLiving m_boltTarget;

            protected readonly BoltSpellHandler m_handler;

            public BoltOnTargetAction(GameLiving actionSource, GameLiving boltTarget, BoltSpellHandler spellHandler) : base(actionSource)
            {
                if (boltTarget == null)
                    throw new ArgumentNullException("boltTarget");
                if (spellHandler == null)
                    throw new ArgumentNullException("spellHandler");
                m_boltTarget = boltTarget;
                m_handler = spellHandler;
            }

            public override void OnTick()
            {
                GameLiving target = m_boltTarget;
                GameLiving caster = (GameLiving)m_actionSource;
                if (target == null) return;
                if (target.CurrentRegionID != caster.CurrentRegionID) return;
                if (target.ObjectState != GameObject.eObjectState.Active) return;
                if (!target.IsAlive) return;

                // Related to PvP hitchance
                // http://www.camelotherald.com/news/news_article.php?storyid=2444
                // No information on bolt hitchance against npc's
                // Bolts are treated as physical attacks for the purpose of ABS only
                // Based on this I am normalizing the miss rate for npc's to be that of a standard spell

                int missrate = 0;

                if (caster is GamePlayer && target is GamePlayer)
                {
                    if (target.InCombat)
                    {
                        foreach (GameLiving attacker in target.Attackers)
                        {
                            if (attacker != caster && target.GetDistanceTo(attacker) <= 200)
                            {
                                // each attacker within 200 units adds a 20% chance to miss
                                missrate += 20;
                            }
                        }
                    }
                }

                if (target is GameNPC || caster is GameNPC)
                {
                    missrate += (int)(ServerProperties.Properties.PVE_SPELL_CONHITPERCENT * caster.GetConLevel(target));
                }

                // add defence bonus from last executed style if any
                AttackData targetAD = (AttackData)target.TempProperties.getProperty<object>(GameLiving.LAST_ATTACK_DATA, null);
                if (targetAD != null
                    && targetAD.AttackResult == GameLiving.eAttackResult.HitStyle
                    && targetAD.Style != null)
                {
                    missrate += targetAD.Style.BonusToDefense;
                }

                AttackData ad = m_handler.CalculateDamageToTarget(target, 0.5 - (caster.GetModified(eProperty.SpellDamage) * 0.01));

                if (caster.EffectList.GetOfType<AdrenalineMageSpellEffect>() != null)
                {
                    missrate -= AdrenalineMageSpellEffect.HIT_BONUS;
                }

                if (Util.Chance(missrate))
                {
                    ad.AttackResult = GameLiving.eAttackResult.Missed;
                    m_handler.MessageToCaster(LanguageMgr.GetTranslation((caster as GamePlayer)?.Client, "SpellHandler.YouMiss"), eChatType.CT_YouHit);
                    m_handler.MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer)?.Client, "SpellHandler.TargetMissed", caster.GetName(0, false)), eChatType.CT_Missed);
                    target.OnAttackedByEnemy(ad);
                    target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, caster);
                    if (target is GameNPC)
                    {
                        IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
                        if (aggroBrain != null)
                            aggroBrain.AddToAggroList(caster, 1);
                    }
                    return;
                }

                ad.Damage = (int)((double)ad.Damage * (1.0 + caster.GetModified(eProperty.SpellDamage) * 0.01));

                // Block
                bool blocked = false;
                if (target is GamePlayer)
                { // mobs left out yet
                    GamePlayer player = (GamePlayer)target;
                    InventoryItem lefthand = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                    if (lefthand != null && (player.AttackWeapon == null || player.AttackWeapon.Item_Type == Slot.RIGHTHAND || player.AttackWeapon.Item_Type == Slot.LEFTHAND))
                    {
                        if (target.IsObjectInFront(caster, 180) && lefthand.Object_Type == (int)eObjectType.Shield)
                        {
                            double shield = 0.5 * player.GetModifiedSpecLevel(Specs.Shields);
                            double blockchance = ((player.Dexterity * 2) - 100) / 40.0 + shield + 5;
                            // Removed 30% increased chance to block, can find no clear evidence this is correct - tolakram
                            blockchance -= target.GetConLevel(caster) * 5;
                            if (blockchance >= 100) blockchance = 99;
                            if (blockchance <= 0) blockchance = 1;

                            if (target.IsEngaging)
                            {
                                EngageEffect engage = target.EffectList.GetOfType<EngageEffect>();
                                if (engage != null && target.AttackState && engage.EngageTarget == caster)
                                {
                                    // Engage raised block change to 85% if attacker is engageTarget and player is in attackstate							
                                    // You cannot engage a mob that was attacked within the last X seconds...
                                    if (engage.EngageTarget.LastAttackedByEnemyTick > engage.EngageTarget.CurrentRegion.Time - EngageAbilityHandler.ENGAGE_ATTACK_DELAY_TICK)
                                    {
                                        if (engage.Owner is GamePlayer)
                                            (engage.Owner as GamePlayer)!.Out.SendMessage(LanguageMgr.GetTranslation((engage.Owner as GamePlayer)?.Client, "SpellHandler.EngageCannotBeUsed", engage.EngageTarget.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }  // Check if player has enough endurance left to engage
                                    else if (engage.Owner.Endurance < EngageAbilityHandler.ENGAGE_DURATION_LOST)
                                    {
                                        engage.Cancel(false); // if player ran out of endurance cancel engage effect
                                    }
                                    else
                                    {
                                        engage.Owner.Endurance -= EngageAbilityHandler.ENGAGE_DURATION_LOST;
                                        if (engage.Owner is GamePlayer)
                                            (engage.Owner as GamePlayer)!.Out.SendMessage(LanguageMgr.GetTranslation((engage.Owner as GamePlayer)?.Client, "SpellHandler.NotEnoughEndurance"), eChatType.CT_Skill, eChatLoc.CL_SystemWindow);

                                        if (blockchance < 85)
                                            blockchance = 85;
                                    }
                                }
                            }

                            if (blockchance >= Util.Random(1, 100))
                            {
                                m_handler.MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellHandler.BoltSpell.PartialBlock", caster.GetName(0, false)), eChatType.CT_Missed);
                                m_handler.MessageToCaster(LanguageMgr.GetTranslation((caster as GamePlayer)?.Client, "SpellHandler.BoltSpell.Blocked", player.GetName(0, true)), eChatType.CT_YouHit);
                                blocked = true;
                            }
                        }
                    }
                }

                double effectiveness = 1.0 + (caster.GetModified(eProperty.SpellDamage) * 0.01);

                // simplified melee damage calculation
                if (blocked == false)
                {
                    // TODO: armor resists to damage type

                    double damage = m_handler.Spell.Damage / 2; // another half is physical damage
                    if (target is GamePlayer)
                        ad.ArmorHitLocation = ((GamePlayer)target).CalculateArmorHitLocation(ad);

                    InventoryItem armor = null;
                    if (target.Inventory != null)
                        armor = target.Inventory.GetItem((eInventorySlot)ad.ArmorHitLocation);

                    double ws = (caster.Level * 8 * (1.0 + (caster.GetModified(eProperty.Dexterity) - 50) / 200.0));

                    damage *= ((ws + 90.68) / (target.GetArmorAF(ad.ArmorHitLocation) + 20 * 4.67));
                    damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                    ad.Modifier = (int)(damage * (ad.Target.GetResist(ad.DamageType) + SkillBase.GetArmorResist(armor, ad.DamageType)) / -100.0);
                    damage += ad.Modifier;

                    damage = damage * effectiveness;
                    damage *= (1.0 + RelicMgr.GetRelicBonusModifier(caster.Realm, eRelicType.Magic));

                    if (damage < 0) damage = 0;
                    ad.Damage += (int)damage;
                }

                if (m_handler is SiegeArrow == false)
                {
                    ad.UncappedDamage = ad.Damage;
                    ad.Damage = (int)Math.Min(ad.Damage, m_handler.DamageCap(effectiveness));
                }

                ad.Damage = (int)(ad.Damage * caster.Effectiveness);

                if (blocked == false && ad.CriticalDamage > 0)
                {
                    int critMax = (target is GamePlayer) ? ad.Damage / 2 : ad.Damage;
                    ad.CriticalDamage = Util.Random(critMax / 10, critMax);
                }

                // Attacked living may modify the attack data.
                ad.Target.ModifyAttack(ad);

                m_handler.SendDamageMessages(ad);
                m_handler.DamageTarget(ad, false, (blocked ? 0x02 : 0x14));
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, caster);
            }
        }

        public BoltSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <inheritdoc />
        public override string GetDelveDescription(GameClient delveClient)
        {
            string language = delveClient?.Account?.Language ?? Properties.SERV_LANGUAGE;
            int recastSeconds = Spell.RecastDelay / 1000;
            string description = LanguageMgr.GetTranslation(language, "SpellDescription.BoltSpell.MainDescription", Spell.Damage, LanguageMgr.GetDamageOfType(delveClient, Spell.DamageType));

            if (Spell.RecastDelay > 0)
            {
                if (Spell.IsSecondary)
                {
                    string secondDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                    string secondaryMessage = LanguageMgr.GetTranslation(language, "SpellDescription.Warlock.SecondarySpell");
                    description += "\n\n" + secondDesc + "\n\n" + secondaryMessage;
                }
                else
                {
                    string secondDesc = LanguageMgr.GetTranslation(language, "SpellDescription.Disarm.MainDescription2", recastSeconds);
                    return description + "\n\n" + secondDesc;
                }
            }

            if (Spell.SubSpellID != 0)
            {
                Spell subSpell = SkillBase.GetSpellByID((int)Spell.SubSpellID);
                if (subSpell != null)
                {
                    ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(m_caster, subSpell, null);
                    if (subSpellHandler != null)
                    {
                        string subspelldesc = subSpellHandler.GetDelveDescription(delveClient);
                        description += "\n\n" + subspelldesc;
                    }
                }
            }

            if (Spell.IsSecondary)
            {
                string secondaryMessage = LanguageMgr.GetTranslation(language, "SpellDescription.Warlock.SecondarySpell");
                description += "\n\n" + secondaryMessage;
            }

            if (Spell.IsPrimary)
            {
                string secondaryMessage = LanguageMgr.GetTranslation(language, "SpellDescription.Warlock.PrimarySpell");
                description += "\n\n" + secondaryMessage;
            }

            return description;
        }
    }
}
