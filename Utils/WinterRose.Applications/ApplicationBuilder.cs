
using System;
using System.Linq;
using System.Reflection;
using WinterRose.DependancyInjection;
using IServiceProvider = System.IServiceProvider;

namespace WinterRose.Applications;

public class ApplicationBuilder
{
    private Func<DependancyInjection.IServiceProvider, Application>? APPLICATION_FACTORY;
    private ServiceBuilder ServiceBuilder = new ServiceBuilder();
    public ServiceBuilder Services => ServiceBuilder;

    public ApplicationBuilder UseApplication<TApplication>()
        where TApplication : Application
    {
        APPLICATION_FACTORY = provider =>
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
        DependancyInjection.IServiceProvider services = ServiceBuilder.Build();
        
        if (APPLICATION_FACTORY == null)
            throw new InvalidOperationException("No Application type configured.");

        Application app = APPLICATION_FACTORY(services);

        return app;
    }
}