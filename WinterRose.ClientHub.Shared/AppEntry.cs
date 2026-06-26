namespace WinterRoseWebApp.Features.FileUploads.Models;

/// <summary>
/// One named upload (e.g. "MyApp"), containing all its versions and diffs.
/// </summary>
public class AppEntry
{
    public string Name { get; init; } = "";
    public List<VersionEntry> Versions { get; init; } = [];
    public List<DiffEntry> Diffs { get; init; } = [];
}
