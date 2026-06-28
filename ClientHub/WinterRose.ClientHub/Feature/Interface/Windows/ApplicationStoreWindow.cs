using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gdk;
using Gtk;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.Web;
using WinterRose.WebServer.Features.FileUploads.Models;
using Window = Gtk.Window;

namespace WinterRose.ClientHub.Feature.Interface.Windows;

public class ApplicationStoreWindow : Window, IWindow
{
    private readonly AppServerClient server;
    private ListBox appList = null!;
    private ListBox versionList = null!;
    private ComboBoxText branchSelector = null!;
    private SearchEntry filterEntry = null!;
    private Revealer sidebar = null!;

    private List<AppEntry> apps = new();

    private AppEntry? selectedApp;
    private VersionEntry? selectedVersion;
    private bool windowBuilt = false;

    private Task? dataRefreshTask;
    
    public ApplicationStoreWindow(AppServerClient server) : base("WinterHub")
    {
        this.server = server;
        DefaultWidth = 900;
        DefaultHeight = 550;
        WindowPosition = WindowPosition.Center;
    }
    
    public async Task Show()
    {
        EnsureWindow();
        
        if(dataRefreshTask == null)
            dataRefreshTask = RefreshAsync();

        Present();
    }

    public void Hide()
    {
        Hide();
    }

    private void EnsureWindow()
    {
        if (windowBuilt)
            return;

        BuildWindow();
        windowBuilt = true;
    }

    private async Task RefreshAsync()
    {
        await LoadDataAsync();
        dataRefreshTask = null;
    }

    protected override bool OnDeleteEvent(Event evnt)
    {
        Hide();

        return true;
    }

    private void BuildWindow()
    {
        Box root = new Box(Orientation.Horizontal, 8);
        root.BorderWidth = 10;

        Add(root);

        // ==========================================================
        // Left Panel
        // ==========================================================

        Box leftPanel = new Box(Orientation.Vertical, 6);

        filterEntry = new SearchEntry
        {
            PlaceholderText = "Search applications..."
        };

        leftPanel.PackStart(filterEntry, false, false, 0);

        ScrolledWindow appScroll = new ScrolledWindow();
        appScroll.ShadowType = ShadowType.In;

        appList = new ListBox();
        appList.SelectionMode = SelectionMode.Single;

        appScroll.Add(appList);

        leftPanel.PackStart(appScroll, true, true, 0);

        root.PackStart(leftPanel, true, true, 0);

        // ==========================================================
        // Sidebar
        // ==========================================================

        sidebar = new Revealer
        {
            RevealChild = false,
            TransitionType = RevealerTransitionType.SlideLeft,
            TransitionDuration = 250
        };

        Box sidebarBox = new Box(Orientation.Vertical, 8)
        {
            BorderWidth = 8,
            WidthRequest = 300
        };

        sidebar.Add(sidebarBox);

        // App title

        Label appTitle = new Label()
        {
            Xalign = 0
        };

        sidebarBox.PackStart(appTitle, false, false, 0);

        // Branch selector

        sidebarBox.PackStart(
            new Label("Branch")
            {
                Xalign = 0
            },
            false,
            false,
            0);

        branchSelector = new ComboBoxText();

        sidebarBox.PackStart(branchSelector, false, false, 0);

        // Versions

        sidebarBox.PackStart(
            new Label("Versions")
            {
                Xalign = 0
            },
            false,
            false,
            0);

        ScrolledWindow versionScroll = new ScrolledWindow();
        versionScroll.ShadowType = ShadowType.In;

        versionList = new ListBox();
        versionList.SelectionMode = SelectionMode.Single;

        versionScroll.Add(versionList);

        sidebarBox.PackStart(versionScroll, true, true, 0);

        // Bottom buttons

        Box buttonRow = new Box(Orientation.Horizontal, 6);

        Button installButton = new Button("Install");

        Button closeButton = new Button("Close");

        buttonRow.PackStart(installButton, true, true, 0);
        buttonRow.PackStart(closeButton, false, false, 0);

        sidebarBox.PackEnd(buttonRow, false, false, 0);

        root.PackStart(sidebar, false, false, 0);

        // ==========================================================
        // Events
        // ==========================================================

        closeButton.Clicked += (sender, args) => { sidebar.RevealChild = false; };

        appList.RowSelected += (sender, args) =>
        {
            if (args.Row == null)
                return;

            if (args.Row.Index < 0 || args.Row.Index >= apps.Count)
                return;

            selectedApp = apps[args.Row.Index];

            appTitle.Text = selectedApp.Name;

            PopulateBranches();

            sidebar.RevealChild = true;
        };

        branchSelector.Changed += (sender, args) => { PopulateVersions(); };

        versionList.RowSelected += (sender, args) =>
        {
            if (args.Row == null)
                return;

            if (selectedApp == null)
                return;

            string selectedBranch = branchSelector.ActiveText ?? "";

            List<VersionEntry> versions = selectedApp.Versions
                .Where(v =>
                    string.IsNullOrEmpty(selectedBranch)
                        ? string.IsNullOrEmpty(v.Tag)
                        : v.Tag == selectedBranch)
                .ToList();

            if (args.Row.Index < 0 || args.Row.Index >= versions.Count)
                return;

            selectedVersion = versions[args.Row.Index];

            bool installed = false;

            installButton.Label = installed
                ? "Update"
                : "Install";
        };

        installButton.Clicked += (sender, args) =>
        {
            if (selectedVersion == null)
                return;

            // TODO
            // Install or update selectedVersion
        };

        ShowAll();

        // Start hidden until an app is selected.
        sidebar.RevealChild = false;
    }
    
