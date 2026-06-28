using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WinterRose.Web;
using WinterRose.WebServer.Features.FileUploads.Models;

namespace WinterRose.ClientHub.Feature.InformationRelay.Services;

public class AppServerClient
{
    private readonly HttpClient httpClient;

    public AppServerClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;

        this.httpClient.DefaultRequestHeaders.Accept.Clear();
        this.httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/winterforge")
        );
    }

    public async Task<List<AppSummary>> GetAppSummariesAsync()
    {
        return await httpClient
            .GetFromWinterForge<List<AppSummary>>("apps/summaries")
            .ConfigureAwait(false) ?? new List<AppSummary>();
    }

    public async Task<List<AppEntry>> GetAppEntriesAsync()
    {
        return await httpClient
            .GetFromWinterForge<List<AppEntry>>("apps")
            .ConfigureAwait(false) ?? new List<AppEntry>();
    }
}
