using System.Collections.Concurrent;

namespace WinterRose.TaskLock
{
    /// <summary>
    /// Generic "single generator, multiple readers" coordinator.
    ///<br/><br/>
    /// If multiple callers request work for the same key concurrently, only one
    /// factory invocation actually runs; all callers await the same Task and get
    /// the same result. This is intentionally domain-agnostic (no knowledge of
    /// diffs, files, etc.) so it can be reused anywhere you need to de-duplicate
    /// concurrent in-flight work keyed by some identity.
    /// </summary>
    /// <remarks>
    /// - Entries are removed from the in-flight table as soon as the task
    ///    completes (success, failure, or cancellation), so a subsequent call
    ///    for the same key will retry rather than replay a stale failure.<br/>
    ///  - Thread-safe. Single-process only by design (in-memory), matching the
    ///    stated requirement; nothing here coordinates across processes.
    /// </remarks>
    /// <typeparam name="TKey">Identity of the unit of work (must be a good dictionary key: implement equality/hashcode sensibly, e.g. a record or string).</typeparam>
    /// <typeparam name="TResult">What the work produces.</typeparam>
    public sealed class TaskLock<TKey, TResult>
        where TKey : notnull
    {
        // Lazy<Task<T>> is the key trick: ConcurrentDictionary.GetOrAdd can invoke
        // the value-factory more than once under contention (it's not atomic with
        // respect to the factory call), but Lazy with ExecutionAndPublication mode
        // guarantees the *inner* factory (our async work) only ever runs once even
        // if GetOrAdd's factory runs multiple times for the same key.
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TResult>>> _inFlight = new();

        /// <summary>
        /// Get the result for <paramref name="key"/>. If work for this key is already
        /// running, awaits and returns that same task's result instead of starting new work.
        /// Otherwise starts <paramref name="factory"/> and registers it for others to join.
        /// </summary>
        /// <param name="key">Identity of the work.</param>
        /// <param name="factory">
        /// Produces the result. Receives a CancellationToken that is only triggered by
        /// <paramref name="cancellationToken"/> passed by the FIRST caller to actually
        /// start the work (see remarks on cancellation below).
        /// </param>
        /// <param name="cancellationToken">
        /// Cancels this caller's wait. IMPORTANT: if this caller is the one who started
        /// the underlying work (i.e. it was not already in flight), cancelling also
        /// cancels the underlying work itself, which affects any other callers who joined it.
        /// If this caller merely joined work someone else started, cancelling this token
        /// only stops *this caller* from waiting further; the underlying work continues
        /// for other joined callers.
        /// </param>
        public Task<TResult> GetOrAddAsync(
            TKey key,
            Func<CancellationToken, Task<TResult>> factory,
            CancellationToken cancellationToken = default)
        {
            Lazy<Task<TResult>> lazy = null!;
            var cts = new CancellationTokenSource();

            lazy = _inFlight.GetOrAdd(key, _ => new Lazy<Task<TResult>>(
                () => RunAndCleanup(key, factory, cts),
                LazyThreadSafetyMode.ExecutionAndPublication));

            // If we didn't win the race to create the entry, our own CTS is unused garbage
            // and that's fine — the winner's CTS is the one wired into the running task.
            var task = lazy.Value;

            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            // Wrap so THIS caller can bail out of waiting without necessarily killing
            // the shared work for other callers.
            return WaitWithCallerCancellation(task, cancellationToken);
        }

        private async Task<TResult> RunAndCleanup(
            TKey key,
            Func<CancellationToken, Task<TResult>> factory,
            CancellationTokenSource cts)
        {
            try
            {
                return await factory(cts.Token).ConfigureAwait(false);
            }
            finally
            {
                // Remove regardless of outcome so a future call retries fresh.
                _inFlight.TryRemove(key, out _);
                cts.Dispose();
            }
        }

        private static async Task<TResult> WaitWithCallerCancellation(
            Task<TResult> sharedTask,
            CancellationToken callerToken)
        {
            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var _ = callerToken.Register(() => tcs.TrySetCanceled(callerToken)).ConfigureAwaitDisposable();

            var completed = await Task.WhenAny(sharedTask, tcs.Task).ConfigureAwait(false);
            return await completed.ConfigureAwait(false);
        }

        /// <summary>True if work is currently in flight for this key. Racy by nature (informational only).</summary>
        public bool IsInFlight(TKey key) => _inFlight.ContainsKey(key);
    }

    internal static class CancellationRegistrationExtensions
    {
        // Small shim so we can 'await using' a CancellationTokenRegistration uniformly
        // across older/newer TFMs without worrying about IAsyncDisposable support.
        public static AsyncDisposableWrapper ConfigureAwaitDisposable(this CancellationTokenRegistration registration)
            => new(registration);

        public readonly struct AsyncDisposableWrapper : IAsyncDisposable
        {
            private readonly CancellationTokenRegistration _registration;
            public AsyncDisposableWrapper(CancellationTokenRegistration registration) => _registration = registration;
            public ValueTask DisposeAsync()
            {
                _registration.Dispose();
                return default;
            }
        }
    }
}
