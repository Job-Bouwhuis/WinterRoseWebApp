using System.Diagnostics;
using System.Text;
using WinterRose.CommandLine.Parsing;

namespace WinterRose.CommandLine;

/// <summary>
/// Builds command-line strings using the argument format understood by
/// <see cref="ProgramArgumentLexer"/> and <see cref="ProgramArgumentParser"/>.
/// </summary>
/// <remarks>
/// This class defines its own escaping format rather than relying on shell
/// quoting rules. The string returned by <see cref="Build"/> is intended to be
/// parsed exclusively by <see cref="Split(string)"/>, which is its exact inverse.
///
/// Do not pass the generated string to a shell expecting it to re-parse the
/// arguments, and do not use <see cref="Split(string)"/> to parse arbitrary
/// shell command lines. The format is intentionally self-contained so that it
/// behaves identically across platforms.
/// </remarks>
public sealed class ProgramArgumentStringBuilder
{
    private readonly List<string> tokens = new();

    /// <summary>
    /// Adds a long parameter.
    /// </summary>
    /// <param name="name">The parameter name without the leading <c>--</c>.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddLongParameter(string name)
    {
        string token = "--" + name;

        if (!ProgramArgumentLexer.IsLongParameter(token))
        {
            throw new ProgramArgumentsException(
                $"'{name}' is not a valid long parameter name - it would not " +
                "round-trip correctly through the lexer. Long parameter names " +
                "must be alphanumeric with single internal hyphens, must not " +
                "start with an extra '-', and must not end with '-'.");
        }

        tokens.Add(token);
        return this;
    }

    /// <summary>
    /// Adds a short parameter.
    /// </summary>
    /// <param name="name">The parameter name without the leading <c>-</c>.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddShortParameter(string name)
    {
        string token = "-" + name;

        if (!ProgramArgumentLexer.IsShortParameter(token))
        {
            throw new ProgramArgumentsException(
                $"'{name}' is not a valid short parameter name - it would not " +
                "round-trip correctly through the lexer. Short parameter names " +
                "must be 1-2 alphanumeric characters.");
        }

        tokens.Add(token);
        return this;
    }

    /// <summary>
    /// Adds a literal value.
    /// </summary>
    /// <param name="value">The literal value to add.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddLiteral(string value)
    {
        ValidateLiteral(value);
        tokens.Add(value);
        return this;
    }

    /// <summary>
    /// Adds a positional value.
    /// </summary>
    /// <param name="value"></param>
    /// <remarks>You should add all your positional values before adding parameters.</remarks>
    /// <returns></returns>
    public ProgramArgumentStringBuilder AddPositional(string value)
    {
        ValidateLiteral(value);
        tokens.Add(value);
        return this;
    }

    /// <summary>
    /// Adds a long parameter followed by its literal value.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddLongValue(string name, string value)
    {
        AddLongParameter(name);
        AddLiteral(value);
        return this;
    }

    /// <summary>
    /// Adds a short parameter followed by its literal value.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddShortValue(string name, string value)
    {
        AddShortParameter(name);
        AddLiteral(value);
        return this;
    }

    /// <summary>
    /// Adds a flag parameter
    /// </summary>
    /// <param name="name">The parameter name without the leading <c>--</c>.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddFlag(string name)
        => AddLongParameter(name);

    /// <summary>
    /// Adds an array value.
    /// </summary>
    /// <remarks>
    /// The opening and closing array delimiters are emitted as separate tokens,
    /// matching the format expected by <see cref="ProgramArgumentLexer"/>.
    /// </remarks>
    /// <param name="values">The array elements.</param>
    /// <returns>The current builder.</returns>
    private ProgramArgumentStringBuilder AddArray(IEnumerable<string> values)
    {
        tokens.Add("[");

        foreach (var value in values)
        {
            ValidateLiteral(value);
            tokens.Add(value);
        }

        tokens.Add("]");
        return this;
    }

    /// <summary>
    /// Adds a long parameter followed by an array value.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="values">The array elements.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddLongArray(string name, IEnumerable<string> values)
    {
        AddLongParameter(name);
        AddArray(values);
        return this;
    }

    /// <summary>
    /// Adds a short parameter followed by an array value.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="values">The array elements.</param>
    /// <returns>The current builder.</returns>
    public ProgramArgumentStringBuilder AddShortArray(string name, IEnumerable<string> values)
    {
        AddShortParameter(name);
        AddArray(values);
        return this;
    }

    /// <summary>
    /// Adds a forward block.
    /// </summary>
    /// <remarks>
    /// Tokens inside the block are emitted verbatim. The builder does not
    /// interpret or validate their meaning beyond preventing ambiguous
    /// structural delimiter tokens.
    /// </remarks>
    /// <param name="rawTokens">The tokens to place inside the forward block.</param>
    /// <returns>The current builder.</returns>
    private ProgramArgumentStringBuilder AddForward(IEnumerable<string> rawTokens)
    {
        tokens.Add("(");

        foreach (var raw in rawTokens)
        {
            ValidateNotBareBracketOrParen(raw, context: "forward block content");
            tokens.Add(raw);
        }

        tokens.Add(")");
        return this;
    }

