namespace WinterRoseWebApp.Features.FileUploads.Models;

/// <summary>
/// A single version folder, e.g. 1_0_0 inside /Uploads/MyApp/
/// </summary>
public class VersionEntry
{
    public string VersionLabel { get; init; } = "";   // "1_0_0"
    public DateTime UploadedAt { get; init; }
    public Version Version
    {
        get
        {
            if (field is null)
            {
                var versionParts = VersionLabel.Split('_');
                if (versionParts.Length != 3)
                    throw new FormatException($"Invalid version format: {VersionLabel}");
                if (!int.TryParse(versionParts[0], out int major) ||
                    !int.TryParse(versionParts[1], out int minor) ||
                    !int.TryParse(versionParts[2], out int patch))
                {
                    throw new FormatException($"Invalid version format: {VersionLabel}");
                }
                field = new Version(major, minor, patch);
            }
            return field;
        }
    }
}
