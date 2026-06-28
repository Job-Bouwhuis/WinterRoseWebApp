using System.Threading.Tasks;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris;

namespace WinterRose.ClientHub.Feature.Uri;

public class Handler(ILogger<Handler> logger) : IUriHandler
{
    
    public string Command { get; set; }
    public bool CanHandle(UriContext context)
    {
        return true;
    }

    public Task HandleAsync(UriContext context)
    {
        logger.Info($"Received a URI: {context}");
        return Task.CompletedTask;
    }
}