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
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;
using DOL.GS.Utils;
using System.Collections;
using DOL.Language;

namespace DOL.GS.Commands
{
    [Cmd(
     "&minorelic",
     ePrivLevel.GM,
     "Commands.GM.MinoRelic.Description",
     "Commands.GM.MinoRelic.Usage.Create",
     "Commands.GM.MinoRelic.Usage.MoveHere",
     "Commands.GM.MinoRelic.Usage.Name",
     "Commands.GM.MinoRelic.Usage.Spell",
     "Commands.GM.MinoRelic.Usage.Model",
     "Commands.GM.MinoRelic.Usage.Effect",
     "Commands.GM.MinoRelic.Usage.Info",
     "Commands.GM.MinoRelic.Usage.DeSpawn",
     "Commands.GM.MinoRelic.Usage.Remove",
     "Commands.GM.MinoRelic.Usage.XP",
     "Commands.GM.MinoRelic.Usage.ShowAll",
     "Commands.GM.MinoRelic.Usage.Spawn")]
    public class MinoRelicCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                #region Create
                case "create":
                    {
                        if (args.Length != 7 || (!args[4].ToLower().Equals("group") && !args[4].ToLower().Equals("self") && !args[4].ToLower().Equals("realm")))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        DBMinotaurRelic relic = new DBMinotaurRelic();

                        relic.Name = args[2];

                        relic.SpawnHeading = client.Player.Position.Orientation.InHeading;
                        relic.SpawnX = client.Player.Position.X;
                        relic.SpawnY = client.Player.Position.Y;
                        relic.SpawnZ = client.Player.Position.Z;
                        relic.SpawnRegion = client.Player.Position.RegionID;

                        relic.relicTarget = args[4].ToLower();

                        try
                        {
                            relic.relicSpell = Convert.ToInt32(args[5]);
                            relic.Model = Convert.ToUInt16(args[3]);
                            relic.Effect = Convert.ToInt32(args[6]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.RelicID = MinotaurRelicManager.GetRelicCount() + 1;
                        GameServer.Database.AddObject(relic);

                        MinotaurRelic rrelic = new MinotaurRelic(relic);
                        rrelic.AddToWorld();

                        MinotaurRelicManager.AddRelic(rrelic);

                        break;
                    }
                #endregion Create
                #region MoveHere
                case "movehere":
                    {
                        if (!(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        relic.Position = client.Player.Position;

                        relic.SaveIntoDatabase();

                        break;
                    }
                #endregion MoveHere
                #region Model
                case "model":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.Model = Convert.ToUInt16(args[2]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.SaveIntoDatabase();

                        break;
                    }
                #endregion Model
                #region Name
                case "name":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        relic.Name = args[2];

                        relic.SaveIntoDatabase();

                        break;
                    }
                #endregion Name
                #region Spell
                case "spell":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.RelicSpellID = Convert.ToInt32(args[2]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.SaveIntoDatabase();

                        break;
                    }
                #endregion Spell
                #region Effect
                case "effect":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.Effect = Convert.ToInt32(args[2]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.SaveIntoDatabase();

                        break;
                    }
                #endregion Effect
                #region Info
                case "info":
                    {
                        if (!(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        var info = new List<string>();
                        info.Add("===========================");
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.RelicInfo"));
                        info.Add("===========================");
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.Name", relic.Name));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.ID", relic.RelicID));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.CurrentXP", relic.XP));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.Level", relic.Level));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.Effect", relic.Effect));
                        info.Add("===========================");
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.PositionInfo"));
                        info.Add("===========================");
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnX", relic.SpawnPosition.X));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnY", relic.SpawnPosition.Y));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnZ", relic.SpawnPosition.Z));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnHeading" + relic.SpawnPosition.Orientation.InHeading));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnRegion", relic.SpawnPosition.RegionID));
                        info.Add("===========================");
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.SpellInfo"));
                        info.Add("===========================");
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.SpellID", relic.RelicSpell));
                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.SpellTarget", relic.RelicTarget));

                        Spell spell = SkillBase.GetSpellByID(relic.RelicSpellID);
                        if (spell != null)
                        {
                            info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.SpellName", spell.Name));
                            info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.SpellType", spell.SpellType));
                            info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Info.SpellDuration", (spell.Duration / 1000)));
                        }

                        client.Out.SendCustomTextWindow("[ " + relic.Name + " ]", info);
                        break;
                    }
                #endregion Info
                #region DeSpawn
                case "despawn":
                    {
                        if (!(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        relic.XP = 0;
                        relic.RemoveFromWorld();
                        relic.RelicDispose();
                        break;
                    }
                #endregion DeSpawn
                #region Remove
                case "remove":
                    {
                        //Lifeflight: Added the option to remove a minorelic by supplying an ID.
                        if (args.Length == 3)
                        {
                            int minorelicID = 0;
                            try
                            {
                                minorelicID = Convert.ToInt32(args[2]);
                            }
                            catch (Exception)
                            {

                            }

                            if (minorelicID == 0)
                            {
                                DisplaySyntax(client);
                                return;
                            }
                            else
                            {

                                foreach (MinotaurRelic relic in MinotaurRelicManager.m_minotaurrelics.Values)
                                {
                                    if (relic != null)
                                    {
                                        if (relic.RelicID == minorelicID)
                                        {
                                            //there is a match!
                                            //remove it from the world
                                            relic.RemoveFromWorld();
                                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the world", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                                            //remove it from the hashtable
                                            MinotaurRelicManager.RemoveRelic(relic);
                                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the Minorelic Hash Table", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                                            DataObject obj = GameServer.Database.FindObjectByKey<DBMinotaurRelic>(relic.RelicID);
                                            if (obj != null)
                                            {
                                                GameServer.Database.DeleteObject(obj);
                                                client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the database!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                                            }

                                            break;
                                        }
                                    }
                                }


                            }

                        }
                        else
                        {
                            if (!(client.Player.TargetObject is MinotaurRelic))
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                            relic.RemoveFromWorld();
                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the world", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                            //remove it from the hashtable
                            MinotaurRelicManager.RemoveRelic(relic);
                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the Minorelic Hash Table", eChatType.CT_System, eChatLoc.CL_SystemWindow);


                            DataObject obj = GameServer.Database.FindObjectByKey<DBMinotaurRelic>(relic.RelicID);
                            if (obj != null)
                            {
                                GameServer.Database.DeleteObject(obj);
                                client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the database!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                            }
                        }
                        break;
                    }
                #endregion Remove
                #region XP
                case "xp":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.XP += Convert.ToInt32(args[2]);
                            if (relic.Owner != null)
                                relic.Owner.Out.SendMinotaurRelicBarUpdate(relic.Owner, (int)relic.XP);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        break;
                    }
                #endregion XP
                #region ShowAll
                case "showall":
                    {
                        var info = new List<string>();

                        if (args.Length > 2)
                        {
                            ushort region = 0;
                            try
                            {
                                region = Convert.ToUInt16(args[2]);
                            }
                            catch (Exception)
                            {
                                return;
                            }

                            foreach (MinotaurRelic relic in MinotaurRelicManager.m_minotaurrelics.Values)
                            {
                                if (relic != null && relic.CurrentRegionID == region)
                                {
                                    info.Add(relic.ToString());
                                    info.Add("===========================");
                                }
                            }

                            info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.ShowAll.Count", info.Count));

                            client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.ShowAll.Infos"), info);

                            return;
                        }

                        foreach (MinotaurRelic relic in MinotaurRelicManager.m_minotaurrelics.Values)
                        {
                            if (relic != null)
                            {
                                info.Add(relic.ToString());
                                info.Add("===========================");
                            }
                        }

                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.ShowAll.Count", MinotaurRelicManager.m_minotaurrelics.Count));

                        client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.ShowAll.Infos"), info);

                        break;
                    }
                #endregion ShowAll
                #region Spawn
                case "spawn":
                    {
                        if (args.Length != 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = MinotaurRelicManager.GetRelic(Convert.ToInt32(args[2]));

                        if (relic == null)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (relic.respawntimer == null)
                        {
                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Commands.GM.MinoRelic.Spawn.AlreadySpawned"));
                            return;
                        }

                        relic.ManualRespawn();

                        break;
                    }
                    #endregion Spawn
            }
        }
    }
}
