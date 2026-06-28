using Microsoft.AspNetCore.Components.Forms;

namespace WinterRose.WebServer.Features.FileUploads.Models;

public class UploadedFileWithPath
{
    public IBrowserFile File { get; set; } = default!;
    public string RelativePath { get; set; } = "";
}