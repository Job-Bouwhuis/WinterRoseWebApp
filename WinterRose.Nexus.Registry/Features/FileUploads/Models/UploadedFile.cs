namespace WinterRose.Nexus.Registry.Features.FileUploads.Models;

public class UploadedFile
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public string Type { get; set; } = "";
}
