using System.Threading.Channels;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Registry.Features.FileUploads.Services;

public record UploadCompletedEvent(
    string Name,
    string BasePath,
    AppVersion AppVersion
);

public sealed class UploadQueue
{
    private readonly Channel<UploadCompletedEvent> _channel =
        Channel.CreateUnbounded<UploadCompletedEvent>(
            new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<UploadCompletedEvent> Writer => _channel.Writer;
    public ChannelReader<UploadCompletedEvent> Reader => _channel.Reader;
}
