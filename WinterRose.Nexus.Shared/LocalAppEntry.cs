namespace WinterRose.Nexus.Shared;

public class LocalAppEntry
{
    public LocalAppEntry(string name, AppVersion version, string installPath)
    {
        Name = name;
        Version = version;
        InstallPath = installPath;
    }

    public string Name { get; set; }
    public AppVersion Version { get; set; }
    public string InstallPath { get; set; }
}