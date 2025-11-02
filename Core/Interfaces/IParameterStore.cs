using System;
using System.Collections.Generic;

namespace UFSM.Core
{
    /// <summary>
    /// Parameter types supported by the FSM system
    /// </summary>
    public enum ParameterType
    {
        Bool,
        Int,
        Float,
        Trigger,
        String
    }

    /// <summary>
    /// Represents a parameter in the system
    /// </summary>
    public interface IParameter
    {
        string Name { get; }
        ParameterType Type { get; }
        object Value { get; set; }
        object DefaultValue { get; }
        void Reset();
    }

    /// <summary>
    /// Parameter store interface (supports both Global and Per-FSM parameters)
    /// </summary>
    public interface IParameterStore
    {
        /// <summary>
        /// Add a new parameter
        /// </summary>
        void AddParameter(string name, ParameterType type, object defaultValue = null);

        /// <summary>
        /// Remove a parameter
        /// </summary>
        void RemoveParameter(string name);

        /// <summary>
        /// Check if parameter exists
        /// </summary>
        bool HasParameter(string name);

        /// <summary>
        /// Get parameter value (generic)
        /// </summary>
        T GetValue<T>(string name);

        /// <summary>
        /// Set parameter value
        /// </summary>
        void SetValue(string name, object value);

        /// <summary>
        /// Get bool parameter
        /// </summary>
        bool GetBool(string name);

        /// <summary>
        /// Set bool parameter
        /// </summary>
        void SetBool(string name, bool value);

        /// <summary>
        /// Get int parameter
        /// </summary>
        int GetInt(string name);

        /// <summary>
        /// Set int parameter
        /// </summary>
        void SetInt(string name, int value);

        /// <summary>
        /// Get float parameter
        /// </summary>
        float GetFloat(string name);

        /// <summary>
        /// Set float parameter
        /// </summary>
        void SetFloat(string name, float value);

        /// <summary>
        /// Get string parameter
        /// </summary>
        string GetString(string name);

        /// <summary>
        /// Set string parameter
        /// </summary>
        void SetString(string name, string value);

        /// <summary>
        /// Set a trigger (auto-resets after one frame)
        /// </summary>
        void SetTrigger(string name);

        /// <summary>
        /// Reset a trigger manually
        /// </summary>
        void ResetTrigger(string name);

        /// <summary>
        /// Check if trigger is set
        /// </summary>
        bool IsTriggerSet(string name);

        /// <summary>
        /// Reset all parameters to default values
        /// </summary>
        void ResetAll();

        /// <summary>
        /// Get all parameter names
        /// </summary>
        IReadOnlyList<string> GetAllParameterNames();

        /// <summary>
        /// Get a specific parameter
        /// </summary>
        IParameter GetParameter(string name);

        /// <summary>
        /// Event triggered when a parameter changes
        /// </summary>
        event Action<string, object> OnParameterChanged;
    }
}