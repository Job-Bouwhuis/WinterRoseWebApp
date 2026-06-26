using WinterRose.Diff;
using WinterRose.WinterForgeSerializing;
using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRoseWebApp.Features.FileUploads.Services;

public sealed class DiffCreatorService(UploadQueue queue, ILogger<DiffCreatorService> logger)
    : BackgroundService
{
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

        var versionDirs = baseDir
            .GetDirectories()
            .Where(d => d.Name != "Latest")
            .Select(d => new
            {
                Directory = d,
                Version = ParseVersion(d.Name)
            })
            .Where(x => x.Version is not null)
            .OrderBy(x => x.Version)
            .ToList();

        if (versionDirs.Count == 0)
        {
            logger.LogWarning("No versions found in {Path}", ev.BasePath);
            return;
        }

        var latest = versionDirs[^1].Directory;
        var previous = versionDirs.Count > 1
            ? versionDirs[^2].Directory
            : null;

        logger.LogInformation("Latest version: {Latest}", latest.FullName);

        if (previous is not null)
        {
            logger.LogInformation("Previous version: {Previous}", previous.FullName);
            logger.LogInformation("Creating diff between {Previous} and {Latest}", previous.FullName, latest.FullName);

            var diff = await directoryDiffer.DiffAsync(previous, latest);

            var diffDir = baseDir.CreateSubdirectory("Diffs");

            string prevVers = Path.GetFileNameWithoutExtension(previous.FullName);
            string latestVer = Path.GetFileNameWithoutExtension(latest.FullName);
            string diffName = $"{prevVers}_TO_{latestVer}";

            logger.LogInformation("Diff created, Saving...");

            diff.Save(Path.Combine(diffDir.FullName, diffName));
            logger.LogInformation("A version diff was created: {0}", diffName);

            DirectoryDiff loaded = DirectoryDiff.Load(Path.Combine(diffDir.FullName, diffName));

            TestDiffApply(previous, latest, loaded);
        }
    }

    private void TestDiffApply(DirectoryInfo previous, DirectoryInfo latest, DirectoryDiff diff)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string testDirectory = Path.Combine(desktop, previous.Name);

        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, true);

        CopyDirectory(previous.FullName, testDirectory);

        var applyer = new DiffApplyer();

        logger.LogInformation("Applying diff to test directory: {0}", testDirectory);
        var t = applyer.ApplyDiff(testDirectory, diff).GetAwaiter().GetResult();

        if (t.Count is 0)
            logger.LogInformation("Diff applied successfully!");
        else
        {
            foreach(string path in t)
            {
                logger.LogWarning("Diff failed for file {}", path);
            }
        }


    }

    private void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    private Version? ParseVersion(string folderName)
    {
        try
        {
            var normalized = folderName.Replace('_', '.');

            return Version.TryParse(normalized, out var version)
                ? version
                : null;
        }
        catch
        {
            return null;
        }
    }
}