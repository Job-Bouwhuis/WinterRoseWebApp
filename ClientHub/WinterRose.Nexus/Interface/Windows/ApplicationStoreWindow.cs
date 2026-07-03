using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.Applications;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Nexus.Exceptions;
using WinterRose.Nexus.Services;
using WinterRose.Nexus.Shared;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Nexus.Interface.Windows;

public class ApplicationStoreWindow : WindowBase
{
    private readonly AppServerClient server;

    private TextBox filterEntry;
    private ListBox appList;
    private ListBox versionList;
    private ComboBox branchSelector;
    private Button installButton;
    private Button uninstallButton;
    private Button closeButton;
    private Label appTitle;
    private Label publisherLabel;
    private TextArea descriptionText;
    private TextArea versionDetailsText;
    private CheckBox pinVersionCheckbox;
    private Panel sidebarContainer;

    /// <summary>
    /// Guards against OnPinVersionChanged firing while we're programmatically
    /// syncing the checkbox to reflect installed state (as opposed to the
    /// user actually clicking it).
    /// </summary>
    private bool suppressPinChangeEvent = false;

    private List<LocalAppEntry> installedApps = new();

    // State
    private List<AppEntry> apps = new List<AppEntry>();
    private AppEntry selectedApp;
    private AppVersion selectedAppVersion;
    private bool sidebarVisible = false;

    private Task? dataRefreshTask;
    private readonly ClientAppRepository clientRepo;
    private readonly ApplicationInstaller installer;
    private readonly ILogger<ApplicationStoreWindow> logger;

    public ApplicationStoreWindow(
        AppServerClient server,
        ClientAppRepository clientRepo,
        ApplicationInstaller installer,
        MainThread main,
        IServiceProvider services,
        ILogger<ApplicationStoreWindow> logger)
        : base("Nexus App Store", main, services)
    {
        this.server = server;
        this.clientRepo = clientRepo;
        this.installer = installer;
        this.logger = logger;

        main.Invoke(() =>
        {
            Width = 750;
            Height = 750;
        });
    }

    protected override void OnShown(EventArgs args)
    {
        base.OnShown(args);
        dataRefreshTask ??= RefreshAsync();
        installedApps = clientRepo.GetInstalledApps();
    }

    private async Task RefreshAsync()
    {
        await LoadDataAsync();
        dataRefreshTask = null;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Visible = false;
        base.OnClosing(e);
    }

