using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using System.Text.RegularExpressions;
using WinterRose.Nexus.Shared;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public static class FileUploadGlobals
{
    public static readonly DirectoryInfo uploadsFolder;

    static FileUploadGlobals()
    {
        var uploadsFolderPath = Path.Combine(AppContext.BaseDirectory, UPLOADS_ROOT);
        if (!Directory.Exists(uploadsFolderPath))
            Directory.CreateDirectory(uploadsFolderPath);
        uploadsFolder = new DirectoryInfo(uploadsFolderPath);
    }

    private const string UPLOADS_ROOT = "uploads";
}

public partial class UploadService
{
    private readonly IWebHostEnvironment environment;
    private readonly UploadQueue uploadQueue;
    private readonly DirectoryInfo uploadsFolder = FileUploadGlobals.uploadsFolder;

    public UploadService(IWebHostEnvironment environment, UploadQueue uploadQueue)
    {
        this.environment = environment;
        this.uploadQueue = uploadQueue;
    }

    public async Task SaveAsync(
        AppEntry appEntry,
        AppVersion version,
        IEnumerable<(IBrowserFile File, string RelativePath)> files)
    {
        if (appEntry == null)
            throw new ArgumentNullException(nameof(appEntry));

        if (string.IsNullOrWhiteSpace(appEntry.Name))
            throw new InvalidOperationException("App Name is required.");

        var appId = GenerateAppId(appEntry.Name);
        var safeVersion = version.ToString(VersionStringFormat.FolderSafe);

        var appRoot = Path.Combine(uploadsFolder.FullName, appId);
        var versionsRoot = Path.Combine(appRoot, "versions");
        var versionPath = Path.Combine(versionsRoot, safeVersion);

        Directory.CreateDirectory(versionPath);

        // ----------------------------
        // Write app metadata (.appdetails)
        // ----------------------------
        WriteAppDetails(appRoot, appEntry);

        // ----------------------------
        // Write files
        // ----------------------------
        foreach (var entry in files)
        {
            var relativePath = NormalizeRelativePath(entry.RelativePath);
            var fullPath = Path.Combine(versionPath, "files", relativePath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var stream = entry.File.OpenReadStream(long.MaxValue);
            await using var fs = File.Create(fullPath);

            await stream.CopyToAsync(fs);
        }

        // ----------------------------
        // Calculate install size
        // ----------------------------
        var filesRoot = Path.Combine(versionPath, "files");

        long installSize = Directory.Exists(filesRoot)
            ? Directory.GetFiles(filesRoot, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length)
            : 0;

        version.InstallSize = installSize;
        
        // ----------------------------
        // Write version metadata
        // ----------------------------
        WriteVersionDetails(
            versionPath,
            version);

        await uploadQueue.Writer.WriteAsync(
            new UploadCompletedEvent(appId, appRoot, version));
    }
    
    private void WriteAppDetails(string appRoot, AppEntry incoming)
    {
        var path = Path.Combine(appRoot, ".appdetails");

        AppEntry? existing = null;

        if (File.Exists(path))
            existing = WinterForge.DeserializeFromFile<AppEntry>(path);

        var final = new AppEntry
        {
            Name = MergeString(incoming.Name, existing?.Name),

            DisplayName = MergeString(incoming.DisplayName, existing?.DisplayName),
            Publisher = MergeString(incoming.Publisher, existing?.Publisher),
            ShortDescription = MergeString(incoming.ShortDescription, existing?.ShortDescription),
            LongDescription = MergeString(incoming.LongDescription, existing?.LongDescription),

            IconPath = MergeString(incoming.IconPath, existing?.IconPath),

            Tags = MergeTags(incoming.Tags, existing?.Tags),

            Versions = existing?.Versions ?? [],
            Diffs = existing?.Diffs ?? []
        };

        if (string.IsNullOrWhiteSpace(final.Name))
            throw new InvalidOperationException("App Name missing (required).");

        WinterForge.SerializeToFile(final, path, TargetFormat.FormattedHumanReadable);
    }

    private void WriteVersionDetails(
        string versionPath,
        AppVersion version)
    {
        var path = Path.Combine(versionPath, ".versiondetails");

        WinterForge.SerializeToFile(version, path, TargetFormat.FormattedHumanReadable);
    }
    
    private string NormalizeRelativePath(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');

        // "MyApp/subfolder/file.dll" > "subfolder/file.dll"
        var slashIndex = path.IndexOf('/');
        if (slashIndex >= 0)
            path = path[(slashIndex + 1)..];

        return path.Replace('/', Path.DirectorySeparatorChar);
    }
    
    private string MergeString(string incoming, string? existing)
    {
        return !string.IsNullOrWhiteSpace(incoming)
            ? incoming
            : existing ?? "";
    }
    
    private List<string> MergeTags(List<string> incoming, List<string> existing)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (existing != null)
            foreach (var t in existing)
                if (!string.IsNullOrWhiteSpace(t))
                    set.Add(t);

        if (incoming != null)
            foreach (var t in incoming)
                if (!string.IsNullOrWhiteSpace(t))
                    set.Add(t);

        return set.ToList();
    }
    
    private string GenerateAppId(string appName)
    {
        var normalized = Sanitize(appName).Trim().ToLowerInvariant();

        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
    
    private string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        input = input.Trim().ToLowerInvariant();

        // Replace invalid characters with underscores
        input = Regex.Replace(input, @"[^a-z0-9_\-]", "_");

        // Collapse multiple underscores into one
        input = Regex.Replace(input, @"_+", "_");

        // Trim underscores from edges
        input = input.Trim('_');

        return input;
    }
}