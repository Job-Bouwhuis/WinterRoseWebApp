using System.Net.Sockets;
using System.Text;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris.UriVerifiers;

namespace WinterRose.Uris;

[LinuxOnly]
public class LinuxUriBootstrapListener : IUriBootstrapListener
{
    private readonly UriOptions options;
    private readonly IUriSchemeRegistar registar;
    private readonly ILogger<LinuxUriBootstrapListener> logger;
    private readonly string SOCKET_PATH;

    public LinuxUriBootstrapListener(UriOptions options, IUriSchemeRegistar registar, ILogger<LinuxUriBootstrapListener> logger)
    {
        this.options = options;
        this.registar = registar;
        this.logger = logger;
        SOCKET_PATH = $"/tmp/winterrose_uri_{options.AppId}.sock";
    }

    public void StartListening(Func<string, Task> onUri, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            await registar.Validate(options);
            
            if (File.Exists(SOCKET_PATH))
                File.Delete(SOCKET_PATH);

            using Socket server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            UnixDomainSocketEndPoint endpoint = new UnixDomainSocketEndPoint(SOCKET_PATH);

            server.Bind(endpoint);
            server.Listen(5);

            logger.Info($"Server bound to {endpoint}");
            
            while (!ct.IsCancellationRequested)
            {
                using Socket client = await server.AcceptAsync(ct);

                byte[] buffer = new byte[8192];
                int received = await client.ReceiveAsync(buffer, SocketFlags.None);

                if (received <= 0)
                    continue;

                string uri = Encoding.UTF8.GetString(buffer, 0, received);

                if (!string.IsNullOrWhiteSpace(uri))
                    await onUri(uri);
            }
        });
    }
}