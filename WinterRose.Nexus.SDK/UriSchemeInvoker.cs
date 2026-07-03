namespace WinterRose.Nexus.SDK;

using System.Diagnostics;

public static class UriSchemeInvoker
{
    public static void Open(string uri, bool escalateToBrowserOnFailure = false)
    {
        if (OperatingSystem.IsWindows())
        {
            OpenWindows(uri, escalateToBrowserOnFailure);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            OpenLinux(uri, escalateToBrowserOnFailure);
            return;
        }

        throw new PlatformNotSupportedException("Only Windows and Linux are supported.");
    }

    private static void OpenWindows(string uri, bool escalateToBrowserOnFailure)
    {
        Exception? primaryException = null;

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });

            if (process is not null)
                return;
            
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            primaryException = ex;
        }

        if (escalateToBrowserOnFailure)
        {
            try
            {
                OpenBrowser(uri);
                return;
            }
            catch (Exception fallbackException)
            {
                throw new InvalidOperationException(
                    $"Both URI scheme and browser fallback failed for: {uri}",
                    new AggregateException(primaryException, fallbackException));
            }
        }

        throw new InvalidOperationException(
            $"Failed to open URI on Windows: {uri}",
            primaryException);
    }
    private static void OpenLinux(string uri, bool escalateToBrowserOnFailure)
    {
        Exception? primaryException = null;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = uri,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
                return;

            var error = process.StandardError.ReadToEnd();

            primaryException = new InvalidOperationException(
                $"xdg-open failed with exit code {process.ExitCode}. {error}");
        }
        catch (Exception ex)
        {
            primaryException = ex;
        }

        if (escalateToBrowserOnFailure)
        {
            try
            {
                OpenBrowser(uri);
                return;
            }
            catch (Exception fallbackException)
            {
                throw new InvalidOperationException(
                    $"Both URI scheme and browser fallback failed for: {uri}",
                    new AggregateException(primaryException, fallbackException));
            }
        }

        throw new InvalidOperationException(
            $"Failed to open URI on Linux: {uri}",
            primaryException);
    }
    
    private static void OpenBrowser(string uri)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true
        });
    }
}