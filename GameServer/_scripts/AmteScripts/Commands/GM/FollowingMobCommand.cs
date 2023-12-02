using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GameEvents;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&followmob",
        ePrivLevel.GM,
        "Commands.GM.FollowingMob.Description",
        "Commands.GM.FollowingMob.Create",
        "Commands.GM.FollowingMob.Follow",
        "Commands.GM.FollowingMob.Reset",
        "Commands.GM.FollowingMob.Copy",
        "Commands.GM.FollowingMob.ResponseList",
        "Commands.GM.FollowingMob.ResponseAdd",
        "Commands.GM.FollowingMob.ResponseRemove"
        )]
    public class FollowMobCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }
            IFollowingMob followingMob = client.Player.TargetObject as IFollowingMob;
            GameNPC mob = followingMob as GameNPC;
            GamePlayer player = client.Player;
            switch (args[1])
            {
                case "create":
                    if (args.Length > 2)
                    {
                        if (args[2] == "friend")
                            followingMob = new FollowingFriendMob();
                    }
                    if (followingMob == null)
                    {
                        followingMob = new FollowingMob();
                    }
                    mob = followingMob as GameNPC;
                    mob.Position = client.Player.Position;
                    mob.Heading = client.Player.Heading;
                    mob.CurrentRegion = client.Player.CurrentRegion;
                    mob.Name = "New Following Mob";
                    mob.Model = 407;
                    mob.LoadedFromScript = false;
                    mob.AddToWorld();
                    mob.SaveIntoDatabase();
                    client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "follow":
                {
                    if (mob == null)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    followingMob.Reset();
                    if (followingMob is FollowingMob fMob)
                    {
                        GameNPC found = null;
                        foreach (GameNPC npc in mob.GetNPCsInRadius(2000))
                            if (args.Length < 3 || npc.Name == args[2])
                            {
                                found = npc;
                                followingMob.Follow(npc);
                                break;
                            }

                        if (found == null)
                        {
                            client.Out.SendMessage(
                                "Aucun mob " + (args.Length > 2 ? "\"" + args[2] + "\" " : "") + "trouvé.",
                                eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            break;
                        }

                        mob.SaveIntoDatabase();
                        client.Out.SendMessage("Le mob suit maintenant '" + found.Name + "'.", eChatType.CT_System,
                            eChatLoc.CL_SystemWindow);
                    }
                    else if (followingMob is FollowingFriendMob friendMob)
                    {
                        friendMob.Follow(client.Player);
                    }

                    break;
                }

                case "reset":
                    if (mob == null)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.FollowingMob.InvalidTarget"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        DisplaySyntax(client);
                        return;
                    }
                    followingMob.Reset();
                    break;

                case "response":
                {
                    if (args.Length < 4)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    if (!(mob is FollowingFriendMob friendMob))
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.FollowingMob.InvalidTarget"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        DisplaySyntax(client);
                        return;
                    }

                    switch (args[2])
                    {
                        case "follow":
                        {
                            switch (args[3])
                            {
                                case "list":
                                {
                                    List<String> lines = new List<String>();
                                    if (friendMob.ResponsesFollow is { Count: > 0 })
                                    {
                                        foreach (var follow in friendMob.ResponsesFollow)
                                        {
                                            lines.Add("[" + follow.Key + "] => " + follow.Value);
                                        }
                                    }
                                    player.Out.SendCustomTextWindow("Réponses follow:", lines);
                                    break;
                                }

                                case "add":
                                {
                                    if (args.Length < 5)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }
                                    string reponse = args[4];
                                    string texte = null;
                                    if (args.Length >= 6)
                                    {
                                        texte = string.Join(" ", args, 5, args.Length - 5);
                                        texte = texte.Replace('|', '\n');
                                        texte = texte.Replace(';', '\n');
                                    }
                                    friendMob.ResponsesFollow ??= new Dictionary<string, string>();
                                    if (friendMob.ResponsesFollow.ContainsKey(reponse))
                                    {
                                        friendMob.ResponsesFollow[reponse] = texte;
                                        player.Out.SendMessage("Réponse follow \"" + reponse + "\" modifiée avec le texte:\n" + texte, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                    }
                                    else
                                    {
                                        friendMob.ResponsesFollow.Add(reponse, texte);
                                        player.Out.SendMessage("Réponse follow \"" + reponse + "\" ajoutée avec le texte:\n" + texte, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                    }
                                    friendMob.SaveIntoDatabase();
                                    break;
                                }

                                case "remove":
                                {
                                    if (args.Length < 5)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }
                                    string reponse = string.Join(" ", args, 4, args.Length - 4);
                                    if (friendMob.ResponsesFollow != null && friendMob.ResponsesFollow.Remove(reponse))
                                    {
                                        friendMob.SaveIntoDatabase();
                                        player.Out.SendMessage("Réponse follow \"" + reponse + "\" supprimée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    else
                                    {
                                        player.Out.SendMessage("Aucune réponse follow \"" + reponse + "\" n'a été trouvée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    break;
                                }

                                default:
                                    DisplaySyntax(client);
                                    break;
                            }
                            break;
                        }

                        case "unfollow":
                        {
                            switch (args[3])
                            {
                                case "list":
                                {
                                    List<String> lines = new List<String>();
                                    if (friendMob.ResponsesUnfollow is { Count: > 0 })
                                    {
                                        foreach (var unfollow in friendMob.ResponsesUnfollow)
                                        {
                                            lines.Add("[" + unfollow.Key + "] => " + unfollow.Value);
                                        }
                                    }
                                    player.Out.SendCustomTextWindow("Réponses unfollow:", lines);
                                    break;
                                }

                                case "add":
                                {
                                    if (args.Length < 5)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }
                                    string reponse = args[4];
                                    string texte = null;
                                    if (args.Length >= 6)
                                    {
                                        texte = string.Join(" ", args, 5, args.Length - 5);
                                        texte = texte.Replace('|', '\n');
                                        texte = texte.Replace(';', '\n');
                                    }
                                    friendMob.ResponsesUnfollow ??= new Dictionary<string, string>();
                                    if (friendMob.ResponsesUnfollow.ContainsKey(reponse))
                                    {
                                        friendMob.ResponsesUnfollow[reponse] = texte;
                                        player.Out.SendMessage("Réponse unfollow \"" + reponse + "\" modifiée avec le texte:\n" + texte, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                    }
                                    else
                                    {
                                        friendMob.ResponsesUnfollow.Add(reponse, texte);
                                        player.Out.SendMessage("Réponse unfollow \"" + reponse + "\" ajoutée avec le texte:\n" + texte, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                    }
                                    friendMob.SaveIntoDatabase();
                                    break;
                                }

                                case "remove":
                                {
                                    if (args.Length < 5)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }
                                    string reponse = string.Join(" ", args, 4, args.Length - 4);
                                    if (friendMob.ResponsesUnfollow != null && friendMob.ResponsesUnfollow.Remove(reponse))
                                    {
                                        friendMob.SaveIntoDatabase();
                                        player.Out.SendMessage("Réponse unfollow \"" + reponse + "\" supprimée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    else
                                    {
                                        player.Out.SendMessage("Aucune réponse unfollow \"" + reponse + "\" n'a été trouvée", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    }
                                    break;
                                }

                                default:
                                    DisplaySyntax(client);
                                    break;
                            }
                            break;
                        }

                        default:
                            DisplaySyntax(client);
                            break;
                    }
                    break;
                }

                case "copy":
                    if (mob == null)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(client, "Commands.GM.FollowingMob.InvalidTarget"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                        DisplaySyntax(client);
                        return;
                    }

                    IFollowingMob oldFollowingMob = followingMob;
                    GameNPC oldmob = mob;

                    if (oldFollowingMob is FollowingMob)
                        mob = new FollowingMob();
                    else if (oldFollowingMob is FollowingFriendMob)
                        mob = new FollowingFriendMob();
                    else
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    followingMob = (IFollowingMob)mob;
                    mob.Position = client.Player.Position;
                    mob.Heading = client.Player.Heading;
                    mob.CurrentRegion = client.Player.CurrentRegion;

                    mob.Level = oldmob.Level;
                    mob.Realm = oldmob.Realm;
                    mob.Name = oldmob.Name;
                    mob.Model = oldmob.Model;
                    mob.Flags = oldmob.Flags;
                    mob.MeleeDamageType = oldmob.MeleeDamageType;
                    mob.RespawnInterval = oldmob.RespawnInterval;
                    mob.MaxSpeedBase = oldmob.MaxSpeedBase;
                    mob.GuildName = oldmob.GuildName;
                    mob.Size = oldmob.Size;
                    mob.Inventory = oldmob.Inventory;
                    followingMob.Follow(mob.CurrentFollowTarget);

                    mob.CustomCopy(oldmob);

                    mob.AddToWorld();
                    mob.LoadedFromScript = false;
                    mob.SaveIntoDatabase();

                    if (oldFollowingMob is FollowingFriendMob oldFollowingFriend)
                    {
                        if (oldFollowingFriend.TextNPCIdle != null)
                            ((FollowingFriendMob)mob).TextNPCIdle = new TextNPCPolicy(mob, oldFollowingFriend.TextNPCIdle);
                        if (oldFollowingFriend.TextNPCFollowing != null)
                            ((FollowingFriendMob)mob).TextNPCFollowing = new TextNPCPolicy(mob, oldFollowingFriend.TextNPCFollowing);

                        mob.SaveIntoDatabase();
                    }
                    client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                default:
                    DisplaySyntax(client);
                    break;
            }
        }
    }
}