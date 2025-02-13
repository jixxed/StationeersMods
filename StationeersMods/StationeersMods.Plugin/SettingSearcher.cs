using System;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using StationeersMods.Interface;
using StationeersMods.Plugin.Configuration;

namespace StationeersMods.Plugin
{
    internal static class SettingSearcher
    {
        private static readonly ICollection<string> _updateMethodNames = new[]
        {
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnGUI"
        };

        /// <summary>
        /// Search for all instances of BaseUnityPlugin loaded by chainloader or other means.
        /// </summary>
        public static BaseUnityPlugin[] FindPlugins()
        {
            // Search for instances of BaseUnityPlugin to also find dynamically loaded plugins.
            // Have to use FindObjectsOfType(Type) instead of FindObjectsOfType<T> because the latter is not available in some older unity versions.
            // Still look inside Chainloader.PluginInfos in case the BepInEx_Manager GameObject uses HideFlags.HideAndDontSave, which hides it from Object.Find methods.
            return Chainloader.PluginInfos.Values.Select(x => x.Instance)
                .Where(plugin => plugin != null)
                .Union(UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>())
                .ToArray();
        }

        public static void CollectSettings(out IEnumerable<SettingEntryBase> results, ConfigFile configFile, ModVersionInfo modVersionInfo)
        {
            var detected = new List<SettingEntryBase>();
            detected.AddRange(configFile.Select(kvp => new ConfigSettingEntry(kvp.Value, modVersionInfo)));
            results = detected;
        }
    }
}