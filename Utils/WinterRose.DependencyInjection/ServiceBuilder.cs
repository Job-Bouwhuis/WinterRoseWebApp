using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris;

namespace WinterRose.DependancyInjection;

public class ServiceBuilder : IServiceBuilder
{
    private readonly List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();
    private readonly List<ServiceFactoryDescriptor> factoryDescriptors = new();

    public IServiceBuilder AddSingleton<TService>(bool selfInitiated = false)
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TService),
            ServiceLifetime.Singleton,
            selfInitiated
        );

        descriptors.Add(descriptor);
        return this;
    }

    public IServiceBuilder AddSingleton<TService, TImplementation>(bool selfInitiated = false)
        where TImplementation : TService
    {
        ServiceDescriptor descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            ServiceLifetime.Singleton,
            selfInitiated
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

        Type serviceType = typeof(TService);
        bool assignable = descriptor.ServiceType.IsAssignableFrom(serviceType)
                          || descriptor.ServiceType == serviceType
                          || descriptor.ImplementationType.IsAssignableFrom(serviceType)
                          || descriptor.ImplementationType == serviceType;
        
        if (!assignable)
        {
            throw new InvalidOperationException(
                $"The most recently registered service is '{descriptor.ServiceType.FullName}', not '{serviceType.FullName}'."
            );
        }

        descriptor.Configurators.Add(instance =>
        {
            configuration((TService)instance);
        });

        return this;
    }

    // ServiceBuilder implementation — one private helper, six thin overloads

    private IServiceBuilder Add(Type serviceType, Type implementationType, ServiceLifetime lifetime, bool  selfInitiated = false)
    {
        // sanity: if registering an open generic pair, both must be open generic definitions
        if (serviceType.IsGenericTypeDefinition != implementationType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Service type '{serviceType}' and implementation type '{implementationType}' " +
                $"must both be open generic definitions or both be closed types.");
        }

        descriptors.Add(new ServiceDescriptor(serviceType, implementationType, lifetime, selfInitiated));
        return this;
    }

    public IServiceBuilder AddSingleton(Type serviceType, Type implementationType, bool selfInitiated = false)
        => Add(serviceType, implementationType, ServiceLifetime.Singleton, selfInitiated);

    public IServiceBuilder AddSingleton(Type implementationType,  bool selfInitiated = false)
        => Add(implementationType, implementationType, ServiceLifetime.Singleton, selfInitiated);

    public IServiceBuilder AddScoped(Type serviceType, Type implementationType)
        => Add(serviceType, implementationType, ServiceLifetime.Scoped);

    public IServiceBuilder AddScoped(Type implementationType)
        => Add(implementationType, implementationType, ServiceLifetime.Scoped);

    public IServiceBuilder AddTransient(Type serviceType, Type implementationType)
        => Add(serviceType, implementationType, ServiceLifetime.Transient);

    public IServiceBuilder AddTransient(Type implementationType)
        => Add(implementationType, implementationType, ServiceLifetime.Transient);
    
    public IServiceBuilder AddFactory<TService>(
        ServiceFactory factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : notnull
    {
        factoryDescriptors.Add(new ServiceFactoryDescriptor(
            typeof(TService),
            (provider) => factory(provider),   // discard the unused typeArguments
            lifetime));
        return this;
    }
    
    public IServiceProvider Build()
    {
        this.AddLogging();
        AddSingleton<IOSEnvironment, RuntimeOSEnvironment>();
        return new ServiceCollection(descriptors, factoryDescriptors);
    }
}