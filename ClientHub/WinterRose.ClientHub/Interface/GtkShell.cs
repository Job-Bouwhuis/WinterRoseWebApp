using Gtk;
using EtoApplication = Eto.Forms.Application;
using Eto.GtkSharp;

namespace WinterRose.ClientHub.Feature.Interface;

public class EtoShell
{
    private EtoApplication etoApp;
    public EtoShell()
    {
        Start();
    }
    
    public void Start()
    {
        if (etoApp is not null)
            return;

        Platform p = new Platform();
        etoApp = new EtoApplication(p);
    }

    public void Shutdown()
    {
        if (etoApp is null)
            return;

        etoApp.Quit();
    }

    public void Tick()
    {
        if (etoApp is null)
            return;
        
        etoApp.RunIteration();
    }
}