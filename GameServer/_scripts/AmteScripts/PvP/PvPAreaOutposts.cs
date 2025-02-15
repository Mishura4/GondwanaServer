using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Database;
using System;
using System.Collections.Generic;
using DOL.GS.Geometry;
using AmteScripts.PvP.CTF;

namespace AmteScripts.PvP
{
    public static class PvPAreaOutposts
    {
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
        private static readonly List<TempObjectTemplate> s_treasureHuntBaseTemplates = new List<TempObjectTemplate>()
        {
            // Banner #1
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name      = "PvP Banner",
                Model     = 3223,
                XOffset   = 116,
                YOffset   = -71,
                ZOffset   = 0,
                HeadingOffset = -32,
            },
            // Banner #2
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name      = "PvP Banner",
                Model     = 3223,
                XOffset   = 0,
                YOffset   = -132,
                ZOffset   = 0,
                HeadingOffset = -11,
            },
            // Chest
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.PVPChest",
                Name      = "PvP Chest",
                Model     = 1596,
                XOffset   = 36,
                YOffset   = -71,
                ZOffset   = 0,
                HeadingOffset = -26,
            },
        };

        /// <summary>
        /// Creates the "TreasureHuntBase" objects at the given center,
        /// for the specified owner (player or guild).
        /// Returns a list of the newly created items, so you can store them in 
        /// the area that called us, and remove them later.
        /// </summary>
        public static List<GameStaticItem> CreateTreasureHuntBase(Position center, GamePlayer ownerPlayer, Guild ownerGuild)
        {
            var createdItems = new List<GameStaticItem>();
            Region region = center.Region;

            if (region == null)
                return createdItems;

            foreach (var template in s_treasureHuntBaseTemplates)
            {
                int finalX = center.X + template.XOffset;
                int finalY = center.Y + template.YOffset;
                int finalZ = center.Z + template.ZOffset;
                ushort finalHeading = (ushort)((center.Orientation.InHeading + template.HeadingOffset) & 0xFFF);

                // Instantiate the item
                GameStaticItem item = CreateStaticItemFromClassType(template.ClassType);

                item.Model = (ushort)template.Model;
                item.Name = template.Name;
                item.Position = Position.Create(center.RegionID, finalX, finalY, finalZ, finalHeading);
                item.Realm = 0;

                item.AddToWorld();

                // Now set the ownership:
                if (ownerGuild != null)
                {
                    item.SetGuildOwner(ownerGuild);
                    item.Emblem = ownerGuild.Emblem;
                }
                else if (ownerPlayer != null)
                {
                    item.AddOwner(ownerPlayer);
                }

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
            GamePlayer ownerPlayer, Guild ownerGuild)
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

            if (ownerGuild != null)
            {
                tempPad.SetGuildOwner(ownerGuild);
                tempPad.Emblem = ownerGuild.Emblem;
            }
            else if (ownerPlayer != null)
            {
                tempPad.AddOwner(ownerPlayer);
            }

            createdItems.Add(tempPad);
            return createdItems;
        }

        private static readonly List<TempObjectTemplate> s_guildOutpostTemplate01Templates = new List<TempObjectTemplate>()
        {
            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name      = "PvP Banner",
                Model     = 3223,
                XOffset   = -96,
                YOffset   = -136,
                ZOffset   = 77,
                HeadingOffset = 26,
            },

            new TempObjectTemplate
            {
                ClassType = "DOL.GS.TerritoryBanner",
                Name      = "PvP Banner",
                Model     = 3223,
                XOffset   = -120,
                YOffset   = 113,
                ZOffset   = 81,
                HeadingOffset = 31,
            },

            new TempObjectTemplate
            {
                ClassType = "DOL.GS.GameStaticItem",
                Name      = "Campfire",
                Model     = 3460,
                XOffset   = -72,
                YOffset   = -6,
                ZOffset   = 72,
                HeadingOffset = 0,
            },

            new TempObjectTemplate
            {
                ClassType = "DOL.GS.GameStaticItem",
                Name      = "Tent",
                Model     = 1600,
                XOffset   = -267,
                YOffset   = -15,
                ZOffset   = 69,
                HeadingOffset = -31,
            },
        };

        /// <summary>
        /// Creates the "Guild Outpost Template 01" objects at the given center,
        /// for the specified owner (player or guild).
        /// This new type is used when the session type is 5 or 6.
        /// </summary>
        public static List<GameStaticItem> CreateGuildOutpostTemplate01(Position center,
            GamePlayer ownerPlayer, Guild ownerGuild)
        {
            var createdItems = new List<GameStaticItem>();
            Region region = center.Region;
            if (region == null)
                return createdItems;

            foreach (var template in s_guildOutpostTemplate01Templates)
            {
                int finalX = center.X + template.XOffset;
                int finalY = center.Y + template.YOffset;
                int finalZ = center.Z + template.ZOffset;
                ushort finalHeading = (ushort)((center.Orientation.InHeading + template.HeadingOffset) & 0xFFF);

                // Instantiate the static item
                GameStaticItem item = new GameStaticItem();
                item.Model = (ushort)template.Model;
                item.Name = template.Name;
                item.Position = Position.Create(center.RegionID, finalX, finalY, finalZ, finalHeading);
                item.Realm = 0;
                item.AddToWorld();

                // Set ownership if provided:
                if (ownerGuild != null)
                {
                    item.SetGuildOwner(ownerGuild);
                    item.Emblem = ownerGuild.Emblem;
                }
                else if (ownerPlayer != null)
                {
                    item.AddOwner(ownerPlayer);
                }

                createdItems.Add(item);
            }

            return createdItems;
        }

        // Helper method to instantiate a GameStaticItem by class name
        private static GameStaticItem CreateStaticItemFromClassType(string classType)
        {
            if (classType.EndsWith("TerritoryBanner"))
                return new TerritoryBanner();
            else if (classType.EndsWith("PVPChest"))
                return new PVPChest();
            else
                return new GameStaticItem(); // fallback
        }

        /// <summary>
        /// A small struct describing the offset from the center and the class model info.
        /// </summary>
        private struct TempObjectTemplate
        {
            public string ClassType;
            public string Name;
            public int Model;
            public int XOffset;
            public int YOffset;
            public int ZOffset;
            public int HeadingOffset;
        }
    }
}
