using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.Uris;

public class ApplicationUpdateUriHandler(ApplicationStarter installer) : IUriHandler
{
    private const string UPDATE_APPLICATION_COMMAND = "update-application";

    public bool CanHandle(UriContext context)
    {
        return context.Command == UPDATE_APPLICATION_COMMAND;
    }

    public async Task HandleAsync(UriContext context)
    {
        string? appId = context.Query["id"];

        installer.Start(appId, context.Query["auto-start"] == "true");
    }
}