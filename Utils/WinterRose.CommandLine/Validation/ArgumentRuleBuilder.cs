namespace WinterRose.CommandLine;

/// <summary>
/// Fluent entry point: registry.RulesFor("port").Bound<int>(...).Raw(...)
/// </summary>
public static class ArgumentValidationRegistryExtensions
{
    public static ArgumentRuleBuilder RulesFor(
        this ArgumentValidationRegistry registry,
        string longName)
        => new(registry, longName);
}

/// <summary>
/// Accumulates rules for a single argument (by LongName) and writes them
/// into the ArgumentValidationRegistry as each fluent call is made.
/// Every method returns `this`, so calls can be chained freely and in
/// any order/stage mixture for the same argument.
/// </summary>
public sealed class ArgumentRuleBuilder
{
    private readonly ArgumentValidationRegistry registry;
    private readonly string longName;

    internal ArgumentRuleBuilder(ArgumentValidationRegistry registry, string longName)
    {
        this.registry = registry;
        this.longName = longName;
    }

    /// <summary>
    /// Validates the raw List<string> collected for this argument,
    /// before any Converter runs. Useful for checks like "must have
    /// exactly N values" or "must not be empty" that don't need the
    /// converted type.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public ArgumentRuleBuilder Raw(
        Func<List<string>, bool> predicate,
        Func<List<string>, string> message)
    {
        registry.AddRule(longName, new ArgumentValidationRule
        {
            Stage = ValidationStage.Raw,
            Predicate = raw => predicate((List<string>)raw!),
            MessageFactory = raw => message((List<string>)raw!)
        });

        return this;
    }

    /// <summary>
    /// Shorthand for the common case: rule only cares about a single
    /// raw string (e.g. a Value-kind argument). Runs against the whole
    /// raw list, but only inspects element [0] for convenience; if the
    /// list is empty, the rule is treated as satisfied (nothing to
    /// check yet - absence is a separate concern from validity).
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public ArgumentRuleBuilder RawValue(
        Func<string, bool> predicate,
        Func<string, string> message)
    {
        return Raw(
            raw => raw.Count == 0 || predicate(raw[0]),
            raw => message(raw.Count == 0 ? "" : raw[0]));
    }

    /// <summary>
    /// Validates the final converted/bound value. T should match
    /// whatever ArgumentDefinition.Converter produces (or the raw
    /// string/string[] if there is no converter). If the bound value is
    /// null, or is not of type T, the rule is skipped rather than
    /// thrown from here - a type mismatch is a binding concern, not a
    /// validation concern, and missing/optional values shouldn't fail
    /// rules meant for present values.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="messageFactory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ArgumentRuleBuilder Bound<T>(
        Func<T, bool> predicate,
        Func<T, string> messageFactory)
    {
        registry.AddRule(longName, new ArgumentValidationRule
        {
            Stage = ValidationStage.Bound,
            Predicate = value => value is not T typed || predicate(typed),
            MessageFactory = value => value is T typed
                ? messageFactory(typed)
                : $"Argument '{longName}' failed validation."
        });

        return this;
    }

    // Convenience: requires the bound value to be non-null (i.e. the
    // argument was actually supplied and successfully converted).
    // Mainly useful for positional arguments where IsOptional = false
    // isn't itself checked by the binder beyond count range - if you
    // want an explicit friendly message for "this one's mandatory",
    // add this rule too.
    public ArgumentRuleBuilder Required(string? message = null)
    {
        registry.AddRule(longName, new ArgumentValidationRule
        {
            Stage = ValidationStage.Bound,
            Predicate = value => value is not null,
            MessageFactory = _ => message ?? $"Argument '{longName}' is required but was not supplied."
        });

        return this;
    }
}