namespace WinterRose.DependancyInjection;

public class ResolutionContext
{
    public ServiceCollection Container { get; }

    public Dictionary<Type, object> ScopedInstances { get; }
    public Stack<Type> ResolutionStack { get; }

    public ResolutionContext(ServiceCollection container)
    {
        Container = container;

        ScopedInstances = new Dictionary<Type, object>();
        ResolutionStack = new Stack<Type>();
    }
}