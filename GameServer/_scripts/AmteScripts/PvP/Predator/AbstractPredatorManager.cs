#nullable enable

using Discord.Net;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using log4net;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmteScripts.PvP
{
    public class PredatorPair(PvPEntity predator, PvPEntity? prey = null)
    {
        private const long REPEAT_KILL_COOLDOWN_MS = 30 * 1000;
        
        private PvPEntity? m_prey = prey;
        private PredatorPair? m_huntedBy;

        public PvPEntity Predator => predator;

        public override string ToString()
        {
            return KeyValuePair.Create(Predator, Prey).ToString();
        }

        public record KillRecord(PvPEntity Killer, long Timestamp);

        public PvPEntity? Prey
        {
            get => m_prey;
            set
            {
                if (m_prey != value)
                {
                    m_killRecords.Clear();
                }
                m_prey = value;
            }
        }

        public bool IsPredator(GameObject? player) { return player != null && Predator.GetPlayers().Contains(player); }

        public bool RecordKill(PvPEntity killer, string victim)
        {
            var time = GameServer.Instance.TickCount;
            bool added = true;
            m_killRecords.AddOrUpdate(
                killer.InternalID,
                _ => [new KillRecord(killer, time)],
                (_, list) =>
                {
                    if (list.Count > 0 && (list.Last().Timestamp + REPEAT_KILL_COOLDOWN_MS) > time)
                    {
                        added = false;
                    }
                    else
                    {
                        list.Add(new(killer, time));
                    }
                    return list;
                }
            );
            return added;
        }

        private readonly ConcurrentDictionary<string, List<KillRecord>> m_killRecords = new();
    }

    public abstract class AbstractPredatorManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!)!;

        private readonly Lock m_predatorsLock = new();
        private readonly List<PredatorPair> m_currentPredators = new();
        private readonly ReaderWriterDictionary<GamePlayer, PredatorPair> m_preyLookup = new();
        private readonly ReaderWriterDictionary<string, long> m_playerTimeouts = new();

        private readonly Lock m_queueLock = new();
        private readonly List<PvPEntity> m_queue = new();

        public void Start(IEnumerable<PvPEntity> players)
        {
            var pairs = AssignPairs(players, null);
            if (pairs.Count > 0)
            {
                var last = pairs.Last();
                var first = pairs.First();
                if (last.Predator.AssociatedGuild != first.Predator.AssociatedGuild || last.Predator.AssociatedGuild == null && first.Predator.AssociatedGuild == null)
                {
                    last.Prey = first.Predator;
                }
            }

            if (log.IsDebugEnabled)
            {
                log.Debug("Predator starting:\n\t" + string.Join("\n\t", pairs));
            }

            lock (m_predatorsLock)
            {
                m_preyLookup.Clear();
                m_currentPredators.Clear();
                m_currentPredators.AddRange(pairs);
                foreach (var pair in pairs)
                {
                    RegisterBounty(pair);
                }
                OnAssignNewPreys(pairs.Where(p => p.Prey != null));
            }
        }

        public void FillInPlayers(bool dequeue = true, bool reassignPreylessPredators = true)
        {
            FillInPlayers([], dequeue, reassignPreylessPredators);
        }

        public void FillInPlayers(IEnumerable<PvPEntity> toFill, bool dequeue = true, bool reassignPreylessPredators = true)
        {
            if (dequeue)
                toFill = Dequeue().Concat(toFill);

            lock (m_predatorsLock)
            {
                var newPlayers = toFill.Where(p => !m_currentPredators.Select(b => b.Predator).Contains(p)).ToList();
                var predators = newPlayers.Select(p => new PredatorPair(p)).ToList();
                var preys = m_currentPredators
                    .Select(b => b.Predator)
                    .Where(p => !m_currentPredators.Select(b => b.Prey).Contains(p))
                    .Concat(newPlayers)
                    .ToList();
                m_currentPredators.AddRange(predators);
                if (reassignPreylessPredators)
                {
                    var now = GameServer.Instance.TickCount;
                    predators = m_currentPredators.Where(b => b.Prey == null && m_playerTimeouts.GetValueOrDefault(b.Predator.InternalID) <= now).ToList();
                }

                log.DebugFormat("[Predator] Filling in {0} predators against {1} preys. ({2} new players)", predators.Count, preys.Count, newPlayers.Count);
                var updated = Recombobulate(predators, preys);
                OnAssignNewPreys(updated);
            }
        }

        protected virtual void RegisterBounty(PredatorPair bounty)
        {
            if (bounty.Prey != null)
            {
                foreach (GamePlayer player in bounty.Prey.GetPlayers())
                {
                    if (!m_preyLookup.TryAdd(player, bounty))
                    {
                        log.ErrorFormat("Predator {0} has a prey {1} that is already being hunted by {2} ; there is an error in the implementation. Removing prey from {0}", bounty.Predator, player, m_preyLookup[player]);
                        bounty.Prey = null;
                    }
                }
            }
        }

        public void Start()
        {
            Start(Dequeue());
        }

        public void Stop()
        {
            lock (m_queueLock)
            {
                lock (m_predatorsLock)
                {
                    FullCleanup();
                }
            }
        }

        private void FullCleanup()
        {
            foreach (PredatorPair bounty in m_currentPredators)
            {
                if (bounty.Prey is not null)
                {
                    var playerPrey = bounty.Prey.AsPlayer;
                    if (playerPrey == null)
                    {
                        log.ErrorFormat("Group preys are not currently supported");
                        continue;
                    }

                    CleanupPrey(playerPrey, bounty);
                }
            }
            m_currentPredators.Clear();
            m_preyLookup.Clear();
            m_queue.Clear();
        }

        public List<PredatorPair> GetBounties()
        {
            lock (m_predatorsLock)
            {
                return m_currentPredators.ToList();
            }
        }

        public PredatorPair? GetBountyForPredator(GamePlayer player)
        {
            lock (m_predatorsLock)
            {
                return m_currentPredators.FirstOrDefault(p => p.IsPredator(player));
            }
        }

        public PredatorPair? GetBountyForPrey(GamePlayer player)
        {
            lock (m_predatorsLock)
            {
                return m_preyLookup.GetValueOrDefault(player);
            }
        }

        public void RemoveFromQueue(GamePlayer player)
        {
            lock (m_queueLock)
            {
                var index = m_queue.FindIndex(p => p.AsPlayer == player);
                if (index != -1)
                    m_queue.RemoveAt(index);
            }
        }

        public virtual bool CanQueue(GamePlayer player, bool quiet = false)
        {
            if (m_playerTimeouts.TryGetValue(player.InternalID, out long abandonTick))
            {
                if (GameServer.Instance.TickCount < abandonTick)
                {
                    return false;
                }
            }

            if (IsActive(player))
                return false;

            return true;
        }

        public bool CanQueue(PvPEntity entity, bool quiet = false)
        {
            return entity.GetPlayers().All(p => CanQueue(p));
        }

        public void Abandon(GamePlayer player)
        {
            Abandon(player, Properties.PREDATOR_DESERTER_SECONDS);
        }

        public void SetPlayerTimeout(GamePlayer player, long timeoutSeconds)
        {
            if (timeoutSeconds > 0)
                m_playerTimeouts[player.InternalID] = GameServer.Instance.TickCount + timeoutSeconds * 1000;
        }

        public virtual void Abandon(GamePlayer player, long timeoutSeconds)
        {
            bool isActive = false;
            lock (m_predatorsLock)
            {
                var index = m_currentPredators.FindIndex(p => p.IsPredator(player));
                if (index != -1)
                {
                    if (OnPredatorAbandon?.Invoke(player, m_currentPredators[index]) is not false)
                    {
                        isActive = true;
                        m_currentPredators.RemoveAt(index);
                    }
                }

                if (m_preyLookup.TryRemove(player, out PredatorPair preyBounty))
                {
                    bool reinsert = false;
                    try
                    {
                        if (OnPreyAbandon?.Invoke(player, preyBounty) is false)
                            reinsert = true;
                    }
                    finally
                    {
                        if (reinsert)
                        {
                            m_preyLookup.Add(player, preyBounty);
                        }
                        else
                        {
                            isActive = true;
                            CleanupPrey(player, preyBounty);
                            OnAssignNewPreys([preyBounty]);
                        }
                    }
                }

                if (isActive)
                    SetPlayerTimeout(player, timeoutSeconds);
            }

            RemoveFromQueue(player);
        }

        /// <summary>
        /// Attempt to find a bounty associated with a predator and a prey.
        /// Calls OnPreyStolen if the killer isn't the predator for the prey,
        /// or calls OnPreyKilled if they are.
        /// </summary>
        /// <param name="killer"></param>
        /// <param name="victim"></param>
        /// <param name="killers"></param>
        /// <returns></returns>
        public virtual bool? CompleteBounty(GamePlayer? killer, GamePlayer victim, IList<GamePlayer>? killers = null)
        {
            if (killers is null)
                killers = killer == null ? [] : [killer]; // Maybe we'll handle this eventually

            if (!m_preyLookup.TryRemove(victim, out PredatorPair? bounty))
            {
                if (log.IsDebugEnabled)
                {
                    log.DebugFormat("[Predator] Dying player {0} was not found to be a prey", victim);
                    lock (m_currentPredators)
                    {
                        // Double check to be sure, when debug log is enabled...
                        bounty = m_currentPredators.FirstOrDefault(p => p.Prey?.GetPlayers().Contains(victim) == true);
                        if (bounty != null)
                            log.ErrorFormat("[Predator] Dying player {0} is a prey for {1}, but not part of prey lookup dictionary", bounty.Prey, bounty.Predator);
                    }
                }
                return null;
            }
            
            bool reinsert = true;
            try
            {
                if (!bounty.IsPredator(killer) && OnPreyStolen?.Invoke(victim, bounty, killers) is true)
                {
                    log.DebugFormat("[Predator] Prey {0} was stolen from predator {1} by {2}", victim, bounty.Predator, killer);
                    reinsert = false;
                    return false;
                }

                log.DebugFormat("[Predator] Prey {0} was killed by predator {1}", victim, bounty.Predator);
                if (OnPreyKilled?.Invoke(victim, bounty, killers) is true or null)
                {
                    reinsert = false;
                    return true;
                }
                return null;
            }
            finally
            {
                if (reinsert)
                {
                    log.DebugFormat("[Predator] Re-linking prey {0} with predator {1}", victim, bounty.Predator);
                    m_preyLookup.TryAdd(victim, bounty);
                }
                else
                {
                    log.DebugFormat("Cleaning up prey {0} with predator {1}", victim, bounty.Predator);
                    CleanupPrey(victim, bounty);
                }
            }
        }

        protected virtual void CleanupPrey(GamePlayer prey, PredatorPair bounty)
        {
            lock (m_predatorsLock)
            {
                m_preyLookup.Remove(prey);
                bounty.Prey = null;
            }
        }

        public delegate bool PreyKilledHandler(GamePlayer prey, PredatorPair predator, IList<GamePlayer> killers);

        public delegate bool PlayerEventHandler(GamePlayer player, PredatorPair bounty);

        public PreyKilledHandler? OnPreyStolen { get; set; }

        public PreyKilledHandler? OnPreyKilled { get; set; }

        public PlayerEventHandler? OnBountyAssigned { get; set; }

        public PlayerEventHandler? OnPreyAbandon { get; set; }

        public PlayerEventHandler? OnPredatorAbandon { get; set; }

        protected List<PredatorPair> AssignPairs(IEnumerable<PvPEntity> entities, PvPEntity? prey)
        {
            // Here what we do, is we find a predator suited for `prey`
            // We remove the predator from the list of available predators
            // And the predator becomes prey
            // To do this we reverse the list as an optimization to keep adding to the end
            List<PredatorPair> pairs = new();
            if (entities is ICollection collection)
                pairs.Capacity = collection.Count;

            var allPredators = entities.Reverse()
                .GroupBy(e => (object?)e.AssociatedGuild ?? e, (g, e) => KeyValuePair.Create(g, new Queue<PvPEntity>(e)))
                .ToList();
            PvPEntity? nextPredator = null;

            bool GetNextPredator()
            {
                // Dequeue from the guild with most players first. This ensures we spread guilds as much as possible
                // and end up with the minimum amount of leftover players.
                // This has the side effect of moving some players from big guilds to the end of the queue
                // Which in PvP sessions, means that we change the priority order normally based on score...
                var bucket = allPredators
                    .Where(e => e.Key != prey && e.Key != prey?.AssociatedGuild)
                    .Select(e => e.Value)
                    .Cast<Queue<PvPEntity>?>() // https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.maxby?view=net-10.0#remarks
                    .MaxBy(e => e!.Count);

                if (bucket is not { Count: > 0 })
                    return false;

                nextPredator = bucket.Dequeue();
                return true;
            }

            while (GetNextPredator())
            {
                Debug.Assert(nextPredator != null);
                pairs.Add(new PredatorPair(nextPredator, prey));
                prey = nextPredator;
            }

            foreach (var bucket in allPredators.Select(e => e.Value))
            {
                while (bucket.TryDequeue(out PvPEntity? leftover))
                {
                    pairs.Add(new PredatorPair(leftover));
                }
            }

            pairs.Reverse();
            return pairs;
        }

        class Bucket(object? key)
        {
            public object? Key => key;

            public int SelectedCount
            {
                get;
                set;
            }
            
            public Queue<PredatorPair>? Predators { get; set; }
            public Queue<PvPEntity>? Preys { get; set; }
        }

        /// <summary>
        /// Re-assigns preys to predators.
        /// </summary>
        /// <param name="preylessPredators">Predators without a prey</param>
        /// <param name="predatorlessPreys">Preys without a predator</param>
        /// <returns>A list of predators who actually had their prey reassigned.</returns>
        protected virtual List<PredatorPair> Recombobulate(IEnumerable<PredatorPair> preylessPredators, IEnumerable<PvPEntity> predatorlessPreys)
        {
            List<Bucket> buckets = new();
            if (preylessPredators is ICollection coll)
                buckets.Capacity = coll.Count;
            Bucket GetOrCreateBucket(object key)
            {
                Bucket? b = buckets.Find(b => b.Key == key);
                if (b is null)
                {
                    b = new Bucket(key);
                    buckets.Add(b);
                }
                return b;
            }

            foreach (var group in preylessPredators.GroupBy(e => (object?)e.Predator.AssociatedGuild ?? e.Predator))
            {
                GetOrCreateBucket(group.Key).Predators = new Queue<PredatorPair>(group);
            }

            foreach (var group in predatorlessPreys.GroupBy(e => (object?)e.AssociatedGuild ?? e))
            {
                GetOrCreateBucket(group.Key).Preys = new Queue<PvPEntity>(group);
            }
            
            List<PredatorPair> ret = new(buckets.Count);
            PredatorPair? pair = null;
            bool ComputeNext()
            {
                var predatorBucket = buckets.MaxBy(b => b.Predators?.Count ?? 0);
                if (predatorBucket is not { Predators.Count: > 0 })
                    return false;

                var predator = predatorBucket.Predators.Dequeue();
                ++predatorBucket.SelectedCount;
                var preyBucket = buckets
                    .Where(e => e.Key != predator.Predator && e.Key != predator.Predator.AssociatedGuild)
                    .Where(b => b.Preys is { Count: >0 })
                    .MaxBy(b => b.Preys!.Count - b.SelectedCount);
                
                pair = predator;
                if (preyBucket is null)
                    return false;

                pair.Prey = preyBucket.Preys!.Dequeue();
                return true;
            }

            while (ComputeNext())
            {
                ret.Add(pair!);
            }
            return ret;
        }

        protected virtual IEnumerable<PredatorPair> Stitch(int currentIndex, bool dequeue)
        {
            /*
            Debug.Assert(Monitor.IsEntered(m_predatorsLock));

            var current = m_currentPredators[currentIndex];
            if (dequeue)
            {
                var dequeued = Dequeue();
                PvPEntity? next = m_currentPredators.Count <= 1 ? null : m_currentPredators[(currentIndex + 1) % m_currentPredators.Count].Predator;
                var (assigned, leftover) = AssignPairs(dequeued, current, next);
            }
            else
            {

            }
            return m_currentPredators.Skip(currentIndex).Take(toInsertCount + 1);
            */
            return [];
        }

        protected virtual IEnumerable<PredatorPair> RemovePrey(int predatorIndex, bool dequeue)
        {
            var removingIndex = (predatorIndex + 1) % m_currentPredators.Count;
            m_currentPredators.RemoveAt(removingIndex);
            return Stitch(predatorIndex, dequeue);
        }

        protected virtual void OnAssignNewPreys(IEnumerable<PredatorPair> changes)
        {
            var handler = OnBountyAssigned;
            if (handler == null)
                return;

            changes.Foreach(p => p.Predator.GetPlayers().ForEach(pl => handler.Invoke(pl, p)));
        }

        protected static IEnumerable<KeyValuePair<string, Queue<PvPEntity>>> SortGuilds(IEnumerable<PvPEntity> entities)
        {
            Dictionary<string, Queue<PvPEntity>> queues = new();

            foreach (var entity in entities)
            {
                var key = entity.AssociatedGuild?.GuildID ?? string.Empty;
                if (!queues.TryGetValue(key, out var list))
                {
                    list = new([entity]);
                    queues[key] = list;
                }
                else
                {
                    list.Enqueue(entity);
                }
            }
            return queues;
        }

        protected List<PvPEntity> Dequeue()
        {
            List<PvPEntity> values = new(m_queue.Count);
            lock (m_queueLock)
            {
                values.AddRange(m_queue);
                m_queue.Clear();
            }
            return values;
        }

        public void Queue(PvPEntity entity, bool quiet = true, bool force = false)
        {
            lock (m_queueLock)
            {
                if (!force && !CanQueue(entity, quiet))
                    return;

                m_queue.Add(entity);
            }
        }

        public void Queue(GamePlayer player, bool quiet = false, bool force = false)
        {
            Queue(new PvPPlayerEntity(player), quiet, force);
        }

        protected bool Remove(PvPEntity entity)
        {
            IEnumerable<PredatorPair> changed = [];
            bool removed = false;
            lock (m_predatorsLock)
            {
                for (int i = 0; i < m_currentPredators.Count; ++i)
                {
                    PredatorPair current = m_currentPredators[i];
                    if (current.Prey == entity)
                    {
                        //changed = RemovePrey(i);
                        removed = true;
                    }
                }
            }
            OnAssignNewPreys(changed);
            return removed;
        }

        public bool IsActive(GamePlayer player)
        {
            return GetBountyForPrey(player) != null || GetBountyForPredator(player) != null;
        }

        public bool IsActive(PvPEntity player)
        {
            if (player is not PvPPlayerEntity)
            {
                log.Error("[Predator] Guild/group pvp entities are not implemented");
                return false;
            }
            var asPlayer = player.AsPlayer;
            return asPlayer != null && IsActive(asPlayer);
        }

        public virtual bool Active => m_currentPredators.Count > 0;
    }
}
