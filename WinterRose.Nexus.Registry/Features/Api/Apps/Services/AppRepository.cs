using System.Text.RegularExpressions;
using WinterRose.Nexus.Registry.Features.FileUploads.Services;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Registry.Features.Api.Apps.Services;

public partial class AppRepository
{
    private readonly DirectoryInfo uploadsFolder;
    private readonly AppDiffService appDiffService;

    public AppRepository(AppDiffService appDiffService)
    {
        this.appDiffService = appDiffService;

        uploadsFolder = FileUploadGlobals.uploadsFolder;
    }

    public Stream OpenVersionFile(
        string appName,
        AppVersion appVersion,
        string relativeFilePath)
    {
        string versionRoot = Path.Combine(
            uploadsFolder.FullName,
            appName,
            appVersion.ToString(VersionStringFormat.FolderSafe));

        if (!Directory.Exists(versionRoot))
            throw new DirectoryNotFoundException(
                $"Version '{appVersion}' of app '{appName}' not found.");

        string fullPath = Path.Combine(versionRoot, relativeFilePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"File '{relativeFilePath}' not found in version '{appVersion}'.");

        return new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }

    public Stream OpenVersionArchive(
        string appName,
        AppVersion appVersion)
    {
        string versionRoot = Path.Combine(
            uploadsFolder.FullName,
            appName,
            appVersion.ToString(VersionStringFormat.FolderSafe));

        if (!Directory.Exists(versionRoot))
            throw new DirectoryNotFoundException(
                $"Version '{appVersion}' of app '{appName}' not found.");

        MemoryStream output = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(
                   output,
                   System.IO.Compression.ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            foreach (string file in Directory.EnumerateFiles(
                         versionRoot,
                         "*",
                         SearchOption.AllDirectories))
            {
                string entryName = Path.GetRelativePath(versionRoot, file);

                var entry = archive.CreateEntry(entryName);

                using var entryStream = entry.Open();
                using var fileStream = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                fileStream.CopyTo(entryStream);
            }
        }

        output.Position = 0;
        return output;
    }


    public async Task<List<AppEntry>> GetAppEntries()
    {
        var nameDirs = Directory.EnumerateDirectories(uploadsFolder.FullName)
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
        AppVersion? latest = null;

        foreach (var subDir in Directory.EnumerateDirectories(appPath))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("latest", StringComparison.OrdinalIgnoreCase))
                continue;

            if (folderName.Equals("diffs", StringComparison.OrdinalIgnoreCase))
                continue;

            AppVersion current;

            try
            {
                current = new AppVersion(folderName);
            }
            catch
            {
                continue;
            }

            if (latest is null || current.CompareTo(latest) > 0)
                latest = current;
        }

        return latest?.ToString(VersionStringFormat.FolderSafe) ?? "0_0_0";
    }

    private AppEntry FetchUploadEntryFromPath(string appPath)
    {
        var uploadName = Path.GetFileName(appPath);

        var versions = new List<AppVersion>();
        var diffs = new List<DiffEntry>();

        foreach (var subDir in Directory.EnumerateDirectories(appPath))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("latest", StringComparison.OrdinalIgnoreCase))
                continue;

            if (folderName.Equals("diffs", StringComparison.OrdinalIgnoreCase))
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

            AppVersion appVersion;

            try
            {
                appVersion = new AppVersion(folderName);
            }
            catch
            {
                continue;
            }

            versions.Add(appVersion);
        }

        versions.Sort((a, b) => a.CompareTo(b));

        return new AppEntry
        {
            Name = uploadName,
            Versions = versions,
            Diffs = diffs,
        };
    }
}