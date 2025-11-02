namespace UFSM.Core
{
    /// <summary>
    /// Abstract base class for conditions
    /// </summary>
    public abstract class ConditionBase<TContext> : ICondition<TContext> where TContext : IContext
    {
        public abstract string ConditionName { get; }
        public TContext Context { get; set; }

        // Non-generic Evaluate (calls typed version)
        public bool Evaluate() => Evaluate(Context);

        // Override this in derived classes
        public abstract bool Evaluate(TContext context);

        public virtual void Reset()
        {
            // Override if needed
        }

        // Helper: Access parameter store
        protected IParameterStore Parameters
        {
            get
            {
                if (Context?.StateMachine == null) return null;

                var fsm = Context.StateMachine;
                var type = fsm.GetType();

                // Look for GetParameterStore method
                var method = type.GetMethod("GetParameterStore");
                if (method != null)
                {
                    return method.Invoke(fsm, null) as IParameterStore;
                }

                return null;
            }
        }
    }
}