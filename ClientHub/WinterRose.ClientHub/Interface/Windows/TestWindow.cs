using Eto.Forms;
using Gdk;
using Gtk;
using WinterRose.Applications;
using WinterRose.DependancyInjection;
using Label = Gtk.Label;
using Orientation = Gtk.Orientation;
using Window = Gtk.Window;

namespace WinterRose.ClientHub.Feature.Interface.Windows;

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