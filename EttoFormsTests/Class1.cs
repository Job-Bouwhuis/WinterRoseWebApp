using Eto.Forms;
using WinterRose.Nexus.SDK;
using WinterRose.Nexus.Shared;

namespace EttoFormsTests;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using Nexus nexus = new Nexus("f2a32f4a9181ee3733b8c8353460ef6b057ea26a");

        nexus.EnsureLatestVersion(args);

        var app = new Application(new Eto.GtkSharp.Platform());
        var form = new Form();
        form.Width = 500;
        form.Height = 500;
        
        var statusLabel = new Label
        {
            Text = "no new version yet"
        };

        nexus.OnNewVersionAvailable += version =>
        {
            Application.Instance.Invoke(() =>
            {
                statusLabel.Text = version.ToString();
            });
        };

        form.Content = new StackLayout
        {
            Padding = 10,
            Spacing = 10,
            Items =
            {
                statusLabel
            }
        };

        app.MainForm = form;
        form.Show();

        app.Run();
    }
}