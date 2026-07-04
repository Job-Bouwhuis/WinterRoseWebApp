using System.Collections.Generic;
using System.Linq;

namespace WinterRose.Nexus.Preferences;

public class UserPreferences
{
    private readonly List<IPreferenceOption> options = new();

    public IReadOnlyList<IPreferenceOption> Options => options;

    public void Register(IPreferenceOption option)
    {
        options.Add(option);
    }

    public IEnumerable<string> GetCategories()
    {
        return options
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x);
    }

    public IEnumerable<IPreferenceOption> GetOptions(string category)
    {
        return options
            .Where(x => x.Category == category)
            .Where(x => x.IsVisibleOnCurrentOs);
    }

    public T Get<T>(string name)
    {
        return (T)options.First(x => x.Name == name).Value!;
    }

    public void Set<T>(string name, T value)
    {
        var opt = options.First(x => x.Name == name);
        opt.Value = value!;
    }
}