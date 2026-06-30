using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using WinterRose.Diff;
using WinterRose.Nexus.Shared;
using WinterRose.ProgressKeeping;

namespace WinterRose.Nexus.Services;

public class ApplicationInstaller
{
    private readonly AppServerClient server;
    private readonly string appsRoot;

    public ApplicationInstaller(AppServerClient server)
    {
        this.server = server;

        appsRoot = Path.Combine(Environment.CurrentDirectory, "apps");
    }

    private string GetAppRoot(string appName)
    {
        return Path.Combine(appsRoot, appName);
    }

    private string GetAppFilesPath(string appName)
    {
        return Path.Combine(appsRoot, appName, "app");
    }

    // =========================================================
    // FULL INSTALL
    // =========================================================
    public async Task InstallFromArchiveAsync(
        string appName,
        AppVersion version,
        IProgressScope progress)
    {
        await using var archiveStream = await server.GetVersionArchiveStreamAsync(appName, version);
        
        var appRoot = GetAppRoot(appName);
        var targetPath = GetAppFilesPath(appName);

        var extractScope = progress.CreateChild(0.85);
        var finalizeScope = progress.CreateChild(0.15);

        string tempPath = Path.Combine(appRoot, "_tmp_install");

        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);

        Directory.CreateDirectory(tempPath);

        await ExtractZipAsync(archiveStream, tempPath, extractScope);

        finalizeScope.Report(0.3, "Replacing installation");

        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        Directory.Move(tempPath, targetPath);

        await File.WriteAllTextAsync(
            Path.Combine(appRoot, "version.txt"),
            version.ToString(VersionStringFormat.FolderSafe));

        finalizeScope.Report(1.0, "Install complete");
    }

    // =========================================================
    // PATCH / UPDATE
    // =========================================================
    public async Task PatchApplicationAsync(
        string appName,
        AppVersion currentVersion,
        AppVersion newVersion,
        IProgressScope progress)
    {
        var appPath = GetAppFilesPath(appName);

        var applyScope = progress.CreateChild(1.0);

        if (!Directory.Exists(appPath))
            throw new DirectoryNotFoundException("App not installed");

        applyScope.Report(0.1, "Applying patch");

        await ApplyDiffStreamAsync(appName, currentVersion, newVersion, appPath, applyScope);

        applyScope.Report(1.0, "Patch complete");
    }

    // =========================================================
    // INTERNAL ZIP EXTRACTOR
    // =========================================================
    private async Task ExtractZipAsync(
        Stream zipStream,
        string targetPath,
        IProgressScope scope)
    {
        using var archive = new ZipArchive(
            zipStream,
            ZipArchiveMode.Read,
            leaveOpen: true);

        var entries = archive.Entries;
        int total = entries.Count;
        int index = 0;

        foreach (var entry in entries)
        {
            double progress = total == 0 ? 1.0 : (double)index / total;

            scope.Report(progress, $"Extracting {entry.FullName}");

            if (string.IsNullOrEmpty(entry.Name))
            {
                index++;
                continue;
            }

            string fullPath = Path.Combine(targetPath, entry.FullName);

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await using var entryStream = entry.Open();
            await using var fileStream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await entryStream.CopyToAsync(fileStream);

            index++;
        }

        scope.Report(1.0, "Extraction complete");
    }


    private async Task ApplyDiffStreamAsync(
        string appName,
        AppVersion currentVersion,
        AppVersion newVersion,
        string targetPath,
        IProgressScope scope)
    {
        await using Stream diffStream =
            await server.GetDiffStreamAsync(appName, currentVersion, newVersion);
        
        var diff = DirectoryDiff.Load(diffStream);
        
        DiffApplier applier = new DiffApplier();
        await applier.ApplyDiff(targetPath, diff, scope);
    }
}