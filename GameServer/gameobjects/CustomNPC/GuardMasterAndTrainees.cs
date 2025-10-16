using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Geometry;
using log4net;

namespace DOL.GS
{
    /// <summary>
    /// PackageID formats supported:
    ///   Bare:      "1001x6,1002x3"
    ///   With opts: "TEMPLATES=1001x6,1002x3;RADIUS=120;SPACING=40;FOLLOW=true;DESPAWN_ON_DEATH=true"
    ///   Per-entry extras (pipes):
    ///     1001x8|80;120                 -> radius range (min;max) for the whole package
    ///     1001x8|80;120|0x13            -> + weapon anim (hex or decimal)
    ///     1001x8|80;120|0x16|0x09       -> + weapon and shield anims
    ///   Examples you can place in mob.PackageID:
    ///     "100025x7|80;120"
    ///     "100025x7|80;120|0x16|0x09"
    /// </summary>
    internal sealed class TraineePackage
    {
        internal sealed class Entry
        {
            public int TemplateId;
            public int Count;
        }

        public List<Entry> Entries = new();
        public int? RadiusMin;
        public int? RadiusMax;
        public int Radius = 100;     // smaller default
        public int RingSpacing = 40; // smaller default
        public bool Follow = false;
        public bool DespawnOnDeath = true;

        public ushort WeaponAnim = 0x13;
        public ushort ShieldAnim = 0x00;

        public static bool TryParse(string package, out TraineePackage spec)
        {
            spec = null;
            if (string.IsNullOrWhiteSpace(package)) return false;

            var result = new TraineePackage();

            // Quick path: bare template list
            if (!package.Contains("="))
            {
                if (!ParseTemplatesFragment(package, result)) return false;
                spec = result;
                return true;
            }

            foreach (var rawPart in package.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = rawPart.Trim();
                var eq = part.IndexOf('=');
                if (eq <= 0) continue;

                var key = part.Substring(0, eq).Trim().ToUpperInvariant();
                var val = part[(eq + 1)..].Trim();

                switch (key)
                {
                    case "TEMPLATES":
                        if (!ParseTemplatesFragment(val, result)) return false;
                        break;

                    case "RADIUS":
                        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
                            result.Radius = Math.Max(50, r);
                        break;

                    case "SPACING":
                    case "RINGSPACING":
                        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                            result.RingSpacing = Math.Max(10, s);
                        break;

                    case "FOLLOW":
                        if (bool.TryParse(val, out var f)) result.Follow = f;
                        break;

                    case "DESPAWN_ON_DEATH":
                        if (bool.TryParse(val, out var d)) result.DespawnOnDeath = d;
                        break;
                }
            }

            if (result.Entries.Count == 0) return false;
            spec = result;
            return true;
        }

