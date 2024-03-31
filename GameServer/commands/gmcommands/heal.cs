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
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&heal",
        ePrivLevel.GM,
        "Commands.GM.Heal.Description",
        "Commands.GM.Heal.Usage",
        "/heal me - heals self")]
    public class HealCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            try
            {
                GameLiving target = client.Player.TargetObject as GameLiving ?? client.Player;

                int argIndex = 1;
                if (args.Length > argIndex)
                {
                    if (args[argIndex] == "me")
                    {
                        target = client.Player;
                        ++argIndex;
                    }
                }

                if (target is not { ObjectState: GameObject.eObjectState.Active })
                {
                    client.Player.SendTranslatedMessage("Commands.GM.Heal.BadTarget");
                    return;
                }

                if (!target.IsAlive)
                {
                    client.Player.SendTranslatedMessage("Commands.GM.Heal.Dead");
                    return;
                }

                int value = 100;
                bool percent = true;
                if (args.Length > argIndex)
                {
                    var arg = args[argIndex];
                    percent = arg.EndsWith('%');

                    if (int.TryParse(percent ? arg.Substring(0, arg.Length - 1) : arg, out value))
                    {
                        if (value < 0)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        argIndex++;
                    }
                }

                if (args.Length > argIndex)
                {
                    switch (args[argIndex])
                    {
                        case "health":
                            if (percent)
                            {
                                target.Health += (int)(target.MaxHealth * (value / 100.0) + 0.5f);
                                client.Player.SendTranslatedMessage("Commands.GM.Heal.Health.Percent", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
                            }
                            else
                            {
                                target.Health += value;
                                client.Player.SendTranslatedMessage("Commands.GM.Heal.Health", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
                            }
                            break;

                        case "endu":
                            if (percent)
                            {
                                target.Endurance += (int)(target.MaxEndurance * (value / 100.0) + 0.5f);
                                client.Player.SendTranslatedMessage("Commands.GM.Heal.Endu.Percent", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
                            }
                            else
                            {
                                target.Endurance += value;
                                client.Player.SendTranslatedMessage("Commands.GM.Heal.Endu", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
                            }
                            break;

                        case "mana":
                            if (percent)
                            {
                                target.Mana += (int)(target.MaxMana * (value / 100.0) + 0.5f);
                                client.Player.SendTranslatedMessage("Commands.GM.Heal.Mana.Percent", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
                            }
                            else
                            {
                                target.Mana += value;
                                client.Player.SendTranslatedMessage("Commands.GM.Heal.Mana", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
                            }
                            break;

                        default:
                            DisplaySyntax(client);
                            break;
                    }
                    return;
                }

                target.Health = percent ? (int)(target.MaxHealth * (value / 100.0) + 0.5f) : target.Health + value;
                target.Endurance = percent ? (int)(target.MaxEndurance * (value / 100.0) + 0.5f) : target.Endurance + value;
                target.Mana = percent ? (int)(target.MaxMana * (value / 100.0) + 0.5f) : target.Mana + value;
                client.Player.SendTranslatedMessage(percent ? "Commands.GM.Heal.Percent" : "Commands.GM.Heal", eChatType.CT_System, eChatLoc.CL_SystemWindow, target.Name, value);
}
            catch (Exception)
            {
                DisplaySyntax(client);
            }
        }
    }
}