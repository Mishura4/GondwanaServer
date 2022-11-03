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
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [Cmd(
       "&tppoint",
       ePrivLevel.GM,
        "Commands.GM.TPPoint.Description",
        "Commands.GM.TPPoint.Usage.Create",
        "Commands.GM.TPPoint.Usage.Load",
        "Commands.GM.TPPoint.Usage.Save",
        "Commands.GM.TPPoint.Usage.Hide",
        "Commands.GM.TPPoint.Usage.Add",
        "Commands.GM.TPPoint.Usage.Remove",
        "Commands.GM.TPPoint.Usage.Type")]
    public class TPPointCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        protected string TEMP_TPPOINT_FIRST = "TEMP_TPPOINT_FIRST";
        protected string TEMP_TPPOINT_LAST = "TEMP_TPPOINT_LAST";
        protected string TEMP_TPPOINT_OBJS = "TEMP_TPPOINT_OBJS";

        private void CreateTempTPPointObject(GameClient client, TPPoint pp, string name)
        {
            // Create a new object
            GameStaticItem obj = new GameStaticItem();

            // Fill the object variables
            obj.X = pp.X;
            obj.Y = pp.Y;
            obj.Z = pp.Z + 1; // raise a bit off of ground level
            obj.CurrentRegion = client.Player.CurrentRegion;
            obj.Heading = client.Player.Heading;
            obj.Name = name;
            obj.Model = 488;
            obj.Emblem = 0;
            obj.AddToWorld();

            ArrayList objs = (ArrayList)client.Player.TempProperties.getProperty<object>(TEMP_TPPOINT_OBJS, null);
            if (objs == null)
            {
                objs = new ArrayList();
            }

            objs.Add(obj);
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_OBJS, objs);
        }

        private void RemoveAllTempTPPointObjects(GameClient client)
        {
            ArrayList objs = (ArrayList)client.Player.TempProperties.getProperty<object>(TEMP_TPPOINT_OBJS, null);
            if (objs == null)
            {
                return;
            }

            // remove the markers
            foreach (GameStaticItem obj in objs)
            {
                obj.Delete();
            }

            // clear the tppoint point array
            objs.Clear();

            // remove all tppoint properties
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_OBJS, null);
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_FIRST, null);
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_LAST, null);
        }

        private void TPPointHide(GameClient client)
        {
            ArrayList objs = (ArrayList)client.Player.TempProperties.getProperty<object>(TEMP_TPPOINT_OBJS, null);
            if (objs == null)
            {
                return;
            }

            // remove the markers
            foreach (GameStaticItem obj in objs)
            {
                obj.Delete();
            }
        }

        private void TPPointCreate(GameClient client)
        {
            // Remove old temp objects
            RemoveAllTempTPPointObjects(client);

            TPPoint startpoint = new TPPoint(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, eTPPointType.Random, new DBTPPoint(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z));
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_FIRST, startpoint);
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_LAST, startpoint);
            client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Create.Result"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            CreateTempTPPointObject(client, startpoint, "TMP PP 1");
        }

        private void TPPointAdd(GameClient client, string[] args)
        {
            TPPoint tppoint = (TPPoint)client.Player.TempProperties.getProperty<object>(TEMP_TPPOINT_LAST, null);
            if (tppoint == null)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Cant"));
                return;
            }

            TPPoint newpp = new TPPoint(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, tppoint.Type, new DBTPPoint(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z));
            tppoint.Next = newpp;
            newpp.Prev = tppoint;
            client.Player.TempProperties.setProperty(TEMP_TPPOINT_LAST, newpp);

            int len = 0;
            while (tppoint.Prev != null)
            {
                len++;
                tppoint = tppoint.Prev;
            }

            len += 2;
            CreateTempTPPointObject(client, newpp, "TMP PP " + len);
            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Add.Result", len));
        }

        private void TPPointType(GameClient client, string[] args)
        {
            TPPoint tppoint = (TPPoint)client.Player.TempProperties.getProperty<object>(TEMP_TPPOINT_LAST, null);
            if (args.Length < 2)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Usage.Type"));
                DisplayMessage(client, "Current tppoint type is '{0}'", tppoint.Type.ToString());
                DisplayMessage(client, "Possible tppointtype values are:");
                DisplayMessage(client, string.Join(", ", Enum.GetNames(typeof(eTPPointType))));
                return;
            }

            if (tppoint == null)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Cant"));
                return;
            }

            eTPPointType tppointType = eTPPointType.Random;
            try
            {
                tppointType = (eTPPointType)Enum.Parse(typeof(eTPPointType), args[2], true);
            }
            catch
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Usage.Type"));
                DisplayMessage(client, "Current tppoint type is '{0}'", tppoint.Type.ToString());
                DisplayMessage(client, "TPPointType must be one of the following:");
                DisplayMessage(client, string.Join(", ", Enum.GetNames(typeof(eTPPointType))));
                return;
            }

            tppoint.Type = tppointType;
            TPPoint temp = tppoint.Prev;
            while ((temp != null) && (temp != tppoint))
            {
                temp.Type = tppointType;
                temp = temp.Prev;
            }

            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Type.Result", tppoint.Type.ToString()));
        }

        private void TPPointLoad(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Usage.Load"));
                return;
            }

            ushort tppointname = ushort.Parse(args[2]);
            TPPoint tppoint = TeleportMgr.LoadTP(tppointname);
            if (tppoint != null)
            {
                RemoveAllTempTPPointObjects(client);
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Load.Result", tppointname));
                client.Player.TempProperties.setProperty(TEMP_TPPOINT_FIRST, tppoint);
                int len = 1;
                while (tppoint.Next != null)
                {
                    CreateTempTPPointObject(client, tppoint, "TMP PP " + len);
                    tppoint = tppoint.Next;
                    len++;
                }

                client.Player.TempProperties.setProperty(TEMP_TPPOINT_LAST, tppoint);
                return;
            }

            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Load.NotFound", tppointname));
        }

        private void TPPointSave(GameClient client, string[] args)
        {
            TPPoint tppoint = (TPPoint)client.Player.TempProperties.getProperty<object>(TEMP_TPPOINT_LAST, null);
            if (args.Length < 3)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Usage.Save"));
                return;
            }

            if (tppoint == null)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Cant"));
                return;
            }

            ushort tppointname = ushort.Parse(args[2]);
            TeleportMgr.SaveTP(tppointname, tppoint);
            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.TPPoint.Save.Result", tppointname));
        }

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "create":
                    {
                        TPPointCreate(client);
                        break;
                    }

                case "add":
                    {
                        TPPointAdd(client, args);
                        break;
                    }

                case "type":
                    {
                        TPPointType(client, args);
                        break;
                    }

                case "save":
                    {
                        TPPointSave(client, args);
                        break;
                    }

                case "load":
                    {
                        TPPointLoad(client, args);
                        break;
                    }

                case "hide":
                    {
                        TPPointHide(client);
                        break;
                    }

                case "delete":
                    {
                        RemoveAllTempTPPointObjects(client);
                        break;
                    }

                default:
                    {
                        DisplaySyntax(client);
                        break;
                    }
            }
        }
    }
}