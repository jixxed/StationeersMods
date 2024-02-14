using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StationeersMods.Shared;
using UnityEditor;

namespace StationeersMods.Editor
{
    internal class AssemblyEditor : SelectionEditor
    {
        public override void DrawHelpBox()
        {
            EditorGUILayout.HelpBox("Add asmdefs from your project to be exported into your mod.", MessageType.Info, true);
        }

        public override List<string> GetCandidates(ExportSettings settings)
        {
            return AssetUtility.GetAssets("t:AssemblyDefinitionAsset").ToList();
        }

        public override List<string> GetSelections(ExportSettings settings)
        {
            return settings.Assemblies.ToList();
        }
    }
}