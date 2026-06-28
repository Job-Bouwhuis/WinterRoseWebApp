namespace WinterRose.Uris.UriVerifiers;

public interface IUriSchemeRegistar
{
    async Task Validate(UriOptions options)
    {
        if (!IsRegistered(options))
            await RegisterAsync(options, CancellationToken.None);
    }
    
    bool IsRegistered(UriOptions options);

    Task RegisterAsync(UriOptions options, CancellationToken ct = default);
}