using StationeersMods.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StationeersMods.Editor
{
    public class ExportValidationError : Exception
    {
        public ExportValidationError(string message) : base(message)
        {
        }
    }


    [CustomEditor(typeof(ExportSettings))]
    public class ExportSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _author;
        private SerializedProperty _description;
        private SerializedProperty _name;
        private SerializedProperty _outputDirectory;
        private SerializedProperty _stationeersDirectory;
        private SerializedProperty _stationeersArguments;
        private SerializedProperty _includePdbs;
        private SerializedProperty _waitForDebugger;
        private SerializedProperty _version;
        private SerializedProperty _prefab;
        private SerializedProperty _scene;
        private SerializedProperty _class;
        private SerializedProperty _types;
        private SerializedProperty _kind;
        private SerializedProperty _assemblies;

        private void OnEnable()
        {
            //var exportSettings = new EditorScriptableSingleton<ExportSettings>();

            _name = serializedObject.FindProperty("_name");
            _author = serializedObject.FindProperty("_author");
            _description = serializedObject.FindProperty("_description");
            _version = serializedObject.FindProperty("_version");
            _outputDirectory = serializedObject.FindProperty("_outputDirectory");
            _stationeersDirectory = serializedObject.FindProperty("_stationeersDirectory");
            _stationeersArguments = serializedObject.FindProperty("_stationeersArguments");
            _includePdbs = serializedObject.FindProperty("_includePdbs");
            _waitForDebugger = serializedObject.FindProperty("_waitForDebugger");
            _prefab = serializedObject.FindProperty("_startupPrefab");
            _scene = serializedObject.FindProperty("_startupScene");
            _class = serializedObject.FindProperty("_startupClass");
            _types = serializedObject.FindProperty("_contentTypes");
            _kind = serializedObject.FindProperty("_bootType");
            _assemblies = serializedObject.FindProperty("_assemblies");

        }

        private void DrawSection(Action thunk)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));

            GUILayout.Space(5);

            try
            {
                thunk();
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();

            }
            catch (ExportValidationError e)
            {
                GUILayout.Space(5);
                EditorGUILayout.EndVertical();
                throw e;
            }
        }

        private void DrawDetails()
        {
            DrawSection(() => {
                EditorGUILayout.PropertyField(_name, new GUIContent("Mod Name:"));
                EditorGUILayout.PropertyField(_author, new GUIContent("Author:"));
                EditorGUILayout.PropertyField(_version, new GUIContent("Version:"));
                EditorGUILayout.PropertyField(_description, new GUIContent("Description:"), GUILayout.Height(60));
            });

            if (_name.stringValue == "" || _author.stringValue == "" || _version.stringValue == "" || _description.stringValue == "")
            {
                throw new ExportValidationError("All mod details must be specified.");
            }
        }

        private ContentType DrawContentSelector()
        {
            var enumValue = (ContentType)_types.intValue;
            enumValue = (ContentType)EditorGUILayout.EnumFlagsField("Included content:", enumValue);
            _types.intValue = (int)enumValue;
            if (_types.intValue == 0)
            {
                throw new ExportValidationError("You must include some content in your mod.");
            }
            return enumValue;
        }

        private BootType DrawBootSelector(ContentType content)
        {
            var forwardChoices = new Dictionary<BootType, string>();
            var backwardChoices = new Dictionary<string, BootType>();

            if (content.HasFlag(ContentType.assemblies))
            {
                forwardChoices[BootType.entrypoint] = "Code";
                backwardChoices["Code"] = BootType.entrypoint;
            }
            if (content.HasFlag(ContentType.prefabs))
            {
                forwardChoices[BootType.prefab] = "Prefab";
                backwardChoices["Prefab"] = BootType.prefab;
            }
            if (content.HasFlag(ContentType.scenes))
            {
                forwardChoices[BootType.scene] = "Scene";
                backwardChoices["Scene"] = BootType.scene;
            }

            var items = forwardChoices.Values.ToList();
            items.Sort();

            var currentInt = _kind.intValue;
            var currentEnum = (BootType)currentInt;

            string currentChoice;
            if (forwardChoices.ContainsKey(currentEnum))
            {
                currentChoice = forwardChoices[currentEnum];
            } else
            {
                currentChoice = forwardChoices[0];
            }

            var currentIndex = items.IndexOf(currentChoice);

            currentIndex = EditorGUILayout.Popup("Startup type:", currentIndex, items.ToArray());
            currentChoice = items[currentIndex];
            currentEnum = backwardChoices[currentChoice];
            _kind.intValue = (int)currentEnum;
            return currentEnum;
        }

        private void DrawStartupSelector(BootType bootType)
        { 
            switch (bootType)
            {
                case BootType.entrypoint:
                    EditorGUILayout.PropertyField(_class, new GUIContent("Startup class:"));
                    if (_class.stringValue == "") throw new ExportValidationError("You must specify a class in your assembly.");
                    break;
                case BootType.prefab:
                    EditorGUILayout.PropertyField(_prefab, new GUIContent("Startup prefab:"));
                    if (_prefab.objectReferenceValue == null) throw new ExportValidationError("You must specify a prefab from your project.");
                    break;
                case BootType.scene:
                    var scenes = AssetDatabase.FindAssets("t:scene").Select(o => AssetDatabase.GUIDToAssetPath(o)).ToList();
                    if (scenes.Count == 0)
                    {
                        throw new ExportValidationError("There are no scenes in this project.");
                    }
                    scenes.Sort();
                    var currentIndex = Math.Max(0, scenes.IndexOf(_scene.stringValue));
                    Debug.Log($"Current scene index: {currentIndex}");
                    _scene.stringValue = scenes[EditorGUILayout.Popup("Startup scene:", currentIndex, scenes.ToArray())];
                    break;
            }
        }

        private void DrawContentSection()
        {
            DrawSection(() => {
                var selectedContent = DrawContentSelector();
                var selectedBoot = DrawBootSelector(selectedContent);
                DrawStartupSelector(selectedBoot);
            });
        }

        private void DrawDirectorySelector(string label, SerializedProperty path)
        {
            GUILayout.BeginHorizontal();

            path.stringValue = EditorGUILayout.TextField(label, path.stringValue);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var selectedDirectory =
                    EditorUtility.SaveFolderPanel("Choose directory", path.stringValue, "");
                if (!string.IsNullOrEmpty(selectedDirectory))
                    path.stringValue = selectedDirectory;

                Repaint();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStationeersDirectorySelector()
        {
            DrawDirectorySelector("Stationeers Directory:", _stationeersDirectory);
        }

        private void DrawStationeersArgumentSelector()
        {
            _stationeersArguments.stringValue = EditorGUILayout.TextField("Stationeers arguments:", _stationeersArguments.stringValue);
        }
        private void DrawStationeersWaitForDebugger()
        {
            _waitForDebugger.boolValue = EditorGUILayout.Toggle("Wait for debugger on game launch:", _waitForDebugger.boolValue);
        }

        private void DrawDevelopmentOptions()
        {
            DrawSection(() => {
                DrawStationeersDirectorySelector();
                DrawStationeersArgumentSelector();
                DrawStationeersWaitForDebugger();
            });
        }

        private void DrawOutputDirectorySelector()
        {
            DrawDirectorySelector("Output Directory*:", _outputDirectory);

            if (_outputDirectory.stringValue == "")
            {
                throw new ExportValidationError("You must specify an output directory.");
            }
        }

        private void DrawLogSelector()
        {
            LogUtility.logLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level:", LogUtility.logLevel);
        }

        private void DrawPdbSelector()
        {
            _includePdbs.boolValue = EditorGUILayout.Toggle("Include Pdb's:", _includePdbs.boolValue);
        }

        private void DrawAssemblySelector()
        {
            DrawSection(() =>
            {
                GUILayout.Label("Assemblies");
                for (int i = 0; i < _assemblies.arraySize; i++)
                {
                    var _assembly = _assemblies.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();
                    Debug.Log(_assembly.ToString());
                    EditorGUILayout.PropertyField(_assembly, new GUIContent("path: "));
                    GUILayout.Button("-", GUILayout.Width(24), GUILayout.Height(14));
                    EditorGUILayout.EndHorizontal();
                }
            });

        }

        private void DrawExportOptions()
        {
            DrawSection(() => {
                DrawLogSelector();
                DrawPdbSelector();
                DrawOutputDirectorySelector();
            });
        }

        private void DrawGUI()
        {
            DrawDetails();
            DrawContentSection();
            DrawExportOptions();
            DrawAssemblySelector();
            DrawDevelopmentOptions();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            try
            {
                DrawGUI();
            } 
            catch (ExportValidationError e)
            {
                EditorGUILayout.HelpBox(e.Message, MessageType.Info);
            } 

            serializedObject.ApplyModifiedProperties();
        }

        public bool DrawExporter()
        {
            serializedObject.Update();
            var valid = true;

            try { DrawGUI(); } 
            catch (ExportValidationError e)
            {
                EditorGUILayout.HelpBox(e.Message, MessageType.Info);
                valid = false;
            } 

            serializedObject.ApplyModifiedProperties();
            return valid;
        }
    }
}