        private static bool ParseTemplatesFragment(string value, TraineePackage into)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            foreach (var chunk in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var s = chunk.Trim();

                // Accept forms:
                //   1001x8
                //   1001
                //   1001x8|80;120
                //   1001|80;120
                //   1001x8|80;120|0x13
                //   1001x8|80;120|0x16|0x09
                string core = s;
                string rangePart = null;
                string weaponPart = null;
                string shieldPart = null;

                var pipeIdx = s.IndexOf('|');
                if (pipeIdx >= 0)
                {
                    core = s.Substring(0, pipeIdx).Trim();
                    var rest = s[(pipeIdx + 1)..].Trim();
                    var extras = rest.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    if (extras.Length >= 1) rangePart = extras[0].Trim();
                    if (extras.Length >= 2) weaponPart = extras[1].Trim();
                    if (extras.Length >= 3) shieldPart = extras[2].Trim();
                }

                // Parse tplId and optional xCount
                var parts = core.Split(new[] { 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
                if (!int.TryParse(parts[0].Trim(), out var tplId)) return false;

                var count = 1;
                if (parts.Length > 1 && !int.TryParse(parts[1].Trim(), out count)) return false;
                if (count <= 0) continue;

                into.Entries.Add(new Entry { TemplateId = tplId, Count = count });

                if (!string.IsNullOrWhiteSpace(rangePart))
                {
                    var partsR = rangePart.Split(';');
                    if (partsR.Length == 2 &&
                        int.TryParse(partsR[0].Trim(), out var rrMin) &&
                        int.TryParse(partsR[1].Trim(), out var rrMax) &&
                        rrMin > 0 && rrMax >= rrMin)
                    {
                        into.RadiusMin = rrMin;
                        into.RadiusMax = rrMax;
                    }
                }

                // Weapon?
                if (!string.IsNullOrWhiteSpace(weaponPart) && TryParseUShortFlexible(weaponPart, out var w))
                    into.WeaponAnim = w;

                // Shield?
                if (!string.IsNullOrWhiteSpace(shieldPart) && TryParseUShortFlexible(shieldPart, out var sh))
                    into.ShieldAnim = sh;
            }

            return into.Entries.Count > 0;
        }

        private static bool TryParseUShortFlexible(string s, out ushort value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();

            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ushort.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

            return ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }

    /// <summary>
    /// Spawns/handles trainees around a master NPC based on its PackageID.
    /// </summary>
    public sealed class GuardTraineeController
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly GameNPC _master;
        private readonly List<GameNPC> _spawned = new();
        private TraineePackage _spec;
        private System.Timers.Timer _animTimer;
        private static readonly Random _rnd = new();

        private int _chosenBaseRadius;

        public GuardTraineeController(GameNPC master)
        {
            _master = master ?? throw new ArgumentNullException(nameof(master));
        }

        public bool TrySpawnFromPackage()
        {
            _spec = null;
            if (!TraineePackage.TryParse(_master.PackageID, out _spec))
                return false;

            SpawnAll();
            HookLifecycle();
            return true;
        }

        private void HookLifecycle()
        {
            GameEventMgr.AddHandler(_master, GameLivingEvent.Dying, OnMasterDying);
            GameEventMgr.AddHandler(_master, GameObjectEvent.RemoveFromWorld, OnMasterRemoved);
            GameEventMgr.AddHandler(_master, GameNPCEvent.NPCReset, OnMasterReset);

            if (_spawned.Count > 0)
            {
                _animTimer = new System.Timers.Timer(1000) { AutoReset = true };
                _animTimer.Elapsed += AttackTick;
                _animTimer.Start();
            }
        }

        private void UnhookLifecycle()
        {
            GameEventMgr.RemoveHandler(_master, GameLivingEvent.Dying, OnMasterDying);
            GameEventMgr.RemoveHandler(_master, GameObjectEvent.RemoveFromWorld, OnMasterRemoved);
            GameEventMgr.RemoveHandler(_master, GameNPCEvent.NPCReset, OnMasterReset);

            if (_animTimer != null)
            {
                _animTimer.Stop();
                _animTimer.Dispose();
                _animTimer = null;
            }
        }

        private void AttackTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_spawned.Count == 0 || _master?.IsAlive != true) return;

            bool masterAttacks = _rnd.Next(8) > 4;
            GameObject attacker, defender;

            if (masterAttacks)
            {
                attacker = _master;
                defender = _spawned[_rnd.Next(_spawned.Count)];
                _master.TargetObject = defender;
                _master.TurnTo(defender);
            }
            else
            {
                attacker = _spawned[_rnd.Next(_spawned.Count)];
                defender = _master;
                _master.TargetObject = attacker;
                _master.TurnTo(attacker);
            }

            ushort weapon = _spec.WeaponAnim;
            ushort shield = _spec.ShieldAnim;
            int attackValue = _rnd.Next(45);
            int defenseValue = masterAttacks ? 11 : (_rnd.Next(2) == 0 ? 11 : _rnd.Next(11));

            foreach (GamePlayer player in _master.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendCombatAnimation(attacker, defender, weapon, shield, attackValue, 0, (byte)defenseValue, 100);
        }

