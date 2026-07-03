using System.Buffers;
using System.Net;
using WinterRose.Nexus.Shared;
using WinterRose.WinterForgeSerializing;
#pragma warning disable CA2024

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

    private bool GetCurrentVersionTag(out string tag)
    {
       // DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
       DirectoryInfo dir =
           new DirectoryInfo(
               "D:\\GitRepositories\\Personal\\WinterRoseWebApp\\ClientHub\\WinterRose.Nexus\\bin\\Debug\\net10.0\\" +
               "apps\\f2a32f4a9181ee3733b8c8353460ef6b057ea26a\\app");
       
        tag = "*";
        while (dir.Name != "app")
        {
            dir = dir.Parent;
            if (dir == null)
                return false;
        }

        dir = dir.Parent;
        if (dir == null)
            return false;

        FileInfo? appdetailsFile = dir.GetFiles(".localappdetails").FirstOrDefault();
        if (appdetailsFile == null)
            return false;

        _ = typeof(LocalAppEntry);
        object? res = WinterForge.DeserializeFromHumanReadableFile(appdetailsFile.FullName);
        if(res is not LocalAppEntry entry)
            return false;
        
        tag = entry.InstalledVersion.Tag;
        if (string.IsNullOrWhiteSpace(tag))
            tag = "release";
        return true;
    }

    private async Task ListenAsync()
    {
        if (!GetCurrentVersionTag(out string tag))
        {
            Console.Error.WriteLine("No version tag found, listening for all release branches!");
        }
        else
        {
            Console.WriteLine($"Listening for new versions on the {tag} branch!");
        }

        HttpResponseMessage r;
        try
        {
            r = await httpClient.GetAsync(
                $"https://localhost:7184/versions/event/{appId}/{tag}",
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (HttpRequestException e)
        {
            Console.Error.WriteLine("Nexus Registry not available.");
            return;
        }

        using var response = r;

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine("Nexus Registry not available.");
            return;
        }

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
                    catch
                    {
                        // we swallow exceptions here. this is a background "nicity" an app can use, not a reliance
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