using System.Collections;

namespace WinterRose.CommandLine.Parsing;

public sealed class ProgramArgumentMap
{
    public Dictionary<string, List<string>> Values { get; } = new();
    public Dictionary<string, bool> Flags { get; } = new();
    public Dictionary<string, List<string>> Arrays { get; } = new();
    public Dictionary<string, List<string>> Forwarded { get; } = new();

    // Leading bare literals that appeared before the first parameter
    // token, in the order they were encountered. Not keyed by name -
    // positional arguments are matched purely by their index here
    // against ArgumentDefinition.Position during binding.
    public List<string> Positionals { get; } = new();

    public IEnumerable EnumerateAll()
    {
        foreach(var  arg in Values)
            yield return arg;
        
        foreach(var arg in Flags)
            yield return arg;

        foreach (var arg in Arrays)
            yield return arg;
        
        foreach (var arg in Forwarded)
            yield return arg;

        // Positionals are intentionally NOT yielded here: they have no
        // string key, so they can't fit the KeyValuePair<string, T>
        // shapes ProgramArgumentBinder pattern-matches on. They're
        // bound in a separate dedicated pass instead - see
        // ProgramArgumentBinder.BindPositionals.
    }
}
