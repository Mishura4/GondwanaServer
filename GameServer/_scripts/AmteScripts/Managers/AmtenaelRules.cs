using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmteScripts.Managers;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.AI.Brain;
using DOL.GS.Keeps;
using DOL.GS.Scripts;
using log4net;
using System.Reflection;
using DOL.Events;
using DOL.GS.PlayerClass;
using DOL.gameobjects.CustomNPC;
using DOL.GS.Finance;
using DOL.Language;
using DOL.Territories;

namespace DOL.GS.ServerRules
{
    [ServerRules(eGameServerType.GST_PvP)]
    public class AmtenaelRules : PvPServerRules
    {
        public static ushort HousingRegionID = 202;
        public static ushort[] UnsafeRegions = new ushort[] { 181 };

        private static readonly List<ushort> BannerDisabledRegionIDs = new List<ushort>();

        /// <summary>
        /// Holds the delegate called when PvE invulnerability is expired
        /// </summary>
        protected GamePlayer.InvulnerabilityExpiredCallback m_pveinvExpiredCallback;

        /// <summary>
        /// This is called when server rules are reloaded for example when reloading server properties
        /// </summary>
        /// <returns></returns>
        public override void Reload()
        {
            lock (BannerDisabledRegionIDs)
            {
                BannerDisabledRegionIDs.Clear();
                if (!String.IsNullOrEmpty(Properties.GUILD_BANNER_DISABLED_REGIONS))
                {
                    foreach (string id in Properties.GUILD_BANNER_DISABLED_REGIONS.Split('|'))
                    {
                        if (!ushort.TryParse(id, out ushort regionId))
                        {
                            log.ErrorFormat("Could not parse region ID {0} for server property GUILD_BANNER_DISABLED_REGIONS", id);
                        }
                        else
                        {
                            BannerDisabledRegionIDs.Add(regionId);
                        }
                    }
                }
            }
        }

        public override string RulesDescription()
        {
            return "Règles de Gondwana (PvP + RvR)";
        }

        public override void OnReleased(DOLEvent e, object sender, EventArgs args)
        {
            if (RvrManager.Instance.IsInRvr(sender as GameLiving) || PvpManager.Instance.IsIn(sender as GameLiving))
                return;
            StartPVEImmunityTimer((GamePlayer)sender, Properties.TIMER_KILLED_BY_MOB * 1000);
            base.OnReleased(e, sender, args);
        }

        private bool _IsAllowedToAttack_PvpImmunity(GameLiving attacker, GamePlayer playerAttacker, GamePlayer playerDefender, bool quiet)
        {
            if (playerDefender != null)
            {
                if (playerDefender.Client.ClientState == GameClient.eClientState.WorldEnter)
                {
                    if (!quiet)
                        MessageToLiving(attacker, playerDefender.Name + " est en train de se connecter, vous ne pouvez pas l'attaquer pour le moment.");
                    return false;
                }

                if (playerAttacker != null && !UnsafeRegions.Contains(playerAttacker.CurrentRegionID))
                {
                    // Attacker immunity
                    if (playerAttacker.IsInvulnerableToAttack)
                    {
                        if (quiet == false)
                            MessageToLiving(attacker, "You can't attack players until your PvP invulnerability timer wears off!");
                        return false;
                    }

                    // Defender immunity
                    if (playerDefender.IsInvulnerableToAttack)
                    {
                        if (quiet == false)
                            MessageToLiving(attacker, playerAttacker.GetPersonalizedName(playerDefender) + " is temporarily immune to PvP attacks!");
                        return false;
                    }
                }
            }
            return true;
        }

        public override void Initialize()
        {
            m_pveinvExpiredCallback = new GamePlayer.InvulnerabilityExpiredCallback(PVEImmunityExpiredCallback);
            base.Initialize();
        }

