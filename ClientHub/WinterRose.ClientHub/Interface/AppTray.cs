using System;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.ClientHub.Feature.Interface.Windows;

namespace WinterRose.ClientHub.Feature.Interface;


public class AppTray(UiManager ui)
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
            Text = "Open Store",
            Command = new Command((s, e) =>
            {
                ui.Show<ApplicationStoreWindow>();
            })
        });
        
        tray.Menu = menu;
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