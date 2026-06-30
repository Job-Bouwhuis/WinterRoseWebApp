using Microsoft.AspNetCore.Components.Forms;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Models;

public class UploadedFileWithPath
{
    public IBrowserFile File { get; set; } = default!;
    public string RelativePath { get; set; } = "";
}