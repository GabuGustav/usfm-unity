using UnityEngine;

namespace UFSM.Core
{
    /// <summary>
    /// Base interface for all FSM contexts. Contexts store state-specific data
    /// and provide access to the GameObject/Transform for MonoBehaviour-based FSMs.
    /// For DOTS compatibility, Owner and Transform can be null.
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// The GameObject this context belongs to (null for DOTS or pure data contexts)
        /// </summary>
        GameObject Owner { get; }

        /// <summary>
        /// Quick access to the Transform (null for DOTS or pure data contexts)
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Reference to the state machine managing this context
        /// </summary>
        IStateMachine StateMachine { get; set; }

        /// <summary>
        /// Called when the context is initialized
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when the context is cleaned up
        /// </summary>
        void Cleanup();
    }
}