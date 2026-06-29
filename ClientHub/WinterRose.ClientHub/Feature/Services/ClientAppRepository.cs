using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WinterRose.ClientHub.Shared;
using WinterRose.ProgressKeeping;
using WinterRose.WebServer.Features.FileUploads.Models;

namespace WinterRose.ClientHub.Feature.InformationRelay.Services;

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