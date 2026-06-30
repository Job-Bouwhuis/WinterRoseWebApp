using Eto.Forms;
using WinterRose.DependancyInjection;

namespace WinterRose.Nexus.Interface.Windows;

public class TestWindow(IServiceProvider services, MainThread main) : WindowBase("asd", main, services)
{
    protected override Control BuildContent()
    {
        DynamicLayout layout = new DynamicLayout();
        layout.BeginVertical();
        layout.Add(new Eto.Forms.Label { Text = "test" });
        layout.EndVertical();
        return layout;
    }
}