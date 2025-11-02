
// ============================================================================
// UFSM - Universal Finite State Machine System for Unity
// Phase 2: ScriptableObject-Based FSM System
// File: Assets/UFSM/Runtime/ScriptableObjects/FSMDefinition.cs
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// ScriptableObject that defines an FSM structure (states, transitions, parameters)
    /// This is the asset you create and configure in the Unity Editor
    /// </summary>
    [CreateAssetMenu(fileName = "NewFSM", menuName = "UFSM/FSM Definition", order = 1)]
    public class FSMDefinition : ScriptableObject
    {
        [Header("FSM Info")]
        public string MachineName = "New FSM";
        [TextArea(2, 4)]
        public string Description;

        [Header("Update Settings")]
        public UpdateMode TransitionUpdateMode = UpdateMode.EveryFrame;
        [Range(0.01f, 5f)]
        public float TransitionCheckInterval = 0.1f;
        public PerformanceProfile PerformanceProfile = PerformanceProfile.Balanced;
        public int Priority = 0;

        [Header("States")]
        public List<StateDefinition> States = new List<StateDefinition>();

        [Header("Transitions")]
        public List<TransitionDefinition> Transitions = new List<TransitionDefinition>();

        [Header("Parameters")]
        public List<ParameterDefinition> Parameters = new List<ParameterDefinition>();

        [Header("Animator Integration")]
        public bool SyncWithAnimator = false;
        public RuntimeAnimatorController AnimatorController;
        public bool AutoCreateAnimatorParameters = true;

        [Header("Initial State")]
        public string InitialStateName = "Idle";

        // Validation
        private void OnValidate()
        {
            // Ensure we have at least one state
            if (States.Count == 0)
            {
                States.Add(new StateDefinition { StateName = "Idle" });
            }

            // Validate initial state exists
            if (!string.IsNullOrEmpty(InitialStateName))
            {
                bool found = States.Exists(s => s.StateName == InitialStateName);
                if (!found && States.Count > 0)
                {
                    InitialStateName = States[0].StateName;
                }
            }
        }

        // Helper methods
        public StateDefinition GetState(string stateName)
        {
            return States.Find(s => s.StateName == stateName);
        }

        public List<TransitionDefinition> GetTransitionsFromState(string stateName)
        {
            return Transitions.FindAll(t => t.FromState == stateName || string.IsNullOrEmpty(t.FromState));
        }

        public ParameterDefinition GetParameter(string paramName)
        {
            return Parameters.Find(p => p.Name == paramName);
        }
    }

    /// <summary>
    /// Defines a state in the FSM
    /// </summary>
    [Serializable]
    public partial class StateDefinition
    {
        public string StateName = "NewState";
        [TextArea(1, 3)]
        public string Description;
        public int Priority = 0;

        [Header("State Behavior References")]
        [Tooltip("Optional: Reference to a MonoBehaviour that implements state logic")]
        public MonoBehaviour StateBehaviorScript;

        [Tooltip("Optional: ScriptableObject containing state behavior")]
        public ScriptableStateBehavior StateBehavior;

        [Header("Animation")]
        public string AnimationStateName;
        public bool TriggerAnimationOnEnter = false;
        public string AnimationTriggerParameter;

        [Header("Audio")]
        public AudioClip EnterSound;
        public AudioClip ExitSound;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnEnterEvent;
        public UnityEngine.Events.UnityEvent OnExitEvent;
    }

    /// <summary>
    /// Defines a transition between states
    /// </summary>
    [Serializable]
    public partial class TransitionDefinition
    {
        [Tooltip("Source state (leave empty for 'Any State')")]
        public string FromState = "";

        [Tooltip("Target state")]
        public string ToState = "";

        [Header("Settings")]
        public int Priority = 0;
        public bool CanInterrupt = true;

        [Range(0f, 10f)]
        public float TransitionDelay = 0f;

        [Header("Conditions")]
        public LogicOperator LogicOperator = LogicOperator.AND;

        [Tooltip("Reusable ScriptableObject conditions")]
        public List<ScriptableCondition> ScriptableConditions = new List<ScriptableCondition>();

        [Tooltip("Inline conditions (defined in inspector)")]
        public List<InlineConditionDefinition> InlineConditions = new List<InlineConditionDefinition>();

        [Header("Callbacks")]
        public UnityEngine.Events.UnityEvent OnTransitionTriggered;
    }

    /// <summary>
    /// Defines an inline condition (not a ScriptableObject)
    /// </summary>
    [Serializable]
    public class InlineConditionDefinition
    {
        public enum ConditionType
        {
            ParameterBool,
            ParameterInt,
            ParameterFloat,
            ParameterTrigger,
            Timer,
            Random
        }

        public ConditionType Type;
        public string ParameterName;

        // For bool
        public bool BoolValue;

        // For int
        public enum IntComparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
        public IntComparison IntComparisonType;
        public int IntValue;

        // For float
        public enum FloatComparison { Greater, GreaterOrEqual, Less, LessOrEqual, InRange }
        public FloatComparison FloatComparisonType;
        public float FloatValue;
        public float FloatRangeMin;
        public float FloatRangeMax;

        // For timer
        public float TimerDuration;

        // For random
        [Range(0f, 1f)]
        public float RandomChance = 0.5f;
    }

    /// <summary>
    /// Defines a parameter
    /// </summary>
    [Serializable]
    public class ParameterDefinition
    {
        public string Name = "NewParameter";
        public ParameterType Type = ParameterType.Bool;

        [Header("Default Value")]
        public bool DefaultBool = false;
        public int DefaultInt = 0;
        public float DefaultFloat = 0f;
        public string DefaultString = "";

        [Header("Animator Sync")]
        public bool SyncToAnimator = true;
        public string AnimatorParameterName; // If different from Name

        public object GetDefaultValue()
        {
            return Type switch
            {
                ParameterType.Bool => DefaultBool,
                ParameterType.Int => DefaultInt,
                ParameterType.Float => DefaultFloat,
                ParameterType.String => DefaultString,
                ParameterType.Trigger => false,
                _ => null
            };
        }
    }
}
