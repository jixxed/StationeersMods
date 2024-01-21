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
        public DevelopmentPatcher Patcher { get; } = new DevelopmentPatcher();

        public bool Draw(ExportSettings settings)
        {
            EditorGUILayout.HelpBox("Various options to assist in mod development. Enabling development mode allows you to attach to the game and debug your code. See https://github.com/jixxed/StationeersMods/tree/main/doc/DEBUGGING.md for a guide.", MessageType.Info, true);

            if (!settings.IncludePdbs)
            {
                EditorGUILayout.HelpBox("Debug information needs to be exported or it will not be possible to debug your code. Enable Export > Include PDBs.", MessageType.Warning, true);
            }

            // Stationeers path
            GUILayout.BeginHorizontal();

            settings.StationeersDirectory = EditorGUILayout.TextField("Stationeers directory:", settings.StationeersDirectory);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var selectedDirectory =
                    EditorUtility.SaveFolderPanel("Choose Stationeers directory", settings.StationeersDirectory, "");
                if (!string.IsNullOrEmpty(selectedDirectory))
                    settings.StationeersDirectory = selectedDirectory;
            }

            GUILayout.EndHorizontal();

            settings.StationeersArguments = EditorGUILayout.TextField("Stationeers arguments:", settings.StationeersArguments);

            GUILayout.Space(5);

            // Development mode
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stationeers is currently in mode: " + (!Patcher.DevelopmentModeEnabled.HasValue ? "UNKNOWN" : (Patcher.DevelopmentModeEnabled.Value ? "DEVELOPMENT" : "RELEASE")));
            if (GUILayout.Button("Check", GUILayout.Width(200)))
            {
                Patcher.CheckDevelopmentMode(settings);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(50);

            if (GUILayout.Button("Enable development mode"))
            {
                Patcher.SetDevelopmentMode(settings, true);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Disable development mode"))
            {
                Patcher.SetDevelopmentMode(settings, false);
            }

            GUILayout.Space(50);
            GUILayout.EndHorizontal();

            return true;
        }
    }
}
