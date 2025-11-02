using System.Collections.Generic;
using UFSM.Core;
using UnityEngine;

namespace UFSM.Runtime.Templates
{
    /// <summary>
    /// Crop Life Cycle FSM: Seed → Sprout → Growing → Mature → Harvested
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/FSM Template/Crop Life Cycle", order = 4)]
    public class CropLifeCycleTemplate : FSMTemplate
    {
        public CropLifeCycleTemplate()
        {
            TemplateName = "Crop Life Cycle FSM";
            Description = "Plant growth FSM: Seed → Sprout → Growing → Mature → Harvested.\nUse 'Plant' trigger to start, time-based progression through stages.";

            // States
            States = new List<StateDefinition>
            {
                new StateDefinition
                {
                    StateName = "Seed",
                    Description = "Crop is planted but not yet sprouted"
                },
                new StateDefinition
                {
                    StateName = "Sprout",
                    Description = "Crop has just sprouted from ground"
                },
                new StateDefinition
                {
                    StateName = "Growing",
                    Description = "Crop is actively growing"
                },
                new StateDefinition
                {
                    StateName = "Mature",
                    Description = "Crop is fully grown and ready to harvest"
                },
                new StateDefinition
                {
                    StateName = "Harvested",
                    Description = "Crop has been harvested"
                },
                new StateDefinition
                {
                    StateName = "Withered",
                    Description = "Crop withered (not harvested in time)"
                }
            };

            // Parameters
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition
                {
                    Name = "Plant",
                    Type = ParameterType.Trigger
                },
                new ParameterDefinition
                {
                    Name = "Harvest",
                    Type = ParameterType.Trigger
                },
                new ParameterDefinition
                {
                    Name = "GrowthStage",
                    Type = ParameterType.Int,
                    DefaultInt = 0
                },
                new ParameterDefinition
                {
                    Name = "SproutTime",
                    Type = ParameterType.Float,
                    DefaultFloat = 2f
                },
                new ParameterDefinition
                {
                    Name = "GrowthTime",
                    Type = ParameterType.Float,
                    DefaultFloat = 5f
                },
                new ParameterDefinition
                {
                    Name = "MaturationTime",
                    Type = ParameterType.Float,
                    DefaultFloat = 3f
                },
                new ParameterDefinition
                {
                    Name = "WitherTime",
                    Type = ParameterType.Float,
                    DefaultFloat = 10f
                },
                new ParameterDefinition
                {
                    Name = "IsWatered",
                    Type = ParameterType.Bool,
                    DefaultBool = true
                },
                new ParameterDefinition
                {
                    Name = "HasSunlight",
                    Type = ParameterType.Bool,
                    DefaultBool = true
                }
            };

            // Transitions
            Transitions = new List<TransitionDefinition>
            {
                // Seed → Sprout (after sprout time)
                new TransitionDefinition
                {
                    FromState = "Seed",
                    ToState = "Sprout",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 2f
                        },
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "IsWatered",
                            BoolValue = true
                        }
                    }
                },
                
                // Sprout → Growing
                new TransitionDefinition
                {
                    FromState = "Sprout",
                    ToState = "Growing",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 5f
                        },
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterBool,
                            ParameterName = "HasSunlight",
                            BoolValue = true
                        }
                    }
                },
                
                // Growing → Mature
                new TransitionDefinition
                {
                    FromState = "Growing",
                    ToState = "Mature",
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 3f
                        }
                    }
                },
                
                // Mature → Harvested (player harvests)
                new TransitionDefinition
                {
                    FromState = "Mature",
                    ToState = "Harvested",
                    Priority = 10,
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.ParameterTrigger,
                            ParameterName = "Harvest"
                        }
                    }
                },
                
                // Mature → Withered (not harvested in time)
                new TransitionDefinition
                {
                    FromState = "Mature",
                    ToState = "Withered",
                    Priority = 5,
                    LogicOperator = LogicOperator.AND,
                    InlineConditions = new List<InlineConditionDefinition>
                    {
                        new InlineConditionDefinition
                        {
                            Type = InlineConditionDefinition.ConditionType.Timer,
                            TimerDuration = 10f
                        }
                    }
                }
            };

            InitialStateName = "Seed";
            DefaultUpdateMode = UpdateMode.TimeInterval;
            DefaultProfile = PerformanceProfile.Memory;
            DefaultPriority = 10;
        }
    }
}