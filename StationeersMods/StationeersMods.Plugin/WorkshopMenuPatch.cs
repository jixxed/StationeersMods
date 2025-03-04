using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using StationeersMods.Interface;
using StationeersMods.Plugin.Configuration;
using StationeersMods.Plugin.Configuration.Utilities;
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
        private static SettingFieldDrawer
            _fieldDrawer = new SettingFieldDrawer(800, 350);

        // private static WorkshopMenu currentInstance;
        [HarmonyPriority(Priority.Last)]
        public static void RefreshButtonsPostfix(WorkshopMenu __instance)
        {
            var selectedMod = GetSelectedModData(__instance);
            WorkshopMenu currentInstance = __instance;
            if (selectedMod.Data is not LocalModData) return;

            if (File.Exists(selectedMod.Data.DirectoryPath + "\\About\\stationeersmods"))
            {
                Debug.Log("stationeersmods file found.");
                __instance.SelectedModButtonRight.GetComponent<Button>().onClick.RemoveAllListeners();
                __instance.SelectedModButtonRight.GetComponent<Button>().onClick
                    .AddListener(delegate() { PublishMod(currentInstance); });
            }
        }

        /// <summary>
        /// Is the config manager main window displayed on screen
        /// </summary>
        public static bool DisplayingWindow
        {
            get => _displayingWindow;
            set
            {
                if (_displayingWindow == value) return;
                _displayingWindow = value;

                SettingFieldDrawer.ClearCache();

                if (_displayingWindow)
                {
                    // CalculateWindowRect();

                    // BuildSettingList();

                    // _focusSearchBox = true;
                    //
                    // // Do through reflection for unity 4 compat
                    // if (_curLockState != null)
                    // {
                    //     _previousCursorLockState = _obsoleteCursor ? Convert.ToInt32((bool)_curLockState.GetValue(null, null)) : (int)_curLockState.GetValue(null, null);
                    //     _previousCursorVisible = (bool)_curVisible.GetValue(null, null);
                    // }
                }
                else
                {
                    // if (!_previousCursorVisible || _previousCursorLockState != 0) // 0 = CursorLockMode.None
                        // SetUnlockCursor(_previousCursorLockState, _previousCursorVisible);
                }

                // DisplayingWindowChanged?.Invoke(this, new ValueChangedEventArgs<bool>(value));
            }
        }
        private static bool _displayingWindow = false;
        private static WorkshopMenu WorkshopMenuInstance;
        public static void SelectModPostfix(WorkshopMenu __instance)
        {
            WorkshopMenuInstance = __instance;
            //if it is a StationeersMods mod
            var selectedMod = GetSelectedModData(__instance);
            if (File.Exists(selectedMod.Data.DirectoryPath + "\\About\\stationeersmods"))
            {
                string descriptionText = XmlSerialization.Deserialize<CustomModAbout>(selectedMod.Data.AboutXmlPath, "ModMetadata").InGameDescription.Value;
                if (descriptionText != null && !descriptionText.Equals(string.Empty))
                {
                    __instance.DescriptionText.text = descriptionText;
                }
            }
            if (File.Exists(selectedMod.Data.DirectoryPath + "\\About\\stationeersmods") || File.Exists(selectedMod.Data.DirectoryPath + "\\About\\bepinex"))
            {
                if(BepinPlugin.ConfigFiles.ContainsKey(selectedMod.Data.DirectoryPath))
                {
                    _configFile = BepinPlugin.ConfigFiles[selectedMod.Data.DirectoryPath];
                }else
                {
                    _configFile = null;
                }
                if(BepinPlugin.ModVersionInfos.ContainsKey(selectedMod.Data.DirectoryPath))
                {
                    _modVersionInfo = BepinPlugin.ModVersionInfos[selectedMod.Data.DirectoryPath];
                }else
                {
                    _modVersionInfo = null;
                }
                DisplayingWindow = _configFile != null && _modVersionInfo != null;
                BuildSettingList();
            }
            else
            {
                DisplayingWindow = false;
            }
        }

        private static GUIStyle windowStyle;
        private static Rect windowRect;
        private static ConfigFile _configFile;
        private static ModVersionInfo _modVersionInfo;

        public static void OnGUI()
        {
            // Only draw the window if showWindow is true
            if (DisplayingWindow)
            {
                if(!WorkshopMenuInstance.gameObject.activeSelf)
                {
                    DisplayingWindow = false;
                    return;
                }
                // Initialize the window style if it hasn't been already
                if (windowStyle == null)
                {
                    windowStyle = new GUIStyle(GUI.skin.box);
                    windowStyle.padding = new RectOffset(50, 50, 50, 50); // Add some padding to the window
                    GameObject panelWorkshopMods = GameObject.Find("PanelWorkshopMods");
                    if (panelWorkshopMods != null)
                    {
                        Texture2D panelTexture = panelWorkshopMods.GetComponent<Image>().mainTexture as Texture2D;

                        windowStyle.normal.background = panelTexture;
                        windowStyle.focused.background = null; // No background for focused state
                        windowStyle.hover.background = null; // No background for hover state
                        windowStyle.active.background = null; // No background for active state
                        windowStyle.border = new RectOffset(64, 64, 64, 64); // Set the border size (adjust accordingly)
                    }
                    else
                    {
                        Debug.LogError("PanelWorkshopMods GameObject not found.");
                    }
                }

                // Create a draggable window
                float margin = 50f;
                float width = 550f; //Screen.width * 0.2f;
                float height = Screen.height - (2 * margin);
                float x = Screen.width - width - margin;
                float y = margin;

                var rect = new Rect(x, y, width, height);
                windowRect = GUILayout.Window(0, rect, DrawWindow, "", windowStyle);
                
                Vector2 mousePosition = UnityInput.Current.mousePosition;
                mousePosition.y = Screen.height - mousePosition.y;

                if (!SettingFieldDrawer.SettingKeyboardShortcut && windowRect.Contains(mousePosition))
                    Input.ResetInputAxes();
            }
        }

        static Vector2 scrollPosition;
        private const string SearchBoxName = "searchBox";
        private static bool _focusSearchBox;
        private static string _searchString = string.Empty;
        private const int WindowId = -68;
        // Method to define the contents of the window
        private static void DrawWindow(int windowID)
        {
            var settingStyle = new GUIStyle();
            settingStyle.fontSize = 14;
            settingStyle.normal.textColor = Color.white;
            // Add UI elements using GUILayout
            var guiStyle = new GUIStyle();
            guiStyle.fontSize = 20;
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.alignment = TextAnchor.MiddleCenter;
            guiStyle.normal.textColor = Color.white;

            GUILayout.Label("Mod Settings", guiStyle);
            GUILayout.Space(8);
            
            GUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUILayout.Label("Search: ", GUILayout.ExpandWidth(false));

                GUI.SetNextControlName(SearchBoxName);
                SearchString = GUILayout.TextField(SearchString, GUILayout.ExpandWidth(true));

                if (_focusSearchBox)
                {
                    GUI.FocusWindow(WindowId);
                    GUI.FocusControl(SearchBoxName);
                    _focusSearchBox = false;
                }

                if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    SearchString = string.Empty;

                GUILayout.Space(8);
            }
            
            GUILayout.EndHorizontal();
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition, GUILayout.Width(450), GUILayout.Height(Screen.height - (5 * 50f)));
            
            _filteredSetings.ForEach(plugin => DrawSinglePlugin(plugin));
            GUILayout.EndScrollView();

            if (!SettingFieldDrawer.DrawCurrentDropdown())
                DrawTooltip(windowRect);
            // GUI.DragWindow();
        }

        private static void DrawSinglePlugin(PluginSettingsData plugin)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // var categoryHeader = new GUIContent($"{plugin.Info.Name} {plugin.Info.Version}");
            // SettingFieldDrawer.DrawPluginHeader(categoryHeader, false);

            foreach (var category in plugin.Categories)
            {
                if (!string.IsNullOrEmpty(category.Name))
                {
                    if (plugin.Categories.Count > 1)
                        SettingFieldDrawer.DrawCategoryHeader(category.Name);
                }

                foreach (var setting in category.Settings)
                {
                    DrawSingleSetting(setting);
                    GUILayout.Space(2);
                }
            }

            GUILayout.EndVertical();
        }

        private static void DrawSingleSetting(SettingEntryBase setting)
        {
            {
                try
                {
                    DrawSettingName(setting);
                    GUILayout.BeginHorizontal();
                    _fieldDrawer.DrawSettingValue(setting);
                    DrawDefaultButton(setting);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to draw setting {setting.DispName} - {ex}");
                    GUILayout.Label("Failed to draw this field, check log for details.");
                }
            }
            GUILayout.EndHorizontal();
        }
        private static void DrawTooltip(Rect area)
        {
            string tooltip = GUI.tooltip;
            if (!string.IsNullOrEmpty(tooltip))
            {
                var style = GUI.skin.box.CreateCopy();
                style.wordWrap = true;
                style.alignment = TextAnchor.MiddleCenter;

                GUIContent content = new GUIContent(tooltip);

                const int width = 350;
                var height = style.CalcHeight(content, 400) + 10;

                var mousePosition = Event.current.mousePosition;

                var x = mousePosition.x + width > area.width
                    ? area.width - width
                    : mousePosition.x;

                var y = mousePosition.y + 25 + height > area.height
                    ? mousePosition.y - height
                    : mousePosition.y + 25;

                Rect position = new Rect(x, y, width, height);
                ImguiUtils.DrawContolBackground(position, Color.black);
                style.Draw(position, content, -1);
            }
        }
        
        private static void DrawSettingName(SettingEntryBase setting)
        {
            if (setting.HideSettingName) return;

            var origColor = GUI.color;
            // if (setting.IsAdvanced == true)
            //     GUI.color = _advancedSettingColor;

            GUILayout.Label(new GUIContent(setting.DispName.TrimStart('!'), null, setting.Description),
                GUILayout.Width(300), GUILayout.MaxWidth(300));

            GUI.color = origColor;
        }

        private static void DrawDefaultButton(SettingEntryBase setting)
        {
            if (setting.HideDefaultButton) return;

            object defaultValue = setting.DefaultValue;
            if (defaultValue != null || setting.SettingType.IsClass)
            {
                GUILayout.Space(5);
                if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                    setting.Set(defaultValue);
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
                    return (CustomModAbout)(string.IsNullOrEmpty(root) ? new XmlSerializer(typeof(CustomModAbout)) : new XmlSerializer(typeof(CustomModAbout), new XmlRootAttribute(root)))
                        .Deserialize(streamReader);
            }
            catch (Exception)
            {
                return default(CustomModAbout);
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
                    new XmlSerializer(typeof(CustomModAbout)).Serialize(streamWriter, savable);
                    return true;
                }
            }
            catch (Exception)
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
            if (File.Exists(mod.DirectoryPath + "\\About\\stationeersmods"))
            {
                aboutData.Tags.Add("StationeersMods");
            }

            if (File.Exists(mod.DirectoryPath + "\\About\\bepinex"))
            {
                aboutData.Tags.Add("StationeersMods");
                aboutData.Tags.Add("BepInEx");
            }

            SaveXml(aboutData, mod.AboutXmlPath);

            string localPath = mod.DirectoryPath;
            string image = localPath + "\\About\\thumb.png";
            if (IsValidModData(aboutData, image))
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
                var (success, fileId, result) = await SteamTransport.Workshop_PublishItemAsync(ItemDetail);
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
            if (aboutData.Name.Length > 128)
            {
                Debug.Log("Mod title exceeds 128 characters limit");
                errorMessages.Add("Mod title exceeds 128 characters limit");
            }

            if (aboutData.Description.Length > 8000)
            {
                Debug.Log("Mod description exceeds 8000 characters limit");
                errorMessages.Add("Mod description exceeds 8000 characters limit");
            }

            if (aboutData.ChangeLog is { Length: > 8000 })
            {
                Debug.Log("Mod changelog exceeds 8000 characters limit");
                errorMessages.Add("Mod changelog exceeds 8000 characters limit");
            }

            if (File.Exists(image) && new FileInfo(image).Length > (1024 * 1024))
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
            return (WorkshopModListItem)selectedModItem;
        }

        private static void HideProgressBar()
        {
            typeof(ProgressPanel).GetMethod("HideProgressBar", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(ProgressPanel.Instance, null);
        }

        /// <summary>
        /// Rebuild the setting list. Use to update the config manager window if config settings were removed or added while it was open.
        /// </summary>
        public static void BuildSettingList()
        {
            if(_configFile != null && _modVersionInfo != null)
            {
                SettingSearcher.CollectSettings(out var results, _configFile, _modVersionInfo);
                _allSettings = results.ToList();
                BuildFilteredSettingList();
            }
        }

        private static List<PluginSettingsData> _filteredSetings = new List<PluginSettingsData>();
        /// <summary>
        /// String currently entered into the search box
        /// </summary>
        public static string SearchString
        {
            get => _searchString;
            private set
            {
                if (value == null)
                    value = string.Empty;

                if (_searchString == value)
                    return;

                _searchString = value;

                BuildFilteredSettingList();
            }
        }
        private static  IEnumerable<SettingEntryBase> _allSettings;
        private static void BuildFilteredSettingList()
        {
            IEnumerable<SettingEntryBase> results = _allSettings;
            var searchStrings = SearchString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (searchStrings.Length > 0)
            {
                results = _allSettings.Where(x => ContainsSearchString(x, searchStrings));
            }
            const string shortcutsCatName = "Keyboard shortcuts";
            _filteredSetings = results
                .GroupBy(x => x.ModVersionInfo)
                .Select(pluginSettings =>
                {
                    var categories = pluginSettings
                        .GroupBy(eb => eb.Category)
                        .OrderBy(x => string.Equals(x.Key, shortcutsCatName, StringComparison.Ordinal))
                        .ThenBy(x => x.Key)
                        .Select(x => new PluginSettingsData.PluginSettingsGroupData { Name = x.Key, Settings = x.OrderByDescending(set => set.Order).ThenBy(set => set.DispName).ToList() });


                    return new PluginSettingsData
                    {
                        Info = pluginSettings.Key,
                        Categories = categories.ToList()
                    };
                })
                .OrderBy(x => x.Info.Name)
                .ToList();
        }
        private static bool ContainsSearchString(SettingEntryBase setting, string[] searchStrings)
        {
            var combinedSearchTarget = setting.DispName + "\n" +
                                       setting.Category + "\n" +
                                       setting.Description + "\n" +
                                       setting.DefaultValue + "\n" +
                                       setting.Get();

            return searchStrings.All(s => combinedSearchTarget.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }
        private sealed class PluginSettingsData
        {
            public ModVersionInfo Info;
            public List<PluginSettingsGroupData> Categories;
            // public int Height;


            public sealed class PluginSettingsGroupData
            {
                public string Name;
                public List<SettingEntryBase> Settings;
            }
        }
    }
}