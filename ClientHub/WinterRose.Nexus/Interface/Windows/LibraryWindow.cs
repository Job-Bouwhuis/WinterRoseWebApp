using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Gtk;
using WinterRose.DependancyInjection;
using WinterRose.Nexus.Interface.Preferences;
using WinterRose.Nexus.Services;
using WinterRose.Nexus.Shared;
using WinterRose.ProgressKeeping;
using WinterRose.Shortcuts;
using Button = Eto.Forms.Button;
using ComboBox = Eto.Forms.ComboBox;
using Dialog = Eto.Forms.Dialog;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;
using Label = Eto.Forms.Label;
using ListBox = Eto.Forms.ListBox;
using MenuBar = Eto.Forms.MenuBar;

namespace WinterRose.Nexus.Interface.Windows;

public class LibraryWindow(
    MainThread mainThread,
    IServiceProvider services,
    ClientAppRepository appRepo,
    AppServerClient server,
    ApplicationInstaller installer,
    ApplicationStarter appStarter,
    [Specifically<ApplicationStoreWindow>] WindowBase storeWindow,
    [Specifically<UserPreferencesWindow>] WindowBase preferencesWindow)
    : WindowBase("Nexus Library", mainThread, services)
{
    // Palette (shared visual language with InstallationProgressWindow)

    private List<LocalAppEntry> installedApps = new();
    private readonly List<AppEntry> apps = new();

    private LocalAppEntry? selectedLocalApp;

    private ListBox installedList;
    private ListBox recentList;

    private Label appTitle;
    private Label appPublisher;
    private Label appVersion;
    private Label appTags;
    private TextArea appDetails;

    private Button launchButton;
    private Button utilityButton;

    private Panel sidebar;

    private bool suppressPinChange;

    protected override Control BuildContent()
    {
        Width = 720;
        Height = 560;
        
        // ==========================================================
        // MENU
        // ==========================================================
        var menu = new MenuBar();

        var installMenu = new ButtonMenuItem
        {
            Text = "Install new app"
        };

        var fromStoreItem = new ButtonMenuItem
        {
            Text = "From store"
        };

        installMenu.Items.Add(fromStoreItem);
        menu.Items.Add(installMenu);

        var preferencesMenu = new ButtonMenuItem
        {
            Text = "Preferences"
        };
        
        preferencesMenu.Click += (sender, args) =>
        {
            preferencesWindow.InitializeWindow();
            preferencesWindow.Show();
        };
        
        menu.Items.Add(preferencesMenu);

        fromStoreItem.Click += OnOpenStoreClicked;

        Menu = menu;

        // ==========================================================
        // LEFT SIDE
        // ==========================================================
        installedList = new ListBox();
        installedList.SelectedIndexChanged += OnInstalledSelected;

        recentList = new ListBox
        {
            Height = 110
        };

        recentList.SelectedIndexChanged += OnRecentSelected;

        var leftPanel = new TableLayout
        {
            Padding = new Padding(16),
            Spacing = new Size(0, 10),
            Style = "muted"
        };

        leftPanel.Rows.Add(new TableRow(
            new Label
            {
                Text = "INSTALLED APPS",
                Font = new Font(SystemFont.Bold, 9),
                Style = "muted"
            }));

        leftPanel.Rows.Add(new TableRow(installedList)
        {
            ScaleHeight = true
        });

        leftPanel.Rows.Add(new TableRow(
            new Label
            {
                Text = "RECENTLY STARTED",
                Font = new Font(SystemFont.Bold, 9),
                Style = "muted"
            }));

        leftPanel.Rows.Add(new TableRow(recentList));

        var leftContainer = new Panel
        {
            BackgroundColor = BackgroundColor,
            Content = leftPanel
        };

        // ==========================================================
        // RIGHT SIDE
        // ==========================================================
        sidebar = new Panel
        {
            Width = 360,
            Visible = false,
            Style = "card-elevated"
        };

        appTitle = new Label
        {
            Font = new Font(SystemFont.Bold, 16),
            Style = "accent"
        };

        appPublisher = new Label
        {
            Style = "muted"
        };

        appTags = new Label
        {
            Style = "muted"
        };

        appVersion = new Label
        {
            Style = "accent"
        };

        appDetails = new TextArea
        {
            ReadOnly = true,
            Wrap = true
        };

        launchButton = new Button
        {
            Text = "Launch"
        };

        utilityButton = new Button
        {
            Text = "Manage..."
        };

        launchButton.Click += OnLaunchClicked;
        utilityButton.Click += OnUtilityClicked;

        var buttonLayout = new TableLayout
        {
            Spacing = new Size(8, 0)
        };

        buttonLayout.Rows.Add(new TableRow(
            new TableCell(launchButton)
            {
                ScaleWidth = true
            },
            utilityButton));

        var sidebarLayout = new TableLayout
        {
            Padding = new Padding(18),
            Spacing = new Size(0, 8),
            Style = "accent"
        };

        sidebarLayout.Rows.Add(new TableRow(appTitle));
        sidebarLayout.Rows.Add(new TableRow(appPublisher));
        sidebarLayout.Rows.Add(new TableRow(appVersion));
        sidebarLayout.Rows.Add(new TableRow(appTags));

        sidebarLayout.Rows.Add(new TableRow(CreateDivider()));

        sidebarLayout.Rows.Add(new TableRow(appDetails)
        {
            ScaleHeight = true
        });

        sidebarLayout.Rows.Add(new TableRow(CreateDivider()));
        sidebarLayout.Rows.Add(new TableRow(buttonLayout));

        sidebar.Content = sidebarLayout;

        // ==========================================================
        // MAIN CONTENT
        // ==========================================================
        var content = new TableLayout
        {
            Padding = new Padding(0),
            Spacing = new Size(0, 0),
            BackgroundColor = BackgroundColor
        };

        content.Rows.Add(new TableRow(
            new TableCell(leftContainer)
            {
                ScaleWidth = true
            },
            sidebar));

        return content;
    }

    // NOTE: this now reads entirely from selectedLocalApp (LocalAppEntry).
    // Publisher, Tags, LongDescription/ShortDescription are NOT currently
    // present on LocalAppEntry - this assumes they exist as:
    //   selectedLocalApp.Publisher            (string)
    //   selectedLocalApp.Tags                 (ICollection<string> or similar)
    //   selectedLocalApp.LongDescription      (string)
    //   selectedLocalApp.ShortDescription     (string)
    // Add these to LocalAppEntry, or adjust the property names below to match.
    private void UpdateSidebar()
    {
        if (selectedLocalApp == null)
            return;

        appTitle.Text = selectedLocalApp.DisplayName ?? selectedLocalApp.AppId;

        appPublisher.Text = $"by {selectedLocalApp.Publisher}";

        appVersion.Text =
            $"Installed: {selectedLocalApp.InstalledVersion.ToString(VersionStringFormat.HumanReadable)}"
            + (selectedLocalApp.PinVersion ? " (Pinned)" : "");

        appTags.Text = selectedLocalApp.Tags.Length > 0
            ? string.Join(" • ", selectedLocalApp.Tags)
            : "";

        appDetails.Text =
            !string.IsNullOrWhiteSpace(selectedLocalApp.LongDescription)
                ? selectedLocalApp.LongDescription
                : selectedLocalApp.ShortDescription;
    }

    Control CreateDivider()
    {
        return new Panel
        {
            BackgroundColor = ThemeManager.NexusPalette.BORDER,
            Height = 1
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RefreshLibrary();
    }

    private void PopulateLists()
    {
        installedList.Items.Clear();
        recentList.Items.Clear();

        foreach (var app in installedApps)
            installedList.Items.Add(app.DisplayName ?? app.AppId);

        var recent = installedApps
            .Where(x => x.LastStartedAt.HasValue)
            .OrderByDescending(x => x.LastStartedAt)
            .Take(5)
            .ToList();

        foreach (var app in recent)
            recentList.Items.Add(app.DisplayName ?? app.AppId);
    }

    // No longer touches the server - purely local refresh now.
    private void RefreshLibrary()
    {
        installedApps = appRepo.GetInstalledApps();
        PopulateLists();

        if (selectedLocalApp == null)
            return;

        var stillInstalled = installedApps
            .FirstOrDefault(x => x.AppId == selectedLocalApp.AppId);

        if (stillInstalled == null)
        {
            HideSidebar();
            return;
        }

        selectedLocalApp = stillInstalled;

        UpdateSidebar();
    }

    private void OnInstalledSelected(object? sender, EventArgs e)
    {
        if (installedList.SelectedIndex < 0 || installedList.SelectedIndex >= installedApps.Count)
            return;

        selectedLocalApp = installedApps[installedList.SelectedIndex];

        ShowSidebar();
        UpdateSidebar();
    }

    private void OnRecentSelected(object? sender, EventArgs e)
    {
        if (recentList.SelectedIndex < 0)
            return;

        var recent = installedApps
            .Where(x => x.LastStartedAt.HasValue)
            .OrderByDescending(x => x.LastStartedAt)
            .Take(5)
            .ToList();

        selectedLocalApp = recent[recentList.SelectedIndex];

        ShowSidebar();
        UpdateSidebar();
    }


    private async void OnLaunchClicked(object? sender, EventArgs e)
    {
        if (selectedLocalApp == null)
            return;

        try
        {
            await appStarter.Start(selectedLocalApp.AppId, true);
            selectedLocalApp.LastStartedAt = DateTime.UtcNow;

            RefreshLibrary();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, MessageBoxType.Error);
        }
    }

    // ==========================================================
    // UTILITY DIALOG (versions + shortcuts + browse local files)
    // ==========================================================
    private async void OnUtilityClicked(object? sender, EventArgs e)
    {
        if (selectedLocalApp == null)
            return;

        var dialog = new Dialog
        {
            Title = $"Manage {selectedLocalApp.DisplayName ?? selectedLocalApp.AppId}",
            Width = 340,
            Height = 320,
            BackgroundColor = BackgroundColor
        };

        var layout = new TableLayout
        {
            Padding = new Padding(14),
            Spacing = new Size(0, 10)
        };

        layout.Rows.Add(new TableRow(new Label
        {
            Text = "Versions",
            Font = new Font(SystemFont.Bold, 10)
        }));

        var changeVersionButton = new Button { Text = "Change version..." };
        changeVersionButton.Click += async (s, e2) =>
        {
            dialog.Close();
            await OnVersionsClicked(sender, e);
        };
        layout.Rows.Add(new TableRow(changeVersionButton));

        layout.Rows.Add(new TableRow(new Label
        {
            Text = "Desktop Shortcut",
            Font = new Font(SystemFont.Bold, 10)
        }));

        string shortcutPath = GetShortcutPath(selectedLocalApp);
        bool shortcutExists = File.Exists(shortcutPath);

        var createShortcutButton = new Button
        {
            Text = "Create desktop shortcut",
            Enabled = !shortcutExists
        };
        var removeShortcutButton = new Button
        {
            Text = "Remove desktop shortcut",
            Enabled = shortcutExists
        };

        createShortcutButton.Click += (s, e2) =>
        {
            try
            {
                string targetPath = Path.Combine(selectedLocalApp.InstallPath, selectedLocalApp.InstalledVersion.LaunchTarget.Path);
                string icoPath = AppIconExtractor.GetIconPath(targetPath);
                if(string.IsNullOrWhiteSpace(icoPath))
                    icoPath = AppIconExtractor.GetIconPath(Environment.ProcessPath);
                
                ShortcutMaker.CreateUriShortcut(
                    shortcutPath,
                    $"winterrose://update-application?id={selectedLocalApp.AppId}&auto-start=true",
                    icoPath);

                createShortcutButton.Enabled = false;
                removeShortcutButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog, ex.Message, MessageBoxType.Error);
            }
        };

        removeShortcutButton.Click += (s, e2) =>
        {
            try
            {
                if (File.Exists(shortcutPath))
                    File.Delete(shortcutPath);

                removeShortcutButton.Enabled = false;
                createShortcutButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog, ex.Message, MessageBoxType.Error);
            }
        };

        layout.Rows.Add(new TableRow(
            new TableLayout
            {
                Spacing = new Size(6, 0),
                Rows = { new TableRow(createShortcutButton, removeShortcutButton) }
            }));

        layout.Rows.Add(new TableRow(new Label
        {
            Text = "Files",
            Font = new Font(SystemFont.Bold, 10)
        }));

        var browseLocalFilesButton = new Button { Text = "Browse local files" };
        browseLocalFilesButton.Click += (s, e2) =>
        {
            try
            {
                OpenInFileExplorer(selectedLocalApp.InstallPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(dialog, ex.Message, MessageBoxType.Error);
            }
        };
        layout.Rows.Add(new TableRow(browseLocalFilesButton));

        layout.Rows.Add(null); // spacer

        var closeButton = new Button { Text = "Close" };
        closeButton.Click += (s, e2) => dialog.Close();
        layout.Rows.Add(new TableRow(new TableCell(closeButton) { ScaleWidth = true }));

        dialog.Content = layout;
        dialog.ShowModal(this);
    }

    private static void OpenInFileExplorer(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            throw new DirectoryNotFoundException($"Application folder not found: {path}");

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{path}\"",
                UseShellExecute = false
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{path}\"",
                UseShellExecute = false
            });
        }
        else
        {
            throw new PlatformNotSupportedException("Cannot browse local files on this platform.");
        }
    }

    private string GetShortcutPath(LocalAppEntry app)
    {
        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string fileName = (app.DisplayName ?? app.AppId).Replace(" ", "_");

        string extension = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? ".lnk"
            : ".desktop";

        return Path.Combine(desktopDir, fileName + extension);
    }

    // ==========================================================
    // VERSION SWITCHING (store-style sidebar in a dialog)
    // This is the ONLY place that still needs the real AppEntry
    // from the server, since branch/version/changelog data isn't
    // (and shouldn't be) cached locally.
    // ==========================================================
    private async Task OnVersionsClicked(object? sender, EventArgs e)
    {
        if (selectedLocalApp == null)
            return;

        var app = await server.GetAppEntryAsync(selectedLocalApp.AppId);

        var dialog = new Dialog<AppVersion>
        {
            Title = "Change Version",
            Width = 360,
            Height = 520,
            BackgroundColor = BackgroundColor
        };

        var layout = new TableLayout
        {
            Padding = new Padding(12),
            Spacing = new Size(0, 8)
        };

        var titleLabel = new Label
        {
            Text = app.DisplayName,
            Font = new Font(SystemFont.Bold, 12),
            TextAlignment = TextAlignment.Left
        };
        layout.Rows.Add(new TableRow(titleLabel));

        var publisherLabel = new Label
        {
            Text = app.Publisher ?? "",
            Style = "muted",
            TextAlignment = TextAlignment.Left
        };
        layout.Rows.Add(new TableRow(publisherLabel));

        layout.Rows.Add(new TableRow(new Label
        {
            Text = "Branch",
            TextAlignment = TextAlignment.Left
        }));

        var branches = app.Versions
            .Select(v => v.Tag) // TODO: add "" > "Release" mapping
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var branchSelector = new ComboBox();
        foreach (var b in branches)
            branchSelector.Items.Add(b);

        layout.Rows.Add(new TableRow(branchSelector));

        layout.Rows.Add(new TableRow(new Label
        {
            Text = "Versions",
            TextAlignment = TextAlignment.Left
        }));

        var versionScroll = new Scrollable { Border = BorderType.Line };
        var versionList = new ListBox();
        versionScroll.Content = versionList;
        layout.Rows.Add(new TableRow(versionScroll) { ScaleHeight = true });

        var versionDetailsText = new TextArea
        {
            ReadOnly = true,
            Wrap = true,
            Height = 90
        };
        layout.Rows.Add(new TableRow(versionDetailsText));

        List<AppVersion> currentBranchVersions = new();

        void PopulateVersionsForBranch(string tag)
        {
            currentBranchVersions = app.Versions
                .Where(v => string.Equals(v.Tag, tag, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(v => v.UploadedAt)
                .ToList();

            versionList.Items.Clear();
            foreach (var v in currentBranchVersions)
                versionList.Items.Add(v.ToString(VersionStringFormat.HumanReadable));

            versionDetailsText.Text = "";
        }

        branchSelector.SelectedIndexChanged += (s, e2) =>
        {
            if (branchSelector.SelectedIndex < 0)
                return;

            PopulateVersionsForBranch(branches[branchSelector.SelectedIndex]);
        };

        versionList.SelectedIndexChanged += (s, e2) =>
        {
            if (versionList.SelectedIndex < 0 || versionList.SelectedIndex >= currentBranchVersions.Count)
                return;

            var v = currentBranchVersions[versionList.SelectedIndex];
            versionDetailsText.Text = v.Changelog;
        };

        // Pre-select current branch / version
        string currentTag = selectedLocalApp.InstalledVersion.Tag;
        int branchIndex = branches.FindIndex(b =>
            string.Equals(b, currentTag, StringComparison.OrdinalIgnoreCase));

        if (branchIndex >= 0)
        {
            branchSelector.SelectedIndex = branchIndex;

            int versionIndex = currentBranchVersions.FindIndex(v =>
                v == selectedLocalApp.InstalledVersion);

            if (versionIndex >= 0)
                versionList.SelectedIndex = versionIndex;
        }
        else if (branches.Count > 0)
        {
            branchSelector.SelectedIndex = 0;
        }

        var applyButton = new Button { Text = "Apply" };
        var cancelButton = new Button { Text = "Cancel" };

        applyButton.Click += (s, e2) =>
        {
            if (versionList.SelectedIndex < 0 || versionList.SelectedIndex >= currentBranchVersions.Count)
            {
                MessageBox.Show(dialog, "Select a version first.", MessageBoxType.Warning);
                return;
            }

            dialog.Close(currentBranchVersions[versionList.SelectedIndex]);
        };

        cancelButton.Click += (s, e2) => dialog.Close(null);

        var buttonLayout = new TableLayout { Spacing = new Size(6, 0) };
        buttonLayout.Rows.Add(new TableRow(
            new TableCell(applyButton) { ScaleWidth = true },
            cancelButton
        ));
        layout.Rows.Add(new TableRow(buttonLayout));

        dialog.Content = layout;

        var result = dialog.ShowModal(this);

        if (result == null)
            return;

        await ApplyVersionSwitch(app.AppId, result);
    }

    private async Task ApplyVersionSwitch(string appId, AppVersion version)
    {
        var progressWindow = new InstallationProgressWindow(
            main,
            services,
            appTitle: selectedLocalApp?.DisplayName ?? appId);

        progressWindow.Show();
        progressWindow.ShowUpdating();
        
        var progress = progressWindow.CreateUiScope();

        await installer.PatchApplicationAsync(appId, version, progress);

        bool shouldPin = !IsLatestInBranch(appId, version);

        installer.SetPinVersion(appId, shouldPin);

        RefreshLibrary();

        await progress.ReportAsync(1.0, "Done", ReportStatus.Success);
    }

    private bool IsLatestInBranch(string appId, AppVersion version)
    {
        var app = apps.FirstOrDefault(x =>
            string.Equals(x.AppId, appId, StringComparison.OrdinalIgnoreCase));

        if (app == null)
            return true;

        var latestInBranch = app.Versions
            .Where(v => string.Equals(v.Tag, version.Tag, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(v => v.UploadedAt)
            .FirstOrDefault();

        if (latestInBranch == null)
            return true;

        return !(latestInBranch > version);
    }

    private void OnOpenStoreClicked(object? sender, EventArgs e)
    {
        storeWindow.InitializeWindow();

        storeWindow.OnHidden += OnStoreWindowClosed;
        storeWindow.Show();
    }

    private void OnStoreWindowClosed(WindowBase window)
    {
        storeWindow.OnHidden -= OnStoreWindowClosed;
        RefreshLibrary();
    }

    private void ShowSidebar() => sidebar.Visible = true;

    private void HideSidebar()
    {
        sidebar.Visible = false;
        selectedLocalApp = null;
    }
}