
using System;
using System.Threading.Tasks;
using Gdk;
using WinterRose.Applications;
using WinterRose.ForgeThread;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;
using Window = Gtk.Window;
namespace WinterRose.ClientHub.Feature.Interface.Windows;

public abstract class WindowBase : Window
{
    private readonly IServiceProvider services;
    private bool windowBuilt;

    private IApplication? app;
    
    public WindowBase(string title, IServiceProvider services) : base(title)
    {
        this.services = services;
    }

    private void EnsureApp()
    {
        if (app is not null)
            return;
        app = services.Resolve<IApplication>();
    }
    
    protected abstract void BuildWindow();
    
    private void EnsureWindow()
    {
        if (windowBuilt)
            return;

        BuildWindow();
        windowBuilt = true;
    }

    protected override void OnShown()
    {
        Invoke(EnsureWindow);
        Invoke(base.OnShown);
    }

    protected override bool OnDeleteEvent(Event evnt)
    {
        Invoke(Hide);

        return true;
    }

    public void Invoke(Action<WindowBase> action)
    {
        EnsureApp();
        app.Invoke(() => action(this));
    }

    protected void Invoke(Action action)
    {
        EnsureApp();
        app.Invoke(action);
    }
}