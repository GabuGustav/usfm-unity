namespace UFSM.Core
{
    public static class StateMachineExtensions
    {
        public static IParameterStore GetParameterStore(this IStateMachine machine)
        {
            // This is implemented in StateMachineBase
            var baseType = machine.GetType().BaseType;
            var method = baseType?.GetMethod("GetParameterStore");
            return method?.Invoke(machine, null) as IParameterStore;
        }
    }
}