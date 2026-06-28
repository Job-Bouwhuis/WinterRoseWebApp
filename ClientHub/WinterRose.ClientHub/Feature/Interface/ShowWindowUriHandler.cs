using System.Threading.Tasks;
using WinterRose.ClientHub.Feature.Interface.Windows;
using WinterRose.Uris;

namespace WinterRose.ClientHub.Feature.Interface;

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
            ui.Show<TestWindow>();
        }

        return Task.CompletedTask;
    }
}