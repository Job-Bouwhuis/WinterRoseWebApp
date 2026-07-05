namespace WinterRose.CommandLine.Parsing;

public static class ProgramArgumentLexer
{
    public static IReadOnlyList<ProgramArgumentToken> Tokenize(
        ReadOnlySpan<string> arguments)
    {
        ProgramArgumentLexerContext context = new()
        {
            Arguments = arguments,
            Tokens = new List<ProgramArgumentToken>()
        };

        while (!context.IsAtEnd)
        {
            ReadNextToken(ref context);
        }

        return context.Tokens;
    }

    private static void ReadNextToken(
        ref ProgramArgumentLexerContext context)
    {
        string value = context.Current;
        int index = context.Index;

        // ARRAY BEGIN
        if (value == "[")
        {
            context.Add(ProgramArgumentTokenType.ArrayBegin);
            context.Advance();
            ReadArrayBody(ref context, index);
            return;
        }

        // ARRAY END (stray, unmatched at top level)
        if (value == "]")
        {
            Console.Error.WriteLine(
                $"[Lexer Error] Index {index}: Unmatched array end ']'");
            context.Add(ProgramArgumentTokenType.ArrayEnd);
            context.Advance();
            return;
        }

        // FORWARD BEGIN
        if (value == "(")
        {
            context.Add(ProgramArgumentTokenType.ForwardBegin);
            context.Advance();
            ReadForwardBody(ref context, index);
            return;
        }

        // FORWARD END (stray, unmatched at top level)
        if (value == ")")
        {
            Console.Error.WriteLine(
                $"[Lexer Error] Index {index}: Unmatched forward end ')'");
            context.Add(ProgramArgumentTokenType.ForwardEnd);
            context.Advance();
            return;
        }

        // LONG PARAMETER
        if (IsLongParameter(value))
        {
            context.Add(
                ProgramArgumentTokenType.LongParameter,
                value.Substring(2));

            context.Advance();
            return;
        }

        // Looked like it wanted to be a long parameter but failed validation
        if (value.StartsWith("--"))
        {
            Console.Error.WriteLine(
                $"[Lexer Error] Index {index}: Invalid long parameter syntax: '{value}'");
            context.Add(ProgramArgumentTokenType.Literal, value);
            context.Advance();
            return;
        }

        // NEGATIVE NUMBER LITERAL
        // Checked before short-parameter recognition so values like "-5"
        // or "-2.5" are never misread as a short parameter named "5".
        if (IsNegativeNumber(value))
        {
            context.Add(ProgramArgumentTokenType.Literal, value);
            context.Advance();
            return;
        }

        // SHORT PARAMETER
        if (IsShortParameter(value))
        {
            context.Add(
                ProgramArgumentTokenType.ShortParameter,
                value.Substring(1));

            context.Advance();
            return;
        }

        // Looked like it wanted to be a short parameter but failed validation
        if (value.Length > 1 && value[0] == '-')
        {
            Console.Error.WriteLine(
                $"[Lexer Error] Index {index}: Invalid short parameter syntax: '{value}'");
            context.Add(ProgramArgumentTokenType.Literal, value);
            context.Advance();
            return;
        }

        // DEFAULT LITERAL
        // The shell has already resolved all quoting for this argv element;
        // whatever text remains is taken completely verbatim.
        context.Add(ProgramArgumentTokenType.Literal, value);
        context.Advance();
    }

    // Reads array contents after a standalone "[" token.
    // Everything until the matching "]" is treated as Literal;
    // parameter syntax is suppressed and nested arrays are rejected.
    private static void ReadArrayBody(
        ref ProgramArgumentLexerContext context,
        int openIndex)
    {
        while (!context.IsAtEnd)
        {
            string value = context.Current;

            if (value == "]")
            {
                context.Add(ProgramArgumentTokenType.ArrayEnd);
                context.Advance();
                return;
            }

            if (value == "[")
            {
                Console.Error.WriteLine(
                    $"[Lexer Error] Index {context.Index}: Nested arrays are not allowed " +
                    $"(array opened at index {openIndex})");
                context.Advance();
                continue;
            }

            context.Add(ProgramArgumentTokenType.Literal, value);
            context.Advance();
        }

        Console.Error.WriteLine(
            $"[Lexer Error] Index {openIndex}: Unclosed array '['");
    }

    // Everything between "(" and ")" is stored verbatim as Literal
    // tokens - no parameter/array parsing occurs inside.
    private static void ReadForwardBody(
        ref ProgramArgumentLexerContext context,
        int openIndex)
    {
        while (!context.IsAtEnd)
        {
            string value = context.Current;

            if (value == ")")
            {
                context.Add(ProgramArgumentTokenType.ForwardEnd);
                context.Advance();
                return;
            }

            context.Add(ProgramArgumentTokenType.Literal, value);
            context.Advance();
        }

        Console.Error.WriteLine(
            $"[Lexer Error] Index {openIndex}: Unclosed forward '('");
    }

    // Exposed internal (rather than private) so ProgramArgumentBuilder can
    // validate names against the exact same rules the lexer enforces,
    // instead of duplicating this logic and risking drift.
    internal static bool IsLongParameter(string value)
    {
        if (value.Length < 3) return false;
        if (!value.StartsWith("--")) return false;
        if (value.StartsWith("---")) return false;
        if (value.EndsWith("-")) return false;

        for (int i = 2; i < value.Length; i++)
        {
            char c = value[i];

            bool valid = char.IsLetterOrDigit(c) || c == '-';
            if (!valid) return false;

            if (c == '-' && i + 1 < value.Length && value[i + 1] == '-')
                return false;
        }

        return true;
    }

