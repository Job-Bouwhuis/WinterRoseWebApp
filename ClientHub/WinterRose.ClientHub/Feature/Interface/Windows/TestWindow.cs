using Gdk;
using Gtk;
using WinterRose.Applications;
using WinterRose.DependancyInjection;
using WinterRose.ForgeThread;
using Window = Gtk.Window;

namespace WinterRose.ClientHub.Feature.Interface.Windows;

public class TestWindow(IServiceProvider services) : WindowBase("asd", services)
{
    protected override void BuildWindow()
    {
        Box root = new Box(Orientation.Vertical, 8);
        root.BorderWidth = 10;
        Add(root);
        
        Label text = new Label("Test");
        root.Add(text);
    }
}