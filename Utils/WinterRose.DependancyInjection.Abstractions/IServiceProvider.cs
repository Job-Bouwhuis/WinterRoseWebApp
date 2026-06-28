namespace WinterRose.DependancyInjection;

public interface IServiceProvider
{
    T Resolve<T>();
    object Resolve(Type type);
    IEnumerable<T> ResolveAll<T>();
    void Initialize();
}