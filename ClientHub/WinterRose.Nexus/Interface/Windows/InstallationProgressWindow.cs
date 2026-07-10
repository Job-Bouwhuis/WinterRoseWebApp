using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.ProgressKeeping;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Nexus.Interface.Windows;

public class InstallationProgressWindow : WindowBase
{
    private enum ProgressState
    {
        CheckingUpdates,
        Updating,
        Starting,
        Idle
    }

    private static readonly Color DebugColor = Color.FromArgb(110, 110, 120);
    private static readonly Color InfoColor = ThemeManager.NexusPalette.TEXT;
    private static readonly Color TextMuted = ThemeManager.NexusPalette.TEXT_MUTED;
    private static readonly Color SuccessColor = Color.FromArgb(120, 255, 120);
    private static readonly Color WarningColor = Color.FromArgb(255, 225, 80);
    private static readonly Color ErrorColor = Color.FromArgb(255, 120, 120);

    private readonly MainThread main;
    private readonly string appTitle;

    private Label titleLabel;
    private Label statusLabel;
    private Label percentLabel;
    private ProgressBar progressBar;
    private Scrollable logScroll;
    private StackLayout logStack;

    private const int LOG_MAX_ENTRIES = 50;

    private readonly LogEntry[] logBuffer = new LogEntry[LOG_MAX_ENTRIES];
    private int logWriteIndex;
    private int logCount;
    private bool logNeedsRefresh;
    
    private sealed class LogRow
    {
        public Label TimeLabel;
        public Label MessageLabel;
        public StackLayout Root;
    }
    
    private readonly Stack<LogRow> logRowPool = new();
    private readonly List<LogRow> activeRows = new();
    
    private readonly List<LogEntry> logEntries = [];

    private sealed record LogEntry(
        DateTime Timestamp,
        string Message,
        ReportStatus Status);

    private Panel statusDot;

    private bool showing = true;
    
    private ProgressState state = ProgressState.CheckingUpdates;

    public InstallationProgressWindow(
        MainThread main,
        IServiceProvider services,
        string appTitle = "Application",
        string windowTitle = null)
        : base(windowTitle ?? $"{appTitle} — Install Progress", main, services)
    {
        this.main = main;
        this.appTitle = appTitle;

        main.Invoke(() =>
        {
            Content = BuildContent();
            Width = 750;
            Height = 750;
            MinimumSize = new Size(560, 480);
            Resizable = true;
        });

        StartLogRefreshLoop();
    }
    
    private async Task StartLogRefreshLoop()
    {
        while (showing)
        {
            if (logNeedsRefresh)
            {
                logNeedsRefresh = false;
                main.Invoke(() => RefreshLogUi());
            }

            await Task.Delay(16); // ~20 FPS UI updates
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        showing = false;
        base.OnClosing(e);
    }

    private static string GetReportStyle(ReportStatus status)
    {
        return status switch
        {
            ReportStatus.Debug => "log-debug",
            ReportStatus.Info => "log-info",
            ReportStatus.Success => "log-success",
            ReportStatus.Warning => "log-warning",
            ReportStatus.Error => "log-error",
            _ => "log-info"
        };
    }

    public void ShowCheckingUpdates()
    {
        main.Invoke(() =>
        {
            state = ProgressState.CheckingUpdates;

            SetStatus("Checking for updates...", TextMuted);
            progressBar.Value = 0;
            SetDot(TextMuted);

            logEntries.Clear();
            logStack.Items.Clear();
        });
    }

    public void ShowUpdateComplete()
    {
        main.Invoke(() =>
        {
            state = ProgressState.Idle;
            ReportProgress(1, "Update complete!");
        });
    }

    public void ShowStartingApp()
    {
        main.Invoke(() =>
        {
            state = ProgressState.Starting;
            AddLogEntry("starting called");
            SetStatus("Starting app...", SuccessColor);
            ReportProgress(1, "Starting app...");
            SetDot(SuccessColor);
        });
    }


    public void ShowUpdating()
    {
        main.Invoke(() =>
        {
            state = ProgressState.Updating;

            SetStatus("Updating...", ThemeManager.NexusPalette.ACCENT);
            progressBar.Value = 0;
            SetDot(ThemeManager.NexusPalette.ACCENT);
        });
    }

    protected override Control BuildContent()
    {
        // Header
        titleLabel = new Label
        {
            Text = appTitle,
            Font = new Font(SystemFont.Bold, 16)
        };

        statusDot = new Panel
        {
            Size = new Size(10, 10),
            BackgroundColor = TextMuted
        };

        statusLabel = new Label
        {
            Text = "Waiting...",
            Font = new Font(SystemFont.Default, 11),
            TextColor = TextMuted
        };

        var statusRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            VerticalContentAlignment = VerticalAlignment.Center,
            Spacing = 8,
            Items = { statusDot, statusLabel }
        };

        var headerStack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Items = { titleLabel, statusRow }
        };

        var header = new Panel
        {
            Padding = new Padding(20, 18, 20, 18),
            Content = headerStack
        };

