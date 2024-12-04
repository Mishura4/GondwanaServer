/* Updated by Taovil  // Instant 50 with RR1-13 
only do:      /mob create DOL.GS.Trainer.I50TrainerRR1_13
 */

using System;
using DOL;
using DOL.GS;
using DOL.Events;
using DOL.GS.PacketHandler;
using System.Reflection;
using System.Collections;
using DOL.Database;
using DOL.GS.GameEvents;
using log4net;
using DOL.Language;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Reactive.Joins;
using System.Numerics;


namespace DOL.GS.Trainer
{
    [NPCGuildScript("Master Trainer")]
    public class MasterTrainer : GameTrainer
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
        public const string PRACTICE_WEAPON_ID1 = "training_mace";
        public const string PRACTICE_WEAPON_ID2 = "practice_sword";
        public const string PRACTICE_WEAPON_ID3 = "training_sword_hib";
        public const string PRACTICE_WEAPON_ID4 = "training_club";
        public const string PRACTICE_WEAPON_ID5 = "training_sword_mid";
        public const string PRACTICE_WEAPON_ID6 = "training_hammer";
        public const string PRACTICE_WEAPON_ID7 = "training_axe";
        public const string PRACTICE_WEAPON_ID8 = "practice_dirk";
        public const string PRACTICE_WEAPON_ID9 = "training_dirk";
        public const string PRACTICE_SHIELD_ID1 = "small_training_shield";
        public const string PRACTICE_SHIELD_ID2 = "training_shield";
        public const string PRACTICE_STAFF_ID1 = "training_staff";
        public const string PRACTICE_STAFF_ID2 = "trimmed_branch";

        public const string ALBWEAPON_ID1 = "slash_sword_item";
        public const string ALBWEAPON_ID2 = "crush_sword_item";
        public const string ALBWEAPON_ID3 = "thrust_sword_item";
        public const string ALBWEAPON_ID4 = "pike_polearm_item";
        public const string ALBWEAPON_ID5 = "twohand_sword_item";
        public const string ALBWEAPON_ID6 = "mauleralb_item_staff";
        public const string ALBWEAPON_ID7 = "mauleralb_item_fist";
        public const string ALBWEAPON_ID8 = "cabalist_item";
        public const string ALBWEAPON_ID9 = "cleric_item";
        public const string ALBWEAPON_ID10 = "friar_staff";
        public const string ALBWEAPON_ID11 = "reaver_item";
        public const string ALBWEAPON_ID12 = "minstrel_item";
        public const string ALBWEAPON_ID13 = "necromancer_item";
        public const string ALBWEAPON_ID14 = "scout_item";
        public const string ALBWEAPON_ID15 = "sorcerer_item";
        public const string ALBWEAPON_ID16 = "theurgist_item";
        public const string ALBWEAPON_ID17 = "wizard_item";
        public const string ALBMAULARMOR_ID1 = "vest_of_the_initiate_alb";
        public const string ALBMAULARMOR_ID2 = "stoneskin_vest_alb";
        public const string ALBMAULARMOR_ID3 = "vest_of_five_paws_alb";
        public const string ALBMAULARMOR_ID4 = "vest_of_the_pugilist_alb2";
        public const string ARMSARMOR_ID1 = "hauberk_of_the_neophyte_alb";
        public const string ARMSARMOR_ID2 = "footsoldiers_hauberk";
        public const string ARMSARMOR_ID3 = "infantry_hauberk_alb";
        public const string ARMSARMOR_ID4 = "soldiers_hauberk_alb";
        public const string CABAARMOR_ID1 = "robes_of_the_apprentice_alb";
        public const string CABAARMOR_ID2 = "journeymans_robe";
        public const string CABAARMOR_ID3 = "imbuers_robe_alb";
        public const string CABAARMOR_ID4 = "creators_robe_alb";
        public const string CLERARMOR_ID1 = "vest_of_the_apprentice_alb";
        public const string CLERARMOR_ID2 = "novices_vest";
        public const string CLERARMOR_ID3 = "curates_vest";
        public const string CLERARMOR_ID4 = "prelates_hauberk_alb";
        public const string FRIARARMOR_ID1 = "robes_of_the_novice_alb";
        public const string FRIARARMOR_ID2 = "chaplains_robe";
        public const string FRIARARMOR_ID3 = "fanatics_robes";
        public const string FRIARARMOR_ID4 = "zealots_robes";
        public const string HEREARMOR_ID1 = "robes_of_the_novice_alb";
        public const string HEREARMOR_ID2 = "robe_of_the_apprentice_heretic";
        public const string HEREARMOR_ID3 = "robe_of_the_initiate_alb";
        public const string HEREARMOR_ID4 = "proselytes_robe_alb";
        public const string INFIARMOR_ID1 = "vest_of_the_initiate_alb";
        public const string INFIARMOR_ID2 = "vest_of_the_lurker_alb";
        public const string INFIARMOR_ID3 = "blue_hands_vest";
        public const string INFIARMOR_ID4 = "spys_vest";
        public const string MERCARMOR_ID1 = "hauberk_of_the_neophyte_alb";
        public const string MERCARMOR_ID2 = "vest_of_the_pugilist_alb";
        public const string MERCARMOR_ID3 = "hauberk_of_the_escalader";
        public const string MERCARMOR_ID4 = "swashbucklers_hauberk_alb";
        public const string MINSARMOR_ID1 = "vest_of_the_initiate_alb";
        public const string MINSARMOR_ID2 = "vest_of_the_sonneteer";
        public const string MINSARMOR_ID3 = "vest_of_the_versesmith";
        public const string MINSARMOR_ID4 = "vest_of_the_lyricist";
        public const string NECRARMOR_ID1 = "robes_of_the_apprentice_alb";
        public const string NECRARMOR_ID2 = "servants_robe";
        public const string NECRARMOR_ID3 = "robe_of_the_summoner_alb";
        public const string NECRARMOR_ID4 = "adepts_robe_alb";
        public const string PALAARMOR_ID1 = "hauberk_of_the_neophyte_alb";
        public const string PALAARMOR_ID2 = "tyros_hauberk_alb";
        public const string PALAARMOR_ID3 = "protectors_hauberk_alb";
        public const string PALAARMOR_ID4 = "hauberk_of_the_defender_alb";
        public const string REAVARMOR_ID1 = "hauberk_of_the_neophyte_alb";
        public const string REAVARMOR_ID2 = "vest_of_the_strongarm";
        public const string REAVARMOR_ID3 = "hauberk_of_the_protector_alb";
        public const string REAVARMOR_ID4 = "soul_protectors_hauberk_alb";
        public const string SCOUARMOR_ID1 = "vest_of_the_initiate_alb";
        public const string SCOUARMOR_ID2 = "bowmans_vest";
        public const string SCOUARMOR_ID3 = "trackers_vest_alb";
        public const string SCOUARMOR_ID4 = "watchers_vest_alb";
        public const string SORCARMOR_ID1 = "robes_of_the_apprentice_alb";
        public const string SORCARMOR_ID2 = "journeymans_robe2";
        public const string SORCARMOR_ID3 = "befuddlers_robe_alb";
        public const string SORCARMOR_ID4 = "charmers_robe_alb";
        public const string THEUARMOR_ID1 = "robes_of_the_apprentice_alb";
        public const string THEUARMOR_ID2 = "journeymans_robe";
        public const string THEUARMOR_ID3 = "summoners_robe_alb";
        public const string THEUARMOR_ID4 = "sappers_robe_alb";
        public const string WIZARMOR_ID1 = "robes_of_the_apprentice_alb";
        public const string WIZARMOR_ID2 = "adepts_robe_alb2";
        public const string WIZARMOR_ID3 = "elementalists_robe";
        public const string WIZARMOR_ID4 = "spellbinders_robe_alb";

