using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StationeersMods.Shared;
using UnityEditor;

namespace StationeersMods.Editor
{
    internal class ArtifactEditor : SelectionEditor
    {
        public override void DrawHelpBox()
        {
            EditorGUILayout.HelpBox("Add files from your project to copy directly into your mod folder.", MessageType.Info, true);
        }

        public override List<string> GetCandidates(ExportSettings settings)
        {
            return AssetDatabase.GetAllAssetPaths().Where(o => !settings.Artifacts.Contains(o)).ToList();
        }

        public override List<string> GetSelections(ExportSettings settings)
        {
            return settings.Artifacts.ToList();
        }
    }
}
