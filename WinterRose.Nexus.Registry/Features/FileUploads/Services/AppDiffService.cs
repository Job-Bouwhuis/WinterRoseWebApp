using WinterRose.Diff;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;
using WinterRose.TaskLock;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public sealed class AppDiffService : IDisposable
{
    /// <summary>
    /// Identity of a single diff generation task: which app, and the exact
    /// from/to version folder names involved. Folder names (rather than
    /// AppVersion/DirectoryInfo) are used as the key components so that two
    /// requests resolving to the same on-disk paths are always recognized as
    /// the same piece of work, regardless of which overload/call path produced them.
    /// </summary>
    private readonly record struct DiffKey(string AppName, string FromVersionFolder, string ToVersionFolder);

    private readonly DirectoryDiffEngine directoryDiffer = new();
    private readonly ILogger<AppDiffService> logger;
    private readonly DirectoryInfo basePath = FileUploadGlobals.uploadsFolder;

    // Caches completed diff files so repeat requests (even well after the
    // originating in-flight request finished) skip regeneration entirely.
    // Idle timeout means rarely-requested diffs don't sit in memory forever.
    private readonly CachedTaskLock<DiffKey, FileInfo> diffCache =
        new(idleTimeout: TimeSpan.FromMinutes(30));

    public AppDiffService(ILogger<AppDiffService> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Ensures the diff file for the given directories exists, generating it if
    /// necessary. Concurrent/repeat calls for the same app+from+to are
    /// de-duplicated and cached (see <see cref="CachedTaskLock{TKey,TResult}"/>).
    /// </summary>
    public async Task<FileInfo> GetOrCreateDiffAsync(
        string appName,
        DirectoryInfo from,
        DirectoryInfo to,
        CancellationToken cancellationToken = default)
    {
        var key = new DiffKey(appName, from.Name, to.Name);

        return await diffCache.GetOrAddAsync(
            key,
            ct => GenerateDiffAsync(appName, from, to, ct),
            cancellationToken);
    }

    /// <summary>
    /// Opens a read-only, share-read stream to the diff file for the given
    /// versions, generating it first if necessary.
    /// </summary>
    public async Task<Stream> OpenDiffStreamAsync(
        string appName,
        AppVersion from,
        AppVersion to,
        CancellationToken cancellationToken = default)
    {
        var appRoot = Path.Combine(basePath.FullName, appName, "versions");

        var fromDir = new DirectoryInfo(
            Path.Combine(appRoot, from.ToString(VersionStringFormat.FolderSafe), "files"));

        var toDir = new DirectoryInfo(
            Path.Combine(appRoot, to.ToString(VersionStringFormat.FolderSafe), "files"));

        if (!fromDir.Exists)
            throw new DirectoryNotFoundException($"From version not found: {from}");

        if (!toDir.Exists)
            throw new DirectoryNotFoundException($"To version not found: {to}");

        var diffFile = await GetOrCreateDiffAsync(appName, fromDir, toDir, cancellationToken);

        return new FileStream(
            diffFile.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 128,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    /// <summary>
    /// Removes a specific diff from the cache (does not delete the file on
    /// disk, and does not affect a generation currently in flight). Useful if
    /// you know a diff needs to be regenerated (e.g. algorithm change) without
    /// waiting for its idle timeout.
    /// </summary>
    public bool InvalidateCacheEntry(string appName, DirectoryInfo from, DirectoryInfo to) =>
        diffCache.Invalidate(new DiffKey(appName, from.Name, to.Name));

    private async Task<FileInfo> GenerateDiffAsync(
        string appName,
        DirectoryInfo from,
        DirectoryInfo to,
        CancellationToken cancellationToken)
    {
        var diffPath = BuildDiffPath(from, to);

        // Fast path: the file already exists on disk from a previous process
        // lifetime (CachedTaskLock's in-memory cache doesn't persist across
        // restarts). Avoids regenerating something that's already there.
        if (File.Exists(diffPath))
            return new FileInfo(diffPath);

        logger.LogInformation(
            "Creating diff {App}: {From} -> {To}",
            appName, from.Name, to.Name);

        var diff = await directoryDiffer.DiffAsync(from, to);

        cancellationToken.ThrowIfCancellationRequested();

        diff.Save(diffPath);

        return new FileInfo(diffPath);
    }

    private string BuildDiffPath(
        DirectoryInfo from,
        DirectoryInfo to)
    {
        var fromDiffDir = Path.Combine(from.Parent.FullName, "diffs");

        Directory.CreateDirectory(fromDiffDir);

        string fileName = $"{to.Parent.Name}.wfdiff";

        return Path.Combine(fromDiffDir, fileName);
    }

    public void Dispose() => diffCache.Dispose();
}