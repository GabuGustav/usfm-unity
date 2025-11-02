using System.Collections.Generic;
using System.Linq;

namespace UFSM.Core
{
    /// <summary>
    /// Base class for transitions
    /// </summary>
    public class TransitionBase<TContext> : ITransition<TContext> where TContext : IContext
    {
        public string FromState { get; set; }
        public string ToState { get; set; }
        public int Priority { get; set; }
        public bool CanInterrupt { get; set; } = true;
        public float TransitionDelay { get; set; } = 0f;
        public LogicOperator LogicOperator { get; set; } = LogicOperator.AND;
        public TContext Context { get; set; }

        private List<ICondition<TContext>> conditions = new List<ICondition<TContext>>();
        public IReadOnlyList<ICondition> Conditions => conditions.Cast<ICondition>().ToList();

        public TransitionBase(string fromState, string toState)
        {
            FromState = fromState;
            ToState = toState;
        }

        public virtual bool Evaluate()
        {
            if (conditions.Count == 0) return false;

            // Set context for all conditions
            foreach (var condition in conditions)
            {
                condition.Context = Context;
            }

            // Evaluate based on logic operator
            switch (LogicOperator)
            {
                case LogicOperator.AND:
                    return conditions.All(c => c.Evaluate());

                case LogicOperator.OR:
                    return conditions.Any(c => c.Evaluate());

                case LogicOperator.NOT:
                    return conditions.Count == 1 && !conditions[0].Evaluate();

                case LogicOperator.NAND:
                    return !conditions.All(c => c.Evaluate());

                case LogicOperator.NOR:
                    return !conditions.Any(c => c.Evaluate());

                case LogicOperator.XOR:
                    return conditions.Count(c => c.Evaluate()) == 1;

                default:
                    return false;
            }
        }

        public void AddCondition(ICondition condition)
        {
            if (condition is ICondition<TContext> typedCondition)
            {
                conditions.Add(typedCondition);
            }
        }

        public void RemoveCondition(ICondition condition)
        {
            if (condition is ICondition<TContext> typedCondition)
            {
                conditions.Remove(typedCondition);
            }
        }

        public void ClearConditions()
        {
            conditions.Clear();
        }
    }
}