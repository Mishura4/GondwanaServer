using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Database;
using System;
using System.Collections.Generic;
using DOL.GS.Geometry;
using AmteScripts.PvP.CTF;
using AmteScripts.Managers;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Vector = DOL.GS.Geometry.Vector;

namespace AmteScripts.PvP
{
    public static class PvPAreaOutposts
    {
        private static TempObjectTemplate s_chestTemplate =>
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.PVPChest",
                Name = "PvP Chest",
                Model = 1596,
                Offset = Vector.Create(0, -90, 0),
                Angle = Angle.Zero
            };
        
        /// <summary>
        /// For the "TreasureHuntBase" set, we define 3 offsets from the center
        /// plus the class type, model, etc.
        /// 
        /// Real example: The user gave coordinates for banners vs. the spawn
        /// but we only need "relative" offsets for them.
        /// 
        /// Suppose SPAWN is: X=457083, Y=466747
        /// Banner1 is: X=457199, Y=466676 => offset (116, -71)
        /// Banner2 is: X=457083, Y=466615 => offset (0, -132)
        /// Chest is:   X=457119, Y=466676 => offset (36, -71)
        /// 
        /// We'll store them in a simple struct:
        /// </summary>
        private static ReadOnlyCollection<TempObjectTemplate> s_treasureHuntBaseTemplates => new(
        [
            // Chest
            s_chestTemplate,
    
            // Banner #1
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name      = "PvP Banner",
                Model     = 3223,
                Offset    = s_chestTemplate.Offset + Vector.Create(x: -75),
                Angle     = s_chestTemplate.Angle,
            },
            
            // Banner #2
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name      = "PvP Banner",
                Model     = 3223,
                Offset    = s_chestTemplate.Offset + Vector.Create(x: 75),
                Angle     = s_chestTemplate.Angle,
            },
        ]);

        /// <summary>
        /// Creates the "TreasureHuntBase" objects at the given center,
        /// for the specified owner (player or guild).
        /// Returns a list of the newly created items, so you can store them in 
        /// the area that called us, and remove them later.
        /// </summary>
        public static List<GameStaticItem> CreateTreasureHuntBase(Position center, GamePlayer ownerPlayer)
        {
            var createdItems = new List<GameStaticItem>();
            if (center.Region == null)
                return createdItems;
            
            var ownerGuild = ownerPlayer.Guild;
            foreach (var template in s_treasureHuntBaseTemplates)
            {
                // Instantiate the item
                GameStaticItem item = CreateStaticItemFromClassType(ownerPlayer, template.ClassType);

                item.Model = (ushort)template.Model;
                item.Name = template.Name;
                var offset = template.Offset.RotatedClockwise(center.Orientation);
                item.Position = center with
                {
                    Coordinate = center.Coordinate + offset,
                    Orientation = center.Orientation + template.Angle
                };
                item.Realm = 0;
                item.Emblem = ownerGuild?.Emblem ?? PvpManager.Instance.GetEmblemForPlayer(ownerPlayer);
                if (item is GamePvPStaticItem pvpItem)
                    pvpItem.SetOwnership(ownerPlayer);

                item.AddToWorld();

                createdItems.Add(item);
            }

            return createdItems;
        }

        /// <summary>
        /// Creates the "Capture Flag" outpost pad at the given center Position.
        /// Optionally pass the player or guild that "owns" it.
        /// Returns the newly created pad as a GameStaticItem (or specialized type).
        /// 
        /// If you want multiple items (e.g. banners, etc.), just expand the code below.
        /// </summary>
        public static List<GameStaticItem> CreateCaptureFlagOutpostPad(Position center,
            GamePlayer ownerPlayer)
        {
            var createdItems = new List<GameStaticItem>();
            var region = center.Region;
            if (region == null)
                return createdItems;

            // For instance, place exactly one pad at an offset from 'center':
            int finalX = center.X;
            int finalY = center.Y;
            int finalZ = center.Z;
            ushort finalHeading = center.Orientation.InHeading;

            // Instantiate your new pad:
            var tempPad = new GameCTFTempPad();
            tempPad.Name = "Outpost Flag Pad";
            tempPad.Model = 2655;
            tempPad.Position = Position.Create(region.ID, finalX, finalY, finalZ, finalHeading);
            tempPad.Realm = 0;

            tempPad.AddToWorld();
            tempPad.SetOwnership(ownerPlayer);

            createdItems.Add(tempPad);
            return createdItems;
        }

        private static TempObjectTemplate s_tentTemplate =>
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.GameStaticItem",
                Name = "Tent",
                Model = 1600,
                Offset = Vector.Create(-267, -15, 69),
                Angle = Angle.Degrees(33)
            };

        private static ReadOnlyCollection<TempObjectTemplate> s_guildOutpostTemplate01Templates => new(
        [
            s_tentTemplate,
            
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name = "PvP Banner",
                Model = 3223,
                Offset = Vector.Create(-96, -136, 77),
                Angle = Angle.Degrees(-90),
            },

            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name = "PvP Banner",
                Model = 3223,
                Offset = Vector.Create(-96, 136, 81),
                Angle = Angle.Degrees(-90),
            },

            new TempObjectTemplate
            {
                ClassType = "DOL.GS.GameStaticItem",
                Name = "Campfire",
                Model = 3460,
                Offset = Vector.Create(-72, -6, 72),
                Angle = Angle.Zero,
            },
        ]);

        /// <summary>
        /// Creates the "Guild Outpost Template 01" objects at the given center,
        /// for the specified owner (player or guild).
        /// This new type is used when the session type is 5 or 6.
        /// </summary>
        public static List<GameStaticItem> CreateGuildOutpostTemplate01(Position center,
            GamePlayer ownerPlayer)
        {
            var createdItems = new List<GameStaticItem>();
            Region region = center.Region;
            if (region == null)
                return createdItems;

            Guild? ownerGuild = ownerPlayer.Guild;
            foreach (var template in s_guildOutpostTemplate01Templates)
            {
                // Instantiate the static item
                GameStaticItem item = new GameStaticItem();
                item.Model = (ushort)template.Model;
                item.Name = template.Name;
                var offset = template.Offset.RotatedClockwise(center.Orientation);
                item.Position = center with
                {
                    Coordinate = center.Coordinate + offset,
                    Orientation = center.Orientation + template.Angle
                };
                item.Realm = 0;
                item.Emblem = ownerGuild?.Emblem ?? PvpManager.Instance.GetEmblemForPlayer(ownerPlayer);
                if (item is GamePvPStaticItem pvpItem)
                    pvpItem.SetOwnership(ownerPlayer);
                item.AddToWorld();

                createdItems.Add(item);
            }

            return createdItems;
        }

        // Helper method to instantiate a GameStaticItem by class name
        private static GameStaticItem CreateStaticItemFromClassType(GamePlayer player, string classType)
        {
            if (classType.EndsWith("TerritoryBanner"))
                return new TerritoryBanner();
            else if (classType.EndsWith("PVPChest"))
            {
                PvPScore score;
                if (player.Guild != null)
                    score = PvpManager.Instance.EnsureGroupScore(player.Guild);
                else
                    score = PvpManager.Instance.EnsureSoloScore(player);
                return new PVPChest(score);
            }
            else
                return new GameStaticItem(); // fallback
        }

        /// <summary>
        /// A small struct describing the offset from the center and the class model info.
        /// </summary>
        private record struct TempObjectTemplate(string ClassType, string Name, int Model, Vector Offset, Angle Angle)
        {
        }
    }
}
