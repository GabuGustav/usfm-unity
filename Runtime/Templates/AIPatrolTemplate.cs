using System.Collections.Generic;
using UFSM.Core;
using UnityEngine;

namespace UFSM.Runtime.Templates
{
    /// <summary>
    /// AI Patrol FSM: Idle → Patrol → Chase → Attack
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/FSM Template/AI Patrol", order = 3)]
    public class AIPatrolTemplate : FSMTemplate
    {
        public AIPatrolTemplate()
        {
            TemplateName = "AI Patrol FSM";
            Description = "AI behavior FSM: Idle → Patrol → Chase (when player detected) → Attack (when in range) → back to Patrol.";

            // States
            States = new List<StateDefinition>
            {
                new StateDefinition
                {
                    StateName = "Idle",
                    Description = "AI is idle, waiting"
                },
                new StateDefinition
                {
                    StateName = "Patrol",
                    Description = "AI is patrolling waypoints"
                },
                new StateDefinition
                {
                    StateName = "Chase",
                    Description = "AI is chasing the player"
                },
                new StateDefinition
                {
                    StateName = "Attack",
                    Description = "AI is attacking the player"
                }
            };

            // Parameters
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition
                {
                    Name = "PlayerDetected",
                    Type = ParameterType.Bool,
                    DefaultBool = false
                },
                new ParameterDefinition
                {
                    Name = "PlayerInAttackRange",
                    Type = ParameterType.Bool,
                    DefaultBool = false
                },
                new ParameterDefinition
                {
                    Name = "PlayerLost",
                    Type = ParameterType.Bool,
                    DefaultBool = false
                },
                new ParameterDefinition
                {
                    Name = "DetectionRange",
                    Type = ParameterType.Float,
                    DefaultFloat = 10f
                },
                new ParameterDefinition
                {
                    Name = "AttackRange",
                    Type = ParameterType.Float,
                    DefaultFloat = 2f
                }
            };

            // Transitions
            Transitions = new List<TransitionDefinition>
            {
                // Idle → Patrol
                new TransitionDefinition
                {
                    FromState = "Idle",
                    ToState = "Patrol",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 1f
                        }
                    }
                },
                
                // Patrol → Chase (player detected)
                new TransitionDefinition
                {
                    FromState = "Patrol",
                    ToState = "Chase",
                    Priority = 10,
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "PlayerDetected",
                            BoolValue = true
                        }
                    }
                },
                
                // Chase → Attack (in range)
                new TransitionDefinition
                {
                    FromState = "Chase",
                    ToState = "Attack",
                    Priority = 10,
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "PlayerInAttackRange",
                            BoolValue = true
                        }
                    }
                },
                
                // Attack → Chase (out of range)
                new TransitionDefinition
                {
                    FromState = "Attack",
                    ToState = "Chase",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "PlayerInAttackRange",
                            BoolValue = false
                        }
                    }
                },
                
                // Chase → Patrol (player lost)
                new TransitionDefinition
                {
                    FromState = "Chase",
                    ToState = "Patrol",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "PlayerLost",
                            BoolValue = true
                        }
                    }
                }
            };

            InitialStateName = "Idle";
            DefaultUpdateMode = UpdateMode.EveryFrame;
            DefaultPriority = 50;
        }
    }
}