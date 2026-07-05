using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WinterRose.Nexus.Exceptions;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

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

    public async Task<List<AppEntry>> GetAppEntriesAsync()
    {
        try
        {
            return await httpClient
                .GetFromWinterForge<List<AppEntry>>("apps")
                .ConfigureAwait(false) ?? new List<AppEntry>();
        }
        catch (HttpRequestException ex)
        {
            throw new ServerUnavailableException(ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ServerUnavailableException(ex);
        }
    }
    
    public async Task<AppEntry> GetAppEntryAsync(string appId)
    {
        AppEntry res = await httpClient
            .GetFromWinterForge<AppEntry>($"apps/{appId}")
            .ConfigureAwait(false);
        return res;
    }

    private async Task<Stream> GetStreamAsync(string url)
    {
        try
        {
            var response = await httpClient
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadAsStreamAsync()
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ServerUnavailableException(ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ServerUnavailableException(ex);
        }
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

    public Task<Stream> GetVersionArchiveStreamAsync(
        string appName,
        AppVersion version)
    {
        string url =
            $"apps/{appName}/versions/{version.ToString(VersionStringFormat.FolderSafe)}/archive";

        return GetStreamAsync(url);
    }

    public async Task<VersionedFile> GetVersionedFileStreamAsync(
        string appId,
        AppVersion version,
        string relativePath)
    {
        string baseUrl =
            $"apps/{appId}/versions/{version:VersionStringFormat.FolderSafe}/file";

        string encodedPath = System.Uri.EscapeDataString(relativePath);
        string hashUrl = $"{baseUrl}/hash/{encodedPath}";
        string streamUrl = $"{baseUrl}/stream/{encodedPath}";

        string hash = await httpClient.GetFromWinterForge<string>(hashUrl);

        Stream netFile = await GetStreamAsync(streamUrl);
        return new VersionedFile(hash, netFile);
    }
}
public sealed class VersionedFile(string hash, Stream s) : IDisposable
{
    public string Hash => hash;
    public Stream Stream => s;

    public void Dispose() => s.Dispose();
}