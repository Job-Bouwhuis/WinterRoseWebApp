using System.Runtime.InteropServices;

namespace WinterRose.Uris;

public class RuntimeOSEnvironment : IOSEnvironment
{
    public OSPlatform Platform { get; }

    public bool IsWindows => Platform == OSPlatform.Windows;
    public bool IsLinux => Platform == OSPlatform.Linux;

    public RuntimeOSEnvironment()
    {
        if (OperatingSystem.IsWindows())
            Platform = OSPlatform.Windows;
        else if (OperatingSystem.IsLinux())
            Platform = OSPlatform.Linux;
        else
            throw new PlatformNotSupportedException();
    }
}