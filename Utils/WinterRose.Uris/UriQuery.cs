using System.Text;

namespace WinterRose.Uris;

public sealed class UriQuery
{
    private readonly Dictionary<string, string?> values = [];

    public int Count => values.Count;
    public IReadOnlyList<string> Keys => values.Keys.ToList();
    
    public string? this[string key]
    {
        get => values.GetValueOrDefault(key);
        set => values[key] = value;
    }

    public void Set<T>(string key, T value)
    {
        values[key] = value?.ToString();
    }

    public T Get<T>(string key)
    {
        if (!values.TryGetValue(key, out string value))
            return default;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public bool Contains(string key)
    {
        return values.ContainsKey(key);
    }

    public IReadOnlyDictionary<string, string> AsDictionary()
    {
        return values;
    }

    public override string ToString()
    {
        if (this.values.Count == 0)
            return "";
        
        var values = this.values.Select(kv => $"{kv.Key}={kv.Value}");
        return "?" + string.Join("&", values);
    }
}