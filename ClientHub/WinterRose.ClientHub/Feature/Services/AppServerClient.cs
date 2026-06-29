using System.Collections.Generic;
using System.IO;
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
    
    public async Task<Stream> GetStreamAsync(string url)
    {
        var response = await httpClient
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadAsStreamAsync()
            .ConfigureAwait(false);
    }
    
    public Task<Stream> GetDiffStreamAsync(
        string appName,
        AppVersion from,
        AppVersion to)
    {
        string url =
            $"apps/{appName}/diff?from={from.ToString(VersionStringFormat.FolderSafe)}&to={to.ToString(VersionStringFormat.FolderSafe)}";

        return GetStreamAsync(url);
    }
    
    public Task<Stream> GetVersionFileStreamAsync(
        string appName,
        AppVersion version,
        string relativePath)
    {
        string url =
            $"apps/{appName}/versions/{version.ToString(VersionStringFormat.FolderSafe)}/file?path={relativePath}";

        return GetStreamAsync(url);
    }
    
    public Task<Stream> GetVersionArchiveStreamAsync(
        string appName,
        AppVersion version)
    {
        string url =
            $"apps/{appName}/versions/{version.ToString(VersionStringFormat.FolderSafe)}/archive";

        return GetStreamAsync(url);
    }
}
