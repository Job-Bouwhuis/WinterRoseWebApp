using Eto.GtkSharp;
using EtoApplication = Eto.Forms.Application;

namespace WinterRose.Nexus.Interface;

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