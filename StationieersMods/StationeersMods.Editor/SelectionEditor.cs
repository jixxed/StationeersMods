using StationeersMods.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace StationeersMods.Editor
{
    internal abstract class SelectionEditor
    {
        int selection = 0;

        void ConstrainSelection(List<string> candidates)
        {
            if (selection < 0 || selection > candidates.Count - 1)
                selection = 0;
        }

        public abstract List<string> GetSelections(ExportSettings settings);
        public abstract List<string> GetCandidates(ExportSettings settings);
        public abstract void DrawHelpBox();

        void DrawSelector(List<string> selections, List<string> candidates)
        {
            ConstrainSelection(candidates);

            EditorGUILayout.BeginHorizontal();

            selection = EditorGUILayout.Popup(selection, candidates.ToArray());

            if (GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(14)))
            {
                selections.Add(candidates[selection]);
            }

            EditorGUILayout.EndHorizontal();
        }

        bool DrawSelection(string selection)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(selection);

            bool removed = false;

            if (GUILayout.Button("-", GUILayout.Width(24), GUILayout.Height(14)))
            {
                removed = true;
            }

            EditorGUILayout.EndHorizontal();

            return removed;
        }

        void DrawSelections(List<string> selections)
        {
            string removed = null;

            foreach (var selection in selections)
            {
                if (DrawSelection(selection))
                    removed = selection;
            }

            if (removed != null)
            {
                selections.Remove(removed);
            }
        }

        List<string> FilterCandidates(List<string> candidates, List<string> selections)
        {
            return candidates.Where(o => !selections.Contains(o)).ToList();
        }

        public string[] Draw(ExportSettings settings)
        {
            var selections = GetSelections(settings);
            var candidates = GetCandidates(settings);
            candidates = FilterCandidates(candidates, selections);


            DrawHelpBox();
            DrawSelector(selections, candidates);
            DrawSelections(selections);

            return selections.ToArray();
        }
    }
}
