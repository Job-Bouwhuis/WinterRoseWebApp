using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WinterRose.Nexus.Interface;

using System.Collections.Concurrent;

public class MainThread
{
    private readonly ConcurrentQueue<Action> actionQueue = new();

    private readonly ConcurrentQueue<TaskCompletionSource<object?>> asyncQueue = new();

    public void Invoke(Action action)
    {
        actionQueue.Enqueue(action);
    }

    public Task InvokeAsync(Action action)
    {
        var tcs = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        actionQueue.Enqueue(() =>
        {
            try
            {
                action();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        actionQueue.Enqueue(() =>
        {
            try
            {
                var result = func();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public void ProcessActions(int maxProcessed = 100)
    {
        int processed = 0;

        while (processed < maxProcessed)
        {
            if (actionQueue.TryDequeue(out Action action))
            {
                action();
                processed++;
            }
            else
            {
                break;
            }
        }
    }
}