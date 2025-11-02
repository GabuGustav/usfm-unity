using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFSM.Core
{
    /// <summary>
    /// Abstract base class for state machines.
    /// Handles state management, transitions, and update logic.
    /// </summary>
    public abstract class StateMachineBase<TContext> : IStateMachine<TContext>
        where TContext : IContext
    {
        // IStateMachine Implementation
        public abstract string MachineName { get; }
        public IState CurrentState { get; protected set; }
        public IState PreviousState { get; protected set; }
        public UpdateMode TransitionUpdateMode { get; set; } = UpdateMode.EveryFrame;
        public float TransitionCheckInterval { get; set; } = 0.1f;
        public PerformanceProfile Profile { get; set; } = PerformanceProfile.Balanced;
        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // IStateMachine<TContext> Implementation
        public TContext Context { get; set; }

        // Events
        public event Action<IState, IState> OnStateChanged;

        // Internal state management
        protected Dictionary<string, IState<TContext>> states = new Dictionary<string, IState<TContext>>();
        protected List<ITransition<TContext>> transitions = new List<ITransition<TContext>>();
        protected IParameterStore parameterStore;
        protected IFSMLogger logger;

        // Transition timing
        private float transitionCheckTimer = 0f;
        private float delayedTransitionTimer = 0f;
        private string delayedTransitionTarget = null;

        // Object pooling (for Speed and Balanced profiles)
        protected Dictionary<string, IState<TContext>> statePool;
        protected bool usePooling => Profile != PerformanceProfile.Memory;

        // Constructor
        // Change logger field to use lazy property
        protected IFSMLogger _logger;
        protected IFSMLogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    var managerObj = UnityEngine.Object.FindObjectOfType<Runtime.FSMManager>();
                    _logger = managerObj?.Logger;
                }
                return _logger;
            }
        }

        protected StateMachineBase()
        {
            // Don't access Logger here!
            if (usePooling)
            {
                statePool = new Dictionary<string, IState<TContext>>();
            }
        }

        // IStateMachine Implementation
        public virtual void Initialize()
        {
            // Create parameter store (per-FSM)
            parameterStore = new Runtime.ParameterStore();

            // Initialize all states
            foreach (var state in states.Values)
            {
                state.Context = Context;
            }

            logger?.LogInfo($"State Machine '{MachineName}' initialized with {states.Count} states");
        }

        public virtual void Start(string initialStateName)
        {
            if (!states.ContainsKey(initialStateName))
            {
                logger?.LogError($"Cannot start FSM '{MachineName}': Initial state '{initialStateName}' not found");
                return;
            }

            ChangeState(initialStateName);
            logger?.LogInfo($"State Machine '{MachineName}' started with initial state '{initialStateName}'");
        }

        public virtual void Update(float deltaTime)
        {
            if (!IsActive || CurrentState == null) return;

            // Update current state
            CurrentState.OnUpdate();

            // Check for delayed transition
            if (delayedTransitionTarget != null)
            {
                delayedTransitionTimer -= deltaTime;
                if (delayedTransitionTimer <= 0f)
                {
                    ForceTransition(delayedTransitionTarget);
                    delayedTransitionTarget = null;
                }
                return; // Don't check other transitions during delay
            }

            // Check transitions based on update mode
            switch (TransitionUpdateMode)
            {
                case UpdateMode.EveryFrame:
                    CheckTransitions();
                    break;

                case UpdateMode.TimeInterval:
                    transitionCheckTimer += deltaTime;
                    if (transitionCheckTimer >= TransitionCheckInterval)
                    {
                        CheckTransitions();
                        transitionCheckTimer = 0f;
                    }
                    break;

                case UpdateMode.Manual:
                    // Don't check automatically
                    break;
            }
        }

        public virtual void FixedUpdate(float fixedDeltaTime)
        {
            if (!IsActive || CurrentState == null) return;
            CurrentState.OnFixedUpdate();
        }

        public virtual void LateUpdate(float deltaTime)
        {
            if (!IsActive || CurrentState == null) return;
            CurrentState.OnLateUpdate();
        }

        public virtual void CheckTransitions()
        {
            if (CurrentState == null) return;

            // Get valid transitions from current state (sorted by priority)
            var validTransitions = transitions
                .Where(t => t.FromState == CurrentState.StateName || string.IsNullOrEmpty(t.FromState))
                .OrderByDescending(t => t.Priority)
                .ToList();

            // Evaluate transitions
            foreach (var transition in validTransitions)
            {
                if (transition.Evaluate())
                {
                    logger?.LogTransitionEvaluation(MachineName,
                        $"{transition.FromState} -> {transition.ToState}", true);

                    // Check if current state can be interrupted
                    if (!transition.CanInterrupt && !CurrentState.CanExit())
                    {
                        continue;
                    }

                    // Handle transition delay
                    if (transition.TransitionDelay > 0f)
                    {
                        delayedTransitionTarget = transition.ToState;
                        delayedTransitionTimer = transition.TransitionDelay;
                        logger?.LogVerbose($"Delaying transition to '{transition.ToState}' by {transition.TransitionDelay}s");
                    }
                    else
                    {
                        ChangeState(transition.ToState);
                    }

                    return; // Only execute first valid transition
                }
            }
        }

        public virtual bool ForceTransition(string stateName)
        {
            if (!states.ContainsKey(stateName))
            {
                logger?.LogError($"Cannot transition to '{stateName}': State not found in '{MachineName}'");
                return false;
            }

            ChangeState(stateName);
            return true;
        }

        public virtual void Stop()
        {
            if (CurrentState != null)
            {
                CurrentState.OnExit();
                logger?.LogInfo($"State Machine '{MachineName}' stopped from state '{CurrentState.StateName}'");
            }

            CurrentState = null;
            PreviousState = null;
            IsActive = false;
        }

        public virtual void Cleanup()
        {
            Stop();
            states.Clear();
            transitions.Clear();
            parameterStore?.ResetAll();

            if (statePool != null)
            {
                statePool.Clear();
            }

            logger?.LogInfo($"State Machine '{MachineName}' cleaned up");
        }

        // State management
        protected virtual void ChangeState(string stateName)
        {
            var newState = states[stateName];

            // Exit current state
            if (CurrentState != null)
            {
                CurrentState.OnExit();
                PreviousState = CurrentState;
            }

            // Change state
            var oldState = CurrentState;
            CurrentState = newState;

            // Enter new state
            CurrentState.OnEnter();

            // Trigger event
            OnStateChanged?.Invoke(oldState, CurrentState);

            logger?.LogStateChange(MachineName,
                oldState?.StateName ?? "None",
                CurrentState.StateName);
        }

        // State registration
        public virtual void RegisterState(IState<TContext> state)
        {
            if (!states.ContainsKey(state.StateName))
            {
                states.Add(state.StateName, state);
                state.Context = Context;
            }
        }

        public virtual void UnregisterState(string stateName)
        {
            states.Remove(stateName);
        }

        // Transition management
        public virtual void RegisterTransition(ITransition<TContext> transition)
        {
            transitions.Add(transition);
        }

        public virtual void UnregisterTransition(ITransition<TContext> transition)
        {
            transitions.Remove(transition);
        }

        // IStateMachine query methods
        public IReadOnlyList<IState> GetAllStates() => states.Values.Cast<IState>().ToList();
        public IState GetState(string stateName) => states.ContainsKey(stateName) ? states[stateName] : null;

        public IReadOnlyList<IState<TContext>> GetAllTypedStates() => states.Values.ToList();
        public IState<TContext> GetTypedState(string stateName) => states.ContainsKey(stateName) ? states[stateName] : null;

        // Parameter store access - PUBLIC so it can be called externally
        public IParameterStore GetParameterStore() => parameterStore;
    }
}
