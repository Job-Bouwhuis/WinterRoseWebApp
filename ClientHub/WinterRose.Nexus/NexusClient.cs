using System.Collections.Generic;
using System.IO;

namespace WinterRose.Nexus;

public class NexusClient
{
    public const string NexusAppId = "5256b5d0d57720c6fd1954b01e141782e5887a06";
    
    public static List<string> IgnoredFiles { get; } =
    [
        "userprefs.wf",
        "NexusVersion.wf"
    ];

    public static List<string> IgnoredDirectories { get; } =
    [
        "logs",
        "apps"
    ];
    
    public static void SafeDeleteDirectory(DirectoryInfo source)
    {
        var dirs = source.GetDirectories();
        foreach (var dir in dirs)
        {
            if (!NexusClient.IgnoredDirectories.Contains(dir.Name))
                SafeDeleteDirectory(dir);
        }

        var files = source.GetFiles();
        foreach (var file in files)
        {
            if (!NexusClient.IgnoredFiles.Contains(file.Name))
            {
                file.IsReadOnly = false;
                file.Delete();
            }
        }

        // only delete directory if it's empty after processing
        if (!NexusClient.IgnoredDirectories.Contains(source.Name))
        {
            try
            {
                source.Delete();
            }
            catch
            {
                // directory might not be empty due to ignored files/folders
            }
        }
    }
}