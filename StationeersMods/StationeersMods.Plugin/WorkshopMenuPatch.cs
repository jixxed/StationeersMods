using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
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
            //Disable the unsubscribe button for own mods
            if(__instance.SelectedModButtonRight.activeSelf || __instance.SelectedModButtonLeft.activeSelf)// if both are disabled -> Core
            {
                __instance.SelectedModButtonRight.SetActive(!__instance.SelectedModButtonLeft.activeSelf);
            }
            
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
            CustomModAbout aboutData = Deserialize(mod.AboutXmlPath);
            aboutData.WorkshopHandle = ItemDetail.PublishedFileId;
            SaveXml(aboutData, mod.AboutXmlPath);
        }
        
        public static CustomModAbout Deserialize(string path, string root = "ModMetadata")
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("File not found at path: " + path);
                using (StreamReader streamReader = new StreamReader(path))
                    return (CustomModAbout) (string.IsNullOrEmpty(root) ? new XmlSerializer(typeof (CustomModAbout)) : new XmlSerializer(typeof (CustomModAbout), new XmlRootAttribute(root))).Deserialize(streamReader);
            }
            catch (Exception ex)
            {
                return default (CustomModAbout);
            }
        }
        
        public static bool SaveXml(CustomModAbout savable, string path)
        {
            return Serialize(savable, path);
        }
        
        public static bool Serialize(CustomModAbout savable, string path)
        {
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    new XmlSerializer(typeof (CustomModAbout)).Serialize(streamWriter, savable);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private static async void PublishMod(WorkshopMenu instance)
        {
            Debug.Log("Publishing Mod");
            ModData mod = GetSelectedModData(instance).Data;
            CustomModAbout aboutData = XmlSerialization.Deserialize<CustomModAbout>(mod.AboutXmlPath, "ModMetadata");
            if (aboutData.Tags == null)
            {
                aboutData.Tags = new List<string>();
            }
            aboutData.Tags.RemoveAll(tag => tag.ToLower().Equals("stationeersmods") || tag.ToLower().Equals("bepinex"));
            if (File.Exists(mod.LocalPath + "\\About\\stationeersmods"))
            {
                aboutData.Tags.Add("StationeersMods");
            }
            if (File.Exists(mod.LocalPath + "\\About\\bepinex"))
            {
                aboutData.Tags.Add("BepInEx");
            }

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
                var (success, fileId, result) =  await SteamTransport.Workshop_PublishItemAsync(ItemDetail);
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
            if(aboutData.ChangeLog is { Length: > 8000 })
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