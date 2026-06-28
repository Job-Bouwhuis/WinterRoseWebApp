namespace WinterRose.Uris;

public interface IUriHandler
{
    string Command { get; set; }
    bool CanHandle(UriContext context);
    Task HandleAsync(UriContext context);
}