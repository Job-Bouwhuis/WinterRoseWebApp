using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public record UploadCompletedEvent(
    string Name,
    string BasePath,
    AppVersion AppVersion
);