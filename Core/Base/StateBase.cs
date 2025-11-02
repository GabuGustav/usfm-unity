namespace UFSM.Core
{
    /// <summary>
    /// Abstract base class for all states.
    /// Provides default implementations and common functionality.
    /// </summary>
    public abstract class StateBase<TContext> : IState<TContext> where TContext : IContext
    {
        // IState Implementation
        public abstract string StateName { get; }
        public virtual int Priority => 0;

        // IState<TContext> Implementation
        public TContext Context { get; set; }

        // Lifecycle methods - override as needed
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnExit() { }
        public virtual bool CanExit() => true;

        // Helper: Quick access to parameter store
        protected IParameterStore Parameters
        {
            get
            {
                if (Context?.StateMachine == null) return null;

                // Try to get parameter store via extension method or direct access
                var fsm = Context.StateMachine;
                var type = fsm.GetType();

                // Look for GetParameterStore method
                var method = type.GetMethod("GetParameterStore");
                if (method != null)
                {
                    return method.Invoke(fsm, null) as IParameterStore;
                }

                // Look for parameterStore field
                var field = type.GetField("parameterStore",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(fsm) as IParameterStore;
                }

                return null;
            }
        }

        // Helper: Quick access to logger
        protected IFSMLogger Logger
        {
            get
            {
                var manager = UnityEngine.Object.FindObjectOfType<Runtime.FSMManager>();
                return manager?.Logger;
            }
        }

        // Helper: Log state activity
        protected void Log(string message)
        {
            Logger?.LogVerbose($"[{StateName}] {message}");
        }
    }
}