using System.Diagnostics;
using WinterRose.DependancyInjection;

namespace WinterRose.Uris.UriVerifiers;

[LinuxOnly]
public class LinuxUriSchemeRegistar : IUriSchemeRegistar
{
    public bool IsRegistered(UriOptions options)
    {
        try
        {
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-mime",
                Arguments = $"query default x-scheme-handler/{options.Scheme}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            string result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return !string.IsNullOrWhiteSpace(result);
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

        string applicationsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local",
            "share",
            "applications"
        );

        Directory.CreateDirectory(applicationsDirectory);

        string desktopFileName = $"{options.AppId}.desktop";
        string desktopFilePath = Path.Combine(applicationsDirectory, desktopFileName);

        string desktopFile = $"""
                              [Desktop Entry]
                              Version=1.0
                              Type=Application
                              Name={options.DisplayName}
                              Exec="{executablePath}" %u
                              Terminal=false
                              MimeType=x-scheme-handler/{options.Scheme};
                              """;

        await File.WriteAllTextAsync(desktopFilePath, desktopFile, ct);

        await RunProcessAsync(
            "xdg-mime",
            $"default {desktopFileName} x-scheme-handler/{options.Scheme}",
            ct);

        await RunProcessAsync(
            "update-desktop-database",
            applicationsDirectory,
            ct);
    }

    private static async Task RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken ct)
    {
        using Process process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync(ct);

            throw new InvalidOperationException(
                $"{fileName} failed with exit code {process.ExitCode}: {error}");
        }
    }
}