        public const string HIBWEAPON_ID1 = "animist_item";
        public const string HIBWEAPON_ID2 = "bainshee_item";
        public const string HIBWEAPON_ID3 = "bard_item";
        public const string HIBWEAPON_ID4 = "druid_item";
        public const string HIBWEAPON_ID5 = "eldritch_item";
        public const string HIBWEAPON_ID6 = "enchanter_item";
        public const string HIBWEAPON_ID7 = "maulerhib_item_staff";
        public const string HIBWEAPON_ID8 = "maulerhib_item_fist";
        public const string HIBWEAPON_ID9 = "mentalist_item";
        public const string HIBWEAPON_ID10 = "blunt_hib_item";
        public const string HIBWEAPON_ID11 = "blades_hib_item";
        public const string HIBWEAPON_ID12 = "piercing_hib_item";
        public const string HIBWEAPON_ID13 = "largeweap_hib_item";
        public const string HIBWEAPON_ID14 = "celticspear_hib_item";
        public const string HIBWEAPON_ID15 = "ranger_item";
        public const string HIBWEAPON_ID16 = "valewalker_item";
        public const string ANIMARMOR_ID1 = "robes_of_the_apprentice_hib";
        public const string ANIMARMOR_ID2 = "apprentices_robe_hib";
        public const string ANIMARMOR_ID3 = "friend_of_gaias_robe_hib";
        public const string ANIMARMOR_ID4 = "plantfriends_robe_hib";
        public const string BAINARMOR_ID1 = "robes_of_the_initiate_hib";
        public const string BAINARMOR_ID2 = "robe_of_the_wraith_apprentice_hib";
        public const string BAINARMOR_ID3 = "robe_of_the_phantom_adept_hib";
        public const string BAINARMOR_ID4 = "robe_of_the_phantom_reaper_hib";
        public const string BARDARMOR_ID1 = "vest_of_the_chanter_hib";
        public const string BARDARMOR_ID2 = "vocalists_vest";
        public const string BARDARMOR_ID3 = "carolers_vest";
        public const string BARDARMOR_ID4 = "choralists_vest";
        public const string BLADARMOR_ID1 = "vest_of_the_neophyte_hib";
        public const string BLADARMOR_ID2 = "stylists_vest";
        public const string BLADARMOR_ID3 = "sabreurs_vest";
        public const string BLADARMOR_ID4 = "bladeweavers_vest";
        public const string CHAMARMOR_ID1 = "vest_of_the_neophyte_hib";
        public const string CHAMARMOR_ID2 = "chargers_vest";
        public const string CHAMARMOR_ID3 = "vest_of_the_propugner";
        public const string CHAMARMOR_ID4 = "huberk_of_the_valiant_hib";
        public const string DRUIARMOR_ID1 = "vest_of_the_novice_hib";
        public const string DRUIARMOR_ID2 = "students_vest_hib";
        public const string DRUIARMOR_ID3 = "apprentices_vest_hib";
        public const string DRUIARMOR_ID4 = "grove_healer_hauberk_hib";
        public const string ELDRARMOR_ID1 = "robes_of_the_apprentice_hib";
        public const string ELDRARMOR_ID2 = "evokers_robe";
        public const string ELDRARMOR_ID3 = "conjurers_robe_hib";
        public const string ELDRARMOR_ID4 = "magius_robe_hib";
        public const string ENCHARMOR_ID1 = "robes_of_the_apprentice_hib";
        public const string ENCHARMOR_ID2 = "illusionists_robe_hib";
        public const string ENCHARMOR_ID3 = "glamourists_robe_hib";
        public const string ENCHARMOR_ID4 = "entrancers_robe_hib";
        public const string HEROARMOR_ID1 = "vest_of_the_neophyte_hib";
        public const string HEROARMOR_ID2 = "servitors_vest";
        public const string HEROARMOR_ID3 = "confidants_vest_hib";
        public const string HEROARMOR_ID4 = "henchmans_vest_hib";
        public const string HIBMAULARMOR_ID1 = "vest_of_the_novice_hib2";
        public const string HIBMAULARMOR_ID2 = "stoneskin_vest_hib";
        public const string HIBMAULARMOR_ID3 = "vest_of_five_paws_hib";
        public const string HIBMAULARMOR_ID4 = "vest_of_the_pugilist_hib";
        public const string MENTARMOR_ID1 = "robes_of_the_apprentice_hib";
        public const string MENTARMOR_ID2 = "adepts_robe_hib";
        public const string MENTARMOR_ID3 = "robe_of_the_thought_walker_hib";
        public const string MENTARMOR_ID4 = "robe_of_the_visionary_hib";
        public const string NIGHARMOR_ID1 = "vest_of_the_huntsman_hib";
        public const string NIGHARMOR_ID2 = "nightwalkers_vest";
        public const string NIGHARMOR_ID3 = "darkshades_vest";
        public const string NIGHARMOR_ID4 = "darkblades_vest";
        public const string RANGARMOR_ID1 = "vest_of_the_huntsman_hib";
        public const string RANGARMOR_ID2 = "vest_of_the_lurker_hib";
        public const string RANGARMOR_ID3 = "archers_vest";
        public const string RANGARMOR_ID4 = "trackers_vest_hib";
        public const string VALEARMOR_ID1 = "robes_of_the_apprentice_hib";
        public const string VALEARMOR_ID2 = "scythewielders_robe";
        public const string VALEARMOR_ID3 = "forestwalkers_robe";
        public const string VALEARMOR_ID4 = "reapers_robe_hib";
        public const string VAMPARMOR_ID1 = "vest_of_the_huntsman_hib";
        public const string VAMPARMOR_ID2 = "apprentices_vest_hib2";
        public const string VAMPARMOR_ID3 = "adepts_vest_hib";
        public const string VAMPARMOR_ID4 = "protectors_vest_hib";
        public const string WARDARMOR_ID1 = "vest_of_the_neophyte_hib";
        public const string WARDARMOR_ID2 = "woodsmans_vest";
        public const string WARDARMOR_ID3 = "guardians_vest";
        public const string WARDARMOR_ID4 = "vest_of_the_hunter";

