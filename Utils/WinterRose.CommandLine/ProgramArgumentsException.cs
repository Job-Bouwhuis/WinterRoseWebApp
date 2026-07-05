namespace WinterRose.CommandLine;

public class ProgramArgumentsException : Exception
{
    public IReadOnlyList<string> Failures { get; }

    public ProgramArgumentsException(string message)
        : base(message)
    {
        Failures = new[] { message };
    }

    public ProgramArgumentsException(string message, Exception inner)
        : base(message, inner)
    {
        Failures = new[] { message };
    }

    // Aggregate constructor: used when the validation pipeline collects
    // multiple failures across different arguments before throwing once.
    public ProgramArgumentsException(IReadOnlyList<string> failures)
        : base(BuildMessage(failures))
    {
        Failures = failures;
    }

    private static string BuildMessage(IReadOnlyList<string> failures)
        => failures.Count == 1
            ? failures[0]
            : $"{failures.Count} argument validation failures:\n" +
              string.Join("\n", failures.Select(f => $"  - {f}"));
}
