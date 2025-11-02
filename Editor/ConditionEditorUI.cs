// Assets/UFSM/Editor/ConditionEditorUI.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UFSM.Runtime;
using UFSM.Core;

namespace UFSM.Editor
{
    /// <summary>
    /// Helper class for building inline condition UI elements
    /// </summary>
    public static class ConditionEditorUI
    {
        /// <summary>
        /// Creates a foldout section for adding inline conditions
        /// </summary>
        public static VisualElement CreateInlineConditionEditor(
            FSMDefinition fsm,
            TransitionDefinition transition,
            System.Action onConditionAdded)
        {
            var container = new VisualElement();
            container.style.marginTop = 10;
            container.style.marginBottom = 10;

            // Foldout for adding new condition
            var foldout = new Foldout { text = "+ Add Inline Condition", value = false };
            foldout.style.marginLeft = 0;

            // Condition type dropdown
            var conditionTypes = new List<string>
            {
                "Parameter Bool",
                "Parameter Int",
                "Parameter Float",
                "Parameter Trigger",
                "Timer",
                "Random"
            };

            var typeDropdown = new DropdownField("Condition Type", conditionTypes, 0);
            foldout.Add(typeDropdown);

            // Container for condition-specific fields (will change based on type)
            var configContainer = new VisualElement();
            configContainer.style.marginTop = 5;
            configContainer.style.paddingLeft = 10;
            configContainer.style.paddingRight = 10;
            configContainer.style.paddingTop = 5;
            configContainer.style.paddingBottom = 5;
            configContainer.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f));
            foldout.Add(configContainer);

            // Add button
            var addButton = new Button { text = "Add Condition" };
            addButton.style.marginTop = 10;
            foldout.Add(addButton);

            // Build initial config UI
            var currentCondition = new InlineConditionDefinition
            {
                Type = InlineConditionDefinition.ConditionType.ParameterBool
            };
            BuildConditionConfig(fsm, configContainer, currentCondition);

            // Handle type change
            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                currentCondition.Type = evt.newValue switch
                {
                    "Parameter Bool" => InlineConditionDefinition.ConditionType.ParameterBool,
                    "Parameter Int" => InlineConditionDefinition.ConditionType.ParameterInt,
                    "Parameter Float" => InlineConditionDefinition.ConditionType.ParameterFloat,
                    "Parameter Trigger" => InlineConditionDefinition.ConditionType.ParameterTrigger,
                    "Timer" => InlineConditionDefinition.ConditionType.Timer,
                    "Random" => InlineConditionDefinition.ConditionType.Random,
                    _ => InlineConditionDefinition.ConditionType.ParameterBool
                };

