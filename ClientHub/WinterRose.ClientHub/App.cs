using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinterRose.Applications;
using WinterRose.ClientHub.Feature.Interface;
using WinterRose.ClientHub.Feature.Interface.Windows;
using WinterRose.DependancyInjection.Logging;

namespace WinterRose.ClientHub;

internal class App : Application
{
    public static ApplicationBuilder CreateBuilder()
    {
        ApplicationBuilder builder = new ApplicationBuilder();
        builder.UseApplication<App>();
        return builder;
    }

    private int delay = 1000;

    private readonly UiManager uiManager;
    private Task ServiceTask;

    public App(UiManager uiManager, ILogger<App> logger)
    {
        this.uiManager = uiManager;
    }

    public void Start()
    {
    }

    public void Stop()
    {
    }

    protected override void Tick(CancellationToken token)
    {
        Gtk.Application.RunIteration();
    }
}