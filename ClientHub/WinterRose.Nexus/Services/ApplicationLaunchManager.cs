using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

public class ApplicationLaunchManager : IApplicationLaunchManager
{
    internal class ProcessInfo
    {
        public required Process Process;
        public int notRespondingTicks;
        public static implicit operator ProcessInfo(Process p) => new() { Process = p };
    }

    private Dictionary<string, ProcessInfo> CurrentlyRunningProcesses = [];
    
    private readonly object processLock = new();

    private readonly IApplicationLauncher launcher;
    private readonly ILogger<ApplicationLaunchManager> logger;

    public ApplicationLaunchManager(
        IApplicationLauncher launcher, 
        IAsyncEventQueue<ApplicationShutdownEvent> shutdownEvent,
        CancellationToken ct,
        ILogger<ApplicationLaunchManager> logger)
    {
        this.launcher = launcher;
        this.logger = logger;
        MonitorTask(shutdownEvent, ct);
    }

    public List<ProcessReference> GetRunningProcesses()
    {
        lock (processLock)
        {
            return CurrentlyRunningProcesses
                .Select(kvp => new ProcessReference(kvp.Key, kvp.Value, this))
                .ToList();
        }
    }
    
    private void MonitorTask(
        IAsyncEventQueue<ApplicationShutdownEvent> shutdownEvent, 
        CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            List<string> toRemove = new();
            while (!ct.IsCancellationRequested)
            {
                foreach (var (appId, info) in CurrentlyRunningProcesses)
                {
                    if (info.Process.HasExited)
                    {
                        shutdownEvent.Publish(new ApplicationShutdownEvent(appId, info.Process));
                        toRemove.Add(appId);
                        continue;
                    }
                    

                    if (!info.Process.Responding)
                    {
                        if (info.notRespondingTicks == 5)
                        {
                            // TODO IN FUTURE: if app still freezes after 5 seconds, and the app is a .NET application,
                            // attatch debugger and extract thread stack trace info and send to Nexus servers
                            // so devs can know about major application freezes
                            // only done when user allows this in the nexus app preferences
                            info.notRespondingTicks = 0;
                        }
                        else
                            info.notRespondingTicks++;
                    }

                    if (info.Process.HasExited)
                    {
                        logger.Info($"Process {appId} has exited");
                        shutdownEvent.Publish(new ApplicationShutdownEvent(appId, info.Process));
                    }
                }
                
                foreach (var id in toRemove)
                    CurrentlyRunningProcesses.Remove(id);
                toRemove.Clear();

                await Task.Delay(1000);
            }
        }, ct);
    }

    public ProcessReference LaunchApplication(string appId, AppLaunchTarget launchTarget, params string[] args)
    {
        lock (processLock)
        {
            if (CurrentlyRunningProcesses.ContainsKey(appId))
                return new ProcessReference(appId, CurrentlyRunningProcesses[appId], this);
        }

        logger.Info($"Launching application {appId}");
        var process = launcher.LaunchApplication(appId, launchTarget, args);

        lock (processLock)
        {
            var info = (ProcessInfo)process;
            CurrentlyRunningProcesses[appId] = info;

            return new ProcessReference(appId, info, this);
        }
    }

    public void KillApplication(string appId)
    {
        Process? process;

        lock (processLock)
        {
            if (!CurrentlyRunningProcesses.TryGetValue(appId, out var info))
                return;

            process = info.Process;
        }

        try
        {
            if (process.HasExited)
                return;

            process.CloseMainWindow();

            if (!process.WaitForExit(15000))
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // ignore shutdown errors
        }
    }
}