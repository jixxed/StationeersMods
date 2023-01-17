using System;
using System.IO;
using System.Reflection;
using Assets.Scripts.Serialization;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StationeersMods.Plugin
{
    /**
     * Patch to undo changes made by Stationeers.Addons.
     * We do not want to filter out anything from publishing.
     */
    public class WorkshopMenuPatch
    {
        // private static WorkshopMenu currentInstance;
        [HarmonyPriority(Priority.Last)]
        public static void RefreshButtonsPostfix(WorkshopMenu __instance)
        {
            var selectedMod = GetSelectedModData(__instance);
            WorkshopMenu  currentInstance = __instance;
            if (!selectedMod.Data.IsLocal) return;
            
            if( File.Exists(selectedMod.Data.LocalPath + "\\About\\stationeersmods"))
            {
                Debug.Log("stationeersmods file found.");
                __instance.SelectedModButtonRight.GetComponent<Button>().onClick.RemoveAllListeners();
                __instance.SelectedModButtonRight.GetComponent<Button>().onClick
                    .AddListener(() => PublishMod(currentInstance));
            }
        }
        
        public static void SelectModPostfix(WorkshopMenu __instance)
        {
            //if it is a StationeersMods mod
            var selectedMod = GetSelectedModData(__instance);
            if( File.Exists(selectedMod.Data.LocalPath + "\\About\\stationeersmods"))
            {
                string descriptionText = XmlSerialization.Deserialize<CustomModAbout>(selectedMod.Data.AboutXmlPath, "ModMetadata").InGameDescription;
                if(descriptionText != null && !descriptionText.Equals(string.Empty))
                {
                    __instance.DescriptionText.text = descriptionText;
                }
            }
        }

        public static void PublishMod(WorkshopMenu instance)
        {
            var type = typeof(WorkshopMenu);
            var publishModMethod = type.GetMethod("PublishMod", BindingFlags.NonPublic | BindingFlags.Instance);
            publishModMethod.Invoke(instance,null);
        }
        
        private static WorkshopModListItem GetSelectedModData(WorkshopMenu instance)
        {
            var selectedModItem = instance.GetType().GetField("_selectedModItem", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return (WorkshopModListItem) selectedModItem;
        }
    }
}