// Assets/UFSM/Editor/FSMDefinitionEditor.cs
using UnityEditor;
using UnityEngine;
using UFSM.Runtime;
using UFSM.Editor.CodeGeneration;

namespace UFSM.Editor
{
    /// <summary>
    /// Custom inspector for FSMDefinition with code generation buttons
    /// Ensures GUID migration on enable so inspector renames won't break graph.
    /// </summary>
    [CustomEditor(typeof(FSMDefinition))]
    public class FSMDefinitionEditor : UnityEditor.Editor
    {
        private FSMDefinition fsm;

        private void OnEnable()
        {
            fsm = (FSMDefinition)target;

            // Ensure GUIDs & transitions migrated when inspector opens
            FSMEditorHelpers.EnsureStateGuids(fsm);
            FSMEditorHelpers.MigrateTransitionGuids(fsm);
        }

        public override void OnInspectorGUI()
        {
            // Header
            EditorGUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField($"FSM: {fsm.MachineName}", headerStyle);

            EditorGUILayout.Space(10);

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            // Open in Visual Editor button
            if (GUILayout.Button("Open in Visual Editor", GUILayout.Height(40)))
            {
                FSMEditorWindow.OpenFSM(fsm);
            }

            EditorGUILayout.Space(5);

            // Generate Code section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Code Generation", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Generate Code", GUILayout.Height(30)))
                    {
                        GenerateCode(false);
                    }

                    if (GUILayout.Button("Generate & Compile", GUILayout.Height(30)))
                    {
                        GenerateCode(true);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    "Generates C# code from this FSM definition.\n" +
                    "• Context with parameters\n" +
                    "• State machine setup\n" +
                    "• State classes (partial)\n" +
                    "• User files (safe to edit)",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Auto-generate toggle
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                bool autoGenerate = EditorPrefs.GetBool($"UFSM_AutoGenerate_{fsm.GetInstanceID()}", false);
                bool newAutoGenerate = EditorGUILayout.Toggle("Auto-Generate on Save", autoGenerate);

                if (newAutoGenerate != autoGenerate)
                {
                    EditorPrefs.SetBool($"UFSM_AutoGenerate_{fsm.GetInstanceID()}", newAutoGenerate);
                }

                if (newAutoGenerate)
                {
                    EditorGUILayout.HelpBox("Code will be generated automatically when you save this FSM.", MessageType.None);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Statistics
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"States: {fsm.States.Count}");
                EditorGUILayout.LabelField($"Transitions: {fsm.Transitions.Count}");
                EditorGUILayout.LabelField($"Parameters: {fsm.Parameters.Count}");
                EditorGUILayout.LabelField($"Initial State: {fsm.InitialStateName}");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Default inspector
            EditorGUILayout.LabelField("FSM Properties", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }

        private void GenerateCode(bool andCompile)
        {
            bool success = FSMCodeGenerator.GenerateCode(fsm, true);

            if (success && andCompile)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Compiling",
                    "Code generated successfully!\nUnity is now compiling...",
                    "OK");
            }
        }
    }

    /// <summary>
    /// Asset modification processor for auto-generation on save
    /// (unchanged, kept for compatibility)
    /// </summary>
    public class FSMAssetProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                // Check if it's an FSM Definition
                if (assetPath.EndsWith(".asset"))
                {
                    var fsm = AssetDatabase.LoadAssetAtPath<FSMDefinition>(assetPath);
                    if (fsm != null)
                    {
                        // Ensure migration is always applied
                        FSMEditorHelpers.EnsureStateGuids(fsm);
                        FSMEditorHelpers.MigrateTransitionGuids(fsm);

                        // Check if auto-generate is enabled for this FSM
                        bool autoGenerate = EditorPrefs.GetBool($"UFSM_AutoGenerate_{fsm.GetInstanceID()}", false);

                        if (autoGenerate)
                        {
                            Debug.Log($"[Auto-Generate] Generating code for {fsm.MachineName}...");
                            FSMCodeGenerator.GenerateCode(fsm, false);
                        }
                    }
                }
            }
        }
    }
}
