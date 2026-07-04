using Eto.Drawing;
using Eto.Forms;
using WinterRose.Applications;
using WinterRose.FuzzySearching;
using WinterRose.Nexus.Interface.Preferences;
using WinterRose.Nexus.Interface.Windows;

namespace WinterRose.Nexus.Interface;


public class AppTray(UiManager ui, IApplication app)
{
    private TrayIndicator tray;
    private bool isInitialized;

    private ContextMenu menu;
    private Image icon;

    public bool IsVisible
    {
        get
        {
            if (!isInitialized)
                return false;

            return tray?.Visible ?? false;
        }
    }

    public void Initialize(Image initialIcon)
    {
        if (isInitialized)
            return;

        icon = initialIcon;

        tray = new TrayIndicator
        {
            Image = icon
        };

        menu = new ContextMenu();

        menu.Items.Add(new ButtonMenuItem
        {
            Text = "Open Library",
            Command = new Command((s, e) =>
            {
                ui.Show<LibraryWindow>();
            })
        });
        
        menu.Items.Add(new ButtonMenuItem
        {
            Text = "Open Store",
            Command = new Command((s, e) =>
            {
                ui.Show<ApplicationStoreWindow>();
            })
        });
        
        menu.Items.Add(new ButtonMenuItem
        {
            Text = "Preferences",
            Command = new Command((s, e) =>
            {
                ui.Show<UserPreferencesWindow>();
            })
        });

        menu.Items.Add(new ButtonMenuItem
        {
            Text = "Close Nexus",
            Command = new Command((s, e) =>
            {
                app.Stop();
            })
        });
        
        tray.Menu = menu;
        tray.Activated += (s, e) =>
        {
            ui.Show<LibraryWindow>();
        };
        tray.Visible = true;

        isInitialized = true;
    }

    public void SetIcon(Image newIcon)
    {
        icon = newIcon;

        if (!isInitialized)
            return;

        tray.Image = icon;
    }

    public void SetMenu(ContextMenu newMenu)
    {
        menu = newMenu;

        if (!isInitialized)
            return;

        tray.Menu = menu;
    }

    public void Show()
    {
        if (!isInitialized)
            return;

        tray.Visible = true;
    }

    public void Hide()
    {
        if (!isInitialized)
            return;

        tray.Visible = false;
    }

    public void Dispose()
    {
        if (!isInitialized)
            return;

        tray.Visible = false;
        tray.Menu = null;
        tray = null;

        isInitialized = false;
    }
}