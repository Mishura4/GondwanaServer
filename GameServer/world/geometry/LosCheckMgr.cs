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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using log4net;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using DOL.GS.Geometry;
using System.Numerics;
using System.Threading;
using System.Diagnostics;
using Vector3 = System.Numerics.Vector3;
using System.Drawing;

namespace DOL.GS
{
    using LosTreeType = QuadtreeNode<Triangle>;

    public static class LosCheckMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        /// <summary>
        /// When a (N+1)-th triangle is added in a node, it will be divised (if it's not the max level and min size)
        /// Testing a thousand triangles is ok, with 100 instead of 1000, it's twice faster but also take more memory
        /// </summary>
        public const int MAX_TRIANGLE_PER_TREE_NODE = 1000;

        private static readonly Dictionary<int, LosTreeType[]> _regionTriangles = new Dictionary<int, LosTreeType[]>();

        public static void Initialize()
        {
            if (!ServerProperties.Properties.LOS_MGR_ENABLE)
            {
                log.Info("LosCheckMgr is disabled by a server property");
                _regionTriangles.Clear();
                return;
            }
            LosTreeType.MaxObjectsPerNode = MAX_TRIANGLE_PER_TREE_NODE;
            Parallel.ForEach(WorldMgr.GetAllRegions(), region =>
            {
                var tick = GameServer.Instance.TickCount;
                log.Debug($"loading region {region.Description} (id:{region.ID})...");
                var zoneTriangles = new LosTreeType[region.Zones.Count];
                lock (_regionTriangles)
                    _regionTriangles.Add(region.ID, zoneTriangles);
                int i = 0;
                foreach (var zone in region.Zones)
                    zoneTriangles[i++] = _LoadZone(region, zone, Path.Combine("obj", $"zone{zone.ZoneSkinID:D3}.obj"));
                if (log.IsDebugEnabled)
                    log.Debug($"[LosMgr] region {region.Description} (id:{region.ID}): {GameServer.Instance.TickCount - tick}ms");
            });

            // just useful to do some tests, I keep it in case I want to implement a BVH version
            //StressTests();
        }

        public static float GetCollisionDistance(GameObject origin, GameObject target, ref RaycastStats stats)
        {
            if (origin.CurrentRegion != target.CurrentRegion)
                return float.PositiveInfinity;
            var height = DOL.GS.Geometry.Vector.Create(0, 0, 64); // maybe we should calculate that from the model id and size -- 64inch seems to be okay-ish for players
            return GetCollisionDistance(origin.CurrentRegion, (origin.Coordinate + height).ToSysVector3(), (target.Coordinate + height).ToSysVector3(), ref stats);
        }

        public static float GetCollisionDistance(Region region, Coordinate origin, Coordinate target, ref RaycastStats stats)
        {
            var height = DOL.GS.Geometry.Vector.Create(0, 0, 64); // maybe we should calculate that from the model id and size -- 64inch seems to be okay-ish for players
            return GetCollisionDistance(region, (origin + height).ToSysVector3(), (target + height).ToSysVector3(), ref stats);
        }
        
        
        
        public static float GetCollisionDistance(Region region, Vector3 origin, Vector3 target, ref RaycastStats stats)
        {
            return GetCollisionDistance(region.ID, origin, target, ref stats);
        }
        public static float GetCollisionDistance(int regionId, Vector3 origin, Vector3 target, ref RaycastStats stats)
        {
            if (!_regionTriangles.ContainsKey(regionId))
                return float.PositiveInfinity;
            var diff = target - origin;
            var distance = diff.Length() + 1e-6f;
            if (distance < 16 || distance > WorldMgr.VISIBILITY_DISTANCE)
                return float.PositiveInfinity;

            stats.nbTests += 1;
            diff = Vector3.Normalize(diff);
            foreach (var tree in _regionTriangles[regionId])
            {
                if (tree == null)
                    continue;
                var rayDist = tree.CollideWithRay(origin, diff, distance, ref stats);
                if (distance > rayDist)
                    return rayDist;
            }
            return float.PositiveInfinity;
        }

