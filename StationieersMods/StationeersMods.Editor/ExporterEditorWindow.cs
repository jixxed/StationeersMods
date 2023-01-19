using UnityEditor;
using UnityEngine;

using StationeersMods.Shared;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;

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


        [MenuItem("StationeersMods/Export Mod")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExporterEditorWindow>();
            window.titleContent = new GUIContent("StationeersMods Exporter");
            window.minSize = new Vector2(450, 320);
            ExtraAssetsWindow.ShowWindow();
            window.Focus();
        }

        private void OnEnable()
        {
            exportSettings = new EditorScriptableSingleton<ExportSettings>();
            exportSettingsEditor = UnityEditor.Editor.CreateEditor(exportSettings.instance) as ExportSettingsEditor;
            assemblyEditor = new AssemblyEditor();
            artifactEditor = new ArtifactEditor();
            exportEditor = new ExportEditor();
        }

        private void OnDisable()
        {
            DestroyImmediate(exportSettingsEditor);
        }

        private void DrawExportEditor(ExportSettings settings)
        {
            if (exportEditor.Draw(settings))
            {
                var buttonPressed = GUILayout.Button("Save & Export", GUILayout.Height(30));

                GUILayout.FlexibleSpace();

                if (buttonPressed)
                    Export.ExportMod(settings);
            }
        }

        private void OnGUI()
        {
            GUI.enabled = !EditorApplication.isCompiling && !Application.isPlaying;

            var settings = exportSettings.instance;

            var tabs = new string[] { "Export", "Assemblies", "Copy Artifacts" };

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
            }
        }

        public static void ExportMod()
        {
            var singleton = new EditorScriptableSingleton<ExportSettings>();
            Export.ExportMod(singleton.instance);
        }
    }
}