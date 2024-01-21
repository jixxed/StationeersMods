using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.Objects.Pipes;
using StationeersMods.Shared;
using UnityEditor;
using UnityEngine;

namespace StationeersMods.Editor
{
    class DevelopmentEditor
    {
        public static DevelopmentPatcher Patcher { get; } = new DevelopmentPatcher();

        public DevelopmentEditor(ExportSettings settings)
        {
            try
            {
                Patcher.CheckDevelopmentMode(settings);
            }
            catch(ArgumentException ex){ /* ignore */ }
        }

        public bool Draw(ExportSettings settings)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("Enabling development mode allows you to attach to the game and debug your code. See [How to debug] for a guide.",
                MessageType.Info, true);
            if (GUILayout.Button("How to debug", GUILayout.Width(200), GUILayout.Height(38)))
            {
                Application.OpenURL("https://github.com/jixxed/StationeersMods/tree/main/doc/DEBUGGING.md");
            }

            GUILayout.EndHorizontal();
            if (Patcher.DevelopmentModeEnabled == true && !settings.IncludePdbs)
            {
                EditorGUILayout.HelpBox("Debug information needs to be exported or it will not be possible to debug your code. Enable: Export > Include PDBs.", MessageType.Warning, true);
            }

            // Stationeers path
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Stationeers directory:", GUILayout.Width(200));
            settings.StationeersDirectory = EditorGUILayout.TextField("", settings.StationeersDirectory);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var selectedDirectory =
                    EditorUtility.SaveFolderPanel("Choose Stationeers directory", settings.StationeersDirectory, "");
                if (!string.IsNullOrEmpty(selectedDirectory))
                    settings.StationeersDirectory = selectedDirectory;
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stationeers launch arguments:", GUILayout.Width(200));
            settings.StationeersArguments = EditorGUILayout.TextField("", settings.StationeersArguments);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Development mode
            GUILayout.BeginHorizontal();
            
            try
            {
                Patcher.CheckDevelopmentMode(settings);
            }
            catch(ArgumentException ex){ /* ignore */ }
            EditorGUILayout.LabelField("Stationeers mode:", GUILayout.Width(200));
            EditorGUILayout.LabelField(!Patcher.DevelopmentModeEnabled.HasValue ? "UNKNOWN" : (Patcher.DevelopmentModeEnabled.Value ? "DEVELOPMENT" : "RELEASE"));
            // if (GUILayout.Button("Check", GUILayout.Width(200)))
            // {
            //     try
            //     {
            //         Patcher.CheckDevelopmentMode(settings);
            //     }
            //     catch(ArgumentException ex)
            //     {
            //         EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            //     }
            //
            // }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            int buttonWidth = 300;
            // GUILayout.Space(Screen.width /2 - buttonWidth);
            if (!Patcher.DevelopmentModeEnabled.HasValue)
            {
                GUI.enabled = false;
            }

            if (Patcher.DevelopmentModeEnabled.HasValue && !Patcher.DevelopmentModeEnabled.Value)
            {
                if (GUILayout.Button("Enable development mode", GUILayout.Width(buttonWidth), GUILayout.Height(35)))
                {
                    try
                    {
                        Patcher.SetDevelopmentMode(settings, true);
                    }
                    catch(ArgumentException ex)
                    {
                        EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Disable development mode", GUILayout.Width(buttonWidth), GUILayout.Height(35)))
                {
                    try
                    {
                        Patcher.SetDevelopmentMode(settings, false);
                    }
                    catch(ArgumentException ex)
                    {
                        EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                    }
                }
            }
            if (!Patcher.DevelopmentModeEnabled.HasValue)
            {
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            return true;
        }
    }
}