#nullable enable
using AmteScripts.Managers;
using AmteScripts.PvP.CTF;
using DOL.GS;
using DOL.GS.Geometry;
using static DOL.GS.GameObject;
using System.Collections.Generic;
using System.Numerics;

namespace AmteScripts.Areas
{
    /// <summary>
    /// PvpTempArea => inherits from Area.Circle but also tracks ownership logic.
    /// </summary>
    public class PvpTempArea : Area.Circle
    {
        private GamePlayer? _ownerPlayer;
        private Guild? _ownerGuild;

        public GamePlayer? OwnerPlayer
        {
            get => _ownerPlayer;
            set
            {
                _ownerPlayer = value;
                m_Description = GetSoloName(value);
                foreach (var item in _ownedStaticItems)
                {
                    item.OwnerGuild = null;
                    item.Owner = value;
                }
            }
        }

        public Guild? OwnerGuild
        {
            get => _ownerGuild;
            set
            {
                _ownerGuild = value;
                m_Description = GetGuildName(value);
                foreach (var item in _ownedStaticItems)
                {
                    item.OwnerGuild = value;
                }
            }
        }

        public int X => Coordinate.X;
        public int Y => Coordinate.Y;
        public int Z => Coordinate.Z;

        private readonly List<GameStaticItem> _ownedStaticItems = new List<GameStaticItem>();
        private readonly Dictionary<string, int> _familyCounts = new Dictionary<string, int>();

        private static string GetGuildName(Guild? owner) => owner == null ? "Outpost" : owner.Name + "'s Group Outpost";

        private static string GetSoloName(GamePlayer? owner) => owner == null ? "Outpost" : owner.Name + "'s Outpost";

        private static string GetDefaultName(GamePlayer? owner) => owner?.Guild != null ? GetGuildName(owner.Guild) : GetSoloName(owner);
        
        public PvpTempArea(GamePlayer owner, int x, int y, int z, int radius, bool safeArea)
            : base(GetDefaultName(owner), x, y, z, radius)
        {
            OwnerPlayer = owner;
            OwnerGuild = owner.Guild;
            IsTemporary = true;
            IsPvP = true;
            CanVol = !safeArea;
            m_safeArea = safeArea;
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

        public void SetOwnership(GamePlayer player)
        {
            if (player?.Guild != null)
            {
                OwnerGuild = player.Guild;
            }
            else
            {
                OwnerPlayer = player;
            }
        }

        public void AddOwnedObject(GameStaticItem obj)
        {
            if (!_ownedStaticItems.Contains(obj))
                _ownedStaticItems.Add(obj);
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