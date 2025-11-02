using System.Collections.Generic;

namespace UFSM.Core
{
    /// <summary>
    /// Represents a transition between states
    /// </summary>
    public interface ITransition
    {
        /// <summary>
        /// Source state name (empty for "Any State")
        /// </summary>
        string FromState { get; }

        /// <summary>
        /// Target state name
        /// </summary>
        string ToState { get; }

        /// <summary>
        /// Priority of this transition (higher = evaluated first)
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// Can this transition interrupt the current state?
        /// </summary>
        bool CanInterrupt { get; set; }

        /// <summary>
        /// Optional delay before transition executes (in seconds)
        /// </summary>
        float TransitionDelay { get; set; }

        /// <summary>
        /// All conditions for this transition
        /// </summary>
        IReadOnlyList<ICondition> Conditions { get; }

        /// <summary>
        /// Logical operator combining the conditions
        /// </summary>
        LogicOperator LogicOperator { get; set; }

        /// <summary>
        /// Evaluate if this transition should fire
        /// </summary>
        bool Evaluate();

        /// <summary>
        /// Add a condition to this transition
        /// </summary>
        void AddCondition(ICondition condition);

        /// <summary>
        /// Remove a condition from this transition
        /// </summary>
        void RemoveCondition(ICondition condition);

        /// <summary>
        /// Clear all conditions
        /// </summary>
        void ClearConditions();
    }

    /// <summary>
    /// Generic transition interface with typed context
    /// </summary>
    public interface ITransition<TContext> : ITransition where TContext : IContext
    {
        /// <summary>
        /// Context for condition evaluation
        /// </summary>
        TContext Context { get; set; }
    }
}