using System;
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

    // Palette
    private static Color BackgroundColor = Color.FromArgb(24, 26, 31);
    private static readonly Color PanelColor = Color.FromArgb(32, 35, 41);
    private static readonly Color BorderColor = Color.FromArgb(48, 51, 58);
    private static readonly Color TextPrimary = Color.FromArgb(235, 236, 238);
    private static readonly Color TextMuted = Color.FromArgb(150, 154, 162);
    private static readonly Color AccentColor = Color.FromArgb(88, 145, 255);
    private static readonly Color AccentColorDim = Color.FromArgb(60, 100, 180);
    private static readonly Color SuccessColor = Color.FromArgb(88, 200, 140);

    private readonly MainThread main;
    private readonly string appTitle;

    private Label titleLabel;
    private Label statusLabel;
    private Label percentLabel;
    private Drawable progressBar;
    private ListBox logList;
    private Panel statusDot;

    private ProgressState state = ProgressState.CheckingUpdates;
    private double progressFraction;

    public Action? OnComplete { get; set; } 

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
            BackgroundColor = InstallationProgressWindow.BackgroundColor;
            Resizable = true;
            
        });
    }

    public void ShowCheckingUpdates()
    {
        main.Invoke(() =>
        {
            state = ProgressState.CheckingUpdates;

            SetStatus("Checking for updates...", TextMuted);
            SetProgress(0);
            SetDot(TextMuted);

            logList.Items.Clear();
        });
    }

    public void ShowNoUpdates()
    {
        main.Invoke(() =>
        {
            state = ProgressState.Starting;

            SetStatus("Starting app...", SuccessColor);
            SetProgress(1.0);
            SetDot(SuccessColor);

            AddLogEntry("No updates found");
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            main.Invoke(Complete);
        });
    }

    public void ShowUpdating()
    {
        main.Invoke(() =>
        {
            state = ProgressState.Updating;

            SetStatus("Updating...", AccentColor);
            SetProgress(0);
            SetDot(AccentColor);
        });
    }

    protected override Control BuildContent()
    {
        // Header
        titleLabel = new Label
        {
            Text = appTitle,
            Font = new Font(SystemFont.Bold, 16),
            TextColor = TextPrimary
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
            BackgroundColor = PanelColor,
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

        progressBar = new Drawable
        {
            Height = 10,
            BackgroundColor = Colors.Transparent
        };
        progressBar.Paint += (s, e) => DrawProgressBar(e.Graphics);

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
        logList = new ListBox
        {
            BackgroundColor = PanelColor,
            TextColor = TextPrimary
        };

        var logHeader = new Label
        {
            Text = "ACTIVITY LOG",
            Font = new Font(SystemFont.Bold, 9),
            TextColor = TextMuted
        };

        var logStack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Items =
            {
                logHeader,
                new StackLayoutItem(logList, expand: true)
            }
        };

        var logPanel = new Panel
        {
            Padding = new Padding(20, 12, 20, 20),
            Content = logStack
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
        return new ProgressScope(ReportProgress);
    }

    private void ReportProgress(double value, string message)
    {
        main.Invoke(() =>
        {
            if (state != ProgressState.Updating)
                return;

            SetProgress(value);

            if (!string.IsNullOrEmpty(message))
            {
                statusLabel.Text = message;
                AddLogEntry(message);
            }

            if (progressFraction >= 1.0)
            {
                _ = Task.Run(async () =>
                {
                    state = ProgressState.Starting;

                    main.Invoke(() =>
                    {
                        SetStatus("Starting app...", SuccessColor);
                        SetDot(SuccessColor);
                    });

                    await Task.Delay(500);

                    main.Invoke(Complete);
                });
            }
        });
    }

    private void Complete()
    {
        if (OnComplete is null)
        {
            Close();
            return;
        }

        OnComplete();
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

    private void SetProgress(double fraction)
    {
        progressFraction = Math.Clamp(fraction, 0.0, 1.0);
        percentLabel.Text = $"{(int)(progressFraction * 100)}%";
        progressBar.Invalidate();
    }

    private void AddLogEntry(string message)
    {
        logList.Items.Insert(0, new ListItem { Text = message });
        logList.SelectedIndex = 0;

        if (logList.Items.Count > 2000)
            logList.Items.RemoveAt(logList.Items.Count - 1);
    }

    private void DrawProgressBar(Graphics g)
    {
        var bounds = progressBar.Bounds;
        var trackRect = new RectangleF(0, 0, bounds.Width, bounds.Height);

        // Track
        g.FillPath(BorderColor, GraphicsPath.GetRoundRect(trackRect, 5));

        // Fill
        float fillWidth = (float)(trackRect.Width * progressFraction);
        if (fillWidth > 1)
        {
            var fillRect = new RectangleF(0, 0, fillWidth, bounds.Height);
            var gradient = new LinearGradientBrush(
                AccentColorDim, AccentColor,
                new PointF(0, 0), new PointF(fillWidth, 0));

            g.FillPath(gradient, GraphicsPath.GetRoundRect(fillRect, 5));
        }
    }
}