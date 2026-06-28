using Microsoft.AspNetCore.Components.Forms;
using System.Text.RegularExpressions;
using WinterRose.WebServer.Features.FileUploads.Models;

namespace WinterRose.WebServer.Features.FileUploads.Services;

public partial class UploadService
{
    private const string UPLOADS_ROOT = "Uploads";

    private readonly IWebHostEnvironment environment;
    private readonly UploadQueue uploadQueue;
    private readonly DirectoryInfo uploadsFolder;

    public UploadService(IWebHostEnvironment environment, UploadQueue uploadQueue)
    {
        this.environment = environment;
        this.uploadQueue = uploadQueue;

        var uploadsFolderPath = Path.Combine(AppContext.BaseDirectory, UPLOADS_ROOT);
        if (!Directory.Exists(uploadsFolderPath))
            Directory.CreateDirectory(uploadsFolderPath);
        uploadsFolder = new DirectoryInfo(uploadsFolderPath);
    }

    public async Task<UploadResult> SaveAsync(
        string name,
        string version,
        IEnumerable<(IBrowserFile File, string RelativePath)> files)
    {
        var safeName = Sanitize(name);

        var versionEntry = new VersionEntry(Sanitize(version));
        var safeVersion = versionEntry.ToString(VersionStringFormat.FolderSafe);

        var basePath = Path.Combine(uploadsFolder.FullName, safeName);
        var versionPath = Path.Combine(basePath, safeVersion);

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

        if (string.IsNullOrEmpty(versionEntry.Tag))
        {
            var latestPath = Path.Combine(basePath, "latest", "release");
            MirrorDirectory(versionPath, latestPath);
        }
        else
        {
            var tagLatestPath = Path.Combine(basePath, "latest", versionEntry.Tag);
            MirrorDirectory(versionPath, tagLatestPath);
        }

        await uploadQueue.Writer.WriteAsync(
            new UploadCompletedEvent(safeName, basePath, versionEntry));

        return new UploadResult
        {
            Name = safeName,
            Version = safeVersion,
            TargetPath = versionPath
        };
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
}