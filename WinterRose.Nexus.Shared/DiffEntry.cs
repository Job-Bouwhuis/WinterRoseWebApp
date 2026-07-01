namespace WinterRose.Nexus.Shared;

/// <summary>
/// A Nexus diff file
/// </summary>
public class DiffEntry
{
    public string FromVersion { get; init; } = "";    // "1_0_0"
    public string ToVersion { get; init; } = "";      // "1_1_0"
    [WFExclude]
    public string FilePath { get; init; } = "";       // absolute path on disk
    public string FileName { get; init; } = "";       // "1_0_0_TO_1_1_0"
}