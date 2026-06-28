namespace WinterRose.Uris;

public interface IUriBootstrapListener
{
    void StartListening(Action<string> onUri);
}