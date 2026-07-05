namespace WinterRose.CommandLine.Parsing;

public static class ProgramArgumentParser
{
    public static ProgramArgumentMap Parse(
        IReadOnlyList<ProgramArgumentToken> tokens)
    {
        ProgramArgumentMap map = new();

        int index = 0;

        ConsumeLeadingPositionals(tokens, ref index, map);

        while (index < tokens.Count)
        {
            ParseToken(tokens, ref index, map);
        }

        return map;
    }

    // Positional arguments are only recognized as a leading run of bare
    // Literal tokens, before the first parameter (or any structural
    // token) appears anywhere in the input. Once a LongParameter,
    // ShortParameter, ArrayBegin, or ForwardBegin token is seen, this
    // stops permanently for the rest of the parse - a literal appearing
    // later belongs to whichever parameter precedes it (or is an orphan),
    // never to Positionals.
    private static void ConsumeLeadingPositionals(
        IReadOnlyList<ProgramArgumentToken> tokens,
        ref int index,
        ProgramArgumentMap map)
    {
        while (index < tokens.Count &&
               tokens[index].Type == ProgramArgumentTokenType.Literal)
        {
            map.Positionals.Add(tokens[index].Text ?? "");
            index++;
        }
    }

    private static void ParseToken(
        IReadOnlyList<ProgramArgumentToken> tokens,
        ref int index,
        ProgramArgumentMap map)
    {
        ProgramArgumentToken token = tokens[index];

        switch (token.Type)
        {
            case ProgramArgumentTokenType.LongParameter:
            case ProgramArgumentTokenType.ShortParameter:
                HandleParameter(tokens, ref index, map);
                return;

            case ProgramArgumentTokenType.ArrayBegin:
                // Top-level array with no owning parameter.
                List<string> anonymous = ConsumeArrayBody(tokens, ref index);
                AppendArray(map, "__anonymous__", anonymous);
                return;

            case ProgramArgumentTokenType.ForwardBegin:
                List<string> anonFwd = ConsumeForwardBody(tokens, ref index);
                map.Forwarded["__forward__"] = anonFwd;
                return;

            case ProgramArgumentTokenType.Literal:
                // orphan literal -> ignore or error later
                index++;
                return;

            default:
                index++;
                return;
        }
    }

    private static void HandleParameter(
        IReadOnlyList<ProgramArgumentToken> tokens,
        ref int index,
        ProgramArgumentMap map)
    {
        ProgramArgumentToken param = tokens[index];
        index++;

        string name = param.Text!;

        // Explicit array immediately following the parameter, e.g.
        // --include [ one two three ]
        if (index < tokens.Count &&
            tokens[index].Type == ProgramArgumentTokenType.ArrayBegin)
        {
            List<string> arrayValues = ConsumeArrayBody(tokens, ref index);
            AppendArray(map, name, arrayValues);
            return;
        }

        // Explicit forward immediately following the parameter, e.g.
        // --forward ( --fullscreen --vsync )
        if (index < tokens.Count &&
            tokens[index].Type == ProgramArgumentTokenType.ForwardBegin)
        {
            List<string> forwardValues = ConsumeForwardBody(tokens, ref index);
            map.Forwarded[name] = forwardValues;
            return;
        }

        List<string> collected = new();

        // AUTO-COLLAPSE RULE:
        // keep consuming literals until next parameter or structural token
        while (index < tokens.Count)
        {
            ProgramArgumentToken next = tokens[index];

            if (next.Type == ProgramArgumentTokenType.LongParameter ||
                next.Type == ProgramArgumentTokenType.ShortParameter ||
                next.Type == ProgramArgumentTokenType.ArrayBegin ||
                next.Type == ProgramArgumentTokenType.ArrayEnd ||
                next.Type == ProgramArgumentTokenType.ForwardBegin ||
                next.Type == ProgramArgumentTokenType.ForwardEnd)
            {
                break;
            }

            collected.Add(next.Text ?? "");
            index++;
        }

        // classification
        if (collected.Count == 0)
        {
            map.Flags[name] = true;
            return;
        }

        // IMPLICIT ARRAY RULE:
        // if this parameter has already produced a value/array, merge
        // (e.g. -ic one -ic two -ic three -> one array under "ic")
        if (map.Arrays.TryGetValue(name, out List<string>? existingArray))
        {
            existingArray.AddRange(collected);
            return;
        }

        if (map.Values.TryGetValue(name, out List<string>? existingValue))
        {
            List<string> merged = new(existingValue);
            merged.AddRange(collected);
            map.Values.Remove(name);
            map.Arrays[name] = merged;
            return;
        }

        if (collected.Count == 1)
        {
            map.Values[name] = collected;
            return;
        }

        map.Arrays[name] = collected;
    }

    // Consumes tokens from just after ArrayBegin through (and including)
    // the matching ArrayEnd, returning the literal contents in order.
    private static List<string> ConsumeArrayBody(
        IReadOnlyList<ProgramArgumentToken> tokens,
        ref int index)
    {
        index++; // skip [

        List<string> values = new();

        while (index < tokens.Count &&
               tokens[index].Type != ProgramArgumentTokenType.ArrayEnd)
        {
            ProgramArgumentToken token = tokens[index];

            if (token.Type == ProgramArgumentTokenType.Literal)
            {
                values.Add(token.Text ?? "");
            }

            index++;
        }

        // skip ]
        if (index < tokens.Count &&
            tokens[index].Type == ProgramArgumentTokenType.ArrayEnd)
        {
            index++;
        }

        return values;
    }

    // Consumes tokens from just after ForwardBegin through (and including)
    // the matching ForwardEnd, returning the raw contents in order.
    private static List<string> ConsumeForwardBody(
        IReadOnlyList<ProgramArgumentToken> tokens,
        ref int index)
    {
        index++; // skip (

        List<string> raw = new();

        while (index < tokens.Count &&
               tokens[index].Type != ProgramArgumentTokenType.ForwardEnd)
        {
            raw.Add(tokens[index].Text ?? "");
            index++;
        }

        if (index < tokens.Count &&
            tokens[index].Type == ProgramArgumentTokenType.ForwardEnd)
        {
            index++;
        }

        return raw;
    }

    // Appends into map.Arrays[name], merging with any values already
    // collected under that name instead of overwriting or dropping them.
    private static void AppendArray(
        ProgramArgumentMap map,
        string name,
        List<string> values)
    {
        if (map.Arrays.TryGetValue(name, out List<string>? existing))
        {
            existing.AddRange(values);
            return;
        }

        map.Arrays[name] = values;
    }
}
