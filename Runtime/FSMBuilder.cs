using System.Collections.Generic;
using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Builds a runtime FSM from a ScriptableObject FSMDefinition
    /// </summary>
    public static class FSMBuilder
    {
        public static IStateMachine BuildFromDefinition(FSMDefinition definition, IContext context)
        {
            // Create runtime FSM
            var fsm = new RuntimeStateMachine(definition.MachineName, context);

            // Configure settings
            fsm.TransitionUpdateMode = definition.TransitionUpdateMode;
            fsm.TransitionCheckInterval = definition.TransitionCheckInterval;
            fsm.Profile = definition.PerformanceProfile;
            fsm.Priority = definition.Priority;

            // Initialize parameter store
            fsm.Initialize();

            // Add parameters
            foreach (var paramDef in definition.Parameters)
            {
                fsm.GetParameterStore().AddParameter(
                    paramDef.Name,
                    paramDef.Type,
                    paramDef.GetDefaultValue()
                );
            }

            // Register states
            foreach (var stateDef in definition.States)
            {
                var state = CreateRuntimeState(stateDef, context, definition);
                fsm.RegisterState(state);
            }

            // Register transitions
            foreach (var transDef in definition.Transitions)
            {
                var transition = CreateRuntimeTransition(transDef, context, fsm);
                fsm.RegisterTransition(transition);
            }

            return fsm;
        }

        private static IState<IContext> CreateRuntimeState(StateDefinition stateDef, IContext context, FSMDefinition fsmDef)
        {
            // If has ScriptableObject behavior, use it
            if (stateDef.StateBehavior != null)
            {
                return new ScriptableObjectState(stateDef, context);
            }

            // If has MonoBehaviour script reference, use it
            if (stateDef.StateBehaviorScript != null && stateDef.StateBehaviorScript is IState<IContext> stateScript)
            {
                return stateScript;
            }

            // Default: create runtime state
            return new RuntimeState(stateDef, context);
        }

        private static ITransition<IContext> CreateRuntimeTransition(
            TransitionDefinition transDef,
            IContext context,
            IStateMachine fsm)
        {
            var transition = new TransitionBase<IContext>(transDef.FromState, transDef.ToState)
            {
                Priority = transDef.Priority,
                CanInterrupt = transDef.CanInterrupt,
                TransitionDelay = transDef.TransitionDelay,
                LogicOperator = transDef.LogicOperator,
                Context = context
            };

            // Add ScriptableObject conditions
            foreach (var scriptableCondition in transDef.ScriptableConditions)
            {
                if (scriptableCondition != null)
                {
                    scriptableCondition.SetContext(context);
                    transition.AddCondition(new ScriptableConditionWrapper(scriptableCondition));
                }
            }

            // Add inline conditions
            foreach (var inlineCondition in transDef.InlineConditions)
            {
                var condition = CreateInlineCondition(inlineCondition, context);
                transition.AddCondition(condition);
            }

            return transition;
        }

        private static ICondition<IContext> CreateInlineCondition(InlineConditionDefinition def, IContext context)
        {
            return def.Type switch
            {
                InlineConditionDefinition.ConditionType.ParameterBool =>
                    new InlineParameterBoolCondition(def.ParameterName, def.BoolValue) { Context = context },

                InlineConditionDefinition.ConditionType.ParameterInt =>
                    new InlineParameterIntCondition(def.ParameterName, def.IntComparisonType, def.IntValue) { Context = context },

                InlineConditionDefinition.ConditionType.ParameterFloat =>
                    new InlineParameterFloatCondition(def.ParameterName, def.FloatComparisonType, def.FloatValue, def.FloatRangeMin, def.FloatRangeMax) { Context = context },

                InlineConditionDefinition.ConditionType.ParameterTrigger =>
                    new InlineParameterTriggerCondition(def.ParameterName) { Context = context },

                InlineConditionDefinition.ConditionType.Timer =>
                    new InlineTimerCondition(def.TimerDuration) { Context = context },

                InlineConditionDefinition.ConditionType.Random =>
                    new InlineRandomCondition(def.RandomChance) { Context = context },

                _ => null
            };
        }
    }

    /// <summary>
    /// Wrapper for ScriptableCondition to work with ICondition<IContext>
    /// </summary>
    public class ScriptableConditionWrapper : UFSM.Core.ConditionBase<IContext>
    {
        private ScriptableCondition scriptableCondition;

        public override string ConditionName => scriptableCondition.ConditionName;

        public ScriptableConditionWrapper(ScriptableCondition condition)
        {
            scriptableCondition = condition;
        }

        public override bool Evaluate(IContext context)
        {
            scriptableCondition.SetContext(context);
            return scriptableCondition.Evaluate();
        }

        public override void Reset()
        {
            scriptableCondition.Reset();
        }
    }

    /// <summary>
    /// Runtime implementation of IStateMachine
    /// </summary>
    public class RuntimeStateMachine : UFSM.Core.StateMachineBase<IContext>
    {
        private string machineName;

        public override string MachineName => machineName;

        public RuntimeStateMachine(string name, IContext context)
        {
            machineName = name;
            Context = context;
        }
    }

    /// <summary>
    /// Runtime state that wraps StateDefinition
    /// </summary>
    public class RuntimeState : UFSM.Core.StateBase<IContext>
    {
        private StateDefinition definition;

        public override string StateName => definition.StateName;
        public override int Priority => definition.Priority;

        public RuntimeState(StateDefinition def, IContext context)
        {
            definition = def;
            Context = context;
        }

        public override void OnEnter()
        {
            // Events are handled by FSMController
        }

        public override void OnUpdate()
        {
            // Custom logic can be added via StateBehaviorScript or ScriptableBehavior
        }
    }

    /// <summary>
    /// State that uses ScriptableObject behavior
    /// </summary>
    public class ScriptableObjectState : UFSM.Core.StateBase<IContext>
    {
        private StateDefinition definition;
        private ScriptableStateBehavior behavior;

        public override string StateName => definition.StateName;
        public override int Priority => definition.Priority;

        public ScriptableObjectState(StateDefinition def, IContext context)
        {
            definition = def;
            behavior = def.StateBehavior;
            Context = context;
        }

        public override void OnEnter() => behavior?.OnEnter(Context);
        public override void OnUpdate() => behavior?.OnUpdate(Context, Time.deltaTime);
        public override void OnFixedUpdate() => behavior?.OnFixedUpdate(Context, Time.fixedDeltaTime);
        public override void OnLateUpdate() => behavior?.OnLateUpdate(Context, Time.deltaTime);
        public override void OnExit() => behavior?.OnExit(Context);
        public override bool CanExit() => behavior?.CanExit(Context) ?? true;
    }

    // Helper inline condition classes (these wrap ScriptableConditions for inline use)
    internal class InlineParameterBoolCondition : UFSM.Core.ConditionBase<IContext>
    {
        private string paramName;
        private bool expectedValue;

        public override string ConditionName => $"Param '{paramName}' == {expectedValue}";

        public InlineParameterBoolCondition(string paramName, bool expectedValue)
        {
            this.paramName = paramName;
            this.expectedValue = expectedValue;
        }

        public override bool Evaluate(IContext context)
        {
            return Parameters?.GetBool(paramName) == expectedValue;
        }
    }

    internal class InlineParameterIntCondition : UFSM.Core.ConditionBase<IContext>
    {
        private string paramName;
        private InlineConditionDefinition.IntComparison comparison;
        private int value;

        public override string ConditionName => $"Param '{paramName}' {comparison} {value}";

        public InlineParameterIntCondition(string paramName, InlineConditionDefinition.IntComparison comparison, int value)
        {
            this.paramName = paramName;
            this.comparison = comparison;
            this.value = value;
        }

        public override bool Evaluate(IContext context)
        {
            int paramValue = Parameters?.GetInt(paramName) ?? 0;

            return comparison switch
            {
                InlineConditionDefinition.IntComparison.Equal => paramValue == value,
                InlineConditionDefinition.IntComparison.NotEqual => paramValue != value,
                InlineConditionDefinition.IntComparison.Greater => paramValue > value,
                InlineConditionDefinition.IntComparison.GreaterOrEqual => paramValue >= value,
                InlineConditionDefinition.IntComparison.Less => paramValue < value,
                InlineConditionDefinition.IntComparison.LessOrEqual => paramValue <= value,
                _ => false
            };
        }
    }

    internal class InlineParameterFloatCondition : UFSM.Core.ConditionBase<IContext>
    {
        private string paramName;
        private InlineConditionDefinition.FloatComparison comparison;
        private float value;
        private float rangeMin;
        private float rangeMax;

        public override string ConditionName => $"Param '{paramName}' {comparison}";

        public InlineParameterFloatCondition(string paramName, InlineConditionDefinition.FloatComparison comparison, float value, float rangeMin, float rangeMax)
        {
            this.paramName = paramName;
            this.comparison = comparison;
            this.value = value;
            this.rangeMin = rangeMin;
            this.rangeMax = rangeMax;
        }

        public override bool Evaluate(IContext context)
        {
            float paramValue = Parameters?.GetFloat(paramName) ?? 0f;

            return comparison switch
            {
                InlineConditionDefinition.FloatComparison.Greater => paramValue > value,
                InlineConditionDefinition.FloatComparison.GreaterOrEqual => paramValue >= value,
                InlineConditionDefinition.FloatComparison.Less => paramValue < value,
                InlineConditionDefinition.FloatComparison.LessOrEqual => paramValue <= value,
                InlineConditionDefinition.FloatComparison.InRange => paramValue >= rangeMin && paramValue <= rangeMax,
                _ => false
            };
        }
    }

    internal class InlineParameterTriggerCondition : UFSM.Core.ConditionBase<IContext>
    {
        private string triggerName;

        public override string ConditionName => $"Trigger '{triggerName}'";

        public InlineParameterTriggerCondition(string triggerName)
        {
            this.triggerName = triggerName;
        }

        public override bool Evaluate(IContext context)
        {
            return Parameters?.IsTriggerSet(triggerName) ?? false;
        }
    }

    internal class InlineTimerCondition : UFSM.Core.ConditionBase<IContext>
    {
        private float duration;
        private float elapsedTime = 0f;
        private bool isActive = false;

        public override string ConditionName => $"Timer {duration}s";

        public InlineTimerCondition(float duration)
        {
            this.duration = duration;
        }

        public override bool Evaluate(IContext context)
        {
            if (!isActive)
            {
                isActive = true;
                elapsedTime = 0f;
            }

            elapsedTime += UnityEngine.Time.deltaTime;
            return elapsedTime >= duration;
        }

        public override void Reset()
        {
            elapsedTime = 0f;
            isActive = false;
        }
    }

    internal class InlineRandomCondition : UFSM.Core.ConditionBase<IContext>
    {
        private float chance;

        public override string ConditionName => $"Random {chance * 100}%";

        public InlineRandomCondition(float chance)
        {
            this.chance = chance;
        }

        public override bool Evaluate(IContext context)
        {
            return UnityEngine.Random.value <= chance;
        }
    }
}