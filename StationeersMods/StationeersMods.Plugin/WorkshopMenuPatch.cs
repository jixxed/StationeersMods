﻿using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine;
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
                    .AddListener(delegate() { PublishMod(currentInstance); });

            }
        }

        public static void SelectModPostfix(WorkshopMenu __instance)
        {
            //if it is a StationeersMods mod
            var selectedMod = GetSelectedModData(__instance);
            if( File.Exists(selectedMod.Data.LocalPath + "\\About\\stationeersmods"))
            {
                string descriptionText = XmlSerialization.Deserialize<CustomModAbout>(selectedMod.Data.AboutXmlPath, "ModMetadata").InGameDescription.Value;
                if(descriptionText != null && !descriptionText.Equals(string.Empty))
                {
                    __instance.DescriptionText.text = descriptionText;
                }
            }
        }

        private static void SaveWorkShopFileHandle(
            SteamTransport.WorkShopItemDetail ItemDetail,
            ModData mod)
        {
            CustomModAbout aboutData = XmlSerialization.Deserialize<CustomModAbout>(mod.AboutXmlPath, "ModMetadata");
            aboutData.WorkshopHandle = ItemDetail.PublishedFileId;
            aboutData.SaveXml<CustomModAbout>(mod.AboutXmlPath);
        }
        
        private static async void PublishMod(WorkshopMenu instance)
        {
            Debug.Log("Publishing Mod");
            ModData mod = GetSelectedModData(instance).Data;
            CustomModAbout aboutData = XmlSerialization.Deserialize<CustomModAbout>(mod.AboutXmlPath, "ModMetadata");
            string localPath = mod.LocalPath;
            string image = localPath + "\\About\\thumb.png";
            if(IsValidModData(aboutData, image))
            {
                SteamTransport.WorkShopItemDetail ItemDetail = new SteamTransport.WorkShopItemDetail()
                {
                    Title = aboutData.Name,
                    Path = localPath,
                    PreviewPath = localPath + "\\About\\thumb.png",
                    Description = aboutData.Description,
                    PublishedFileId = aboutData.WorkshopHandle,
                    Type = SteamTransport.WorkshopType.Mod,
                    CustomTags = aboutData.Tags,
                    ChangeNote = aboutData.ChangeLog
                };
                ProgressPanel.ShowProgressBar(false);
                var (success, fileId) =  await SteamTransport.Workshop_PublishItemAsync(ItemDetail);
                HideProgressBar();
                if (!success)
                    return;
                
                ItemDetail.PublishedFileId = fileId;
                SaveWorkShopFileHandle(ItemDetail, mod);
                AlertPanel.Instance.ShowAlert("Mod has been successfully published", AlertState.Alert);
            }
        }

        private static bool IsValidModData(CustomModAbout aboutData, string image)
        {
            List<string> errorMessages = new List<string>();
            if(aboutData.Name.Length > 128)
            {
                Debug.Log("Mod title exceeds 128 characters limit");
                errorMessages.Add("Mod title exceeds 128 characters limit");
            }
            if(aboutData.Description.Length > 8000)
            {
                Debug.Log("Mod description exceeds 8000 characters limit");
                errorMessages.Add("Mod description exceeds 8000 characters limit");
            }
            if(aboutData.ChangeLog.Length > 8000)
            {
                Debug.Log("Mod changelog exceeds 8000 characters limit");
                errorMessages.Add("Mod changelog exceeds 8000 characters limit");
            }
            if(File.Exists(image) && new FileInfo(image).Length > (1024 * 1024))
            {
                Debug.Log("Mod image size exceeds 1MB limit");
                errorMessages.Add("Mod image size exceeds 1MB limit");
            }

            if (errorMessages.Count == 0) return true;
            AlertPanel.Instance.ShowAlert(string.Join("\n", errorMessages), AlertState.Alert);
            return false;
        }

        private static WorkshopModListItem GetSelectedModData(WorkshopMenu instance)
        {
            var selectedModItem = instance.GetType().GetField("_selectedModItem", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return (WorkshopModListItem) selectedModItem;
        }
        private static void HideProgressBar()
        {
            typeof(ProgressPanel).GetMethod("HideProgressBar", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(ProgressPanel.Instance, null);

        }
    }
}