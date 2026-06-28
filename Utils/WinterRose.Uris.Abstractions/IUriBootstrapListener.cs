namespace WinterRose.Uris;

public interface IUriBootstrapListener
{
    void StartListening(Func<string, Task> onUri, CancellationToken ct);
}