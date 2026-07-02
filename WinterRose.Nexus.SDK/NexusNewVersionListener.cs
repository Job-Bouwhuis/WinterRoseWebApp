using System.Buffers;
using WinterRose.Nexus.Shared;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.SDK;

using System.Net.Http;

internal sealed class NexusNewVersionListener(string appId) : IDisposable
{
    private readonly HttpClient httpClient = new();

    private CancellationTokenSource cts = new();

    public event Action<AppVersion> OnMessage = delegate { };

    private Task? listeningTask;

    public void Start()
    {
        listeningTask = Task.Run(ListenAsync).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Console.Error.WriteLine(t.Exception);
            }
        });
    }

    private async Task ListenAsync()
    {
        Console.WriteLine(appId);
        using var response = await httpClient.GetAsync(
            $"https://localhost:7184/versions/event/{appId}",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        var buffer = new ArrayBufferWriter<byte>();

        while (!cts.Token.IsCancellationRequested && !reader.EndOfStream)
        {
            int value = reader.Read();

            if (value == -1)
                break;

            byte b = (byte)value;

            if (b > 250)
            {
                if (buffer.WrittenCount > 0)
                {
                    using MemoryStream mem = new MemoryStream(buffer.WrittenMemory.ToArray());
                    buffer.Clear();
                    
                    try
                    {
                        object? rawPayload = WinterForge.DeserializeFromHumanReadableStream(mem);
                        if (rawPayload is not AppVersion appVersion)
                            continue;

                        OnMessage.Invoke(appVersion);
                    }
                    catch (Exception e)
                    {
                    }
                }

                continue;
            }

            var span = buffer.GetSpan(1);
            span[0] = b;
            buffer.Advance(1);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}