    // Convenience: --name ( tok1 tok2 ... )
    public ProgramArgumentStringBuilder AddForward(string name, IEnumerable<string> rawTokens)
    {
        AddLongParameter(name);
        AddForward(rawTokens);
        return this;
    }

    /// <summary>
    /// Validates that a value can be emitted as a literal token without being
    /// interpreted as another token type by the lexer.
    /// </summary>
    private static void ValidateLiteral(string value)
    {
        if (value is null)
        {
            throw new ProgramArgumentsException(
                "Cannot add a null literal value.");
        }

        ValidateNotBareBracketOrParen(value, context: "literal value");

        // Mirror the lexer's own precedence exactly: a negative number
        // like "-5" or "-2.5" is deliberately tokenized as a Literal,
        // NOT a short parameter, even though it starts with a single
        // "-". Checking this first avoids rejecting values the lexer
        // would actually accept as literals.
        if (ProgramArgumentLexer.IsNegativeNumber(value))
            return;

        if (ProgramArgumentLexer.IsLongParameter(value) ||
            ProgramArgumentLexer.IsShortParameter(value))
        {
            throw new ProgramArgumentsException(
                $"'{value}' looks like a parameter (starts with '-' or '--') but is " +
                "being added as a plain literal value. If this is intentional, the " +
                "receiving lexer will misinterpret it as a parameter, not a value - " +
                "rethink the value, or if the target truly expects a literal that " +
                "happens to start with a dash, be aware this format cannot express " +
                "that unambiguously (the lexer has no escape syntax for it).");
        }
    }

    /// <summary>
    /// Ensures a value is not one of the reserved structural delimiter tokens.
    /// </summary>
    private static void ValidateNotBareBracketOrParen(string value, string context)
    {
        if (value is "[" or "]" or "(" or ")")
        {
            throw new ProgramArgumentsException(
                $"'{value}' cannot be used as a {context} - it is indistinguishable " +
                "from a structural array/forward delimiter once emitted as its own " +
                "token, and would misparse as one on the receiving end.");
        }
    }

    /// <summary>
    /// Builds the final command-line string.
    /// </summary>
    /// <remarks>
    /// Tokens are separated by spaces. Tokens containing whitespace, quotes, or
    /// empty strings are quoted using this class's escaping rules. The resulting
    /// string is intended to be parsed only by <see cref="Split(string)"/>.
    /// </remarks>
    /// <returns>The serialized argument string.</returns>
    public string Build()
    {
        StringBuilder sb = new();

        for (int i = 0; i < tokens.Count; i++)
        {
            if (i > 0)
                sb.Append(' ');

            sb.Append(EscapeToken(tokens[i]));
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// Adds the arguments represented by this builder to the specified
    /// <see cref="ProcessStartInfo"/>'s <see cref="ProcessStartInfo.ArgumentList"/>.
    /// </summary>
    /// <remarks>
    /// This is the recommended way to launch a process using the arguments in this
    /// builder. Each argument is added individually, allowing the runtime to apply
    /// the correct platform-specific escaping and quoting rules.
    /// </remarks>
    /// <param name="startInfo">
    /// The <see cref="ProcessStartInfo"/> whose
    /// <see cref="ProcessStartInfo.ArgumentList"/> will receive the arguments.
    /// </param>
    /// <returns>The current builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInfo"/> is <see langword="null"/>.
    /// </exception>
    public void Build(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        foreach (string token in tokens)
        {
            startInfo.ArgumentList.Add(token);
        }
    }

    /// <summary>
    /// Escapes a single token for inclusion in the serialized command-line
    /// string.
    /// </summary>
    private static string EscapeToken(string token)
    {
        bool needsQuoting =
            token.Length == 0 ||
            token.Contains(' ') ||
            token.Contains('"');

        if (!needsQuoting)
            return token;

        string doubled = token.Replace("\"", "\"\"");
        return "\"" + doubled + "\"";
    }

    /// <summary>
    /// Splits a command-line string produced by <see cref="Build"/> back into
    /// its original tokens.
    /// </summary>
    /// <remarks>
    /// This method is the inverse of <see cref="Build"/>. It is not a general
    /// purpose command-line parser and should not be used to parse shell command
    /// lines.
    /// </remarks>
    /// <param name="command">The serialized argument string.</param>
    /// <returns>The reconstructed tokens.</returns>
    public static IReadOnlyList<string> Split(string command)
    {
        List<string> result = new();
        int i = 0;
        int n = command.Length;

        while (i < n)
        {
            while (i < n && command[i] == ' ')
                i++;

            if (i >= n)
                break;

            if (command[i] == '"')
            {
                i++; // skip opening quote
                StringBuilder token = new();

                while (i < n)
                {
                    if (command[i] == '"')
                    {
                        if (i + 1 < n && command[i + 1] == '"')
                        {
                            token.Append('"');
                            i += 2;
                            continue;
                        }

                        i++; // skip closing quote
                        break;
                    }

                    token.Append(command[i]);
                    i++;
                }

                result.Add(token.ToString());
            }
            else
            {
                int start = i;
                while (i < n && command[i] != ' ')
                    i++;

                result.Add(command.Substring(start, i - start));
            }
        }

        return result;
    }

    public override string ToString() => Build();

    public void AddForwarded(string[] originalArgs) => tokens.AddRange(originalArgs);
}
