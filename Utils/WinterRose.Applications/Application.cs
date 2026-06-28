using System;
using System.Threading.Tasks;
using System.Threading;

namespace WinterRose.Applications;

public abstract class Application
{
    private CancellationTokenSource? cancelSource;
    private Task? runningTask;

    public void Run()
    {
        cancelSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, args) => cancelSource.Cancel();
        
        InvokeStarting();
        SetRunning(true);

        try
        {
            Execute(cancelSource.Token).GetAwaiter().GetResult();
        }
        finally
        {
            SetRunning(false);
            InvokeStopping();
        }
    }

    public Task RunAsync()
    {
        cancelSource = new CancellationTokenSource();

        runningTask = Task.Run(async () =>
        {
            InvokeStarting();
            SetRunning(true);

            try
            {
                await Execute(cancelSource.Token);
            }
            finally
            {
                SetRunning(false);
                InvokeStopping();
            }
        });

        return runningTask;
    }

    public void Stop()
    {
        cancelSource?.Cancel();
    }

    protected abstract Task Execute(CancellationToken token);

    protected virtual void OnStarting() { }
    protected virtual void OnStopping() { }

    protected bool IsRunning { get; private set; }

    private void SetRunning(bool running)
    {
        IsRunning = running;
    }

    private void InvokeStarting() => OnStarting();
    private void InvokeStopping() => OnStopping();
}