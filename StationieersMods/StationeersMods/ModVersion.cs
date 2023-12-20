using System.Xml.Serialization;

namespace StationeersMods.Plugin
{
    [XmlRoot("Mod")]
    public class ModVersion
    {
        [XmlElement] public string Version;
        [XmlElement] public ulong Id;

        public ModVersion(string version, ulong modId)
        {
            Version = version;
            Id = modId;
        }

        public ModVersion()
        {
            Version = "-1";
        }

        public bool IsSame(in string version, in ulong modId)
        {
            return modId == Id && (version.Equals("-1") || Version.Equals("-1") || version.Equals(Version));
        }

        public override string ToString()
        {
            return "{" + Id + "@" + (!Version.Equals("-1") ? Version : "Any version" ) + "}";
        }
    }
}