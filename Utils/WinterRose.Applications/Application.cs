using System;
using System.Threading.Tasks;
using System.Threading;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Applications;

public abstract class Application : IApplication
{
    protected internal CancellationTokenSource cancelSource = new CancellationTokenSource();
    public IServiceProvider Services { get; set; }
    
    private Task? runningTask;

    public bool IsRunning { get; private set; }
    
    public void Run()
    {
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



    private void SetRunning(bool running)
    {
        IsRunning = running;
    }

    private void InvokeStarting() => OnStarting();
    private void InvokeStopping() => OnStopping();
}