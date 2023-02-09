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
    public class Mod : Resource
    {
        private List<string> _assemblyNames;
        private List<Mod> _conflictingMods;
        private List<GameObject> _prefabs;
        private List<ModScene> _scenes;

        private Dictionary<Type, object> allInstances;
        private List<Assembly> assemblies;

        public List<string> assemblyFiles { get; private set; }

        private readonly AssetBundleResource assetsResource;
        private readonly AssetBundleResource scenesResource;

        /// <summary>
        ///     Initialize a new Mod with a path to a mod file.
        /// </summary>
        /// <param name="path">The path to a mod file</param>
        public Mod(string path) : base(Path.GetFileNameWithoutExtension(path))
        {
            modInfo = ModInfo.Load(path);

            contentType = modInfo.content;
            contentTypes = modInfo.contentTypes;

            var modDirectory = Path.GetDirectoryName(path);
            var platformDirectory = Path.Combine(modDirectory, Application.platform.GetModPlatform().ToString());
            assemblyFiles = AssemblyUtility.GetAssemblies(modDirectory, AssemblyFilter.ModAssemblies);
            if (contentTypes.HasFlag(ContentType.scenes))
            {
                var assetsPath = Path.Combine(platformDirectory, modInfo.name.ToLower() + ".assets");
                var scenesPath = Path.Combine(platformDirectory, modInfo.name.ToLower() + ".scenes");

                if (File.Exists(assetsPath))
                    assetsResource = new AssetBundleResource(name + " assets", assetsPath);
                if (File.Exists(scenesPath))
                    scenesResource = new AssetBundleResource(name + " scenes", scenesPath);
            }

            isValid = true;

            Initialize();

            VerifyAssemblies();

            CheckResources();
        }

        /// <summary>
        ///     Collection of names of Assemblies included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> assemblyNames { get; private set; }

        /// <summary>
        ///     Collection of Mods that are in conflict with this Mod.
        /// </summary>
        public ReadOnlyCollection<Mod> conflictingMods { get; private set; }

        /// <summary>
        ///     Collection of names of Scenes included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> sceneNames { get; private set; }

        /// <summary>
        ///     Collection of paths of assets included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> assetPaths { get; private set; }

        /// <summary>
        ///     Collection of ModScenes included in this Mod.
        /// </summary>
        public ReadOnlyCollection<ModScene> scenes { get; private set; }

        /// <summary>
        ///     Collection of loaded prefabs included in this Mod. Only available when the mod is loaded.
        /// </summary>
        public ReadOnlyCollection<GameObject> prefabs { get; private set; }

        /// <summary>
        ///     This mod's ModInfo.
        /// </summary>
        public ModInfo modInfo { get; }

        /// <summary>
        ///     Types of content included in this Mod.
        /// </summary>
        public ModContent contentType { get; }

        public ContentType contentTypes { get; }

        /// <summary>
        ///     Is this Mod or any of its resources currently busy loading?
        /// </summary>
        public override bool isBusy
        {
            get { return base.isBusy || _scenes.Any(s => s.isBusy); }
        }

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
        public bool isEnabled
        {
            get => modInfo.isEnabled;
            set
            {
                modInfo.isEnabled = value;
                modInfo.Save();
            }
        }

        /// <summary>
        ///     Is the mod valid? A Mod becomes invalid when it is no longer being managed by the ModManager,
        ///     when any of its resources is missing or can't be loaded.
        /// </summary>
        public bool isValid { get; private set; }

        /// <summary>
        ///     The Mod's ContentHandler. Use for instantiating Objects and adding Components that have to be initialized for this
        ///     mod,
        ///     or cleaned up after the mod is unloaded.
        /// </summary>
        public ContentHandler contentHandler { get; set; }

        /// <summary>
        ///     Occurs when a ModScene has been loaded
        /// </summary>
        public event Action<ModScene> SceneLoaded;

        /// <summary>
        ///     Occurs when a ModScene has been unloaded
        /// </summary>
        public event Action<ModScene> SceneUnloaded;

        /// <summary>
        ///     Occurs when a ModScene has cancelled async loading.
        /// </summary>
        public event Action<ModScene> SceneLoadCancelled;

        private void Initialize()
        {
            allInstances = new Dictionary<Type, object>();
            assemblies = new List<Assembly>();
            _prefabs = new List<GameObject>();
            _scenes = new List<ModScene>();
            _conflictingMods = new List<Mod>();
            _assemblyNames = new List<string>();

            prefabs = _prefabs.AsReadOnly();
            scenes = _scenes.AsReadOnly();
            conflictingMods = _conflictingMods.AsReadOnly();
            assemblyNames = _assemblyNames.AsReadOnly();

            if (contentTypes.HasFlag(ContentType.scenes))
            {
                sceneNames = scenesResource.assetPaths;
                scenesResource.Loaded += OnScenesResourceLoaded;
                foreach (var sceneName in sceneNames)
                {
                    var modScene = new ModScene(sceneName, this);

                    modScene.Loaded += OnSceneLoaded;
                    modScene.Unloaded += OnSceneUnloaded;
                    modScene.LoadCancelled += OnSceneLoadCancelled;

                    _scenes.Add(modScene);
                }

                assetPaths = assetsResource.assetPaths;
                assetsResource.Loaded += OnAssetsResourceLoaded;
            }


            foreach (var assembly in assemblyFiles)
                _assemblyNames.Add(Path.GetFileName(assembly));

            contentHandler = new ContentHandler(this, _scenes.Cast<IResource>().ToList().AsReadOnly(), prefabs);
        }

        private void CheckResources()
        {
            Debug.Log("Checking Resources...");
            if (!modInfo.platforms.HasRuntimePlatform(Application.platform))
            {
                isValid = false;
                Debug.Log("Platform not supported for Mod: " + name);

                return;
            }

            if (contentTypes.HasFlag(ContentType.prefabs) && !assetsResource.canLoad)
            {
                isValid = false;
                Debug.Log("Assets assetbundle missing for Mod: " + name);
            }

            if (contentTypes.HasFlag(ContentType.scenes) && !scenesResource.canLoad)
            {
                isValid = false;
                Debug.Log("Scenes assetbundle missing for Mod: " + name);
            }

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

        private void VerifyAssemblies()
        {
            //if (!AssemblyVerifier.VerifyAssemblies(assemblyFiles))
            //{
            //    SetInvalid();
            //    Debug.Log("Incompatible assemblies found for Mod: " + name);
            //}
        }

        private void LoadAssemblies()
        {
            foreach (var path in assemblyFiles)
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    var assembly = Assembly.Load(File.ReadAllBytes(path));
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

        private void OnAssetsResourceLoaded(Resource resource)
        {
            try
            {
                if (assetsResource == null || assetsResource.assetBundle == null)
                    throw new Exception("Could not load assets.");

                var prefabs = assetsResource.assetBundle.LoadAllAssets<GameObject>();
                _prefabs.AddRange(prefabs);
            }
            catch (Exception e)
            {
                LogUtility.LogException(e);
                SetInvalid();
                Unload();
            }
        }

        private void OnScenesResourceLoaded(Resource resource)
        {
            if (scenesResource == null || scenesResource.assetBundle == null)
            {
                LogUtility.LogError("Could not load scenes.");
                SetInvalid();
                Unload();
            }
        }

        protected override IEnumerator LoadResources()
        {
            LogUtility.LogInfo("Loading Mod: " + name);

            LoadAssemblies();
            if (assetsResource != null)
                assetsResource.Load();

            if (scenesResource != null)
                scenesResource.Load();

            yield break;
        }

        protected override IEnumerator LoadResourcesAsync()
        {
            LogUtility.LogInfo("Async loading Mod: " + name);

            LoadAssemblies();

            if (assetsResource != null)
                assetsResource.LoadAsync();

            if (scenesResource != null)
                scenesResource.LoadAsync();

            yield return UpdateProgress(assetsResource, scenesResource);
        }

        private IEnumerator UpdateProgress(params Resource[] resources)
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
            contentHandler.Clear();

            _scenes.ForEach(s => s.Unload());

            foreach (var loader in GetInstances<IModHandler>()) loader.OnUnloaded();
        }

        protected override void UnloadResources()
        {
            LogUtility.LogInfo("Unloading Mod: " + name);

            allInstances.Clear();
            assemblies.Clear();
            _prefabs.Clear();

            if (assetsResource != null)
                assetsResource.Unload();
            if (scenesResource != null)
                scenesResource.Unload();

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private void OnSceneLoaded(Resource scene)
        {
            SceneLoaded?.Invoke((ModScene) scene);
        }

        private void OnSceneLoadCancelled(Resource scene)
        {
            SceneLoadCancelled?.Invoke((ModScene) scene);

            if (!_scenes.Any(s => s.isBusy))
                End();
        }

        private void OnSceneUnloaded(Resource scene)
        {
            SceneUnloaded?.Invoke((ModScene) scene);

            if (!_scenes.Any(s => s.isBusy))
                End();
        }

        protected override void OnLoadResumed()
        {
            //resume scene loading
            foreach (var scene in _scenes)
                if (scene.loadState == ResourceLoadState.Cancelling)
                    scene.Load();

            base.OnLoadResumed();
        }

        protected override void OnLoaded()
        {
            foreach (var loader in GetInstances<IModHandler>())
                loader.OnLoaded(contentHandler);

            base.OnLoaded();
        }

        /// <summary>
        ///     Update this Mod's conflicting Mods with the supplied Mod
        /// </summary>
        /// <param name="other">Another Mod</param>
        public void UpdateConflicts(Mod other)
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

            if (sceneNames != null)
                foreach (var sceneName in sceneNames)
                    if (other.sceneNames != null)
                        foreach (var otherSceneName in other.sceneNames)
                            if (sceneName == otherSceneName)
                            {
                                Debug.Log("Scene " + other.name + "/" + otherSceneName + " conflicting with " + name +
                                          "/" +
                                          sceneName);

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
        public void UpdateConflicts(IEnumerable<Mod> mods)
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
        ///     Get an asset with name.
        /// </summary>
        /// <param name="name">The asset's name.</param>
        /// <returns>The asset if it has been found. Null otherwise</returns>
        public Object GetAsset(string name)
        {
            if (assetsResource != null && assetsResource.loadState == ResourceLoadState.Loaded)
                return assetsResource.assetBundle.LoadAsset(name);

            return null;
        }

        /// <summary>
        ///     Get an asset with name of a certain Type.
        /// </summary>
        /// <param name="name">The asset's name.</param>
        /// <typeparam name="T">The asset Type.</typeparam>
        /// <returns>The asset if it has been found. Null otherwise</returns>
        public T GetAsset<T>(string name) where T : Object
        {
            if (assetsResource != null && assetsResource.loadState == ResourceLoadState.Loaded)
                return assetsResource.assetBundle.LoadAsset<T>(name);

            return null;
        }

        /// <summary>
        ///     Get all assets of a certain Type.
        /// </summary>
        /// <typeparam name="T">The asset Type.</typeparam>
        /// <returns>AssetBundleRequest that can be used to get the asset.</returns>
        public T[] GetAssets<T>() where T : Object
        {
            if (assetsResource != null && assetsResource.loadState == ResourceLoadState.Loaded)
                return assetsResource.assetBundle.LoadAllAssets<T>();

            return new T[0];
        }

        /// <summary>
        ///     Get an asset with name of a certain Type.
        /// </summary>
        /// <param name="name">The asset's name.</param>
        /// <typeparam name="T">The asset's Type</typeparam>
        /// <returns>AssetBundleRequest that can be used to get the asset.</returns>
        public AssetBundleRequest GetAssetAsync<T>(string name) where T : Object
        {
            if (assetsResource != null && assetsResource.loadState == ResourceLoadState.Loaded)
                return assetsResource.assetBundle.LoadAssetAsync<T>(name);

            return null;
        }

        /// <summary>
        ///     Get all assets of a certain Type.
        /// </summary>
        /// <typeparam name="T">The asset Type.</typeparam>
        /// <returns>AssetBundleRequest that can be used to get the assets.</returns>
        public AssetBundleRequest GetAssetsAsync<T>() where T : Object
        {
            if (assetsResource != null && assetsResource.loadState == ResourceLoadState.Loaded)
                return assetsResource.assetBundle.LoadAllAssetsAsync<T>();

            return null;
        }

        /// <summary>
        ///     Get all Components of type T in all prefabs
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInPrefabs<T>()
        {
            var components = new List<T>();

            foreach (var prefab in prefabs) components.AddRange(prefab.GetComponentsInChildren<T>());

            return components.ToArray();
        }

        /// <summary>
        ///     Get all Components of type T in all loaded ModScenes.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInScenes<T>()
        {
            if (!typeof(T).IsSubclassOf(typeof(Component)))
                throw new ArgumentException(typeof(T).Name + " is not a component.");

            var components = new List<T>();

            foreach (var scene in _scenes) components.AddRange(scene.GetComponentsInScene<T>());

            return components.ToArray();
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