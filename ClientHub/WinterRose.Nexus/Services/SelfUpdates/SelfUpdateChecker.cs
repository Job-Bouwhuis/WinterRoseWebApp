using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WinterRose.Nexus.Shared;
using WinterRose.Nexus.Utils;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.Services.SelfUpdates;

public class SelfUpdateChecker(AppServerClient server)
{
    
    
    public bool Check()
    {
        return CheckAsync().GetAwaiter().GetResult();
    }

    private async Task<bool> CheckAsync()
    {
        try
        {
            AppEntry entry = await server.GetAppEntryAsync(NexusClient.NexusAppId).ConfigureAwait(false);

            if (!File.Exists("NexusVersion.wf"))
                return true;
        
            object? versionResult = WinterForge.DeserializeFromHumanReadableFile("NexusVersion.wf");
            if (versionResult is not LocalAppEntry localVersion)
                return true;

            AppVersion? latest = entry.Versions.GetLatest(localVersion.InstalledVersion.Tag);
            if (latest is null)
                return true;
            
            return latest > localVersion.InstalledVersion;
        }
        catch (HttpRequestException e)
        {
            return false;
        }
    }
}