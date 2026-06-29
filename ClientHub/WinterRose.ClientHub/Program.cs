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
using WinterRose.ClientHub;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.ClientHub.Feature.Interface;
using WinterRose.ClientHub.Feature.Interface.Windows;
using WinterRose.ClientHub.Feature.Uri;
using WinterRose.Configuration;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

_ = typeof(ByteArrayValueProvider);

App host = null;

// this is for testing purposes.
//args = ["winterrose://show-window"];

ServiceBuilder lockServicesBuilder = new ServiceBuilder();
lockServicesBuilder.AddApplicationMutex("winterrose.hub");
lockServicesBuilder.AddUriForwardersOnly("winterrose.hub");
var lockServices = lockServicesBuilder.Build();

{
    var mutex = lockServices.Resolve<IApplicationMutex>();

    if (!mutex.IsFirstInstance)
    {
        if (args.Length == 0)
            // if no args are provided, we will instruct the first instance to show the window.
            args = ["winterrose://show-window"];

        var forwarder = lockServices.Resolve<IUriForwarder>();
        forwarder.ForwardAsync(args[0]).GetAwaiter().GetResult();
        lockServices.Dispose();
        return;
    }
}

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
builder.Services.AddSingleton<IUriHandler, ShutdownUriHandler>();

builder.Services.AddSingleton<MainThread>();

builder.Services.AddSingleton<ApplicationInstaller>();
builder.Services.AddSingleton<ClientAppRepository>();

// adds linux and windows variants of the Uri services which the DI container resolves for whatever OS the
// app runs on
builder.Services.AddUriListener("winterrose.hub", "winterrose", "WinterRose Hub");

// Building the app adds loggers automatically
App app = builder.Build<App>();

app.BeginUriListener();

if (args.Length > 0)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        await app.Services.Resolve<IUriForwarder>().ForwardAsync(args[0]);
    });
}

app.Run();

await app.DisposeAsync();
lockServices.Dispose();

bool ValidateAppMutex(ServiceCollection services, string[] args)
{
    return false;
}