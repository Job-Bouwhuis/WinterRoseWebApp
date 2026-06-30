using WinterRose.Diff;
using WinterRose.WinterForgeSerializing;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public sealed class DiffCreatorService(AppDiffService diffService, UploadQueue queue, ILogger<DiffCreatorService> logger)
    : BackgroundService
{
    private static string UploadsFolderPath = "Uploads";
    
    private DirectoryDiffEngine directoryDiffer = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var ev in queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await HandleAsync(ev, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed processing upload event for {Name}", ev.Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("DiffCreatorService is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred in DiffCreatorService.");
        }
    }

    private async Task HandleAsync(UploadCompletedEvent ev, CancellationToken ct)
    {
        logger.LogInformation(
            "Upload completed: {Name} at {BasePath}",
            ev.Name,
            ev.BasePath);

        var baseDir = new DirectoryInfo(ev.BasePath);

        if (!baseDir.Exists)
        {
            logger.LogWarning("Base path does not exist: {Path}", ev.BasePath);
            return;
        }

        string targetTag = string.IsNullOrEmpty(ev.AppVersion.Tag)
            ? "release"
            : ev.AppVersion.Tag.ToLowerInvariant();

        var versionDirs = baseDir
            .GetDirectories()
            .Where(d =>
                !d.Name.Equals("latest", StringComparison.OrdinalIgnoreCase) &&
                !d.Name.Equals("diffs", StringComparison.OrdinalIgnoreCase))
            .Select(d => new
            {
                Directory = d,
                Version = ParseVersionEntry(d.Name)
            })
            .Where(x => x.Version is not null)
            .ToList();

        var sameTag = versionDirs
            .Where(x =>
                string.IsNullOrEmpty(x.Version!.Tag)
                    ? targetTag == "release"
                    : x.Version!.Tag.Equals(ev.AppVersion.Tag, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Version)
            .ToList();

        if (sameTag.Count < 2)
        {
            logger.LogWarning("Not enough versions to create diff for {Tag}", targetTag);
            return;
        }

        var to = sameTag[^1].Directory;
        var from = sameTag[^2].Directory;

        logger.LogInformation("Preparing diff: {From} -> {To}", from.Name, to.Name);

        await diffService.GetOrCreateDiffAsync(
            ev.Name,
            from,
            to);

        logger.LogInformation("Diff ready for {App}", ev.Name);
    }
    
    
    
    private AppVersion? ParseVersionEntry(string folderName)
    {
        try
        {
            return new AppVersion(folderName);
        }
        catch
        {
            return null;
        }
    }
}