using UnityEditor;
using UnityEngine;
using UFSM.Runtime;
using UFSM.Editor.CodeGeneration;
using System.Collections.Generic;

namespace UFSM.Editor
{
    /// <summary>
    /// Batch code generation window
    /// </summary>
    public class FSMCodeGeneratorWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private List<FSMDefinition> allFSMs = new List<FSMDefinition>();
        private Dictionary<FSMDefinition, bool> selectedFSMs = new Dictionary<FSMDefinition, bool>();

        [MenuItem("Window/UFSM/Batch Code Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<FSMCodeGeneratorWindow>();
            window.titleContent = new GUIContent("FSM Code Generator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshFSMList();
        }

        private void RefreshFSMList()
        {
            allFSMs.Clear();
            selectedFSMs.Clear();

            // Find all FSM Definitions in project
            string[] guids = AssetDatabase.FindAssets("t:FSMDefinition");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var fsm = AssetDatabase.LoadAssetAtPath<FSMDefinition>(path);

                if (fsm != null)
                {
                    allFSMs.Add(fsm);
                    selectedFSMs[fsm] = false;
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Batch Code Generator", headerStyle);

            EditorGUILayout.Space(10);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Refresh List"))
                {
                    RefreshFSMList();
                }

                if (GUILayout.Button("Select All"))
                {
                    foreach (var fsm in allFSMs)
                    {
                        selectedFSMs[fsm] = true;
                    }
                }

                if (GUILayout.Button("Select None"))
                {
                    foreach (var fsm in allFSMs)
                    {
                        selectedFSMs[fsm] = false;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // FSM list
            EditorGUILayout.LabelField($"FSM Definitions ({allFSMs.Count})", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            {
                foreach (var fsm in allFSMs)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    {
                        selectedFSMs[fsm] = EditorGUILayout.Toggle(selectedFSMs[fsm], GUILayout.Width(20));

                        EditorGUILayout.LabelField(fsm.MachineName, EditorStyles.boldLabel);

                        EditorGUILayout.LabelField($"{fsm.States.Count} states", GUILayout.Width(80));

                        if (GUILayout.Button("Generate", GUILayout.Width(80)))
                        {
                            FSMCodeGenerator.GenerateCode(fsm, true);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Generate selected button
            int selectedCount = 0;
            foreach (var kvp in selectedFSMs)
            {
                if (kvp.Value) selectedCount++;
            }

            EditorGUI.BeginDisabledGroup(selectedCount == 0);
            {
                if (GUILayout.Button($"Generate Code for Selected ({selectedCount})", GUILayout.Height(40)))
                {
                    GenerateSelectedFSMs();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void GenerateSelectedFSMs()
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var kvp in selectedFSMs)
            {
                if (kvp.Value)
                {
                    if (FSMCodeGenerator.GenerateCode(kvp.Key, false))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Batch Generation Complete",
                $"Successfully generated: {successCount}\nFailed: {failCount}",
                "OK");
        }
    }
}
