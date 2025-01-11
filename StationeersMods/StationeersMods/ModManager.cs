using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using BepInEx;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using StationeersMods.Plugin;
using StationeersMods.Shared;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;

namespace StationeersMods
{
    /// <summary>
    ///     Main class for finding mods
    /// </summary>
    public class ModManager : UnitySingleton<ModManager>
    {
        private readonly object _lock = new object();

        private Dictionary<string, AssemblyMod> _modPaths;
        public Dictionary<GameObject, AssemblyMod> modMap;
        private List<AssemblyMod> _mods;
        private int _refreshInterval;

        private Dispatcher dispatcher;
        private List<AssemblyMod> queuedRefreshMods;

        private static List<ModSearchDirectory> searchDirectories;
        private WaitForSeconds wait;

        /// <summary>
        ///     Default directory that will be searched for mods.
        /// </summary>
        public string defaultSearchDirectory { get; private set; }

        /// <summary>
        ///     The interval (in seconds) between refreshing Mod search directories.
        ///     Set to 0 to disable auto refreshing.
        /// </summary>
        public int refreshInterval
        {
            get => _refreshInterval;
            set
            {
                _refreshInterval = value;

                StopAllCoroutines();

                if (_refreshInterval < 1)
                    return;

                wait = new WaitForSeconds(_refreshInterval);
                StartCoroutine(AutoRefreshSearchDirectories());
            }
        }

        /// <summary>
        ///     All mods that have been found in all search directories.
        /// </summary>
        public ReadOnlyCollection<AssemblyMod> mods { get; private set; }

        /// <summary>
        ///     Occurs when the collection of Mods has changed.
        /// </summary>
        public event Action ModsChanged;

        /// <summary>
        ///     Occurs when a Mod has been found.
        /// </summary>
        public event Action<AssemblyMod> ModFound;

        /// <summary>
        ///     Occurs when a Mod has been removed. The Mod will be marked invalid.
        /// </summary>
        public event Action<AssemblyMod> ModRemoved;

        /// <summary>
        ///     Occurs when a Mod has been loaded
        /// </summary>
        public event Action<AssemblyMod> ModLoaded;

        /// <summary>
        ///     Occurs when a Mod has been Unloaded
        /// </summary>
        public event Action<AssemblyMod> ModUnloaded;

        /// <summary>
        ///     Occurs when a Mod has cancelled async loading
        /// </summary>
        public event Action<AssemblyMod> ModLoadCancelled;

        /// <summary>
        ///     Occurs when a ModScene has been loaded
        /// </summary>
        public event Action<ModScene> SceneLoaded;

        /// <summary>
        ///     Occurs when a ModScene has been unloaded
        /// </summary>
        public event Action<ModScene> SceneUnloaded;

        /// <summary>
        ///     Occurs when a ModScene has cancelled async loading
        /// </summary>
        public event Action<ModScene> SceneLoadCancelled;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);

            LogUtility.logLevel = LogLevel.Debug;

            dispatcher = Dispatcher.instance;

            _mods = new List<AssemblyMod>();
            _modPaths = new Dictionary<string, AssemblyMod>();
            queuedRefreshMods = new List<AssemblyMod>();
            searchDirectories = new List<ModSearchDirectory>();

            mods = _mods.AsReadOnly();
            //patch for running both Addons and StationeersMods, somehow causing StringManager not to be initialized in time
            StringManager.Initialize();

