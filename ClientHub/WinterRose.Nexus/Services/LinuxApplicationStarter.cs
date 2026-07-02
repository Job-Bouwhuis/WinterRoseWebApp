using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using WinterRose.DependancyInjection;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

[LinuxOnly]
public class LinuxApplicationStarter : IApplicationLauncher
{
    public Process LaunchApplication(string appId, AppLaunchTarget launchTarget, params string[] extraArgs)
    {
        string fullPath = Path.GetFullPath(launchTarget.Path);

        EnsureExecutable(fullPath);

        if (fullPath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                WorkingDirectory = Path.GetDirectoryName(fullPath)!,
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add(fullPath);

            foreach (var arg in launchTarget.Arguments)
                startInfo.ArgumentList.Add(arg);

            foreach (var arg in extraArgs)
                startInfo.ArgumentList.Add(arg);

            return Process.Start(startInfo)!;
        }

        var p = new ProcessStartInfo
        {
            FileName = fullPath,
            WorkingDirectory = Path.GetDirectoryName(fullPath)!,
            UseShellExecute = false
        };

        foreach (var arg in launchTarget.Arguments)
            p.ArgumentList.Add(arg);

        foreach (var arg in extraArgs)
            p.ArgumentList.Add(arg);

        return Process.Start(p)!;
    }

    private static void EnsureExecutable(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;

            var chmod = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                ArgumentList = { "+x", path },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            chmod?.WaitForExit(2000);
        }
        catch
        {
            // ignore permission enforcement failures
        }
    }
}