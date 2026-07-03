using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Eto.Drawing;
using WinterRose.Applications;
using WinterRose.Applications.ApplicationInstanceLocks;
using WinterRose.Nexus;
using WinterRose.Configuration;
using WinterRose.DependancyInjection;
using WinterRose.Nexus.Interface;
using WinterRose.Nexus.Interface.Dialogs;
using WinterRose.Nexus.Interface.Windows;
using WinterRose.Nexus.Services;
using WinterRose.Nexus.Shared;
using WinterRose.Nexus.Uri;
using WinterRose.Uris;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

_ = typeof(ByteArrayValueProvider);

// this is for testing purposes.
//args = ["winterrose://show-window"];


ServiceBuilder lockServicesBuilder = new ServiceBuilder();
lockServicesBuilder.AddApplicationMutex("winterrose.hub");
lockServicesBuilder.AddUriForwardersOnly("winterrose.hub");
var lockServices = lockServicesBuilder.Build();

var mutex = lockServices.Resolve<IApplicationMutex>();

if (!mutex.IsFirstInstance)
{
    if (args.Length == 0)
        // if no args are provided, we will instruct the first instance to show the window.
        args = ["winterrose://show-window"];
    
    // we get the forwarder that was made for the OS we run on
    var forwarder = lockServices.Resolve<IUriForwarder>();
    await forwarder.ForwardAsync(args[0]);
    lockServices.Dispose();
    return;
}

Config config = new("config/hub.txt");

ApplicationBuilder builder = App.CreateBuilder();

// The Http client to talk to the Nexus Registry
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
builder.Services.AddSingleton<WindowBase, LibraryWindow>();

builder.Services.AddSingleton<IApplicationLauchManager, ApplicationLauchManager>();
// DI resolves only the one for the OS the app runs on
builder.Services.AddSingleton<IApplicationLauncher, WindowsApplicationStarter>();
builder.Services.AddSingleton<IApplicationLauncher, LinuxApplicationStarter>();
builder.Services.AddSingleton<ApplicationStarter>();

builder.Services.AddSingleton<IModalDialog, EtoModalDialog>();

builder.Services.AddSingleton<AppServerClient>();
builder.Services.AddSingleton<EtoShell>();
builder.Services.AddSingleton<UiManager>();
builder.Services.AddApplicationMutex("winterrose.hub");

// consuming services request a IUriHandler[] and get all we register here
builder.Services.AddSingleton<IUriHandler, URiLoggerHandler>();
builder.Services.AddSingleton<IUriHandler, ShowWindowUriHandler>();
builder.Services.AddSingleton<IUriHandler, ShutdownUriHandler>();
builder.Services.AddSingleton<IUriHandler, ApplicationUpdateUriHandler>();

builder.Services.AddSingleton<MainThread>();

builder.Services.AddSingleton<ApplicationInstaller>();
builder.Services.AddSingleton<ClientAppRepository>();

builder.Services.AddSingleton(typeof(IAsyncEventQueue<>), typeof(AsyncEventQueue<>));

builder.Services.AddSingleton<AppTray>();

// adds linux and windows variants of the Uri services which the DI container resolves for whatever OS the
// app runs on
builder.Services.AddUriListener("winterrose.hub", "winterrose", "WinterRose Hub");

// Building the app adds loggers automatically
await using App app = builder.Build<App>();

app.BeginUriListener();

if (args.Length > 0)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        await app.Services.Resolve<IUriForwarder>().ForwardAsync(args[0]);
    });
}

if(!File.Exists(""))

app.Services.Resolve<AppTray>().Initialize(new Bitmap("Icons\\NexusLogo.bmp"));

app.Run();

lockServices.Dispose();