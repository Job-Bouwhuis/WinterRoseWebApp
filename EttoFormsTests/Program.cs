using Eto.Forms;
using WinterRose.Nexus.SDK;
using WinterRose.Nexus.Shared;

namespace EttoFormsTests;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Nexus nexus = new Nexus("833a95b17fad79e63b2356b089a1497404a0a08d");
        
        Console.WriteLine($"Args: [{string.Join(", ", args)}]");
        Console.WriteLine("This is version 1.0.2");
        
        Console.WriteLine("Hello World!");
        //nexus.EnsureLatestVersion(args);
        
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