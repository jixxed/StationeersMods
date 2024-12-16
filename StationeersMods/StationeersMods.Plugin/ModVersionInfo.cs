namespace StationeersMods.Plugin;

public class ModVersionInfo
{
    public string Name { get; set; }
    public string Version { get; set; }
    
    public ModVersionInfo(string name, string version)
    {
        Name = name;
        Version = version;
    }
}