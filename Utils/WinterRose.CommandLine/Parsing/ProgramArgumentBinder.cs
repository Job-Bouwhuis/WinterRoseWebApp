using WinterRose.CommandLine.Definitions;
using WinterRose.CommandLine.Parsing;
using WinterRose.Reflection;

namespace WinterRose.CommandLine;

internal static class ProgramArgumentBinder
{
    public static void Bind(
        ProgramArgumentMap arguments,
        ArgumentRegistry registry,
        ArgumentValidationRegistry validation)
    {
        registry.ValidatePositionalShape();

        List<string> failures = new();

        BindNamed(arguments, registry, validation, failures);
        BindPositionals(arguments, registry, validation, failures);

        if (failures.Count > 0)
            throw new ProgramArgumentsException(failures);
    }

    private static void BindNamed(
        ProgramArgumentMap arguments,
        ArgumentRegistry registry,
        ArgumentValidationRegistry? validation,
        List<string> failures)
    {
        foreach (var argDef in registry.EnumerateArguments())
        {
            // Positional-only definitions have no --/- name to match
            // against Values/Flags/Arrays/Forwarded; they're handled
            // exclusively by BindPositionals below.
            if (argDef.Position is not null)
                continue;

            bool matched = false;

            foreach (var arg in arguments.EnumerateAll())
            {
                if (arg is KeyValuePair<string, List<string>> kvp)
                {
                    if(kvp.Key == argDef.LongName ||
                       kvp.Key == argDef.ShortName)
                    {
                        matched = true;

                        if (!RunRawRules(argDef, kvp.Value, validation, failures))
                            break;

                        try
                        {
                            object? value;
                            if (argDef.Converter is null && argDef.Kind is ArgumentKind.Array or ArgumentKind.Forward)
                                value = kvp.Value.ToArray();
                            else
                                value = argDef.Converter?.Invoke(kvp.Value);
                            
                            argDef.value = value;
                        }
                        catch (Exception e)
                        {
                            failures.Add($"Argument '{kvp.Key}' could not be bound: {e.Message}");
                            break;
                        }

                        break;
                    }
                }
                else if (arg is KeyValuePair<string, bool> flagPair)
                {
                    if (flagPair.Key == argDef.LongName ||
                        flagPair.Key == argDef.ShortName)
                    {
                        matched = true;
                        argDef.value = flagPair.Value;
                        break;
                    }
                }
                else
                {
                    ReflectionHelper rh = new(arg);
                    if (rh.GetMember("Key") is MemberData member)
                    {
                        failures.Add($"Argument {member.GetValue(arg)} could not be bound.");
                        continue;
                    }
                    failures.Add("An unknown argument could not be bound.");
                }
            }

            RunBoundRules(argDef, validation, failures, wasSupplied: matched);
        }
    }

    private static void BindPositionals(
        ProgramArgumentMap arguments,
        ArgumentRegistry registry,
        ArgumentValidationRegistry? validation,
        List<string> failures)
    {
        var positionalDefs = registry.EnumeratePositionals().ToList();

        if (positionalDefs.Count == 0)
        {
            if (arguments.Positionals.Count > 0)
            {
                failures.Add(
                    $"{arguments.Positionals.Count} positional argument(s) were supplied, " +
                    "but no positional arguments are defined for this command.");
            }

            return;
        }

        // Required slots are however many leading defs have IsOptional == false
        // (ValidatePositionalShape already guaranteed these are contiguous
        // from position 0, followed only by optional slots).
        int requiredCount = positionalDefs.Count(d => !d.IsOptional);
        int totalSlots = positionalDefs.Count;
        int suppliedCount = arguments.Positionals.Count;

        if (suppliedCount < requiredCount)
        {
            failures.Add(
                $"Not enough positional arguments supplied: expected at least " +
                $"{requiredCount}, but got {suppliedCount}.");
            return;
        }

        if (suppliedCount > totalSlots)
        {
            failures.Add(
                $"Too many positional arguments supplied: expected at most " +
                $"{totalSlots}, but got {suppliedCount}.");
            return;
        }

        // Supplied values fill slots left-to-right, contiguously, in
        // order - this is what makes "second omitted, third given"
        // structurally impossible rather than something to detect: the
        // Nth supplied value always lands in the Nth slot.
        foreach (var argDef in positionalDefs)
        {
            int position = argDef.Position!.Value;
            bool wasSupplied = position < suppliedCount;

            if (wasSupplied)
            {
                string rawValue = arguments.Positionals[position];
                List<string> rawList = new() { rawValue };

                if (!RunRawRules(argDef, rawList, validation, failures))
                {
                    RunBoundRules(argDef, validation, failures, wasSupplied: true);
                    continue;
                }

                try
                {
                    object? value;

                    if (argDef.Converter is null && argDef.Kind is ArgumentKind.Array)
                        value = rawList.ToArray();
                    else if (argDef.Converter is not null)
                        value = argDef.Converter.Invoke(rawList);
                    else
                        value = rawValue;

                    argDef.value = value;
                }
                catch (Exception e)
                {
                    failures.Add(
                        $"Positional argument '{argDef.LongName}' (position {position}) " +
                        $"could not be bound: {e.Message}");
                }
            }

            RunBoundRules(argDef, validation, failures, wasSupplied);
        }
    }

    // Runs all Raw-stage rules for this argument against its raw
    // collected values. Returns false if any rule failed (caller
    // should skip conversion in that case, since converting an
    // already-invalid raw value rarely produces a meaningful error).
    private static bool RunRawRules(
        ArgumentDefinition argDef,
        List<string> rawValues,
        ArgumentValidationRegistry? validation,
        List<string> failures)
    {
        if (validation is null)
            return true;

        bool allPassed = true;

        foreach (var rule in validation.GetRules(argDef.LongName))
        {
            if (rule.Stage != ValidationStage.Raw)
                continue;

            if (!rule.Predicate(rawValues))
            {
                failures.Add(rule.MessageFactory(rawValues));
                allPassed = false;
            }
        }

        return allPassed;
    }

    // Runs all Bound-stage rules for this argument against its final
    // value. Rules still run even when the argument wasn't supplied
    // (value will be null) - e.g. a .Required() rule specifically
    // wants to fire in that case. Rules that only care about present
    // values are expected to no-op on null themselves (see Bound<T> in
    // ArgumentRuleBuilder, which skips on type mismatch/null).
    private static void RunBoundRules(
        ArgumentDefinition argDef,
        ArgumentValidationRegistry? validation,
        List<string> failures,
        bool wasSupplied)
    {
        if (validation is null)
            return;

        foreach (var rule in validation.GetRules(argDef.LongName))
        {
            if (rule.Stage != ValidationStage.Bound)
                continue;

            if (!rule.Predicate(argDef.value))
            {
                failures.Add(rule.MessageFactory(argDef.value));
            }
        }
    }
}
