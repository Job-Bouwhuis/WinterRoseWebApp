using WinterRose.Diff;
using WinterRose.WinterForgeSerializing;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public sealed class DiffCreatorService(
    AppDiffService diffService,
    IAsyncEventQueue<UploadCompletedEvent> eventQueue,
    ILogger<DiffCreatorService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await eventQueue.SubscribeAsync(async (@event, ct) =>
        {
            try
            {
                await HandleAsync(@event, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed processing upload event for {Name}", @event.Name);
            }
        }, stoppingToken);
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

        var versionDirs = baseDir.CreateSubdirectory("versions")
            .GetDirectories()
            .Select(d => new
            {
                Directory = new DirectoryInfo(Path.Combine(d.FullName, "files")),
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

        FileInfo diffFile = await diffService.GetOrCreateDiffAsync(
            ev.Name,
            from,
            to);

        logger.LogInformation("Diff ready for {App} at {path}", ev.Name, diffFile.FullName);
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