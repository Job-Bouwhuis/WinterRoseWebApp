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
    private Task? runningTask;

    public bool IsRunning { get; private set; }

    public void RunTock()
    {
        Tick(cancelSource.Token);
    }
    
    public void Run()
    {
        Console.CancelKeyPress += (sender, args) => cancelSource.Cancel();
        
        InvokeStarting();
        IsRunning = true;

        try
        {
            while (!cancelSource.IsCancellationRequested)
            {
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
    
    protected abstract void Tick(CancellationToken token);

    protected virtual void OnStarting() { }
    protected virtual void OnStopping() { }
    

    private void InvokeStarting() => OnStarting();
    private void InvokeStopping() => OnStopping();
}