    protected override Control BuildContent()
    {
        // ==========================================================
        // Main Layout
        // ==========================================================
        var root = new TableLayout
        {
            Padding = new Padding(10),
            Spacing = new Size(0, 8)
        };

        // ==========================================================
        // Toolbar
        // ==========================================================
        filterEntry = new TextBox
        {
            PlaceholderText = "Search applications..."
        };

        var secretIdButton = new Button
        {
            Text = "By Secret ID..."
        };

        var toolbar = new TableLayout
        {
            Spacing = new Size(6, 0)
        };

        toolbar.Rows.Add(new TableRow(
            new TableCell(filterEntry) { ScaleWidth = true },
            secretIdButton
        ));

        // ==========================================================
        // Left Panel
        // ==========================================================
        var leftPanel = new TableLayout
        {
            Spacing = new Size(0, 6)
        };

        var appScroll = new Scrollable
        {
            Border = BorderType.Line
        };

        appList = new ListBox();
        appList.SelectedIndexChanged += OnAppSelected;

        appScroll.Content = appList;

        leftPanel.Rows.Add(new TableRow(appScroll) { ScaleHeight = true });

        // ==========================================================
        // Sidebar
        // ==========================================================
        sidebarContainer = new Panel
        {
            Visible = false,
            Width = 320
        };

        var sidebarLayout = new TableLayout
        {
            Padding = new Padding(8),
            Spacing = new Size(0, 8)
        };

        appTitle = new Label
        {
            Text = "",
            Font = new Font(SystemFont.Bold, 12),
            TextAlignment = TextAlignment.Left
        };

        sidebarLayout.Rows.Add(new TableRow(appTitle));

        publisherLabel = new Label
        {
            Text = "",
            TextColor = Colors.Gray,
            TextAlignment = TextAlignment.Left
        };

        sidebarLayout.Rows.Add(new TableRow(publisherLabel));

        descriptionText = new TextArea
        {
            ReadOnly = true,
            Wrap = true,
            Height = 70
        };

        sidebarLayout.Rows.Add(new TableRow(descriptionText));

        sidebarLayout.Rows.Add(new TableRow(
            new Label
            {
                Text = "Branch",
                TextAlignment = TextAlignment.Left
            }));

        branchSelector = new ComboBox();
        branchSelector.SelectedIndexChanged += OnBranchChanged;

        sidebarLayout.Rows.Add(new TableRow(branchSelector));

        sidebarLayout.Rows.Add(new TableRow(
            new Label
            {
                Text = "Versions",
                TextAlignment = TextAlignment.Left
            }));

        var versionScroll = new Scrollable
        {
            Border = BorderType.Line
        };

        versionList = new ListBox();
        versionList.SelectedIndexChanged += OnVersionSelected;

        versionScroll.Content = versionList;

        sidebarLayout.Rows.Add(new TableRow(versionScroll) { ScaleHeight = true });

        versionDetailsText = new TextArea
        {
            ReadOnly = true,
            Wrap = true,
            Height = 90
        };

        sidebarLayout.Rows.Add(new TableRow(versionDetailsText));

        pinVersionCheckbox = new CheckBox { Text = "Pin this version (skip auto-updates)" };
        pinVersionCheckbox.CheckedChanged += OnPinVersionChanged;

        sidebarLayout.Rows.Add(new TableRow(pinVersionCheckbox));

        installButton = new Button { Text = "Install" };
        uninstallButton = new Button { Text = "Uninstall", Enabled = false };
        closeButton = new Button { Text = "Close" };

        var buttonLayout = new TableLayout
        {
            Spacing = new Size(6, 0)
        };

        buttonLayout.Rows.Add(new TableRow(
            new TableCell(installButton) { ScaleWidth = true },
            uninstallButton,
            closeButton
        ));

        sidebarLayout.Rows.Add(new TableRow(buttonLayout));

        sidebarContainer.Content = sidebarLayout;

        // ==========================================================
        // Content Area
        // ==========================================================
        var content = new TableLayout();

        content.Rows.Add(new TableRow(
            new TableCell(leftPanel) { ScaleWidth = true },
            new TableCell(sidebarContainer) { ScaleWidth = false }
        ));

        // ==========================================================
        // Assemble
        // ==========================================================
        root.Rows.Add(new TableRow(toolbar) { ScaleHeight = false });
        root.Rows.Add(new TableRow(content) { ScaleHeight = true });

        // ==========================================================
        // Events
        // ==========================================================
        installButton.Click += OnInstallClicked;
        uninstallButton.Click += OnUninstallClicked;
        closeButton.Click += (sender, e) => HideSidebar();
        secretIdButton.Click += OnSecretIdClicked;

        return root;
    }

    private async void OnSecretIdClicked(object? sender, EventArgs e)
    {
        var dialog = new Dialog<string>();

        var textBox = new TextBox { PlaceholderText = "xxxxxxxxxxx-abc-00000" };

        dialog.Content = new TableLayout(
            new Label { Text = "Enter a secret ID" },
            new TableRow(textBox),
            new TableRow(
                new Button
                {
                    Text = "Lookup",
                    Command = new Command((s, e2) => dialog.Close(textBox.Text))
                })
        );

        string? secretId = dialog.ShowModal(this);

        if (string.IsNullOrWhiteSpace(secretId))
            return;

        AppEntry? app = null; //await server.GetBySecretIdAsync(secretId);

        if (app == null)
        {
            MessageBox.Show(this,
                "No application was found for that Secret ID.",
                MessageBoxType.Warning);
            return;
        }

        selectedApp = app;
        UpdateAppDetails();

        PopulateBranches();
        PopulateVersions();
        RefreshUninstallButtonState();

        ShowSidebar();
    }

