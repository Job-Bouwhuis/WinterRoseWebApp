using System.Net;
using System.Net.Sockets;
using System.Text;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris.UriVerifiers;

namespace WinterRose.Uris;

[WindowsOnly]
public class WindowsUriBootstrapListener : IUriBootstrapListener, IDisposable
{
    private readonly UriOptions options;
    private readonly IUriSchemeRegistar registar;
    private readonly int port;
    private Socket? serverSocket;
    private Task? listenerTask;
    private readonly CancellationTokenSource shutdownCts = new();

    public WindowsUriBootstrapListener(UriOptions options, IUriSchemeRegistar registar)
    {
        this.options = options;
        this.registar = registar;
        port = GetStablePort(options.AppId);
    }

    private static int GetStablePort(string appId)
    {
        int hash = 0;
        foreach (char c in appId)
            hash = hash * 31 + c;
        return 5123 + Math.Abs(hash) % 1000;
    }

    public void Dispose()
    {
        shutdownCts.Cancel();
        serverSocket?.Close(); // unblocks AcceptAsync
        shutdownCts.Dispose();
    }

    public void StartListening(Func<string, Task> onUri, CancellationToken ct)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, shutdownCts.Token);

        listenerTask = Task.Run(async () =>
        {
            await registar.Validate(options);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            serverSocket.Listen(10);


            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    Socket client;
                    try
                    {
                        client = await serverSocket.AcceptAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        break; // serverSocket.Close() was called
                    }

                    _ = Task.Run(async () =>
                    {
                        using Socket c = client;
                        try
                        {
                            // read 4 byte length prefix
                            byte[] lengthBuf = new byte[4];
                            await c.ReceiveExactlyAsync(lengthBuf, CancellationToken.None);
                            int length = BitConverter.ToInt32(lengthBuf);

                            // read message
                            byte[] messageBuf = new byte[length];
                            await c.ReceiveExactlyAsync(messageBuf, CancellationToken.None);
                            string uri = Encoding.UTF8.GetString(messageBuf);

                            // ack
                            await c.SendAsync(new byte[] { 0x01 }, SocketFlags.None, CancellationToken.None);
                            c.Shutdown(SocketShutdown.Both);

                            if (!string.IsNullOrWhiteSpace(uri))
                                await onUri(uri);
                        }
                        catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException)
                        {
                            // client disconnected or shutting down
                        }
                    });
                }
            }
            finally
            {
                serverSocket.Close();
                linkedCts.Dispose();
            }
        });
    }
}

public static class SocketExtensions
{
    public static async Task ReceiveExactlyAsync(this Socket socket, byte[] buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await socket.ReceiveAsync(
                buffer.AsMemory(totalRead, buffer.Length - totalRead),
                SocketFlags.None,
                ct);

            if (read == 0)
                throw new IOException("Connection closed before all bytes were received");

            totalRead += read;
        }
    }
}