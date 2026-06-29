using System.Threading.Tasks;
using WinterRose.DependancyInjection;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Applications;

public interface IApplication : IAsyncDisposable
{
    IServiceProvider Services { get; set; }
    bool IsRunning { get; }
    void Run();
    Task RunAsync();
    void Stop();
}