        private void OnMasterReset(DOLEvent e, object sender, EventArgs args)
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                var t = _spawned[i];
                if (t?.IsAlive == true && t.ObjectState == GameObject.eObjectState.Active)
                {
                    var dest = ComputeSlotPosition(i, _spawned.Count, _chosenBaseRadius, _spec.RingSpacing);
                    t.PathTo(dest, (short)Math.Max(100, t.MaxSpeed / 2));
                }
            }
        }

        private void OnMasterDying(DOLEvent e, object sender, EventArgs args)
        {
            if (_spec?.DespawnOnDeath != true) return;
            DespawnAll();
        }

        private void OnMasterRemoved(DOLEvent e, object sender, EventArgs args)
        {
            DespawnAll();
            UnhookLifecycle();
        }

        private void DespawnAll()
        {
            foreach (var t in _spawned)
            {
                if (t == null) continue;

                if (t.ObjectState == GameObject.eObjectState.Active)
                    t.RemoveFromWorld();

                t.DeleteFromDatabase();
                t.Delete();
            }
            _spawned.Clear();
        }

        private void SpawnAll()
        {
            _chosenBaseRadius = (_spec.RadiusMin.HasValue && _spec.RadiusMax.HasValue)
                ? Util.Random(_spec.RadiusMin.Value, _spec.RadiusMax.Value)
                : _spec.Radius;

            int total = _spec.Entries.Sum(e => e.Count);
            int spawnedIdx = 0;

            foreach (var entry in _spec.Entries)
            {
                var tpl = NpcTemplateMgr.GetTemplate(entry.TemplateId);
                if (tpl == null)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"GuardTraineeController: NPCTemplate {entry.TemplateId} not found for master '{_master.Name}'");
                    continue;
                }

                for (int i = 0; i < entry.Count; i++, spawnedIdx++)
                {
                    var npc = new AmteMob
                    {
                        Realm = _master.Realm,
                        CurrentRegionID = _master.CurrentRegionID,

                        // start at master position
                        Position = _master.Position,
                        Home = _master.Position,
                        SpawnPosition = _master.Position,

                        PackageID = $"{_master.PackageID}#trainee",
                        LoadedFromScript = true
                    };

                    npc.LoadTemplate(tpl);
                    npc.Faction = _master.Faction;

                    // calm by default
                    if (npc.Brain is StandardMobBrain brain)
                    {
                        brain.AggroLevel = 0;
                        brain.AggroRange = 0;
                    }

                    // final placement
                    var dest = ComputeSlotPosition(spawnedIdx, total, _chosenBaseRadius, _spec.RingSpacing);
                    npc.Position = Position.Create(_master.CurrentRegionID, dest.X, dest.Y, dest.Z, _master.Position.Orientation.InHeading);
                    npc.Home = npc.Position;
                    npc.SpawnPosition = npc.Position;

                    // ensure trainee faces the master and is "locked on"
                    npc.TargetObject = _master;
                    npc.TurnTo(_master.Coordinate);

                    if (npc.AddToWorld())
                    {
                        if (_spec.Follow) npc.Follow(_master, 100, 1200);
                        _spawned.Add(npc);
                    }
                }
            }
        }

        /// <summary>
        /// Places N trainees around the master in concentric rings (8 per ring).
        /// </summary>
        private Coordinate ComputeSlotPosition(int index, int total, int baseRadius, int ringSpacing)
        {
            const int perRing = 8;
            int ring = index / perRing;
            int idxInRing = index % perRing;
            int radius = baseRadius + ring * ringSpacing;

            double angle = (Math.PI * 2.0) * (idxInRing / (double)perRing);
            int x = (int)(_master.Position.X + Math.Cos(angle) * radius);
            int y = (int)(_master.Position.Y + Math.Sin(angle) * radius);
            int z = (int)_master.Position.Z;

            return Coordinate.Create(x, y, z);
        }
    }

    /// <summary>
    /// Drop-in master NPC type. If PackageID parses, spawns and manages trainees.
    /// </summary>
    public class GuardMaster : GameNPC
    {
        private GuardTraineeController _trainees;

        public override bool AddToWorld()
        {
            var ok = base.AddToWorld();
            if (!ok) return false;

            if (!string.IsNullOrWhiteSpace(PackageID))
            {
                _trainees = new GuardTraineeController(this);
                _trainees.TrySpawnFromPackage();
            }

            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;
            Say(Language.LanguageMgr.GetTranslation(player.Client, "GuardTraineeController.Master.Busy", player.Name ?? "soldier"));
            return true;
        }
    }
}