        // Progress bar (custom-drawn for full styling control)
        percentLabel = new Label
        {
            Text = "0%",
            Font = new Font(SystemFont.Bold, 11),
            TextColor = TextMuted,
            TextAlignment = TextAlignment.Right
        };

        progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0
        };

        var progressStack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Items =
            {
                new StackLayoutItem(progressBar, HorizontalAlignment.Stretch),
                new StackLayoutItem(percentLabel, HorizontalAlignment.Right)
            }
        };

        var progressPanel = new Panel
        {
            Padding = new Padding(20, 16, 20, 16),
            Content = progressStack
        };

        // Log list
        logStack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };

        logScroll = new Scrollable
        {
            Border = BorderType.None,
            Content = logStack
        };

        var logHeader = new Label
        {
            Text = "ACTIVITY LOG",
            Font = new Font(SystemFont.Bold, 9),
            TextColor = TextMuted
        };

        var copyButton = new Button
        {
            Text = "Copy Log"
        };

        copyButton.Click += async (_, _) => await CopyHistoryToClipboardAsync();

        var headerRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Items =
            {
                new StackLayoutItem(logHeader, true),
                copyButton
            }
        };

        var logPanel = new Panel
        {
            Padding = new Padding(20, 12, 20, 20),
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    headerRow,
                    new StackLayoutItem(logScroll, true)
                }
            }
        };

        var body = new TableLayout
        {
            Rows =
            {
                new TableRow(progressPanel),
                new TableRow(logPanel) { ScaleHeight = true }
            }
        };

        var root = new TableLayout
        {
            BackgroundColor = BackgroundColor,
            Rows =
            {
                new TableRow(header),
                new TableRow(body) { ScaleHeight = true }
            }
        };

        return root;
    }

    public IProgressScope CreateUiScope()
    {
        return new ProgressScope(ReportProgressAsync);
    }

    public Task ReportProgressAsync(
        double value,
        string message,
        ReportStatus status = ReportStatus.Info)
    {
        return main.InvokeAsync(() => ReportProgress(value, message, status));
    }

    public void ReportProgress(
        double value,
        string message,
        ReportStatus status = ReportStatus.Info)
    {
        progressBar.Value = (int)Math.Round(value);
        //AddLogEntry(progressBar.Value.ToString(), status);
        if (!string.IsNullOrWhiteSpace(message))
        {
            statusLabel.Text = message;
            if(status != ReportStatus.Info)
                AddLogEntry(message, status);
        }
    }

    private void Complete()
    {
        Close();
    }

    private void SetStatus(string text, Color color)
    {
        statusLabel.Text = text;
        statusLabel.TextColor = color;
    }

    private void SetDot(Color color)
    {
        statusDot.BackgroundColor = color;
    }

    private void AddLogEntry(string message, ReportStatus status = ReportStatus.Info)
    {
        var entry = new LogEntry(DateTime.Now, message, status);

        logBuffer[logWriteIndex] = entry;

        logWriteIndex = (logWriteIndex + 1) % LOG_MAX_ENTRIES;
        logCount = Math.Min(logCount + 1, LOG_MAX_ENTRIES);

        logNeedsRefresh = true;
    }
    
    private void RefreshLogUi()
    {
        logStack.Items.Clear();

        int index = logWriteIndex - 1;
        if (index < 0)
            index = LOG_MAX_ENTRIES - 1;

        for (int i = 0; i < logCount; i++)
        {
            LogEntry entry = logBuffer[index];
            index--;
            if (index < 0)
                index = LOG_MAX_ENTRIES - 1;

            var row = GetOrCreateRow(entry);
            logStack.Items.Add(row.Root);
        }

        logNeedsRefresh = false;
    }
    
    private LogRow GetOrCreateRow(LogEntry entry)
    {
        if (!logRowPool.TryPop(out LogRow row))
        {
            row = new LogRow
            {
                TimeLabel = new Label
                {
                    Width = 70,
                    TextColor = TextMuted,
                    Font = new Font(SystemFont.Default, 9),
                    VerticalAlignment = VerticalAlignment.Center
                },
                MessageLabel = new Label
                {
                    Font = new Font(SystemFont.Default, 10),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            row.Root = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    row.TimeLabel,
                    new StackLayoutItem(row.MessageLabel, true)
                }
            };
        }

        row.TimeLabel.Text = entry.Timestamp.ToString("HH:mm:ss");
        row.MessageLabel.Text = entry.Message;
        row.MessageLabel.Style = GetReportStyle(entry.Status);

        return row;
    }
    
    private void ReturnRowsToPool()
    {
        foreach (var row in activeRows)
            logRowPool.Push(row);

        activeRows.Clear();
    }

    public async Task CopyHistoryToClipboardAsync()
    {
        var builder = new StringBuilder();

        for (int i = logEntries.Count - 1; i >= 0; i--)
        {
            LogEntry entry = logEntries[i];

            builder.Append('[');
            builder.Append(entry.Timestamp.ToString("HH:mm:ss"));
            builder.Append("] [");
            builder.Append(entry.Status);
            builder.Append("] ");
            builder.AppendLine(entry.Message);
        }

        Clipboard.Instance.Text = builder.ToString();
    }
}