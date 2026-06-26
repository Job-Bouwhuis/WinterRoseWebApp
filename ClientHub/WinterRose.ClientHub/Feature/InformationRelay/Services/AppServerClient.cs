using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRose.ClientHub.Feature.InformationRelay.Services;

public class AppServerClient
{
    private readonly HttpClient httpClient;

    public AppServerClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<AppSummary>> GetAppSummariesAsync()
    {
        return await httpClient
            .GetFromJsonAsync<List<AppSummary>>("apps/summaries")
            .ConfigureAwait(false) ?? new List<AppSummary>();
    }

    public async Task<List<AppEntry>> GetAppEntriesAsync()
    {
        return await httpClient
            .GetFromJsonAsync<List<AppEntry>>("apps")
            .ConfigureAwait(false) ?? new List<AppEntry>();
    }
}
