using System.Threading.Tasks;
using WinterRose.Nexus.Interface.Windows;
using WinterRose.Uris;

namespace WinterRose.Nexus.Interface;

public class ShowWindowUriHandler(UiManager ui) : IUriHandler
{
    public string Command { get; set; }
    public bool CanHandle(UriContext context)
    {
        return context.Command == "show-window";
    }

    public Task HandleAsync(UriContext context)
    {
        if (context.Query.Count == 0)
        {
            ui.Show<ApplicationStoreWindow>();
        }

        return Task.CompletedTask;
    }
}