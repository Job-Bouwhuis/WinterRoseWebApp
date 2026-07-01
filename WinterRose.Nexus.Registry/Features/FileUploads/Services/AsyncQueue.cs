using System.Threading.Channels;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public sealed class AsyncQueue<T> : IAsyncQueue<T>
{
    private readonly ConcurrentDictionary<Guid, Channel<T>> subscribers = new();

    public IDisposable Subscribe(
        Func<T, CancellationToken, Task> handler,
        CancellationToken externalCt)
    {
        var channel = Channel.CreateUnbounded<T>();

        var internalCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            internalCts.Token,
            externalCt);

        var id = Guid.NewGuid();
        subscribers.TryAdd(id, channel);

        var task = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(linkedCts.Token))
                {
                    await handler(item, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected shutdown
            }
            finally
            {
                subscribers.TryRemove(id, out _);
            }
        });

        return new SubscriptionHandle(
            task,
            () =>
            {
                internalCts.Cancel();
                channel.Writer.TryComplete();
            });
    }

    public IDisposable Subscribe(
        Func<T, Task> handler,
        CancellationToken externalCt)
    {
        return Subscribe((item, ct) => handler(item), externalCt);
    }

    public Task SubscribeAsync(
        Func<T, CancellationToken, Task> handler,
        CancellationToken externalCt)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var handle = Subscribe(async (item, ct) =>
        {
            await handler(item, ct);
        }, externalCt);

        externalCt.Register(() =>
        {
            handle.Dispose();
            tcs.TrySetResult();
        });

        return tcs.Task;
    }
    
    public Task SubscribeAsync(
        Func<T, Task> handler,
        CancellationToken externalCt) => SubscribeAsync((item, ct) => handler(item), externalCt);

    public void Publish(T evt)
    {
        foreach (var subscriber in subscribers.Values)
        {
            subscriber.Writer.TryWrite(evt);
        }
    }

    public sealed class SubscriptionHandle : IDisposable
    {
        private readonly Action dispose;

        public Task Completion { get; }

        public SubscriptionHandle(Task completion, Action dispose)
        {
            Completion = completion;
            this.dispose = dispose;
        }

        public void Dispose() => dispose();
    }
}
