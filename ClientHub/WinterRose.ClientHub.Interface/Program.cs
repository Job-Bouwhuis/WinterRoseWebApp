using Gtk;

using Gtk;
using System;
using System.Net.Http;
using System.Threading.Tasks;


public class AppResponse
{
    public List<AppDto> Data { get; set; }
}

public class AppDto
{
    public string Name { get; set; }
    public List<VersionDto> Versions { get; set; }
    public List<DiffDto> Diffs { get; set; }
}

public class VersionDto
{
    public string VersionLabel { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Version { get; set; }
}

public class DiffDto
{
    public string FromVersion { get; set; }
    public string ToVersion { get; set; }
    public string FileName { get; set; }
}

class Program
{
    static readonly HttpClient HTTP_CLIENT = new HttpClient();
    static TreeView treeView = new TreeView();
    static ListStore store;

    static void Main(string[] args)
    {
        Application.Init();

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
            string json = await HTTP_CLIENT.GetStringAsync("http://localhost:5089/apps");

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = System.Text.Json.JsonSerializer.Deserialize<AppResponse>(json, options);

            if (response?.Data == null)
                return;

            foreach (var app in response.Data)
            {
                store.AppendValues(app.Name, "APP", "", "");

                foreach (var version in app.Versions ?? new List<VersionDto>())
                {
                    store.AppendValues(
                        "",
                        "VERSION",
                        version.VersionLabel,
                        version.UploadedAt.ToString("u")
                    );
                }

                foreach (var diff in app.Diffs ?? new List<DiffDto>())
                {
                    store.AppendValues(
                        "",
                        "DIFF",
                        $"{diff.FromVersion} → {diff.ToVersion}",
                        diff.FileName
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