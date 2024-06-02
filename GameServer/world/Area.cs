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
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Database;
using DOL.Geometry;
using DOL.GS.Geometry;
using DOL.Language;
using System.Numerics;
using static DOL.GS.WarMapMgr;

namespace DOL.GS
{
    /// <summary>
    /// Collection of basic area shapes
    /// Circle
    /// Square
    /// </summary>
    public class Area
    {
        public class Square : AbstractArea
        {
            /// <summary>
            /// The center coordinate of this Area
            /// </summary>
            public Coordinate Coordinate { get; private set; }

            /// <summary>
            /// The width of this Area 
            /// </summary>
            protected int m_Width;

            /// <summary>
            /// The height of this Area 
            /// </summary>
            protected int m_Height;

            public Square()
                : base()
            { }

            public Square(string desc, int x, int y, int z, int width, int height, bool isPvp) : base(desc)
            {
                Coordinate = Coordinate.Create(x, y, z);
                m_Height = height;
                m_Width = width;
                IsPvP = isPvp;
            }

            /// <summary>
            /// Returns the Width of this Area
            /// </summary>
            public int Width
            {
                get { return m_Width; }
            }

            /// <summary>
            /// Returns the Height of this Area
            /// </summary>
            public int Height
            {
                get { return m_Height; }
            }

            /// <summary>
            /// Checks wether area intersects with given zone
            /// </summary>
            /// <param name="zone"></param>
            /// <returns></returns>
            public override bool IsIntersectingZone(Zone zone)
            {
                if (Coordinate.X + Width < zone.Offset.X)
                    return false;
                if (Coordinate.X - Width >= zone.Offset.X + 65536)
                    return false;
                if (Coordinate.Y + Height < zone.Offset.Y)
                    return false;
                if (Coordinate.Y - Height >= zone.Offset.Y + 65536)
                    return false;

                return true;
            }

            /// <summary>
            /// Checks wether given point is within area boundaries
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            public override bool IsContaining(Coordinate p, bool checkZ)
            {
                var m_xdiff = p.X - Coordinate.X;
                if (m_xdiff < 0 || m_xdiff > Width)
                    return false;

                var m_ydiff = p.Y - Coordinate.Y;
                if (m_ydiff < 0 || m_ydiff > Height)
                    return false;

                /*
                //SH: Removed Z checks when one of the two Z values is zero(on ground)
                if (Z != 0 && spotZ != 0)
                {
                    long m_zdiff = (long) spotZ - Z;
                    if (m_zdiff> Radius)
                        return false;
                }
                */

                return true;
            }

            /// <inheritdoc />
            public override float DistanceSquared(Coordinate position, bool checkZ)
            {
                int dirX = position.X - Coordinate.X;
                int dirY = position.X - Coordinate.X;
                int interX;
                int interY;

                if (Math.Abs(dirX) < Width && Math.Abs(dirY) < Height) // Inside
                {
                    return 0.0f;
                }
                if (dirX > 0)
                {
                    interX = Coordinate.X + Math.Min(Width, dirX);
                }
                else
                {
                    interX = Coordinate.X - Math.Max(-Width, dirX);
                }

                if (dirY > 0)
                {
                    interY = Coordinate.Y + Math.Min(Height, dirY);
                }
                else
                {
                    interY = Coordinate.Y - Math.Max(-Height, dirY);
                }
                float dx = Coordinate.X - interX;
                float dy = Coordinate.Y - interY;
                return dx * dx + dy * dy;
            }

            public override void LoadFromDatabase(DBArea area)
            {
                DbArea = area;
                m_translationId = area.TranslationId;
                m_Description = area.Description;
                Coordinate = Coordinate.Create(area.X, area.Y, area.Z);
                m_Width = area.Radius;
                m_Height = area.Radius;
                this.CanVol = area.AllowVol;
                RealmPoints = area.RealmPoints;
                this.IsPvP = area.IsPvP;
            }
        }

        public class Circle : AbstractArea
        {
            /// <summary>
            /// The radius of the area in Coordinates
            /// </summary>
            protected int m_Radius;

            protected long m_distSq;

            public Circle()
                : base()
            {
            }

            public Circle(string desc, Coordinate center, int radius) : base(desc)
            {
                m_Description = desc;
                Coordinate = center;
                m_Radius = radius;
                m_RadiusRadius = radius * radius;
            }

