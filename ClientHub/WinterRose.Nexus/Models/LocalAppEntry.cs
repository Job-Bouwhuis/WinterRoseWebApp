using System;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Models;

/// <summary>
/// Represents an app installed locally on this machine. All internal
/// identification/lookups should use <see cref="AppId"/>; <see cref="DisplayName"/>
/// is for UI presentation only and may be stale until refreshed from the server.
/// </summary>
public class LocalAppEntry
{
    private LocalAppEntry() {} // serialization
    
    public LocalAppEntry(string appId, string displayName, AppVersion installedVersion, string installPath, bool pinVersion = false)
    {
        AppId = appId;
        DisplayName = displayName;
        InstalledVersion = installedVersion;
        InstallPath = installPath;
        PinVersion = pinVersion;
    }

    /// <summary>
    /// Stable identifier for this app. Matches <see cref="AppEntry.AppId"/>
    /// and the name of this app's folder under the apps root.
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// Human-friendly name for display in the UI. Sourced from
    /// <see cref="AppEntry.DisplayName"/> the last time it was known.
    /// Never used as a lookup key.
    /// </summary>
    public string DisplayName { get; set; }

    public AppVersion InstalledVersion { get; set; }

    public string InstallPath { get; set; }
    
    public DateTime? LastStartedAt { get; set; }
    
    /// <summary>
    /// When true, this app should not be auto-updated past <see cref="InstalledVersion"/>.
    /// </summary>
    public bool PinVersion { get; set; }
}
