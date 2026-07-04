using System;
using System.Collections.Generic;
using System.IO;
using WinterRose.Recordium;

namespace WinterRose.Nexus.Services.SelfUpdates;

public class SelfUpdateStarter
{
    private List<string> ignoredFiles =
    [
        "userprefs.wf"
    ];

    private List<string> ignoredDirectories =
    [
        "logs",
        "apps"
    ];

    private DirectoryInfo nexusTempInstallLocation =
        new DirectoryInfo(Path.Combine(Path.GetTempPath(), "WinterRoseNexus"));

    public void StartSelfUpdate(string[] originalArgs)
    {
        DirectoryInfo nexusDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        new Log("SelfUpdateStarter").Info(nexusDir.FullName);
        new Log("SelfUpdateStarter").Info(nexusTempInstallLocation.FullName);
        if (nexusDir.FullName[..^1] == nexusTempInstallLocation.FullName)
            return;
        
        CopyDirectory(nexusDir, nexusTempInstallLocation);
    }


    private void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        var dirs = source.GetDirectories();
        foreach (var dir in dirs)
        {
            if (!ignoredDirectories.Contains(dir.Name))
                CopyDirectory(dir, target.CreateSubdirectory(dir.Name));
        }

        var files = source.GetFiles();
        foreach (var file in files)
        {
            if (!ignoredFiles.Contains(file.Name))
                File.Copy(file.FullName, Path.Combine(target.FullName, file.Name), true);
        }
    }
}