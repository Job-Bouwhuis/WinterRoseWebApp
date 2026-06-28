using System.IO.Pipes;
using System.Text;
using WinterRose.DependancyInjection;

namespace WinterRose.Uris;

[WindowsOnly]
public class WindowsUriForwarder : IUriForwarder
{
    private readonly string PIPE_NAME;

    public WindowsUriForwarder(string appId)
    {
        PIPE_NAME = $"winterrose_uri_{appId}";
    }

    public async Task ForwardAsync(string uri)
    {
        using NamedPipeClientStream client = new NamedPipeClientStream(
            ".",
            PIPE_NAME,
            PipeDirection.Out
        );

        await client.ConnectAsync(1500);

        using StreamWriter writer = new StreamWriter(client, Encoding.UTF8);

        await writer.WriteAsync(uri);
        await writer.FlushAsync();
    }
}