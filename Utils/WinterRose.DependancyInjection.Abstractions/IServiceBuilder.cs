namespace WinterRose.DependancyInjection;

public interface IServiceBuilder
{
    IServiceBuilder AddSingleton<TService>();

    IServiceBuilder AddSingleton<TService, TImplementation>()
        where TImplementation : TService;

    IServiceBuilder AddScoped<TService>();

    IServiceBuilder AddScoped<TService, TImplementation>()
        where TImplementation : TService;

    IServiceBuilder AddTransient<TService>();

    IServiceBuilder AddTransient<TService, TImplementation>()
        where TImplementation : TService;

    IServiceBuilder Configure<TService>(
        Action<TService> configuration);

    IServiceProvider Build();
}