// ============================================================================
// UFSM - Universal Finite State Machine System for Unity
// Phase 3: Visual FSM Editor - Main Window
// File: Assets/UFSM/Editor/FSMEditorWindow.cs
// ============================================================================

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UFSM.Runtime;
using UFSM.Editor.CodeGeneration;

namespace UFSM.Editor
{
    /// <summary>
    /// Main FSM Editor Window - Split view with Graph + Properties
    /// </summary>
    public class FSMEditorWindow : EditorWindow
    {
        // Current FSM being edited
        private FSMDefinition currentFSM;

        // Main UI sections
        private VisualElement rootContainer;
        private VisualElement toolbarContainer;
        private TwoPaneSplitView splitView;
        private VisualElement graphViewContainer;
        private VisualElement propertiesContainer;

        // Child components
        private FSMGraphView graphView;
        private FSMPropertiesPanel propertiesPanel;
        private FSMToolbar toolbar;

        // State
        private bool hasUnsavedChanges = false;


        [MenuItem("Window/UFSM/FSM Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<FSMEditorWindow>();
            window.titleContent = new GUIContent("FSM Editor", EditorGUIUtility.IconContent("ScriptableObject Icon").image);
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// Open editor with specific FSM
        /// </summary>
        public static void OpenFSM(FSMDefinition fsm)
        {
            var window = GetWindow<FSMEditorWindow>();
            window.LoadFSM(fsm);
            window.Show();
            window.Focus();
        }

        private void CreateGUI()
        {
            // Load USS stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UFSM/Editor/Styles/FSMEditorStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            BuildUI();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // Root container
            rootContainer = new VisualElement { name = "root-container" };
            rootContainer.style.flexGrow = 1;
            rootVisualElement.Add(rootContainer);

            // Toolbar
            toolbarContainer = new VisualElement { name = "toolbar-container" };
            toolbar = new FSMToolbar(this);
            toolbarContainer.Add(toolbar.GetRootElement());
            rootContainer.Add(toolbarContainer);

            // Split view (Graph + Properties)
            splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            rootContainer.Add(splitView);

            // Graph View (left side)
            graphViewContainer = new VisualElement { name = "graph-view-container" };
            graphViewContainer.style.flexGrow = 1;
            graphView = new FSMGraphView(this);
            graphViewContainer.Add(graphView);
            splitView.Add(graphViewContainer);

            // Properties Panel (right side)
            propertiesContainer = new VisualElement { name = "properties-container" };
            propertiesContainer.style.width = 350;
            propertiesPanel = new FSMPropertiesPanel(this);
            propertiesContainer.Add(propertiesPanel.GetRootElement());
            splitView.Add(propertiesContainer);

            // Load current FSM if any
            if (currentFSM != null)
            {
                RefreshAll();
            }
            else
            {
                ShowWelcomeScreen();
            }
        }

        private void ShowWelcomeScreen()
        {
            var welcome = new VisualElement { name = "welcome-screen" };
            welcome.style.flexGrow = 1;
            welcome.style.alignItems = Align.Center;
            welcome.style.justifyContent = Justify.Center;

            var label = new Label("No FSM Loaded\n\nCreate or open an FSM to get started");
            label.style.fontSize = 18;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(0.7f, 0.7f, 0.7f);

            welcome.Add(label);
            graphViewContainer.Add(welcome);
        }

        /// <summary>
        /// Load an FSM into the editor
        /// </summary>
        public void LoadFSM(FSMDefinition fsm)
        {
            if (fsm == null)
            {
                Debug.LogWarning("Cannot load null FSM");
                return;
            }

            // Save changes if needed
            if (hasUnsavedChanges && currentFSM != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{currentFSM.MachineName}'?",
                    "Save", "Don't Save"))
                {
                    SaveCurrentFSM();
                }
            }

            currentFSM = fsm;
            hasUnsavedChanges = false;

            RefreshAll();

            Debug.Log($"Loaded FSM: {fsm.MachineName}");
        }

        /// <summary>
        /// Create a new FSM
        /// </summary>
        public void CreateNewFSM()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New FSM",
                "NewFSM",
                "asset",
                "Enter a name for the new FSM");

            if (string.IsNullOrEmpty(path)) return;

            var newFSM = CreateInstance<FSMDefinition>();
            newFSM.MachineName = System.IO.Path.GetFileNameWithoutExtension(path);

            // Add default Idle state
            newFSM.States.Add(new StateDefinition { StateName = "Idle", Description = "Default idle state" });
            newFSM.InitialStateName = "Idle";

            AssetDatabase.CreateAsset(newFSM, path);
            AssetDatabase.SaveAssets();

            LoadFSM(newFSM);

            EditorUtility.DisplayDialog("FSM Created",
                $"Created new FSM: {newFSM.MachineName}", "OK");
        }

        /// <summary>
        /// Save current FSM
        /// </summary>
        public void SaveCurrentFSM()
        {
            if (currentFSM == null) return;

            EditorUtility.SetDirty(currentFSM);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            hasUnsavedChanges = false;

            Debug.Log($"Saved FSM: {currentFSM.MachineName}");
        }

        /// <summary>
        /// Mark that changes have been made
        /// </summary>
        public void MarkDirty()
        {
            hasUnsavedChanges = true;
            titleContent.text = $"*FSM Editor - {currentFSM?.MachineName ?? "No FSM"}";
        }

        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        public void RefreshAll()
        {
            if (currentFSM == null) return;

            titleContent.text = $"FSM Editor - {currentFSM.MachineName}";

            // Clear welcome screen if it exists
            var welcomeScreen = graphViewContainer.Q("welcome-screen");
            if (welcomeScreen != null)
            {
                graphViewContainer.Remove(welcomeScreen);
            }

            // Refresh components
            graphView?.Refresh(currentFSM);
            propertiesPanel?.Refresh(currentFSM);
            toolbar?.Refresh(currentFSM);
        }

        /// <summary>
        /// Get current FSM
        /// </summary>
        public FSMDefinition GetCurrentFSM() => currentFSM;

        /// <summary>
        /// Check if FSM is loaded
        /// </summary>
        public bool HasFSM() => currentFSM != null;

        private void OnDestroy()
        {
            // Save on close if needed
            if (hasUnsavedChanges && currentFSM != null)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Save changes to '{currentFSM.MachineName}' before closing?",
                    "Save", "Don't Save"))
                {
                    SaveCurrentFSM();
                }
            }
        }
    }
}