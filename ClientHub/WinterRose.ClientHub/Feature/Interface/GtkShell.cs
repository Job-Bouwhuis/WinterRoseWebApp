using Gtk;
using WinterRose.ClientHub.Feature.Interface.Windows;

namespace WinterRose.ClientHub.Feature.Interface;

public class GtkShell
{
    private bool initialized;

    public GtkShell()
    {
        Start();
    }
    
    public void Start()
    {
        if (initialized)
            return;

        Application.Init();
        initialized = true;
    }

    public void Shutdown()
    {
        if (!initialized)
            return;

        Application.Quit();
        initialized = false;
    }
}