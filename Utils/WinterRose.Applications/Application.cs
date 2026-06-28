using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using WinterRose.ForgeThread;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Applications;

public abstract class Application : IApplication
{
    protected internal CancellationTokenSource cancelSource = new CancellationTokenSource();
    public IServiceProvider Services { get; set; }


    private ConcurrentQueue<Action> actions = [];
    
    
    private Task? runningTask;

    public bool IsRunning { get; private set; }

    public void Run()
    {
        Console.CancelKeyPress += (sender, args) => cancelSource.Cancel();
        
        InvokeStarting();
        IsRunning = true;

        try
        {
            while (!cancelSource.IsCancellationRequested)
            {
                InvokeActions();
                Tick(cancelSource.Token);
            }
        }
        finally
        {
            IsRunning = false;
            InvokeStopping();
        }
    }

    public Task RunAsync()
    {
        cancelSource = new CancellationTokenSource();

        runningTask = Task.Run(async () =>
        {
            InvokeStarting();
            IsRunning = true;

            try
            {
                while (!cancelSource.IsCancellationRequested)
                {
                    InvokeActions();
                    Tick(cancelSource.Token);
                }
            }
            finally
            {
                IsRunning = false;
                InvokeStopping();
            }
        });

        return runningTask;
    }

    public void Stop()
    {
        cancelSource?.Cancel();
    }

    public void Invoke(Action action)
    {
        actions.Enqueue(action);
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        throw new NotImplementedException();
    }

    private void InvokeActions()
    {
        int actionsHandled = 0;
        while (actions.TryDequeue(out var action))
        {
            action();
            actionsHandled++;
            if (actionsHandled > 1000)
                break;
        }
    }
    
    protected abstract void Tick(CancellationToken token);

    protected virtual void OnStarting() { }
    protected virtual void OnStopping() { }
    

    private void InvokeStarting() => OnStarting();
    private void InvokeStopping() => OnStopping();
}