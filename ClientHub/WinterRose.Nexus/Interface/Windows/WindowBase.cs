using System;
using System.ComponentModel;
using Eto.Forms;
using WinterRose.Applications;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Nexus.Interface.Windows;

public abstract class WindowBase : Form
{
    protected readonly MainThread main;
    protected readonly IServiceProvider services;

    public event Action<WindowBase> OnHidden = delegate { };
    
    private IApplication? app;
    private bool initialized;

    public WindowBase(string title, MainThread main, IServiceProvider services)
    {
        Title = title;
        this.main = main;
        this.services = services;
    }

    public void Hide()
    {
        Visible = false;
        OnHidden.Invoke(this);
    }
    
    internal void InitializeWindow()
    {
        if (initialized)
            return;
        initialized = true;
        
        EnsureApp();
        Initialize();
        Content = BuildContent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        main.Invoke(Hide);
        
        base.OnClosing(e);
    }

    private void EnsureApp()
    {
        if (app is not null)
            return;
        app = services.Resolve<IApplication>();
    }
    
    protected abstract Control BuildContent();

}