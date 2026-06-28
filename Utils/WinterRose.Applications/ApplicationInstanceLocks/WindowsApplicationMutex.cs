using System.Threading;
using WinterRose.DependancyInjection;

namespace WinterRose.Applications.ApplicationInstanceLocks;

[WindowsOnly]
internal sealed class WindowsApplicationMutex : IApplicationMutex
{
    private readonly Mutex MUTEX;
    public bool IsFirstInstance { get; }

    public WindowsApplicationMutex(MutexOptions options)
    {
        MUTEX = new Mutex(true, $"Global\\{options.AppId}", out bool createdNew);
        IsFirstInstance = createdNew;
    }

    public void Dispose()
    {
        if (IsFirstInstance)
            MUTEX.ReleaseMutex();

        MUTEX.Dispose();
    }
}