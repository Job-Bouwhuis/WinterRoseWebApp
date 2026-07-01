using System.Runtime.InteropServices.JavaScript;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.SDK;

/// <summary>
/// The interaction bridge between the Nexus app update system and your app
/// </summary>
public class Nexus(string appId) : IDisposable
{
    private const string NEXUS_URI_BASE = "winterrose://";
    private const string UPDATE_APPLICATION_COMMAND = "update-application";
    private const string NEXUS_APPROVED_ARG = "--nexus-approved";

    private NexusNewVersionListener? newVersionListener;

    /// <summary>
    /// Establishes a Server Sent Events connection with the Nexus server. <br/>
    /// This can be used in combination with <see cref="EnsureLatestVersion"/> with no provided arguments to allow for quick development
    /// where you (the dev) upload a new version, and seconds later your tester already has the new version installed and running
    /// </summary>
    public event Action<AppVersion> OnNewVersionAvailable
    {
        add
        {
            if (newVersionListener == null)
            {
                newVersionListener = new NexusNewVersionListener(appId);
                newVersionListener.Start();
            }

            newVersionListener.OnMessage += value;
        }
        remove
        {
            if (newVersionListener is null)
                return;
            newVersionListener.OnMessage -= value;
        }
    }


    /// <summary>
    /// Allow Nexus to ensure the app is on the latest version the user configured. <br />
    /// <b>For example</b>, if the user downloaded the app anew, this will make sure that your app only executes
    /// when its on the latest release branch <br/><br/>
    ///
    /// When the user specifically configured the app not to update past a certain update, Nexus wont update the app.<br/><br/>
    ///
    /// Nexus will also ensure that the app wont continue executing if it, or a specific version, has been marked as "blocked"
    /// </summary>
    /// <remarks>This method will terminate the app if the required command line arg from the Nexus client is not provided. <br/>
    /// If execution of your app comes past this method it is to assume the app is on the correct version and is authorized to run</remarks>
    /// <param name="args">The command line args passed to <c>public static void Main(string[] args)</c></param>
    public void EnsureLatestVersion(string[] args)
    {
        try
        {
            if (!args.Contains(NEXUS_APPROVED_ARG))
                UriSchemeInvoker.Open($"{NEXUS_URI_BASE}{UPDATE_APPLICATION_COMMAND}?id={appId}");

            Environment.Exit(0);
        }
        catch (Exception e)
        {
            throw new NexusNotInstalledException(e);
        }
    }

    public void Dispose() => newVersionListener?.Dispose();
}

internal class NexusNotInstalledException(Exception inner) : Exception("Launching the Nexus client failed. Do you have Nexus installed?", inner);