using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UFSM.Runtime;
using UFSM.Runtime.Templates;
using UnityEditor.UIElements;
using UFSM.Core;

namespace UFSM.Editor
{
    /// <summary>
    /// Toolbar for FSM Editor
    /// </summary>
    public class FSMToolbar
    {
        private FSMEditorWindow window;
        private VisualElement root;

        // Toolbar buttons
        private Button newButton;
        private Button openButton;
        private Button saveButton;
        private Button addStateButton;
        private Button addTransitionButton;
        private Button addParameterButton;
        private ToolbarMenu templatesMenu;
        private ToolbarMenu viewMenu;

        public FSMToolbar(FSMEditorWindow window)
        {
            this.window = window;
            BuildToolbar();
        }

        private void BuildToolbar()
        {
            root = new VisualElement { name = "toolbar" };
            root.style.flexDirection = FlexDirection.Row;
            root.style.height = 30;
            root.style.borderBottomWidth = 1;
            root.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
            root.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            root.style.paddingLeft = 5;
            root.style.paddingRight = 5;

            // File operations
            newButton = CreateButton("New", "Create new FSM", OnNewClicked);
            openButton = CreateButton("Open", "Open existing FSM", OnOpenClicked);
            saveButton = CreateButton("Save", "Save current FSM", OnSaveClicked);

            AddSeparator();

            // Edit operations
            addStateButton = CreateButton("+ State", "Add new state", OnAddStateClicked);
            addTransitionButton = CreateButton("+ Transition", "Add new transition", OnAddTransitionClicked);
            addParameterButton = CreateButton("+ Parameter", "Add new parameter", OnAddParameterClicked);

            AddSeparator();

            // Templates menu
            templatesMenu = new ToolbarMenu { text = "Templates" };
            templatesMenu.menu.AppendAction("Apply Template/Basic 2-State", _ => ApplyTemplate("Basic2StateTemplate"));
            templatesMenu.menu.AppendAction("Apply Template/Basic 3-State", _ => ApplyTemplate("Basic3StateTemplate"));
            templatesMenu.menu.AppendAction("Apply Template/AI Patrol", _ => ApplyTemplate("AIPatrolTemplate"));
            templatesMenu.menu.AppendAction("Apply Template/Crop Life Cycle", _ => ApplyTemplate("CropLifeCycleTemplate"));
            root.Add(templatesMenu);

            AddSeparator();

            // View menu
            viewMenu = new ToolbarMenu { text = "View" };
            viewMenu.menu.AppendAction("Fit to Window", _ => FitToWindow());
            viewMenu.menu.AppendAction("Reset Zoom", _ => ResetZoom());
            viewMenu.menu.AppendSeparator();
            viewMenu.menu.AppendAction("Show Grid", a => ToggleGrid());
            root.Add(viewMenu);

            UpdateButtonStates();
        }

        private Button CreateButton(string text, string tooltip, System.Action callback)
        {
            var button = new Button(callback) { text = text, tooltip = tooltip };
            button.style.height = 24;
            button.style.marginTop = 3;
            button.style.marginBottom = 3;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            root.Add(button);
            return button;
        }

        private void AddSeparator()
        {
            var separator = new VisualElement();
            separator.style.width = 1;
            separator.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            separator.style.marginLeft = 5;
            separator.style.marginRight = 5;
            root.Add(separator);
        }

        // Button callbacks
        private void OnNewClicked() => window.CreateNewFSM();

        private void OnOpenClicked()
        {
            string path = EditorUtility.OpenFilePanel("Open FSM", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convert absolute path to relative
            path = "Assets" + path.Substring(Application.dataPath.Length);

            var fsm = AssetDatabase.LoadAssetAtPath<FSMDefinition>(path);
            if (fsm != null)
            {
                window.LoadFSM(fsm);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to load FSM", "OK");
            }
        }

        private void OnSaveClicked() => window.SaveCurrentFSM();

        private void OnAddStateClicked()
        {
            if (!window.HasFSM()) return;

            var fsm = window.GetCurrentFSM();
            var newState = new StateDefinition
            {
                StateName = GetUniqueStateName(fsm, "NewState")
            };

            fsm.States.Add(newState);
            window.MarkDirty();
            window.RefreshAll();
        }

        private void OnAddTransitionClicked()
        {
            if (!window.HasFSM()) return;

            var fsm = window.GetCurrentFSM();
            if (fsm.States.Count < 2)
            {
                EditorUtility.DisplayDialog("Add Transition", "Need at least 2 states to create a transition", "OK");
                return;
            }

            var newTransition = new TransitionDefinition
            {
                FromState = fsm.States[0].StateName,
                ToState = fsm.States.Count > 1 ? fsm.States[1].StateName : fsm.States[0].StateName
            };

            fsm.Transitions.Add(newTransition);
            window.MarkDirty();
            window.RefreshAll();
        }

        private void OnAddParameterClicked()
        {
            if (!window.HasFSM()) return;

            var fsm = window.GetCurrentFSM();
            var newParam = new ParameterDefinition
            {
                Name = GetUniqueParameterName(fsm, "NewParameter"),
                Type = ParameterType.Bool
            };

            fsm.Parameters.Add(newParam);
            window.MarkDirty();
            window.RefreshAll();
        }

        private void ApplyTemplate(string templateName)
        {
            if (!window.HasFSM()) return;

            // Find template
            string[] guids = AssetDatabase.FindAssets($"t:{templateName}");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Template Not Found", $"Could not find template: {templateName}", "OK");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var template = AssetDatabase.LoadAssetAtPath<FSMTemplate>(path);

            if (template != null)
            {
                if (EditorUtility.DisplayDialog("Apply Template",
                    $"Apply '{template.TemplateName}' template?\nThis will replace all current states, transitions, and parameters.",
                    "Apply", "Cancel"))
                {
                    template.ApplyTo(window.GetCurrentFSM());
                    window.MarkDirty();
                    window.RefreshAll();
                }
            }
        }

        private void FitToWindow()
        {
            // TODO: Implement in graph view
            Debug.Log("Fit to window");
        }

        private void ResetZoom()
        {
            // TODO: Implement in graph view
            Debug.Log("Reset zoom");
        }

        private void ToggleGrid()
        {
            // TODO: Implement in graph view
            Debug.Log("Toggle grid");
        }

        private string GetUniqueStateName(FSMDefinition fsm, string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (fsm.States.Exists(s => s.StateName == name))
            {
                name = $"{baseName}{counter}";
                counter++;
            }

            return name;
        }

        private string GetUniqueParameterName(FSMDefinition fsm, string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (fsm.Parameters.Exists(p => p.Name == name))
            {
                name = $"{baseName}{counter}";
                counter++;
            }

            return name;
        }

        private void UpdateButtonStates()
        {
            bool hasFSM = window.HasFSM();

            saveButton.SetEnabled(hasFSM);
            addStateButton.SetEnabled(hasFSM);
            addTransitionButton.SetEnabled(hasFSM);
            addParameterButton.SetEnabled(hasFSM);
            templatesMenu.SetEnabled(hasFSM);
        }

        public void Refresh(FSMDefinition fsm)
        {
            UpdateButtonStates();
        }

        public VisualElement GetRootElement() => root;
    }
}