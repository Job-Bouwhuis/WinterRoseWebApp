using System.Reflection;
using WinterRose.Uris;

namespace WinterRose.DependancyInjection;

public class ServiceDescriptor
{
    public Type ServiceType { get; set; }
    public Type ImplementationType { get; set; }

    public ServiceLifetime Lifetime { get; set; }
    public bool SelfInitiated { get; private set; }

    public List<Action<object>> Configurators { get; } = [];

    public ServiceDescriptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime,
        bool selfInitiated = false)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        SelfInitiated = selfInitiated;
    }
    
    public bool IsCompatible(IOSEnvironment os)
    {
        OSConstraintAttribute? constraint = ImplementationType.GetCustomAttribute<OSConstraintAttribute>();

        if (constraint == null)
            return true;

        return constraint.Platforms.Contains(os.Platform);
    }
}