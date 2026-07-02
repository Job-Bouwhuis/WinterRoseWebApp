using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;

namespace WinterRose.Nexus.Interface.Dialogs;

public enum DialogResultType
{
    Ok,
    Yes,
    No,
    Cancel,
    Custom
}

public enum DialogKind
{
    Info,
    Warning,
    Error,
    Question
}

public class DialogRequest
{
    public string Title { get; set; } = "Nexus";

    public string Message { get; set; } = "";

    public DialogKind Kind { get; set; } = DialogKind.Info;

    public string? PrimaryButton { get; set; }

    public string? SecondaryButton { get; set; }

    public string? TertiaryButton { get; set; }

    public bool AllowCancel { get; set; } = true;

    public static implicit operator DialogRequest(string message) => new()
    {
        Message = message,
        AllowCancel = false
    };
}

public class DialogResponse
{
    public DialogResultType Result { get; set; }

    public string? SelectedOption { get; set; }
}

public interface IModalDialog
{
    Task<DialogResponse> ShowAsync(DialogRequest request);
}

public class EtoModalDialog(MainThread main) : IModalDialog
{
    public Task<DialogResponse> ShowAsync(DialogRequest request)
    {
        var tcs = new TaskCompletionSource<DialogResponse>();

        main.Invoke(() =>
        {
            var dialog = new Dialog
            {
                Title = request.Title,
                ClientSize = new Size(420, 180),
                Resizable = false
            };

            var message = new Label
            {
                Text = request.Message,
                Wrap = WrapMode.Word
            };

            var buttons = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalContentAlignment = HorizontalAlignment.Right
            };

            void AddButton(string text, DialogResultType resultType)
            {
                var btn = new Button { Text = text };

                btn.Click += (_, _) =>
                {
                    tcs.TrySetResult(new DialogResponse
                    {
                        Result = resultType,
                        SelectedOption = text
                    });

                    dialog.Close();
                };

                buttons.Items.Add(btn);
            }

            switch (request.Kind)
            {
                case DialogKind.Info:
                    AddButton(request.PrimaryButton ?? "OK", DialogResultType.Ok);
                    break;

                case DialogKind.Warning:
                case DialogKind.Error:
                    AddButton(request.PrimaryButton ?? "OK", DialogResultType.Ok);
                    break;

                case DialogKind.Question:
                    AddButton(request.SecondaryButton ?? "No", DialogResultType.No);
                    AddButton(request.PrimaryButton ?? "Yes", DialogResultType.Yes);
                    break;
            }

            dialog.Content = new TableLayout
            {
                Padding = 12,
                Spacing = new Size(10, 10),
                Rows =
                {
                    new TableRow(message),
                    new TableRow([]) { ScaleHeight = true },
                    new TableRow(buttons)
                }
            };

            dialog.ShowModal();
        });

        return tcs.Task;
    }
}