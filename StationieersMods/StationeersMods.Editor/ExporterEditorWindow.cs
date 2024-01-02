using UnityEditor;
using UnityEngine;

using StationeersMods.Shared;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace StationeersMods.Editor
{
    public class ExporterEditorWindow : EditorWindow
    {
        private EditorScriptableSingleton<ExportSettings> exportSettings;
        private ExportSettingsEditor exportSettingsEditor;

        private int selectedTab = 0;
        // private int selectedPath = 0;
        private List<string> addedAsmdefs = new List<string>();

        ExportEditor exportEditor;
        AssemblyEditor assemblyEditor;
        ArtifactEditor artifactEditor;
        DevelopmentEditor developmentEditor;

        public static string GetShortString(string str)
        {
            if (str == null)
            {
                return null;
            }

            var maxWidth = (int)EditorGUIUtility.currentViewWidth - 252;
            var cutoffIndex = Mathf.Max(0, str.Length - 7 - maxWidth / 7);
            var shortString = str.Substring(cutoffIndex);
            if (cutoffIndex > 0)
                shortString = "..." + shortString;
            return shortString;
        }

        [MenuItem("StationeersMods/Export Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExporterEditorWindow>();
            window.titleContent = new GUIContent("StationeersMods Exporter");
            window.minSize = new Vector2(450, 320);
            ExtraAssetsWindow.ShowWindow();
            window.Focus();
        }

        [MenuItem("StationeersMods/Export Mod", false, 20)]
        public static void ExportModMenuItem()
        {
            ExportMod();
        }

        [MenuItem("StationeersMods/Export && Run Mod", false, 20)]
        public static void ExportAndRunModMenuItem()
        {
            ExportMod();
            RunGame();
        }

        private void OnEnable()
        {
            exportSettings = new EditorScriptableSingleton<ExportSettings>();
            exportSettingsEditor = UnityEditor.Editor.CreateEditor(exportSettings.instance) as ExportSettingsEditor;
            assemblyEditor = new AssemblyEditor();
            artifactEditor = new ArtifactEditor();
            exportEditor = new ExportEditor();
            developmentEditor = new DevelopmentEditor();
        }

        private void OnDisable()
        {
            DestroyImmediate(exportSettingsEditor);
        }

        private void DrawExportEditor(ExportSettings settings)
        {
            if (exportEditor.Draw(settings))
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Space(50);

                if (GUILayout.Button("Save & Export", GUILayout.Height(30)))
                {
                    Export.ExportMod(settings);
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Save & Export & Run", GUILayout.Height(30)))
                {
                    Export.ExportMod(settings);
                    Export.RunGame(settings);
                }

                GUILayout.Space(50);
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
            }
        }

        private void OnGUI()
        {
            GUI.enabled = !EditorApplication.isCompiling && !Application.isPlaying;

            var settings = exportSettings.instance;

            var tabs = new string[] { "Export", "Assemblies", "Copy Artifacts", "Development" };

            selectedTab = GUILayout.Toolbar(selectedTab, tabs);

            switch (tabs[selectedTab])
            {
                case "Export":
                    DrawExportEditor(settings);
                    break;
                case "Assemblies":
                    settings.Assemblies = assemblyEditor.Draw(settings);
                    break;
                case "Copy Artifacts":
                    settings.Artifacts = artifactEditor.Draw(settings);
                    break;
                case "Development":
                    developmentEditor.Draw(settings);
                    break;
            }
        }

        public static void ExportMod()
        {
            var singleton = new EditorScriptableSingleton<ExportSettings>();
            Export.ExportMod(singleton.instance);
        }

        public static void RunGame()
        {
            var singleton = new EditorScriptableSingleton<ExportSettings>();
            Export.RunGame(singleton.instance);
        }
    }
}