// Assets/UFSM/Editor/FSMPropertiesPanel.cs
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UFSM.Runtime;
using UFSM.Core;

namespace UFSM.Editor
{
    /// <summary>
    /// Properties panel showing lists of states, transitions, and parameters
    /// GUID-aware and uses helper utilities for safe mutations.
    /// </summary>
    public class FSMPropertiesPanel
    {
        private FSMEditorWindow window;
        private VisualElement root;

        // Tabs
        private ToolbarToggle statesTab;
        private ToolbarToggle transitionsTab;
        private ToolbarToggle parametersTab;
        private ToolbarToggle settingsTab;

        // Content containers
        private VisualElement contentContainer;
        private ScrollView statesContainer;
        private ScrollView transitionsContainer;
        private ScrollView parametersContainer;
        private ScrollView settingsContainer;

        private FSMDefinition currentFSM;

        public FSMPropertiesPanel(FSMEditorWindow window)
        {
            this.window = window;
            BuildPanel();
        }

        private void BuildPanel()
        {
            root = new VisualElement { name = "properties-panel" };
            root.style.flexGrow = 1;

            // Tabs
            var tabBar = new Toolbar();
            tabBar.style.height = 30;

            statesTab = CreateTab("States", true);
            transitionsTab = CreateTab("Transitions", false);
            parametersTab = CreateTab("Parameters", false);
            settingsTab = CreateTab("Settings", false);

            tabBar.Add(statesTab);
            tabBar.Add(transitionsTab);
            tabBar.Add(parametersTab);
            tabBar.Add(settingsTab);

            root.Add(tabBar);

            // Content container
            contentContainer = new VisualElement { name = "content-container" };
            contentContainer.style.flexGrow = 1;
            root.Add(contentContainer);

            // Create content views
            statesContainer = CreateScrollView("states-list");
            transitionsContainer = CreateScrollView("transitions-list");
            parametersContainer = CreateScrollView("parameters-list");
            settingsContainer = CreateScrollView("settings-list");

            ShowStates();
        }

