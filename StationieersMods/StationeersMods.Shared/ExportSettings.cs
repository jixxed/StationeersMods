using System;
using System.Collections.Generic;
using UnityEngine;

namespace StationeersMods.Shared
{
    /// <summary>
    ///     Stores the exporter's settings.
    /// </summary>
    /// 

    [Flags]
    public enum ContentType { assemblies = 2, prefabs = 4, scenes = 8 }

    public enum BootType { entrypoint = 0, prefab = 1, scene = 2, bepin = 3 }

    public class ExportSettings : ScriptableObject
    {
        [SerializeField] private string _author;

        [SerializeField] private string _description;

        [SerializeField] private string _name;

        [SerializeField] private string _outputDirectory;

        [SerializeField] private string _stationeersDirectory;

        [SerializeField] private string _stationeersArguments;

        [SerializeField] private string _version;

        [SerializeField] private string[] _assemblies = new string[] { };

        [SerializeField] private string[] _artifacts = new string[] { };

        [SerializeField] private ContentType _contentTypes;

        [SerializeField] private bool _includePdbs;

        [SerializeField] private BootType _bootType;

        [SerializeField] private GameObject _startupPrefab;

        [SerializeField] private string _startupClass;

        [SerializeField] private string _startupScene;

        /// <summary>
        ///     The Mod's name.
        /// </summary>
        public string Name {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        ///     The Mod's author.
        /// </summary>
        public string Author {
            get => _author;
            set => _author = value;
        }

        /// <summary>
        ///     The Mod's description.
        /// </summary>
        public string Description {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        ///     The Mod's version.
        /// </summary>
        public string Version {
            get => _version;
            set => _version = value;
        }

        /// <summary>
        ///     The directory to which the Mod will be exported.
        /// </summary>
        public string[] Assemblies {
            get => _assemblies;
            set => _assemblies = value;
        }
        public string[] Artifacts {
            get => _artifacts;
            set => _artifacts = value;
        }

        public string OutputDirectory { get => _outputDirectory; set => _outputDirectory = value; }

        public string StationeersDirectory { get => _stationeersDirectory; set => _stationeersDirectory = value; }

        public string StationeersArguments { get => _stationeersArguments; set => _stationeersArguments = value; }

        public ContentType ContentTypes { get => _contentTypes; set => _contentTypes =value;}

        public bool IncludePdbs { get => _includePdbs; set => _includePdbs = value; }

        public BootType BootType { get => _bootType; set => _bootType =value;}

        public string StartupClass { get => _startupClass; set => _startupClass =value;}

        public string StartupScene { get => _startupScene; set => _startupScene =value;}

        public GameObject StartupPrefab { get => _startupPrefab; set => _startupPrefab =value;}

    }
}