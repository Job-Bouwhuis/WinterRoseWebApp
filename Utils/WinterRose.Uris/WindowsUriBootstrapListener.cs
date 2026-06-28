using System.IO.Pipes;
using System.Text;
using WinterRose.DependancyInjection;

namespace WinterRose.Uris;

[WindowsOnly]
public class WindowsUriBootstrapListener : IUriBootstrapListener
{
    private readonly string PIPE_NAME;

    public WindowsUriBootstrapListener(string appId)
    {
        PIPE_NAME = $"winterrose_uri_{appId}";
    }

    public void StartListening(Action<string> onUri)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                using NamedPipeServerStream pipe = new NamedPipeServerStream(
                    PIPE_NAME,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                await pipe.WaitForConnectionAsync();

                using StreamReader reader = new StreamReader(pipe, Encoding.UTF8);

                string uri = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(uri))
                    onUri(uri);
            }
        });
    }
}

