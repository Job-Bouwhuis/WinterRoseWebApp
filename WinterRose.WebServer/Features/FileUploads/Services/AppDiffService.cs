using WinterRose.Diff;
using WinterRose.WebServer.Features.FileUploads.Models;

namespace WinterRose.WebServer.Features.FileUploads.Services;

public sealed class AppDiffService
{
    private readonly DirectoryDiffEngine directoryDiffer = new();
    private readonly ILogger<AppDiffService> logger;

    public AppDiffService(ILogger<AppDiffService> logger)
    {
        this.logger = logger;
    }

    public async Task<string> GetOrCreateDiffAsync(
        string appName,
        DirectoryInfo from,
        DirectoryInfo to)
    {
        var diffRoot = GetDiffRoot(appName);

        string diffPath = BuildDiffPath(diffRoot, from, to);

        if (File.Exists(diffPath))
            return diffPath;

        logger.LogInformation("Creating diff {App}: {From} -> {To}",
            appName, from.Name, to.Name);

        var diff = await directoryDiffer.DiffAsync(from, to);
        diff.Save(diffPath);

        return diffPath;
    }

    private DirectoryInfo GetDiffRoot(string appName)
    {
        var root = Path.Combine("Uploads", appName, "Diffs");
        return Directory.CreateDirectory(root);
    }

    public async Task<Stream> OpenDiffStreamAsync(
        string appName,
        AppVersion from,
        AppVersion to)
    {
        var appRoot = Path.Combine("Uploads", appName);

        var fromDir = new DirectoryInfo(
            Path.Combine(appRoot, from.ToString(VersionStringFormat.FolderSafe)));

        var toDir = new DirectoryInfo(
            Path.Combine(appRoot, to.ToString(VersionStringFormat.FolderSafe)));

        if (!fromDir.Exists)
            throw new DirectoryNotFoundException($"From version not found: {from}");

        if (!toDir.Exists)
            throw new DirectoryNotFoundException($"To version not found: {to}");

        var diffRoot = new DirectoryInfo(Path.Combine(appRoot, "Diffs"));

        string diffPath = await CreateDiffAsyncInternal(
            appName,
            fromDir,
            toDir,
            diffRoot);

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
        DirectoryInfo to,
        DirectoryInfo diffRoot)
    {
        var fromDir = diffRoot.CreateSubdirectory(from.Name);

        string fileName = $"{to.Name}.wfdiff";
        string fullPath = Path.Combine(fromDir.FullName, fileName);

        if (File.Exists(fullPath))
            return fullPath;

        logger.LogInformation(
            "Creating on-demand diff {App}: {From} -> {To}",
            appName, from.Name, to.Name);

        var diff = await directoryDiffer.DiffAsync(from, to);
        diff.Save(fullPath);

        return fullPath;
    }
    
    private string BuildDiffPath(
        DirectoryInfo diffRoot,
        DirectoryInfo from,
        DirectoryInfo to)
    {
        var fromDir = diffRoot.CreateSubdirectory(from.Name);

        string fileName = $"{to.Name}.wfdiff";

        return Path.Combine(fromDir.FullName, fileName);
    }
}