        private ToolbarToggle CreateTab(string text, bool defaultValue)
        {
            var toggle = new ToolbarToggle { text = text, value = defaultValue };
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    // Deselect other tabs
                    statesTab.value = toggle == statesTab;
                    transitionsTab.value = toggle == transitionsTab;
                    parametersTab.value = toggle == parametersTab;
                    settingsTab.value = toggle == settingsTab;

                    // Show corresponding content
                    if (toggle == statesTab) ShowStates();
                    else if (toggle == transitionsTab) ShowTransitions();
                    else if (toggle == parametersTab) ShowParameters();
                    else if (toggle == settingsTab) ShowSettings();
                }
            });
            return toggle;
        }

        private ScrollView CreateScrollView(string name)
        {
            var scroll = new ScrollView { name = name };
            scroll.style.flexGrow = 1;
            return scroll;
        }

        public void Refresh(FSMDefinition fsm)
        {
            currentFSM = fsm;

            // Ensure GUIDs & migrations are in place
            FSMEditorHelpers.EnsureStateGuids(currentFSM);
            FSMEditorHelpers.MigrateTransitionGuids(currentFSM);

            // Refresh current view
            if (statesTab.value) ShowStates();
            else if (transitionsTab.value) ShowTransitions();
            else if (parametersTab.value) ShowParameters();
            else if (settingsTab.value) ShowSettings();
        }

        private void ShowStates()
        {
            contentContainer.Clear();
            contentContainer.Add(statesContainer);
            statesContainer.Clear();

            if (currentFSM == null || currentFSM.States == null) return;

            // Header
            var header = new Label("States");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 10;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 5;
            statesContainer.Add(header);

            // Add state button
            var addButton = new Button(() => AddState()) { text = "+ Add State" };
            addButton.style.marginLeft = 10;
            addButton.style.marginRight = 10;
            addButton.style.marginBottom = 10;
            statesContainer.Add(addButton);

            // State list
            foreach (var state in currentFSM.States)
            {
                var stateElement = CreateStateElement(state);
                statesContainer.Add(stateElement);
            }
        }

        private VisualElement CreateStateElement(StateDefinition state)
        {
            var container = new VisualElement();
            ApplyCardStyle(container);

            // Header row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;

            var nameField = new TextField("Name") { value = state.StateName };
            nameField.style.flexGrow = 1;
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (currentFSM == null) return;
                var newName = evt.newValue?.Trim();
                if (string.IsNullOrEmpty(newName)) return;

                // reject duplicates
                if (currentFSM.States.Exists(s => s != state && s.StateName == newName)) return;

                FSMEditorHelpers.MarkDirty(currentFSM, "Rename State");
                state.StateName = newName;

                // Update transition name references (keep string fields for runtime compatibility)
                foreach (var trans in currentFSM.Transitions)
                {
                    if (trans.FromState == evt.previousValue) trans.FromState = newName;
                    if (trans.ToState == evt.previousValue) trans.ToState = newName;
                }

                window?.RefreshAll();
            });
            headerRow.Add(nameField);

            var deleteButton = new Button(() => DeleteState(state)) { text = "✖" };
            deleteButton.style.width = 30;
            headerRow.Add(deleteButton);

            container.Add(headerRow);

            // Description
            var descField = new TextField("Description") { value = state.Description, multiline = true };
            descField.style.height = 50;
            descField.RegisterValueChangedCallback(evt =>
            {
                if (currentFSM == null) return;
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit State Description");
                state.Description = evt.newValue;
            });
            container.Add(descField);

            // Priority
            var priorityField = new IntegerField("Priority") { value = state.Priority };
            priorityField.RegisterValueChangedCallback(evt =>
            {
                if (currentFSM == null) return;
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit State Priority");
                state.Priority = evt.newValue;
            });
            container.Add(priorityField);

            // Initial state checkbox
            var isInitial = state.StateName == currentFSM.InitialStateName;
            var initialToggle = new Toggle("Initial State") { value = isInitial };
            initialToggle.RegisterValueChangedCallback(evt =>
            {
                if (currentFSM == null) return;
                if (evt.newValue)
                {
                    FSMEditorHelpers.MarkDirty(currentFSM, "Set Initial State");
                    currentFSM.InitialStateName = state.StateName;
                    window?.RefreshAll();
                }
            });
            container.Add(initialToggle);

            return container;
        }

        private void ShowTransitions()
        {
            contentContainer.Clear();
            contentContainer.Add(transitionsContainer);
            transitionsContainer.Clear();

            if (currentFSM == null) return;

            // Header
            var header = new Label("Transitions");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 10;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 5;
            transitionsContainer.Add(header);

            // Add transition button
            var addButton = new Button(() => AddTransition()) { text = "+ Add Transition" };
            addButton.style.marginLeft = 10;
            addButton.style.marginRight = 10;
            addButton.style.marginBottom = 10;
            transitionsContainer.Add(addButton);

            // Transition list
            foreach (var transition in currentFSM.Transitions)
            {
                var transElement = CreateTransitionElement(transition);
                transitionsContainer.Add(transElement);
            }
        }

        private VisualElement CreateTransitionElement(TransitionDefinition transition)
        {
            var container = new VisualElement();
            ApplyCardStyle(container);

            // Header with delete button
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;

            var fromName = FSMEditorHelpers.FindStateByGuid(currentFSM, transition.FromStateGuid)?.StateName ?? transition.FromState ?? "(Any State)";
            var toName = FSMEditorHelpers.FindStateByGuid(currentFSM, transition.ToStateGuid)?.StateName ?? transition.ToState ?? "(Unknown)";

            var label = new Label($"{fromName} → {toName}");
            label.style.fontSize = 14;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(label);

            var deleteButton = new Button(() => DeleteTransition(transition)) { text = "✖" };
            deleteButton.style.width = 30;
            headerRow.Add(deleteButton);

            container.Add(headerRow);

            // From/To dropdowns
            var stateNames = currentFSM.States.ConvertAll(s => s.StateName);
            stateNames.Insert(0, "(Any State)");

            int fromIndex = 0;
            if (!string.IsNullOrEmpty(transition.FromStateGuid))
            {
                var s = FSMEditorHelpers.FindStateByGuid(currentFSM, transition.FromStateGuid);
                if (s != null) fromIndex = stateNames.IndexOf(s.StateName);
            }
            else if (!string.IsNullOrEmpty(transition.FromState))
            {
                fromIndex = stateNames.IndexOf(transition.FromState);
                if (fromIndex < 0) fromIndex = 0;
            }
            var fromField = new DropdownField("From", stateNames, Mathf.Clamp(fromIndex, 0, Mathf.Max(0, stateNames.Count - 1)));
            fromField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Change Transition From");
                if (evt.newValue == "(Any State)")
                {
                    transition.FromState = "";
                    transition.FromStateGuid = "";
                }
                else
                {
                    transition.FromState = evt.newValue;
                    var s = FSMEditorHelpers.FindStateByName(currentFSM, evt.newValue);
                    transition.FromStateGuid = s?.Guid ?? "";
                }
                window?.RefreshAll();
            });
            container.Add(fromField);

            var toChoices = currentFSM.States.ConvertAll(s => s.StateName);
            int toIndex = toChoices.IndexOf(transition.ToState);
            if (toIndex < 0 && !string.IsNullOrEmpty(transition.ToStateGuid))
            {
                var s = FSMEditorHelpers.FindStateByGuid(currentFSM, transition.ToStateGuid);
                if (s != null) toIndex = toChoices.IndexOf(s.StateName);
            }
            toIndex = Mathf.Clamp(toIndex, 0, Mathf.Max(0, toChoices.Count - 1));
            var toField = new DropdownField("To", toChoices, toIndex);
            toField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Change Transition To");
                transition.ToState = evt.newValue;
                var s = FSMEditorHelpers.FindStateByName(currentFSM, evt.newValue);
                transition.ToStateGuid = s?.Guid ?? "";
                window?.RefreshAll();
            });
            container.Add(toField);

            // Priority
            var priorityField = new IntegerField("Priority") { value = transition.Priority };
            priorityField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Transition Priority");
                transition.Priority = evt.newValue;
            });
            container.Add(priorityField);

            // Can interrupt
            var interruptToggle = new Toggle("Can Interrupt") { value = transition.CanInterrupt };
            interruptToggle.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Transition Interrupt");
                transition.CanInterrupt = evt.newValue;
            });
            container.Add(interruptToggle);

            // Delay
            var delayField = new FloatField("Delay (seconds)") { value = transition.TransitionDelay };
            delayField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Transition Delay");
                transition.TransitionDelay = evt.newValue;
            });
            container.Add(delayField);

            // ========== CONDITIONS SECTION (NEW) ==========

            var conditionsHeader = new Label("Conditions");
            conditionsHeader.style.marginTop = 10;
            conditionsHeader.style.fontSize = 12;
            conditionsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(conditionsHeader);

            // Logic operator
            // Logic operator
            var logicOperators = new List<string> { "AND", "OR", "NOT", "NAND", "NOR", "XOR" };
            var logicDropdown = new DropdownField("Logic Operator", logicOperators, (int)transition.LogicOperator);
            logicDropdown.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Change Logic Operator");
                int index = logicOperators.IndexOf(evt.newValue);
                transition.LogicOperator = (LogicOperator)index;
            });
            container.Add(logicDropdown);

            // Display existing inline conditions
            var conditionsContainer = new VisualElement();
            conditionsContainer.style.marginTop = 5;
            container.Add(conditionsContainer);

            void RefreshConditionsList()
            {
                conditionsContainer.Clear();

                // Show inline conditions
                if (transition.InlineConditions.Count > 0)
                {
                    var inlineHeader = new Label("Inline Conditions:");
                    inlineHeader.style.fontSize = 10;
                    inlineHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                    inlineHeader.style.marginTop = 5;
                    inlineHeader.style.marginBottom = 3;
                    conditionsContainer.Add(inlineHeader);

                    foreach (var condition in transition.InlineConditions)
                    {
                        var conditionElement = ConditionEditorUI.CreateConditionDisplay(
                            currentFSM,
                            condition,
                            () =>
                            {
                                transition.InlineConditions.Remove(condition);
                                FSMEditorHelpers.MarkDirty(currentFSM, "Remove Inline Condition");
                                RefreshConditionsList();
                            });
                        conditionsContainer.Add(conditionElement);
                    }
                }

                // Show scriptable conditions
                if (transition.ScriptableConditions.Count > 0)
                {
                    var scriptableHeader = new Label("Scriptable Conditions:");
                    scriptableHeader.style.fontSize = 10;
                    scriptableHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                    scriptableHeader.style.marginTop = 5;
                    scriptableHeader.style.marginBottom = 3;
                    conditionsContainer.Add(scriptableHeader);

                    foreach (var condition in transition.ScriptableConditions)
                    {
                        if (condition == null) continue; // Skip null references

                        var conditionElement = ConditionEditorUI.CreateScriptableConditionDisplay(
                            condition,
                            () =>
                            {
                                transition.ScriptableConditions.Remove(condition);
                                FSMEditorHelpers.MarkDirty(currentFSM, "Remove Scriptable Condition");
                                RefreshConditionsList();
                            });
                        conditionsContainer.Add(conditionElement);
                    }
                }

                // Show message if no conditions
                if (transition.InlineConditions.Count == 0 && transition.ScriptableConditions.Count == 0)
                {
                    var noConditionsLabel = new Label("No conditions (transition always fires)");
                    noConditionsLabel.style.fontSize = 10;
                    noConditionsLabel.style.marginTop = 5;
                    noConditionsLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                    conditionsContainer.Add(noConditionsLabel);
                }
            }

            RefreshConditionsList();

            // Add inline condition editor
            var inlineConditionEditor = ConditionEditorUI.CreateInlineConditionEditor(
                currentFSM,
                transition,
                RefreshConditionsList);
            container.Add(inlineConditionEditor);

            // Add scriptable condition editor
            var scriptableConditionEditor = ConditionEditorUI.CreateScriptableConditionEditor(
                currentFSM,
                transition,
                RefreshConditionsList);
            container.Add(scriptableConditionEditor);

            return container;
        }

        private void ShowParameters()
        {
            contentContainer.Clear();
            contentContainer.Add(parametersContainer);
            parametersContainer.Clear();

            if (currentFSM == null) return;

            // Header
            var header = new Label("Parameters");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 10;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 5;
            parametersContainer.Add(header);

            // Add parameter button
            var addButton = new Button(() => AddParameter()) { text = "+ Add Parameter" };
            addButton.style.marginLeft = 10;
            addButton.style.marginRight = 10;
            addButton.style.marginBottom = 10;
            parametersContainer.Add(addButton);

            // Parameter list
            foreach (var param in currentFSM.Parameters)
            {
                var paramElement = CreateParameterElement(param);
                parametersContainer.Add(paramElement);
            }
        }

        private VisualElement CreateParameterElement(ParameterDefinition param)
        {
            var container = new VisualElement();
            ApplyCardStyle(container);

            // Header row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;

            var nameField = new TextField("Name") { value = param.Name };
            nameField.style.flexGrow = 1;
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (currentFSM == null) return;
                if (!string.IsNullOrEmpty(evt.newValue) && !currentFSM.Parameters.Exists(p => p != param && p.Name == evt.newValue))
                {
                    FSMEditorHelpers.MarkDirty(currentFSM, "Rename Parameter");
                    param.Name = evt.newValue;
                }
            });
            headerRow.Add(nameField);

            var deleteButton = new Button(() => DeleteParameter(param)) { text = "✖" };
            deleteButton.style.width = 30;
            headerRow.Add(deleteButton);

            container.Add(headerRow);

            // Type
            var typeField = new EnumField("Type", param.Type);
            typeField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Change Parameter Type");
                param.Type = (ParameterType)evt.newValue;
                ShowParameters(); // Refresh to show appropriate default value field
            });
            container.Add(typeField);

            // Default value based on type
            switch (param.Type)
            {
                case ParameterType.Bool:
                    var boolField = new Toggle("Default Value") { value = param.DefaultBool };
                    boolField.RegisterValueChangedCallback(evt =>
                    {
                        FSMEditorHelpers.MarkDirty(currentFSM, "Edit Parameter Default Bool");
                        param.DefaultBool = evt.newValue;
                    });
                    container.Add(boolField);
                    break;

                case ParameterType.Int:
                    var intField = new IntegerField("Default Value") { value = param.DefaultInt };
                    intField.RegisterValueChangedCallback(evt =>
                    {
                        FSMEditorHelpers.MarkDirty(currentFSM, "Edit Parameter Default Int");
                        param.DefaultInt = evt.newValue;
                    });
                    container.Add(intField);
                    break;

                case ParameterType.Float:
                    var floatField = new FloatField("Default Value") { value = param.DefaultFloat };
                    floatField.RegisterValueChangedCallback(evt =>
                    {
                        FSMEditorHelpers.MarkDirty(currentFSM, "Edit Parameter Default Float");
                        param.DefaultFloat = evt.newValue;
                    });
                    container.Add(floatField);
                    break;

                case ParameterType.String:
                    var stringField = new TextField("Default Value") { value = param.DefaultString };
                    stringField.RegisterValueChangedCallback(evt =>
                    {
                        FSMEditorHelpers.MarkDirty(currentFSM, "Edit Parameter Default String");
                        param.DefaultString = evt.newValue;
                    });
                    container.Add(stringField);
                    break;
            }

            // Animator sync
            var syncToggle = new Toggle("Sync to Animator") { value = param.SyncToAnimator };
            syncToggle.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Toggle Param Sync");
                param.SyncToAnimator = evt.newValue;
            });
            container.Add(syncToggle);

            return container;
        }

        private void ShowSettings()
        {
            contentContainer.Clear();
            contentContainer.Add(settingsContainer);
            settingsContainer.Clear();

            if (currentFSM == null) return;

            // Header
            var header = new Label("FSM Settings");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 10;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 10;
            settingsContainer.Add(header);

            var container = new VisualElement();
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;

            // Machine name
            var nameField = new TextField("Machine Name") { value = currentFSM.MachineName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Machine Name");
                currentFSM.MachineName = evt.newValue;
            });
            container.Add(nameField);

            // Description
            var descField = new TextField("Description") { value = currentFSM.Description, multiline = true };
            descField.style.height = 80;
            descField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit FSM Description");
                currentFSM.Description = evt.newValue;
            });
            container.Add(descField);

            // Update mode
            var updateModeField = new EnumField("Update Mode", currentFSM.TransitionUpdateMode);
            updateModeField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Update Mode");
                currentFSM.TransitionUpdateMode = (UpdateMode)evt.newValue;
            });
            container.Add(updateModeField);

            // Check interval
            var intervalField = new FloatField("Check Interval") { value = currentFSM.TransitionCheckInterval };
            intervalField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Check Interval");
                currentFSM.TransitionCheckInterval = evt.newValue;
            });
            container.Add(intervalField);

            // Performance profile
            var profileField = new EnumField("Performance Profile", currentFSM.PerformanceProfile);
            profileField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit Performance Profile");
                currentFSM.PerformanceProfile = (PerformanceProfile)evt.newValue;
            });
            container.Add(profileField);

            // Priority
            var priorityField = new IntegerField("Priority") { value = currentFSM.Priority };
            priorityField.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Edit FSM Priority");
                currentFSM.Priority = evt.newValue;
            });
            container.Add(priorityField);

            // Animator sync
            var animSyncToggle = new Toggle("Sync With Animator") { value = currentFSM.SyncWithAnimator };
            animSyncToggle.RegisterValueChangedCallback(evt =>
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Toggle FSM Animator Sync");
                currentFSM.SyncWithAnimator = evt.newValue;
            });
            container.Add(animSyncToggle);

            settingsContainer.Add(container);
        }

        // Helper methods
        private void AddState()
        {
            if (currentFSM == null) return;

            FSMEditorHelpers.MarkDirty(currentFSM, "Add State");

            var newState = new StateDefinition
            {
                Guid = System.Guid.NewGuid().ToString(),
                StateName = FSMEditorHelpers.GetUniqueStateName(currentFSM, "NewState")
            };

            currentFSM.States.Add(newState);
            ShowStates();
            window?.RefreshAll();
        }

        private void DeleteState(StateDefinition state)
        {
            if (currentFSM == null || state == null) return;

            if (EditorUtility.DisplayDialog("Delete State",
                $"Delete state '{state.StateName}'?", "Delete", "Cancel"))
            {
                FSMEditorHelpers.MarkDirty(currentFSM, "Delete State");

                currentFSM.States.Remove(state);
                currentFSM.Transitions.RemoveAll(t => t.FromStateGuid == state.Guid || t.ToStateGuid == state.Guid
                                                     || t.FromState == state.StateName || t.ToState == state.StateName);

                ShowStates();
                window?.RefreshAll();
            }
        }

        private void AddTransition()
        {
            if (currentFSM == null || currentFSM.States.Count < 2) return;

            FSMEditorHelpers.MarkDirty(currentFSM, "Add Transition");

            var newTrans = new TransitionDefinition
            {
                FromStateGuid = currentFSM.States[0].Guid,
                FromState = currentFSM.States[0].StateName,
                ToStateGuid = currentFSM.States.Count > 1 ? currentFSM.States[1].Guid : currentFSM.States[0].Guid,
                ToState = currentFSM.States.Count > 1 ? currentFSM.States[1].StateName : currentFSM.States[0].StateName
            };

            currentFSM.Transitions.Add(newTrans);
            ShowTransitions();
            window?.RefreshAll();
        }

        private void DeleteTransition(TransitionDefinition transition)
        {
            if (currentFSM == null || transition == null) return;

            FSMEditorHelpers.MarkDirty(currentFSM, "Delete Transition");
            currentFSM.Transitions.Remove(transition);
            ShowTransitions();
            window?.RefreshAll();
        }

        private void AddParameter()
        {
            if (currentFSM == null) return;

            FSMEditorHelpers.MarkDirty(currentFSM, "Add Parameter");

            var newParam = new ParameterDefinition
            {
                Name = FSMEditorHelpers.GetUniqueParameterName(currentFSM, "NewParameter"),
                Type = ParameterType.Bool
            };

            currentFSM.Parameters.Add(newParam);
            ShowParameters();
        }

        private void DeleteParameter(ParameterDefinition param)
        {
            if (currentFSM == null || param == null) return;
            FSMEditorHelpers.MarkDirty(currentFSM, "Delete Parameter");
            currentFSM.Parameters.Remove(param);
            ShowParameters();
        }

        private void AddCondition(TransitionDefinition transition)
        {
            // TODO: Open condition picker dialog
            Debug.Log($"Add condition to transition: {transition.FromState} -> {transition.ToState}");
        }

        private void ApplyCardStyle(VisualElement e)
        {
            e.style.marginLeft = 10;
            e.style.marginRight = 10;
            e.style.marginBottom = 10;
            e.style.paddingLeft = 10;
            e.style.paddingRight = 10;
            e.style.paddingTop = 10;
            e.style.paddingBottom = 10;
            e.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
        }

        public VisualElement GetRootElement() => root;
    }
}
