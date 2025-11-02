using System;

namespace UFSM.Core
{
    /// <summary>
    /// Log level for FSM debugging
    /// </summary>
    public enum LogLevel
    {
        None,       // No logging
        Error,      // Only errors
        Warning,    // Errors + warnings
        Info,       // Errors + warnings + info
        Verbose     // Everything including state updates
    }

    /// <summary>
    /// Logging interface for FSM system
    /// </summary>
    public interface IFSMLogger
    {
        /// <summary>
        /// Current log level
        /// </summary>
        LogLevel CurrentLogLevel { get; set; }

        /// <summary>
        /// Log an error
        /// </summary>
        void LogError(string message, Exception exception = null);

        /// <summary>
        /// Log a warning
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Log info
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// Log verbose/debug info
        /// </summary>
        void LogVerbose(string message);

        /// <summary>
        /// Log a state change
        /// </summary>
        void LogStateChange(string machineName, string fromState, string toState);

        /// <summary>
        /// Log a transition evaluation
        /// </summary>
        void LogTransitionEvaluation(string machineName, string transition, bool result);

        /// <summary>
        /// Log a parameter change
        /// </summary>
        void LogParameterChange(string parameterName, object oldValue, object newValue);
    }
}