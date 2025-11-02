namespace UFSM.Core
{
    /// <summary>
    /// Base interface for all states in the FSM system
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Unique identifier for this state
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// Priority for this state (used in hierarchical FSMs)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Called when entering this state
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// Called during FixedUpdate while in this state (for physics)
        /// </summary>
        void OnFixedUpdate();

        /// <summary>
        /// Called during LateUpdate while in this state
        /// </summary>
        void OnLateUpdate();

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called to check if this state can be interrupted
        /// </summary>
        bool CanExit();
    }

    /// <summary>
    /// Generic state interface with typed context
    /// </summary>
    public interface IState<TContext> : IState where TContext : IContext
    {
        /// <summary>
        /// The context this state operates on
        /// </summary>
        TContext Context { get; set; }
    }
}