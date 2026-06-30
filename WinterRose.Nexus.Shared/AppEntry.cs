namespace WinterRose.Nexus.Shared;

/// <summary>
/// One named upload (e.g. "MyApp"), containing all its versions and diffs.
/// </summary>
public class AppEntry
{
    public string Name { get; init; } = "";

    public string DisplayName { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string ShortDescription { get; set; } = "";

    public string LongDescription { get; set; } = "";

    /// <summary>
    /// Relative path to the application's icon within Nexus storage.
    /// Empty when no icon has been uploaded.
    /// </summary>
    public string IconPath { get; set; } = "";

    /// <summary>
    /// Free-form tags such as "Game", "Multiplayer", "Internal Tool".
    /// </summary>
    public List<string> Tags { get; init; } = [];

    public List<AppVersion> Versions { get; init; } = [];

    public List<DiffEntry> Diffs { get; init; } = [];
}
