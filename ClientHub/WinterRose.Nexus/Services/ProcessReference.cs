namespace WinterRose.Nexus.Services;

public struct ProcessReference
{
    internal readonly string AppId;
    internal readonly ApplicationLauchManager.ProcessInfo Info;
    internal readonly ApplicationLauchManager Owner;

    internal ProcessReference(
        string appId,
        ApplicationLauchManager.ProcessInfo info,
        ApplicationLauchManager owner)
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