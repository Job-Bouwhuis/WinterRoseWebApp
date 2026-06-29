using System.Text.Json.Serialization.Metadata;
using WinterRose.Applications;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris.UriVerifiers;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Uris;

public static class Extensions
{
    extension(IServiceBuilder services)
    {
        /// <summary>
        /// Adds services that configure the OS so that the custom URI scheme is registered to this application
        /// and adds services to send and receive URI forwarding from one process to the other.
        /// Use in combination with a Mutex application lock in order to determine whether to forward a URI or process it with the current app instance.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="scheme"></param>
        /// <param name="displayName"></param>
        public void AddUriListener(string appId, string scheme, string displayName)
        {
            services.AddSingleton<UriOptions>().Configure<UriOptions>(options =>
            {
                options.AppId = appId;
                options.Scheme = scheme;
                options.DisplayName = displayName;
            });
            services.AddSingleton<IUriBootstrapListener, LinuxUriBootstrapListener>();
            services.AddSingleton<IUriBootstrapListener, WindowsUriBootstrapListener>();

            services.AddSingleton<IUriSchemeRegistar, WindowsUriSchemeRegistar>();
            services.AddSingleton<IUriSchemeRegistar, LinuxUriSchemeRegistar>();
           
            services.AddSingleton<IUriForwarder, LinuxUriForwarder>();
            services.AddSingleton<IUriForwarder, WindowsUriForwarder>();
            
            services.AddSingleton<UriListener>();

        }

        public void AddUriForwardersOnly(string appId)
        {
            services.AddSingleton<UriOptions>().Configure<UriOptions>(options =>
            {
                options.AppId = appId;
            });
            
            services.AddSingleton<IUriForwarder, LinuxUriForwarder>();
            services.AddSingleton<IUriForwarder, WindowsUriForwarder>();
        }
    }

    extension(IApplication app)
    {
        public void BeginUriListener()
        {
            app.Services.Resolve<UriListener>();
        }
    }
}

internal class UriListener
{
    public Action<UriContext> OnUri { get; set; }
    
    public UriListener(IServiceProvider services, IUriBootstrapListener listener, CancellationToken ct, ILogger<UriListener> logger)
    {
        var handlers = services.ResolveAll<IUriHandler>().ToArray();
        
        if(handlers.Length == 0)
            logger.Warning("UriListener has no handlers. wont start listening!");
        
        listener.StartListening(async uri =>
        {
            var ctx = UriContextParser.Parse(uri);
            
            foreach (var handler in handlers)
            {
                if(handler.CanHandle(ctx))
                    await handler.HandleAsync(ctx);
            }
        }, ct);
    }
}