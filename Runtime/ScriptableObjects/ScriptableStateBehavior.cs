using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Base class for ScriptableObject-based state behaviors.
    /// Create custom state behaviors by inheriting from this.
    /// </summary>
    public abstract class ScriptableStateBehavior : ScriptableObject
    {
        [TextArea(2, 4)]
        public string Description;

        public virtual void OnEnter(IContext context) { }
        public virtual void OnUpdate(IContext context, float deltaTime) { }
        public virtual void OnFixedUpdate(IContext context, float fixedDeltaTime) { }
        public virtual void OnLateUpdate(IContext context, float deltaTime) { }
        public virtual void OnExit(IContext context) { }
        public virtual bool CanExit(IContext context) => true;
    }
}