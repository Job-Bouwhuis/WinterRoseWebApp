using System.Collections.Generic;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Services;

public interface IApplicationLaunchManager
{
    List<ProcessReference> GetRunningProcesses();
    ProcessReference LaunchApplication(string appId, AppLaunchTarget launchTarget, params string[] args);
    void KillApplication(string appId);
}