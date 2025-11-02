using System.Collections.Generic;
using UFSM.Core;
using UnityEngine;

namespace UFSM.Runtime.Templates
{
    /// <summary>
    /// Basic 2-state FSM: Idle ↔ Active
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/FSM Template/Basic 2-State", order = 1)]
    public class Basic2StateTemplate : FSMTemplate
    {
        public Basic2StateTemplate()
        {
            TemplateName = "Basic 2-State FSM";
            Description = "Simple FSM with two states: Idle and Active.\nUse 'IsActive' parameter to switch between states.";

            // States
            States = new List<StateDefinition>
            {
                new StateDefinition
                {
                    StateName = "Idle",
                    Description = "Object is idle/inactive"
                },
                new StateDefinition
                {
                    StateName = "Active",
                    Description = "Object is active/working"
                }
            };

            // Parameters
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition
                {
                    Name = "IsActive",
                    Type = ParameterType.Bool,
                    DefaultBool = false
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
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "IsActive",
                            BoolValue = true
                        }
                    }
                },
                new TransitionDefinition
                {
                    FromState = "Active",
                    ToState = "Idle",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "IsActive",
                            BoolValue = false
                        }
                    }
                }
            };

            InitialStateName = "Idle";
        }
    }
}
