using System.Collections.Generic;
using UFSM.Core;
using UnityEngine;

namespace UFSM.Runtime.Templates
{
    /// <summary>
    /// Basic 3-state FSM: Idle → Active → Cooldown
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/FSM Template/Basic 3-State (Cooldown)", order = 2)]
    public class Basic3StateTemplate : FSMTemplate
    {
        public Basic3StateTemplate()
        {
            TemplateName = "Basic 3-State FSM (with Cooldown)";
            Description = "FSM with cooldown cycle: Idle → Active → Cooldown → Idle.\nUse 'Activate' trigger to start, cooldown timer returns to idle.";

            // States
            States = new List<StateDefinition>
            {
                new StateDefinition
                {
                    StateName = "Idle",
                    Description = "Ready to activate"
                },
                new StateDefinition
                {
                    StateName = "Active",
                    Description = "Currently active/executing"
                },
                new StateDefinition
                {
                    StateName = "Cooldown",
                    Description = "Cooling down before returning to idle"
                }
            };

            // Parameters
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition
                {
                    Name = "Activate",
                    Type = ParameterType.Trigger
                },
                new ParameterDefinition
                {
                    Name = "ActiveDuration",
                    Type = ParameterType.Float,
                    DefaultFloat = 2f
                },
                new ParameterDefinition
                {
                    Name = "CooldownDuration",
                    Type = ParameterType.Float,
                    DefaultFloat = 1f
                }
            };

            // Transitions
            Transitions = new List<TransitionDefinition>
            {
                new TransitionDefinition
                {
                    FromState = "Idle",
                    ToState = "Active",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterTrigger,
                            ParameterName = "Activate"
                        }
                    }
                },
                new TransitionDefinition
                {
                    FromState = "Active",
                    ToState = "Cooldown",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 2f
                        }
                    }
                },
                new TransitionDefinition
                {
                    FromState = "Cooldown",
                    ToState = "Idle",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 1f
                        }
                    }
                }
            };

            InitialStateName = "Idle";
        }
    }
}