using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using JetBrains.Annotations;

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
        [XmlIgnore]
        private string _inGameDescription;
        [XmlElement("InGameDescription", IsNullable = true)]
        public System.Xml.XmlCDataSection InGameDescription
        {
            get
            {
                return new System.Xml.XmlDocument().CreateCDataSection(_inGameDescription);
            }
            set => _inGameDescription = value.Value;
        }
        // [XmlElement("InGameDescription")]
        // public XmlNode InGameDescription
        // {
        //     get
        //     {
        //         XmlDocument doc = new XmlDocument();
        //         return doc.CreateCDataSection(_inGameDescription);
        //     }
        //     set
        //     {
        //         _inGameDescription = value.Value;
        //     }
        // }
        [XmlElement(IsNullable = true)] 
        public string ChangeLog;
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