    internal static bool IsShortParameter(string value)
    {
        if (value.Length < 2) return false;
        if (!value.StartsWith("-")) return false;
        if (value.StartsWith("--")) return false;

        string content = value.Substring(1);

        if (content.Length < 1 || content.Length > 2)
            return false;

        foreach (char c in content)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }

        return true;
    }

    // Same reasoning as IsLongParameter/IsShortParameter above.
    internal static bool IsNegativeNumber(string value)
    {
        if (value.Length < 2 || value[0] != '-') return false;
        return double.TryParse(value.AsSpan(1), out _);
    }
}

/*
 PROGRAM ARGUMENT SYNTAX SPEC
=============================

This lexer is responsible only for syntactic recognition and tokenization.
It does NOT resolve argument definitions, types, or semantics.

GENERAL RULES
-------------
- Input is a pre-split sequence of strings, as delivered by the operating
  system / shell to `Main(string[] args)`.
- The lexer does NOT perform its own quote parsing. Quoting, escaping, and
  whitespace splitting are entirely the responsibility of the calling shell
  before the process starts. By the time a value reaches this lexer, it is
  already a complete, final string for that argv position.
- Because of this, there is no cross-platform way to recover original quote
  characters or to join multiple argv elements into one value based on
  quoting - that information no longer exists once the OS/shell has split
  the command line into argv. This lexer does not attempt to.
- Output is a linear stream of tokens.
- All semantic meaning is deferred to later parsing stages.

TOKEN TYPES
-----------
- LongParameter:   --name
- ShortParameter:  -a / -ab
- Literal:         any non-syntax value, taken verbatim from its argv slot
- ArrayBegin:      [
- ArrayEnd:        ]
- ForwardBegin:    (
- ForwardEnd:      )

PARAMETER RULES
---------------
LongParameter:
- Must start with "--"
- Must NOT start with "---"
- Must NOT contain consecutive "--" after prefix
- Must NOT end with "-"
- Valid characters after prefix: A-Z a-z 0-9 and '-'
- Example: --output-file, --v2

ShortParameter:
- Must start with single "-"
- Must NOT be "--"
- Must be 1-2 alphanumeric characters after "-"
- Example: -v, -o, -a1
- Exception: a "-" followed by a valid number (e.g. -1, -2.5) is a
  Literal, not a parameter, so negative numeric values can be passed
  without needing extra quoting.

ARRAY RULES
-----------
- Arrays are delimited by "[" and "]" as their OWN, standalone argv
  elements. Because argv splitting already happened before this lexer
  runs, "[foo" or "foo]" glued onto a value cannot be reliably
  distinguished from a literal that legitimately starts/ends with a
  bracket character (shell quoting may have put it there intentionally).
  Therefore an array delimiter is only recognized when the ENTIRE argv
  element is exactly "[" or exactly "]". Callers who want array syntax
  must ensure their shell passes the brackets as separate words, e.g.:
      --include [ one two three ]
      --include [ "one two" three ]
  A value like "[one" (no surrounding spaces at the shell level) is
  simply a Literal containing a bracket character.
- Array mode starts at "[" and ends at the matching "]".
- Inside arrays:
  - Parameter syntax is ignored.
  - All tokens are treated as Literal unless they are the structural "]"
    (or, invalidly, a nested "[").
- Arrays may be written explicitly:
    --include [ one two three ]
    --include [ "one two" three ]     (shell joins "one two" into 1 argv slot)
- OR implicitly via repeated arguments (whether shorthand or longhand):
    -ic one -ic two -ic three
- Array consumption rules:
  - Array consumes tokens until the matching "]".
  - Nested arrays are NOT allowed and are reported as an error.

FORWARD ARGUMENT RULES
----------------------
- Forward arguments are delimited by "(" and ")", each as their OWN
  standalone argv element, for the same reason as array brackets above.
- Everything inside is stored verbatim as Literal tokens.
- No parsing of parameters, arrays, or anything else occurs inside.
- Example:
    --forward ( --fullscreen --vsync )

LITERAL RULES
-------------
- Anything not recognized as syntax is a Literal.
- Literals are preserved exactly as they arrive in their argv slot -
  including any characters a shell may have passed through due to the
  user's own quoting on their end (e.g. spaces, brackets, leading dashes).
- The lexer performs no quote stripping or joining of its own; if the
  value needs special characters, that is handled by the user's shell,
  not by this lexer.

ERROR HANDLING
--------------
- Lexer MUST produce errors for invalid syntax.
- Errors are written to STDERR.
- Each error MUST include:
  - Index in argument stream
  - Raw token value (where applicable)
  - Description of failure

Examples:
- Invalid long parameter syntax (e.g. "---foo", "--foo-", "--foo--bar")
- Invalid short parameter syntax (e.g. "-abc", "-!")
- Unmatched array end "]"
- Unclosed array "["
- Unmatched forward end ")"
- Unclosed forward "("
- Nested array "[" inside another array

POSITION TRACKING
------------------
- Each input argument has a numeric index (its position in argv).
- Multi-token constructs (arrays, forwards) report the index of their
  opening token when raising an error.
- Errors do NOT stop tokenization immediately unless the syntax construct
  is unrecoverable; the lexer performs best-effort recovery and keeps
  producing tokens for the remainder of the input.

DESIGN INTENT
-------------
- Lexer is intentionally syntax-only.
- No knowledge of parameter definitions exists here.
- No type conversion occurs here.
- No custom quote-parsing exists here; that is the shell's job, and any
  such information is unrecoverable once argv has been split.
- Output is always raw structural tokens + literals.
- All semantic meaning is deferred to the parser stage.
*/
