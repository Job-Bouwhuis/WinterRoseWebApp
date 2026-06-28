
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using WinterRose.DependancyInjection;
using IServiceProvider = System.IServiceProvider;

namespace WinterRose.Applications;

public class ApplicationBuilder
{
    private Func<DependancyInjection.IServiceProvider, Application>? applicationFactory;
    private IServiceBuilder serviceBuilder = new ServiceBuilder();
    public IServiceBuilder Services => serviceBuilder;

    public ApplicationBuilder UseApplication<TApplication>()
        where TApplication : Application
    {
        applicationFactory = provider =>
        {
            ConstructorInfo constructor = typeof(TApplication)
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            object[] args = constructor
                .GetParameters()
                .Select(p => provider.Resolve(p.ParameterType))
                .ToArray();

            return (Application)constructor.Invoke(args);
        };

        return this;
    }

    public T Build<T>() where T : Application => (T)Build();
    
    public Application Build()
    {
        Application app = null;
        serviceBuilder.AddFactory<CancellationToken>(services =>
        {
            return app!.cancelSource.Token;
        }, ServiceLifetime.Transient);
        
        DependancyInjection.IServiceProvider services = serviceBuilder.Build();
        
        if (applicationFactory == null)
            throw new InvalidOperationException("No Application type configured.");
        
        app = applicationFactory(services);
        app.Services = services;
        services.Initialize();
        
        return app;
    }
}