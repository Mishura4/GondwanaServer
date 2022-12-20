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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DOL.GS;
using DOL.GS.ServerProperties;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&market",
		ePrivLevel.Player,
	    "Commands.Players.Market.Description",
	    "Commands.Players.Market.Usage")]
	public class MarketCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "market"))
				return;

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			GameNPC targetMob = null;
			if (client.Player.TargetObject != null && client.Player.TargetObject is ChiefMerchant)
				targetMob = (GameNPC)client.Player.TargetObject;

			switch (args.GetValue(1).ToString().ToLower())
			{
				#region Open
				case "open":
					{
						if (client.Player.HasAbility(DOL.GS.Abilities.Trading))
						{
							//check if player is in safearea
							bool isInsafeArea = false;
							if  (client.Player.CurrentAreas.Count > 0)
							{
								foreach (AbstractArea area in client.Player.CurrentAreas)
								{
									if (area.IsSafeArea)
									{
										isInsafeArea = true;
										break;
									}
								}
							}

							if(!isInsafeArea)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Market.No.SafeArea"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
								break;
							}

							if(client.Player.TemporaryConsignmentMerchant == null || client.Player.TemporaryConsignmentMerchant.ObjectState == GameObject.eObjectState.Deleted)
							{
								client.Player.Out.SendDialogBox(eDialogCode.OpenMarket, (ushort)client.Player.ObjectID, 0, 0, 0, eDialogType.YesNo, false,
								 LanguageMgr.GetTranslation(client, "Commands.Players.Market.Confirm.Open"));
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Market.Already.Created"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
							}
						}
						else
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Market.No.Ability"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
						}
						
						break;
					}
				#endregion Open
				#region Close
				case "close":
					{
						if(client.Player.TemporaryConsignmentMerchant != null)
							client.Player.Out.SendDialogBox(eDialogCode.CloseMarket, 0, 0, 0, 0, eDialogType.YesNo, true,
								LanguageMgr.GetTranslation(client, "Commands.Players.Market.Confirm.Close"));
						break;
					}
				#endregion Close
				#region Name
				case "name":
					{
						if (args.Length < 3)
							DisplaySyntax(client);
						else if (client.Player.TemporaryConsignmentMerchant != null)
							client.Player.TemporaryConsignmentMerchant.Name = args[2];
						break;
					}
				#endregion Name
				#region Default
				default:
					{
						DisplaySyntax(client);
						return;
					}
				#endregion Default
			}
		}
	}
}
