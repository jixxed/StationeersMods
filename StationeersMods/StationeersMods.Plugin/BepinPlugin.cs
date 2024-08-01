using System;
using System.Collections;
using BepInEx;
using StationeersMods.Interface;
using StationeersMods.Shared;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.UI;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        public void Awake()
        {
            DontDestroyOnLoad(this);
            SceneManager.sceneLoaded += (Scene, LoadSceneMode) =>
            {
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
                            [typeof(PublishedFileId)], 
                            null);
                        

                        var deleteFileAsyncMethodPrefix = typeof(SteamUGCPatch).GetMethod("DeleteFileAsyncPrefix");
                        harmony.Patch(deleteFileAsyncMethod, prefix: new HarmonyMethod(deleteFileAsyncMethodPrefix));

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
                    StartCoroutine(versionCheck());
                }
            };
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
                        string currentVersion = @group.Value;
                        System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                        string version = "V" + fvi.FileVersion;
                        Log($"Latest StationeersMods version is {currentVersion}. Installed {version}");

                        // If the current version is the same as the latest one, just exit the coroutine.
                        if (version.ToLower().Equals(currentVersion.ToLower()))
                            yield break;

                        Log("New version of StationeersMods is available!");
                        AlertPanel.Instance.ShowAlert($"New version of StationeersMods: ({currentVersion}) is available!", AlertState.Alert);
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
                                        i.contentHandler = ((Mod) assmod).contentHandler;
                                        i.OnLoaded(((Mod) assmod).contentHandler);
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
                                    i.contentHandler = ((Mod) assmod).contentHandler;
                                    i.OnLoaded(((Mod) assmod).contentHandler);
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
                                    // gameObj.GetComponents<BaseUnityPlugin>().ToList().ForEach(i =>
                                    // {
                                    //     i.contentHandler = mod.contentHandler;
                                    //     i.OnLoaded(mod.contentHandler);
                                    // });
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
    }
}