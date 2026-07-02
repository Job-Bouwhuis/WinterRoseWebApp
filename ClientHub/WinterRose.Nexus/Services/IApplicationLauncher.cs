using System.Diagnostics;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

public interface IApplicationLauncher
{
    public Process LaunchApplication(string appId, AppLaunchTarget launchTarget, params string[] extraArgs);
}