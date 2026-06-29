using System;
using System.IO;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Uris;

namespace WinterRose.Applications.ApplicationInstanceLocks;

internal sealed class ApplicationMutex : IApplicationMutex
{
    private readonly FileStream? lockFile;
    public bool IsFirstInstance { get; }

    public ApplicationMutex(MutexOptions options)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{options.AppId}.lock");
        
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