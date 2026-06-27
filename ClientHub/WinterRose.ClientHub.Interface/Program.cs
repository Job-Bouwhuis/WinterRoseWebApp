using Gtk;
using WinterRose.Web;
using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRose.ClientHub.Interface;

class Program
{
    static readonly HttpClient HTTP_CLIENT = new HttpClient();
    static TreeView treeView = new TreeView();
    static ListStore store;

    static void Main(string[] args)
    {
        Application.Init();

        HTTP_CLIENT.DefaultRequestHeaders.Accept.Clear();
        HTTP_CLIENT.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/winterforge")
        );

        Window window = new Window("API Viewer");
        window.SetDefaultSize(700, 500);

        window.DeleteEvent += (o, e) => Application.Quit();

        Box layout = new Box(Orientation.Vertical, 8);

        Button loadButton = new Button("Load Apps");

        store = new ListStore(
            typeof(string), // App
            typeof(string), // Type (Version / Diff)
            typeof(string), // Details
            typeof(string)  // Extra info
        );

        treeView.Model = store;

        treeView.AppendColumn("App", new CellRendererText(), "text", 0);
        treeView.AppendColumn("Type", new CellRendererText(), "text", 1);
        treeView.AppendColumn("Info", new CellRendererText(), "text", 2);
        treeView.AppendColumn("Extra", new CellRendererText(), "text", 3);

        loadButton.Clicked += async (sender, e) =>
        {
            await LoadDataAsync();
        };

        layout.PackStart(loadButton, false, false, 0);
        layout.PackStart(treeView, true, true, 0);

        window.Add(layout);
        window.ShowAll();

        Application.Run();
    }

    static async Task LoadDataAsync()
    {
        store.Clear();

        try
        {
            var response = await HTTP_CLIENT.GetFromWinterForge<APIResponse>("http://localhost:5089/apps");
            if (response?.Data is not List<AppEntry> appSummaries)
                return;

            foreach (var app in appSummaries)
            {
                store.AppendValues(app.Name, "APP", "", "");

                foreach (var version in app.Versions)
                {
                    string tagDisplay = string.IsNullOrEmpty(version.Tag)
                        ? "release"
                        : version.Tag;

                    store.AppendValues(
                        "",
                        "VERSION",
                        $"{version.Major}.{version.Minor}.{version.Patch} [{tagDisplay}]",
                        version.UploadedAt.ToString("D")
                    );
                }

                foreach (var diff in app.Diffs)
                {
                    store.AppendValues(
                        "",
                        "DIFF",
                        $"{diff.FromVersion} → {diff.ToVersion}",
                        System.IO.Path.GetFileName(diff.FilePath)
                    );
                }
            }

        }
        catch (Exception ex)
        {
            store.AppendValues("ERROR", ex.Message, "", "");
        }
    }
}