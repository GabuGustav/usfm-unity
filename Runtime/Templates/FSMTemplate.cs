using System.Collections.Generic;
using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime.Templates
{
    /// <summary>
    /// Base class for FSM templates. Create custom templates by inheriting from this.
    /// </summary>
    [CreateAssetMenu(menuName = "UFSM/FSM Template/Custom Template", order = 0)]
    public class FSMTemplate : ScriptableObject
    {
        [Header("Template Info")]
        public string TemplateName = "Custom FSM Template";
        [TextArea(3, 6)]
        public string Description;
        public Sprite Icon;

        [Header("FSM Structure")]
        public List<StateDefinition> States = new List<StateDefinition>();
        public List<TransitionDefinition> Transitions = new List<TransitionDefinition>();
        public List<ParameterDefinition> Parameters = new List<ParameterDefinition>();

        [Header("Default Settings")]
        public UpdateMode DefaultUpdateMode = UpdateMode.EveryFrame;
        public PerformanceProfile DefaultProfile = PerformanceProfile.Balanced;
        public int DefaultPriority = 0;
        public string InitialStateName = "Idle";

        /// <summary>
        /// Apply this template to an FSM Definition
        /// </summary>
        public virtual void ApplyTo(FSMDefinition target)
        {
            target.States = new List<StateDefinition>(States);
            target.Transitions = new List<TransitionDefinition>(Transitions);
            target.Parameters = new List<ParameterDefinition>(Parameters);

            target.TransitionUpdateMode = DefaultUpdateMode;
            target.PerformanceProfile = DefaultProfile;
            target.Priority = DefaultPriority;
            target.InitialStateName = InitialStateName;

            Debug.Log($"Applied template '{TemplateName}' to FSM '{target.MachineName}'");
        }

        /// <summary>
        /// Create a new FSM Definition from this template
        /// </summary>
        public virtual FSMDefinition CreateInstance(string fsmName)
        {
            var newFSM = CreateInstance<FSMDefinition>();
            newFSM.MachineName = fsmName;
            ApplyTo(newFSM);
            return newFSM;
        }
    }
}