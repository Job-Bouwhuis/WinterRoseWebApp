using System.Diagnostics.CodeAnalysis;
using WinterRose.CommandLine.Definitions;
using WinterRose.CommandLine.Parsing;

namespace WinterRose.CommandLine;

public static class ProgramArguments
{
    private static ArgumentRegistry valueRegistry;
    private static ArgumentValidationRegistry validationRegistry;
    public static ProgramArgumentBuilder CreateBuilder()
    {
        var builder = new ProgramArgumentBuilder();
        
        builder.DefineRegistryReference((r, v) =>
        {
            valueRegistry = r;
            validationRegistry = v;
        });
        return builder;
    }

    [return: MaybeNull]
    public static T Get<T>(string key)
    {
        foreach (var arg in valueRegistry.EnumerateArguments())
            if (arg.LongName == key)
                return arg.GetValue<T>();
        
        return default;
    }
    
    public static bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        foreach (var arg in valueRegistry.EnumerateArguments())
            if (arg.LongName == key)
                return arg.TryGetValue<T>(out value);

        value = default;
        return false;
    }

    public static void Parse(string[] args)
    {
        var tokens = ProgramArgumentLexer.Tokenize(args);
        var argMap = ProgramArgumentParser.Parse(tokens);
        ProgramArgumentBinder.Bind(argMap, valueRegistry, validationRegistry);
    }

    public static bool Exists(string key)
    {
        foreach (var arg in valueRegistry.EnumerateArguments())
            if (arg.LongName == key && arg.value != null)
                return true;
                

        return false;
    }
}