using Microsoft.Win32;
using WinterRose.DependancyInjection;

namespace WinterRose.Uris.UriVerifiers;

[WindowsOnly]
public class WindowsUriSchemeRegistar : IUriSchemeRegistar
{
    public bool IsRegistered(UriOptions options)
    {
        try
        {
            using RegistryKey key =
                Registry.ClassesRoot.OpenSubKey($"{options.Scheme}");

            if (key == null)
                return false;

            object value = key.GetValue("URL Protocol");
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task RegisterAsync(UriOptions options, CancellationToken ct = default)
    {
        string executablePath = Environment.ProcessPath
                                ?? throw new InvalidOperationException(
                                    "Unable to determine executable path."
                                );

        await Task.Run(() =>
        {
            using RegistryKey schemeKey =
                Registry.ClassesRoot.CreateSubKey(options.Scheme);

            schemeKey.SetValue("", $"URL:{options.DisplayName}");
            schemeKey.SetValue("URL Protocol", "");

            using RegistryKey defaultIconKey =
                schemeKey.CreateSubKey("DefaultIcon");

            defaultIconKey.SetValue("", executablePath);

            using RegistryKey commandKey =
                schemeKey.CreateSubKey(@"shell\open\command");

            commandKey.SetValue(
                "",
                $"\"{executablePath}\" \"%1\""
            );
        }, ct);
    }
}