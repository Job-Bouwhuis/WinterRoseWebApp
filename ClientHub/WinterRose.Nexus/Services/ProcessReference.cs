namespace WinterRose.Nexus.Services;

public struct ProcessReference
{
    internal readonly string AppId;
    internal readonly ApplicationLaunchManager.ProcessInfo Info;
    internal readonly ApplicationLaunchManager Owner;

    internal ProcessReference(
        string appId,
        ApplicationLaunchManager.ProcessInfo info,
        ApplicationLaunchManager owner)
    {
        AppId = appId;
        Info = info;
        Owner = owner;
    }

    public bool IsRunning => !Info.Process.HasExited;

    public int ProcessId => Info.Process.Id;

    public string Id => AppId;

    public void Kill() => Owner.KillApplication(AppId);
}