using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.Applications;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.ClientHub.Shared;
using WinterRose.ForgeThread;
using WinterRose.WebServer.Features.FileUploads.Models;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.ClientHub.Feature.Interface.Windows;

public class ApplicationStoreWindow : WindowBase
{
    private readonly AppServerClient server;

    private TextBox filterEntry;
    private ListBox appList;
    private ListBox versionList;
    private ComboBox branchSelector;
    private Button installButton;
    private Button closeButton;
    private Label appTitle;
    private Panel sidebarContainer;
    
    private List<LocalAppEntry> installedApps = new();

    // State
    private List<AppEntry> apps = new List<AppEntry>();
    private AppEntry selectedApp;
    private AppVersion selectedAppVersion;
    private bool sidebarVisible = false;

    private Task? dataRefreshTask;
    private readonly ClientAppRepository clientRepo;
    private readonly ApplicationInstaller installer;

    public ApplicationStoreWindow(
        AppServerClient server,
        ClientAppRepository clientRepo,
        ApplicationInstaller installer,
        MainThread main,
        IServiceProvider services)
        : base("WinterHub", main, services)
    {
        this.server = server;
        this.clientRepo = clientRepo;
        this.installer = installer;

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
            Width = 300
        };

        var sidebarLayout = new TableLayout
        {
            Padding = new Padding(8),
            Spacing = new Size(0, 8)
        };

        appTitle = new Label
        {
            Text = "",
            TextAlignment = TextAlignment.Left
        };

        sidebarLayout.Rows.Add(new TableRow(appTitle));

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

        installButton = new Button { Text = "Install" };
        closeButton = new Button { Text = "Close" };

        var buttonLayout = new TableLayout
        {
            Spacing = new Size(6, 0)
        };

        buttonLayout.Rows.Add(new TableRow(
            new TableCell(installButton) { ScaleWidth = true },
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
        closeButton.Click += (sender, e) => HideSidebar();
        secretIdButton.Click += OnSecretIdClicked;

        return root;
    }

    private async void OnSecretIdClicked(object? sender, EventArgs e)
    {
        var dialog = new Dialog<string>();

        var textBox = new TextBox { PlaceholderText = "xxxxxxxxxxx-abc-00000"};

        dialog.Content = new TableLayout(
                new Label { Text = "Enter a secret ID"},
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

        try
        {
            AppEntry? app = null; //await server.GetBySecretIdAsync(secretId);

            if (app == null)
            {
                MessageBox.Show(this,
                    "No application was found for that Secret ID.",
                    MessageBoxType.Warning);
                return;
            }

            selectedApp = app;
            appTitle.Text = app.Name;

            PopulateBranches();
            PopulateVersions();

            ShowSidebar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                ex.Message,
                MessageBoxType.Error);
        }
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
        appTitle.Text = selectedApp.Name;

        PopulateBranches();
        PopulateVersions();

        ShowSidebar();
    }

    private void OnBranchChanged(object? sender, EventArgs e)
    {
        PopulateVersions();
        installButton.Text = GetInstallAction(selectedAppVersion);
    }

    private void OnVersionSelected(object? sender, EventArgs e)
    {
        selectedAppVersion = null;

        if (selectedApp == null)
            return;

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
            return;
        }
        
        if (index >= 0 && index < versions.Count)
            selectedAppVersion = versions[index];
        
        installButton.Text = GetInstallAction(selectedAppVersion);
    }

    private void RefreshInstalledApps()
    {
        installedApps = clientRepo.GetInstalledApps();

        if (selectedAppVersion != null)
            installButton.Text = GetInstallAction(selectedAppVersion);
    }
    
    private async void OnInstallClicked(object? sender, EventArgs e)
    {
        if (selectedApp == null || selectedAppVersion == null)
            return;

        try
        {
            var progressWindow = new InstallationProgressWindow(main, services);
            progressWindow.Show();

            var progress = progressWindow.CreateUiScope();

            string action = GetInstallAction(selectedAppVersion);

            if (action is "Update" or "Migrate")
            {
                LocalAppEntry installed = GetInstalledApp(selectedApp.Name)!;
                await installer.PatchApplicationAsync(installed.Name, installed.Version, selectedAppVersion, progress);
            }
            else
            {
                // "Install" or "Re-Install" — full archive
                await installer.InstallFromArchiveAsync(selectedApp.Name, selectedAppVersion, progress);
            }

            RefreshInstalledApps();
            ShowSidebar();
            
            progress.Report(1.0, "Done");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, MessageBoxType.Error);
        }
    }

    private void PopulateApplicationList()
    {
        appList.Items.Clear();

        foreach (AppEntry app in apps)
        {
            appList.Items.Add(app.Name);
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
            return;

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
    }

    private string GetInstallAction(AppVersion selected)
    {
        if (selectedApp == null)
            return "Install";

        var installed = GetInstalledApp(selectedApp.Name);

        if (installed == null)
            return "Install";

        var installedVersion = installed.Version;

        // same branch
        bool sameBranch = installedVersion.Tag == selected.Tag;

        if (selected > installedVersion)
            return "Update";

        if (!sameBranch || selected < installedVersion)
            return "Migrate";

        return "Re-Install";
    }
    
    private LocalAppEntry? GetInstalledApp(string appName)
    {
        return installedApps.FirstOrDefault(x =>
            string.Equals(x.Name, appName, StringComparison.OrdinalIgnoreCase));
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
        catch (Exception ex)
        {
            apps.Clear();

            PopulateApplicationList();

            Console.WriteLine(ex);
        }
    }
    
   

    private void HideSidebar()
    {
        sidebarVisible = false;
        sidebarContainer.Visible = false;

        appList.SelectedIndex = -1;
    }

    private void ShowSidebar()
    {
        sidebarVisible = true;
        sidebarContainer.Visible = true;
    }
}