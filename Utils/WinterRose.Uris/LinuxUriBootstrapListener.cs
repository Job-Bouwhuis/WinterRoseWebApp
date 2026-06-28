using System.Net.Sockets;
using System.Text;
using WinterRose.DependancyInjection;

namespace WinterRose.Uris;

[LinuxOnly]
public class LinuxUriBootstrapListener : IUriBootstrapListener
{
    private readonly string SOCKET_PATH;

    public LinuxUriBootstrapListener(string appId)
    {
        SOCKET_PATH = $"/tmp/winterrose_uri_{appId}.sock";
    }

    public void StartListening(Action<string> onUri)
    {
        Task.Run(async () =>
        {
            if (File.Exists(SOCKET_PATH))
                File.Delete(SOCKET_PATH);

            using Socket server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            UnixDomainSocketEndPoint endpoint = new UnixDomainSocketEndPoint(SOCKET_PATH);

            server.Bind(endpoint);
            server.Listen(5);

            while (true)
            {
                using Socket client = await server.AcceptAsync();

                byte[] buffer = new byte[8192];
                int received = await client.ReceiveAsync(buffer, SocketFlags.None);

                if (received <= 0)
                    continue;

                string uri = Encoding.UTF8.GetString(buffer, 0, received);

                if (!string.IsNullOrWhiteSpace(uri))
                    onUri(uri);
            }
        });
    }
}