        /// <summary>
        /// Removes immunity from the players
        /// </summary>
        /// <player></player>
        public void PVEImmunityExpiredCallback(GamePlayer player)
        {
            if (player.ObjectState != GameObject.eObjectState.Active) return;
            if (player.Client.IsPlaying == false) return;

            player.Out.SendMessage("Your pve temporary invulnerability timer has expired.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            return;
        }

        /// <summary>
        /// Starts the immunity timer for a player
        /// </summary>
        /// <param name="player">player that gets immunity</param>
        /// <param name="duration">amount of milliseconds when immunity ends</param>
        public void StartPVEImmunityTimer(GamePlayer player, int duration)
        {
            if (duration > 0)
            {
                player.StartPVEInvulnerabilityTimer(duration, m_pveinvExpiredCallback);
            }
        }

        /// <summary>
        /// Called when player has changed the region
        /// </summary>
        /// <param name="e">event</param>
        /// <param name="sender">GamePlayer object that has changed the region</param>
        /// <param name="args"></param>
        public override void OnRegionChanged(DOLEvent e, object sender, EventArgs args)
        {
            StartPVEImmunityTimer((GamePlayer)sender, Properties.TIMER_PVE_REGION_CHANGED * 1000);
            base.OnRegionChanged(e, sender, args);
        }

        /// <summary>
        /// Should be called whenever a player teleports to a new location
        /// </summary>
        /// <param name="player"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public override void OnPlayerTeleport(GamePlayer player, Teleport destination)
        {
            // Since region change already starts an immunity timer we only want to do this if a player
            // is teleporting within the same region
            if (player.CurrentRegionID == destination.RegionID)
            {
                StartPVEImmunityTimer(player, Properties.TIMER_PVE_TELEPORT * 1000);
            }
            base.OnPlayerTeleport(player, destination);
        }

        /// <summary>
        /// Called when player enters the game for first time
        /// </summary>
        /// <param name="e">event</param>
        /// <param name="sender">GamePlayer object that has entered the game</param>
        /// <param name="args"></param>
        public override void OnGameEntered(DOLEvent e, object sender, EventArgs args)
        {
            StartPVEImmunityTimer((GamePlayer)sender, Properties.TIMER_GAME_ENTERED * 1000);
            base.OnGameEntered(e, sender, args);
        }

        public override bool IsAllowedToHelp(GameLiving source, GameLiving target, bool quiet)
        {
            if (source == null || target == null)
            {
                return false;
            }

            if (target == source)
            {
                return true;
            }

            if (!target.IsVisibleTo(source) || target is AreaEffect)
            {
                return false;
            }

            if (source is GameNPC srcNpc)
            {
                if (!IsSameRealm(source, target, true))
                {
                    if (target is GamePlayer tarPlayer)
                    {
                        if (!MobGroups.MobGroup.IsQuestFriendly(srcNpc, tarPlayer))
                        {
                            if (!quiet) MessageToLiving(source, "Cette cible est hostile.");
                            return false;
                        }
                    }
                    else
                    {
                        if (!quiet) MessageToLiving(source, "Cette cible est hostile.");
                        return false;
                    }
                }
                else // same realm
                {
                     if (target is GameNPC { IsCannotTarget: true })
                     {
                         return false;
                     }
                }
            }
            else if (source is GamePlayer srcPlayer)
            {
                if (!IsSameRealm(source, target, true))
                {
                    if (target is GamePlayer tarPlayer)
                    {
                        if (srcPlayer.Guild != tarPlayer.Guild)
                        {
                            if (!quiet) MessageToLiving(source, "Cette cible est hostile.");
                            return false;
                        }
                    }
                    else
                    {
                        if (!quiet) MessageToLiving(source, "Cette cible est hostile.");
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
        {
            if (attacker == null || defender == null)
                return false;

            if ((defender is ShadowNPC) || (defender is AreaEffect))
                return false;

            //dead things can't attack
            if (!defender.IsAlive || !attacker.IsAlive)
                return false;

            if (attacker == defender)
            {
                if (quiet == false) MessageToLiving(attacker, "Vous ne pouvez pas vous attaquer vous-même.");
                return false;
            }

            // PEACE NPCs can't be attacked/attack
            var attackerNpc = attacker as GameNPC;
            var defenderNpc = defender as GameNPC;
            if (attackerNpc != null && (attackerNpc.IsPeaceful) ||
                (defenderNpc != null && (defenderNpc.IsPeaceful)))
                return false;

            var playerAttacker = attacker as GamePlayer;
            var playerDefender = defender as GamePlayer;

            // if friend, let's define the controller once
            if (defenderNpc != null)
            {
                if (defenderNpc.Brain is IControlledBrain controlledBrain)
                {
                    playerDefender = controlledBrain.GetPlayerOwner();
                }

                if (defenderNpc is FollowingFriendMob followMob)
                {
                    playerDefender = followMob.PlayerFollow;
                }
            }

            if (attackerNpc != null)
            {
                if (attackerNpc.Brain is IControlledBrain controlledBrain)
                {
                    playerAttacker = controlledBrain.GetPlayerOwner();
                }
                quiet = false;
            }

            if (playerDefender != null && playerDefender == playerAttacker)
            {
                if (quiet == false) MessageToLiving(attacker, "Vous ne pouvez pas vous attaquer vous-même.");
                return false;
            }


            //GMs can't be attacked
            if (playerDefender != null && playerDefender.Client.Account.PrivLevel > 1 && !Properties.ALLOW_GM_ATTACK)
                return false;

            if (attackerNpc != null && playerDefender != null)
            {
                if (MobGroups.MobGroup.IsQuestFriendly(attackerNpc, playerDefender))
                {
                    return false;
                }

                // PEACE NPCs can't be attacked/attack
                if (this.IsPeacefulNPC(attackerNpc, defenderNpc, playerDefender))
                {
                    return false;
                }
                //Territory
                //Check guilds and ally Guilds
                if (attackerNpc.IsInTerritory && playerDefender.GuildName != null)
                {
                    if (attackerNpc.GuildName.Equals(playerDefender.GuildName))
                    {
                        return false;
                    }

                    var guild = GuildMgr.GetGuildByName(playerDefender.GuildName);

                    if (guild != null && guild.alliance != null && guild.alliance.Guilds != null)
                    {
                        foreach (var guildObj in guild.alliance.Guilds)
                        {
                            Guild allyGuild = guildObj as Guild;

                            if (allyGuild == null)
                            {
                                continue;
                            }

                            if (attacker.GuildName.Equals(allyGuild.Name))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            //Groupmobs quest friendly
            if (playerAttacker != null)
            {
                if (defenderNpc != null)
                {
                    if (MobGroups.MobGroup.IsQuestFriendly(defenderNpc, playerAttacker))
                    {
                        return false;
                    }
                    // PEACE NPCs can't be attacked/attack
                    if (this.IsPeacefulNPC(attackerNpc, defenderNpc, playerAttacker))
                    {
                        return false;
                    }

                    // Forbid attacks against a player's own territory NPCs
                    if (defenderNpc.CurrentTerritory?.IsOwnedBy(playerAttacker) == true)
                    {
                        return false;
                    }
                }

                // PVE Timer
                if (playerAttacker.IsInvulnerableToPVEAttack)
                {
                    if (quiet == false) MessageToLiving(attacker, "You can't attack mobs until your PvE invulnerability timer wears off!");
                    return false;
                }
            }

            if (playerDefender != null)
            {
                // PVE Timer
                if (playerDefender.IsInvulnerableToPVEAttack)
                {
                    return false;
                }

                if (playerAttacker != null) // PVP
                {
                    if (attacker.CurrentRegionID == HousingRegionID || defender.CurrentRegionID == HousingRegionID)
                    {
                        return false;
                    }
                    if (!_IsAllowedToAttack_PvpImmunity(attacker, playerAttacker, playerDefender, quiet))
                        return false;
                }
            }

            // Your pet can only attack stealthed players you have selected
            if (defender.IsStealthed && attackerNpc != null)
            {
                var contBrain = attackerNpc.Brain as IControlledBrain;
                if (contBrain != null && playerDefender != null &&
                    attacker.TargetObject != defender)
                    return false;
            }

            //Checking for shadowed necromancer, can't be attacked.
            if (defender.ControlledBrain != null && defender.ControlledBrain.Body != null && defender.ControlledBrain.Body is NecromancerPet)
            {
                if (quiet == false) MessageToLiving(attacker, "You can't attack a shadowed necromancer!");
                return false;
            }

            // Pets
            if (attackerNpc != null)
            {
                var controlled = attackerNpc.Brain as IControlledBrain;
                if (controlled != null)
                {
                    var newAttacker = controlled.GetLivingOwner();
                    if (newAttacker != null)
                    {
                        attacker = newAttacker;
                    }
                    quiet = true; // silence all attacks by controlled npc
                }
            }
            if (defenderNpc != null)
            {
                if (((GameNPC)defender).Brain is IControlledBrain controlled)
                    defender = controlled.GetLivingOwner() ?? defender;
                else if (defender is FollowingFriendMob followMob)
                    defender = followMob.PlayerFollow ?? defender;
            }

            if (playerAttacker != null && JailMgr.IsPrisoner(playerAttacker))
            {
                if (quiet == false)
                    MessageToLiving(attacker, "Vous ne pouvez pas attaquer lorsque vous êtes en prison.");
                return false;
            }
            if (playerDefender != null && JailMgr.IsPrisoner(playerDefender))
            {
                if (quiet == false)
                    MessageToLiving(attacker, "Vous ne pouvez pas attaquer un prisonnier.");
                return false;
            }

            // RvR Rules
            if (RvrManager.Instance != null && RvrManager.Instance.IsInRvr(attacker))
                return RvrManager.Instance.IsAllowedToAttack(attacker, defender, quiet);

            // Safe area
            if (attacker is GamePlayer && defender is GamePlayer)
            {
                if (defender.CurrentAreas.Cast<AbstractArea>().Any(area => area.IsSafeArea) ||
                    attacker.CurrentAreas.Cast<AbstractArea>().Any(area => area.IsSafeArea))
                {
                    if (quiet == false)
                        MessageToLiving(attacker, "Vous ne pouvez pas attaquer quelqu'un dans une zone safe !");
                    return false;
                }
            }

            // PVP)
            if (playerAttacker != null && playerDefender != null)
            {
                //check group
                if (playerAttacker.Group != null && playerAttacker.Group.IsInTheGroup(playerDefender))
                {
                    if (!quiet) MessageToLiving(playerAttacker, "Vous ne pouvez pas attaquer un membre de votre groupe.");
                    return false;
                }

                if (playerAttacker.DuelTarget != defender)
                {
                    //check guild
                    if (playerAttacker.Guild != null && playerAttacker.Guild == playerDefender.Guild)
                    {
                        if (!quiet) MessageToLiving(playerAttacker, "Vous ne pouvez pas attaquer un membre de votre guilde.");
                        return false;
                    }

                    // Player can't hit other members of the same BattleGroup
                    var mybattlegroup = (BattleGroup)playerAttacker.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                    if (mybattlegroup != null && mybattlegroup.IsInTheBattleGroup(playerDefender))
                    {
                        if (!quiet) MessageToLiving(playerAttacker, "Vous ne pouvez pas attaquer un membre de votre groupe de combat.");
                        return false;
                    }
                }
            }

            // Simple GvG Guards
            if (defender is SimpleGvGGuard && (defender.GuildName == attacker.GuildName || (playerAttacker != null && playerAttacker.GuildName == defender.GuildName)))
                return false;
            if (attacker is SimpleGvGGuard && (defender.GuildName == attacker.GuildName || (playerDefender != null && playerDefender.GuildName == attacker.GuildName)))
                return false;

            // allow mobs to attack mobs
            if (attacker.Realm == 0 && defender.Realm == 0)
            {
                if (attacker is GameNPC && !((GameNPC)attacker).IsConfused &&
                    defender is GameNPC && !((GameNPC)defender).IsConfused)
                    return !((GameNPC)attacker).IsFriend((GameNPC)defender);
                return true;
            }
            if ((attacker.Realm != 0 || defender.Realm != 0) && playerDefender == null && playerAttacker == null)
                return true;

            //allow confused mobs to attack same realm
            if (attacker is GameNPC && (attacker as GameNPC).IsConfused && attacker.Realm == defender.Realm)
                return true;

            // "friendly" NPCs can't attack "friendly" players
            if (defender is GameNPC && defender.Realm != 0 && attacker.Realm != 0 && defender is GameKeepGuard == false && defender is GameFont == false)
            {
                if (quiet == false) MessageToLiving(attacker, "Vous ne pouvez pas attaquer un PNJ amical.");
                return false;
            }
            // "friendly" NPCs can't be attacked by "friendly" players
            if (attacker is GameNPC && attacker.Realm != 0 && defender.Realm != 0 && attacker is GameKeepGuard == false)
            {
                if (MobGroups.MobGroup.IsQuestAggresive(attackerNpc, playerDefender))
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        public override bool IsSameRealm(GameLiving source, GameLiving target, bool quiet)
        {
            if (source == null || target == null)
                return false;

            GameLiving realSource = source;
            GameLiving realTarget = target;

            if (realSource is GameNPC { Brain: IControlledBrain sourceBrain })
                realSource = sourceBrain.Owner;

            if (realTarget is GameNPC { Brain: IControlledBrain targetBrain })
                realTarget = targetBrain.Owner;
            
            var targetPlayer = realTarget as GamePlayer;
            var sourcePlayer = realSource as GamePlayer;

            var targetNpc = realTarget as GameNPC;
            var sourceNpc = realSource as GameNPC;

            if (sourceNpc != null)
            {
                if (targetPlayer != null)
                {
                    if (MobGroups.MobGroup.IsQuestAggresive(sourceNpc, targetPlayer))
                    {
                        return false;
                    }
                    if (realSource is GuardNPC && targetPlayer.Reputation >= 0)
                    {
                        return true;
                    }
                    if (sourceNpc.CurrentTerritory?.IsOwnedBy(targetPlayer) == true)
                    {
                        return true;
                    }
                }
                else if (targetNpc != null)
                {
                    if (sourceNpc.CurrentTerritory != null && sourceNpc.CurrentTerritory == targetNpc.CurrentTerritory)
                    {
                        return true;
                    }
                }

                if (sourceNpc.IsPeaceful)
                    return true;
            }
            else if (sourcePlayer != null)
            {
                if (targetNpc != null)
                {
                    if (MobGroups.MobGroup.IsQuestAggresive(targetNpc, sourcePlayer))
                    {
                        return false;
                    }
                    if (targetNpc.CurrentTerritory?.IsOwnedBy(targetPlayer) == true)
                    {
                        return true;
                    }

                    if (targetNpc.IsPeaceful)
                        return true;
                }
            }

            if (RvrManager.Instance != null && (RvrManager.Instance.IsInRvr(realSource) || RvrManager.Instance.IsInRvr(realTarget)))
                return realSource.Realm == realTarget.Realm;

            if (realSource.Attackers != null && realTarget.Attackers.Contains(realTarget))
                return false;

            return base.IsSameRealm(source, target, quiet);
        }

        public override bool CheckAbilityToUseItem(GameLiving living, ItemTemplate item)
        {
            if (living == null || item == null)
                return false;

            GamePlayer player = living as GamePlayer;

            // GMs can equip everything
            if (player != null && player.Client.Account.PrivLevel > (uint)ePrivLevel.Player)
                return true;

            // allow usage of all house items
            if ((item.Object_Type == 0 || item.Object_Type >= (int)eObjectType._FirstHouse) && item.Object_Type <= (int)eObjectType._LastHouse)
                return true;

            // on some servers we may wish for dropped items to be used by all realms regardless of what is set in the db
            if (!Properties.ALLOW_CROSS_REALM_ITEMS && item.Realm != 0 && item.Realm != (int)living.Realm)
                return false;

            // classes restriction. 0 means every class
            if (player != null && !Util.IsEmpty(item.AllowedClasses, true) && !Util.SplitCSV(item.AllowedClasses, true).Contains(player.CharacterClass.ID.ToString()))
                return false;

            //armor
            if (item.Object_Type >= (int)eObjectType._FirstArmor && item.Object_Type <= (int)eObjectType._LastArmor)
            {
                int armorAbility = -1;
                switch ((eRealm)item.Realm)
                {
                    case eRealm.Albion: armorAbility = living.GetAbilityLevel(Abilities.AlbArmor); break;
                    case eRealm.Hibernia: armorAbility = living.GetAbilityLevel(Abilities.HibArmor); break;
                    case eRealm.Midgard: armorAbility = living.GetAbilityLevel(Abilities.MidArmor); break;
                    default: // use old system
                        armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.AlbArmor));
                        armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.HibArmor));
                        armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.MidArmor));
                        break;
                }
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.GenericArmor: return armorAbility >= ArmorLevel.GenericArmor;
                    case eObjectType.Cloth: return armorAbility >= ArmorLevel.Cloth;
                    case eObjectType.Leather: return armorAbility >= ArmorLevel.Leather;
                    case eObjectType.Reinforced:
                    case eObjectType.Studded: return armorAbility >= ArmorLevel.Studded;
                    case eObjectType.Scale:
                    case eObjectType.Chain: return armorAbility >= ArmorLevel.Chain;
                    case eObjectType.Plate: return armorAbility >= ArmorLevel.Plate;
                    default: return false;
                }
            }

            // non-armors
            string abilityCheck = null;
            string[] otherCheck = new string[0];

            //http://dol.kitchenhost.de/files/dol/Info/itemtable.txt
            switch ((eObjectType)item.Object_Type)
            {
                case eObjectType.GenericItem: return true;
                case eObjectType.GenericArmor: return true;
                case eObjectType.GenericWeapon: return true;
                case eObjectType.Staff: abilityCheck = Abilities.Weapon_Staves; break;
                case eObjectType.Fired: abilityCheck = Abilities.Weapon_Shortbows; break;
                case eObjectType.FistWraps: abilityCheck = Abilities.Weapon_FistWraps; break;
                case eObjectType.MaulerStaff: abilityCheck = Abilities.Weapon_MaulerStaff; break;

                //alb
                case eObjectType.CrushingWeapon: abilityCheck = Abilities.Weapon_Crushing; break;
                case eObjectType.SlashingWeapon: abilityCheck = Abilities.Weapon_Slashing; break;
                case eObjectType.ThrustWeapon: abilityCheck = Abilities.Weapon_Thrusting; break;
                case eObjectType.TwoHandedWeapon: abilityCheck = Abilities.Weapon_TwoHanded; break;
                case eObjectType.PolearmWeapon: abilityCheck = Abilities.Weapon_Polearms; break;
                case eObjectType.Longbow:
                    otherCheck = new[] { Abilities.Weapon_Longbows, Abilities.Weapon_Archery };
                    break;
                case eObjectType.Crossbow: abilityCheck = Abilities.Weapon_Crossbow; break;
                case eObjectType.Flexible: abilityCheck = Abilities.Weapon_Flexible; break;
                //TODO: case 5: abilityCheck = Abilities.Weapon_Thrown; break;

                //mid
                case eObjectType.Sword: abilityCheck = Abilities.Weapon_Swords; break;
                case eObjectType.Hammer: abilityCheck = Abilities.Weapon_Hammers; break;
                case eObjectType.LeftAxe:
                case eObjectType.Axe: abilityCheck = Abilities.Weapon_Axes; break;
                case eObjectType.Spear: abilityCheck = Abilities.Weapon_Spears; break;
                case eObjectType.CompositeBow:
                    otherCheck = new[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Archery };
                    break;
                case eObjectType.Thrown: abilityCheck = Abilities.Weapon_Thrown; break;
                case eObjectType.HandToHand: abilityCheck = Abilities.Weapon_HandToHand; break;

                //hib
                case eObjectType.RecurvedBow:
                    otherCheck = new[] { Abilities.Weapon_RecurvedBows, Abilities.Weapon_Archery };
                    break;
                case eObjectType.Blades: abilityCheck = Abilities.Weapon_Blades; break;
                case eObjectType.Blunt: abilityCheck = Abilities.Weapon_Blunt; break;
                case eObjectType.Piercing: abilityCheck = Abilities.Weapon_Piercing; break;
                case eObjectType.LargeWeapons: abilityCheck = Abilities.Weapon_LargeWeapons; break;
                case eObjectType.CelticSpear: abilityCheck = Abilities.Weapon_CelticSpear; break;
                case eObjectType.Scythe: abilityCheck = Abilities.Weapon_Scythe; break;

                //misc
                case eObjectType.Magical: return true;
                case eObjectType.Shield: return living.GetAbilityLevel(Abilities.Shield) >= item.Type_Damage;
                case eObjectType.Bolt: abilityCheck = Abilities.Weapon_Crossbow; break;
                case eObjectType.Arrow: otherCheck = new string[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Longbows, Abilities.Weapon_RecurvedBows, Abilities.Weapon_Shortbows }; break;
                case eObjectType.Poison: return living.GetModifiedSpecLevel(Specs.Envenom) > 0;
                case eObjectType.Instrument: return living.HasAbility(Abilities.Weapon_Instruments);
                    //TODO: different shield sizes
            }

            if (abilityCheck != null && living.HasAbility(abilityCheck))
                return true;

            foreach (string str in otherCheck)
                if (living.HasAbility(str))
                    return true;

            return false;
        }
        private bool IsPeacefulNPC(GameNPC attackerNpc, GameNPC defenderNpc, GamePlayer player)
        {
            if (attackerNpc is { IsPeaceful: true })
            {
                if (MobGroups.MobGroup.IsQuestAggresive(attackerNpc, player))
                {
                    return false;
                }

                return true;
            }

            if (defenderNpc is { IsPeaceful: true })
            {
                if (MobGroups.MobGroup.IsQuestAggresive(defenderNpc, player))
                {
                    return false;
                }

                return true;
            }

            return false;
        }
        public override bool IsAllowedToCastSpell(GameLiving caster, GameLiving target, Spell spell, SpellLine spellLine)

        {
            if (target is ShadowNPC)
                return false;
            var plc = caster as GamePlayer;
            var plt = target as GamePlayer;
            // player on horse cant heal, cure or cast a pet spell
            if (plc != null && plc.IsOnHorse && (spell.SpellType.Contains("Heal") || spell.SpellType.Contains("Cure") || spell.SpellType.Contains("Summon") || (plc.CharacterClass is ClassHeretic && spell.Pulse != 0)))
                return false;
            if ((plc != null && JailMgr.IsPrisoner(plc)) || (plt != null && JailMgr.IsPrisoner(plt)))
                return false;
            return base.IsAllowedToCastSpell(caster, target, spell, spellLine);
        }

        public override bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet)
        {
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            if (JailMgr.IsPrisoner(source) || JailMgr.IsPrisoner(target))
                return false;
            return true;
        }

        public override bool IsAllowedToJoinGuild(GamePlayer source, Guild guild)
        {
            if (RvrManager.Instance.IsInRvr(source))
                return source.Realm == guild.Realm;
            return true;
        }

        public override bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet)
        {
            var pls = source as GamePlayer;
            var plt = target as GamePlayer;
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            if ((source != null && JailMgr.IsPrisoner(pls)) || (plt != null && JailMgr.IsPrisoner(plt)))
                return false;
            return base.IsAllowedToTrade(source, target, quiet);
        }

        public override bool IsAllowedToUnderstand(GameLiving source, GamePlayer target)
        {
            if (RvrManager.Instance.IsInRvr(source) || RvrManager.Instance.IsInRvr(target))
                return source.Realm == target.Realm;
            return true;
        }

        /// <summary>
        /// Is this player allowed to summon their guild's banner
        /// </summary>
        /// <param name="player">The player trying to summon the guild banner</param>
        /// <returns></returns>
        public override bool IsAllowedToSummonBanner(GamePlayer player, bool quiet)
        {
            if (player.Client.Account.PrivLevel > (uint)ePrivLevel.Player)
                return true;

            lock (BannerDisabledRegionIDs)
            {
                if (BannerDisabledRegionIDs.Contains(player.CurrentRegionID))
                {
                    if (!quiet)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.BannerCantInDungeon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                    }
                    return false;
                }
            }
            return true;
        }

        public override string ReasonForDisallowMounting(GamePlayer player)
        {
            return RvrManager.Instance.IsInRvr(player) ? "GameObjects.GamePlayer.UseSlot.CantCallMountRVR" : base.ReasonForDisallowMounting(player);
        }
        public override string GetPlayerName(GamePlayer source, GamePlayer target)
        {
            if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
                return source.Client.RaceToTranslatedName(target.Race, target.Gender == eGender.Male ? 1 : 2);
            return base.GetPlayerName(source, target);
        }
        public override string GetPlayerLastName(GamePlayer source, GamePlayer target)
        {
            if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
                return "";
            return base.GetPlayerLastName(source, target);
        }
        public override string GetPlayerPrefixName(GamePlayer source, GamePlayer target)
        {
            if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
                return "";
            return base.GetPlayerPrefixName(source, target);
        }
        public override string GetPlayerTitle(GamePlayer source, GamePlayer target)
        {
            if (RvrManager.Instance.IsInRvr(source) && RvrManager.Instance.IsInRvr(target) && source.Realm != target.Realm)
                return "";
            return base.GetPlayerTitle(source, target);
        }
        public override byte GetColorHandling(GameClient client)
        {
            if (client.Player != null && RvrManager.Instance.IsInRvr(client.Player))
                return 0;
            return base.GetColorHandling(client);
        }

        public override void OnPlayerKilled(GamePlayer killedPlayer, GameObject killer)
        {
            var gainers = killedPlayer.XPGainers.ToArray();

            if (Properties.ENABLE_WARMAPMGR && killer is GamePlayer && killer.CurrentRegion.ID == 163)
                WarMapMgr.AddFight((byte)killer.CurrentZone.ID, (int)killer.Position.X, (int)killer.Position.Y, (byte)killer.Realm, (byte)killedPlayer.Realm);

            killedPlayer.LastDeathRealmPoints = 0;
            // "player has been killed recently"
            long noExpSeconds = Properties.RP_WORTH_SECONDS;
            if (killedPlayer.DeathTime + noExpSeconds > killedPlayer.PlayedTime)
            {
                foreach (var de in gainers)
                {
                    if (de.Key is GamePlayer pl)
                    {
                        pl.Out.SendMessage(pl.GetPersonalizedName(killedPlayer) + " has been killed recently and is worth no realm points!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        pl.Out.SendMessage(pl.GetPersonalizedName(killedPlayer) + " has been killed recently and is worth no experience!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }
                }
                return;
            }

            bool dealNoXP = false;
            var totalDamage = 0.0;
            //Collect the total damage
            foreach (var de in gainers)
            {
                GameObject obj = (GameObject)de.Key;
                if (obj is GamePlayer)
                {
                    //If a gameplayer with privlevel > 1 attacked the
                    //mob, then the players won't gain xp ...
                    if (((GamePlayer)obj).Client.Account.PrivLevel > 1)
                    {
                        dealNoXP = true;
                        break;
                    }
                }
                totalDamage += (double)de.Value;

            }

            if (dealNoXP)
            {
                foreach (var de in gainers)
                {
                    GamePlayer player = de.Key as GamePlayer;
                    if (player != null)
                        player.Out.SendMessage("You gain no experience from this kill!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                return;
            }


            long playerExpValue = killedPlayer.ExperienceValue;
            playerExpValue = (long)(playerExpValue * Properties.XP_RATE);
            int playerRPValue = killedPlayer.RealmPointsValue;
            int playerBPValue = 0;

            bool BG = false;
            if (!Properties.ALLOW_BPS_IN_BGS)
            {
                foreach (AbstractGameKeep keep in GameServer.KeepManager.GetKeepsOfRegion(killedPlayer.CurrentRegionID))
                {
                    if (keep.DBKeep.BaseLevel < 50)
                    {
                        BG = true;
                        break;
                    }
                }
            }

            if (!BG)
                playerBPValue = killedPlayer.BountyPointsValue;
            long playerMoneyValue = killedPlayer.MoneyValue;

            List<KeyValuePair<GamePlayer, int>> playerKillers = new List<KeyValuePair<GamePlayer, int>>();

            //Now deal the XP and RPs to all livings
            foreach (var de in gainers)
            {
                GameLiving living = de.Key as GameLiving;
                GamePlayer expGainPlayer = living as GamePlayer;
                if (living == null) continue;
                if (living.ObjectState != GameObject.eObjectState.Active) continue;
                /*
                 * http://www.camelotherald.com/more/2289.shtml
                 * Dead players will now continue to retain and receive their realm point credit
                 * on targets until they release. This will work for solo players as well as
                 * grouped players in terms of continuing to contribute their share to the kill
                 * if a target is being attacked by another non grouped player as well.
                 */
                //if (!living.Alive) continue;
                if (!living.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;


                var damagePercent = de.Value / totalDamage;
                if (!living.IsAlive) //Dead living gets 25% exp only
                    damagePercent *= 0.25f;

                // realm points
                int rpCap = living.RealmPointsValue * 2;
                int realmPoints = (int)(playerRPValue * damagePercent);
                //rp bonuses from RR and Group
                //20% if R1L0 char kills RR10,if RR10 char kills R1L0 he will get -20% bonus
                //100% if full group,scales down according to player count in group and their range to target
                if (living is GamePlayer killerPlayer)
                {
                    //only gain rps in a battleground if you are under the cap
                    Battleground bg = GameServer.KeepManager.GetBattleground(killerPlayer.CurrentRegionID);
                    if (bg == null || (killerPlayer.RealmLevel < bg.MaxRealmLevel))
                    {
                        realmPoints = (int)(realmPoints * (1.0 + 2.0 * (killedPlayer.RealmLevel - killerPlayer.RealmLevel) / 900.0));
                        if (killerPlayer.Group != null && killerPlayer.Group.MemberCount > 1)
                        {
                            lock (killerPlayer.Group)
                            {
                                int count = 0;
                                foreach (GamePlayer pl in killerPlayer.Group.GetPlayersInTheGroup())
                                {
                                    if (!pl.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE)) continue;
                                    count++;
                                }
                                realmPoints = (int)(realmPoints * (1.0 + count * 0.125));
                            }
                        }

                    }
                    if (killerPlayer.IsInRvR)
                    {
                        //Get Territory by RegionId
                        var territory = RvrManager.Instance.GetRvRTerritory(killerPlayer.CurrentRegionID);
                        if (territory != null)
                        {
                            if (territory.IsOwnedBy(killedPlayer))
                            {
                                int bonus = 0;
                                //Is Player inside the Territory Area?
                                var isInsideTerritory = killedPlayer.CurrentAreas.Any(territory.IsInTerritory);
                                if (isInsideTerritory)
                                {
                                    bonus = Properties.RvR_INSIDE_AREA_RP_BONUS;
                                }
                                else
                                {
                                    //otherwise give small bonus
                                    bonus = Properties.RvR_OUTSIDE_AREA_RP_BONUS;
                                }
                                killerPlayer.Out.SendMessage(string.Format("Vous obtenez un bonus aux RP de {0}% grâce à votre capture du fort.", bonus), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                realmPoints += realmPoints * bonus / 100;
                            }
                        }
                    }

                    if (realmPoints > rpCap)
                        realmPoints = rpCap;
                    if (realmPoints > 0)
                    {

                        killedPlayer.LastDeathRealmPoints += realmPoints;
                        playerKillers.Add(new KeyValuePair<GamePlayer, int>(killerPlayer, realmPoints));
                        living.GainRealmPoints(realmPoints);
                    }
                }

                // bounty points
                int bpCap = living.BountyPointsValue * 2;
                int bountyPoints = (int)(playerBPValue * damagePercent);
                if (bountyPoints > bpCap)
                    bountyPoints = bpCap;

                //FIXME: [WARN] this is guessed, i do not believe this is the right way, we will most likely need special messages to be sent
                //apply the keep bonus for bounty points
                if (killer != null)
                {
                    if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_5, (eRealm)killer.Realm))
                        bountyPoints += (bountyPoints / 100) * 5;
                    else if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Bounty_Points_3, (eRealm)killer.Realm))
                        bountyPoints += (bountyPoints / 100) * 3;
                }

                if (bountyPoints > 0)
                {
                    living.GainBountyPoints(bountyPoints);
                }

                // experience
                // TODO: pets take 25% and owner gets 75%
                long xpReward = (long)(playerExpValue * damagePercent); // exp for damage percent

                long expCap = (long)(living.ExperienceValue * ServerProperties.Properties.XP_PVP_CAP_PERCENT / 100);
                if (xpReward > expCap)
                    xpReward = expCap;

                //outpost XP
                //1.54 http://www.camelotherald.com/more/567.shtml
                //- Players now receive an exp bonus when fighting within 16,000
                //units of a keep controlled by your realm or your guild.
                //You get 20% bonus if your guild owns the keep or a 10% bonus
                //if your realm owns the keep

                long outpostXP = 0;

                if (!BG && living is GamePlayer)
                {
                    AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(living.Position, 16000);
                    if (keep != null)
                    {
                        byte bonus = 0;
                        if (keep.Guild != null && keep.Guild == (living as GamePlayer).Guild)
                            bonus = 20;
                        else if (GameServer.Instance.Configuration.ServerType == eGameServerType.GST_Normal &&
                                 keep.Realm == living.Realm)
                            bonus = 10;

                        outpostXP = (xpReward / 100) * bonus;
                    }
                }
                xpReward += outpostXP;

                living.GainExperience(GameLiving.eXPSource.Player, xpReward);

                // gold
                GamePlayer player = living as GamePlayer;
                if (player != null)
                {
                    long money = (long)(playerMoneyValue * damagePercent);
                    if (player.GetSpellLine("Spymaster") != null)
                    {
                        money += 20 * money / 100;
                    }
                    //long money = (long)(Money.GetMoney(0, 0, 17, 85, 0) * damagePercent * killedPlayer.Level / 50);
                    player.AddMoney(Currency.Copper.Mint(money));
                    player.SendSystemMessage(string.Format("You receive {0}", Money.GetString(money)));
                    InventoryLogging.LogInventoryAction(killer, player, eInventoryActionType.Other, money);
                }

                if (killedPlayer.ReleaseType != GamePlayer.eReleaseType.Duel && expGainPlayer != null)
                {
                    switch (killedPlayer.Realm)
                    {
                        case eRealm.Albion:
                            expGainPlayer.KillsAlbionPlayers++;
                            if (expGainPlayer == killer)
                            {
                                expGainPlayer.KillsAlbionDeathBlows++;
                                if ((double)de.Value == totalDamage)
                                    expGainPlayer.KillsAlbionSolo++;
                            }
                            break;

                        case eRealm.Hibernia:
                            expGainPlayer.KillsHiberniaPlayers++;
                            if (expGainPlayer == killer)
                            {
                                expGainPlayer.KillsHiberniaDeathBlows++;
                                if ((double)de.Value == totalDamage)
                                    expGainPlayer.KillsHiberniaSolo++;
                            }
                            break;

                        case eRealm.Midgard:
                            expGainPlayer.KillsMidgardPlayers++;
                            if (expGainPlayer == killer)
                            {
                                expGainPlayer.KillsMidgardDeathBlows++;
                                if ((double)de.Value == totalDamage)
                                    expGainPlayer.KillsMidgardSolo++;
                            }
                            break;
                    }
                    killedPlayer.DeathsPvP++;
                }
            }

            if (Properties.LOG_PVP_KILLS && playerKillers.Count > 0)
            {
                try
                {
                    foreach (var pair in playerKillers)
                    {

                        var killLog = new PvPKillsLog();
                        killLog.KilledIP = killedPlayer.Client.TcpEndpointAddress;
                        killLog.KilledName = killedPlayer.Name;
                        killLog.KilledRealm = GlobalConstants.RealmToName(killedPlayer.Realm);
                        killLog.KillerIP = pair.Key.Client.TcpEndpointAddress;
                        killLog.KillerName = pair.Key.Name;
                        killLog.KillerRealm = GlobalConstants.RealmToName(pair.Key.Realm);
                        killLog.RPReward = pair.Value;
                        killLog.RegionName = killedPlayer.CurrentRegion.Description;
                        killLog.IsInstance = killedPlayer.CurrentRegion.IsInstance;

                        if (killedPlayer.Client.TcpEndpointAddress == pair.Key.Client.TcpEndpointAddress)
                            killLog.SameIP = 1;

                        GameServer.Database.AddObject(killLog);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}