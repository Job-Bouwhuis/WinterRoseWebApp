namespace WinterRose.Nexus.Shared;

/// <summary>
/// One named upload (e.g. "MyApp"), containing all its versions and diffs.
/// </summary>
public class AppEntry
{
    public string AppId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string ShortDescription { get; set; } = "";

    public string LongDescription { get; set; } = "";

    /// <summary>
    /// Free-form tags such as "Game", "Multiplayer", "Internal Tool".
    /// </summary>
    public List<string> Tags { get; init; } = [];

    public List<AppVersion> Versions { get; init; } = [];

    public List<DiffEntry> Diffs { get; init; } = [];
}
