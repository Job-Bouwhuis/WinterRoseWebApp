using System;
using System.Diagnostics;
using System.Net.Http;
using WinterRose.Applications;
using WinterRose.Applications.ApplicationInstanceLocks;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.ClientHub.Feature.Interface;
using WinterRose.ClientHub.Feature.Interface.Windows;
using WinterRose.Configuration;
using WinterRose.DependancyInjection;
using WinterRose.Uris;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.ClientHub;

internal class Program
{
    private static App host;

    private static void Main(string[] args)
    {
        Config config = new("config/hub.txt");

        ApplicationBuilder builder = App.CreateBuilder();
        
        builder.Services.AddHttpClient()
            .Configure<HttpClient>(client =>
            {
                client.BaseAddress = new System.Uri(config.Get<string>("ServerUri"));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/winterforge")
                );
            });
        
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<Gtk.Window, ApplicationStoreWindow>();
        builder.Services.AddSingleton<AppServerClient>();
        builder.Services.AddSingleton<GtkShell>();
        builder.Services.AddSingleton<UiManager>();
        builder.Services.AddApplicationMutex();
        builder.Services.AddSingleton<IUriBootstrapListener, LinuxUriBootstrapListener>();
        builder.Services.AddSingleton<IUriBootstrapListener, WindowsUriBootstrapListener>();
        builder.Services.AddSingleton<IUriForwarder, LinuxUriForwarder>();
        builder.Services.AddSingleton<IUriForwarder, WindowsUriForwarder>();
            
        App app = builder.Build<App>();
        app.Run();
    }
}