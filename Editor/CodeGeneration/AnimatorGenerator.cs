using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UFSM.Runtime;
using System.Collections.Generic;
using UFSM.Core;

namespace UFSM.Editor.CodeGeneration
{
    /// <summary>
    /// Automatically generates/updates Animator Controller parameters from FSM
    /// </summary>
    public static class AnimatorGenerator
    {
        /// <summary>
        /// Create or update Animator Controller for FSM
        /// </summary>
        public static AnimatorController GenerateAnimatorController(FSMDefinition fsm, bool createIfMissing = true)
        {
            if (fsm == null) return null;

            AnimatorController controller = fsm.AnimatorController as AnimatorController;

            // Create new controller if needed
            if (controller == null && createIfMissing)
            {
                string path = $"Assets/Generated/{fsm.MachineName}/{fsm.MachineName}Animator.controller";

                // Ensure directory exists
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                fsm.AnimatorController = controller;

                Debug.Log($"[Animator Generator] Created Animator Controller at {path}");
            }

            if (controller == null) return null;

            // Update parameters
            UpdateAnimatorParameters(controller, fsm);

            // Update states (optional - creates basic animation states)
            if (fsm.SyncWithAnimator)
            {
                UpdateAnimatorStates(controller, fsm);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return controller;
        }

        /// <summary>
        /// Update Animator parameters to match FSM parameters
        /// </summary>
        private static void UpdateAnimatorParameters(AnimatorController controller, FSMDefinition fsm)
        {
            // Get existing parameters
            var existingParams = new HashSet<string>();
            foreach (var param in controller.parameters)
            {
                existingParams.Add(param.name);
            }

            // Add missing parameters
            foreach (var paramDef in fsm.Parameters)
            {
                if (!paramDef.SyncToAnimator) continue;

                string paramName = string.IsNullOrEmpty(paramDef.AnimatorParameterName)
                    ? paramDef.Name
                    : paramDef.AnimatorParameterName;

                if (!existingParams.Contains(paramName))
                {
                    AnimatorControllerParameterType animType = GetAnimatorParameterType(paramDef.Type);
                    controller.AddParameter(paramName, animType);

                    // Set default value
                    for (int i = 0; i < controller.parameters.Length; i++)
                    {
                        if (controller.parameters[i].name == paramName)
                        {
                            var param = controller.parameters[i];

                            switch (paramDef.Type)
                            {
                                case ParameterType.Bool:
                                    param.defaultBool = paramDef.DefaultBool;
                                    break;
                                case ParameterType.Int:
                                    param.defaultInt = paramDef.DefaultInt;
                                    break;
                                case ParameterType.Float:
                                    param.defaultFloat = paramDef.DefaultFloat;
                                    break;
                            }

                            controller.parameters[i] = param;
                            break;
                        }
                    }

                    Debug.Log($"[Animator Generator] Added parameter: {paramName} ({animType})");
                }
            }

            // Remove obsolete parameters (optional - commented out for safety)
            /*
            var fsmParamNames = new HashSet<string>();
            foreach (var paramDef in fsm.Parameters)
            {
                if (paramDef.SyncToAnimator)
                {
                    string paramName = string.IsNullOrEmpty(paramDef.AnimatorParameterName) 
                        ? paramDef.Name 
                        : paramDef.AnimatorParameterName;
                    fsmParamNames.Add(paramName);
                }
            }
            
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
            {
                if (!fsmParamNames.Contains(controller.parameters[i].name))
                {
                    controller.RemoveParameter(i);
                    Debug.Log($"[Animator Generator] Removed obsolete parameter: {controller.parameters[i].name}");
                }
            }
            */
        }

        /// <summary>
        /// Create basic animation states in Animator (optional)
        /// </summary>
        private static void UpdateAnimatorStates(AnimatorController controller, FSMDefinition fsm)
        {
            var rootStateMachine = controller.layers[0].stateMachine;

            // Get existing states
            var existingStates = new HashSet<string>();
            foreach (var state in rootStateMachine.states)
            {
                existingStates.Add(state.state.name);
            }

            // Add missing states
            int stateIndex = 0;
            foreach (var stateDef in fsm.States)
            {
                if (!existingStates.Contains(stateDef.StateName))
                {
                    // Calculate position
                    Vector3 position = new Vector3(100 + (stateIndex % 3) * 250, 100 + (stateIndex / 3) * 100, 0);

                    // Create state
                    var animState = rootStateMachine.AddState(stateDef.StateName, position);

                    // Set as default if it's the initial state
                    if (stateDef.StateName == fsm.InitialStateName)
                    {
                        rootStateMachine.defaultState = animState;
                    }

                    Debug.Log($"[Animator Generator] Added animation state: {stateDef.StateName}");
                }

                stateIndex++;
            }
        }

        private static AnimatorControllerParameterType GetAnimatorParameterType(ParameterType fsmType)
        {
            return fsmType switch
            {
                ParameterType.Bool => AnimatorControllerParameterType.Bool,
                ParameterType.Int => AnimatorControllerParameterType.Int,
                ParameterType.Float => AnimatorControllerParameterType.Float,
                ParameterType.Trigger => AnimatorControllerParameterType.Trigger,
                _ => AnimatorControllerParameterType.Float
            };
        }

        /// <summary>
        /// Menu item to generate animator for selected FSM
        /// </summary>
        [MenuItem("Assets/UFSM/Generate Animator Controller", true)]
        private static bool ValidateGenerateAnimator()
        {
            return Selection.activeObject is FSMDefinition;
        }

        [MenuItem("Assets/UFSM/Generate Animator Controller")]
        private static void GenerateAnimatorFromMenu()
        {
            var fsm = Selection.activeObject as FSMDefinition;
            if (fsm != null)
            {
                var controller = GenerateAnimatorController(fsm, true);
                if (controller != null)
                {
                    EditorUtility.DisplayDialog("Animator Generated",
                        $"Successfully generated Animator Controller for '{fsm.MachineName}'\n\n" +
                        $"Location: {AssetDatabase.GetAssetPath(controller)}\n\n" +
                        $"Parameters created: {controller.parameters.Length}",
                        "OK");

                    // Select the controller
                    Selection.activeObject = controller;
                    EditorGUIUtility.PingObject(controller);
                }
            }
        }
    }
}