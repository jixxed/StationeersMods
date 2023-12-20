using System.Collections.Generic;
using System.Xml.Serialization;

namespace StationeersMods.Plugin
{
    [XmlRoot("ModMetadata")]
    public class CustomModAbout
    {
        
        [XmlIgnore]
        public bool IsValid = true;
        public const string ROOT_NAME = "ModMetadata";
        [XmlElement]
        public string Name;
        [XmlElement]
        public string Author;
        [XmlElement]
        public string Version;
        [XmlElement]
        public string Description;
        [XmlElement]
        public string InGameDescription;
        [XmlElement]
        public ulong WorkshopHandle;
        [XmlArray("Tags")]
        [XmlArrayItem("Tag")]
        public List<string> Tags;
       
        [XmlArray("Dependencies")]
        [XmlArrayItem("Mod")]
        public List<ModVersion> Dependencies;
        
        [XmlArray("LoadBefore")]
        [XmlArrayItem("Mod")]
        public List<ModVersion> LoadBefore;
        
        [XmlArray("LoadAfter")]
        [XmlArrayItem("Mod")]
        public List<ModVersion> LoadAfter;
    }
}