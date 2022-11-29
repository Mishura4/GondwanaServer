using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.Language;
using DOL.MobGroups;
using System.Linq;
using DOL.GS.Styles;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&areaeffect",
		ePrivLevel.GM,
        "Commands.GM.AreaEffect.Description",
        "Commands.GM.AreaEffect.Usage.Create",
        "Commands.GM.AreaEffect.Usage.Spell",
        "Commands.GM.AreaEffect.Usage.HealHarm",
        "Commands.GM.AreaEffect.Usage.Mana",
        "Commands.GM.AreaEffect.Usage.Endurance",
        "Commands.GM.AreaEffect.Usage.Radius",
        "Commands.GM.AreaEffect.Usage.Effect",
        "Commands.GM.AreaEffect.Usage.Interval",
        "Commands.GM.AreaEffect.Usage.GroupMob",
        "Commands.GM.AreaEffect.Usage.Disable",
        "Commands.GM.AreaEffect.Usage.Enable",
        "Commands.GM.AreaEffect.Usage.Family",
        "Commands.GM.AreaEffect.Usage.CallAreaEffect",
        "Commands.GM.AreaEffect.Usage.OneUse",
        "Commands.GM.AreaEffect.Usage.MissChance",
        "Commands.GM.AreaEffect.Usage.Message",
        "Commands.GM.AreaEffect.Usage.Info")]
	public class AreaEffectCommand : AbstractCommandHandler, ICommandHandler
	{
        /// <summary>
        /// Method to add a groopmob to a AreaEffect
        /// </summary>
        /// <param name="client"></param>
        /// <param name="targetDoor"></param>
        /// <param name="args"></param>
        private void GroupMob(GameClient client, AreaEffect targetArea, string[] args)
        {
            if(targetArea == null)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            string groupId = null;
            // default false
            bool turn = false;
            if (args.Length > 3)
            {
                groupId = args[2];
                if(args[3] == "ON")
                {
                    turn = true;
                }
            }
            else
            {
                DisplaySyntax(client);
                return;
            }

            if (!string.IsNullOrEmpty(groupId) && !MobGroupManager.Instance.Groups.ContainsKey(groupId))
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.GroupMob.NotFound", groupId), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            targetArea.Group_Mob_Id = groupId;
            targetArea.Group_Mob_Turn = turn;
            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.GroupMob", targetArea.Name, groupId, turn? "ON":"OFF"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            targetArea.SaveIntoDatabase();
        }

        private void Disable(GameClient client, AreaEffect targetArea, string[] args)
        {
            if (targetArea == null)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            targetArea.Disable = true;
            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.Disable", targetArea.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            targetArea.SaveIntoDatabase();
        }

        private void Enable(GameClient client, AreaEffect targetArea, string[] args)
        {
            if (targetArea == null)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            targetArea.Disable = false;
            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.Enable", targetArea.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            targetArea.SaveIntoDatabase();
        }

        private void OneUse(GameClient client, AreaEffect targetArea, string[] args)
        {
            if (targetArea == null)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            targetArea.OneUse = !targetArea.OneUse;
            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.OneUse", targetArea.Name, targetArea.OneUse), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            targetArea.SaveIntoDatabase();
        }

        private void AddFamily(GameClient client, AreaEffect targetArea, string[] args)
        {
            if (targetArea == null)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                return;
            }
            ushort familyID = 0;
            ushort orderinfamily = 1;
            if (args.Length > 3)
            {
                familyID = ushort.Parse(args[2]);
                orderinfamily = ushort.Parse(args[3]);
            }
            else
            {
                DisplaySyntax(client);
                return;
            }

            targetArea.AreaEffectFamily = familyID;
            targetArea.OrderInFamily = orderinfamily;
            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.Family", targetArea.Name, targetArea.AreaEffectFamily, targetArea.OrderInFamily), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            targetArea.SaveIntoDatabase();
        }

        /// <summary>
        /// Enable without save
        /// </summary>
        /// <param name="client"></param>
        /// <param name="targetArea"></param>
        /// <param name="args"></param>
        private void CallAreaEffect(GameClient client, string[] args)
        {
            ushort familyID = 0;
            if (args.Length > 2)
            {
                ushort.TryParse(args[2], out familyID);
            }
            else
            {
                DisplaySyntax(client);
                return;
            }
            if(familyID == 0 && client != null)
                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.CallAreaEffect.NotFound", familyID), eChatType.CT_System, eChatLoc.CL_ChatWindow);
            else
            {
                DBAreaEffect areaEffect = GameServer.Database.SelectObjects<DBAreaEffect>(DB.Column("AreaEffectFamily").IsEqualTo(familyID)).OrderBy((area) => area.OrderInFamily).FirstOrDefault();
                if(areaEffect != null)
                {
                    Mob mob = GameServer.Database.SelectObjects<Mob>(DB.Column("Mob_ID").IsEqualTo(areaEffect.MobID)).FirstOrDefault();
                    if(mob != null)
                    {
                        AreaEffect areaMob = WorldMgr.GetNPCsByName(mob.Name, (eRealm)mob.Realm).FirstOrDefault((npc) => npc.InternalID == mob.ObjectId) as AreaEffect;
                        if(areaMob != null)
                        {
                            areaMob.CallAreaEffect();
                            if(client != null)
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.CallAreaEffect", familyID), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }   
                    }
                }
                else if (client != null)
                {
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.CallAreaEffect.NotFound", familyID), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

            }
        }

        public void OnCommand(GameClient client, string[] args)
        {
            AreaEffect AE = null;
            if (client != null)
                AE = client.Player.TargetObject as AreaEffect;

			if (args.Length == 2 && args[1].ToLower() == "info" && AE != null)
			{
				var infos = new List<string>
				    {
				        "- Information de l'AreaEffect " + AE.Name,
				        "Vie: " + AE.HealHarm + " points de vie (+/- 10%).",
                        "Mana: " + AE.AddMana + " points de mana.",
                        "Endurance: " + AE.AddEndurance + " points d'endurance.",
				        "Rayon: " + AE.Radius,
				        "Effect: " + AE.SpellEffect,
                        "Spell: " + AE.SpellID,
                        "Interval entre chaque effet " + AE.IntervalMin + " à " + AE.IntervalMax + " secondes",
				        "Chance de miss: " + AE.MissChance + "%",
				        "Message: " + AE.Message
				    };
                infos.Add(" + Spell: " + AE.SpellID);
                infos.Add(" + Family: " + AE.AreaEffectFamily);
                infos.Add(" + Order in family: " + AE.OrderInFamily);
                infos.Add(" + Groupmob: " + AE.Group_Mob_Id);
                infos.Add(" + Groupmob ON/OFF: " + AE.Group_Mob_Turn);
                infos.Add(" + Enable: " + !AE.Disable);
                infos.Add(" + OneUse: " + AE.OneUse);
                client.Out.SendCustomTextWindow("Info AreaEffect", infos);
				return;
			}

            if(args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            GamePlayer player = null;
            if(client != null)
                player = client.Player;

            switch (args[1].ToLower())
			{
				case "create":
					if (args.Length < 4)
					{
						DisplaySyntax(client);
						return;
					}
					int Radius;
					int EffectID;
					if (!int.TryParse(args[2], out Radius) || !int.TryParse(args[3], out EffectID))
					{
						DisplaySyntax(client);
						return;
					}
					AE = new AreaEffect
					    {
					        Position = player.Position,
					        CurrentRegion = player.CurrentRegion,
					        Heading = player.Heading,
					        Model = 1,
					        Name = "Area Effect",
					        Radius = Radius,
					        SpellEffect = EffectID,
                            Flags = GameNPC.eFlags.DONTSHOWNAME | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.PEACE | GameNPC.eFlags.FLYING,
					        LoadedFromScript = false
					    };
			        AE.AddToWorld();
					client.Out.SendMessage("Création d'un AreaEffect, OID:" + AE.ObjectID, eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;
 
                case "spell":
                    if (AE == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        return;
                    }
                    if (!int.TryParse(args[2], out AE.SpellID))
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.Result.Spell", AE.Name, AE.SpellID),
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "heal":
                case "harm":
			        {
                        if (AE == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            return;
                        }
                        int value;
			            if (!int.TryParse(args[2], out value))
			            {
			                DisplaySyntax(client);
			                return;
			            }
			            if (args[1].ToLower() == "harm" && value > 0)
			                value = -value;
			            AE.HealHarm = value;
			            client.Out.SendMessage(
			                AE.Name + (AE.HealHarm > 0 ? " heal" : " harm") + " maintenant de " + Math.Abs(AE.HealHarm) + " points de vie.",
			                eChatType.CT_System, eChatLoc.CL_SystemWindow);
			            break;
			        }

                case "mana":
			        {
                        if (AE == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            return;
                        }
                        int value;
			            if (!int.TryParse(args[2], out value))
			            {
			                DisplaySyntax(client);
			                return;
			            }
			            AE.AddMana = value;
			            client.Out.SendMessage(
                            AE.Name + " ajoute maintenant " + AE.AddMana + " points de mana.",
			                eChatType.CT_System, eChatLoc.CL_SystemWindow);
			            break;
			        }

                case "groupmob":
                    GroupMob(client, AE, args);
                    break;

                case "disable":
                    Disable(client, AE, args);
                    break;

                case "enable":
                    Enable(client, AE, args);
                    break;

                case "family":
                    AddFamily(client, AE, args);
                    break;

                case "callareaeffect":
                    CallAreaEffect(client, args);
                    break;

                case "oneuse":
                    OneUse(client, AE, args);
                    break;

                case "endu":
                case "endurance":
                    {
                        if (AE == null)
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            return;
                        }
                        int value;
                        if (!int.TryParse(args[2], out value))
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        AE.AddEndurance = value;
                        client.Out.SendMessage(
                            AE.Name + " ajoute maintenant " + AE.AddEndurance + " points de mana.",
                            eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        break;
                    }

			    case "radius":
                    if (AE == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        return;
                    }
                    int radius;
					if (!int.TryParse(args[2], out radius))
					{
						DisplaySyntax(client);
						return;
					}
					AE.Radius = radius;
					client.Out.SendMessage(AE.Name + " a maintenant un rayon d'effet de " + AE.Radius, eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

				case "effect":
                    if (AE == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        return;
                    }
                    int effect;
					if (!int.TryParse(args[2], out effect))
					{
						DisplaySyntax(client);
						return;
					}
					AE.SpellEffect = effect;
					client.Out.SendMessage(AE.Name + " a maintenant l'effet du spell " + AE.SpellEffect, eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

				case "interval":
                    if (AE == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        return;
                    }
                    int min;
					if (!int.TryParse(args[2], out min))
					{
						DisplaySyntax(client);
						return;
					}
					int max = 0;
					if (args.Length >= 4 && !int.TryParse(args[3], out max))
					{
						DisplaySyntax(client);
						return;
					}
					max = Math.Max(min, max);
					if (min > max)
					{
						int i = max;
						max = min;
						min = i;
					}
					AE.IntervalMin = min;
					AE.IntervalMax = max;
					client.Out.SendMessage(AE.Name + " a maintenant un interval compris entre " + min + " et " + max,
					                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				case "miss":
				case "chance":
				case "misschance":
                    if (AE == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        return;
                    }
                    int chance;
					if (!int.TryParse(args[2], out chance))
					{
						DisplaySyntax(client);
						return;
					}
					if (chance > 100)
						chance = 100;
					else if (chance < 0)
						chance = 0;
					AE.MissChance = chance;
					client.Out.SendMessage(AE.Name + " a maintenant " + chance + " chance de raté.", eChatType.CT_System,
					                       eChatLoc.CL_SystemWindow);
					break;

				case "message":
                    if (AE == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.AreaEffect.NeedTarget"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        return;
                    }
                    AE.Message = string.Join(" ", args, 2, args.Length - 2);
					client.Out.SendMessage(
						AE.Name + " a maintenant le message \"" + AE.Message + "\" lorsqu'on est touché par son effect.",
						eChatType.CT_System, eChatLoc.CL_SystemWindow);
					break;

				default:
					DisplaySyntax(client);
					break;
			}
			if(AE != null)
			    AE.SaveIntoDatabase();
		}
	}
}