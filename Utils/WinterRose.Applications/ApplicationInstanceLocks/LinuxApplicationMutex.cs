using System.IO;
using WinterRose.DependancyInjection;

namespace WinterRose.Applications.ApplicationInstanceLocks;

[LinuxOnly]
public sealed class LinuxApplicationMutex : IApplicationMutex
{
    private readonly FileStream? lockFile;
    public bool IsFirstInstance { get; }

    public LinuxApplicationMutex(string appId)
    {
        string path = $"/tmp/{appId}.lock";

        try
        {
            lockFile = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            );

            IsFirstInstance = true;
        }
        catch (IOException)
        {
            IsFirstInstance = false;
        }
    }

    public void Dispose()
    {
        lockFile?.Dispose();
    }
}