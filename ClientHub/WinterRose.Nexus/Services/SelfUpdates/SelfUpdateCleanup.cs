using System.IO;

namespace WinterRose.Nexus.Services.SelfUpdates;

public class SelfUpdateCleanup
{
    private DirectoryInfo nexusTempInstallLocation =
        new DirectoryInfo(Path.Combine(Path.GetTempPath(), "WinterRoseNexus"));

    public void Clean()
    {
        if (nexusTempInstallLocation.Exists)
            Directory.Delete(nexusTempInstallLocation.FullName, true);
    }
}