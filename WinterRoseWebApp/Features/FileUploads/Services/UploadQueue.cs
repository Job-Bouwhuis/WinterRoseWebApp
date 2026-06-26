using System.Threading.Channels;

namespace WinterRoseWebApp.Features.FileUploads.Services;

public record UploadCompletedEvent(
    string Name,
    string BasePath
);

public sealed class UploadQueue
{
    private readonly Channel<UploadCompletedEvent> _channel =
        Channel.CreateUnbounded<UploadCompletedEvent>(
            new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<UploadCompletedEvent> Writer => _channel.Writer;
    public ChannelReader<UploadCompletedEvent> Reader => _channel.Reader;
}
