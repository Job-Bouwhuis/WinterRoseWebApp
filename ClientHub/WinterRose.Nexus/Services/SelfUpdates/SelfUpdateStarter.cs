using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using GLib;
using WinterRose.CommandLine;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Recordium;

namespace WinterRose.Nexus.Services.SelfUpdates;

public class SelfUpdateStarter(ILogger<SelfUpdateStarter> logger)
{
    private DirectoryInfo nexusTempInstallLocation =
        new DirectoryInfo(Path.Combine(Path.GetTempPath(), "WinterRoseNexus"));

    public void StartSelfUpdate(string[] originalArgs)
    {
        DirectoryInfo nexusDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory));
        if (nexusDir.FullName[..^1] == nexusTempInstallLocation.FullName)
            return;

        CopyDirectory(nexusDir, nexusTempInstallLocation);

        string originalExecutablePath = Environment.ProcessPath!;

        ProgramArgumentStringBuilder argBuilder = new ProgramArgumentStringBuilder();
        argBuilder.AddFlag("self-update");
        argBuilder.AddLongValue("original-path", originalExecutablePath);
        argBuilder.AddLongValue("processId", Environment.ProcessId.ToString());
        argBuilder.AddForward("forward", originalArgs);

        string originalExecutableName = Path.GetFileName(originalExecutablePath);
        string newExecutablePath = Path.Combine(nexusTempInstallLocation.FullName, originalExecutableName);

        ProcessStartInfo startInfo = new(newExecutablePath);
        argBuilder.Build(startInfo);

        var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
            logger.Error("Failed to start self-update");
        else
            logger.Info("Started Nexus clone with process ID " + process.Id);
    }


    private void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        var dirs = source.GetDirectories();
        foreach (var dir in dirs)
        {
            if (!NexusClient.IgnoredDirectories.Contains(dir.Name))
                CopyDirectory(dir, target.CreateSubdirectory(dir.Name));
        }

        var files = source.GetFiles();
        foreach (var file in files)
        {
            if (!NexusClient.IgnoredFiles.Contains(file.Name))
                File.Copy(file.FullName, Path.Combine(target.FullName, file.Name), true);
        }
    }
}