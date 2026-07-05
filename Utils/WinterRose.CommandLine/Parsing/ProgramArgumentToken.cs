namespace WinterRose.CommandLine.Parsing;

public readonly record struct ProgramArgumentToken(
    ProgramArgumentTokenType Type,
    string? Text);