    private void OnAppSelected(object? sender, EventArgs e)
    {
        int index = appList.SelectedIndex;

        if (index < 0 || index >= apps.Count)
        {
            selectedApp = null;
            HideSidebar();
            return;
        }

        selectedApp = apps[index];
        UpdateAppDetails();

        PopulateBranches();
        PopulateVersions();
        RefreshUninstallButtonState();

        ShowSidebar();
    }

    private void OnBranchChanged(object? sender, EventArgs e)
    {
        PopulateVersions();
        installButton.Text = GetInstallAction(selectedAppVersion);
        RefreshPinCheckboxState();
    }

    private void OnVersionSelected(object? sender, EventArgs e)
    {
        selectedAppVersion = null;

        if (selectedApp == null)
        {
            UpdateVersionDetails();
            return;
        }

        string selectedBranch =
            branchSelector.SelectedIndex <= 0
                ? ""
                : branchSelector.SelectedValue?.ToString() ?? "";

        List<AppVersion> versions = selectedApp.Versions
            .Where(version =>
                string.IsNullOrEmpty(selectedBranch)
                    ? string.IsNullOrEmpty(version.Tag)
                    : version.Tag == selectedBranch)
            .OrderByDescending(version => version.UploadedAt)
            .ToList();

        int index = versionList.SelectedIndex;

        if (index == -1)
        {
            installButton.Text = "Install";
            UpdateVersionDetails();
            return;
        }

        if (index >= 0 && index < versions.Count)
            selectedAppVersion = versions[index];

        installButton.Text = GetInstallAction(selectedAppVersion);
        UpdateVersionDetails();
        RefreshPinCheckboxState();
    }

    private void RefreshInstalledApps()
    {
        installedApps = clientRepo.GetInstalledApps();

        if (selectedAppVersion != null)
            installButton.Text = GetInstallAction(selectedAppVersion);

        RefreshUninstallButtonState();
    }

    private async void OnInstallClicked(object? sender, EventArgs e)
    {
        if (selectedApp == null || selectedAppVersion == null)
            return;


        var progressWindow = new InstallationProgressWindow(main, services, selectedApp.DisplayName);
        progressWindow.Show();
        progressWindow.ShowUpdating();
        
        var progress = progressWindow.CreateUiScope();

        string action = GetInstallAction(selectedAppVersion);

        if (action is "Update" or "Migrate")
        {
            LocalAppEntry installed = GetInstalledApp(selectedApp.AppId)!;
            await installer.PatchApplicationAsync(installed.AppId, selectedAppVersion, progress);
        }
        else
        {
            // "Install" or "Re-Install" — full archive
            await installer.InstallFromArchiveAsync(selectedApp.AppId, selectedAppVersion, progress);
        }

        // Auto pin/unpin based on whether the version just installed is
        // the latest in its branch. Any prior manual pin choice is
        // superseded by this fresh evaluation, per policy.
        ApplyAutoPinPolicy(selectedApp.AppId, selectedAppVersion);

        RefreshInstalledApps();
        RefreshPinCheckboxState();
        ShowSidebar();

        progress.Report(1.0, "Done");
    }

    private async void OnUninstallClicked(object? sender, EventArgs e)
    {
        if (selectedApp == null)
            return;

        LocalAppEntry? installed = GetInstalledApp(selectedApp.AppId);

        if (installed == null)
            return;

        DialogResult confirm = MessageBox.Show(
            this,
            $"Are you sure you want to uninstall \"{selectedApp.DisplayName}\"?\n\nThis will remove all installed files for this application.",
            "Confirm Uninstall",
            MessageBoxButtons.YesNo,
            MessageBoxType.Warning,
            MessageBoxDefaultButton.No);

        if (confirm != DialogResult.Yes)
            return;

        uninstallButton.Enabled = false;

        await installer.UninstallApplicationAsync(installed.AppId);

        RefreshInstalledApps();
        RefreshPinCheckboxState();

        if (selectedAppVersion != null)
            installButton.Text = GetInstallAction(selectedAppVersion);

        ShowSidebar();
        RefreshUninstallButtonState();
    }

