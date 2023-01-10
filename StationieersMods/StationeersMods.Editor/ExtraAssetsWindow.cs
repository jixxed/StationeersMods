using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace StationeersMods.Editor
{
    class ExtraAssetsWindow : EditorWindow
    {
        Vector2 scrollPosition;

        [MenuItem("StationeersMods/Show Copy Assets")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExtraAssetsWindow>("Additional Copy Assets", typeof(ExporterEditorWindow));
            window.minSize = new Vector2(250, 350);
        }

        private void OnGUI()
        {
            var windowRect = new Rect(0, 0, Screen.width, Screen.height);
            scrollPosition = GUI.BeginScrollView(windowRect, scrollPosition, windowRect);
            foreach (var path in AssetUtility.GetAssets("l:Copy"))
            {
                EditorGUILayout.LabelField(path);
            }
            GUI.EndScrollView(true);
        }
    }
}
