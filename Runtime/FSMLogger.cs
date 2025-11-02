using System;
using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Logger implementation for FSM system
    /// </summary>
    public class FSMLogger : IFSMLogger
    {
        public LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

        public void LogError(string message, Exception exception = null)
        {
            if (CurrentLogLevel >= LogLevel.Error)
            {
                if (exception != null)
                {
                    Debug.LogError($"[UFSM ERROR] {message}\n{exception}");
                }
                else
                {
                    Debug.LogError($"[UFSM ERROR] {message}");
                }
            }
        }

        public void LogWarning(string message)
        {
            if (CurrentLogLevel >= LogLevel.Warning)
            {
                Debug.LogWarning($"[UFSM WARNING] {message}");
            }
        }

        public void LogInfo(string message)
        {
            if (CurrentLogLevel >= LogLevel.Info)
            {
                Debug.Log($"[UFSM INFO] {message}");
            }
        }

        public void LogVerbose(string message)
        {
            if (CurrentLogLevel >= LogLevel.Verbose)
            {
                Debug.Log($"[UFSM VERBOSE] {message}");
            }
        }

        public void LogStateChange(string machineName, string fromState, string toState)
        {
            if (CurrentLogLevel >= LogLevel.Info)
            {
                Debug.Log($"[UFSM STATE] {machineName}: {fromState} → {toState}");
            }
        }

        public void LogTransitionEvaluation(string machineName, string transition, bool result)
        {
            if (CurrentLogLevel >= LogLevel.Verbose)
            {
                Debug.Log($"[UFSM TRANSITION] {machineName}: {transition} = {result}");
            }
        }

        public void LogParameterChange(string parameterName, object oldValue, object newValue)
        {
            if (CurrentLogLevel >= LogLevel.Verbose)
            {
                Debug.Log($"[UFSM PARAM] {parameterName}: {oldValue} → {newValue}");
            }
        }
    }
}
