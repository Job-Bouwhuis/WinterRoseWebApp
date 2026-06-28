namespace WinterRose.Uris;

public sealed class UriContext
{
    public string Command { get; }

    public UriQuery Query { get; }

    public UriContext(string command, UriQuery query)
    {
        Command = command;
        Query = query;
    }

    public override string ToString()
    {
        return Command + Query;
    }
}