    private void PopulateApplicationList()
    {
        appList.Items.Clear();

        foreach (AppEntry app in apps)
        {
            appList.Items.Add(app.DisplayName);
        }
    }

    private void PopulateBranches()
    {
        branchSelector.Items.Clear();

        if (selectedApp == null)
            return;

        branchSelector.Items.Add("Release");

        HashSet<string> branches = new(StringComparer.OrdinalIgnoreCase);

        foreach (AppVersion version in selectedApp.Versions)
        {
            if (string.IsNullOrWhiteSpace(version.Tag))
                continue;

            if (branches.Add(version.Tag))
                branchSelector.Items.Add(version.Tag);
        }

        branchSelector.SelectedIndex = 0;
    }

    private void PopulateVersions()
    {
        versionList.Items.Clear();

        selectedAppVersion = null;

        if (selectedApp == null)
        {
            UpdateVersionDetails();
            return;
        }

        string selectedBranch =
            branchSelector.SelectedIndex <= 0
                ? ""
                : branchSelector.SelectedValue?.ToString() ?? "";

        var versions = selectedApp.Versions
            .Where(version =>
                string.IsNullOrEmpty(selectedBranch)
                    ? string.IsNullOrEmpty(version.Tag)
                    : version.Tag == selectedBranch)
            .OrderByDescending(version => version.UploadedAt)
            .ToList();

        foreach (AppVersion version in versions)
        {
            versionList.Items.Add(
                $"{version.Major}.{version.Minor}.{version.Patch}    ({version.UploadedAt:D})");
        }

        // auto select latest
        if (versions.Count > 0)
        {
            versionList.SelectedIndex = 0;
            selectedAppVersion = versions[0];

            installButton.Text = GetInstallAction(selectedAppVersion);
        }

        UpdateVersionDetails();
        RefreshPinCheckboxState();
    }

    /// <summary>
    /// Fills in the publisher / description block for the currently
    /// selected app. Clears it when nothing is selected.
    /// </summary>
    private void UpdateAppDetails()
    {
        if (selectedApp == null)
        {
            appTitle.Text = "";
            publisherLabel.Text = "";
            descriptionText.Text = "";
            return;
        }

        appTitle.Text = selectedApp.DisplayName;

        publisherLabel.Text = string.IsNullOrWhiteSpace(selectedApp.Publisher)
            ? "Unknown publisher"
            : $"By {selectedApp.Publisher}";

        descriptionText.Text =
            !string.IsNullOrWhiteSpace(selectedApp.LongDescription)
                ? selectedApp.LongDescription
                : selectedApp.ShortDescription;
    }

