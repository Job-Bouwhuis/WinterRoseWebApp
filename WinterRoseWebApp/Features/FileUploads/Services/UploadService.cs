using Microsoft.AspNetCore.Components.Forms;
using System.Text.RegularExpressions;
using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRoseWebApp.Features.FileUploads.Services;

public partial class UploadService
{
    private const string UPLOADS_ROOT = "Uploads";

    private readonly IWebHostEnvironment environment;
    private readonly UploadQueue uploadQueue;

    public UploadService(IWebHostEnvironment environment, UploadQueue uploadQueue)
    {
        this.environment = environment;
        this.uploadQueue = uploadQueue;
    }

    public async Task<UploadResult> SaveAsync(
        string name,
        string version,
        IEnumerable<(IBrowserFile File, string RelativePath)> files)
    {
        var safeName = Sanitize(name);
        var safeVersion = Sanitize(version);

        var basePath = Path.Combine(environment.ContentRootPath, "Uploads", safeName);
        var versionPath = Path.Combine(basePath, safeVersion);
        var latestPath = Path.Combine(basePath, "latest");

        Directory.CreateDirectory(versionPath);

        foreach (var entry in files)
        {
            var relativePath = NormalizeRelativePath(entry.RelativePath);

            var fullPath = Path.Combine(versionPath, relativePath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var stream = entry.File.OpenReadStream(long.MaxValue);
            await using var fs = File.Create(fullPath);

            await stream.CopyToAsync(fs);
        }

        MirrorDirectory(versionPath, latestPath);

        await uploadQueue.Writer.WriteAsync(new UploadCompletedEvent(safeName, basePath));

        return new UploadResult
        {
            Name = safeName,
            Version = safeVersion,
            TargetPath = versionPath
        };
    }

    /// <summary>
    /// Walks /Uploads/{name}/{version}/ on disk and returns structured groups.
    /// Version folders match the pattern N_N_N (any number of segments).
    /// </summary>
    public Task<List<UploadGroup>> GetUploadGroupsAsync()
    {
        var result = new List<UploadGroup>();

        if (!Directory.Exists(UPLOADS_ROOT))
            return Task.FromResult(result);

        foreach (var nameDir in Directory.EnumerateDirectories(UPLOADS_ROOT).OrderBy(d => d))
        {
            var uploadName = Path.GetFileName(nameDir);
            var versions = new List<VersionEntry>();
            var diffs = new List<DiffEntry>();

            foreach (var subDir in Directory.EnumerateDirectories(nameDir))
            {
                var folderName = Path.GetFileName(subDir);

                // Skip reserved folders
                if (folderName.Equals("latest", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (folderName.Equals("Diffs", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse diff files: {FROM}_TO_{TO}
                    foreach (var diffFile in Directory.EnumerateFiles(subDir))
                    {
                        var fileName = Path.GetFileName(diffFile);
                        var toIndex = fileName.IndexOf("_TO_", StringComparison.OrdinalIgnoreCase);
                        if (toIndex < 0) continue;

                        var from = fileName[..toIndex];
                        var to = fileName[(toIndex + 4)..];

                        diffs.Add(new DiffEntry
                        {
                            FromVersion = from,
                            ToVersion = to,
                            FilePath = diffFile,
                            FileName = fileName,
                        });
                    }

                    continue;
                }

                // Match version pattern: only digits and underscores, at least one underscore
                if (IsVersionFolder(folderName))
                {
                    var uploadedAt = Directory.GetCreationTimeUtc(subDir);

                    versions.Add(new VersionEntry
                    {
                        VersionLabel = folderName,
                        UploadedAt = uploadedAt,
                    });
                }
            }

            // Sort versions ascending
            versions.Sort((a, b) => CompareVersionLabels(a.VersionLabel, b.VersionLabel));

            result.Add(new UploadGroup
            {
                Name = uploadName,
                Versions = versions,
                Diffs = diffs,
            });
        }

        return Task.FromResult(result);
    }

    private static bool IsVersionFolder(string name) => VersionNamePattern().IsMatch(name);

    private static int CompareVersionLabels(string a, string b)
    {
        static int[] Parse(string v) =>
            v.Split('_').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        var pa = Parse(a);
        var pb = Parse(b);
        var len = Math.Max(pa.Length, pb.Length);

        for (int i = 0; i < len; i++)
        {
            var ai = i < pa.Length ? pa[i] : 0;
            var bi = i < pb.Length ? pb[i] : 0;
            if (ai != bi) return ai.CompareTo(bi);
        }

        return 0;
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
    private void MirrorDirectory(string source, string target)
    {
        if (Directory.Exists(target))
            Directory.Delete(target, true);

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var dst = Path.Combine(target, relative);

            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.Copy(file, dst, true);
        }
    }
    private string Sanitize(string input)
    {
        input = input.Trim().ToLowerInvariant();
        input = Regex.Replace(input, @"[^a-z0-9_\-]", "_");
        return input;
    }

    [GeneratedRegex(@"^\d+(_\d+)+$")]
    private static partial Regex VersionNamePattern();
}