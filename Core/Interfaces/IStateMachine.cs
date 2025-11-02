using System;
using System.Collections.Generic;

namespace UFSM.Core
{
    /// <summary>
    /// Update mode for state machine transition checking
    /// </summary>
    public enum UpdateMode
    {
        EveryFrame,          // Check transitions every frame (high priority FSMs)
        TimeInterval,        // Check at fixed time intervals (background FSMs)
        Manual              // Only check when explicitly called
    }

    /// <summary>
    /// Performance profile for balancing speed vs memory
    /// </summary>
    public enum PerformanceProfile
    {
        Speed,              // Optimize for speed (more memory usage)
        Balanced,           // Balance between speed and memory
        Memory              // Optimize for memory (less speed)
    }

    /// <summary>
    /// Base interface for all state machines
    /// </summary>
    public interface IStateMachine
    {
        /// <summary>
        /// Unique identifier for this FSM
        /// </summary>
        string MachineName { get; }

        /// <summary>
        /// Current active state
        /// </summary>
        IState CurrentState { get; }

        /// <summary>
        /// Previous state (for history/debugging)
        /// </summary>
        IState PreviousState { get; }

        /// <summary>
        /// How often transitions should be evaluated
        /// </summary>
        UpdateMode TransitionUpdateMode { get; set; }

        /// <summary>
        /// Time interval for TimeInterval update mode (in seconds)
        /// </summary>
        float TransitionCheckInterval { get; set; }

        /// <summary>
        /// Performance profile setting
        /// </summary>
        PerformanceProfile Profile { get; set; }

        /// <summary>
        /// Priority for this FSM (higher = updated first)
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// Is this FSM currently active?
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Initialize the state machine
        /// </summary>
        void Initialize();

        /// <summary>
        /// Start the FSM with an initial state
        /// </summary>
        void Start(string initialStateName);

        /// <summary>
        /// Update the current state (called by Central Manager)
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Fixed update for physics
        /// </summary>
        void FixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// Late update
        /// </summary>
        void LateUpdate(float deltaTime);

        /// <summary>
        /// Manually check and evaluate transitions
        /// </summary>
        void CheckTransitions();

        /// <summary>
        /// Force a transition to a specific state
        /// </summary>
        bool ForceTransition(string stateName);

        /// <summary>
        /// Stop the state machine
        /// </summary>
        void Stop();

        /// <summary>
        /// Clean up resources
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Get all registered states
        /// </summary>
        IReadOnlyList<IState> GetAllStates();

        /// <summary>
        /// Get a specific state by name
        /// </summary>
        IState GetState(string stateName);

        /// <summary>
        /// Event triggered when state changes
        /// </summary>
        event Action<IState, IState> OnStateChanged;
    }

    /// <summary>
    /// Generic state machine interface with typed context
    /// </summary>
    public interface IStateMachine<TContext> : IStateMachine where TContext : IContext
    {
        /// <summary>
        /// The context this state machine operates on
        /// </summary>
        TContext Context { get; set; }

        /// <summary>
        /// Get all states as typed states
        /// </summary>
        IReadOnlyList<IState<TContext>> GetAllTypedStates();

        /// <summary>
        /// Get a specific typed state by name
        /// </summary>
        IState<TContext> GetTypedState(string stateName);
    }
}