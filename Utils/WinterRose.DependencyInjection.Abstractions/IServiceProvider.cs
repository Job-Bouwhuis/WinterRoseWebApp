namespace WinterRose.DependancyInjection;

public interface IServiceProvider : IDisposable
{
    T Resolve<T>();
    object Resolve(Type type);
    IEnumerable<T> ResolveAll<T>();
    void Initialize();

}