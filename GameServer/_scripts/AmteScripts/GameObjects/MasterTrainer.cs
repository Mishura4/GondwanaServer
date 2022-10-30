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
using log4net;


namespace DOL.GS.Trainer
{
    [NPCGuildScript("Master Trainer")]
    public class MasterTrainer : GameTrainer
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
            player.Out.SendTrainerWindow();
            return true;
        }



        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;

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






        #endregion RespecDialogResponse

    }
}