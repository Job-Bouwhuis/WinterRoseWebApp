namespace WinterRose.CommandLine.Parsing;

public enum ProgramArgumentTokenType
{
    LongParameter,
    ShortParameter,

    Literal,

    ArrayBegin,
    ArrayEnd,

    ForwardBegin,
    ForwardEnd,
}