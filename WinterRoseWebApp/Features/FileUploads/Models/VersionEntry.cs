namespace WinterRoseWebApp.Features.FileUploads.Models;

/// <summary>
/// A single version folder, e.g. 1_0_0 inside /Uploads/MyApp/
/// </summary>
public class VersionEntry
{
    public string VersionLabel { get; init; } = "";   // "1_0_0"
    public DateTime UploadedAt { get; init; }
}