        public static LosTreeType InitializeForTestOnly(string filename)
        {
            var reg = WorldMgr.GetAllRegions().FirstOrDefault();
            var zone = new Zone(reg, 0, "Test zone", 8, 8, 8, 8, 0, false, 0, false, 0, 0, 0, 0, 0, true, true, 1.0f, false);
            return _LoadZone(reg, zone, filename);
        }

        public static bool HasDataFor(Region region)
        {
            return _regionTriangles.ContainsKey(region.ID);
        }

        private static LosTreeType _LoadZone(Region region, Zone zone, string filename)
        {
            var (vertices, objects) = WavefrontObjFile.Load(zone, filename);
            if (vertices == null || vertices.Length == 0 || objects == null)
                return null;

            var zoneMin = new Vector3(float.PositiveInfinity);
            var zoneMax = new Vector3(float.NegativeInfinity);
            foreach (var v in vertices)
            {
                if (v.Z <= 0 || v.Z > 65530)
                    continue;
                zoneMin = Vector3.Min(zoneMin, v);
                zoneMax = Vector3.Max(zoneMax, v);
            }
            zoneMin.Z -= 500;
            zoneMax.Z += 500;

            var zoneTree = new LosTreeType(new AABoundingBox(zoneMin, zoneMax), 1);
            foreach (var faces in objects)
                foreach (var (a, b, c) in faces)
                    zoneTree.AddObject(new Triangle(vertices, a, b, c));
            if (log.IsDebugEnabled)
                log.Debug($"[LosMgr] Zone {zone.ZoneSkinID} loaded: {objects.Count:N0} objects {objects.Select(o => o.Length).Sum():N0} faces {vertices.Length:N0} vertices");
            return zoneTree;
        }

        /// <summary>
        /// This function is useful if you want to test some parameters
        /// </summary>
        private static void StressTests()
        {
            log.Debug($"{LosTreeType.createdNodes} tree nodes");
            var reg = WorldMgr.GetRegion(201);

            // just warm the hot path
            var stats = new RaycastStats();
            var objects = reg.Objects.Where(o => o != null).ToList();
            foreach (var o1 in objects.Take(1000))
                foreach (var o2 in objects.Skip(1000).Take(1000))
                    GetCollisionDistance(o1.CurrentRegion, o1.Coordinate, o2.Coordinate, ref stats);

            var sw = new Stopwatch();
            sw.Start();
            stats = new RaycastStats();
            foreach (var o1 in objects)
                foreach (var o2 in objects)
                    GetCollisionDistance(o1.CurrentRegion, o1.Coordinate, o2.Coordinate, ref stats);
            sw.Stop();
            long count = objects.Count * objects.Count;
            var raySeconds = stats.nbTests * 1000 / sw.ElapsedMilliseconds;
            log.Debug($"tests {count:N0} tests ({stats.nbTests:N0} ray, {stats.nbNodeTests:N0} node, {stats.nbAABBTests:N0} aabb, {stats.nbFaceTests:N0} face) in {sw.ElapsedMilliseconds} ms (~{count:N0} tests/s ~{raySeconds:N0} r/s)");

            reg = WorldMgr.GetRegion(51);
            objects = reg.Objects.Where(o => o != null && (o.CurrentZone.ID == 51 || o.CurrentZone.ID == 52)).ToList();
            sw.Reset();
            sw.Start();
            stats = new RaycastStats();
            foreach (var o1 in objects)
                foreach (var o2 in objects)
                    GetCollisionDistance(o1.CurrentRegion, o1.Coordinate, o2.Coordinate, ref stats);
            sw.Stop();
            count = objects.Count * objects.Count;
            raySeconds = stats.nbTests * 1000 / sw.ElapsedMilliseconds;
            log.Debug($"tests {count:N0} tests ({stats.nbTests:N0} ray, {stats.nbNodeTests:N0} node, {stats.nbAABBTests:N0} aabb, {stats.nbFaceTests:N0} face) in {sw.ElapsedMilliseconds} ms (~{count:N0} tests/s ~{raySeconds:N0} r/s)");

            // stop to read the logs
            Console.ReadKey();
        }
    }
}
