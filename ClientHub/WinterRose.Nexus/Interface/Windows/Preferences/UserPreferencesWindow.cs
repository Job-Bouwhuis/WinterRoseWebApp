using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using WinterRose.AnonymousTypes;
using WinterRose.Nexus.Interface.Dialogs;
using WinterRose.Nexus.Interface.Windows;
using WinterRose.Nexus.Preferences;
using WinterRose.WinterForgeSerializing;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Nexus.Interface.Preferences;

public class UserPreferencesWindow(
    MainThread mainThread,
    IServiceProvider services,
    UserPreferences preferences,
    IModalDialog dialog)
    : WindowBase("Preferences", mainThread, services)
{
    private TabControl tabControl = new();
    private TableLayout rootLayout;

    protected override Control BuildContent()
    {
        Width = 700;
        Height = 600;

        tabControl = new TabControl();

        rootLayout = new TableLayout
        {
            Padding = new Padding(16),
            Spacing = new Size(0, 12)
        };

        RebuildUi();

        var saveButton = new Button { Text = "Save" };
        var resetButton = new Button { Text = "Reset" };

        saveButton.Click += OnSaveClicked;
        resetButton.Click += OnResetClicked;

        var buttonRow = new TableLayout
        {
            Spacing = new Size(8, 0)
        };

        buttonRow.Rows.Add(new TableRow(
            new TableCell(saveButton) { ScaleWidth = true },
            resetButton
        ));

        rootLayout.Rows.Add(new TableRow(tabControl) { ScaleHeight = true });
        rootLayout.Rows.Add(new TableRow(buttonRow));

        return rootLayout;
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        DialogResponse res = await dialog.ShowAsync(new DialogRequest
        {
            AllowCancel = true,
            Kind = DialogKind.Question,
            Title = "Reset Preferences",
            Message = "Are you sure you want to reset your preferences?\n" +
                      "This will revert all preferences back to their default value.",
            PrimaryButton = "Yes reset my preferences",
            SecondaryButton = "No keep my preferneces"
        });

        if (res.Result == DialogResultType.Yes)
        {
            res = await dialog.ShowAsync(new DialogRequest
            {
                AllowCancel = true,
                Kind = DialogKind.Question,
                Title = "Reset Preferences",
                Message = "Are you sure absolutely 100% sure you want to reset your preferences?\n" +
                          "After this you wont be able to restore them to what they are now",
                PrimaryButton = "Yes reset my preferences",
                SecondaryButton = "No keep my preferneces",
                TertiaryButton = "Help im panicking"
            });

            if (res.Result == DialogResultType.Yes)
            {
                await main.InvokeAsync(() =>
                {
                    ResetPreferences();
                    RebuildUi();
                });
            }
        }
    }

    private void RebuildUi()
    {
        tabControl.Pages.Clear();

        foreach (var category in preferences.GetCategories())
        {
            tabControl.Pages.Add(new TabPage
            {
                Text = category,
                Content = BuildCategoryPage(category)
            });
        }
    }
    
    private void ResetPreferences()
    {
        foreach (var category in preferences.GetCategories())
            foreach (var option in preferences.GetOptions(category))
                option.Value = option.DefaultValue;
    }

    private Control BuildCategoryPage(string category)
    {
        var stack = new StackLayout
        {
            Spacing = 10
        };

        foreach (var option in preferences.GetOptions(category))
        {
            var control = RenderOption(option);
            stack.Items.Add(control);
        }

        return new Scrollable
        {
            Content = stack
        };
    }

    private Control RenderOption(IPreferenceOption option)
    {
        var container = new StackLayout
        {
            Spacing = 4
        };

        var label = new Label
        {
            Text = option.Name,
            Style = "accent"
        };

        var description = new Label
        {
            Text = option.Description,
            Style = "muted"
        };

        Control editor = CreateEditor(option);

        container.Items.Add(label);
        container.Items.Add(description);
        container.Items.Add(editor);

        return container;
    }

    private Control CreateEditor(IPreferenceOption option)
    {
        if (option.ValueType == typeof(bool))
        {
            var cb = new CheckBox
            {
                Checked = (bool)option.Value!
            };

            cb.CheckedChanged += (s, e) => { option.Value = cb.Checked ?? false; };

            return cb;
        }

        if (option.ValueType.IsEnum)
        {
            if (option.ValueType.IsDefined(typeof(FlagsAttribute), false))
                return CreateFlagsEnum(option);

            return CreateEnumDropdown(option);
        }
        
        if (numberPrimitives.Contains(option.ValueType))
        {
            if (option.Hint == ControlHint.Slider)
                return CreateSlider(option);

            return CreateNumeric(option);
        }

        if (option.ValueType == typeof(string))
        {
            return CreateStringEditor(option);
        }

        return new Label
        {
            Text = $"Preference type {option.ValueType.Name} is unsupported!"
        };
    }
    
    public static List<Type> numberPrimitives { get; } =
    [
        typeof(byte),
        typeof(sbyte),
        typeof(decimal),
        typeof(double),
        typeof(float),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(short),
        typeof(ushort)
    ];
    
    private Control CreateEnumDropdown(IPreferenceOption option)
    {
        var combo = new ComboBox();

        var enumValues = Enum.GetValues(option.ValueType);

        foreach (var v in enumValues)
        {
            combo.Items.Add(v.ToString());
        }

        combo.SelectedIndex = Array.IndexOf(enumValues.Cast<object>().ToArray(), option.Value);

        combo.SelectedIndexChanged += (s, e) =>
        {
            if (combo.SelectedIndex < 0)
                return;

            option.Value = enumValues.GetValue(combo.SelectedIndex);
        };

        return combo;
    }
    
    private Control CreateFlagsEnum(IPreferenceOption option)
    {
        var stack = new StackLayout
        {
            Spacing = 4
        };

        var enumValues = Enum.GetValues(option.ValueType);

        foreach (var v in enumValues)
        {
            var value = v;
            var cb = new CheckBox
            {
                Text = value.ToString(),
                Checked = ((Enum)option.Value!).HasFlag((Enum)value)
            };

            cb.CheckedChanged += (s, e) =>
            {
                var current = (Enum)option.Value!;
                var flag = (Enum)value;

                if (cb.Checked == true)
                    option.Value = Enum.ToObject(option.ValueType, Convert.ToInt64(current) | Convert.ToInt64(flag));
                else
                    option.Value = Enum.ToObject(option.ValueType, Convert.ToInt64(current) & ~Convert.ToInt64(flag));
            };

            stack.Items.Add(cb);
        }

        return stack;
    }
    
    private Control CreateSlider(IPreferenceOption option)
    {
        var slider = new Eto.Forms.Slider
        {
            MinValue = Convert.ToInt32(option.GetType()
                .GetProperty("MinValue")?.GetValue(option) ?? 0),

            MaxValue = Convert.ToInt32(option.GetType()
                .GetProperty("MaxValue")?.GetValue(option) ?? 100),

            Value = Convert.ToInt32(option.Value ?? 0),
        };

        slider.ValueChanged += (s, e) =>
        {
            option.Value = slider.Value;
        };

        return new StackLayout
        {
            Padding = new Padding(0, 4, 0, 6),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Items =
            {
                new StackLayoutItem(slider, HorizontalAlignment.Stretch)
            }
        };
    }
    
    private Control CreateNumeric(IPreferenceOption option)
    {
        var numeric = new NumericStepper
        {
            Value = Convert.ToDouble(option.Value ?? 0),
            Increment = 1
        };

        numeric.ValueChanged += (s, e) =>
        {
            option.Value = (int)numeric.Value;
        };

        return numeric;
    }
    
    private Control CreateStringEditor(IPreferenceOption option)
    {
        var box = new TextBox
        {
            Text = option.Value?.ToString() ?? ""
        };

        box.TextChanged += (s, e) =>
        {
            option.Value = box.Text;
        };

        return box;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        Anonymous prefs = new Anonymous();
        
        foreach (var cat in preferences.GetCategories())
        {
            Anonymous c = new Anonymous();
            prefs[cat] = c;
            
            foreach (var opt in preferences.GetOptions(cat))
            {
                c[Sanitize(opt.Name)] = opt.Value;
            }
        }
        
        WinterForge.SerializeToFile(prefs, "userprefs.wf", TargetFormat.FormattedHumanReadable);
    }

    private static readonly HashSet<string> KEYWORDS = new HashSet<string>
    {
        "class", "struct", "interface", "enum", "delegate",
        "string", "int", "float", "double", "bool",
        "public", "private", "protected", "internal",
        "void", "return", "if", "else", "for", "while",
        "switch", "case", "break", "new", "null", "true", "false"
    };

    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "_";

        string normalized = input.Normalize(NormalizationForm.FormC);

        StringBuilder builder = new StringBuilder(normalized.Length);
        bool lastUnderscore = false;

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];

            if (IsValidChar(c, i == 0))
            {
                builder.Append(c);
                lastUnderscore = false;
                continue;
            }

            if (!lastUnderscore)
            {
                builder.Append('_');
                lastUnderscore = true;
            }
        }

        string result = builder.ToString().Trim('_');

        if (string.IsNullOrEmpty(result))
            result = "_";

        if (!IsValidFirst(result[0]))
            result = "_" + result;

        if (KEYWORDS.Contains(result))
            result = "_" + result;

        return result;
    }

    private static bool IsValidChar(char c, bool first)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

        if (first)
        {
            return IsValidFirst(c);
        }

        return
            category == UnicodeCategory.UppercaseLetter ||
            category == UnicodeCategory.LowercaseLetter ||
            category == UnicodeCategory.TitlecaseLetter ||
            category == UnicodeCategory.ModifierLetter ||
            category == UnicodeCategory.OtherLetter ||
            category == UnicodeCategory.DecimalDigitNumber ||
            c == '_';
    }

    private static bool IsValidFirst(char c)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

        return
            category == UnicodeCategory.UppercaseLetter ||
            category == UnicodeCategory.LowercaseLetter ||
            category == UnicodeCategory.TitlecaseLetter ||
            category == UnicodeCategory.ModifierLetter ||
            category == UnicodeCategory.OtherLetter ||
            c == '_';
    }
}