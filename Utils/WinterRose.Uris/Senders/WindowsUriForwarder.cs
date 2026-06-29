using System.Net;
using System.Net.Sockets;
using System.Text;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;

namespace WinterRose.Uris;

[WindowsOnly]
public class WindowsUriForwarder : IUriForwarder
{
    private readonly int port;
    private readonly ILogger<WindowsUriForwarder> logger;

    public WindowsUriForwarder(UriOptions options, ILogger<WindowsUriForwarder> logger)
    {
        port = GetStablePort(options.AppId);
        this.logger = logger;
    }

    private static int GetStablePort(string appId)
    {
        int hash = 0;
        foreach (char c in appId)
            hash = hash * 31 + c;
        return 5123 + Math.Abs(hash) % 1000;
    }

    public async Task ForwardAsync(string uri)
    {
        try
        {
            logger.Info($"Forwarding to port {port}");
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            await socket.ConnectAsync(IPAddress.Loopback, port, CancellationToken.None);
            logger.Info($"Connected to listener, sending {uri.Length} chars");
            
            byte[] messageBytes = Encoding.UTF8.GetBytes(uri);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            await socket.SendAsync(lengthBytes, SocketFlags.None, CancellationToken.None);
            await socket.SendAsync(messageBytes, SocketFlags.None, CancellationToken.None);

            // wait for ack then close
            byte[] ack = new byte[1];
            await socket.ReceiveAsync(ack, SocketFlags.None, CancellationToken.None);

            if (ack[0] != 0x01)
                logger.Error($"Unexpected ack byte {ack[0]} from listener");

            socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error while trying to forward uri {uri}");
        }
    }
}