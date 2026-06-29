using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using WinterRose.Applications;
using WinterRose.Applications.ApplicationInstanceLocks;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.ClientHub.Feature.Interface;
using WinterRose.ClientHub.Feature.Interface.Windows;
using WinterRose.ClientHub.Feature.Uri;
using WinterRose.Configuration;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.ClientHub;

internal class Program
{
    private static App host;

    private static void Main(string[] args)
    {
        // this is for testing purposes.
        args = ["winterrose://show-window"];
        
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        Config config = new("config/hub.txt");
        
        ApplicationBuilder builder = App.CreateBuilder();
        
        // The Http client to talk to the remote server
        builder.Services.AddHttpClient()
            .Configure<HttpClient>(client =>
            {
                client.BaseAddress = new Uri(config.Get<string>("ServerUri"));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/winterforge")
                );
            });
        
        builder.Services.AddSingleton<WindowBase, ApplicationStoreWindow>();
        builder.Services.AddSingleton<WindowBase, TestWindow>();
        
        builder.Services.AddSingleton<AppServerClient>();
        builder.Services.AddSingleton<EtoShell>();
        builder.Services.AddSingleton<UiManager>();
        builder.Services.AddApplicationMutex("winterrose.hub");
        builder.Services.AddSingleton<IUriHandler, URiLoggerHandler>();
        builder.Services.AddSingleton<IUriHandler, ShowWindowUriHandler>();
        builder.Services.AddSingleton<MainThread>();

        builder.Services.AddSingleton<ApplicationInstaller>();
        builder.Services.AddSingleton<ClientAppRepository>();
        
        // adds linux and windows variants of the Uri services which the DI container resolves for whatever OS the
        // app runs on
        builder.Services.AddUriListener("winterrose.hub", "winterrose", "WinterRose Hub");
        
        // Building the app adds loggers automatically
        App app = builder.Build<App>();

        if (ValidateAppMutex(app, args)) 
            return;

        if (args.Length > 0)
        {
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                await app.Services.Resolve<IUriForwarder>().ForwardAsync(args[0]);
            });
        }
        
        SetupListener(app);
        
        app.Run();
    }

    private static void SetupListener(App app)
    {
        app.BeginUriListener();
    }

    private static bool ValidateAppMutex(App app, string[] args)
    {
        var mutex = app.Services.Resolve<IApplicationMutex>();

        if (!mutex.IsFirstInstance)
        {
            if (args.Length == 0)
            {
                var log = app.Services.Resolve<ILogger<Program>>();
                log.Warning("invoked as non-owner, and no args are provided. closing without doing anything.");
                // log is flushed regardless of how the app is shutdown
                return true;
            }
            
            var forwarder = app.Services.Resolve<IUriForwarder>();
            forwarder.ForwardAsync(args[0]).GetAwaiter().GetResult();
            return true;
        }

        return false;
    }
}