using System.Net.Sockets;
using System.Text;
using WinterRose.DependancyInjection;

namespace WinterRose.Uris;

[LinuxOnly]
public class LinuxUriForwarder : IUriForwarder
{
    private readonly string SOCKET_PATH;

    public LinuxUriForwarder(UriOptions options)
    {
        SOCKET_PATH = $"/tmp/winterrose_uri_{options.AppId}.sock";
    }

    public async Task ForwardAsync(string uri)
    {
        using Socket client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        UnixDomainSocketEndPoint endpoint = new UnixDomainSocketEndPoint(SOCKET_PATH);

        await client.ConnectAsync(endpoint);

        byte[] data = Encoding.UTF8.GetBytes(uri);

        await client.SendAsync(data, SocketFlags.None);
    }
}