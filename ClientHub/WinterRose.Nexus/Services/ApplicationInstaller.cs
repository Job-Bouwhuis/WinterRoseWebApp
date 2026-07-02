using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using WinterRose.Diff;
using WinterRose.Nexus.Models;
using WinterRose.Nexus.Shared;
using WinterRose.ProgressKeeping;

namespace WinterRose.Nexus.Services;

public class ApplicationInstaller
{
    private readonly AppServerClient server;
    private readonly ClientAppRepository repository;
    private readonly string appsRoot;

    public ApplicationInstaller(AppServerClient server, ClientAppRepository repository)
    {
        this.server = server;
        this.repository = repository;

        appsRoot = Path.Combine(Environment.CurrentDirectory, "apps");
    }

    private string GetAppRoot(string appId) => repository.GetAppRoot(appId);

    private string GetAppFilesPath(string appId) => repository.GetAppFilesPath(appId);

    // =========================================================
    // FULL INSTALL
    // =========================================================
    public async Task InstallFromArchiveAsync(
        string appId,
        AppVersion version,
        IProgressScope progress,
        bool pinVersion = false)
    {
        await using var archiveStream = await server.GetVersionArchiveStreamAsync(appId, version);
        AppEntry entry = await server.GetAppEntryAsync(appId);
        
        var appRoot = GetAppRoot(appId);
        var targetPath = GetAppFilesPath(appId);

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

        finalizeScope.Report(0.7, "Writing app details");

        var details = new LocalAppEntry(appId, entry.DisplayName, version, targetPath, pinVersion);
        repository.SaveLocalAppDetails(details);

        finalizeScope.Report(1.0, "Install complete");
    }

    // =========================================================
    // PATCH / UPDATE
    // =========================================================
    public async Task PatchApplicationAsync(
        string appId,
        AppVersion newVersion,
        IProgressScope progress)
    {
        var appPath = GetAppFilesPath(appId);

        var applyScope = progress.CreateChild(1.0);

        if (!Directory.Exists(appPath))
            throw new DirectoryNotFoundException("App not installed");

        var details = repository.TryReadLocalAppDetails(appId)
            ?? throw new InvalidOperationException(
                $"No .localappdetails found for '{appId}'; app is not properly installed.");

        var currentVersion = details.InstalledVersion;

        applyScope.Report(0.1, "Applying patch");

        await ApplyDiffStreamAsync(appId, currentVersion, newVersion, appPath, applyScope);

        applyScope.Report(0.9, "Writing app details");

        details.InstalledVersion = newVersion;
        repository.SaveLocalAppDetails(details);

        applyScope.Report(1.0, "Patch complete");
    }

    /// <summary>
    /// Updates only the user's pin preference for an installed app, without
    /// touching the installed files or version.
    /// </summary>
    public void SetPinVersion(string appId, bool pinVersion)
    {
        var details = repository.TryReadLocalAppDetails(appId)
            ?? throw new InvalidOperationException(
                $"No .localappdetails found for '{appId}'; app is not installed.");

        details.PinVersion = pinVersion;
        repository.SaveLocalAppDetails(details);
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
        string appId,
        AppVersion currentVersion,
        AppVersion newVersion,
        string targetPath,
        IProgressScope scope)
    {
        await using Stream diffStream =
            await server.GetDiffStreamAsync(appId, currentVersion, newVersion);

        var diff = DirectoryDiff.Load(diffStream);

        DiffApplier applier = new DiffApplier();
        await applier.ApplyDiff(targetPath, diff, scope);
    }

    public async Task UninstallApplicationAsync(string appId)
    {
        DirectoryInfo installDir = new DirectoryInfo(GetAppFilesPath(appId));
        if (!installDir.Exists)
            throw new DirectoryNotFoundException("App not installed");

        await Task.Run(() =>
        {
            Directory.Delete(installDir.FullName, true);
        });
    }
}
