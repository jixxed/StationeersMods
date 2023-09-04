using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using StationeersMods.Cecil;
using StationeersMods.Interface;
using StationeersMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace StationeersMods
{
    /// <summary>
    ///     Class that represents a Mod.
    ///     A Mod lets you load scenes, prefabs and Assemblies that have been exported with the game's generated ModTools.
    /// </summary>
    public class AssemblyMod : Resource
    {
        private List<string> _assemblyNames;
        protected List<AssemblyMod> _conflictingMods;

        private Dictionary<Type, object> allInstances;
        private List<Assembly> assemblies;

        public List<string> assemblyFiles { get; protected set; }

        /// <summary>
        ///     Initialize a new Mod with a path to a mod file.
        /// </summary>
        /// <param name="path">The path to a mod file</param>
        public AssemblyMod(string path) : base(Path.GetFileNameWithoutExtension(path), Path.GetDirectoryName(path))
        {
            // modInfo = ModInfo.Load(path);

            // contentType = modInfo.content;
            // contentTypes = modInfo.contentTypes;

            modDirectory = Path.GetDirectoryName(path);
            // var platformDirectory = Path.Combine(modDirectory, Application.platform.GetModPlatform().ToString());
            assemblyFiles = AssemblyUtility.GetAssemblies(modDirectory, AssemblyFilter.ModAssemblies);
            // if (contentTypes.HasFlag(ContentType.scenes))
            // {
                // var assetsPath = Path.Combine(platformDirectory, modInfo.name.ToLower() + ".assets");
                // var scenesPath = Path.Combine(platformDirectory, modInfo.name.ToLower() + ".scenes");

                // if (File.Exists(assetsPath))
                    // assetsResource = new AssetBundleResource(name + " assets", assetsPath);
                // if (File.Exists(scenesPath))
                    // scenesResource = new AssetBundleResource(name + " scenes", scenesPath);
            // }

            isValid = true;

            Initialize();

            VerifyAssemblies();

            // CheckResources();
        }

        /// <summary>
        ///     Collection of names of Assemblies included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> assemblyNames { get; private set; }

        /// <summary>
        ///     Collection of Mods that are in conflict with this Mod.
        /// </summary>
        public ReadOnlyCollection<AssemblyMod> conflictingMods { get; private set; }

    
        /// <summary>
        ///     Types of content included in this Mod.
        /// </summary>
        public ModContent contentType { get; set; }

        public ContentType contentTypes { get;set; }
        
        /// <summary>
        ///     Can this mod be loaded? False if a conflicting mod is loaded, if the mod is not enabled or if the mod is not valid
        /// </summary>
        public override bool canLoad
        {
            get
            {
                CheckResources();
                return !ConflictingModsLoaded() && isValid;
            }
        }

        /// <summary>
        ///     Set the mod to be enabled or disabled
        /// </summary>
        public virtual bool isEnabled
        {
            get => true;
            set { }
        }

        /// <summary>
        ///     Is the mod valid? A Mod becomes invalid when it is no longer being managed by the ModManager,
        ///     when any of its resources is missing or can't be loaded.
        /// </summary>
        public bool isValid { get; protected set; }
        

        /// <summary>
        ///     Occurs when a ModScene has cancelled async loading.
        /// </summary>
        // public event Action<ModScene> SceneLoadCancelled;

        private void Initialize()
        {
            allInstances = new Dictionary<Type, object>();
            assemblies = new List<Assembly>();
            _conflictingMods = new List<AssemblyMod>();
            _assemblyNames = new List<string>();

            conflictingMods = _conflictingMods.AsReadOnly();
            assemblyNames = _assemblyNames.AsReadOnly();
            
            foreach (var assembly in assemblyFiles)
                _assemblyNames.Add(Path.GetFileName(assembly));
            
        }

        private void CheckResources()
        {
            Debug.Log("Checking Resources...");
            // if (!modInfo.platforms.HasRuntimePlatform(Application.platform))
            // {
            //     isValid = false;
            //     Debug.Log("Platform not supported for Mod: " + name);
            //
            //     return;
            // }
            
            if (contentTypes.HasFlag(ContentType.assemblies) && assemblyFiles.Count == 0)
            {
                isValid = false;
                Debug.Log("Assemblies missing for Mod: " + name);
            }

            foreach (var path in assemblyFiles)
                if (!File.Exists(path))
                {
                    isValid = false;
                    Debug.Log(path + " missing for Mod: " + name);
                }
        }

        protected override IEnumerator LoadResources()
        {
            LogUtility.LogInfo("Loading Mod: " + name);

            LoadAssemblies();

            yield break;
        }

        protected override IEnumerator LoadResourcesAsync()
        {
            LogUtility.LogInfo("Async loading Mod: " + name);

            LoadAssemblies();

            yield break;
        }

        protected void VerifyAssemblies()
        {
            //if (!AssemblyVerifier.VerifyAssemblies(assemblyFiles))
            //{
            //    SetInvalid();
            //    Debug.Log("Incompatible assemblies found for Mod: " + name);
            //}
        }

        protected void LoadAssemblies()
        {
            foreach (var path in assemblyFiles)
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    var assembly = Assembly.LoadFrom(path);
                    assembly.GetTypes();
                    assemblies.Add(assembly);
                }
                catch (Exception e)
                {
                    LogUtility.LogException(e);
                    SetInvalid();
                    Unload();
                }
            }
        }
        
        protected IEnumerator UpdateProgress(params Resource[] resources)
        {
            if (resources == null || resources.Length == 0)
                yield break;

            if (resources != null)
            {
                var loadingResources = resources.Where(r => r.canLoad);

                var count = loadingResources.Count();

                while (true)
                {
                    var isDone = true;
                    float progress = 0;

                    foreach (var resource in loadingResources)
                    {
                        isDone = isDone && resource.loadState == ResourceLoadState.Loaded;
                        progress += resource.loadProgress;
                    }

                    loadProgress = progress / count;

                    if (isDone)
                        yield break;

                    yield return null;
                }
            }
        }

        protected override void PreUnLoadResources()
        {
            // contentHandler.Clear();

            foreach (var loader in GetInstances<IModHandler>()) loader.OnUnloaded();
        }

        protected override void UnloadResources()
        {
            LogUtility.LogInfo("Unloading Assembly Mod: " + name);

            allInstances.Clear();
            assemblies.Clear();

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        

        /// <summary>
        ///     Update this Mod's conflicting Mods with the supplied Mod
        /// </summary>
        /// <param name="other">Another Mod</param>
        public virtual void UpdateConflicts(AssemblyMod other)
        {
            if (other == this || !isValid)
                return;

            if (!other.isValid)
            {
                if (_conflictingMods.Contains(other))
                    _conflictingMods.Remove(other);

                return;
            }

            foreach (var assemblyName in _assemblyNames)
            foreach (var otherAssemblyName in other.assemblyNames)
                if (assemblyName == otherAssemblyName)
                {
                    Debug.Log("Assembly " + other.name + "/" + otherAssemblyName + " conflicting with " + name + "/" +
                              assemblyName);

                    if (!_conflictingMods.Contains(other))
                    {
                        _conflictingMods.Add(other);
                        return;
                    }
                }
        }

        /// <summary>
        ///     Update this Mod's conflicting Mods with the supplied Mods
        /// </summary>
        /// <param name="mods">A collection of Mods</param>
        public void UpdateConflicts(IEnumerable<AssemblyMod> mods)
        {
            foreach (var mod in mods) UpdateConflicts(mod);
        }

        /// <summary>
        ///     Is another conflicting Mod loaded?
        /// </summary>
        /// <returns>True if another conflicting mod is loaded</returns>
        public bool ConflictingModsLoaded()
        {
            return _conflictingMods.Any(m => m.loadState != ResourceLoadState.Unloaded);
        }

        /// <summary>
        ///     Is another conflicting Mod enabled?
        /// </summary>
        /// <returns>True if another conflicting mod is enabled</returns>
        public bool ConflictingModsEnabled()
        {
            return _conflictingMods.Any(m => m.isEnabled);
        }

        /// <summary>
        ///     Invalidate the mod
        /// </summary>
        public void SetInvalid()
        {
            isValid = false;
        }

        /// <summary>
        ///     Get instances of all Types included in the Mod that implement or derive from Type T.
        ///     Reuses existing instances and creates new instances for Types that have no instance yet.
        ///     Does not instantiate Components; returns all active instances of the Component instead.
        /// </summary>
        /// <typeparam name="T">The Type that will be looked for</typeparam>
        /// <param name="args">Optional arguments for the Type's constructor</param>
        /// <returns>A List of Instances of Types that implement or derive from Type T</returns>
        public T[] GetInstances<T>(params object[] args)
        {
            var instances = new List<T>();

            if (loadState != ResourceLoadState.Loaded)
                return instances.ToArray();

            foreach (var assembly in assemblies)
                try
                {
                    instances.AddRange(GetInstances<T>(assembly, args));
                }
                catch (Exception e)
                {
                    LogUtility.LogException(e);
                }

            return instances.ToArray();
        }

        private T[] GetInstances<T>(Assembly assembly, params object[] args)
        {
            var instances = new List<T>();

            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(T).IsAssignableFrom(type))
                    continue;

                if (type.IsAbstract)
                    continue;

                if (!type.IsClass)
                    continue;

                object foundInstance;
                if (allInstances.TryGetValue(type, out foundInstance))
                {
                    //LogUtility.Log("existing instance of " + typeof(T).Name + " found: " + type.Name);
                    instances.Add((T) foundInstance);
                    continue;
                }

                if (type.IsSubclassOf(typeof(Component)))
                {
                    foreach (var component in GetComponents(type)) instances.Add((T) (object) component);
                    continue;
                }

                try
                {
                    var instance = (T) Activator.CreateInstance(type, args);
                    instances.Add(instance);
                    allInstances.Add(type, instance);
                }
                catch (Exception e)
                {
                    if (e is MissingMethodException)
                        Debug.Log(e.Message);
                    else
                        LogUtility.LogException(e);
                }
            }

            return instances.ToArray();
        }

        private static Component[] GetComponents(Type componentType)
        {
            var components = new List<Component>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
                components.AddRange(SceneManager.GetSceneAt(i).GetComponentsInScene(componentType));

            return components.ToArray();
        }
    }
}