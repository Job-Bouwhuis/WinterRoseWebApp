using System.Diagnostics.CodeAnalysis;

namespace WinterRose.CommandLine.Definitions;

public sealed class ArgumentRegistry
{
    private readonly Dictionary<string, ArgumentDefinition> longMap = new();
    private readonly Dictionary<string, ArgumentDefinition> shortMap = new();

    public void Register(ArgumentDefinition definition)
    {
        longMap[definition.LongName] = definition;

        if (!string.IsNullOrEmpty(definition.ShortName))
            shortMap[definition.ShortName] = definition;
    }

    public bool TryGetLong(string name, [NotNullWhen(true)] out ArgumentDefinition? def)
        => longMap.TryGetValue(name, out def);

    public bool TryGetShort(string name, [NotNullWhen(true)] out ArgumentDefinition? def)
        => shortMap.TryGetValue(name, out def);

    
    public bool TryResolveAlias(
        string token,
        [NotNullWhen(true)] out ArgumentDefinition? def)
    {
        if (token.StartsWith("--"))
            return TryGetLong(token[2..], out def);

        if (token.StartsWith("-"))
            return TryGetShort(token[1..], out def);

        def = null!;
        return false;
    }

    public IEnumerable<ArgumentDefinition> EnumerateArguments()
    {
        foreach(var  arg in longMap.Values)
            yield return arg;
        // shortmap contains optional duplicates of the ones in longmap,
        // thats why we only enumerate over the longmap, it will return all arguments registered
    }

    public IEnumerable<ArgumentDefinition> EnumeratePositionals()
        => EnumerateArguments()
            .Where(d => d.Position is not null)
            .OrderBy(d => d.Position!.Value);

    // Enforces the structural rule for optional positionals: once a
    // positional slot is marked optional, every slot after it (by
    // Position index) must ALSO be optional. This makes "how many
    // values were supplied" unambiguous - supplied positionals always
    // fill slots left-to-right with no gaps, so an optional slot
    // followed by a required one would create a slot that can never be
    // reached without also supplying the optional one before it, which
    // is a contradiction in terms.
    //
    // Called at the start of ProgramArgumentBinder.Bind, not at
    // Register time, since it's a property of the full registered set
    // rather than any single registration call.
    public void ValidatePositionalShape()
    {
        List<ArgumentDefinition> positionals = EnumeratePositionals().ToList();

        if (positionals.Count == 0)
            return;

        // Duplicate Position values are also a shape error - two
        // definitions can't both claim the same slot.
        var duplicate = positionals
            .GroupBy(d => d.Position!.Value)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new ProgramArgumentsException(
                $"Multiple positional arguments declare the same Position " +
                $"({duplicate.Key}): " +
                string.Join(", ", duplicate.Select(d => d.LongName)) + ".");
        }

        bool seenOptional = false;

        foreach (var def in positionals)
        {
            if (def.IsOptional)
            {
                seenOptional = true;
                continue;
            }

            if (seenOptional)
            {
                throw new ProgramArgumentsException(
                    $"Positional argument '{def.LongName}' (position {def.Position}) is " +
                    "required, but an earlier positional slot is optional. Optional " +
                    "positional arguments must form a contiguous trailing run - once one " +
                    "positional is optional, every positional after it must be optional too.");
            }
        }

        // Positions should also be contiguous from 0 with no gaps -
        // otherwise a "slot" exists that no supplied value could ever
        // reach in order.
        for (int i = 0; i < positionals.Count; i++)
        {
            if (positionals[i].Position!.Value != i)
            {
                throw new ProgramArgumentsException(
                    $"Positional arguments must occupy a contiguous range starting at 0 " +
                    $"with no gaps. Expected position {i} but found " +
                    $"'{positionals[i].LongName}' at position {positionals[i].Position}.");
            }
        }
    }
}
