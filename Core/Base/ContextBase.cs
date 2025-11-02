using UnityEngine;

namespace UFSM.Core
{
    /// <summary>
    /// Abstract base class for all FSM contexts.
    /// Provides common functionality for both MonoBehaviour and DOTS contexts.
    /// </summary>
    public abstract class ContextBase : IContext
    {
        // IContext Implementation
        public virtual GameObject Owner { get; protected set; }
        public virtual Transform Transform { get; protected set; }
        public IStateMachine StateMachine { get; set; }

        // Constructor for MonoBehaviour contexts
        protected ContextBase(GameObject owner)
        {
            Owner = owner;
            Transform = owner?.transform;
        }

        // Constructor for pure data contexts (DOTS)
        protected ContextBase()
        {
            Owner = null;
            Transform = null;
        }

        public virtual void Initialize()
        {
            // Override in derived classes
        }

        public virtual void Cleanup()
        {
            // Override in derived classes
        }
    }

    /// <summary>
    /// Generic context base with typed state machine reference
    /// </summary>
    public abstract class ContextBase<TStateMachine> : ContextBase
        where TStateMachine : IStateMachine
    {
        public new TStateMachine StateMachine
        {
            get => (TStateMachine)base.StateMachine;
            set => base.StateMachine = value;
        }

        protected ContextBase(GameObject owner) : base(owner) { }
        protected ContextBase() : base() { }
    }
}