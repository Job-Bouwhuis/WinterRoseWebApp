using WinterRose.Diff;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public sealed class AppDiffService
{
    private readonly DirectoryDiffEngine directoryDiffer = new();
    private readonly ILogger<AppDiffService> logger;
    private readonly DirectoryInfo basePath = FileUploadGlobals.uploadsFolder;

    public AppDiffService(ILogger<AppDiffService> logger)
    {
        this.logger = logger;
    }

    public async Task<string> GetOrCreateDiffAsync(
        string appName,
        DirectoryInfo from,
        DirectoryInfo to)
    {
        string diffPath = BuildDiffPath(from, to);

        if (File.Exists(diffPath))
            return diffPath;

        logger.LogInformation("Creating diff {App}: {From} -> {To}",
            appName, from.Name, to.Name);

        var diff = await directoryDiffer.DiffAsync(from, to);
        diff.Save(diffPath);

        return diffPath;
    }
    
    private DirectoryInfo GetVersionsRoot(string appName)
    {
        var root = Path.Combine(basePath.FullName, appName, "versions");
        return new DirectoryInfo(root);
    }

    public async Task<Stream> OpenDiffStreamAsync(
        string appName,
        AppVersion from,
        AppVersion to)
    {
        var appRoot = Path.Combine(basePath.FullName, appName, "versions");

        var fromDir = new DirectoryInfo(
            Path.Combine(appRoot, from.ToString(VersionStringFormat.FolderSafe)));

        var toDir = new DirectoryInfo(
            Path.Combine(appRoot, to.ToString(VersionStringFormat.FolderSafe)));

        if (!fromDir.Exists)
            throw new DirectoryNotFoundException($"From version not found: {from}");

        if (!toDir.Exists)
            throw new DirectoryNotFoundException($"To version not found: {to}");

        string diffPath = await CreateDiffAsyncInternal(
            appName,
            fromDir,
            toDir);

        return new FileStream(
            diffPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 128,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }
    
    private async Task<string> CreateDiffAsyncInternal(
        string appName,
        DirectoryInfo from,
        DirectoryInfo to)
    {
        var diffPath = BuildDiffPath(from, to);

        if (File.Exists(diffPath))
            return diffPath;

        logger.LogInformation(
            "Creating on-demand diff {App}: {From} -> {To}",
            appName, from.Name, to.Name);

        var diff = await directoryDiffer.DiffAsync(from, to);
        diff.Save(diffPath);

        return diffPath;
    }
    
    private string BuildDiffPath(
        DirectoryInfo from,
        DirectoryInfo to)
    {
        var fromDiffDir = Path.Combine(from.FullName, "diffs");

        Directory.CreateDirectory(fromDiffDir);

        string fileName = $"{to.Name}.wfdiff";

        return Path.Combine(fromDiffDir, fileName);
    }
}