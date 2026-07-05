using WinterRose.AnonymousTypes;

namespace WinterRose.CommandLine.Definitions;

public sealed class ArgumentDefinition
{
    public string LongName { get; init; }
    public string? ShortName { get; init; }
    public ArgumentKind Kind { get; init; }
    public Anonymous Metadata { get; } = new();

    /// <summary>
    /// If set, this argument is bound from ProgramArgumentMap.Positionals
    /// at this zero-based index instead of from a --/- flagged parameter.
    /// Leave null for normal named parameters.
    /// </summary>
    public int? Position { get; init; }
    
    /// <summary>
    /// Only meaningful when Position is set. Optional positional slots
    /// must form a contiguous trailing run across all registered
    /// positional definitions (no required slot may follow an optional
    /// one) - this shape is enforced by ArgumentRegistry.ValidatePositionalShape,
    /// which runs at the start of value binding
    /// </summary>
    public bool IsOptional { get; init; }

    internal object? value { get; set; }

    public T? GetValue<T>()
    {
        if (value is null)
            return default;
        
        if(value is not T v)
            throw new ProgramArgumentsException($"Argument '{LongName}' is not of type '{typeof(T).Name}' but of '{value.GetType().Name}'.");
        return v;
    }

    public bool TryGetValue<T>(out T v)
    {
        if (value is null)
        {
            v = default;
            return false;
        }
        
        if(value is not T va)
            throw new ProgramArgumentsException($"Argument '{LongName}' is not of type '{typeof(T).Name}' but of '{value.GetType().Name}'.");
        v = va;
        return true;
    }
    
    public Func<List<string>, object?>? Converter { get; init; }
}
