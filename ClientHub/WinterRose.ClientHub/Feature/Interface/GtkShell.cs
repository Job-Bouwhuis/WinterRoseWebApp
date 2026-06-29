using Gtk;
using EtoApplication = Eto.Forms.Application;

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
        
        etoApp = new EtoApplication();
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