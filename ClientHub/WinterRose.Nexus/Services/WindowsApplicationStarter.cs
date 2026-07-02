using System;
using System.Diagnostics;
using System.IO;
using WinterRose.DependancyInjection;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

[WindowsOnly]
public class WindowsApplicationStarter : IApplicationLauncher
{
    public Process LaunchApplication(string appId, AppLaunchTarget launchTarget, params string[] extraArgs)
    {
        string fullPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "apps",
            appId,
            "app",
            launchTarget.Path);

        var startInfo = new ProcessStartInfo
        {
            FileName = fullPath,
            WorkingDirectory = Path.GetDirectoryName(fullPath)!,
            UseShellExecute = true
        };

        foreach (var arg in launchTarget.Arguments)
            startInfo.ArgumentList.Add(arg);
        
        foreach (var arg in extraArgs)
            startInfo.ArgumentList.Add(arg);

        return Process.Start(startInfo)!;
    }
}