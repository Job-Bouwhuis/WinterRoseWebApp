
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Eto.Forms;
using WinterRose.Applications;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;
using Window = Gtk.Window;
namespace WinterRose.ClientHub.Feature.Interface.Windows;

public abstract class WindowBase : Form
{
    protected readonly MainThread main;
    protected readonly IServiceProvider services;

    private IApplication? app;
    
    public WindowBase(string title, MainThread main, IServiceProvider services)
    {
        Title = title;
        this.main = main;
        this.services = services;
    }

    internal void InitializeWindow()
    {
        EnsureApp();
        Initialize();
        Content = BuildContent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        main.Invoke(() => Visible = false);
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