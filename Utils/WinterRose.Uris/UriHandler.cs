namespace WinterRose.Uris;

public interface IUriHandler
{
    bool CanHandle(UriContext context);
    Task HandleAsync(UriContext context);
}