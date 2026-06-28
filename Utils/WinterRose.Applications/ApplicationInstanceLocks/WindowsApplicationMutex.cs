using System.Threading;
using WinterRose.DependancyInjection;

namespace WinterRose.Applications.ApplicationInstanceLocks;

[WindowsOnly]
public sealed class WindowsApplicationMutex : IApplicationMutex
{
    private readonly Mutex MUTEX;
    public bool IsFirstInstance { get; }

    public WindowsApplicationMutex(string appId)
    {
        bool createdNew;
        MUTEX = new Mutex(true, $"Global\\{appId}", out createdNew);
        IsFirstInstance = createdNew;
    }

    public void Dispose()
    {
        if (IsFirstInstance)
            MUTEX.ReleaseMutex();

        MUTEX.Dispose();
    }
}