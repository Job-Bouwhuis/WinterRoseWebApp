using System.Text.RegularExpressions;
using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRoseWebApp.Features.Api.Apps.Services;

public partial class AppRepository
{
    const string UPLOADS_FOLDER_NAME = "Uploads";
    private readonly DirectoryInfo uploadsFolder;
    private string UploadsFolderPath { get; } = Path.Combine(AppContext.BaseDirectory, UPLOADS_FOLDER_NAME);

    public AppRepository()
    {
        if (!Directory.Exists(UploadsFolderPath))
            Directory.CreateDirectory(UploadsFolderPath);
        uploadsFolder = new DirectoryInfo(UploadsFolderPath);
    }

    public AppEntry GetAppEntry(string appName)
    {
        var appPath = Path.Combine(UploadsFolderPath, appName);

        if (!Directory.Exists(appPath))
            throw new DirectoryNotFoundException($"App '{appName}' not found");

        return FetchUploadEntryFromPath(appPath);
    }

    public List<AppSummary> GetAppSummaries()
    {
        var nameDirs = Directory.EnumerateDirectories(UploadsFolderPath)
            .OrderBy(d => d);

        var result = new List<AppSummary>();

        foreach (var appDir in nameDirs)
        {
            var appName = Path.GetFileName(appDir);

            var latestVersion = GetLatestVersionFromAppPath(appDir);

            result.Add(new AppSummary(appName, latestVersion));
        }

        return result;
    }
    public AppSummary GetAppSummary(string appName)
    {
        var appPath = Path.Combine(UploadsFolderPath, appName);

        if (!Directory.Exists(appPath))
            throw new DirectoryNotFoundException($"App '{appName}' not found");

        var latestVersion = GetLatestVersionFromAppPath(appPath);

        return new AppSummary(appName, latestVersion);
    }

    public async Task<List<AppEntry>> GetAppEntries()
    {
        var nameDirs = Directory.EnumerateDirectories(UploadsFolderPath)
            .OrderBy(d => d)
            .ToList();

        var result = new AppEntry[nameDirs.Count];

        using var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

        var tasks = nameDirs.Select(async (nameDir, index) =>
        {
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                result[index] = FetchUploadEntryFromPath(nameDir);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return result.Where(r => r != null).ToList();
    }



    private string GetLatestVersionFromAppPath(string appPath)
    {
        string latest = null;

        foreach (var subDir in Directory.EnumerateDirectories(appPath))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("latest", StringComparison.OrdinalIgnoreCase))
                continue;

            if (folderName.Equals("Diffs", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsVersionFolder(folderName))
                continue;

            if (latest == null || CompareVersionLabels(folderName, latest) > 0)
                latest = folderName;
        }

        return latest ?? "0_0_0";
    }

    private AppEntry FetchUploadEntryFromPath(string appName)
    {
        var uploadName = Path.GetFileName(appName);

        var versions = new List<VersionEntry>();
        var diffs = new List<DiffEntry>();

        foreach (var subDir in Directory.EnumerateDirectories(appName))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("latest", StringComparison.OrdinalIgnoreCase))
                continue;

            if (folderName.Equals("Diffs", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var diffFile in Directory.EnumerateFiles(subDir))
                {
                    var fileName = Path.GetFileName(diffFile);
                    var toIndex = fileName.IndexOf("_TO_", StringComparison.OrdinalIgnoreCase);
                    if (toIndex < 0) continue;

                    var from = fileName[..toIndex];
                    var to = fileName[(toIndex + 4)..];

                    diffs.Add(new DiffEntry
                    {
                        FromVersion = from,
                        ToVersion = to,
                        FilePath = diffFile,
                        FileName = fileName,
                    });
                }

                continue;
            }

            if (IsVersionFolder(folderName))
            {
                var uploadedAt = Directory.GetCreationTimeUtc(subDir);

                versions.Add(new VersionEntry
                {
                    VersionLabel = folderName,
                    UploadedAt = uploadedAt,
                });
            }
        }

        versions.Sort((a, b) => CompareVersionLabels(a.VersionLabel, b.VersionLabel));

        return new AppEntry
        {
            Name = uploadName,
            Versions = versions,
            Diffs = diffs,
        };
    }

    private static bool IsVersionFolder(string name) => VersionNamePattern().IsMatch(name);

    private static int CompareVersionLabels(string a, string b)
    {
        static int[] Parse(string v) =>
            v.Split('_').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        var pa = Parse(a);
        var pb = Parse(b);
        var len = Math.Max(pa.Length, pb.Length);

        for (int i = 0; i < len; i++)
        {
            var ai = i < pa.Length ? pa[i] : 0;
            var bi = i < pb.Length ? pb[i] : 0;
            if (ai != bi) return ai.CompareTo(bi);
        }

        return 0;
    }
    [GeneratedRegex(@"^\d+(_\d+)+$")]
    private static partial Regex VersionNamePattern();
}
