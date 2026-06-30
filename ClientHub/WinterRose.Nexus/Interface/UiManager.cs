using System;
using System.Collections.Generic;
using System.Linq;
using WinterRose.Nexus.Interface.Windows;

namespace WinterRose.Nexus.Interface;

public class UiManager
{
    private readonly EtoShell shell;
    private readonly MainThread main;
    private readonly Dictionary<Type, WindowBase> windows;

    public UiManager(EtoShell shell, MainThread main, List<WindowBase> windows)
    {
        this.shell = shell;
        this.main = main;
        this.windows = windows.ToDictionary(window => window.GetType());
        foreach (var window in windows)
            window.InitializeWindow();
    }

    public int ShownWindowCount => windows.Values.Count(w => w is { Visible: true });

    public void StopUi()
    {
        shell.Shutdown();
    }

    private WindowBase GetWindow<T>() where T : WindowBase
    {
        if (windows.TryGetValue(typeof(T), out WindowBase window))
            return window;
        throw new WindowNotFoundException<T>();
    }

    public void Show<T>() where T : WindowBase
    {
        var window = GetWindow<T>();
        Invoke(() =>
        {
            window.ShowActivated = true;
            window.Visible = true;
            window.Show();
        });
    }

    public void Hide<T>() where T : WindowBase
    {
        var window = GetWindow<T>();
        Invoke(window.Close);
    }

    public void Invoke(Action action) => main.Invoke(action);

    internal void ProcessUIStuff() => shell.Tick();
}