using BepInEx;
using StationeersMods.Interface;
using StationeersMods.Shared;
using System.Linq;
using System.Reflection;
using UnityEngine;
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
                    Init();
                }
            };
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
                        $"{mod.name}: {mod.assemblyNames.Count} assemblies, {mod.contentHandler.prefabs.Count} prefabs, isValid={mod.isValid}, state {mod.loadState}");
            };

            mm.ModFound += mod =>
            {
                Debug.Log(
                    $"Mod found: {mod.name} {mod.assemblyNames.Count} assemblies, {mod.contentHandler.prefabs.Count} prefabs, isValid={mod.isValid}, state {mod.loadState}");

                foreach (var assetPath in mod.assetPaths) Debug.Log($" - {assetPath}");

                mod.Load();

                mod.Loaded += resource => { Debug.Log($"Resource loaded? {resource.loadState} - {resource.name}"); };

                Debug.Log(
                    $"Mod loaded?: {mod.name} {mod.assemblyNames.Count} assemblies, {mod.contentHandler.prefabs.Count} prefabs, isValid={mod.isValid}, state {mod.loadState}");
            };

            mm.ModLoaded += mod =>
            {
                Debug.Log($"{mod.name} loaded. Looking for ExportSettings.");
                var settings = mod.GetAsset<ExportSettings>("ExportSettings");

                if (settings == null)
                {
                    Debug.LogError("Couldn't find ExportSettings in mod assetbundle.");
                    return;
                }


                if (settings.StartupPrefab != null)
                {
                    Debug.Log("StationeersMods starting with prefab: " + settings.StartupPrefab);
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
                    Debug.Log("StationeersMods starting with class: " + settings.StartupClass);
                    GameObject gameObj = new GameObject();
                    System.Type scriptType = System.Type.GetType(settings.StartupClass);
                    if (scriptType == null)
                    {
                        Debug.Log("starting class not available, looking through assemblies");
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
                        Debug.Log("StationeersMods found class: " + settings.StartupClass);
                        gameObj.AddComponent(scriptType);
                        gameObj.GetComponents<ModBehaviour>().ToList().ForEach(i =>
                        {
                            i.contentHandler = mod.contentHandler;
                            i.OnLoaded(mod.contentHandler);
                        });
                    }
                }
            };

            mm.ModLoadCancelled += mod => { Debug.LogWarning($"Mod loading canceled: {mod.name}"); };

            mm.ModUnloaded += mod => { Debug.Log($"Mod UNLOADED: {mod.name}"); };
        }
    }
}