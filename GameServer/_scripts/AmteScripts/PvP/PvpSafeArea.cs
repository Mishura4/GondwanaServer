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
        public GamePlayer OwnerPlayer { get; set; }
        public Guild OwnerGuild { get; set; }

        public int X => Coordinate.X;
        public int Y => Coordinate.Y;
        public int Z => Coordinate.Z;

        private readonly List<GameStaticItem> _ownedStaticItems = new List<GameStaticItem>();
        private readonly Dictionary<string, int> _familyCounts = new Dictionary<string, int>();

        private static string GetDefaultName(GamePlayer owner) => owner.Guild == null ? owner.Name + "'s Outpost" : owner.Name + "'s Group Outpost";
        public PvpTempArea(GamePlayer owner, int x, int y, int z, int radius, bool safeArea)
            : base(GetDefaultName(owner), x, y, z, radius)
        {
            OwnerPlayer = owner;
            OwnerGuild = owner.Guild;
            IsTemporary = true;
            IsPvP = true;
            CanVol = true;
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
            m_Description = GetDefaultName(player);
            OwnerGuild = player.Guild;
            var previousOwner = OwnerPlayer;
            OwnerPlayer = player;

            foreach (var item in _ownedStaticItems)
            {
                item.Emblem = OwnerGuild?.Emblem ?? 0;
                if (item is GamePvPStaticItem pvpItem)
                    pvpItem.SetOwnership(player);
                if (previousOwner != null)
                    item.RemoveOwner(OwnerPlayer);
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