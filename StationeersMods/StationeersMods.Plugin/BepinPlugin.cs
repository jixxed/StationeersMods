using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using StationeersMods.Interface;
using StationeersMods.Shared;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.UI;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using StationeersMods.Plugin.Configuration;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.IO.Compression;

namespace StationeersMods.Plugin
{
    //This is an example plugin that can be put in BepInEx/plugins/StationeersMods/StationeesMods.Plugin.dll to test out.
    //Lets examine what each line of code is for:

    //This attribute is required, and lists metadata for your plugin.
    //The GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config). I like to use the java package notation, which is "com.[your name here].[your plugin name here]"
    //The name is the name of the plugin that's displayed on load, and the version number just specifies what version the plugin is.
    [BepInPlugin("nl.jixxed.stationeers", "StationeersModsPlugin", "1.0")]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class BepinPlugin : BaseUnityPlugin
    {
        
        //The Awake() method is run at the very start when the game is initialized.
        public static Dictionary<string, ConfigFile> ConfigFiles = new Dictionary<string, ConfigFile>();
        public static Dictionary<string, ModVersionInfo> ModVersionInfos = new Dictionary<string, ModVersionInfo>();

        public void Awake()
        {
            DontDestroyOnLoad(this);
            
            // Cleanup temporary and backup DLL files from previous updates
            CleanupUpdateFiles();

            bool isPatched = PatchBepInEx();
            SceneManager.sceneLoaded += (Scene, LoadSceneMode) =>
            {
                Debug.Log($"Loaded scene: {Scene.name}");
                if (Scene.name.ToLower().Equals("splash"))
                {
                    Debug.Log("Patching WorkshopManager using Harmony...");
                    try
                    {
                        Harmony harmony = new Harmony("StationeersMods");
                        var refreshButtonsMethod = typeof(WorkshopMenu).GetMethod("RefreshButtons",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var refreshButtonsPostfix = typeof(WorkshopMenuPatch).GetMethod("RefreshButtonsPostfix");
                        harmony.Patch(refreshButtonsMethod, postfix: new HarmonyMethod(refreshButtonsPostfix));

                        var selectModMethod = typeof(WorkshopMenu).GetMethod("SelectMod",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var selectModPostfix = typeof(WorkshopMenuPatch).GetMethod("SelectModPostfix");
                        harmony.Patch(selectModMethod, postfix: new HarmonyMethod(selectModPostfix));

                        var deleteFileAsyncMethod = typeof(SteamUGC).GetMethod(
                            "DeleteFileAsync",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                            null,
                            new Type[] { typeof(PublishedFileId) },
                            null);

                        var deleteFileAsyncMethodPrefix = typeof(SteamUGCPatch).GetMethod("DeleteFileAsyncPrefix");
                        harmony.Patch(deleteFileAsyncMethod, prefix: new HarmonyMethod(deleteFileAsyncMethodPrefix));

                        //patch LoadDataFilesAtPath
                        var loadMethod = typeof(WorldManager).GetMethod("LoadDataFilesAtPath",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        var loadPostfix = typeof(WorldManagerPatch).GetMethod("LoadDataFilesAtPathPostfix");
                        harmony.Patch(loadMethod, postfix: new HarmonyMethod(loadPostfix));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Failed to initialize workshop publish patch.");
                        Debug.LogException(ex);
                    }

                    Init();
                }

                if (Scene.name.ToLower().Equals("base"))
                {
                    if (isPatched)
                    {
                        patched();
                    }
                    else
                    {
                        StartCoroutine(versionCheck());
                    }
                }
            };
            WorkshopModListItem.Selected += SelectMod;
        }

        private void patched()
        {
            AlertPanel.Instance.ShowAlert("BepInEx config has been patched and requires a game restart NOW!", AlertState.Alert);
        }

        private void SelectMod(WorkshopModListItem modItem)
        {
            Debug.Log("Mod Path:" + modItem.Data.DirectoryPath);
            try
            {
                ConfigFile configFile;
                ConfigFiles.TryGetValue(modItem.Data.DirectoryPath, out configFile);
                if (configFile != null)
                {
                    Debug.Log("Configfile found!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error selecting mod: {e.Message}");
                AlertPanel.Instance.ShowAlert("BepInEx config has been patched and requires a game restart NOW!", AlertState.Alert);
            }
        }

        public static ModBehaviour[] FindPlugins()
        {
            // Search for instances of BaseUnityPlugin to also find dynamically loaded plugins.
            // Have to use FindObjectsOfType(Type) instead of FindObjectsOfType<T> because the latter is not available in some older unity versions.
            // Still look inside Chainloader.PluginInfos in case the BepInEx_Manager GameObject uses HideFlags.HideAndDontSave, which hides it from Object.Find methods.
            return UnityEngine.Object.FindObjectsOfType<ModBehaviour>(true).ToArray();
        }

        public IEnumerator versionCheck()
        {
            Log("Checking for StationeersMods version...");

            // Perform simple web request to get the latest version from github
            using (var webRequest = UnityWebRequest.Get("https://api.github.com/repos/jixxed/StationeersMods/releases/latest"))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Regex rx = new Regex(@"""tag_name""\:\s""([V\d.]*)""",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    MatchCollection matches = rx.Matches(webRequest.downloadHandler.text);
                    if (matches.Count > 0)
                    {
                        var @group = matches[0].Groups[1];
                        string latestVersion = @group.Value;
                        System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                        string currentVersion = "V" + fvi.FileVersion;
                        Log($"Latest StationeersMods version is {latestVersion}. Installed {currentVersion}");

                        // If the current version is the same as the latest one, just exit the coroutine.
                        Version current = new Version(fvi.FileVersion);
                        Version latest = new Version(latestVersion.TrimStart('V', 'v'));

                        if (current >= latest)
                        {
                            yield break;
                        }

                        Log("New version of StationeersMods is available!");
                        AlertPanel.Instance.ShowAlert($"New version of StationeersMods: ({latestVersion}) is available!", AlertState.Alert);

                        // Self-update logic
                        yield return StartCoroutine(SelfUpdateMod(latestVersion));
                    }
                    else
                    {
                        Debug.LogError(
                            $"Failed to request latest StationeersMods version. Result: {webRequest.result} Error: '\"{webRequest.error}\""
                        );
                        Debug.LogError("Failed to check latest StationeersMods version!\n");
                    }
                }
                else
                {
                    Debug.LogError(
                        $"Failed to request latest StationeersMods version. Result: {webRequest.result} Error: '\"{webRequest.error}\""
                    );
                    Debug.LogError("Failed to check latest StationeersMods version!\n");

                    // Wait for the alert window to close
                    while (AlertPanel.Instance.AlertWindow.activeInHierarchy)
                        yield return null;
                }
            }
        }

        private IEnumerator SelfUpdateMod(string newVersion)
        {
            Log($"Attempting to download and update to version {newVersion}");

            // Fetch the latest release details
            UnityWebRequest webRequest = UnityWebRequest.Get("https://api.github.com/repos/jixxed/StationeersMods/releases/latest");
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch release details. Error: {webRequest.error}");
                AlertPanel.Instance.ShowAlert("Failed to check for updates.", AlertState.Alert);
                yield break;
            }

            // Find the ZIP asset in the release
            Regex zipRx = new Regex(@"""browser_download_url""\:\s""([^""]*StationeersMods\.zip)""", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            MatchCollection zipMatches = zipRx.Matches(webRequest.downloadHandler.text);

            if (zipMatches.Count == 0)
            {
                Debug.LogError("Could not find ZIP in the latest release.");
                AlertPanel.Instance.ShowAlert("Could not find update package.", AlertState.Alert);
                yield break;
            }

            string zipDownloadUrl = zipMatches[0].Groups[1].Value;
            
            // Download the ZIP file
            UnityWebRequest zipDownloadRequest = UnityWebRequest.Get(zipDownloadUrl);
            yield return zipDownloadRequest.SendWebRequest();

            if (zipDownloadRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download update ZIP. Error: {zipDownloadRequest.error}");
                AlertPanel.Instance.ShowAlert("Failed to download StationeersMods update.", AlertState.Alert);
                yield break;
            }

            // List of DLLs to update
            string[] dllsToUpdate = new[]
            {
                "StationeersMods.Cecil.dll",
                "StationeersMods.dll",
                "StationeersMods.Interface.dll",
                "StationeersMods.Patcher.dll",
                "StationeersMods.Plugin.dll",
                "StationeersMods.Shared.dll"
            };

            // Create a temporary directory for extraction
            string tempDir = Path.Combine(Path.GetTempPath(), "StationeersMods_Update");
            Directory.CreateDirectory(tempDir);

            // Save the downloaded ZIP
            string tempZipPath = Path.Combine(tempDir, "StationeersMods.zip");
            File.WriteAllBytes(tempZipPath, zipDownloadRequest.downloadHandler.data);

            // Handle DLL updates
            yield return StartCoroutine(UpdateDlls(tempZipPath, dllsToUpdate, newVersion));

            // Clean up temporary files
            try 
            {
                File.Delete(tempZipPath);
                Directory.Delete(tempDir, true);
                AlertPanel.Instance.ShowAlert($"StationeersMods updated to version {newVersion}. Please restart NOW.", AlertState.Alert);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clean up update files: {ex.Message}");
                AlertPanel.Instance.ShowAlert("Update completed, but failed to clean up temporary files.", AlertState.Alert);
            }
        }

        private IEnumerator UpdateDlls(string zipPath, string[] dllsToUpdate, string newVersion)
        {
            // Create lists to track update results outside of try-catch
            var successfulUpdates = new List<string>();
            var failedUpdates = new List<string>();

            // Open the archive before processing
            using (var archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
            {
                // Process each DLL
                foreach (string dllName in dllsToUpdate)
                {
                    // Find the entry in the archive
                    var entry = archive.Entries.FirstOrDefault(e => e.Name == dllName);
                    if (entry == null)
                    {
                        failedUpdates.Add(dllName);
                        yield return null;
                        continue;
                    }

                    // Find the current DLL path
                    string currentDllPath = FindDllPath(dllName);
                    if (string.IsNullOrEmpty(currentDllPath))
                    {
                        failedUpdates.Add(dllName);
                        yield return null;
                        continue;
                    }

                    // Attempt to replace the DLL
                    bool updateSuccessful = ForceDllReplacement(currentDllPath, entry, dllName);

                    // Track the result
                    if (updateSuccessful)
                    {
                        successfulUpdates.Add(dllName);
                        Log($"Successfully updated {dllName} to version {newVersion}");
                    }
                    else
                    {
                        failedUpdates.Add(dllName);
                    }

                    // Yield to prevent freezing
                    yield return null;
                }
            }

            // Show alert if any updates failed
            if (failedUpdates.Any())
            {
                string failedDllList = string.Join(", ", failedUpdates);
                AlertPanel.Instance.ShowAlert($"Some DLLs could not be updated: {failedDllList}. Please manually update or restart.", AlertState.Alert);
            }
        }

        private bool ForceDllReplacement(string currentDllPath, System.IO.Compression.ZipArchiveEntry entry, string dllName)
        {
            string backupDllPath = currentDllPath + ".bak";
            string tempDllPath = currentDllPath + ".temp";

            // Attempt to create a backup
            try 
            {
                if (File.Exists(backupDllPath))
                {
                    File.Delete(backupDllPath);
                }
                File.Copy(currentDllPath, backupDllPath, true);
            }
            catch 
            {
                // Silently ignore backup failures
            }

            // Force DLL replacement using multiple strategies
            try 
            {
                // Strategy 1: Extract directly to the file
                using (var fileStream = new FileStream(currentDllPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    entry.Open().CopyTo(fileStream);
                }
                return true;
            }
            catch 
            {
                try 
                {
                    // Strategy 2: Rename existing DLL and replace
                    File.Move(currentDllPath, tempDllPath);
                    
                    try 
                    {
                        using (var fileStream = new FileStream(currentDllPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            entry.Open().CopyTo(fileStream);
                        }
                        
                        // Successfully replaced, delete temp file
                        File.Delete(tempDllPath);
                        return true;
                    }
                    catch
                    {
                        // Restore original DLL if replacement fails
                        try 
                        {
                            File.Move(tempDllPath, currentDllPath);
                        }
                        catch 
                        {
                            // Silently ignore restoration failures
                        }
                        return false;
                    }
                }
                catch 
                {
                    // Silently ignore rename failures
                    return false;
                }
            }
        }

        private string FindDllPath(string dllName)
        {
            // Try to find the DLL in the current directory or BepInEx plugin directories
            string[] searchPaths = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "core")
            };

            foreach (string searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    string dllPath = Directory.GetFiles(searchPath, dllName, SearchOption.AllDirectories)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(dllPath))
                    {
                        return dllPath;
                    }
                }
            }

            // Fallback to the path of the currently executing assembly if no other path is found
            if (dllName == "StationeersMods.Plugin.dll")
            {
                return Assembly.GetExecutingAssembly().Location;
            }

            return null;
        }

        private void Log(string message)
        {
            Debug.Log($"[StationeersMods] {message}");
        }

        public void Init()
        {
            var mm = ModManager.instance;

            mm.RefreshSearchDirectories();

            mm.ModsChanged += () =>
            {
                Debug.Log("Mods changed.");
                foreach (var mod in mm.mods)
                    Debug.Log(
                        $"{mod.name}: {mod.assemblyNames.Count} assemblies, {(mod is Mod m ? m.contentHandler.prefabs.Count : 0)} prefabs, isValid={mod.isValid}, state {mod.loadState}");
            };

            mm.ModFound += mod =>
            {
                Debug.Log(
                    $"Mod found: {mod.name} {mod.assemblyNames.Count} assemblies, {(mod is Mod m ? m.contentHandler.prefabs.Count : 0)} prefabs, isValid={mod.isValid}, state {mod.loadState}");
                if (mod is Mod modm && modm.assetPaths != null)
                    foreach (var assetPath in modm.assetPaths)
                        Debug.Log($" - {assetPath}");

                mod.Load();

                mod.Loaded += resource => { Debug.Log($"Resource loaded? {resource.loadState} - {resource.name}"); };

                Debug.Log(
                    $"Mod loaded?: {mod.name} {mod.assemblyNames.Count} assemblies, {(mod is Mod m2 ? m2.contentHandler.prefabs.Count : 0)} prefabs, isValid={mod.isValid}, state {mod.loadState}");
            };

            mm.ModLoaded += assmod =>
            {
                Debug.Log($"{assmod.name} loaded. Looking for ExportSettings.");
                if (assmod is Mod mod)
                {
                    var settings = mod.GetAsset<ExportSettings>("ExportSettings");
                    if (settings != null)
                    {
                        Debug.Log($"{assmod.name} - prefab {settings.StartupPrefab} - class {settings.StartupClass}");
                        if (settings.StartupPrefab != null)
                        {
                            Debug.Log("(Settings)StationeersMods starting with prefab: " + settings.StartupPrefab);
                            // We want to defer this logic to after the gameprefabs have been loaded.
                            var gobj = Instantiate(settings.StartupPrefab);
                            Object.DontDestroyOnLoad(gobj);
                            gobj.GetComponents<ModBehaviour>().ToList().ForEach(i =>
                            {
                                ConfigFiles.Add(mod.modBaseDirectory, i.Config);
                                ModVersionInfos.Add(mod.modBaseDirectory, new ModVersionInfo(mod.name, mod.modInfo.version));
                                i.contentHandler = mod.contentHandler;
                                i.OnLoaded(mod.contentHandler);
                            });
                        }

                        if (settings.StartupClass != null)
                        {
                            Debug.Log("(Settings)StationeersMods starting with class: " + settings.StartupClass);
                            GameObject gameObj = new GameObject();
                            System.Type scriptType = System.Type.GetType(settings.StartupClass);
                            if (scriptType == null)
                            {
                                Debug.Log("(Settings)starting class not available, looking through assemblies");
                                foreach (Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    var tempType = a.GetType(settings.StartupClass);
                                    if (tempType != null)
                                    {
                                        scriptType = tempType;
                                    }
                                }
                            }

                            if (scriptType != null)
                            {
                                Debug.Log("(Settings)StationeersMods found class: " + settings.StartupClass);
                                gameObj.AddComponent(scriptType);
                                gameObj.GetComponents<ModBehaviour>().ToList().ForEach(i =>
                                {
                                    ConfigFiles.Add(mod.modBaseDirectory, i.Config);
                                    ModVersionInfos.Add(mod.modBaseDirectory, new ModVersionInfo(mod.name, mod.modInfo.version));
                                    i.contentHandler = mod.contentHandler;
                                    i.OnLoaded(mod.contentHandler);
                                });
                            }
                        }
                    }
                    else
                    {
                        var type = typeof(ModBehaviour);
                        assmod.assemblyFiles.ForEach(assfile => Debug.Log(assfile));
                        if (assmod.assemblyFiles.Any())
                        {
                            assmod.assemblyFiles.ForEach(path =>
                            {
                                Debug.Log("Load assembly from: " + path);
                                Assembly modAssembly = Assembly.LoadFrom(path);
                                var types = modAssembly.GetTypes()
                                    .Where(p => type.IsAssignableFrom(p));
                                foreach (Type t in types)
                                {
                                    GameObject gameObj = new GameObject();
                                    Debug.Log("(Settings)StationeersMods found class: " + t.Name);
                                    gameObj.AddComponent(t);
                                    gameObj.GetComponents<ModBehaviour>().ToList().ForEach(i =>
                                    {
                                        ConfigFiles.Add(mod.modBaseDirectory, i.Config);
                                        ModVersionInfos.Add(mod.modBaseDirectory, new ModVersionInfo(mod.name, mod.modInfo.version));
                                        i.contentHandler = ((Mod)assmod).contentHandler;
                                        i.OnLoaded(((Mod)assmod).contentHandler);
                                    });
                                }
                            });
                        }
                    }
                }
                else
                {
                    var type = typeof(ModBehaviour);
                    if (assmod.assemblyFiles.Any())
                    {
                        assmod.assemblyFiles.ForEach(path =>
                        {
                            Debug.Log("Load assembly from: " + path);
                            Assembly modAssembly = Assembly.LoadFrom(path);
                            var types = modAssembly.GetTypes()
                                .Where(p => type.IsAssignableFrom(p));
                            foreach (Type t in types)
                            {
                                GameObject gameObj = new GameObject();
                                Debug.Log("StationeersMods found class: " + t.Name);
                                gameObj.AddComponent(t);
                                gameObj.GetComponents<ModBehaviour>().ToList().ForEach(i =>
                                {
                                    ConfigFiles.Add(assmod.modBaseDirectory, i.Config);
                                    ModVersionInfos.Add(assmod.modBaseDirectory, new ModVersionInfo(assmod.name, ModBehaviour.GetMetadata(i).Version.ToString()));
                                    i.contentHandler = ((Mod)assmod).contentHandler;
                                    i.OnLoaded(((Mod)assmod).contentHandler);
                                });
                            }

                            //bepinex loading attempt
                            if (!types.Any())
                            {
                                var type = typeof(BaseUnityPlugin);
                                var types2 = modAssembly.GetTypes()
                                    .Where(p => type.IsAssignableFrom(p));
                                foreach (Type t in types2)
                                {
                                    GameObject gameObj = new GameObject();
                                    Debug.Log("StationeersMods found BepinEx class: " + t.Name);
                                    gameObj.AddComponent(t);
                                    var baseUnityPlugin = gameObj.GetComponent<BaseUnityPlugin>();
                                    ConfigFiles.Add(assmod.modBaseDirectory, baseUnityPlugin.Config);
                                    ModVersionInfos.Add(assmod.modBaseDirectory, new ModVersionInfo(assmod.name, baseUnityPlugin.Info.Metadata.Version.ToString()));
                                }
                            }
                        });
                    }
                    else
                    {
                        Debug.LogError("Couldn't find ExportSettings in mod assetbundle or Modbehaviour class");
                        return;
                    }
                }
            };

            mm.ModLoadCancelled += mod => { Debug.LogWarning($"Mod loading canceled: {mod.name}"); };

            mm.ModUnloaded += mod => { Debug.Log($"Mod UNLOADED: {mod.name}"); };
        }

        public void OnGUI()
        {
            WorkshopMenuPatch.OnGUI();
        }

       private Boolean PatchBepInEx()
        {
            // Path to the BepInEx.cfg file
            string configFilePath = Path.Combine(Paths.ConfigPath, "BepInEx.cfg");
            bool needsPatching = false;
            // Ensure the config file exists before modifying it
            if (File.Exists(configFilePath))
            {
                // Read all lines from the configuration file
                var configLines = File.ReadAllLines(configFilePath);

                // Look for the HideManagerGameObject setting
                bool settingFound = false;
                for (int i = 0; i < configLines.Length; i++)
                {
                    if (configLines[i].StartsWith("HideManagerGameObject", System.StringComparison.OrdinalIgnoreCase))
                    {
                        needsPatching = configLines[i].StartsWith("HideManagerGameObject = false", System.StringComparison.OrdinalIgnoreCase);
                        configLines[i] = "HideManagerGameObject = true";
                        settingFound = true;
                        break;
                    }
                }

                // If the setting doesn't exist, add it
                if (!settingFound)
                {
                    var configList = new System.Collections.Generic.List<string>(configLines);
                    configList.Add("[ChainLoader]");
                    configList.Add("HideManagerGameObject = true");
                    configLines = configList.ToArray();
                    needsPatching = true;
                }

                if (needsPatching)
                {
                    // Write the updated lines back to the file
                    File.WriteAllLines(configFilePath, configLines);
                    Logger.LogInfo("Successfully updated HideManagerGameObject setting.");
                }
                else
                {
                    Logger.LogInfo("BepInEx.cfg did not require patching.");
                }
            }
            else
            {
                Logger.LogError("BepInEx.cfg file not found.");
            }

            return needsPatching;
        }

        private void CleanupUpdateFiles()
        {
            // List of DLLs to clean up
            string[] dllsToCleanup = new[]
            {
                "StationeersMods.Cecil.dll",
                "StationeersMods.dll",
                "StationeersMods.Interface.dll",
                "StationeersMods.Patcher.dll",
                "StationeersMods.Plugin.dll",
                "StationeersMods.Shared.dll"
            };

            // Search paths for cleanup
            string[] searchPaths = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "core")
            };

            // Cleanup temp and backup files
            foreach (string searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath)) continue;

                foreach (string dllName in dllsToCleanup)
                {
                    try 
                    {
                        // Find all matching DLLs
                        string[] dllPaths = Directory.GetFiles(searchPath, dllName, SearchOption.AllDirectories);
                        
                        foreach (string dllPath in dllPaths)
                        {
                            // Remove .bak files
                            string backupPath = dllPath + ".bak";
                            if (File.Exists(backupPath))
                            {
                                File.Delete(backupPath);
                            }

                            // Remove .temp files
                            string tempPath = dllPath + ".temp";
                            if (File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                        }
                    }
                    catch 
                    {
                        // Silently ignore any cleanup errors
                    }
                }
            }
        }
    }
}