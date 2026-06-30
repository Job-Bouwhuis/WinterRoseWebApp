using System;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.ProgressKeeping;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Nexus.Interface.Windows;

public class InstallationProgressWindow : WindowBase
{
    private readonly MainThread main;
    private Label statusLabel;
    private ProgressBar progressBar;
    private ListBox logList;

    public Action OnComplete { get; set; }
    
    public InstallationProgressWindow(MainThread main, IServiceProvider services)
        : base("Install progress", main, services)
    {
        this.main = main;
        this.Content = BuildContent();
        
        main.Invoke(() =>
        {
            Width = 750;
            Height = 750;
        });

        OnComplete = Close;
    }

    protected override Control BuildContent()
    {
        statusLabel = new Label { Text = "Waiting..." };

        progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0
        };

        logList = new ListBox();

        var layout = new TableLayout
        {
            Padding = 10,
            Spacing = new Size(5, 5)
        };

        layout.Rows.Add(statusLabel);
        layout.Rows.Add(progressBar);
        layout.Rows.Add(new TableRow(logList));

        return layout;
    }

    public IProgressScope CreateUiScope()
    {
        return new ProgressScope(ReportProgress);
    }

    private void ReportProgress(double value, string message)
    {
        main.Invoke(() =>
        {
            progressBar.Value = (int)(value * 100);

            if (!string.IsNullOrEmpty(message))
            {
                statusLabel.Text = message;
                logList.Items.Insert(0, new ListItem { Text = message });
                logList.SelectedIndex = 0;
                
                if (logList.Items.Count > 2000)
                    logList.Items.RemoveAt(logList.Items.Count - 1);
            }

            if (progressBar.Value == 100)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    OnComplete?.Invoke();
                });
            }
        });
    }
}