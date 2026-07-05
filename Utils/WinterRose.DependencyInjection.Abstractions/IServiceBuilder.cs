namespace WinterRose.DependancyInjection;

public interface IServiceBuilder
{
    IServiceBuilder AddSingleton<TService>(bool selfInitiated = false);

    IServiceBuilder AddSingleton<TService, TImplementation>(bool selfInitiated = false)
        where TImplementation : TService;

    IServiceBuilder AddScoped<TService>();

    IServiceBuilder AddScoped<TService, TImplementation>()
        where TImplementation : TService;

    IServiceBuilder AddTransient<TService>();

    IServiceBuilder AddTransient<TService, TImplementation>()
        where TImplementation : TService;

    IServiceBuilder Configure<TService>(
        Action<TService> configuration);
    
    IServiceBuilder AddFactory<TService>(
        ServiceFactory factory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton);

    IServiceBuilder AddSingleton(Type serviceType, Type implementationType, bool selfInitiated = false);
    IServiceBuilder AddSingleton(Type implementationType, bool selfInitiated = false);
    IServiceBuilder AddScoped(Type serviceType, Type implementationType);
    IServiceBuilder AddScoped(Type implementationType);
    IServiceBuilder AddTransient(Type serviceType, Type implementationType);
    IServiceBuilder AddTransient(Type implementationType);
    
    IServiceProvider Build();
}