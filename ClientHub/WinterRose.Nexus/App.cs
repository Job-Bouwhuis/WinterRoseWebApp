using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinterRose.Applications;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Nexus.Interface;

namespace WinterRose.Nexus;

internal class App : Application
{
    public static ApplicationBuilder CreateBuilder()
    {
        // the builder from WinterRose.Applications
        ApplicationBuilder builder = new ApplicationBuilder();
        builder.UseApplication<App>();
        return builder;
    }

    private readonly UiManager uiManager;
    private readonly MainThread mainThread;

    public App(UiManager uiManager, MainThread mainThread, ILogger<App> logger)
    {
        this.uiManager = uiManager;
        this.mainThread = mainThread;
    }

    protected override void Tick(CancellationToken token)
    {
        uiManager.ProcessUIStuff();
        mainThread.ProcessActions();
    }
}