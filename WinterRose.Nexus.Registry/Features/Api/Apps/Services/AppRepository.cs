using System.Text.RegularExpressions;
using WinterRose.Nexus.Registry.Features.FileUploads.Services;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.Registry.Features.Api.Apps.Services;

public class AppRepository
{
    private readonly DirectoryInfo uploadsFolder;

    public AppRepository()
    {
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
            "versions",
            appVersion.ToString(VersionStringFormat.FolderSafe),
            "files");

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
            "versions",
            appVersion.ToString(VersionStringFormat.FolderSafe),
            "files");

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
    
    public AppEntry GetAppEntry(string appId)
    {
        var appPath = Path.Combine(uploadsFolder.FullName, appId);

        return Directory.Exists(appPath)
            ? FetchUploadEntryFromPath(appPath)
            : throw new InvalidOperationException($"App with id {appId} not found in the registry.");
    }

    public AppVersion GetLatestVersion(string appId, string tag = "")
    {
        AppVersion? latest = null;

        string versionsRoot = Path.Combine(
            uploadsFolder.FullName,
            appId,
            "versions");

        if (!Directory.Exists(versionsRoot))
            throw new DirectoryNotFoundException(versionsRoot);

        foreach (string versionDir in Directory.EnumerateDirectories(versionsRoot))
        {
            string detailsPath = Path.Combine(versionDir, ".versiondetails");

            if (!File.Exists(detailsPath))
                continue;

            using FileStream stream = new(
                detailsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            AppVersion current = WinterForge.DeserializeFromHumanReadableStream<AppVersion>(stream);

            if (!string.Equals(current.Tag, tag, StringComparison.OrdinalIgnoreCase))
                continue;

            if (latest is null || current > latest)
                latest = current;
        }

        return latest ?? throw new InvalidOperationException(
            $"No versions found for app '{appId}' on branch '{tag}'.");
    }

    private AppEntry FetchUploadEntryFromPath(string appPath)
    {
        string detailsPath = Path.Combine(appPath, ".appdetails");

        if (!File.Exists(detailsPath))
            throw new FileNotFoundException(detailsPath);

        AppEntry app;

        using (FileStream stream = new(
                   detailsPath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.Read))
        {
            app = WinterForge.DeserializeFromHumanReadableStream<AppEntry>(stream);
        }

        app.Versions.Clear();
        app.Diffs.Clear();

        string versionsRoot = Path.Combine(appPath, "versions");

        if (!Directory.Exists(versionsRoot))
            return app;

        foreach (string versionDir in Directory.EnumerateDirectories(versionsRoot))
        {
            string versionDetailsPath = Path.Combine(versionDir, ".versiondetails");

            if (!File.Exists(versionDetailsPath))
                continue;

            AppVersion version;

            using (FileStream stream = new(
                       versionDetailsPath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            {
                version =
                    WinterForge.DeserializeFromHumanReadableStream<AppVersion>(stream);
            }

            app.Versions.Add(version);

            string diffDir = Path.Combine(versionDir, "diffs");

            if (!Directory.Exists(diffDir))
                continue;

            foreach (string diffFile in Directory.EnumerateFiles(diffDir, "*.wfdiff"))
            {
                string toVersion = Path.GetFileNameWithoutExtension(diffFile);

                app.Diffs.Add(new DiffEntry
                {
                    FromVersion = version.ToString(VersionStringFormat.FolderSafe),
                    ToVersion = toVersion,
                    FileName = Path.GetFileName(diffFile),
                    FilePath = diffFile
                });
            }
        }

        app.Versions.Sort();

        return app;
    }

    public async Task DeleteVersion(string appAppId, string versionVersionLabel)
    {
        
    }
}