    private void PopulateApplicationList()
    {
        foreach (Widget child in appList.Children)
        {
            appList.Remove(child);
        }

        foreach (AppEntry app in apps)
        {
            ListBoxRow row = new ListBoxRow();

            Label label = new Label(app.Name)
            {
                Xalign = 0
            };

            row.Add(label);

            appList.Add(row);
        }

        appList.ShowAll();
    }
    
    private void PopulateBranches()
    {
        branchSelector.RemoveAll();

        if (selectedApp == null)
            return;

        branchSelector.Append("", "Release");

        HashSet<string> branches = new(StringComparer.OrdinalIgnoreCase);

        foreach (VersionEntry version in selectedApp.Versions)
        {
            if (string.IsNullOrWhiteSpace(version.Tag))
                continue;

            if (branches.Add(version.Tag))
            {
                branchSelector.Append(version.Tag, version.Tag);
            }
        }

        branchSelector.ActiveId = "";
    }
    
    private void PopulateVersions()
    {
        foreach (Widget child in versionList.Children)
        {
            versionList.Remove(child);
        }

        selectedVersion = null;

        if (selectedApp == null)
            return;

        string selectedBranch = branchSelector.ActiveId ?? "";

        IEnumerable<VersionEntry> versions = selectedApp.Versions
            .Where(version =>
                string.IsNullOrEmpty(selectedBranch)
                    ? string.IsNullOrEmpty(version.Tag)
                    : version.Tag == selectedBranch)
            .OrderByDescending(version => version.UploadedAt);

        foreach (VersionEntry version in versions)
        {
            ListBoxRow row = new ListBoxRow();

            Box box = new Box(Orientation.Vertical, 2);

            Label versionLabel = new Label(
                $"{version.Major}.{version.Minor}.{version.Patch}")
            {
                Xalign = 0
            };

            Label uploadLabel = new Label(
                version.UploadedAt.ToString("D"))
            {
                Xalign = 0
            };

            box.PackStart(versionLabel, false, false, 0);
            box.PackStart(uploadLabel, false, false, 0);

            row.Add(box);

            versionList.Add(row);
        }

        versionList.ShowAll();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var appEntries = await server.GetAppEntriesAsync();

            apps = appEntries;

            selectedApp = null;
            selectedVersion = null;

            sidebar.RevealChild = false;

            PopulateApplicationList();
        }
        catch (Exception ex)
        {
            apps.Clear();
            PopulateApplicationList();

            Console.WriteLine(ex);
        }
    }
}