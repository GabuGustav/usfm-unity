namespace UFSM.Core
{
    /// <summary>
    /// Logical operator for combining conditions
    /// </summary>
    public enum LogicOperator
    {
        AND,    // All conditions must be true
        OR,     // At least one condition must be true
        NOT,    // Inverts the condition result
        NAND,   // NOT (all conditions true)
        NOR,    // NOT (any condition true)
        XOR     // Exactly one condition must be true
    }

    /// <summary>
    /// Base interface for transition conditions
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// Name/description of this condition
        /// </summary>
        string ConditionName { get; }

        /// <summary>
        /// Evaluate the condition (non-generic version)
        /// </summary>
        bool Evaluate();

        /// <summary>
        /// Reset the condition to its initial state
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Generic condition interface with typed context
    /// </summary>
    public interface ICondition<TContext> : ICondition where TContext : IContext
    {
        /// <summary>
        /// The context to evaluate against
        /// </summary>
        TContext Context { get; set; }

        /// <summary>
        /// Evaluate the condition with typed context
        /// </summary>
        bool Evaluate(TContext context);
    }
}