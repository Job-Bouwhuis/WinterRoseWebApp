using System;
using Eto;
using EtoApplication = Eto.Forms.Application;
using Eto.Drawing;
using Eto.Forms;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace WinterRose.Nexus.Interface;

public class EtoShell
{
    private EtoApplication etoApp;

    public EtoShell()
    {
        Start();
    }

    public void Start()
    {
        if (etoApp is not null)
            return;

        Eto.GtkSharp.Platform p = new();
        etoApp = new EtoApplication(p);

        if (OperatingSystem.IsWindows())
             ThemeManager.ApplyNexusTheme();
    }

    public void Shutdown()
    {
        if (etoApp is null)
            return;

        etoApp.Quit();
    }

    public void Tick()
    {
        if (etoApp is null)
            return;

        etoApp.RunIteration();
    }

    [SupportedOSPlatform("windows")]
    private void ApplyWindowsDarkTheme()
    {
        IsDarkMode = IsWindowsInDarkMode();
        AccentColor = GetWindowsAccentColor();

        Color background     = IsDarkMode ? Color.FromArgb(0x20, 0x20, 0x20) : Colors.White;
        Color panelBackground = IsDarkMode ? Color.FromArgb(0x2D, 0x2D, 0x2D) : Color.FromArgb(0xF3, 0xF3, 0xF3);
        Color controlBackground = IsDarkMode ? Color.FromArgb(0x33, 0x33, 0x33) : Colors.White;
        Color foreground     = IsDarkMode ? Colors.White : Colors.Black;
        Color border         = IsDarkMode ? Color.FromArgb(0x45, 0x45, 0x45) : Color.FromArgb(0xD0, 0xD0, 0xD0);
        
        // TableLayout — used for root, toolbar, leftPanel, sidebarLayout, buttonLayout, content
        Style.Add<TableLayout>(null, (TableLayout t) => t.BackgroundColor = background);

        // TextBox — filterEntry
        Style.Add<TextBox>(null, (TextBox t) =>
        {
            t.BackgroundColor = controlBackground;
            t.TextColor = foreground;
        });

        // Button — secretIdButton, installButton, closeButton
        Style.Add<Button>(null, (Button b) =>
        {
            b.TextColor = foreground;
            b.BackgroundColor = controlBackground;
        });

        // Scrollable — appScroll, versionScroll
        Style.Add<Scrollable>(null, (Scrollable s) => s.BackgroundColor = controlBackground);

        // ListBox — appList, versionList
        Style.Add<ListBox>(null, (ListBox l) =>
        {
            l.BackgroundColor = controlBackground;
            l.TextColor = foreground;
        });

        // Panel — sidebarContainer
        Style.Add<Panel>(null, (Panel p) => p.BackgroundColor = panelBackground);

        // Label — appTitle, "Branch", "Versions" labels
        Style.Add<Label>(null, (Label l) => l.TextColor = foreground);

        // ComboBox — branchSelector
        Style.Add<ComboBox>(null, (ComboBox c) =>
        {
            c.BackgroundColor = controlBackground;
            c.TextColor = foreground;
        });
        
        Style.Add<Form>(null,f => f.BackgroundColor = background);
        Style.Add<Dialog>(null,d => d.BackgroundColor = background);

        Style.Add<Panel>(null,p => p.BackgroundColor = panelBackground);
        Style.Add<GroupBox>(null,g =>
        {
            g.BackgroundColor = panelBackground;
            g.TextColor = foreground;
        });

        Style.Add<Label>(null, l => l.TextColor = foreground);

        Style.Add<TextBox>(null,t =>
        {
            t.BackgroundColor = controlBackground;
            t.TextColor = foreground;
        });
        Style.Add<TextArea>(null,t =>
        {
            t.BackgroundColor = controlBackground;
            t.TextColor = foreground;
        });

        Style.Add<ComboBox>(null,c =>
        {
            c.BackgroundColor = controlBackground;
            c.TextColor = foreground;
        });

        Style.Add<CheckBox>(null,c => c.TextColor = foreground);
        Style.Add<RadioButton>(null,r => r.TextColor = foreground);

        
        Style.Add<GridView>(null,g =>
        {
            g.BackgroundColor = controlBackground;
        });
        Style.Add<TreeGridView>(null,t =>
        {
            t.BackgroundColor = controlBackground;
        });

        Style.Add<TabControl>(null,t => t.BackgroundColor = background);

        // Accent-driven controls
        Style.Add<LinkButton>(null,l => l.TextColor = AccentColor);
    }

    public static Color AccentColor { get; private set; } = Colors.SteelBlue;
    public static bool IsDarkMode { get; private set; }

    [SupportedOSPlatform("windows")]
    private static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            // 0 = dark mode, 1 = light mode
            return value is int i && i == 0;
        }
        catch
        {
            return true;
        }
    }

    [SupportedOSPlatform("windows")]
    private static Color GetWindowsAccentColor()
    {
        try
        {
            // DWM accent color (ColorizationColor is ARGB, stored as uint)
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            var value = key?.GetValue("AccentColor") ?? key?.GetValue("ColorizationColor");

            if (value is int argbInt)
            {
                uint argb = unchecked((uint)argbInt);
                byte a = (byte)((argb >> 24) & 0xFF);
                byte r = (byte)((argb >> 16) & 0xFF);
                byte g = (byte)((argb >> 8) & 0xFF);
                byte b = (byte)(argb & 0xFF);
                return Color.FromArgb(r, g, b, a == 0 ? (byte)255 : a);
            }
        }
        catch
        {
            // fall through to default
        }

        return Colors.Pink;
    }
}