            Harmony harmony = new Harmony("StationeersMods.ModManager");
            harmony.Patch(
                typeof(WorldManager).GetMethod("LoadDataFiles", BindingFlags.NonPublic | BindingFlags.Static),
                new HarmonyMethod(typeof(ModManager).GetMethod("WorldManagerFix",
                    BindingFlags.NonPublic | BindingFlags.Static)));
            AddLocalAndWorkshopItems();
        }

        // private static void LoadDataFiles()
        // {
        //     WorldManager.LoadDataFilesAtPath(Application.streamingAssetsPath + "/Data");
        //     List<ModData> mods = WorkshopMenu.ModsConfig.Mods;
        //     for (int index = mods.Count - 1; index >= 0; --index)
        //     {
        //         ModData modData = mods[index];
        //         if (!modData.IsCore && modData.IsEnabled)
        //             WorldManager.LoadDataFilesAtPath(modData.LocalPath + "/GameData");
        //     }
        // }
        private static bool WorldManagerFix()
        {
            initMods();
            List<ModData> mods = WorkshopMenu.ModsConfig?.Mods != null ? WorkshopMenu.ModsConfig.Mods : new List<ModData>();
            try
            {
                validateModOrder();
                for (int index = mods.Count - 1; index >= 0; --index)
                {
                    ModData modData = mods[index];
                    if (!modData.IsCore && modData.IsEnabled)
                    {
                        typeof(WorldManager).GetMethod("LoadDataFilesAtPath", BindingFlags.NonPublic | BindingFlags.Static)
                            ?.Invoke(null, new object[] {modData.LocalPath + "/GameData"});
                    }
                    else if (modData.IsCore)
                    {
                        typeof(WorldManager).GetMethod("LoadDataFilesAtPath", BindingFlags.NonPublic | BindingFlags.Static)
                            ?.Invoke(null, new object[] {Application.streamingAssetsPath + "/Data"});
                    }
                }
            }
            catch (MissingDependencyException ex)
            {
                //do not load mods
                //log error
                Debug.LogError(ex.Message);
                PromptPanel.Instance.ShowPrompt("Missing dependencies detected", ex.Message, "Subscribe", () => SubscribeToMissingMods(ex.Missing));
            }
            catch (DependencyException ex)
            {
                //do not load mods
                //log error
                Debug.LogError(ex.Message);
                AlertPanel.Instance.ShowAlert(ex.Message, AlertState.Alert);
            }

            return false;
        }

        private static async void SubscribeToMissingMods(List<ModVersion> missing)
        {
            List<UniTask<bool>> tasks = missing.Select(modVersion =>
            {
                Debug.Log("Try to subscribe to: " + modVersion.Id);
                return SubscribeToMod(modVersion.Id);
            }).ToList();
            var results = await UniTask.WhenAll(tasks);

            if (results.All(result => result == false))
            {
                AlertPanel.Instance.ShowAlert($"StationeersMods has failed to subscribe to all the listed mods. \nYou will need to remove the mods that fail to load.", AlertState.Alert);
            }
            else if (results.Any(result => result == false))
            {
                AlertPanel.Instance.ShowAlert($"StationeersMods has failed to subscribe to some of the listed mods. \nYou will need to remove the mods that fail to load. \nA restart of the game is required!", AlertState.Alert);
            }
            else
            {
                AlertPanel.Instance.ShowAlert($"StationeersMods has subscribed to all listed mods. \nA restart of the game is required!", AlertState.Alert);
            }
        }

        private static UniTask<bool> SubscribeToMod(ulong modId)
        {
            object[] parameters = {modId};
            UniTask<bool> task = (UniTask<bool>) typeof(SteamTransport).GetMethod("Workshop_SubscribeToItemAsync", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, parameters);
            return task;
        }

        private static async void initMods()
        {
            var items = await GetLocalAndWorkshopItems(SteamTransport.WorkshopType.Mod);

            List<ulong> addedHandles = new List<ulong>();
            foreach (SteamTransport.ItemWrapper localAndWorkshopItem in items.OrderBy(wrapper => wrapper.IsLocal()).Reverse())
            {
                SteamTransport.ItemWrapper item = localAndWorkshopItem;
                ulong workshopHandle = 0;
                if(File.Exists(item.DirectoryPath + "\\About\\About.xml"))
                {
                    var modAbout = XmlSerialization.Deserialize<ModAbout>(item.DirectoryPath + "\\About\\About.xml", "ModMetadata");
                    workshopHandle = modAbout.WorkshopHandle;
                }
                if (!addedHandles.Contains(workshopHandle))
                {
                    if(workshopHandle != 0) // 0 indicates no workshophandle
                        addedHandles.Add(workshopHandle);
                    try
                    {
                        if (WorkshopMenu.ModsConfig.Mods.All<ModData>((Func<ModData, bool>) (x => x.LocalPath != item.DirectoryPath)))
                            WorkshopMenu.ModsConfig.Mods.Add(new ModData(item, true));
                    }
                    catch (Exception ex)
                    {
                        ConsoleWindow.PrintError(string.Format("Error loading mod with id {0}", (object) item.Id), false);
                        ConsoleWindow.PrintError(ex.Message, false);
                    }
                }
            }

            SaveModConfig();
        }

        private static void validateModOrder()
        {
            ValidationResult validationResult = new ValidationResult();
            do
            {
                //if any mod has been reordered > 50 times, there is likely a circular dependency and we can stop loading.
                if (validationResult.ReorderCount.Values.Any(value => value > 50))
                {
                    throw new DependencyException("Mod loading failed. Circular dependency detected. (A load before B, B load before A)");
                }

                validationResult.Retry = false;
                List<ModData> allMods = WorkshopMenu.ModsConfig.Mods;
                List<ModVersion> availableMods = listAvailableMods();
                Debug.Log("available mods: " + availableMods.Join((mod) => mod.ToString(), ","));

                validateOrder(allMods, availableMods, validationResult);
            } while (validationResult.Retry);

            if (validationResult.NeedSave)
            {
                SaveModConfig();
                AlertPanel.Instance.ShowAlert($"StationeersMods has updated the mod loading order. A restart of the game is required!", AlertState.Alert);
            }
        }

        private static void validateOrder(List<ModData> allMods, List<ModVersion> availableMods, ValidationResult validationResult)
        {
            List<ModVersion> loadedMods = new List<ModVersion>();
            for (int index = allMods.Count - 1; index >= 0; --index)
            {
                ModData modData = allMods[index];
                if (modData.IsCore)
                {
                    loadedMods.Add(new ModVersion(VersionHelper.GameVersion(), 1UL));
                    continue;
                }

                if (!File.Exists(modData.AboutXmlPath))
                {
                    //skip for mods with missing about.xml, because we can't determine workshop handle or load order
                    continue;
                }

                var modAbout = XmlSerialization.Deserialize<CustomModAbout>(modData.AboutXmlPath, "ModMetadata");
                validateDependencies(availableMods, modAbout);

                if (checkModLoadAfter(availableMods, validationResult, modAbout, loadedMods, modData)) break;

                if (checkModLoadBefore(validationResult, modAbout, loadedMods, modData)) break;

                loadedMods.Add(new ModVersion(modAbout.Version, modAbout.WorkshopHandle));
            }
        }

        private static bool checkModLoadBefore(ValidationResult validationResult, CustomModAbout modAbout, List<ModVersion> loadedMods, ModData modData)
        {
            var loadBefore = modAbout.LoadBefore;

            // Debug.Log( "Mods to load before: " + loadBefore.Join((mod) => mod.ToString(), ","));
            if (loadBefore.Any(beforeMod => loadedMods.Any(loadedMod => loadedMod.IsSame(beforeMod.Version, beforeMod.Id))))
            {
                //not all required dependencies have been listed after
                loadBefore.FindAll(beforeMod => loadedMods.Any(loadedMod => loadedMod.IsSame(beforeMod.Version, beforeMod.Id))).ForEach(beforeMod =>
                {
                    var mod = WorkshopMenu.ModsConfig.Mods.Find(modx =>
                    {
                        if (beforeMod.Id == 1UL)
                        {
                            return modx.IsCore;
                        }

                        if (!File.Exists(modx.AboutXmlPath))
                        {
                            //skip for missing mods
                            return false;
                        }

                        var aboutDatax = XmlSerialization.Deserialize<CustomModAbout>(modx.AboutXmlPath, "ModMetadata");
                        return beforeMod.IsSame(aboutDatax.Version, aboutDatax.WorkshopHandle);
                    });
                    while (WorkshopMenu.ModsConfig.Mods.IndexOf(mod) > WorkshopMenu.ModsConfig.Mods.IndexOf(modData))
                    {
                        WorkshopMenu.ModsConfig.MoveModUp(mod);
                    }
                });
                validationResult.ReorderCount[modAbout.WorkshopHandle] = validationResult.ReorderCount.GetValueOrDefault(modAbout.WorkshopHandle, 0) + 1;
                validationResult.Retry = true;
                validationResult.NeedSave = true;
                return true;
            }

            return false;
        }

        private static bool checkModLoadAfter(List<ModVersion> availableMods, ValidationResult validationResult, CustomModAbout modAbout, List<ModVersion> loadedMods, ModData modData)
        {
            var loadAfter = modAbout.LoadAfter;
            // Debug.Log( "Mods to load after: " + loadAfter.Join((mod) => mod.ToString(), ","));
            if (!loadAfter.TrueForAll(afterMod =>
                !availableMods.Any(availableMod => availableMod.IsSame(afterMod.Version, afterMod.Id)) || loadedMods.Any(loadedMod => loadedMod.IsSame(afterMod.Version, afterMod.Id))))
            {
                //not all required dependencies have been listed before
                loadAfter.FindAll(
                        afterMod => availableMods.Any(availableMod => availableMod.IsSame(afterMod.Version, afterMod.Id)) &&
                                    !loadedMods.Any(loadedMod => loadedMod.IsSame(afterMod.Version, afterMod.Id)))
                    .ForEach(
                        afterMod =>
                        {
                            var mod = WorkshopMenu.ModsConfig.Mods.Find(modx =>
                            {
                                if (afterMod.Id == 1UL)
                                {
                                    return modx.IsCore;
                                }

                                if (!File.Exists(modx.AboutXmlPath))
                                {
                                    //skip for missing mods
                                    return false;
                                }

                                var aboutDatax = XmlSerialization.Deserialize<CustomModAbout>(modx.AboutXmlPath, "ModMetadata");
                                return afterMod.IsSame(aboutDatax.Version, aboutDatax.WorkshopHandle);
                            });
                            while (WorkshopMenu.ModsConfig.Mods.IndexOf(mod) < WorkshopMenu.ModsConfig.Mods.IndexOf(modData))
                            {
                                WorkshopMenu.ModsConfig.MoveModDown(mod);
                            }
                        });
                validationResult.ReorderCount[modAbout.WorkshopHandle] = validationResult.ReorderCount.GetValueOrDefault(modAbout.WorkshopHandle, 0) + 1;
                validationResult.Retry = true;
                validationResult.NeedSave = true;
                return true;
            }

            return false;
        }

        private static void validateDependencies(List<ModVersion> availableMods, CustomModAbout modAbout)
        {
            var dependencies = modAbout.Dependencies;
            if (!dependencies.TrueForAll(modVersion => availableMods.Any(availableMod => availableMod.IsSame(modVersion.Version, modVersion.Id))))
            {
                //error - not all required dependencies are available
                throw new MissingDependencyException("Mod loading failed due to missing dependency: Mod " + modAbout.Name + " requires the following missing mods with workshop handles [" +
                                                     dependencies.FindAll(modVersion => !availableMods.Any(availableMod => availableMod.IsSame(modVersion.Version, modVersion.Id)))
                                                         .Join((handle) => handle.ToString(), ",") + "]",
                    dependencies.FindAll(modVersion => !availableMods.Any(availableMod => availableMod.IsSame(modVersion.Version, modVersion.Id))).ToList()
                );
            }
        }

        private static void SaveModConfig()
        {
            if (WorkshopMenu.ModsConfig == null || WorkshopMenu.ModsConfig.SaveXml<ModConfig>("modconfig.xml"))
                return;
            Debug.LogError((object) "Error saving modconfig.xml");
        }

        private static List<ModVersion> listAvailableMods()
        {
            List<ModData> mods = WorkshopMenu.ModsConfig.Mods;
            List<ModVersion> available = new List<ModVersion>();
            available.Add(new ModVersion(VersionHelper.GameVersion(), 1UL)); //core mod
            for (int index = mods.Count - 1; index >= 0; --index)
            {
                ModData modData = mods[index];
                if (File.Exists(modData.AboutXmlPath))
                {
                    Debug.Log("Loading about data for: " + modData.LocalPath + " - " + modData.AboutXmlPath);
                    var aboutData = XmlSerialization.Deserialize<CustomModAbout>(modData.AboutXmlPath, "ModMetadata");
                    //LogMod(aboutData);
                    if( aboutData.Version != null)//version is a string and can be null
                    {
                        available.Add(new ModVersion(aboutData.Version, aboutData.WorkshopHandle));
                    }
                    else
                    {
                        available.Add(new ModVersion(aboutData.WorkshopHandle));
                    }
                }
            }

            return available;
        }

        private static void LogMod(CustomModAbout aboutData)
        {
            Debug.Log("handle: " + aboutData.WorkshopHandle);
            Debug.Log("name: " + aboutData.Name);
            Debug.Log("author: " + aboutData.Author);
            Debug.Log("version: " + aboutData.Version);
            Debug.Log("description: " + aboutData.Description);
            Debug.Log("in game description: " + aboutData.InGameDescription.Value);
            Debug.Log("change log: " + aboutData.ChangeLog);
            Debug.Log("tags: " + aboutData.Tags.Join((tag) => tag, ","));
            Debug.Log("dependencies: " + aboutData.Dependencies.Join((dep) => dep.ToString(), ","));
            Debug.Log("load before: " + aboutData.LoadBefore.Join((dep) => dep.ToString(), ","));
            Debug.Log("load after: " + aboutData.LoadAfter.Join((dep) => dep.ToString(), ","));
            
        }

        private async void AddLocalAndWorkshopItems()
        {
            Debug.Log("StationeersMods: Start adding local and workshop mods");

            if (string.IsNullOrEmpty(Settings.CurrentData.SavePath))
                Settings.CurrentData.SavePath = StationSaveUtils.DefaultSavePath;
            var steamTransport = new SteamTransport();
            try
            {
                if (!SteamClient.IsValid)
                {
                    SteamClient.Init(544550U);
                }
            }
            catch (Exception)
            {
                Debug.Log("Steam client init failed. Workshop mods won't load.");
            }

            try
            {
                var items = await GetLocalAndWorkshopItems(SteamTransport.WorkshopType.Mod);

                List<ulong> addedHandles = new List<ulong>();
                foreach (SteamTransport.ItemWrapper localAndWorkshopItem in items.OrderBy(wrapper => wrapper.IsLocal()).Reverse())
                {
                    SteamTransport.ItemWrapper item = localAndWorkshopItem;
                    ulong workshopHandle = 0;
                    if(File.Exists(item.DirectoryPath + "\\About\\About.xml"))
                    {
                        var modAbout = XmlSerialization.Deserialize<ModAbout>(item.DirectoryPath + "\\About\\About.xml", "ModMetadata");
                        workshopHandle = modAbout.WorkshopHandle;
                    }
                    if (!addedHandles.Contains(workshopHandle))
                    {
                        if(workshopHandle != 0) // 0 indicates no workshophandle
                            addedHandles.Add(workshopHandle);

                        if (File.Exists(item.DirectoryPath + "\\About\\stationeersmods"))
                        {
                            Debug.Log("StationeersMods mod found in directory: " + item.DirectoryPath + ". name: " +
                                      item.Title);
                            AddSearchDirectory(item.DirectoryPath);
                        }

                        if (File.Exists(item.DirectoryPath + "\\About\\bepinex"))
                        {
                            Debug.Log("BepInEx mod found in directory: " + item.DirectoryPath + ". name: " +
                                      item.Title);
                            AddSearchDirectory(item.DirectoryPath);
                        }
                    }
                    else
                    {
                        Debug.Log("Skipping duplicate mod: " + item.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            SteamClient.Shutdown();
        }

        public static async UniTask<IReadOnlyList<SteamTransport.ItemWrapper>> GetLocalAndWorkshopItems(
            SteamTransport.WorkshopType type)
        {
            List<SteamTransport.ItemWrapper> items;

            DirectoryInfo localDirInfo = type.GetLocalDirInfo();
            string fileName = type.GetLocalFileName();
            items = new List<SteamTransport.ItemWrapper>();
            if (localDirInfo.Exists)
            {
                items.AddRange(
                    localDirInfo.GetDirectories("*", SearchOption.AllDirectories)
                        .SelectMany((d => (IEnumerable<FileInfo>) d.GetFiles()))
                        .Where(f => f.Name == fileName)
                        .Select(f => SteamTransport.ItemWrapper.WrapLocalItem(f, type))
                );
            }

            if (SteamClient.IsValid)
            {
                var workshopItems = await Workshop_QueryItemsAsync(type);
                items.AddRange(workshopItems);
            }

            items.Sort(((b, a) =>
                a.LastWriteTime.CompareTo(b.LastWriteTime)));
            return items;
        }

        public static async UniTask<IEnumerable<SteamTransport.ItemWrapper>> Workshop_QueryItemsAsync(
            SteamTransport.WorkshopType itemType,
            uint page = 1)
        {
            List<Item> entries;
            IEnumerable<SteamTransport.ItemWrapper> result;

            if (!SteamClient.IsValid)
            {
                result = Enumerable.Empty<SteamTransport.ItemWrapper>();
            }
            else
            {
                entries = new List<Item>();
                try
                {
                    Query query = Query.Items;
                    query = query.WithTag(GetTagFromType(itemType));
                    var resultPage = await query.AllowCachedResponse(0).WhereUserSubscribed()
                        .GetPageAsync((int) page).AsUniTask();

                    entries = (resultPage.HasValue
                        ? resultPage.GetValueOrDefault().Entries.ToList()
                        : null) ?? new List<Item>();

                    var test = await UniTask.WhenAll(
                        entries
                            .Where(x => x.NeedsUpdate || !Directory.Exists(x.Directory))
                            .Select(x => SteamUGC.DownloadAsync(x.Id).AsUniTask())
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                string fileName = itemType.GetLocalFileName();
                if (itemType == SteamTransport.WorkshopType.Mod)
                {
                    fileName = "About\\" + fileName;
                }

                result = entries.Select(x => SteamTransport.ItemWrapper.WrapWorkshopItem(x, fileName));
            }

            return result;
        }

        private static string GetTagFromType(SteamTransport.WorkshopType type)
        {
            switch (type)
            {
                case SteamTransport.WorkshopType.World:
                    return "World Save";
                case SteamTransport.WorkshopType.Mod:
                    return "Mod";
                case SteamTransport.WorkshopType.ICCode:
                    return "IC Code";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnModLoaded(Resource mod)
        {
            if (ModLoaded != null)
            {
                ModLoaded.Invoke((AssemblyMod) mod);
            }
        }

        private void OnModUnloaded(Resource mod)
        {
            var _mod = (AssemblyMod) mod;

            if (ModUnloaded != null)
                ModUnloaded.Invoke(_mod);

            if (queuedRefreshMods.Contains(_mod))
            {
                queuedRefreshMods.Remove(_mod);
                if (_mod is Mod modm)
                    OnModChanged(modm.modBaseDirectory, modm.modInfo.path);
            }
        }

        private void OnModLoadCancelled(Resource mod)
        {
            if (ModLoadCancelled != null)
                ModLoadCancelled.Invoke((AssemblyMod) mod);
        }

        private void OnSceneLoaded(ModScene scene)
        {
            if (SceneLoaded != null)
                SceneLoaded.Invoke(scene);
        }

        private void OnSceneUnloaded(ModScene scene)
        {
            if (SceneUnloaded != null)
                SceneUnloaded.Invoke(scene);
        }

        private void OnSceneLoadCancelled(ModScene scene)
        {
            if (SceneLoadCancelled != null) SceneLoadCancelled.Invoke(scene);
        }

        /// <summary>
        ///     Add a directory that will be searched for Mods
        /// </summary>
        /// <param name="path">The path of the search directory.</param>
        public void AddSearchDirectory(string path)
        {
            if (searchDirectories.Any(s => s.BasePath.NormalizedPath() == path.NormalizedPath()))
                return;

            var directory = new ModSearchDirectory(path);
            Debug.Log("Added mod path: " + path);
            directory.ModFound += OnModFound;
            directory.ModRemoved += OnModRemoved;
            directory.ModChanged += OnModChanged;

            searchDirectories.Add(directory);

            directory.Refresh();
        }

        /// <summary>
        ///     Remove a directory that will be searched for mods
        /// </summary>
        /// <param name="path">The path of the search directory.</param>
        public void RemoveSearchDirectory(string path)
        {
            var directory = searchDirectories.Find(s => s.BasePath.NormalizedPath() == path.NormalizedPath());

            if (directory == null)
                return;

            directory.Dispose();

            searchDirectories.Remove(directory);
        }

        /// <summary>
        ///     Refresh all search directories and update any new, changed or removed Mods.
        /// </summary>
        public void RefreshSearchDirectories()
        {
            foreach (var searchDirectory in searchDirectories)
                searchDirectory.Refresh();
        }

        private IEnumerator AutoRefreshSearchDirectories()
        {
            while (true)
            {
                RefreshSearchDirectories();
                yield return wait;
            }
        }

        private void OnModFound(string modBasePath, string path)
        {
            //AddMod(path);
            ThreadPool.QueueUserWorkItem(o => AddMod(modBasePath,path));
        }

        private void OnModRemoved(string modBasePath, string path)
        {
            RemoveMod(path);
        }

        private void OnModChanged(string modBasePath, string path)
        {
            RefreshMod(modBasePath, path);
        }

        private void RefreshMod(string modBasePath, string path)
        {
            LogUtility.LogInfo("Mod refreshing: " + path);
            OnModRemoved(modBasePath, path);
            OnModFound(modBasePath, path);
        }

        private void QueueModRefresh(AssemblyMod mod)
        {
            if (queuedRefreshMods.Contains(mod))
                return;

            LogUtility.LogInfo("Mod refresh queued: " + mod.name);
            mod.SetInvalid();
            queuedRefreshMods.Add(mod);
        }

        private void AddMod(string modBasePath, string path)
        {
            lock (_lock)
            {
                if (_modPaths.ContainsKey(path))
                    return;
            }

            Debug.Log("creating new Mod from " + path);
            var mod = (path.EndsWith(".info")) ? new Mod(modBasePath, path) : new AssemblyMod(modBasePath, path);

            lock (_lock)
            {
                _modPaths.Add(path, mod);
            }

            dispatcher.Enqueue(() => AddMod(mod), true);
        }

        private void AddMod(AssemblyMod mod)
        {
            try
            {
                mod.Loaded += OnModLoaded;
                mod.Unloaded += OnModUnloaded;
                mod.LoadCancelled += OnModLoadCancelled;
                if (mod is Mod modm)
                {
                    LogUtility.LogInfo("Mod is Mod: " + mod.name);
                    modm.SceneLoaded += OnSceneLoaded;
                    modm.SceneUnloaded += OnSceneUnloaded;
                    modm.SceneLoadCancelled += OnSceneLoadCancelled;
                }
                else
                {
                    LogUtility.LogInfo("Mod is AssemblyMod: " + mod.name);
                }

                mod.UpdateConflicts(_mods);
                foreach (var other in _mods)
                    other.UpdateConflicts(mod);

                LogUtility.LogInfo("Mod found: " + mod.name + " - " + mod.contentType);
                _mods.Add(mod);

                ModFound?.Invoke(mod);
                ModsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void RemoveMod(string path)
        {
            lock (_lock)
            {
                AssemblyMod mod;

                if (_modPaths.TryGetValue(path, out mod))
                {
                    if (mod.loadState != ResourceLoadState.Unloaded)
                    {
                        dispatcher.Enqueue(() => QueueModRefresh(mod));
                        return;
                    }

                    _modPaths.Remove(path);

                    dispatcher.Enqueue(() => RemoveMod(mod), true);
                }
            }
        }

        private void RemoveMod(AssemblyMod mod)
        {
            mod.Loaded -= OnModLoaded;
            mod.Unloaded -= OnModUnloaded;
            mod.LoadCancelled -= OnModLoadCancelled;

            if (mod is Mod modm)
            {
                modm.SceneLoaded -= OnSceneLoaded;
                modm.SceneUnloaded -= OnSceneUnloaded;
                modm.SceneLoadCancelled -= OnSceneLoadCancelled;
            }

            mod.SetInvalid();

            foreach (var other in _mods)
                other.UpdateConflicts(mod);

            LogUtility.LogInfo("Mod removed: " + mod.name);
            _mods.Remove(mod);

            ModRemoved?.Invoke(mod);
            ModsChanged?.Invoke();
        }

        protected override void OnDestroy()
        {
            queuedRefreshMods.Clear();

            foreach (var mod in _mods)
            {
                mod.Unload();
                mod.SetInvalid();
            }

            foreach (var searchDirectory in searchDirectories) searchDirectory.Dispose();

            base.OnDestroy();
        }
    }
}