using AmteScripts.PvP.CTF;
using DOL.GS;
using DOL.GS.Geometry;
using static DOL.GS.GameObject;
using System.Collections.Generic;

namespace AmteScripts.Areas
{
    /// <summary>
    /// PvpCircleArea => inherits from Area.Circle but also tracks ownership logic.
    /// </summary>
    public class PvpCircleArea : Area.Circle
    {
        public GamePlayer OwnerPlayer { get; set; }
        public Guild OwnerGuild { get; set; }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        private readonly List<GameStaticItem> _ownedStaticItems = new List<GameStaticItem>();
        private readonly Dictionary<string, int> _familyCounts = new Dictionary<string, int>();

        public PvpCircleArea(string desc, int x, int y, int z, int radius)
            : base(desc, x, y, z, radius)
        {
            IsTemporary = true;
            IsPvP = true;
            CanVol = true;

            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public int GetFamilyCount(string guildName)
        {
            if (string.IsNullOrEmpty(guildName)) return 0;
            _familyCounts.TryGetValue(guildName, out int count);
            return count;
        }

        public void SetFamilyCount(string guildName, int newCount)
        {
            if (string.IsNullOrEmpty(guildName)) return;
            _familyCounts[guildName] = newCount;
        }

        public void AddOwnedObject(GameStaticItem obj)
        {
            if (!_ownedStaticItems.Contains(obj))
                _ownedStaticItems.Add(obj);

            if (OwnerGuild != null)
            {
                obj.SetGuildOwner(OwnerGuild);
                obj.Emblem = OwnerGuild.Emblem;
            }
            else if (OwnerPlayer != null)
            {
                obj.AddOwner(OwnerPlayer);
            }
        }

        public void RemoveAllOwnedObjects()
        {
            foreach (var item in _ownedStaticItems)
            {
                if (item.ObjectState == GameObject.eObjectState.Active)
                {
                    item.Delete();
                }
            }
            _ownedStaticItems.Clear();
        }
    }

    /// <summary>
    /// PvpSafeArea => inherits from Area.SafeArea but also tracks ownership logic.
    /// </summary>
    public class PvpSafeArea : Area.SafeArea
    {
        public GamePlayer OwnerPlayer { get; set; }
        public Guild OwnerGuild { get; set; }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        private readonly List<GameStaticItem> _ownedStaticItems = new List<GameStaticItem>();
        private readonly Dictionary<string, int> _familyCounts = new Dictionary<string, int>();

        public IReadOnlyList<GameStaticItem> Items => _ownedStaticItems.AsReadOnly();
        
        public PvpSafeArea(string desc, int x, int y, int z, int radius)
            : base(desc, x, y, z, radius)
        {
            IsTemporary = true;
            IsPvP = true;
            CanVol = false;

            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public int GetFamilyCount(string guildName)
        {
            if (string.IsNullOrEmpty(guildName)) return 0;
            _familyCounts.TryGetValue(guildName, out int count);
            return count;
        }

        public void SetFamilyCount(string guildName, int newCount)
        {
            if (string.IsNullOrEmpty(guildName)) return;
            _familyCounts[guildName] = newCount;
        }

        public void AddOwnedObject(GameStaticItem obj)
        {
            if (!_ownedStaticItems.Contains(obj))
                _ownedStaticItems.Add(obj);

            if (OwnerGuild != null)
            {
                obj.SetGuildOwner(OwnerGuild);
                obj.Emblem = OwnerGuild.Emblem;
            }
            else if (OwnerPlayer != null)
            {
                obj.AddOwner(OwnerPlayer);
            }
        }

        public void RemoveAllOwnedObjects()
        {
            foreach (var item in _ownedStaticItems)
            {
                if (item.ObjectState == GameObject.eObjectState.Active)
                {
                    item.Delete();
                }
            }
            _ownedStaticItems.Clear();
        }
    }
}