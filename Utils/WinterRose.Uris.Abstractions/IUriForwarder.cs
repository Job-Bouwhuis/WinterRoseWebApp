namespace WinterRose.Uris;

public interface IUriForwarder
{
    Task ForwardAsync(string uri);
}