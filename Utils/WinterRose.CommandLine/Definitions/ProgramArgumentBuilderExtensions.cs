using WinterRose.CommandLine.Definitions;

namespace WinterRose.CommandLine;

public static class ProgramArgumentBuilderExtensions
{
    public static void Flag(
        this ProgramArgumentBuilder builder,
        string longName,
        string? shortName = null,
        int? position = null,
        bool optional = false)
    {
        builder.Register(new ArgumentDefinition
        {
            LongName = longName,
            ShortName = shortName,
            Kind = ArgumentKind.Flag,
            Position = position,
            IsOptional = optional
        });
    }
    
    
    public static ArgumentRuleBuilder Value<T>(
        this ProgramArgumentBuilder builder,
        string longName,
        string? shortName = null,
        Func<List<string>, object>? converter = null,
        int? position = null,
        bool optional = false)
    {
        Func<List<string>, object>? finalConverter;

        if (converter != null)
        {
            finalConverter = v => converter(v);
        }
        else
        {
            finalConverter = ArgumentConverterRegistry.TryResolve(typeof(T));
        }

        builder.Register(new ArgumentDefinition
        {
            LongName = longName,
            ShortName = shortName,
            Kind = ArgumentKind.Value,
            Converter = finalConverter,
            Position = position,
            IsOptional = optional
        });

        return builder.GetRuleBuilderFor(longName);
    }
    
    public static ArgumentRuleBuilder Array<T>(
        this ProgramArgumentBuilder builder,
        string longName,
        string? shortName = null,
        Func<List<string>, T[]>? converter = null,
        int? position = null,
        bool optional = false)
    {
        Func<List<string>, object>? finalConverter;

        if (converter != null)
        {
            finalConverter = v => converter(v);
        }
        else
        {
            finalConverter = ArgumentConverterRegistry.TryResolve(typeof(T));
        }
        
        builder.Register(new ArgumentDefinition
        {
            LongName = longName,
            ShortName = shortName,
            Kind = ArgumentKind.Array,
            Converter = converter != null
                ? finalConverter
                : null,
            Position = position,
            IsOptional = optional
        });

        return builder.GetRuleBuilderFor(longName);
    }
    
    public static ArgumentRuleBuilder Forward(
        this ProgramArgumentBuilder builder,
        string longName,
        string? shortName = null,
        int? position = null,
        bool optional = false)
    {
        builder.Register(new ArgumentDefinition
        {
            LongName = longName,
            ShortName = shortName,
            Kind = ArgumentKind.Forward,
            Position = position,
            IsOptional = optional
        });

        return builder.GetRuleBuilderFor(longName);
    }
}
