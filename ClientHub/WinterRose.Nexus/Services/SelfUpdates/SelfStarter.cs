using System;
using System.Diagnostics;
using WinterRose.CommandLine;

namespace WinterRose.Nexus.Services.SelfUpdates;

public class SelfStarter
{
    public void Start()
    {
        string[] originalArgs = ProgramArguments.Get<string[]>("forward");
        
        ProgramArgumentStringBuilder argBuilder = new();
        if (originalArgs != null)
            argBuilder.AddForwarded(originalArgs);
        argBuilder.AddFlag("clean-copy");
        argBuilder.AddLongValue("processId", Environment.ProcessId.ToString());
        
        string originalPath = ProgramArguments.Get<string>("original-path");

        ProcessStartInfo info = new(originalPath);
        argBuilder.Build(info);
        try
        {
            Process.Start(info);
        }
        catch (Exception e)
        {
            
        }
    }
}