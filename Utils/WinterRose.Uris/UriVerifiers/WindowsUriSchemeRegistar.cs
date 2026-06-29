using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
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
                Registry.LocalMachine.OpenSubKey($@"Software\Classes\{options.Scheme}");

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
            ?? throw new InvalidOperationException("Unable to determine executable path.");

        string script = BuildPowerShellScript(options, executablePath);

        string tempScriptPath = Path.Combine(Path.GetTempPath(), $"{options.Scheme}_register.ps1");
        await File.WriteAllTextAsync(tempScriptPath, script, ct);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
            UseShellExecute = true,
            Verb = "runas"
        };

        Process process = Process.Start(psi);

        if (process == null)
            throw new InvalidOperationException("Failed to start elevated PowerShell process.");
    }

    private string BuildPowerShellScript(UriOptions options, string executablePath)
    {
        StringBuilder builder = new StringBuilder();

        string keyPath = $@"HKLM:\Software\Classes\{options.Scheme}";

        builder.AppendLine($"New-Item -Path '{keyPath}' -Force | Out-Null");
        builder.AppendLine($"Set-ItemProperty -Path '{keyPath}' -Name '(default)' -Value 'URL:{options.DisplayName}'");
        builder.AppendLine($"New-ItemProperty -Path '{keyPath}' -Name 'URL Protocol' -Value '' -Force | Out-Null");

        builder.AppendLine($"New-Item -Path '{keyPath}\\DefaultIcon' -Force | Out-Null");
        builder.AppendLine($"Set-ItemProperty -Path '{keyPath}\\DefaultIcon' -Name '(default)' -Value '{executablePath}'");

        builder.AppendLine($"New-Item -Path '{keyPath}\\shell\\open\\command' -Force | Out-Null");
        builder.AppendLine($"Set-ItemProperty -Path '{keyPath}\\shell\\open\\command' -Name '(default)' -Value '\"{executablePath}\" \"%1\"'");

        return builder.ToString();
    }
}