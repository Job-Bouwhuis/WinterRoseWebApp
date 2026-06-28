using System;
using System.Collections.Generic;
using System.Linq;
using WinterRose.ClientHub.Feature.Interface.Windows;
using WinterRose.DependancyInjection;

namespace WinterRose.ClientHub.Feature.Interface;

public class UiManager
{
    private readonly GtkShell shell;
    private readonly Dictionary<Type, IWindow> windows;

    public UiManager(GtkShell shell, List<IWindow> windows)
    {
        this.shell = shell;
        this.windows = windows.ToDictionary(window => window.GetType());
        StartUi();
    }

    public void StartUi()
    {
        shell.Start();
    }

    public void StopUi()
    {
        shell.Shutdown();
    }

    private IWindow GetWindow<T>() where T : IWindow
    {
        if (windows.TryGetValue(typeof(T), out IWindow window))
            return window;
        throw new WindowNotFoundException<T>();
    }

    public void Show<T>() where T : IWindow
    {
        shell.Start();

        var window = GetWindow<T>();
        
        window.Show();
    }

    public void Hide<T>() where T : IWindow 
    {
        var window = GetWindow<T>();
        window?.Hide();
    }
}

internal class WindowNotFoundException<T>() : Exception($"Window of type {typeof(T).Name} not registered");