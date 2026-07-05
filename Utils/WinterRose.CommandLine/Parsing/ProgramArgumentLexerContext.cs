namespace WinterRose.CommandLine.Parsing;

internal ref struct ProgramArgumentLexerContext
{
    public ReadOnlySpan<string> Arguments;
    public int Index;

    public List<ProgramArgumentToken> Tokens;

    public bool IsAtEnd => Index >= Arguments.Length;

    public string Current => Arguments[Index];

    public void Advance()
    {
        Index++;
    }

    public void Add(
        ProgramArgumentTokenType type,
        string? text = null)
    {
        Tokens.Add(new ProgramArgumentToken(type, text));
    }
}