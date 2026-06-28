using System;
using System.Collections.Generic;
using System.Linq;
using WinterRose.ClientHub.Feature.Interface.Windows;

namespace WinterRose.ClientHub.Feature.Interface;

public class UiManager
{
    private readonly GtkShell shell;
    private readonly Dictionary<Type, WindowBase> windows;

    public UiManager(GtkShell shell, List<WindowBase> windows)
    {
        this.shell = shell;
        this.windows = windows.ToDictionary(window => window.GetType());
    }

    public int ShownWindowCount => windows.Values.Count(w => w is {Visible: true});

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
        window.Invoke(w => window.Show());
    }

    public void Hide<T>() where T : WindowBase 
    {
        var window = GetWindow<T>();
        window?.Hide();
    }
}