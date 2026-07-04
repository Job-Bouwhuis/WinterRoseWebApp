using WinterRose.AnonymousTypes;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Configuration;

public class Config
{
    private readonly string configPath;
    private Anonymous config;

    public Config(string configPath, bool autoLoad = true)
    {
        this.configPath = configPath;
        if(autoLoad)
            LoadConfig();
    }

    public object this[string key]
    {
        get => config[key];
        set => config[key] = value;
    }
    
    public object Get(string key) => config[key];
    public T Get<T>(string key) => config.Get<T>(key);
    public void Set(string key, object value) => config.Set(key, value);
    public void Set<T>(string key, T value) => config.Set(key, value);
    

    public void LoadConfig()
    {
        if (File.Exists(configPath))
        {
            using FileStream file = new(configPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            object result = WinterForge.DeserializeFromHumanReadableStream(file);
            if (result is Anonymous a)
            {
                config = a;
                return;
            }
        }

        CreateNewConfig();
    }

    public void SaveConfig()
    {
        WinterForge.SerializeToFile(config,  configPath, TargetFormat.FormattedHumanReadable);
    }
    
    private void CreateNewConfig()
    {
        config = new();
        config["DemoConfig1"] = "this is a demo";
        config["DemoConfig2"] = 42;
        Anonymous nested = new();
        nested["NestedConfigValue"] = "you can nest as much as youd like";
        config["NestedDemo"] = nested;
        
        WinterForge.SerializeToFile(config, configPath, TargetFormat.FormattedHumanReadable);
    }
}