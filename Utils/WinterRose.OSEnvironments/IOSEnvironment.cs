using System.Runtime.InteropServices;

namespace WinterRose.Uris;

public interface IOSEnvironment
{
    OSPlatform Platform { get; }
    bool IsWindows { get; }
    bool IsLinux { get; }
}