                configContainer.Clear();
                BuildConditionConfig(fsm, configContainer, currentCondition);
            });

            // Handle add button
            addButton.clicked += () =>
            {
                if (ValidateCondition(fsm, currentCondition))
                {
                    transition.InlineConditions.Add(CloneCondition(currentCondition));
                    FSMEditorHelpers.MarkDirty(fsm, "Add Inline Condition");
                    onConditionAdded?.Invoke();
                    foldout.value = false; // Collapse after adding
                }
            };

            container.Add(foldout);
            return container;
        }

        /// <summary>
        /// Builds configuration UI based on condition type
        /// </summary>
        private static void BuildConditionConfig(
            FSMDefinition fsm,
            VisualElement container,
            InlineConditionDefinition condition)
        {
            container.Clear();

            switch (condition.Type)
            {
                case InlineConditionDefinition.ConditionType.ParameterBool:
                    BuildParameterBoolConfig(fsm, container, condition);
                    break;

                case InlineConditionDefinition.ConditionType.ParameterInt:
                    BuildParameterIntConfig(fsm, container, condition);
                    break;

                case InlineConditionDefinition.ConditionType.ParameterFloat:
                    BuildParameterFloatConfig(fsm, container, condition);
                    break;

                case InlineConditionDefinition.ConditionType.ParameterTrigger:
                    BuildParameterTriggerConfig(fsm, container, condition);
                    break;

                case InlineConditionDefinition.ConditionType.Timer:
                    BuildTimerConfig(container, condition);
                    break;

                case InlineConditionDefinition.ConditionType.Random:
                    BuildRandomConfig(container, condition);
                    break;
            }
        }

        // ========== CONFIG BUILDERS ==========

        private static void BuildParameterBoolConfig(
            FSMDefinition fsm,
            VisualElement container,
            InlineConditionDefinition condition)
        {
            // Get bool/trigger parameters
            var boolParams = fsm.Parameters
                .Where(p => p.Type == ParameterType.Bool || p.Type == ParameterType.Trigger)
                .Select(p => p.Name)
                .ToList();

            if (boolParams.Count == 0)
            {
                container.Add(new Label("No Bool/Trigger parameters found. Add one first."));
                return;
            }

            // Parameter dropdown
            int currentIndex = boolParams.IndexOf(condition.ParameterName);
            if (currentIndex < 0) currentIndex = 0;

            var paramDropdown = new DropdownField("Parameter Name", boolParams, currentIndex);
            paramDropdown.RegisterValueChangedCallback(evt => condition.ParameterName = evt.newValue);
            container.Add(paramDropdown);

            // Set initial value
            if (string.IsNullOrEmpty(condition.ParameterName))
                condition.ParameterName = boolParams[0];

            // Expected value toggle
            var valueToggle = new Toggle("Expected Value") { value = condition.BoolValue };
            valueToggle.RegisterValueChangedCallback(evt => condition.BoolValue = evt.newValue);
            container.Add(valueToggle);
        }

        private static void BuildParameterIntConfig(
     FSMDefinition fsm,
     VisualElement container,
     InlineConditionDefinition condition)
        {
            // Get int parameters
            var intParams = fsm.Parameters
                .Where(p => p.Type == ParameterType.Int)
                .Select(p => p.Name)
                .ToList();

            if (intParams.Count == 0)
            {
                container.Add(new Label("No Int parameters found. Add one first."));
                return;
            }

            // Parameter dropdown
            int currentIndex = intParams.IndexOf(condition.ParameterName);
            if (currentIndex < 0) currentIndex = 0;

            var paramDropdown = new DropdownField("Parameter Name", intParams, currentIndex);
            paramDropdown.RegisterValueChangedCallback(evt => condition.ParameterName = evt.newValue);
            container.Add(paramDropdown);

            if (string.IsNullOrEmpty(condition.ParameterName))
                condition.ParameterName = intParams[0];

            // Comparison type
            var comparisonTypes = new List<string> { "Equal", "NotEqual", "Greater", "GreaterOrEqual", "Less", "LessOrEqual" };
            var comparisonDropdown = new DropdownField("Comparison", comparisonTypes, (int)condition.IntComparisonType);
            comparisonDropdown.RegisterValueChangedCallback(evt =>
            {
                // FIX: Convert string to enum by finding index
                int index = comparisonTypes.IndexOf(evt.newValue);
                condition.IntComparisonType = (InlineConditionDefinition.IntComparison)index;
            });
            container.Add(comparisonDropdown);

            // Value
            var valueField = new IntegerField("Value") { value = condition.IntValue };
            valueField.RegisterValueChangedCallback(evt => condition.IntValue = evt.newValue);
            container.Add(valueField);
        }

        private static void BuildParameterFloatConfig(
    FSMDefinition fsm,
    VisualElement container,
    InlineConditionDefinition condition)
        {
            // Get float parameters
            var floatParams = fsm.Parameters
                .Where(p => p.Type == ParameterType.Float)
                .Select(p => p.Name)
                .ToList();

            if (floatParams.Count == 0)
            {
                container.Add(new Label("No Float parameters found. Add one first."));
                return;
            }

            // Parameter dropdown
            int currentIndex = floatParams.IndexOf(condition.ParameterName);
            if (currentIndex < 0) currentIndex = 0;

            var paramDropdown = new DropdownField("Parameter Name", floatParams, currentIndex);
            paramDropdown.RegisterValueChangedCallback(evt => condition.ParameterName = evt.newValue);
            container.Add(paramDropdown);

            if (string.IsNullOrEmpty(condition.ParameterName))
                condition.ParameterName = floatParams[0];

            // Comparison type
            var comparisonTypes = new List<string> { "Greater", "GreaterOrEqual", "Less", "LessOrEqual", "InRange" };
            var comparisonDropdown = new DropdownField("Comparison", comparisonTypes, (int)condition.FloatComparisonType);

            // Container for value fields (will change if InRange is selected)
            var valueContainer = new VisualElement();
            container.Add(valueContainer);

            void UpdateValueFields()
            {
                valueContainer.Clear();

                if (condition.FloatComparisonType == InlineConditionDefinition.FloatComparison.InRange)
                {
                    var minField = new FloatField("Range Min") { value = condition.FloatRangeMin };
                    minField.RegisterValueChangedCallback(evt => condition.FloatRangeMin = evt.newValue);
                    valueContainer.Add(minField);

                    var maxField = new FloatField("Range Max") { value = condition.FloatRangeMax };
                    maxField.RegisterValueChangedCallback(evt => condition.FloatRangeMax = evt.newValue);
                    valueContainer.Add(maxField);
                }
                else
                {
                    var valueField = new FloatField("Value") { value = condition.FloatValue };
                    valueField.RegisterValueChangedCallback(evt => condition.FloatValue = evt.newValue);
                    valueContainer.Add(valueField);
                }
            }

            comparisonDropdown.RegisterValueChangedCallback(evt =>
            {
                // FIX: Convert string to enum by finding index
                int index = comparisonTypes.IndexOf(evt.newValue);
                condition.FloatComparisonType = (InlineConditionDefinition.FloatComparison)index;
                UpdateValueFields(); // Refresh value fields based on comparison type
            });
            container.Add(comparisonDropdown);

            // Initial value fields
            UpdateValueFields();
        }

        private static void BuildParameterTriggerConfig(
            FSMDefinition fsm,
            VisualElement container,
            InlineConditionDefinition condition)
        {
            // Get trigger parameters
            var triggerParams = fsm.Parameters
                .Where(p => p.Type == ParameterType.Trigger)
                .Select(p => p.Name)
                .ToList();

            if (triggerParams.Count == 0)
            {
                container.Add(new Label("No Trigger parameters found. Add one first."));
                return;
            }

            // Parameter dropdown
            int currentIndex = triggerParams.IndexOf(condition.ParameterName);
            if (currentIndex < 0) currentIndex = 0;

            var paramDropdown = new DropdownField("Trigger Name", triggerParams, currentIndex);
            paramDropdown.RegisterValueChangedCallback(evt => condition.ParameterName = evt.newValue);
            container.Add(paramDropdown);

            if (string.IsNullOrEmpty(condition.ParameterName))
                condition.ParameterName = triggerParams[0];

            container.Add(new Label("Condition passes when trigger is set."));
        }

        private static void BuildTimerConfig(VisualElement container, InlineConditionDefinition condition)
        {
            var durationField = new FloatField("Duration (seconds)") { value = condition.TimerDuration };
            durationField.RegisterValueChangedCallback(evt => condition.TimerDuration = evt.newValue);
            container.Add(durationField);

            container.Add(new Label("Timer starts when state is entered."));
        }

        private static void BuildRandomConfig(VisualElement container, InlineConditionDefinition condition)
        {
            var chanceSlider = new Slider("Chance", 0f, 1f) { value = condition.RandomChance };
            chanceSlider.RegisterValueChangedCallback(evt => condition.RandomChance = evt.newValue);
            container.Add(chanceSlider);

            var chanceLabel = new Label($"{(condition.RandomChance * 100):F0}% chance");
            chanceSlider.RegisterValueChangedCallback(evt =>
            {
                chanceLabel.text = $"{(evt.newValue * 100):F0}% chance";
            });
            container.Add(chanceLabel);
        }

        // ========== HELPERS ==========

        private static bool ValidateCondition(FSMDefinition fsm, InlineConditionDefinition condition)
        {
            switch (condition.Type)
            {
                case InlineConditionDefinition.ConditionType.ParameterBool:
                case InlineConditionDefinition.ConditionType.ParameterInt:
                case InlineConditionDefinition.ConditionType.ParameterFloat:
                case InlineConditionDefinition.ConditionType.ParameterTrigger:
                    if (string.IsNullOrEmpty(condition.ParameterName))
                    {
                        Debug.LogWarning("Parameter name is empty");
                        return false;
                    }
                    break;

                case InlineConditionDefinition.ConditionType.Timer:
                    if (condition.TimerDuration <= 0f)
                    {
                        Debug.LogWarning("Timer duration must be greater than 0");
                        return false;
                    }
                    break;

                case InlineConditionDefinition.ConditionType.Random:
                    if (condition.RandomChance < 0f || condition.RandomChance > 1f)
                    {
                        Debug.LogWarning("Random chance must be between 0 and 1");
                        return false;
                    }
                    break;
            }

            return true;
        }

        private static InlineConditionDefinition CloneCondition(InlineConditionDefinition source)
        {
            return new InlineConditionDefinition
            {
                Type = source.Type,
                ParameterName = source.ParameterName,
                BoolValue = source.BoolValue,
                IntComparisonType = source.IntComparisonType,
                IntValue = source.IntValue,
                FloatComparisonType = source.FloatComparisonType,
                FloatValue = source.FloatValue,
                FloatRangeMin = source.FloatRangeMin,
                FloatRangeMax = source.FloatRangeMax,
                TimerDuration = source.TimerDuration,
                RandomChance = source.RandomChance
            };
        }

        /// <summary>
        /// Creates a display element for an existing inline condition
        /// </summary>
        public static VisualElement CreateConditionDisplay(
            FSMDefinition fsm,
            InlineConditionDefinition condition,
            System.Action onRemove)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.marginBottom = 5;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;
            container.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));

            // Condition description
            var description = GetConditionDescription(condition);
            var label = new Label(description);
            label.style.flexGrow = 1;
            container.Add(label);

            // Remove button
            var removeButton = new Button { text = "✖" };
            removeButton.style.width = 30;
            removeButton.clicked += () => onRemove?.Invoke();
            container.Add(removeButton);

            return container;
        }

        private static string GetConditionDescription(InlineConditionDefinition condition)
        {
            return condition.Type switch
            {
                InlineConditionDefinition.ConditionType.ParameterBool =>
                    $"{condition.ParameterName} == {condition.BoolValue}",

                InlineConditionDefinition.ConditionType.ParameterInt =>
                    $"{condition.ParameterName} {GetIntComparisonSymbol(condition.IntComparisonType)} {condition.IntValue}",

                InlineConditionDefinition.ConditionType.ParameterFloat =>
                    condition.FloatComparisonType == InlineConditionDefinition.FloatComparison.InRange
                        ? $"{condition.ParameterName} in [{condition.FloatRangeMin}, {condition.FloatRangeMax}]"
                        : $"{condition.ParameterName} {GetFloatComparisonSymbol(condition.FloatComparisonType)} {condition.FloatValue}",

                InlineConditionDefinition.ConditionType.ParameterTrigger =>
                    $"Trigger: {condition.ParameterName}",

                InlineConditionDefinition.ConditionType.Timer =>
                    $"Timer: {condition.TimerDuration}s",

                InlineConditionDefinition.ConditionType.Random =>
                    $"Random: {condition.RandomChance * 100:F0}%",

                _ => "Unknown Condition"
            };
        }

        private static string GetIntComparisonSymbol(InlineConditionDefinition.IntComparison comparison)
        {
            return comparison switch
            {
                InlineConditionDefinition.IntComparison.Equal => "==",
                InlineConditionDefinition.IntComparison.NotEqual => "!=",
                InlineConditionDefinition.IntComparison.Greater => ">",
                InlineConditionDefinition.IntComparison.GreaterOrEqual => ">=",
                InlineConditionDefinition.IntComparison.Less => "<",
                InlineConditionDefinition.IntComparison.LessOrEqual => "<=",
                _ => "?"
            };
        }

        private static string GetFloatComparisonSymbol(InlineConditionDefinition.FloatComparison comparison)
        {
            return comparison switch
            {
                InlineConditionDefinition.FloatComparison.Greater => ">",
                InlineConditionDefinition.FloatComparison.GreaterOrEqual => ">=",
                InlineConditionDefinition.FloatComparison.Less => "<",
                InlineConditionDefinition.FloatComparison.LessOrEqual => "<=",
                _ => "?"
            };
        }

        /// <summary>
        /// Creates a section for adding ScriptableConditions
        /// </summary>
        public static VisualElement CreateScriptableConditionEditor(
            FSMDefinition fsm,
            TransitionDefinition transition,
            System.Action onConditionAdded)
        {
            var container = new VisualElement();
            container.style.marginTop = 10;

            // Foldout for adding scriptable condition
            var foldout = new Foldout { text = "+ Add Scriptable Condition", value = false };
            foldout.style.marginLeft = 0;

            // Find all ScriptableCondition assets in project
            var allConditions = FindAllScriptableConditions();

            if (allConditions.Count == 0)
            {
                foldout.Add(new Label("No ScriptableConditions found in project.\nCreate one via: Assets > Create > UFSM > Conditions"));
                container.Add(foldout);
                return container;
            }

            // Dropdown of available conditions
            var conditionNames = allConditions.Select(c => c.name).ToList();
            var dropdown = new DropdownField("Select Condition", conditionNames, 0);
            foldout.Add(dropdown);

            // Preview of selected condition
            var previewLabel = new Label();
            previewLabel.style.marginTop = 5;
            previewLabel.style.fontSize = 10;
            previewLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            foldout.Add(previewLabel);

            void UpdatePreview(string conditionName)
            {
                var condition = allConditions.FirstOrDefault(c => c.name == conditionName);
                if (condition != null)
                {
                    previewLabel.text = string.IsNullOrEmpty(condition.Description)
                        ? "(No description)"
                        : condition.Description;
                }
            }

            UpdatePreview(conditionNames[0]);
            dropdown.RegisterValueChangedCallback(evt => UpdatePreview(evt.newValue));

            // Add button
            var addButton = new Button { text = "Add Condition" };
            addButton.style.marginTop = 10;
            addButton.clicked += () =>
            {
                var selectedCondition = allConditions.FirstOrDefault(c => c.name == dropdown.value);
                if (selectedCondition != null)
                {
                    // Check if already added
                    if (transition.ScriptableConditions.Contains(selectedCondition))
                    {
                        UnityEditor.EditorUtility.DisplayDialog("Duplicate Condition",
                            $"'{selectedCondition.name}' is already added to this transition.",
                            "OK");
                        return;
                    }

                    transition.ScriptableConditions.Add(selectedCondition);
                    FSMEditorHelpers.MarkDirty(fsm, "Add Scriptable Condition");
                    onConditionAdded?.Invoke();
                    foldout.value = false;
                }
            };
            foldout.Add(addButton);

            container.Add(foldout);
            return container;
        }

        /// <summary>
        /// Creates a display element for a ScriptableCondition
        /// </summary>
        public static VisualElement CreateScriptableConditionDisplay(
            ScriptableCondition condition,
            System.Action onRemove)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.marginBottom = 5;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;
            container.style.backgroundColor = new StyleColor(new Color(0.25f, 0.3f, 0.25f)); // Slightly green tint

            var leftSide = new VisualElement();
            leftSide.style.flexGrow = 1;

            // Condition name
            var nameLabel = new Label($"📦 {condition.name}");
            nameLabel.style.fontSize = 11;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftSide.Add(nameLabel);

            // Description (if any)
            if (!string.IsNullOrEmpty(condition.Description))
            {
                var descLabel = new Label(condition.Description);
                descLabel.style.fontSize = 9;
                descLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                leftSide.Add(descLabel);
            }

            container.Add(leftSide);

            // Remove button
            var removeButton = new Button { text = "✖" };
            removeButton.style.width = 30;
            removeButton.clicked += () => onRemove?.Invoke();
            container.Add(removeButton);

            return container;
        }

        /// <summary>
        /// Finds all ScriptableCondition assets in the project
        /// </summary>
        private static List<ScriptableCondition> FindAllScriptableConditions()
        {
            var conditions = new List<ScriptableCondition>();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableCondition");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var condition = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableCondition>(path);
                if (condition != null)
                {
                    conditions.Add(condition);
                }
            }
#endif

            return conditions;
        }

    }
}