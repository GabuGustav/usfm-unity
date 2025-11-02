using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Base class for reusable ScriptableObject conditions.
    /// These can be created once and used in multiple transitions.
    /// </summary>
    public abstract class ScriptableCondition : ScriptableObject, ICondition
    {
        [TextArea(2, 4)]
        public string Description;

        public virtual string ConditionName => name;

        protected IContext currentContext;

        public void SetContext(IContext context)
        {
            currentContext = context;
        }

        public abstract bool Evaluate();

        public virtual void Reset() { }

        // Helper: Access parameter store
        protected IParameterStore Parameters => currentContext?.StateMachine?.GetParameterStore();

        // Helper: Access global parameters
        protected GlobalParameterStore GlobalParams => GlobalParameterStore.Instance;
    }
}
