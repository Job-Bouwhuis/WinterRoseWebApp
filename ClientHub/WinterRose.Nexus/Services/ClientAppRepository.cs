using System;
using System.Collections.Generic;
using System.IO;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

public class ClientAppRepository
{
    private readonly string appsRoot;

    public ClientAppRepository()
    {
        appsRoot = Path.Combine(
            Environment.CurrentDirectory,
            "apps");
    }
    
    public List<LocalAppEntry> GetInstalledApps()
    {
        if (!Directory.Exists(appsRoot))
            return new List<LocalAppEntry>();

        var result = new List<LocalAppEntry>();

        foreach (var appDir in Directory.EnumerateDirectories(appsRoot))
        {
            string appName = Path.GetFileName(appDir);

            string versionFile = Path.Combine(appDir, "version.txt");

            AppVersion version = new("0_0_0");

            if (File.Exists(versionFile))
            {
                string versionText = File.ReadAllText(versionFile);
                version = new AppVersion(versionText);
            }

            result.Add(new LocalAppEntry(appName, version, Path.Combine(appDir, "app")));
        }

        return result;
    }
}