namespace WinterRose.CommandLine;

public static class ArgumentConverterRegistry
{
    private static readonly Dictionary<Type, Func<List<string>, object?>> converters = new();

    static ArgumentConverterRegistry()
    {
        Register<string>(static v => v.Count > 0 ? v[0] : "");
        Register<int>(static v => int.Parse(v[0]));
        Register<bool>(static v => bool.Parse(v[0]));
        Register<float>(static v => float.Parse(v[0]));
        Register<double>(static v => double.Parse(v[0]));
        Register<long>(static v => long.Parse(v[0]));
        Register<Guid>(static v => Guid.Parse(v[0]));
    }

    public static void Register<T>(Func<List<string>, T> converter)
    {
        converters[typeof(T)] = l => converter(l);
    }

    public static Func<List<string>, object?>? Resolve(Type type)
    {
        if (converters.TryGetValue(type, out Func<List<string>, object?>? del))
            return values => del(values);

        return null;
    }

    public static Func<List<string>, object?>? TryResolve(Type t)
    {
        if (!converters.TryGetValue(t, out Func<List<string>, object?>? del))
            return null;

        return values => del(values);
    }
}