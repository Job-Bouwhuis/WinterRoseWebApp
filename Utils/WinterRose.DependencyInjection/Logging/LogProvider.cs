using WinterRose.Recordium;

namespace WinterRose.DependancyInjection.Logging;

public interface ILogProvider
{
    ILogger GetLogger(string category);
}