            public Circle(string desc, int x, int y, int z, int radius) : this(desc, Coordinate.Create(x, y, z), radius)
            {
            }

            public Coordinate Coordinate { get; private set; }

            /// <summary>
            /// Returns the Height of this Area
            /// </summary>
            public int Radius
            {
                get { return m_Radius; }
            }

            /// <summary>
            /// Cache for radius*radius to increase performance of circle check,
            /// radius is still needed for square check
            /// </summary>
            protected int m_RadiusRadius;


            /// <summary>
            /// Checks wether area intersects with given zone
            /// </summary>
            /// <param name="zone"></param>
            /// <returns></returns>
            public override bool IsIntersectingZone(Zone zone)
            {
                if (Coordinate.X + Radius < zone.Offset.X)
                    return false;
                if (Coordinate.X - Radius >= zone.Offset.X + 65536)
                    return false;
                if (Coordinate.Y + Radius < zone.Offset.Y)
                    return false;
                if (Coordinate.Y - Radius >= zone.Offset.Y + 65536)
                    return false;

                return true;
            }

            public override bool IsContaining(Coordinate point, bool checkZ)
            {
                // spot is not in square around circle no need to check for circle...
                var diff = point - Coordinate;

                // check if spot is in circle
                var m_distSq = diff.X * diff.X + diff.Y * diff.Y;
                if (Coordinate.Z != 0 && point.Z != 0 && checkZ)
                {
                    int m_zdiff = point.Z - Coordinate.Z;
                    m_distSq += m_zdiff * m_zdiff;
                }

                return (m_distSq <= m_RadiusRadius);
            }

            /// <inheritdoc />
            public override float DistanceSquared(Coordinate position, bool checkZ)
            {
                var direction = position - Coordinate;
                float radiusSquared = Radius * Radius;
                var distanceToCenterSquared = (float)(checkZ ? direction.Length : direction.Length2D);

                if (distanceToCenterSquared < radiusSquared) // Inside
                {
                    return 0.0f;
                }
                return distanceToCenterSquared - radiusSquared;
            }

            public override void LoadFromDatabase(DBArea area)
            {
                DbArea = area;
                m_translationId = area.TranslationId;
                m_Description = area.Description;
                Coordinate = Coordinate.Create(area.X, area.Y, area.Z);
                m_Radius = area.Radius;
                m_RadiusRadius = area.Radius * area.Radius;
                this.CanVol = area.AllowVol;
                RealmPoints = area.RealmPoints;
                this.IsPvP = area.IsPvP;
            }
        }

        public class Polygon : AbstractArea
        {
            /// <summary>
            /// The center coordinate of this Area
            /// </summary>
            public Coordinate Coordinate { get; private set; }

            /// <summary>
            /// Returns the Height of this Area
            /// </summary>
            protected int m_Radius;

            /// <summary>
            /// The radius of the area in Coordinates
            /// </summary>
            public int Radius
            {
                get { return m_Radius; }
            }

            /// <summary>
            /// The Points string
            /// </summary>
            protected string m_stringpoints;

            /// <summary>
            /// The Points list
            /// </summary>
            protected IList<Point2D> m_points;

            public Polygon()
                : base()
            {
            }

            public Polygon(string desc, int x, int y, int z, int radius, string points)
                : base(desc)
            {
                m_Description = desc;
                Coordinate = Coordinate.Create(x, y, z);
                m_Radius = radius;
                StringPoints = points;
            }

            /// <summary>
            /// Get / Set(init) the serialized points
            /// </summary>
            public string StringPoints
            {
                get
                {
                    return m_stringpoints;
                }
                set
                {
                    m_stringpoints = value;
                    m_points = new List<Point2D>();
                    if (m_stringpoints.Length < 1) return;
                    string[] points = m_stringpoints.Split('|');
                    foreach (string point in points)
                    {
                        string[] pts = point.Split(';');
                        if (pts.Length != 2) continue;
                        int x = Convert.ToInt32(pts[0]);
                        int y = Convert.ToInt32(pts[1]);
                        Point2D p = new Point2D(x, y);
                        if (!m_points.Contains(p)) m_points.Add(p);
                    }
                }
            }

