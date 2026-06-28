using WinterRose.Uris;

namespace WinterRose.DependancyInjection;

public class ServiceBuilder : IServiceBuilder
{
    private readonly List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();

    public IServiceBuilder AddSingleton<TService>()
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TService),
            ServiceLifetime.Singleton
        );

        descriptors.Add(descriptor);
        return this;
    }

    public IServiceBuilder AddSingleton<TService, TImplementation>()
        where TImplementation : TService
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            ServiceLifetime.Singleton
        );
        
        

        descriptors.Add(descriptor);
        return this;
    }

    public IServiceBuilder AddScoped<TService>()
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TService),
            ServiceLifetime.Scoped
        );

        descriptors.Add(descriptor);
        return this;
    }

    public IServiceBuilder AddScoped<TService, TImplementation>()
        where TImplementation : TService
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            ServiceLifetime.Scoped
        );

        descriptors.Add(descriptor);
        return this;
    }

    public IServiceBuilder AddTransient<TService>()
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TService),
            ServiceLifetime.Transient
        );

        descriptors.Add(descriptor);
        return this;
    }

    public IServiceBuilder AddTransient<TService, TImplementation>()
        where TImplementation : TService
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            ServiceLifetime.Transient
        );

        descriptors.Add(descriptor);
        return this;
    }
    
    public IServiceBuilder Configure<TService>(
        Action<TService> configuration)
    {
        if (descriptors.Count == 0)
        {
            throw new InvalidOperationException(
                "No service has been registered to configure.");
        }

        ServiceDescriptor descriptor = descriptors[^1];

        if (descriptor.ServiceType != typeof(TService))
        {
            throw new InvalidOperationException(
                $"The most recently registered service is '{descriptor.ServiceType.FullName}', not '{typeof(TService).FullName}'."
            );
        }

        descriptor.Configurators.Add(instance =>
        {
            configuration((TService)instance);
        });

        return this;
    }

    public IServiceProvider Build()
    {
        AddSingleton<IOSEnvironment, RuntimeOSEnvironment>();
        return new ServiceCollection(descriptors);
    }
}