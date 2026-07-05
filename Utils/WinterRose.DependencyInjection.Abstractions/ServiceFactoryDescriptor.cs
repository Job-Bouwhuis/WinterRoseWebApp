namespace WinterRose.DependancyInjection;

public sealed class ServiceFactoryDescriptor
{
    /// <summary>
    /// The service type this factory handles.
    /// May be an open generic type definition (e.g. typeof(ILogger&lt;&gt;))
    /// or a plain concrete/interface type (e.g. typeof(IEventBus)).
    /// </summary>
    public Type ServiceType { get; }
    public ServiceFactory Factory { get; }
    public ServiceLifetime Lifetime { get; }

    public ServiceFactoryDescriptor(
        Type serviceType,
        ServiceFactory factory,
        ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        Factory     = factory;
        Lifetime    = lifetime;
    }
}