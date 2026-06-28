using System.IO.Pipes;
using System.Text;
using WinterRose.DependancyInjection;
using WinterRose.Uris.UriVerifiers;

namespace WinterRose.Uris;

[WindowsOnly]
public class WindowsUriBootstrapListener : IUriBootstrapListener
{
    private readonly UriOptions options;
    private readonly IUriSchemeRegistar registar;
    private readonly string PIPE_NAME;

    public WindowsUriBootstrapListener(UriOptions options, IUriSchemeRegistar registar)
    {
        this.options = options;
        this.registar = registar;
        PIPE_NAME = $"winterrose_uri_{options.AppId}";
    }

    public void StartListening(Func<string, Task> onUri, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            await registar.Validate(options);
            
            while (!ct.IsCancellationRequested)
            {
                using NamedPipeServerStream pipe = new NamedPipeServerStream(
                    PIPE_NAME,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                await pipe.WaitForConnectionAsync(ct);

                using StreamReader reader = new StreamReader(pipe, Encoding.UTF8);

                string uri = await reader.ReadToEndAsync(ct);

                if (!string.IsNullOrWhiteSpace(uri))
                    await onUri(uri);
            }
        });
    }
}