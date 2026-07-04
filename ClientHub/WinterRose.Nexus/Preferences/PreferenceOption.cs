using System;
using System.Collections.Generic;
using System.Linq;

namespace WinterRose.Nexus.Preferences;

public class PreferenceOption<T> : IPreferenceOption
{
    public string Name { get; }
    public string Description { get; }
    public string Category { get; }

    public T Value { get; set; }
    public T DefaultValue { get; }

    public bool RequiresRestart { get; }

    public ControlHint? Hint { get; }

    public IReadOnlyList<string>? AllowedOs { get; }

    public T? MinValue { get; }
    public T? MaxValue { get; }

    public bool IsFlagsEnum => typeof(T).IsEnum && typeof(T).IsDefined(typeof(FlagsAttribute), false);

    public Type ValueType => typeof(T);

    object? IPreferenceOption.Value
    {
        get => Value;
        set => Value = (T)value!;
    }

    object? IPreferenceOption.DefaultValue => DefaultValue;

    public bool IsVisibleOnCurrentOs
    {
        get
        {
            if (AllowedOs == null || AllowedOs.Count == 0)
                return true;

            if (OperatingSystem.IsWindows() && AllowedOs.Contains("windows"))
                return true;

            if (OperatingSystem.IsLinux() && AllowedOs.Contains("linux"))
                return true;

            if (OperatingSystem.IsMacOS() && AllowedOs.Contains("mac"))
                return true;

            return false;
        }
    }

    public PreferenceOption(
        string name,
        string description,
        string category,
        T defaultValue,
        bool requiresRestart = false,
        ControlHint? hint = null,
        IReadOnlyList<string>? allowedOs = null,
        T? minValue = default,
        T? maxValue = default)
    {
        Name = name;
        Description = description;
        Category = category;

        Value = defaultValue;
        DefaultValue = defaultValue;

        RequiresRestart = requiresRestart;
        Hint = hint;
        AllowedOs = allowedOs;

        MinValue = minValue;
        MaxValue = maxValue;
    }

    public void ResetToDefault()
    {
        Value = DefaultValue;
    }
}