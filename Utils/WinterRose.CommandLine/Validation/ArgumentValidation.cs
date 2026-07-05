namespace WinterRose.CommandLine;

public enum ValidationStage
{
    // Runs against the raw List<string> collected for this argument,
    // before ArgumentDefinition.Converter (if any) is invoked.
    Raw,

    // Runs against the final converted/bound value (ArgumentDefinition.value),
    // after conversion has completed successfully.
    Bound
}

// A single validation rule for one argument (matched by LongName).
// Predicate/MessageFactory operate on:
//   - Stage == Raw:   the List<string> of raw collected values
//   - Stage == Bound: the converted value (may be null if unset)
public sealed class ArgumentValidationRule
{
    public required ValidationStage Stage { get; init; }
    public required Func<object?, bool> Predicate { get; init; }
    public required Func<object?, string> MessageFactory { get; init; }
}

// Holds all registered validation rules, keyed by the argument's
// LongName. Positional arguments are matched the same way, since they
// also carry a LongName.
public sealed class ArgumentValidationRegistry
{
    private readonly Dictionary<string, List<ArgumentValidationRule>> rules = new();

    public void AddRule(string longName, ArgumentValidationRule rule)
    {
        if (!rules.TryGetValue(longName, out var list))
        {
            list = new List<ArgumentValidationRule>();
            rules[longName] = list;
        }

        list.Add(rule);
    }

    public IReadOnlyList<ArgumentValidationRule> GetRules(string longName)
        => rules.TryGetValue(longName, out var list)
            ? list
            : Array.Empty<ArgumentValidationRule>();
}