        public const string MIDWEAPON_ID1 = "sword_mid_item";
        public const string MIDWEAPON_ID2 = "hammer_mid_item";
        public const string MIDWEAPON_ID3 = "axe_mid_item";
        public const string MIDWEAPON_ID4 = "leftaxe_mid_item";
        public const string MIDWEAPON_ID5 = "spear_mid_item";
        public const string MIDWEAPON_ID6 = "handtohand_mid_item";
        public const string MIDWEAPON_ID7 = "bonedancer_item";
        public const string MIDWEAPON_ID8 = "healer_item";
        public const string MIDWEAPON_ID9 = "hunter_item";
        public const string MIDWEAPON_ID10 = "maulermid_item_staff";
        public const string MIDWEAPON_ID11 = "maulermid_item_fist";
        public const string MIDWEAPON_ID12 = "runemaster_item";
        public const string MIDWEAPON_ID13 = "shadowblade_item";
        public const string MIDWEAPON_ID14 = "shaman_item";
        public const string MIDWEAPON_ID15 = "spiritmaster_item";
        public const string MIDWEAPON_ID16 = "warlock_item";
        public const string BERZARMOR_ID1 = "vest_of_the_huntsman_mid2";
        public const string BERZARMOR_ID2 = "seekers_vest";
        public const string BERZARMOR_ID3 = "fervents_vest_mid";
        public const string BERZARMOR_ID4 = "pillagers_vest2";
        public const string BONEARMOR_ID1 = "vest_of_the_eleve_mid";
        public const string BONEARMOR_ID2 = "bonegatherers_vest";
        public const string BONEARMOR_ID3 = "tribals_vest_mid";
        public const string BONEARMOR_ID4 = "apprentices_vest_mid";
        public const string HEALARMOR_ID1 = "vest_of_the_seer_mid";
        public const string HEALARMOR_ID2 = "practioners_vest2";
        public const string HEALARMOR_ID3 = "journeymans_vest_mid4";
        public const string HEALARMOR_ID4 = "vest_of_the_wise";
        public const string HUNTARMOR_ID1 = "vest_of_the_huntsman_mid2";
        public const string HUNTARMOR_ID2 = "vest_of_the_shadowed_seeker";
        public const string HUNTARMOR_ID3 = "journeymans_vest_mid3";
        public const string HUNTARMOR_ID4 = "vest_of_the_prey_stalker";
        public const string MIDMAULARMOR_ID1 = "vest_of_the_novice_mid";
        public const string MIDMAULARMOR_ID2 = "stoneskin_vest_mid";
        public const string MIDMAULARMOR_ID3 = "vest_of_five_paws_mid";
        public const string MIDMAULARMOR_ID4 = "vest_of_the_pugilist_mid";
        public const string RUNEARMOR_ID1 = "vest_of_the_eleve_mid";
        public const string RUNEARMOR_ID2 = "runic_practitioners_vest";
        public const string RUNEARMOR_ID3 = "runecarvers_vest";
        public const string RUNEARMOR_ID4 = "stonetellers_vest";
        public const string SAVAARMOR_ID1 = "vest_of_the_huntsman_mid2";
        public const string SAVAARMOR_ID2 = "apprentices_vest_mid2";
        public const string SAVAARMOR_ID3 = "servants_vest_mid";
        public const string SAVAARMOR_ID4 = "tribals_vest_mid2";
        public const string SHADOARMOR_ID1 = "vest_of_the_huntsman_mid";
        public const string SHADOARMOR_ID2 = "dark_seekers_vest";
        public const string SHADOARMOR_ID3 = "deceivers_vest_mid";
        public const string SHADOARMOR_ID4 = "shadow_lurkers_vest";
        public const string SHAMARMOR_ID1 = "vest_of_the_seer_mid";
        public const string SHAMARMOR_ID2 = "practitioners_vest3";
        public const string SHAMARMOR_ID3 = "journeymans_vest_mid2";
        public const string SHAMARMOR_ID4 = "medicines_vest_mid";
        public const string SKALARMOR_ID1 = "vest_of_the_huntsman_mid2";
        public const string SKALARMOR_ID2 = "chanters_vest";
        public const string SKALARMOR_ID3 = "song_weavers_vest";
        public const string SKALARMOR_ID4 = "saga_spinners_vest";
        public const string SPIRARMOR_ID1 = "vest_of_the_eleve_mid";
        public const string SPIRARMOR_ID2 = "practitioners_vest";
        public const string SPIRARMOR_ID3 = "journeymans_vest_mid1";
        public const string SPIRARMOR_ID4 = "summoners_vest_mid";
        public const string THANARMOR_ID1 = "hauberk_of_the_neophyte_mid";
        public const string THANARMOR_ID2 = "stormhammers_vest";
        public const string THANARMOR_ID3 = "storm_callers_vest";
        public const string THANARMOR_ID4 = "thors_hammers_vest";
        public const string VALKARMOR_ID1 = "hauberk_of_the_neophyte_mid";
        public const string VALKARMOR_ID2 = "handmaidens_hauberk_mid";
        public const string VALKARMOR_ID3 = "servants_hauberk_mid";
        public const string VALKARMOR_ID4 = "protectors_hauberk_mid";
        public const string WARLARMOR_ID1 = "vest_of_the_eleve_mid";
        public const string WARLARMOR_ID2 = "conjurers_vest";
        public const string WARLARMOR_ID3 = "hels_initiate_vest";
        public const string WARLARMOR_ID4 = "hels_spiritists_vest";
        public const string WARRARMOR_ID1 = "hauberk_of_the_neophyte_mid";
        public const string WARRARMOR_ID2 = "yeomans_vest";
        public const string WARRARMOR_ID3 = "footmans_vest";
        public const string WARRARMOR_ID4 = "veterans_vest_mid";

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Master Trainer Initializing...");

        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            return eQuestIndicator.Lesson;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            TurnTo(player, 50);
            OfferTrainingItems(player);
            if (CanPromotePlayer(player))
            {
                DisplayClassDescriptions(player);
            }
            return true;
        }

        /// <inheritdoc />
        public override IList GetExamineMessages(GamePlayer player)
        {
            return new ArrayList()
            {
                LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.YouTarget",
                                           GetName(0, false, player.Client.Account.Language, this)),
                LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.YouExamine.Master",
                                           GetName(0, false, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                           GetAggroLevelString(player, false)),
                LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.RightClick")
            };
        }

        /// <inheritdoc />
        public override bool CanTrain(GamePlayer player)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool CanPromotePlayer(GamePlayer player)
        {
            switch ((eCharacterClass)player.CharacterClass.ID)
            {
                case eCharacterClass.Acolyte:
                case eCharacterClass.AlbionRogue:
                case eCharacterClass.Disciple:
                case eCharacterClass.Elementalist:
                case eCharacterClass.Fighter:
                case eCharacterClass.Mage:
                case eCharacterClass.Forester:
                case eCharacterClass.Guardian:
                case eCharacterClass.Magician:
                case eCharacterClass.Stalker:
                case eCharacterClass.Naturalist:
                case eCharacterClass.MidgardRogue:
                case eCharacterClass.Mystic:
                case eCharacterClass.Seer:
                case eCharacterClass.Viking:
                    if (player.Level >= 5)
                        return true;
                    break;
            }
            return false;
        }

        private void OfferTrainingItems(GamePlayer player)
        {
            if (player.Level <= 5)
            {
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID1, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Acolyte))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID2, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Fighter))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID3, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Guardian))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID4, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Naturalist))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID5, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.MidgardRogue))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID6, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Seer))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID7, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Viking))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID8, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.AlbionRogue))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID9, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Stalker))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_SHIELD_ID1, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Fighter || player.CharacterClass.ID == (int)eCharacterClass.Viking || player.CharacterClass.ID == (int)eCharacterClass.Seer))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text3", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_SHIELD_ID2, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Naturalist || player.CharacterClass.ID == (int)eCharacterClass.Guardian))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Interact.Text3", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }

                if ((player.Inventory.CountItemTemplate(PRACTICE_STAFF_ID1, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Magician || player.CharacterClass.ID == (int)eCharacterClass.Forester))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MagicianTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                if ((player.Inventory.CountItemTemplate(PRACTICE_STAFF_ID2, eInventorySlot.MinEquipable, eInventorySlot.LastBackpack) == 0) && (player.CharacterClass.ID == (int)eCharacterClass.Mage || player.CharacterClass.ID == (int)eCharacterClass.Mystic || player.CharacterClass.ID == (int)eCharacterClass.Elementalist || player.CharacterClass.ID == (int)eCharacterClass.Disciple))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MagicianTrainer.Interact.Text2", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
            }
        }

        private void DisplayClassDescriptions(GamePlayer player)
        {
            switch (player.CharacterClass.ID)
            {
                case (int)eCharacterClass.Acolyte:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Acolyte1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.AlbionRogue:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.AlbionRogue1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Disciple:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Disciple1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Elementalist:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Elementalist1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Fighter:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Fighter1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Mage:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Mage1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Forester:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Forester1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Guardian:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Guardian1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Magician:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Magician1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Naturalist:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Naturalist1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Stalker:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Stalker1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.MidgardRogue:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.MidgardRogue1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Mystic:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Mystic1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Seer:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Seer1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;

                case (int)eCharacterClass.Viking:
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.Viking1", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    break;
            }
        }

        private Dictionary<GamePlayer, string> playerLastClassOffers = new Dictionary<GamePlayer, string>();
        private Dictionary<GamePlayer, string> playerWeaponOffers = new Dictionary<GamePlayer, string>();

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;

            str = str.ToLower();

            if ((str == "yes" || str == "oui") && playerLastClassOffers.ContainsKey(player))
            {
                string classOffer = playerLastClassOffers[player];
                if (player.CharacterClass.ID == (int)eCharacterClass.Acolyte || player.CharacterClass.ID == (int)eCharacterClass.AlbionRogue || player.CharacterClass.ID == (int)eCharacterClass.Disciple || player.CharacterClass.ID == (int)eCharacterClass.Elementalist || player.CharacterClass.ID == (int)eCharacterClass.Fighter || player.CharacterClass.ID == (int)eCharacterClass.Mage || player.CharacterClass.ID == (int)eCharacterClass.Forester || player.CharacterClass.ID == (int)eCharacterClass.Guardian || player.CharacterClass.ID == (int)eCharacterClass.Magician || player.CharacterClass.ID == (int)eCharacterClass.Stalker || player.CharacterClass.ID == (int)eCharacterClass.Naturalist || player.CharacterClass.ID == (int)eCharacterClass.MidgardRogue || player.CharacterClass.ID == (int)eCharacterClass.Mystic || player.CharacterClass.ID == (int)eCharacterClass.Seer || player.CharacterClass.ID == (int)eCharacterClass.Viking)
                    if (classOffer == "armsman")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "arms_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "paladin")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "pala_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "mercenary")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "merc_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "reaver")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "reav_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "albmauler")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "albmaul_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "heretic")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "here_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "infiltrator")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "infi_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "hero")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "hero_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "champion")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "cham_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "blademaster")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "blad_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "hibmauler")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "hibmaul_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "nightshade")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "nigh_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "warden")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "ward_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "berserker")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "berz_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "savage")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "sava_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "skald")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "skal_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "thane")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "than_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "warrior")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "warr_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "valkyrie")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "valk_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else if (classOffer == "midmauler")
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.Interact.Text4", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        playerWeaponOffers[player] = "midmaul_pending";
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
                    else
                    {
                        HandleClassSelectionResponse(player, true);
                        playerLastClassOffers.Remove(player);
                        return true;
                    }
            }

            if (str == "no" || str == "non")
            {
                if (player.CharacterClass.ID == (int)eCharacterClass.Acolyte || player.CharacterClass.ID == (int)eCharacterClass.AlbionRogue || player.CharacterClass.ID == (int)eCharacterClass.Disciple || player.CharacterClass.ID == (int)eCharacterClass.Elementalist || player.CharacterClass.ID == (int)eCharacterClass.Fighter || player.CharacterClass.ID == (int)eCharacterClass.Mage || player.CharacterClass.ID == (int)eCharacterClass.Forester || player.CharacterClass.ID == (int)eCharacterClass.Guardian || player.CharacterClass.ID == (int)eCharacterClass.Magician || player.CharacterClass.ID == (int)eCharacterClass.Stalker || player.CharacterClass.ID == (int)eCharacterClass.Naturalist || player.CharacterClass.ID == (int)eCharacterClass.MidgardRogue || player.CharacterClass.ID == (int)eCharacterClass.Mystic || player.CharacterClass.ID == (int)eCharacterClass.Seer || player.CharacterClass.ID == (int)eCharacterClass.Viking)
                {
                    playerLastClassOffers.Remove(player);
                    playerWeaponOffers.Remove(player);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.ChooseLater", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    return true;
                }
            }

            string messageKey = null;

            switch ((eCharacterClass)player.CharacterClass.ID)
            {
                case eCharacterClass.Acolyte:
                    switch (str)
                    {
                        case "cleric":
                        case "clerc":
                            if (player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Briton || player.Race == (int)eRace.Highlander)
                            {
                                messageKey = "AcolyteTrainer.Cleric.Explain";
                                playerLastClassOffers[player] = "cleric";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AcolyteTrainer.Cleric.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "friar":
                        case "moine":
                            if (player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Briton || player.Race == (int)eRace.Highlander)
                            {
                                messageKey = "AcolyteTrainer.Friar.Explain";
                                playerLastClassOffers[player] = "friar";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AcolyteTrainer.Friar.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "heretic":
                        case "hérétique":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.AlbionMinotaur || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.Highlander)
                            {
                                messageKey = "AcolyteTrainer.Heretic.Explain";
                                playerLastClassOffers[player] = "heretic";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AcolyteTrainer.Heretic.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.AlbionRogue:
                    switch (str)
                    {
                        case "infiltrator":
                        case "sicaire":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Highlander || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.Inconnu)
                            {
                                messageKey = "AlbionRogueTrainer.Infiltrator.Explain";
                                playerLastClassOffers[player] = "infiltrator";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AlbionRogueTrainer.Infiltrator.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "minstrel":
                        case "ménestrel":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.Highlander || player.Race == (int)eRace.Inconnu)
                            {
                                messageKey = "AlbionRogueTrainer.Minstrel.Explain";
                                playerLastClassOffers[player] = "minstrel";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AlbionRogueTrainer.Minstrel.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "scout":
                        case "éclaireur":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.Highlander || player.Race == (int)eRace.Inconnu)
                            {
                                messageKey = "AlbionRogueTrainer.Scout.Explain";
                                playerLastClassOffers[player] = "scout";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AlbionRogueTrainer.Scout.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Disciple:
                    switch (str)
                    {
                        case "necromancer":
                        case "prêtre d'arawn":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.Avalonian)
                            {
                                messageKey = "DiscipleTrainer.Necromancer.Explain";
                                playerLastClassOffers[player] = "necromancer";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DiscipleTrainer.Necromancer.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "wraithsummoner":
                        case "conjurateur des ombres":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.Avalonian)
                            {
                                messageKey = "DiscipleTrainer.WraithSummoner.Explain";
                                playerLastClassOffers[player] = "wraithsummoner";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DiscipleTrainer.WraithSummoner.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Elementalist:
                    switch (str)
                    {
                        case "theurgist":
                        case "théurgiste":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.HalfOgre)
                            {
                                messageKey = "ElementalistTrainer.Theurgist.Explain";
                                playerLastClassOffers[player] = "theurgist";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ElementalistTrainer.Theurgist.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "wizard":
                        case "thaumaturge":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.HalfOgre || player.Race == (int)eRace.Inconnu)
                            {
                                messageKey = "ElementalistTrainer.Wizard.Explain";
                                playerLastClassOffers[player] = "wizard";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ElementalistTrainer.Wizard.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Fighter:
                    switch (str)
                    {
                        case "armsman":
                        case "maître d'armes":
                            if (player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Briton || player.Race == (int)eRace.HalfOgre || player.Race == (int)eRace.Highlander || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.AlbionMinotaur)
                            {
                                messageKey = "FighterTrainer.Armsman.Explain";
                                playerLastClassOffers[player] = "armsman";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FighterTrainer.Armsman.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "mercenary":
                        case "mercenaire":
                            if (player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Briton || player.Race == (int)eRace.HalfOgre || player.Race == (int)eRace.Highlander || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Saracen || player.Race == (int)eRace.AlbionMinotaur)
                            {
                                messageKey = "FighterTrainer.Mercenary.Explain";
                                playerLastClassOffers[player] = "mercenary";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FighterTrainer.Mercenary.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "paladin":
                            if (player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Briton || player.Race == (int)eRace.Highlander || player.Race == (int)eRace.Saracen)
                            {
                                messageKey = "FighterTrainer.Paladin.Explain";
                                playerLastClassOffers[player] = "paladin";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FighterTrainer.Paladin.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "reaver":
                        case "fléau d'arawn":
                            if (player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Briton || player.Race == (int)eRace.AlbionMinotaur || player.Race == (int)eRace.Saracen)
                            {
                                messageKey = "FighterTrainer.Reaver.Explain";
                                playerLastClassOffers[player] = "reaver";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FighterTrainer.Reaver.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "mauler":
                        case "kan-laresh":
                            if (player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Briton || player.Race == (int)eRace.AlbionMinotaur || player.Race == (int)eRace.HalfOgre)
                            {
                                messageKey = "Baseclass.Mauler.Explain";
                                playerLastClassOffers[player] = "albmauler";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Baseclass.Mauler.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Mage:
                    switch (str)
                    {
                        case "cabalist":
                        case "cabaliste":
                            if (player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.Briton || player.Race == (int)eRace.HalfOgre || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Saracen)
                            {
                                messageKey = "MageTrainer.Cabalist.Explain";
                                playerLastClassOffers[player] = "cabalist";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MageTrainer.Cabalist.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "sorcerer":
                        case "sorcier":
                            if (player.Race == (int)eRace.Briton || player.Race == (int)eRace.Avalonian || player.Race == (int)eRace.HalfOgre || player.Race == (int)eRace.Inconnu || player.Race == (int)eRace.Saracen)
                            {
                                messageKey = "MageTrainer.Sorcerer.Explain";
                                playerLastClassOffers[player] = "sorcerer";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MageTrainer.Sorcerer.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Forester:
                    switch (str)
                    {
                        case "animist":
                        case "animiste":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.Sylvan || player.Race == (int)eRace.Elf)
                            {
                                messageKey = "ForesterTrainer.Animist.Explain";
                                playerLastClassOffers[player] = "animist";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ForesterTrainer.Animist.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "valewalker":
                        case "faucheur":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.Sylvan || player.Race == (int)eRace.HiberniaMinotaur)
                            {
                                messageKey = "ForesterTrainer.Valewalker.Explain";
                                playerLastClassOffers[player] = "valewalker";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ForesterTrainer.Valewalker.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Guardian:
                    switch (str)
                    {
                        case "hero":
                        case "protecteur":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.Lurikeen || player.Race == (int)eRace.Shar || player.Race == (int)eRace.Sylvan || player.Race == (int)eRace.HiberniaMinotaur)
                            {
                                messageKey = "GuardianTrainer.Hero.Explain";
                                playerLastClassOffers[player] = "hero";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardianTrainer.Hero.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "champion":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen || player.Race == (int)eRace.Shar || player.Race == (int)eRace.Sylvan || player.Race == (int)eRace.HiberniaMinotaur)
                            {
                                messageKey = "GuardianTrainer.Champion.Explain";
                                playerLastClassOffers[player] = "champion";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardianTrainer.Champion.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "blademaster":
                        case "finelame":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Elf || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.Shar || player.Race == (int)eRace.HiberniaMinotaur || player.Race == (int)eRace.Lurikeen)
                            {
                                messageKey = "GuardianTrainer.Blademaster.Explain";
                                playerLastClassOffers[player] = "blademaster";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GuardianTrainer.Blademaster.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "mauler":
                        case "kan-laresh":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.HiberniaMinotaur || player.Race == (int)eRace.Lurikeen)
                            {
                                messageKey = "Baseclass.Mauler.Explain";
                                playerLastClassOffers[player] = "hibmauler";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Baseclass.Mauler.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Magician:
                    switch (str)
                    {
                        case "eldritch":
                            if (player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen)
                            {
                                messageKey = "MagicianTrainer.Eldritch.Explain";
                                playerLastClassOffers[player] = "eldritch";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MagicianTrainer.Eldritch.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "enchanter":
                        case "enchanteur":
                            if (player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen)
                            {
                                messageKey = "MagicianTrainer.Enchanter.Explain";
                                playerLastClassOffers[player] = "enchanter";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MagicianTrainer.Enchanter.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "mentalist":
                        case "empathe":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen || player.Race == (int)eRace.Shar)
                            {
                                messageKey = "MagicianTrainer.Mentalist.Explain";
                                playerLastClassOffers[player] = "mentalist";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MagicianTrainer.Mentalist.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "bainshee":
                        case "banshee":
                            if ((player.Race == (int)eRace.Celt || player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen) && player.Gender == eGender.Female)
                            {
                                messageKey = "MagicianTrainer.Bainshee.Explain";
                                playerLastClassOffers[player] = "bainshee";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MagicianTrainer.Bainshee.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Naturalist:
                    switch (str)
                    {
                        case "bard":
                        case "barde":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg)
                            {
                                messageKey = "NaturalistTrainer.Bard.Explain";
                                playerLastClassOffers[player] = "bard";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NaturalistTrainer.Bard.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "druid":
                        case "druide":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.Sylvan)
                            {
                                messageKey = "NaturalistTrainer.Druid.Explain";
                                playerLastClassOffers[player] = "druid";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NaturalistTrainer.Druid.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "warden":
                        case "sentinelle":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Firbolg || player.Race == (int)eRace.Sylvan || player.Race == (int)eRace.HiberniaMinotaur || player.Race == (int)eRace.Shar)
                            {
                                messageKey = "NaturalistTrainer.Warden.Explain";
                                playerLastClassOffers[player] = "warden";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NaturalistTrainer.Warden.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Stalker:
                    switch (str)
                    {
                        case "ranger":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen || player.Race == (int)eRace.Shar || player.Race == (int)eRace.Sylvan)
                            {
                                messageKey = "StalkerTrainer.Ranger.Explain";
                                playerLastClassOffers[player] = "ranger";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "StalkerTrainer.Ranger.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "nightshade":
                        case "ombre":
                            if (player.Race == (int)eRace.Elf || player.Race == (int)eRace.Lurikeen || player.Race == (int)eRace.Celt)
                            {
                                messageKey = "StalkerTrainer.Nightshade.Explain";
                                playerLastClassOffers[player] = "nightshade";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "StalkerTrainer.Nightshade.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "vampiir":
                        case "séide de leanansidhe":
                            if (player.Race == (int)eRace.Celt || player.Race == (int)eRace.Lurikeen || player.Race == (int)eRace.Shar)
                            {
                                messageKey = "StalkerTrainer.Vampiir.Explain";
                                playerLastClassOffers[player] = "vampiir";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "StalkerTrainer.Vampiir.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.MidgardRogue:
                    switch (str)
                    {
                        case "hunter":
                        case "chasseur":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Valkyn)
                            {
                                messageKey = "MidgardRogueTrainer.Hunter.Explain";
                                playerLastClassOffers[player] = "hunter";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MidgardRogueTrainer.Hunter.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "shadowblade":
                        case "assassin":
                            if (player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Valkyn || player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Frostalf)
                            {
                                messageKey = "MidgardRogueTrainer.Shadowblade.Explain";
                                playerLastClassOffers[player] = "shadowblade";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MidgardRogueTrainer.Shadowblade.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Mystic:
                    switch (str)
                    {
                        case "runemaster":
                        case "prêtre d'odin":
                            if (player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Dwarf)
                            {
                                messageKey = "MysticTrainer.Runemaster.Explain";
                                playerLastClassOffers[player] = "runemaster";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MysticTrainer.Runemaster.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                                return true;
                            }
                        case "spiritmaster":
                        case "prêtre de hel":
                            if (player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Norseman)
                            {
                                messageKey = "MysticTrainer.Spiritmaster.Explain";
                                playerLastClassOffers[player] = "spiritmaster";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MysticTrainer.Spiritmaster.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                                return true;
                            }
                        case "bonedancer":
                        case "prêtre de bogdar":
                            if (player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Troll || player.Race == (int)eRace.Valkyn)
                            {
                                messageKey = "MysticTrainer.Bonedancer.Explain";
                                playerLastClassOffers[player] = "bonedancer";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MysticTrainer.Bonedancer.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                                return true;
                            }
                        case "warlock":
                        case "helhaxa":
                            if (player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Troll)
                            {
                                messageKey = "MysticTrainer.Warlock.Explain";
                                playerLastClassOffers[player] = "warlock";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MysticTrainer.Warlock.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Seer:
                    switch (str)
                    {
                        case "shaman":
                        case "chaman":
                            if (player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Troll || player.Race == (int)eRace.Dwarf)
                            {
                                messageKey = "SeerTrainer.Shaman.Explain";
                                playerLastClassOffers[player] = "shaman";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SeerTrainer.Shaman.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "healer":
                        case "guérisseur":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Norseman)
                            {
                                messageKey = "SeerTrainer.Healer.Explain";
                                playerLastClassOffers[player] = "healer";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SeerTrainer.Healer.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
                case eCharacterClass.Viking:
                    switch (str)
                    {
                        case "warrior":
                        case "guerrier":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Troll || player.Race == (int)eRace.Valkyn || player.Race == (int)eRace.MidgardMinotaur)
                            {
                                messageKey = "VikingTrainer.Warrior.Explain";
                                playerLastClassOffers[player] = "warrior";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Warrior.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "berserker":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Troll || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Valkyn || player.Race == (int)eRace.MidgardMinotaur)
                            {
                                messageKey = "VikingTrainer.Berserker.Explain";
                                playerLastClassOffers[player] = "berserker";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Berserker.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "skald":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Troll || player.Race == (int)eRace.MidgardMinotaur)
                            {
                                messageKey = "VikingTrainer.Skald.Explain";
                                playerLastClassOffers[player] = "skald";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Skald.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "thane":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Frostalf || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Troll || player.Race == (int)eRace.MidgardMinotaur || player.Race == (int)eRace.Valkyn)
                            {
                                messageKey = "VikingTrainer.Thane.Explain";
                                playerLastClassOffers[player] = "thane";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Thane.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "savage":
                        case "sauvage":
                            if (player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Troll || player.Race == (int)eRace.Valkyn)
                            {
                                messageKey = "VikingTrainer.Savage.Explain";
                                playerLastClassOffers[player] = "savage";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Savage.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "valkyrie":
                            if ((player.Race == (int)eRace.Dwarf || player.Race == (int)eRace.Valkyn || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Frostalf) && player.Gender == eGender.Female)
                            {
                                messageKey = "VikingTrainer.Valkyrie.Explain";
                                playerLastClassOffers[player] = "valkyrie";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VikingTrainer.Valkyrie.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                        case "mauler":
                        case "kan-laresh":
                            if (player.Race == (int)eRace.Kobold || player.Race == (int)eRace.Norseman || player.Race == (int)eRace.Troll || player.Race == (int)eRace.MidgardMinotaur)
                            {
                                messageKey = "Baseclass.Mauler.Explain";
                                playerLastClassOffers[player] = "midmauler";
                                break;
                            }
                            else
                            {
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Baseclass.Mauler.Refuse", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                                return true;
                            }
                    }
                    break;
            }

            if (HandleItemRequests(str, player))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(messageKey))
            {
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, messageKey, this.Name) + "\r\n\n" + LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.ChooseClass", this.Name);
                player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return true;
            }

            // Weapon choice handling for classes having weapon selection choice
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "arms_pending")
            {
                HandleArmsmanWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "pala_pending")
            {
                HandlePaladinWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "merc_pending")
            {
                HandleMercenaryWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "reav_pending")
            {
                HandleReaverWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "albmaul_pending")
            {
                HandleAlbmaulWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "here_pending")
            {
                HandleHereticWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "infi_pending")
            {
                HandleInfiltratorWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "Hero_pending")
            {
                HandleHeroWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "cham_pending")
            {
                HandleChampionWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "blad_pending")
            {
                HandleBlademasterWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "hibmaul_pending")
            {
                HandleHibmaulWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "nigh_pending")
            {
                HandleNightshadeWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "ward_pending")
            {
                HandleWardenWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "berz_pending")
            {
                HandleBerserkerWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "sava_pending")
            {
                HandleSavageWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "than_pending")
            {
                HandleThaneWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "skal_pending")
            {
                HandleSkaldWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "warr_pending")
            {
                HandleWarriorWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "valk_pending")
            {
                HandleValkyrieWeaponChoice(player, str);
                return true;
            }
            if (playerWeaponOffers.ContainsKey(player) && playerWeaponOffers[player] == "midmaul_pending")
            {
                HandleMidmaulWeaponChoice(player, str);
                return true;
            }

            HandleNewClassOrWeaponSelection(player, str);
            return true;

            #region Respecs
            if (str == "Realmrank")
            {
                player.Out.SendMessage("You want to [delete] ?", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (str == "delete")
            {
                if (player.Level == 50)
                {
                    player.RealmPoints = 0;
                    player.RespecRealm();
                    player.RealmLevel = 0;
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(0, false);
                    player.Out.SendMessage("Now u can choose your RR again!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }


                return false;
            }
            if (str == "Realmranks")
            {
                player.Out.SendMessage("You can choose for: [RR1],[RR2],[RR3],[RR4],[RR5],[RR6],[RR7],[RR8],[RR9],[RR10],[RR11],[RR12] and [RR13]", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage("Which Rank you want to be ?", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (str == "RR1")
            {
                if (player.Level == 50)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(0, false);
                    player.Out.SendMessage("You will not get any Realmpoints for RR1!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }


                return false;
            }
            if (str == "RR2")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(7126, false);
                    player.Out.SendMessage("You got RR2 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR3")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(61755, false);
                    player.Out.SendMessage("You got RR3 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }

            if (str == "RR4")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(213880, false);
                    player.Out.SendMessage("You got RR4 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR5")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(513550, false);
                    player.Out.SendMessage("You got RR5 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR6")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(1010630, false);
                    player.Out.SendMessage("You got RR6 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR7")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(1755255, false);
                    player.Out.SendMessage("You got RR6 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR8")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(2797380, false);
                    player.Out.SendMessage("You got RR8 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR9")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(4187010, false);
                    player.Out.SendMessage("You got RR9 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR10")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(5974130, false);
                    player.Out.SendMessage("You got RR10 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR11")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(8208760, false);
                    player.Out.SendMessage("You got RR11 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR12")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(23308100, false);
                    player.Out.SendMessage("You got RR12 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return false;
                }
                return true;
            }
            if (str == "RR13")
            {
                if (player.Level == 50 && player.RealmPoints == 0)
                {
                    player.SaveIntoDatabase();
                    player.Out.SendUpdatePlayer();
                    player.GainRealmPoints(66181550, false);
                    player.Out.SendMessage("You got RR13 and cannot change it anymore!", eChatType.CT_System, eChatLoc.CL_PopupWindow);

                    return false;
                }
                return true;
            }
            #endregion RealmPoints

            #region Respecs

            if (str == "Respecs")
            {
                player.Out.SendMessage("You currently have:\n" + player.RespecAmountAllSkill + " Full     skill respecs\n" + player.RespecAmountSingleSkill + " Single skill respecs\n" + player.RespecAmountRealmSkill + " Realm skill respecs\n" + player.RespecAmountChampionSkill + " Champion skill respecs", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                player.Out.SendMessage("Which would you like to buy:\n[Full], [Single], [Realm], [MasterLevel] or [Champion]?", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (str == "Full")
            {
                //first check if the player has too many
                if (player.RespecAmountAllSkill >= 5)
                {
                    player.Out.SendMessage("You already have " + player.RespecAmountAllSkill + " Full skill respecs, to use them simply target me and type /respec ALL", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }
                //TODO next, check that the player can afford it

                //send dialog to player to confirm the purchase of the full skill respec
                player.Out.SendCustomDialog("Full Skill Respec price is: FREE Do you really want to buy one?", new CustomDialogResponse(RespecFullDialogResponse));

                return true;
            }
            if (str == "Single")
            {
                //first check if the player has too many
                if (player.RespecAmountSingleSkill >= 5)
                {
                    player.Out.SendMessage("You already have " + player.RespecAmountAllSkill + " Single skill Respecs, to use them simply target me and type /respec <line>", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }
                //TODO next, check that the player can afford it

                //send dialog to player to confirm the purchase of the single skill respec
                player.Out.SendCustomDialog("Single Skill Respec price is: FREE Do you really want to buy one?", new CustomDialogResponse(RespecSingleDialogResponse));

                return true;
            }
            if (str == "Realm")
            {
                //first check if the player has too many
                if (player.RespecAmountRealmSkill >= 5)
                {
                    player.Out.SendMessage("You already have " + player.RespecAmountRealmSkill + " Realm skill respecs, to use them simply target me and type /respec Realm", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }
                //TODO next, check that the player can afford it

                //send dialog to player to confirm the purchase of the full skill respec
                player.Out.SendCustomDialog("Realm Skill Respec price is: FREE Do you really want to buy one?", new CustomDialogResponse(RespecRealmDialogResponse));

                return true;
            }

            if (str == "ChampionLevel")
            {
                if (player.RespecAmountChampionSkill >= 5)
                {
                    player.Out.SendMessage("You already have " + player.RespecAmountChampionSkill + " Champion skill respecs, to use them please visit the Champion Level Master.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    return true;
                }

                //TODO next, check that the player can afford it

                //send dialog to player to confirm the purchase of the Champion skill respec token
                player.Out.SendCustomDialog("CL Skill Respec price is: FREE Do you really want to buy one?", new CustomDialogResponse(RespecChampionDialogResponse));

                return true;
            }



            #endregion Respec

            player.Out.SendTrainerWindow();
            return true;
        }

        private bool HandleItemRequests(string str, GamePlayer player)
        {
            if (str.Contains("practice weapon") || str.Contains("arme d'entraînement"))
            {
                if ((player.CharacterClass.ID == (int)eCharacterClass.Acolyte) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID1, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID1, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.Fighter) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID2, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID2, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.Guardian) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID3, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID3, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.Naturalist) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID4, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID4, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.MidgardRogue) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID5, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID5, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.Seer) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID6, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID6, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.Viking) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID7, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID7, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.AlbionRogue) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID8, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID8, eInventoryActionType.Other);
                    return true;
                }
                if ((player.CharacterClass.ID == (int)eCharacterClass.Stalker) && (player.Inventory.CountItemTemplate(PRACTICE_WEAPON_ID9, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_WEAPON_ID9, eInventoryActionType.Other);
                    return true;
                }
            }

            if (str.Contains("training shield") || str.Contains("bouclier d'entraînement"))
            {
                if (player.CharacterClass.ID is (int)eCharacterClass.Fighter or (int)eCharacterClass.Viking or (int)eCharacterClass.Seer && (player.Inventory.CountItemTemplate(PRACTICE_SHIELD_ID1, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_SHIELD_ID1, eInventoryActionType.Other);
                    return true;
                }
                if (player.CharacterClass.ID is (int)eCharacterClass.Naturalist or (int)eCharacterClass.Guardian && (player.Inventory.CountItemTemplate(PRACTICE_SHIELD_ID2, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_SHIELD_ID2, eInventoryActionType.Other);
                    return true;
                }
            }

            if (str.Contains("practice staff") || str.Contains("bâton d'entraînement"))
            {
                if (player.CharacterClass.ID is (int)eCharacterClass.Magician or (int)eCharacterClass.Forester && (player.Inventory.CountItemTemplate(PRACTICE_STAFF_ID1, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_STAFF_ID1, eInventoryActionType.Other);
                    return true;
                }
                if (player.CharacterClass.ID is (int)eCharacterClass.Mage or (int)eCharacterClass.Mystic or (int)eCharacterClass.Elementalist or (int)eCharacterClass.Disciple && (player.Inventory.CountItemTemplate(PRACTICE_STAFF_ID2, eInventorySlot.Min_Inv, eInventorySlot.Max_Inv) == 0))
                {
                    player.ReceiveItem(this, PRACTICE_STAFF_ID2, eInventoryActionType.Other);
                    return true;
                }
            }
            return false;
        }

        private void HandleClassSelectionResponse(GamePlayer player, bool accept)
        {
            if (accept)
            {
                string classOffer = playerLastClassOffers[player];
                if (CanTrain(player))
                {
                    switch (classOffer)
                    {
                        // ALBION Classes
                        case "cleric":
                            PromotePlayer(player, (int)eCharacterClass.Cleric, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID9, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ClericTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, CLERARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "friar":
                            PromotePlayer(player, (int)eCharacterClass.Friar, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID10, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FriarTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, FRIARARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "minstrel":
                            PromotePlayer(player, (int)eCharacterClass.Minstrel, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon3", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID12, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MinstrelTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, MINSARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "scout":
                            PromotePlayer(player, (int)eCharacterClass.Scout, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID14, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ScoutTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, SCOUARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "necromancer":
                            PromotePlayer(player, (int)eCharacterClass.Necromancer, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID13, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NecromancerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, NECRARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "wraithsummoner":
                            PromotePlayer(player, (int)eCharacterClass.WraithSummonerAlb, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID13, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NecromancerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, NECRARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "theurgist":
                            PromotePlayer(player, (int)eCharacterClass.Theurgist, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID16, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TheurgistTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, THEUARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "wizard":
                            PromotePlayer(player, (int)eCharacterClass.Wizard, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID17, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WizardTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, WIZARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "cabalist":
                            PromotePlayer(player, (int)eCharacterClass.Cabalist, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID8, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CabalistTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, CABAARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "sorcerer":
                            PromotePlayer(player, (int)eCharacterClass.Sorcerer, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, ALBWEAPON_ID15, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SorcererTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, SORCARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        // HIBERNIA Classes
                        case "animist":
                            PromotePlayer(player, (int)eCharacterClass.Animist, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AnimistTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, ANIMARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "valewalker":
                            PromotePlayer(player, (int)eCharacterClass.Valewalker, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID16, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValewalkerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, ANIMARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "eldritch":
                            PromotePlayer(player, (int)eCharacterClass.Eldritch, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID5, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EldritchTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, ELDRARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "enchanter":
                            PromotePlayer(player, (int)eCharacterClass.Enchanter, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID6, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EnchanterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, ENCHARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "mentalist":
                            PromotePlayer(player, (int)eCharacterClass.Mentalist, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID9, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, MENTARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "bainshee":
                            PromotePlayer(player, (int)eCharacterClass.Bainshee, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID2, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BainsheeTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, BAINARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "bard":
                            PromotePlayer(player, (int)eCharacterClass.Bard, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon3 ", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID2, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BardTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, BARDARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "druid":
                            PromotePlayer(player, (int)eCharacterClass.Druid, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID4, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DruidTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, DRUIARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "ranger":
                            PromotePlayer(player, (int)eCharacterClass.Ranger, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID15, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RangerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, RANGARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "vampiir":
                            PromotePlayer(player, (int)eCharacterClass.Vampiir, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, HIBWEAPON_ID12, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VampiirTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, VAMPARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        // MIDGARD Classes
                        case "hunter":
                            PromotePlayer(player, (int)eCharacterClass.Hunter, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID9, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HunterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, HUNTARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "shadowblade":
                            PromotePlayer(player, (int)eCharacterClass.Shadowblade, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID13, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShadowbladeTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, SHADOARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "runemaster":
                            PromotePlayer(player, (int)eCharacterClass.Runemaster, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID12, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RunemasterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, RUNEARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "spiritmaster":
                            PromotePlayer(player, (int)eCharacterClass.Spiritmaster, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID15, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SpiritmasterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, SPIRARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "bonedancer":
                            PromotePlayer(player, (int)eCharacterClass.Bonedancer, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID7, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BonedancerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, BONEARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "warlock":
                            PromotePlayer(player, (int)eCharacterClass.Warlock, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon2 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID16, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarlockTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, WARLARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "shaman":
                            PromotePlayer(player, (int)eCharacterClass.Shaman, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID14, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShamanTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, SHAMARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                        case "healer":
                            PromotePlayer(player, (int)eCharacterClass.Healer, LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.GiveWeapon1 ", this.Name), null);
                            player.ReceiveItem(this, MIDWEAPON_ID8, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HealerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            player.ReceiveItem(this, HEALARMOR_ID1, eInventoryActionType.Other);
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.CharacterClass.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            break;
                    }
                }
            }
            else
            {
                if (CanTrain(player))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.ChooseLater", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
            }
        }

        private void HandleArmsmanWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Fighter)
                switch (weaponChoice)
                {
                    case "tranchante":
                    case "slashing":
                        PromotePlayer(player, (int)eCharacterClass.Armsman, LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMSARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "contondante":
                    case "crushing":
                        PromotePlayer(player, (int)eCharacterClass.Armsman, LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMSARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "perforante":
                    case "thrusting":
                        PromotePlayer(player, (int)eCharacterClass.Armsman, LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMSARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "arme d'hast":
                    case "polearms":
                        PromotePlayer(player, (int)eCharacterClass.Armsman, LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID4, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMSARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "deux mains":
                    case "two handed":
                        PromotePlayer(player, (int)eCharacterClass.Armsman, LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.WhisperReceive.Text5", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID5, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ARMSARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated01", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandlePaladinWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Fighter)
                switch (weaponChoice)
                {
                    case "tranchante":
                    case "slashing":
                        PromotePlayer(player, (int)eCharacterClass.Paladin, LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, PALAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "contondante":
                    case "crushing":
                        PromotePlayer(player, (int)eCharacterClass.Paladin, LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, PALAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "perforante":
                    case "thrusting":
                        PromotePlayer(player, (int)eCharacterClass.Paladin, LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, PALAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "deux mains":
                    case "two handed":
                        PromotePlayer(player, (int)eCharacterClass.Paladin, LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID5, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, PALAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated02", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleMercenaryWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Fighter)
                switch (weaponChoice)
                {
                    case "tranchante":
                    case "slashing":
                        PromotePlayer(player, (int)eCharacterClass.Mercenary, LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, MERCARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "contondante":
                    case "crushing":
                        PromotePlayer(player, (int)eCharacterClass.Mercenary, LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, MERCARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "perforante":
                    case "thrusting":
                        PromotePlayer(player, (int)eCharacterClass.Mercenary, LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, MERCARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated03", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleReaverWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Fighter)
                switch (weaponChoice)
                {
                    case "tranchante":
                    case "slashing":
                        PromotePlayer(player, (int)eCharacterClass.Reaver, LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, REAVARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "contondante":
                    case "crushing":
                        PromotePlayer(player, (int)eCharacterClass.Reaver, LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, REAVARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "perforante":
                    case "thrusting":
                        PromotePlayer(player, (int)eCharacterClass.Reaver, LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, REAVARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "flexible":
                        PromotePlayer(player, (int)eCharacterClass.Reaver, LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, REAVARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated04", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleAlbmaulWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Fighter)
                switch (weaponChoice)
                {
                    case "staff":
                    case "bâton":
                        PromotePlayer(player, (int)eCharacterClass.MaulerAlb, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID6, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ALBMAULARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "fists":
                    case "poings":
                        PromotePlayer(player, (int)eCharacterClass.MaulerAlb, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID7, eInventoryActionType.Other);
                        player.ReceiveItem(this, ALBWEAPON_ID7, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, ALBMAULARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated05", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleHereticWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Acolyte)
                switch (weaponChoice)
                {
                    case "contondante":
                    case "crushing":
                        PromotePlayer(player, (int)eCharacterClass.Heretic, LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID9, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEREARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "flexible":
                        PromotePlayer(player, (int)eCharacterClass.Heretic, LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEREARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated06", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleInfiltratorWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.AlbionRogue)
                switch (weaponChoice)
                {
                    case "tranchante":
                    case "slashing":
                        PromotePlayer(player, (int)eCharacterClass.Infiltrator, LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, INFIARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "perforante":
                    case "thrusting":
                        PromotePlayer(player, (int)eCharacterClass.Infiltrator, LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, ALBWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, INFIARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated07", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleHeroWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Guardian)
                switch (weaponChoice)
                {
                    case "blades":
                    case "épées":
                        PromotePlayer(player, (int)eCharacterClass.Hero, LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEROARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "blunt":
                    case "contondante":
                        PromotePlayer(player, (int)eCharacterClass.Hero, LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID10, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEROARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "piercing":
                    case "perforante":
                        PromotePlayer(player, (int)eCharacterClass.Hero, LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID12, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEROARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "large weapons":
                    case "grandes armes":
                        PromotePlayer(player, (int)eCharacterClass.Hero, LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID13, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEROARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "celtic spear":
                    case "lance celtique":
                        PromotePlayer(player, (int)eCharacterClass.Hero, LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.WhisperReceive.Text5", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID14, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HEROARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated08", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleChampionWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Guardian)
                switch (weaponChoice)
                {
                    case "blades":
                    case "épées":
                        PromotePlayer(player, (int)eCharacterClass.Champion, LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, CHAMARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "blunt":
                    case "contondante":
                        PromotePlayer(player, (int)eCharacterClass.Champion, LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID10, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, CHAMARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "piercing":
                    case "perforante":
                        PromotePlayer(player, (int)eCharacterClass.Champion, LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID12, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, CHAMARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "large weapons":
                    case "grandes armes":
                        PromotePlayer(player, (int)eCharacterClass.Champion, LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID13, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, CHAMARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated09", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleBlademasterWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Guardian)
                switch (weaponChoice)
                {
                    case "blades":
                    case "épées":
                        PromotePlayer(player, (int)eCharacterClass.Blademaster, LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BLADARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "blunt":
                    case "contondante":
                        PromotePlayer(player, (int)eCharacterClass.Blademaster, LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID10, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BLADARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "piercing":
                    case "perforante":
                        PromotePlayer(player, (int)eCharacterClass.Blademaster, LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID12, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BLADARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated10", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleHibmaulWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Guardian)
                switch (weaponChoice)
                {
                    case "staff":
                    case "bâton":
                        PromotePlayer(player, (int)eCharacterClass.MaulerHib, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID7, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HIBMAULARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "fists":
                    case "poings":
                        PromotePlayer(player, (int)eCharacterClass.MaulerHib, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID8, eInventoryActionType.Other);
                        player.ReceiveItem(this, HIBWEAPON_ID8, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, HIBMAULARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated05", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleWardenWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Naturalist)
                switch (weaponChoice)
                {
                    case "blades":
                    case "épées":
                        PromotePlayer(player, (int)eCharacterClass.Warden, LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, WARDARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "blunt":
                    case "contondante":
                        PromotePlayer(player, (int)eCharacterClass.Warden, LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID10, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, WARDARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated11", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleNightshadeWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Stalker)
                switch (weaponChoice)
                {
                    case "blades":
                    case "épées":
                        PromotePlayer(player, (int)eCharacterClass.Nightshade, LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, NIGHARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "piercing":
                    case "perforante":
                        PromotePlayer(player, (int)eCharacterClass.Nightshade, LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, HIBWEAPON_ID12, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, NIGHARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated12", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleBerserkerWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Viking)
                switch (weaponChoice)
                {
                    case "sword":
                    case "épée":
                        PromotePlayer(player, (int)eCharacterClass.Berserker, LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BERZARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "hammer":
                    case "marteau":
                        PromotePlayer(player, (int)eCharacterClass.Berserker, LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BERZARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "axe":
                    case "hache":
                        PromotePlayer(player, (int)eCharacterClass.Berserker, LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BERZARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "left axe":
                    case "hache main gauche":
                        PromotePlayer(player, (int)eCharacterClass.Berserker, LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID4, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, BERZARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated13", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleSavageWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Viking)
                switch (weaponChoice)
                {
                    case "sword":
                    case "épée":
                        PromotePlayer(player, (int)eCharacterClass.Savage, LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SAVAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "hammer":
                    case "marteau":
                        PromotePlayer(player, (int)eCharacterClass.Savage, LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SAVAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "axe":
                    case "hache":
                        PromotePlayer(player, (int)eCharacterClass.Savage, LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SAVAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "hand to hand":
                    case "griffe":
                        PromotePlayer(player, (int)eCharacterClass.Savage, LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.WhisperReceive.Text4", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID6, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SAVAARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated14", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleSkaldWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Viking)
                switch (weaponChoice)
                {
                    case "sword":
                    case "épée":
                        PromotePlayer(player, (int)eCharacterClass.Skald, LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SKALARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "hammer":
                    case "marteau":
                        PromotePlayer(player, (int)eCharacterClass.Skald, LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SKALARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "axe":
                    case "hache":
                        PromotePlayer(player, (int)eCharacterClass.Skald, LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, SKALARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated15", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleThaneWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Viking)
                switch (weaponChoice)
                {
                    case "sword":
                    case "épée":
                        PromotePlayer(player, (int)eCharacterClass.Thane, LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, THANARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "hammer":
                    case "marteau":
                        PromotePlayer(player, (int)eCharacterClass.Thane, LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, THANARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "axe":
                    case "hache":
                        PromotePlayer(player, (int)eCharacterClass.Thane, LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, THANARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated15", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleWarriorWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Viking)
                switch (weaponChoice)
                {
                    case "sword":
                    case "épée":
                        PromotePlayer(player, (int)eCharacterClass.Warrior, LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, WARRARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "hammer":
                    case "marteau":
                        PromotePlayer(player, (int)eCharacterClass.Warrior, LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID2, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, WARRARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "axe":
                    case "hache":
                        PromotePlayer(player, (int)eCharacterClass.Warrior, LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.WhisperReceive.Text3", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID3, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, WARRARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated15", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleValkyrieWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Viking)
                switch (weaponChoice)
                {
                    case "sword":
                    case "épée":
                        PromotePlayer(player, (int)eCharacterClass.Valkyrie, LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID1, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, VALKARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "spear":
                    case "lance":
                        PromotePlayer(player, (int)eCharacterClass.Valkyrie, LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID5, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, VALKARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated16", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleMidmaulWeaponChoice(GamePlayer player, string weaponChoice)
        {
            if (player.CharacterClass.ID == (int)eCharacterClass.Guardian)
                switch (weaponChoice)
                {
                    case "staff":
                    case "bâton":
                        PromotePlayer(player, (int)eCharacterClass.MaulerMid, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.WhisperReceive.Text1", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID10, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, MIDMAULARMOR_ID1, eInventoryActionType.Other);
                        break;
                    case "fists":
                    case "poings":
                        PromotePlayer(player, (int)eCharacterClass.MaulerMid, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.WhisperReceive.Text2", player.GetName(0, false)), null);
                        player.ReceiveItem(this, MIDWEAPON_ID11, eInventoryActionType.Other);
                        player.ReceiveItem(this, MIDWEAPON_ID11, eInventoryActionType.Other);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.ReceiveArmor.Text1", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.ReceiveItem(this, MIDMAULARMOR_ID1, eInventoryActionType.Other);
                        break;
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.WeaponNotRelated05", this.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
        }

        private void HandleNewClassOrWeaponSelection(GamePlayer player, string input)
        {
            if ((player.CharacterClass.ID == (int)eCharacterClass.Acolyte || player.CharacterClass.ID == (int)eCharacterClass.AlbionRogue || player.CharacterClass.ID == (int)eCharacterClass.Disciple || player.CharacterClass.ID == (int)eCharacterClass.Elementalist || player.CharacterClass.ID == (int)eCharacterClass.Fighter || player.CharacterClass.ID == (int)eCharacterClass.Mage || player.CharacterClass.ID == (int)eCharacterClass.Forester || player.CharacterClass.ID == (int)eCharacterClass.Guardian || player.CharacterClass.ID == (int)eCharacterClass.Magician || player.CharacterClass.ID == (int)eCharacterClass.Stalker || player.CharacterClass.ID == (int)eCharacterClass.Naturalist || player.CharacterClass.ID == (int)eCharacterClass.MidgardRogue || player.CharacterClass.ID == (int)eCharacterClass.Mystic || player.CharacterClass.ID == (int)eCharacterClass.Seer || player.CharacterClass.ID == (int)eCharacterClass.Viking) && player.Level == 5)
            {
                switch (input)
                {
                    default:
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MasterTrainer.Interact.ChooseAgain", this.Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        break;
                }
            }
        }

        #region TrainSpecLine
        public void TrainSpecLine(GamePlayer player, string line, int points)
        {
            if (player == null)
                return;

            if (!(player.TargetObject is GameTrainer))
            {
                player.Out.SendMessage("You must have your trainer targetted to be trained in a specialization line.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if ((points <= 0) || (points >= 51))
            {
                player.Out.SendMessage("An Error occurred, there was an invalid amount to train: " + points + " points is not valid!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            int target = points;
            Specialization spec = player.GetSpecialization(line);
            if (spec == null)
            {
                player.Out.SendMessage("An Error occurred, there was an invalid line name: " + line + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            int current = spec.Level;
            if (current >= player.Level)
            {
                player.Out.SendMessage("You can't train in " + line + " again this level.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (points <= current)
            {
                player.Out.SendMessage("You have already trained this amount in " + line + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            target = target - current;
            ushort skillspecialtypoints = 0;
            int speclevel = 0;
            bool changed = false;
            for (int i = 0; i < target; i++)
            {
                if (spec.Level + speclevel >= player.Level)
                {
                    player.Out.SendMessage("You can't train in " + line + " again this level!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }

                if ((player.SkillSpecialtyPoints + player.GetAutoTrainPoints(spec, 3)) - skillspecialtypoints >= (spec.Level + speclevel) + 1)
                {
                    changed = true;
                    skillspecialtypoints += (ushort)((spec.Level + speclevel) + 1);
                    if (spec.Level + speclevel < player.Level / 4 && player.GetAutoTrainPoints(spec, 4) != 0)
                        skillspecialtypoints -= (ushort)((spec.Level + speclevel) + 1);
                    speclevel++;
                }
                else
                {
                    player.Out.SendMessage("That specialization costs " + (spec.Level + 1) + " specialization points!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage("You don't have that many specialization points left for this level.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
            }
            if (changed)
            {
                //            player.SkillSpecialtyPoints -= skillspecialtypoints;
                spec.Level += speclevel;
                player.OnSkillTrained(spec);
                player.Out.SendUpdatePoints();
                player.Out.SendTrainerWindow();
                player.Out.SendMessage("You now have " + points + " points in the " + line + " line!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            return;

        }
        #endregion TrainSpecLine
        #region RespecDialogResponse
        protected void RespecFullDialogResponse(GamePlayer player, byte response)
        {
            if (response != 0x01) return; //declined
            player.RespecAmountAllSkill++;
            player.Out.SendMessage("You just bought a Full skill respec!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage("Target the trainer and type /respec ALL to use it!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

        }
        protected void RespecSingleDialogResponse(GamePlayer player, byte response)
        {
            if (response != 0x01) return; //declined
            player.RespecAmountSingleSkill++;
            player.Out.SendMessage("You just bought a Single skill respec!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage("Target the trainer and type /respec <line> to use it!", eChatType.CT_System, eChatLoc.CL_SystemWindow);


        }
        protected void RespecRealmDialogResponse(GamePlayer player, byte response)
        {
            if (response != 0x01) return; //declined
            player.RespecAmountRealmSkill++;
            player.Out.SendMessage("You just bought a Realm skill respec!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage("Target the trainer and type /respec Realm to use it!", eChatType.CT_System, eChatLoc.CL_SystemWindow);


        }

        protected void RespecChampionDialogResponse(GamePlayer player, byte response)
        {
            if (response != 0x01) return; //declined
            player.RespecAmountChampionSkill++;
            player.Out.SendMessage("You just bought a Champion skill respec!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage("Please visit the Champion Level master to use it!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source == null || item == null) return false;

            GamePlayer player = source as GamePlayer;

            if (player!.Level >= 10 && player.Level < 15)
            {
                if (item.Id_nb == CLERARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Cleric)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ClericTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(CLERARMOR_ID2, player);
                }
                if (item.Id_nb == FRIARARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Friar)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FriarTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(FRIARARMOR_ID2, player);
                }
                if (item.Id_nb == HEREARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Heretic)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HEREARMOR_ID2, player);
                }
                if (item.Id_nb == INFIARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Infiltrator)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(INFIARMOR_ID2, player);
                }
                if (item.Id_nb == MINSARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Minstrel)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MinstrelTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(MINSARMOR_ID2, player);
                }
                if (item.Id_nb == SCOUARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Scout)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ScoutTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SCOUARMOR_ID2, player);
                }
                if (item.Id_nb == NECRARMOR_ID1 && (player.CharacterClass.ID == (int)eCharacterClass.Necromancer || player.CharacterClass.ID == (int)eCharacterClass.WraithSummonerAlb))
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NecromancerTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(NECRARMOR_ID2, player);
                }
                if (item.Id_nb == THEUARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Theurgist)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TheurgistTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(THEUARMOR_ID2, player);
                }
                if (item.Id_nb == WIZARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Wizard)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WizardTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WIZARMOR_ID2, player);
                }
                if (item.Id_nb == ARMSARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Armsman)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ARMSARMOR_ID2, player);
                }
                if (item.Id_nb == MERCARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Mercenary)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(MERCARMOR_ID2, player);
                }
                if (item.Id_nb == PALAARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Paladin)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(PALAARMOR_ID2, player);
                }
                if (item.Id_nb == ALBMAULARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.MaulerAlb)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ALBMAULARMOR_ID2, player);
                }
                if (item.Id_nb == CABAARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Cabalist)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CabalistTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(CABAARMOR_ID2, player);
                }
                if (item.Id_nb == SORCARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Sorcerer)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SorcererTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SORCARMOR_ID2, player);
                }
                if (item.Id_nb == REAVARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Reaver)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(REAVARMOR_ID2, player);
                }
                if (item.Id_nb == ANIMARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Animist)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AnimistTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ANIMARMOR_ID2, player);
                }
                if (item.Id_nb == BAINARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Bainshee)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BainsheeTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BAINARMOR_ID2, player);
                }
                if (item.Id_nb == BARDARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Bard)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BardTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BARDARMOR_ID2, player);
                }
                if (item.Id_nb == BLADARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Blademaster)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BLADARMOR_ID2, player);
                }
                if (item.Id_nb == CHAMARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Champion)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(CHAMARMOR_ID2, player);
                }
                if (item.Id_nb == DRUIARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Druid)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DruidTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(DRUIARMOR_ID2, player);
                }
                if (item.Id_nb == ELDRARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Eldritch)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EldritchTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ELDRARMOR_ID2, player);
                }
                if (item.Id_nb == ENCHARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Enchanter)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EnchanterTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ENCHARMOR_ID2, player);
                }
                if (item.Id_nb == HEROARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Hero)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HEROARMOR_ID2, player);
                }
                if (item.Id_nb == HIBMAULARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.MaulerHib)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HIBMAULARMOR_ID2, player);
                }
                if (item.Id_nb == MENTARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Mentalist)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(MENTARMOR_ID2, player);
                }
                if (item.Id_nb == NIGHARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Nightshade)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(NIGHARMOR_ID2, player);
                }
                if (item.Id_nb == RANGARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Ranger)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RangerTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(RANGARMOR_ID2, player);
                }
                if (item.Id_nb == VALEARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Valewalker)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValewalkerTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(VALEARMOR_ID2, player);
                }
                if (item.Id_nb == VAMPARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Vampiir)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VampiirTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(VAMPARMOR_ID2, player);
                }
                if (item.Id_nb == WARDARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Warden)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WARDARMOR_ID2, player);
                }
                if (item.Id_nb == BERZARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Berserker)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BERZARMOR_ID2, player);
                }
                if (item.Id_nb == BONEARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Bonedancer)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BonedancerTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BONEARMOR_ID2, player);
                }
                if (item.Id_nb == HEALARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Healer)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HealerTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HEALARMOR_ID2, player);
                }
                if (item.Id_nb == HUNTARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Hunter)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HunterTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HUNTARMOR_ID2, player);
                }
                if (item.Id_nb == MIDMAULARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.MaulerMid)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(MIDMAULARMOR_ID2, player);
                }
                if (item.Id_nb == RUNEARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Runemaster)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RunemasterTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(RUNEARMOR_ID2, player);
                }
                if (item.Id_nb == SAVAARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Savage)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SAVAARMOR_ID2, player);
                }
                if (item.Id_nb == SHADOARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Shadowblade)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShadowbladeTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SHADOARMOR_ID2, player);
                }
                if (item.Id_nb == SHAMARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Shaman)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShamanTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SHAMARMOR_ID2, player);
                }
                if (item.Id_nb == SKALARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Skald)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SKALARMOR_ID2, player);
                }
                if (item.Id_nb == SPIRARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Spiritmaster)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SpiritmasterTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SPIRARMOR_ID2, player);
                }
                if (item.Id_nb == THANARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Thane)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(THANARMOR_ID2, player);
                }
                if (item.Id_nb == VALKARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Valkyrie)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(VALKARMOR_ID2, player);
                }
                if (item.Id_nb == WARLARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Warlock)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarlockTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WARLARMOR_ID2, player);
                }
                if (item.Id_nb == WARRARMOR_ID1 && player.CharacterClass.ID == (int)eCharacterClass.Warrior)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.ReceiveArmor.Text2", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WARRARMOR_ID2, player);
                }
            }
            if (player.Level >= 15 && player.Level < 20)
            {
                switch (item.Id_nb)
                {
                    case CLERARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ClericTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(CLERARMOR_ID3, player);
                        break;
                    case FRIARARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FriarTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(FRIARARMOR_ID3, player);
                        break;
                    case HEREARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(HEREARMOR_ID3, player);
                        break;
                    case INFIARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(INFIARMOR_ID3, player);
                        break;
                    case MINSARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MinstrelTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(MINSARMOR_ID3, player);
                        break;
                    case SCOUARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ScoutTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SCOUARMOR_ID3, player);
                        break;
                    case NECRARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NecromancerTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(NECRARMOR_ID3, player);
                        break;
                    case THEUARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TheurgistTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(THEUARMOR_ID3, player);
                        break;
                    case WIZARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WizardTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(WIZARMOR_ID3, player);
                        break;
                    case ARMSARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ARMSARMOR_ID3, player);
                        break;
                    case MERCARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(MERCARMOR_ID3, player);
                        break;
                    case PALAARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(PALAARMOR_ID3, player);
                        break;
                    case ALBMAULARMOR_ID2:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ALBMAULARMOR_ID3, player);
                        break;
                }
                if (item.Id_nb == CABAARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CabalistTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(CABAARMOR_ID3, player);
                }
                if (item.Id_nb == SORCARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SorcererTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SORCARMOR_ID3, player);
                }
                if (item.Id_nb == REAVARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(REAVARMOR_ID3, player);
                }
                if (item.Id_nb == ANIMARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AnimistTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ANIMARMOR_ID3, player);
                }
                if (item.Id_nb == BAINARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BainsheeTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BAINARMOR_ID3, player);
                }
                if (item.Id_nb == BARDARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BardTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BARDARMOR_ID3, player);
                }
                if (item.Id_nb == BLADARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BLADARMOR_ID3, player);
                }
                if (item.Id_nb == CHAMARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(CHAMARMOR_ID3, player);
                }
                if (item.Id_nb == DRUIARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DruidTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(DRUIARMOR_ID3, player);
                }
                if (item.Id_nb == ELDRARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EldritchTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ELDRARMOR_ID3, player);
                }
                if (item.Id_nb == ENCHARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EnchanterTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(ENCHARMOR_ID3, player);
                }
                if (item.Id_nb == HEROARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HEROARMOR_ID3, player);
                }
                if (item.Id_nb == HIBMAULARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HIBMAULARMOR_ID3, player);
                }
                if (item.Id_nb == MENTARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(MENTARMOR_ID3, player);
                }
                if (item.Id_nb == NIGHARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(NIGHARMOR_ID3, player);
                }
                if (item.Id_nb == RANGARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RangerTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(RANGARMOR_ID3, player);
                }
                if (item.Id_nb == VALEARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValewalkerTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(VALEARMOR_ID3, player);
                }
                if (item.Id_nb == VAMPARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VampiirTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(VAMPARMOR_ID3, player);
                }
                if (item.Id_nb == WARDARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WARDARMOR_ID3, player);
                }
                if (item.Id_nb == BERZARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BERZARMOR_ID3, player);
                }
                if (item.Id_nb == BONEARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BonedancerTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(BONEARMOR_ID3, player);
                }
                if (item.Id_nb == HEALARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HealerTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HEALARMOR_ID3, player);
                }
                if (item.Id_nb == HUNTARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HunterTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(HUNTARMOR_ID3, player);
                }
                if (item.Id_nb == MIDMAULARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(MIDMAULARMOR_ID3, player);
                }
                if (item.Id_nb == RUNEARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RunemasterTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(RUNEARMOR_ID3, player);
                }
                if (item.Id_nb == SAVAARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SAVAARMOR_ID3, player);
                }
                if (item.Id_nb == SHADOARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShadowbladeTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SHADOARMOR_ID3, player);
                }
                if (item.Id_nb == SHAMARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShamanTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SHAMARMOR_ID3, player);
                }
                if (item.Id_nb == SKALARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SKALARMOR_ID3, player);
                }
                if (item.Id_nb == SPIRARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SpiritmasterTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(SPIRARMOR_ID3, player);
                }
                if (item.Id_nb == THANARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(THANARMOR_ID3, player);
                }
                if (item.Id_nb == VALKARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(VALKARMOR_ID3, player);
                }
                if (item.Id_nb == WARLARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarlockTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WARLARMOR_ID3, player);
                }
                if (item.Id_nb == WARRARMOR_ID2)
                {
                    player.Inventory.RemoveCountFromStack(item, 1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.ReceiveArmor.Text3", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    addGift(WARRARMOR_ID3, player);
                }
            }
            if (player.Level >= 20 && player.Level < 50)
            {
                switch (item.Id_nb)
                {
                    case CLERARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ClericTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(CLERARMOR_ID4, player);
                        break;
                    case FRIARARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "FriarTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(FRIARARMOR_ID4, player);
                        break;
                    case HEREARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HereticTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(HEREARMOR_ID4, player);
                        break;
                    case INFIARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "InfiltratorTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(INFIARMOR_ID4, player);
                        break;
                    case MINSARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MinstrelTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(MINSARMOR_ID4, player);
                        break;
                    case SCOUARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ScoutTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SCOUARMOR_ID4, player);
                        break;
                    case NECRARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NecromancerTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(NECRARMOR_ID4, player);
                        break;
                    case THEUARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TheurgistTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(THEUARMOR_ID4, player);
                        break;
                    case WIZARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WizardTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(WIZARMOR_ID4, player);
                        break;
                    case ARMSARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ArmsmanTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ARMSARMOR_ID4, player);
                        break;
                    case MERCARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MercenaryTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(MERCARMOR_ID4, player);
                        break;
                    case PALAARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PaladinTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(PALAARMOR_ID4, player);
                        break;
                    case ALBMAULARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerAlbTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ALBMAULARMOR_ID4, player);
                        break;
                    case CABAARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CabalistTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(CABAARMOR_ID4, player);
                        break;
                    case SORCARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SorcererTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SORCARMOR_ID4, player);
                        break;
                    case REAVARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ReaverTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(REAVARMOR_ID4, player);
                        break;
                    case ANIMARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AnimistTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ANIMARMOR_ID4, player);
                        break;
                    case BAINARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BainsheeTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(BAINARMOR_ID4, player);
                        break;
                    case BARDARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BardTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(BARDARMOR_ID4, player);
                        break;
                    case BLADARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BlademasterTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(BLADARMOR_ID4, player);
                        break;
                    case CHAMARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ChampionTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(CHAMARMOR_ID4, player);
                        break;
                    case DRUIARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DruidTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(DRUIARMOR_ID4, player);
                        break;
                    case ELDRARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EldritchTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ELDRARMOR_ID4, player);
                        break;
                    case ENCHARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "EnchanterTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(ENCHARMOR_ID4, player);
                        break;
                    case HEROARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HeroTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(HEROARMOR_ID4, player);
                        break;
                    case HIBMAULARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(HIBMAULARMOR_ID4, player);
                        break;
                    case MENTARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MentalistTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(MENTARMOR_ID4, player);
                        break;
                    case NIGHARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "NightshadeTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(NIGHARMOR_ID4, player);
                        break;
                    case RANGARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RangerTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(RANGARMOR_ID4, player);
                        break;
                    case VALEARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValewalkerTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(VALEARMOR_ID4, player);
                        break;
                    case VAMPARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "VampiirTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(VAMPARMOR_ID4, player);
                        break;
                    case WARDARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WardenTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(WARDARMOR_ID4, player);
                        break;
                    case BERZARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BerserkerTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(BERZARMOR_ID4, player);
                        break;
                    case BONEARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "BonedancerTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(BONEARMOR_ID4, player);
                        break;
                    case HEALARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HealerTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(HEALARMOR_ID4, player);
                        break;
                    case HUNTARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "HunterTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(HUNTARMOR_ID4, player);
                        break;
                    case MIDMAULARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerMidTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(MIDMAULARMOR_ID4, player);
                        break;
                    case RUNEARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RunemasterTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(RUNEARMOR_ID4, player);
                        break;
                    case SAVAARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SavageTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SAVAARMOR_ID4, player);
                        break;
                    case SHADOARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShadowbladeTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SHADOARMOR_ID4, player);
                        break;
                    case SHAMARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ShamanTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SHAMARMOR_ID4, player);
                        break;
                    case SKALARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SkaldTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SKALARMOR_ID4, player);
                        break;
                    case SPIRARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SpiritmasterTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(SPIRARMOR_ID4, player);
                        break;
                    case THANARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ThaneTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(THANARMOR_ID4, player);
                        break;
                    case VALKARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "ValkyrieTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(VALKARMOR_ID4, player);
                        break;
                    case WARLARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarlockTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(WARLARMOR_ID4, player);
                        break;
                    case WARRARMOR_ID3:
                        player.Inventory.RemoveCountFromStack(item, 1);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "WarriorTrainer.ReceiveArmor.Text4", this.Name, player.Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        addGift(WARRARMOR_ID4, player);
                        break;
                }
            }
            return base.ReceiveItem(source, item);
        }
        #endregion RespecDialogResponse
    }
}