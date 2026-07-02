using System;
using System.Collections.Generic;
using System.IO;
using WinterRose.Nexus.Models;
using WinterRose.Nexus.Shared;
using WinterRose.WinterForgeSerializing;
using LocalAppEntry = WinterRose.Nexus.Models.LocalAppEntry;

namespace WinterRose.Nexus.Services;

public class ClientAppRepository
{
    private readonly string appsRoot;

    public const string LocalAppDetailsFileName = ".localappdetails";

    public ClientAppRepository()
    {
        appsRoot = Path.Combine(
            Environment.CurrentDirectory,
            "apps");
    }

    /// <summary>
    /// Absolute path to the root folder for a given app, e.g. apps/{appId}/
    /// </summary>
    public string GetAppRoot(string appId) => Path.Combine(appsRoot, appId);

    /// <summary>
    /// Absolute path to the installed application files for a given app,
    /// e.g. apps/{appId}/app/
    /// </summary>
    public string GetAppFilesPath(string appId) => Path.Combine(appsRoot, appId, "app");

    /// <summary>
    /// Absolute path to the .localappdetails file for a given app.
    /// </summary>
    public string GetLocalAppDetailsPath(string appId) =>
        Path.Combine(appsRoot, appId, LocalAppDetailsFileName);

    public List<LocalAppEntry> GetInstalledApps()
    {
        var result = new List<LocalAppEntry>();

        if (!Directory.Exists(appsRoot))
            return result;

        foreach (var appDir in Directory.EnumerateDirectories(appsRoot))
        {
            string appId = Path.GetFileName(appDir);

            LocalAppEntry? details = TryReadLocalAppDetails(appId);

            if (details is null)
                // No/corrupt .localappdetails -- not a valid install, skip it
                // rather than guessing at a version.
                continue;

            result.Add(new LocalAppEntry(
                appId: details.AppId,
                displayName: details.DisplayName,
                installedVersion: details.InstalledVersion,
                installPath: GetAppFilesPath(appId),
                pinVersion: details.PinVersion));
        }

        return result;
    }

    /// <summary>
    /// Reads a single app's local install details, or null if not installed
    /// / the file is missing or unreadable.
    /// </summary>
    public LocalAppEntry? TryReadLocalAppDetails(string appId)
    {
        string path = GetLocalAppDetailsPath(appId);

        if (!File.Exists(path))
            return null;

        try
        {
            return WinterForge.DeserializeFromHumanReadableFile<LocalAppEntry>(path);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes/overwrites the .localappdetails file for an app.
    /// </summary>
    public void SaveLocalAppDetails(LocalAppEntry details)
    {
        string appRoot = GetAppRoot(details.AppId);
        Directory.CreateDirectory(appRoot);

        string path = GetLocalAppDetailsPath(details.AppId);
        WinterForge.SerializeToFile(details, path, TargetFormat.FormattedHumanReadable);
    }

    public bool IsInstalled(string appId) => File.Exists(GetLocalAppDetailsPath(appId));
}
