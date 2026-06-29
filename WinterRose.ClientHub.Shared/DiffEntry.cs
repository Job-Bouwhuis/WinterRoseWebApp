namespace WinterRose.WebServer.Features.FileUploads.Models;

/// <summary>
/// A diff file, e.g. 1_0_0_TO_1_1_0 inside /Uploads/MyApp/Diffs/
/// </summary>
public class DiffEntry
{
    public string FromVersion { get; init; } = "";    // "1_0_0"
    public string ToVersion { get; init; } = "";      // "1_1_0"
    [WFExclude]
    public string FilePath { get; init; } = "";       // absolute path on disk
    public string FileName { get; init; } = "";       // "1_0_0_TO_1_1_0"
}