    /// <summary>
    /// Fills in the details block (changelog, install size, upload date,
    /// launch target) for the currently selected version. Clears it when
    /// nothing is selected.
    /// </summary>
    private void UpdateVersionDetails()
    {
        if (selectedAppVersion == null)
        {
            versionDetailsText.Text = "";
            return;
        }

        AppVersion v = selectedAppVersion;

        var lines = new List<string>
        {
            $"Version: {v.ToString(VersionStringFormat.HumanReadable)}",
            $"Uploaded: {v.UploadedAt:g}",
            $"Install size: {FormatBytes(v.InstallSize)}"
        };

        if (!string.IsNullOrWhiteSpace(v.LaunchTarget?.Path))
            lines.Add($"Launch target: {v.LaunchTarget.Path}");

        if (!string.IsNullOrWhiteSpace(v.Changelog))
        {
            lines.Add("");
            lines.Add("Changelog:");
            lines.Add(v.Changelog);
        }

        versionDetailsText.Text = string.Join(Environment.NewLine, lines);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private string GetInstallAction(AppVersion selected)
    {
        if (selectedApp == null)
            return "Install";

        var installed = GetInstalledApp(selectedApp.AppId);

        if (installed == null)
            return "Install";

        var installedVersion = installed.InstalledVersion;

        // same branch
        bool sameBranch = installedVersion.Tag == selected.Tag;

        if (selected > installedVersion)
            return "Update";

        if (!sameBranch || selected < installedVersion)
            return "Migrate";

        return "Re-Install";
    }

    private LocalAppEntry? GetInstalledApp(string appId)
    {
        return installedApps.FirstOrDefault(x =>
            string.Equals(x.AppId, appId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// True if <paramref name="version"/> is the newest known version within
    /// its own release branch (Tag) for <see cref="selectedApp"/>.
    /// </summary>
    private bool IsLatestInBranch(AppVersion version)
    {
        if (selectedApp == null || version == null)
            return true;

        AppVersion? latestInBranch = selectedApp.Versions
            .Where(v => string.Equals(v.Tag, version.Tag, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.UploadedAt)
            .FirstOrDefault();

        if (latestInBranch == null)
            return true;

        return !(latestInBranch > version);
    }

    /// <summary>
    /// Syncs the pin checkbox to reflect the installed app's actual pin
    /// state (from .localappdetails), without firing the change handler.
    /// Enabled only when there's something installed to pin.
    /// </summary>
    private void RefreshPinCheckboxState()
    {
        if (selectedApp == null)
        {
            pinVersionCheckbox.Enabled = false;
            return;
        }

        LocalAppEntry? installed = GetInstalledApp(selectedApp.AppId);

        suppressPinChangeEvent = true;

        if (installed == null)
        {
            // Nothing installed yet -- pin has no meaning until after install.
            pinVersionCheckbox.Checked = false;
            pinVersionCheckbox.Enabled = false;
        }
        else
        {
            pinVersionCheckbox.Enabled = true;
            pinVersionCheckbox.Checked = installed.PinVersion;
        }

        suppressPinChangeEvent = false;
    }

    /// <summary>
    /// Enables the Uninstall button only when the currently selected app
    /// has a local install to remove.
    /// </summary>
    private void RefreshUninstallButtonState()
    {
        uninstallButton.Enabled =
            selectedApp != null && GetInstalledApp(selectedApp.AppId) != null;
    }

    private void OnPinVersionChanged(object? sender, EventArgs e)
    {
        if (suppressPinChangeEvent || selectedApp == null)
            return;

        LocalAppEntry? installed = GetInstalledApp(selectedApp.AppId);

        if (installed == null)
            return;

        bool newPinState = pinVersionCheckbox.Checked ?? false;


        installer.SetPinVersion(installed.AppId, newPinState);
        RefreshInstalledApps();
    }

    /// <summary>
    /// Decides whether an app should be pinned immediately after an
    /// install/patch completes: auto-pin if the version just installed
    /// is not the latest in its branch, auto-unpin if it is. Manual pin
    /// changes made afterward via the checkbox always take precedence
    /// until the next install/patch re-evaluates this.
    /// </summary>
    private void ApplyAutoPinPolicy(string appId, AppVersion installedVersion)
    {
        bool shouldPin = !IsLatestInBranch(installedVersion);
        installer.SetPinVersion(appId, shouldPin);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            apps = await server.GetAppEntriesAsync();

            selectedApp = null;
            selectedAppVersion = null;

            HideSidebar();
            PopulateApplicationList();
        }
        catch (ServerUnavailableException)
        {
            apps.Clear();

            HideSidebar();
            PopulateApplicationList();

            DialogResult r = MessageBox.Show(
                this,
                "The server may be offline or your internet connection may be unavailable.\n\nDo you want to quit the app?",
                "WinterHub couldn't reach the server.",
                MessageBoxButtons.YesNo, MessageBoxType.Warning, MessageBoxDefaultButton.Yes);

            if (r == DialogResult.Yes)
                services.Resolve<IApplication>().Stop();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, MessageBoxType.Error);
            logger.Error(ex);
        }
    }


    private void HideSidebar()
    {
        sidebarVisible = false;
        sidebarContainer.Visible = false;

        appList.SelectedIndex = -1;

        suppressPinChangeEvent = true;
        pinVersionCheckbox.Checked = false;
        pinVersionCheckbox.Enabled = false;
        suppressPinChangeEvent = false;

        uninstallButton.Enabled = false;
    }

    private void ShowSidebar()
    {
        sidebarVisible = true;
        sidebarContainer.Visible = true;
    }
}