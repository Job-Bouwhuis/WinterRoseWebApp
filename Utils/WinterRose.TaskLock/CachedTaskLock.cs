using System.Collections.Concurrent;

namespace WinterRose.TaskLock
{
    /// <summary>
    /// Wraps <see cref="TaskLock{TKey, TResult}"/> to additionally cache successful
    /// results beyond the lifetime of the in-flight request that produced them.
    /// <br/><br/>
    /// Without this wrapper, <see cref="TaskLock{TKey, TResult}"/> only de-duplicates
    /// work that is concurrently in flight: as soon as a task completes it's removed,
    /// so the next caller (even a millisecond later) triggers a brand new factory call.
    /// <see cref="CachedTaskLock{TKey, TResult}"/> keeps the completed result around so
    /// callers can skip regeneration entirely, until the entry has gone unused for
    /// longer than <see cref="IdleTimeout"/>, at which point it's evicted.
    /// </summary>
    /// <remarks>
    /// - Expiration is sliding/idle-based, not absolute: every successful access
    ///   (cache hit OR the completion of a fresh generation) refreshes the entry's
    ///   last-access time. An entry is only evicted once it has gone unused for the
    ///   full idle timeout.<br/>
    /// - Only successful results are cached. Failures/cancellations are never stored;
    ///   the next call after a failure will simply attempt generation again, same as
    ///   plain <see cref="TaskLock{TKey, TResult}"/>.<br/>
    /// - Concurrent callers for the same key while generation is in flight still only
    ///   trigger one factory invocation, via the underlying <see cref="TaskLock{TKey, TResult}"/>.<br/>
    /// - Thread-safe. Single-process, in-memory, matching <see cref="TaskLock{TKey, TResult}"/>.<br/>
    /// - Implements <see cref="IDisposable"/> because it owns a background sweep timer;
    ///   dispose it when you're done with the cache (e.g. on app shutdown).
    /// </remarks>
    /// <typeparam name="TKey">Identity of the unit of work / cache key.</typeparam>
    /// <typeparam name="TResult">What the work produces, and what gets cached.</typeparam>
    public sealed class CachedTaskLock<TKey, TResult> : IDisposable
        where TKey : notnull
    {
        private sealed class CacheEntry
        {
            public required TResult Value { get; init; }
            public long LastAccessedTicks; // Environment.TickCount64, written via Interlocked.Exchange

            public void Touch() => Interlocked.Exchange(ref LastAccessedTicks, Environment.TickCount64);

            public bool IsExpired(TimeSpan idleTimeout)
            {
                var elapsedMs = Environment.TickCount64 - Interlocked.Read(ref LastAccessedTicks);
                return elapsedMs >= idleTimeout.TotalMilliseconds;
            }
        }

        private readonly TaskLock<TKey, TResult> _taskLock = new();
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
        private readonly Timer _sweepTimer;
        private readonly object _disposeLock = new();
        private bool _disposed;

        /// <summary>
        /// How long a cache entry may go without being accessed before it becomes
        /// eligible for eviction. Sliding: any access (hit or a fresh completion)
        /// resets this clock for that entry.
        /// </summary>
        public TimeSpan IdleTimeout { get; }

        /// <summary>
        /// How often the background sweep checks for expired entries. Defaults to
        /// a fraction of <paramref name="idleTimeout"/> if not specified, so expiry
        /// is reasonably prompt without being a tight busy-loop for long timeouts.
        /// </summary>
        /// <param name="idleTimeout">Idle-time-since-last-access after which an entry is evicted.</param>
        /// <param name="sweepInterval">
        /// How often to scan for expired entries. If null, defaults to
        /// <paramref name="idleTimeout"/> / 4, clamped to [1s, 5min].
        /// </param>
        public CachedTaskLock(TimeSpan idleTimeout, TimeSpan? sweepInterval = null)
        {
            if (idleTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(idleTimeout), "Idle timeout must be positive.");

            IdleTimeout = idleTimeout;

            var interval = sweepInterval ?? ClampInterval(TimeSpan.FromTicks(idleTimeout.Ticks / 4));
            _sweepTimer = new Timer(_ => Sweep(), null, interval, interval);
        }

        private static TimeSpan ClampInterval(TimeSpan proposed)
        {
            var min = TimeSpan.FromSeconds(1);
            var max = TimeSpan.FromMinutes(5);
            if (proposed < min) return min;
            if (proposed > max) return max;
            return proposed;
        }

        /// <summary>
        /// Get the cached result for <paramref name="key"/> if present and not expired;
        /// otherwise generate it (de-duplicated against concurrent callers via the
        /// underlying <see cref="TaskLock{TKey, TResult}"/>), cache the result, and return it.
        /// </summary>
        /// <param name="key">Identity of the work / cache entry.</param>
        /// <param name="factory">Produces the result on a cache miss.</param>
        /// <param name="cancellationToken">
        /// See <see cref="TaskLock{TKey, TResult}.GetOrAddAsync"/> for cancellation semantics
        /// on the underlying in-flight de-duplication. Cancelling does not affect cache entries.
        /// </param>
        public async Task<TResult> GetOrAddAsync(
            TKey key,
            Func<CancellationToken, Task<TResult>> factory,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cache.TryGetValue(key, out var existing))
            {
                existing.Touch();
                return existing.Value;
            }

            var result = await _taskLock.GetOrAddAsync(key, factory, cancellationToken).ConfigureAwait(false);

            // Store (or refresh, if another caller raced us and already stored it —
            // AddOrUpdate resolves that deterministically without a double-store bug).
            var entry = _cache.AddOrUpdate(
                key,
                _ => new CacheEntry { Value = result },
                (_, existingEntry) => existingEntry);

            entry.Touch();
            return entry.Value;
        }

        /// <summary>
        /// Removes a specific entry from the cache immediately, regardless of its
        /// idle time. Does not affect any generation currently in flight for that key.
        /// </summary>
        public bool Invalidate(TKey key) => _cache.TryRemove(key, out _);

        /// <summary>Removes all cached entries immediately. Does not affect in-flight generations.</summary>
        public void Clear() => _cache.Clear();

        /// <summary>True if a non-expired cached value currently exists for this key. Informational/racy.</summary>
        public bool IsCached(TKey key) => _cache.ContainsKey(key);

        /// <summary>True if work is currently in flight (being generated) for this key. Informational/racy.</summary>
        public bool IsInFlight(TKey key) => _taskLock.IsInFlight(key);

        private void Sweep()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired(IdleTimeout))
                {
                    // Remove only if it's still the same entry instance we inspected —
                    // avoids evicting an entry that was just refreshed/replaced between
                    // the expiry check and the removal.
                    ((ICollection<KeyValuePair<TKey, CacheEntry>>)_cache).Remove(kvp);
                }
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            _sweepTimer.Dispose();
            _cache.Clear();
        }
    }
}
