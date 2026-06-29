using System.Threading.Tasks;
using WinterRose.Applications;
using WinterRose.Uris;

namespace WinterRose.ClientHub.Feature.Uri;

public class ShutdownUriHandler(IApplication app) : IUriHandler
{
    public bool CanHandle(UriContext context)
    {
        return context.Command == "shutdown";
    }

    public Task HandleAsync(UriContext context)
    {
        app.Stop();
        return Task.CompletedTask;
    }
}