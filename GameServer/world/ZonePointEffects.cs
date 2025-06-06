﻿/*
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
using System.Linq;
using System.Collections.Generic;

using DOL.Events;
using DOL.Database;
using DOL.GS.Geometry;
using System.Numerics;

namespace DOL.GS.GameEvents
{
    public class ZonePointEffect
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
        {

            // What npctemplate should we use for the zonepoint ?
            ushort model;
            NpcTemplate zp;
            try
            {
                model = (ushort)ServerProperties.Properties.ZONEPOINT_NPCTEMPLATE;
                zp = new NpcTemplate(DOLDB<DBNpcTemplate>.SelectObjects(DB.Column(nameof(DBNpcTemplate.TemplateId)).IsEqualTo(model)).FirstOrDefault());
                if (model <= 0 || zp == null) throw new ArgumentNullException();
            }
            catch
            {
                return;
            }

            // processing all the ZP
            IList<ZonePoint> zonePoints = GameServer.Database.SelectAllObjects<ZonePoint>();
            foreach (var z in zonePoints)
            {
                if (z.SourceRegion == 0) continue;

                // find target region for the current zonepoint
                Region r = WorldMgr.GetRegion(z.TargetRegion);
                if (r == null)
                {
                    log.Warn("Zonepoint Id (" + z.Id + ") references an inexistent target region " + z.TargetRegion + " - skipping, ZP not created");
                    continue;
                }

                GameNPC npc = new GameNPC(zp);

                npc.Position = z.GetSourcePosition().With(npc.Orientation);
                npc.Name = r.Description;
                npc.GuildName = "ZonePoint (Open)";
                if (r.IsDisabled) npc.GuildName = "ZonePoint (Closed)";

                npc.AddToWorld();
            }
        }
    }
}