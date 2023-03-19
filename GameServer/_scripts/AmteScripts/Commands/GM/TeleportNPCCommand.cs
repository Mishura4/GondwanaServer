using System;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Scripts
{
    [CmdAttribute(
         "&teleportnpc",
         ePrivLevel.GM,
         "Commands.GM.TeleportNPC.Description",
         "Commands.GM.TeleportNPC.Usage.Create",
         "Commands.GM.TeleportNPC.Usage.Create.Douanier",
         "Commands.GM.TeleportNPC.Usage.Text",
         "Commands.GM.TeleportNPC.Usage.Refuse",
         "Commands.GM.TeleportNPC.Usage.Radius",
         "Commands.GM.TeleportNPC.Usage.Level",
         "Commands.GM.TeleportNPC.Usage.AddJump",
         "Commands.GM.TeleportNPC.Usage.Jump",
         "Commands.GM.TeleportNPC.Usage.RemoveJump",
         "Commands.GM.TeleportNPC.Usage.Conditions.Visible",
         "Commands.GM.TeleportNPC.Usage.Conditions.Item",
         "Commands.GM.TeleportNPC.Usage.Conditions.Niveaux",
         "Commands.GM.TeleportNPC.Usage.Conditions.Bind",
         "Commands.GM.TeleportNPC.Usage.AdditionalDescription")]
    public class TeleportNPCCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player == null) return;
            GamePlayer player = client.Player;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            TeleportNPC npc = player.TargetObject as TeleportNPC;
            string text = "";
            switch (args[1].ToLower())
            {
                #region create - text - refuse
                case "create":
                    bool isRenaissance = false;
                    if (args.Length > 2 && args[2] == "douanier")
                    {
                        if (args.Length < 4)
                        {
                            DisplaySyntax(client);
                            break;
                        }

                        int price = 0;

                        if (!int.TryParse(args[3], out price))
                        {
                            DisplaySyntax(client);
                            break;
                        }

                        if (args.Length == 5)
                        {
                            if (!bool.TryParse(args[4], out isRenaissance))
                            {
                                DisplaySyntax(client);
                                player.Out.SendMessage("Le parametre IsRenaissance(true ou false) n'est pas correct" + text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                return;
                            }
                        }

                        npc = new Douanier()
                        {
                            Position = player.Position,
                            Heading = player.Heading,
                            CurrentRegion = player.CurrentRegion,
                            Name = "Maitre Douanier",
                            GuildName = "Douanier",
                            Realm = 0,
                            Model = 40,
                            Price = Money.GetMoney(0, 0, price, 0, 0),
                            IsRenaissance = isRenaissance

                        };
                    }
                    else
                    {
                        if (args.Length == 3)
                        {
                            if (!bool.TryParse(args[2], out isRenaissance))
                            {
                                DisplaySyntax(client);
                                player.Out.SendMessage("Le parametre IsRenaissance(true ou false) n'est pas correct" + text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                                return;
                            }
                        }

                        npc = new TeleportNPC
                        {
                            Position = player.Position,
                            Heading = player.Heading,
                            CurrentRegion = player.CurrentRegion,
                            Name = "Nouveau téléporteur",
                            Realm = 0,
                            Model = 40,
                            Text = "Texte à définir.{5}",
                            IsRenaissance = isRenaissance
                        };
                    }

                    if (!npc.IsPeaceful)
                        npc.Flags ^= GameNPC.eFlags.PEACE;
                    npc.LoadedFromScript = false;
                    npc.AddToWorld();
                    npc.SaveIntoDatabase();
                    break;

                case "text":
                    if (npc == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    text = string.Join(" ", args, 2, args.Length - 2);
                    text = text.Replace('|', '\n');
                    text = text.Replace(';', '\n');
                    npc.Text = text;
                    npc.SaveIntoDatabase();
                    player.Out.SendMessage("Texte défini:\n" + text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;

                case "refuse":
                    if (npc == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    text = string.Join(" ", args, 2, args.Length - 2);
                    text = text.Replace('|', '\n');
                    text = text.Replace(';', '\n');
                    if (text == "NO TEXT")
                        npc.Text_Refuse = "";
                    else
                        npc.Text_Refuse = text;
                    npc.SaveIntoDatabase();
                    player.Out.SendMessage("Texte défini:\n" + text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
                #endregion

                #region radius - level
                case "radius":
                    if (npc == null || args.Length <= 2)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    try
                    {
                        ushort min = (ushort)int.Parse(args[2]);
                        if (min > 500) min = 500;
                        npc.Range = min;
                        npc.Realm = 0;
                        if (!npc.IsPeaceful)
                            npc.Flags ^= GameNPC.eFlags.PEACE;
                        if (!npc.IsDontShowName)
                            npc.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
                        npc.Model = 1;
                        npc.SaveIntoDatabase();
                        player.Out.SendMessage("Le rayon est maintenant de " + min + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    catch { DisplaySyntax(client); }
                    break;

                case "level":
                    if (npc == null || args.Length <= 2)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    try
                    {
                        byte min = (byte)int.Parse(args[2]);
                        if (min > 49) min = 49;
                        npc.MinLevel = min;
                        npc.SaveIntoDatabase();
                        player.Out.SendMessage("Le niveau minimum requis est maintenant de " + min + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    catch { DisplaySyntax(client); }
                    break;
                #endregion

                #region addjump - removejump - jump
                case "addjump":
                    if (npc == null || args.Length <= 7)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    int X;
                    int Y;
                    int Z;
                    ushort Heading;
                    ushort RegionID;
                    try
                    {
                        X = int.Parse(args[2]);
                        Y = int.Parse(args[3]);
                        Z = int.Parse(args[4]);
                        Heading = (ushort)int.Parse(args[5]);
                        RegionID = (ushort)int.Parse(args[6]);
                        text = string.Join(" ", args, 7, args.Length - 7);
                    }
                    catch { DisplaySyntax(client); return; }
                    if (text.ToLower() == "area")
                        text = "Area";

                    npc.AddJumpPos(text, X, Y, Z, Heading, RegionID);
                    npc.SaveIntoDatabase();
                    player.Out.SendMessage("Le jump \"" + text + "\" a été ajouté.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "removejump":
                    if (npc == null || args.Length <= 2)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (npc.RemoveJumpPos(text))
                    {
                        player.Out.SendMessage("Le jump \"" + text + "\" a été supprimé.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        npc.SaveIntoDatabase();
                    }
                    else
                        player.Out.SendMessage("Le jump \"" + text + "\" n'existe pas.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;

                case "jump":
                    if (npc == null)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    foreach (TeleportNPC.JumpPos pos in npc.GetJumpList())
                    {
                        text += pos.Name + ": X:" + pos.X + " Y:" + pos.Y + " Z:" + pos.Z + " Heading:" + pos.Heading + " Region:" + pos.RegionID + "\n";
                        text += " -> " + pos.Conditions + "\n";
                    }
                    if (text == "")
                        text = "Aucun jump";
                    player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    break;
                #endregion

                #region conditions
                case "condition":
                case "conditions":
                    _OnConditionCommand(client, args, npc);
                    break;
                #endregion

                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        private void _OnConditionCommand(GameClient client, string[] args, TeleportNPC npc)
        {
            if (args.Length < 5 || npc == null)
            {
                DisplaySyntax(client);
                return;
            }
            if (!npc.JumpPositions.ContainsKey(args[2]))
            {
                DisplayMessage(client, "Le pnj sélectionné ne contient pas le jump \"" + args[2] + "\" !");
                return;
            }
            TeleportNPC.JumpPos jump = npc.JumpPositions[args[2]];
            int min;
            switch (args[3].ToLower())
            {
                #region visible
                case "visible":
                    if (args[4].Equals("on", StringComparison.CurrentCultureIgnoreCase) || args[4].Equals("off", StringComparison.CurrentCultureIgnoreCase))
                    {
                        jump.Conditions.Visible = args[4].Equals("on", StringComparison.CurrentCultureIgnoreCase);
                        DisplayMessage(client,
                                       "Le jump \"" + jump.Name + "\" est maintenant "
                                       + (jump.Conditions.Visible ? "" : "in") + "visible dans la liste des jumps.");
                    }
                    else
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    break;
                #endregion

                #region item
                case "item":
                    jump.Conditions.Item = args[4];
                    DisplayMessage(client,
                                   "Le jump \"" + jump.Name + "\" nécessite maintenant l'item avec le template: \""
                                   + args[4] + "\".");
                    break;
                #endregion

                #region niveaux
                case "niveau":
                case "niveaux":
                    if (int.TryParse(args[4], out min))
                    {
                        int max = jump.Conditions.LevelMax;
                        if (args.Length > 5 && !int.TryParse(args[5], out max))
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        jump.Conditions.LevelMin = min;
                        jump.Conditions.LevelMax = max;
                        DisplayMessage(client,
                                       "Le jump \"" + jump.Name + "\" demande un niveau compris entre " + min + " et "
                                       + max + ".");
                    }
                    else
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    break;
                #endregion

                case "bind":
                    if (args[4].Equals("on", StringComparison.CurrentCultureIgnoreCase) || args[4].Equals("off", StringComparison.CurrentCultureIgnoreCase))
                    {
                        jump.Conditions.Bind = args[4].Equals("on", StringComparison.CurrentCultureIgnoreCase);
                        if (jump.Conditions.Bind)
                            DisplayMessage(client, "Le jump \"" + jump.Name + "\" bind le joueur après l'avoir téléporté.");
                        else
                            DisplayMessage(client, "Le jump \"" + jump.Name + "\" ne bind pas le joueur après l'avoir téléporté.");
                    }
                    else
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    break;
            }
            npc.SaveIntoDatabase();
        }
    }
}
