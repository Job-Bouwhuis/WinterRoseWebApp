using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;
using WinterRose.Recordium;
using WinterRose.DependancyInjection.Logging;

namespace WinterRose.Applications;

public abstract class Application : IApplication
{
    protected internal CancellationTokenSource cancelSource = new CancellationTokenSource();
    public IServiceProvider Services { get; set; }
    private Task? runningTask;

    private bool disposed = false;
    
    public bool IsRunning => !cancelSource.IsCancellationRequested;

    public void RunTick()
    {
        Tick(cancelSource.Token);
    }
    
    public void Run()
    {
        Console.CancelKeyPress += (sender, args) =>
        {
            cancelSource.Cancel();
        };

        ILogger<Application> logger = Services.Resolve<ILogger<Application>>();
        
        OnStarting();

        try
        {
            while (!cancelSource.IsCancellationRequested)
                Tick(cancelSource.Token);
            
            logger.Info("Application stopping...");
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "An error occurred while running the application");
            Environment.ExitCode = 1;
        }
        OnStopping();
    }

    public Task RunAsync()
    {
        cancelSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, args) => cancelSource.Cancel();
        
        ILogger<Application> logger = Services.Resolve<ILogger<Application>>();
        
        runningTask = Task.Run(async () =>
        {
            OnStarting();

            try
            {
                while (!cancelSource.IsCancellationRequested)
                    Tick(cancelSource.Token);
                logger.Info("Application stopping...");
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "An error occurred while running the application");
                Environment.ExitCode = 1;
            }
            OnStopping();
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

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;
        disposed = true;
        
        ILogger<Application> logger = Services.Resolve<ILogger<Application>>();
        GC.SuppressFinalize(this);
        cancelSource.Cancel();
        runningTask?.Wait();
        Services.Dispose();
        
        logger.Info("Application stopped.");
    }
}