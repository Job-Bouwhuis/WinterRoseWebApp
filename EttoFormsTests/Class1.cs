using Eto.Forms;

namespace EttoFormsTests;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new Application();
        var form = new MyForm();
    
        // Handle the Closing event to prevent actual closure
        form.Closing += (sender, e) => 
        {
            e.Cancel = true;        // Prevent the form from closing
            form.Visible = false;  // Hide it instead
        };
    
        app.MainForm = form;
        form.Show();
    
        bool running = true;
        Console.CancelKeyPress += (sender, e) => running = false;
    
        while (running)
        {
            if (Console.KeyAvailable)
                if (Console.ReadKey(true).Key == ConsoleKey.W)
                    form.Show();    // Show it again (works because it was hidden, not closed)
        
            app.RunIteration();
        }
    
        app.Quit();
        app.Dispose();
    }
}

public class MyForm : Form
{
    public MyForm()
    {
        Title = "My Form";
        
        var layout = new DynamicLayout();

        // Begin a vertical section for the fields
        layout.BeginVertical();
        layout.AddRow(new Label { Text = "Field 1" }, new TextBox());
        layout.AddRow(new Label { Text = "Field 2" }, new ComboBox());
        layout.EndVertical();

        // Begin a new vertical section for the buttons
        layout.BeginVertical();
        // Passing null creates a scaled column to push the buttons to the right
        layout.AddRow(null, new Button { Text = "Cancel" }, new Button { Text = "Ok" });
        layout.EndVertical();

        Content = layout;
    }
}