            /// <summary>
            /// Checks wether area intersects with given zone
            /// </summary>
            /// <param name="zone"></param>
            /// <returns></returns>
            public override bool IsIntersectingZone(Zone zone)
            {
                // TODO if needed
                if (Coordinate.X + Radius < zone.Offset.X)
                    return false;
                if (Coordinate.X - Radius >= zone.Offset.X + 65536)
                    return false;
                if (Coordinate.Y + Radius < zone.Offset.Y)
                    return false;
                if (Coordinate.Y - Radius >= zone.Offset.Y + 65536)
                    return false;

                return true;
            }

            public override bool IsContaining(Coordinate obj, bool _checkZ)
            {
                if (m_points.Count < 3) return false;
                Point2D p1, p2;
                bool inside = false;

                Point2D oldpt = new Point2D(m_points[m_points.Count - 1].X, m_points[m_points.Count - 1].Y);

                foreach (Point2D pt in m_points)
                {
                    Point2D newpt = new Point2D(pt.X, pt.Y);

                    if (newpt.X > oldpt.X) { p1 = oldpt; p2 = newpt; }
                    else { p1 = newpt; p2 = oldpt; }

                    if ((newpt.X < obj.X) == (obj.X <= oldpt.X)
                        && (obj.Y - p1.Y) * (p2.X - p1.X) < (p2.Y - p1.Y) * (obj.X - p1.X))
                        inside = !inside;

                    oldpt = newpt;
                }
                return inside;
            }

            /// <inheritdoc />
            public override float DistanceSquared(Coordinate position, bool checkZ)
            {
                var direction = position - Coordinate;
                float radiusSquared = Radius * Radius;
                var distanceToCenterSquared = (float)(checkZ ? direction.Length : direction.Length2D);

                if (distanceToCenterSquared < radiusSquared) // Inside
                {
                    return 0.0f;
                }
                return distanceToCenterSquared - radiusSquared;
            }

            public override void LoadFromDatabase(DBArea area)
            {
                DbArea = area;
                m_translationId = area.TranslationId;
                m_Description = area.Description;
                Coordinate = Coordinate.Create(area.X, area.Y, area.Z);
                m_Radius = area.Radius;
                StringPoints = area.Points;
                this.CanVol = area.AllowVol;
                RealmPoints = area.RealmPoints;
                this.IsPvP = area.IsPvP;
            }
        }

        public class BindArea : Circle
        {
            protected BindPoint m_dbBindPoint;

            public BindArea()
                : base()
            {
                m_displayMessage = false;
            }

            public BindArea(string desc, BindPoint dbBindPoint)
                : base(desc, dbBindPoint.X, dbBindPoint.Y, dbBindPoint.Z, dbBindPoint.Radius)
            {
                m_dbBindPoint = dbBindPoint;
                m_displayMessage = false;
            }

            public BindPoint BindPoint
            {
                get { return m_dbBindPoint; }
            }

            public override void LoadFromDatabase(DBArea area)
            {
                base.LoadFromDatabase(area);

                m_dbBindPoint = new BindPoint();
                m_dbBindPoint.Radius = (ushort)area.Radius;
                m_dbBindPoint.X = area.X;
                m_dbBindPoint.Y = area.Y;
                m_dbBindPoint.Z = area.Z;
                m_dbBindPoint.Region = area.Region;
            }
        }

        public class SafeArea : Circle
        {
            public SafeArea()
                : base()
            {
                m_safeArea = true;
            }

            public SafeArea(string desc, int x, int y, int z, int radius)
                : base
                (desc, x, y, z, radius)
            {
                m_safeArea = true;
            }
        }

        public class CombatZone : Circle
        {
            public CombatZone()
            {
            }

            public CombatZone(Guild owningGuild, Coordinate position)
            : base("", position, ServerProperties.Properties.GUILD_COMBAT_ZONE_RADIUS)
            {
                CanVol = true;
                RealmPoints = 2;
                IsPvP = true;
                CanBroadcast = true;
                Sound = 0;
                CheckLOS = false;
                IsTemporary = true;
                OwningGuild = owningGuild;
            }

            public Guild OwningGuild { get; init; }

            /// <inheritdoc />
            public override string GetDescriptionForPlayer(GamePlayer player)
            {
                return LanguageMgr.GetTranslation(player.Client.Account.Language, "Commands.Players.Guild.CombatZone.AreaName", OwningGuild.Name);
            }

            /// <inheritdoc />
            public override string GetDescriptionForPlayer(GamePlayer player, out string screenDescription)
            {
                screenDescription = GetDescriptionForPlayer(player);
                return screenDescription;
            }
        }
    }
}
