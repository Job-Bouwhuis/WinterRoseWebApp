using System.Text;

namespace WinterRose.Uris;

public sealed class UriQuery
{
    private readonly Dictionary<string, string> VALUES = [];

    public int Count => VALUES.Count;
    public IReadOnlyList<string> Keys => VALUES.Keys.ToList();
    
    public string this[string key]
    {
        get => VALUES.TryGetValue(key, out string value) ? value : null;
        set => VALUES[key] = value;
    }

    public void Set<T>(string key, T value)
    {
        VALUES[key] = value?.ToString();
    }

    public T Get<T>(string key)
    {
        if (!VALUES.TryGetValue(key, out string value))
            return default;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public bool Contains(string key)
    {
        return VALUES.ContainsKey(key);
    }

    public IReadOnlyDictionary<string, string> AsDictionary()
    {
        return VALUES;
    }

    public override string ToString()
    {
        if (VALUES.Count == 0)
            return "";
        
        var values = VALUES.Select(kv => $"{kv.Key}={kv.Value}");
        return "?" + string.Join("&", values);
    }
}