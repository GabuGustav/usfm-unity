using System;
using System.Collections.Generic;
using System.Linq;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Concrete implementation of a parameter
    /// </summary>
    public class Parameter : IParameter
    {
        public string Name { get; private set; }
        public ParameterType Type { get; private set; }
        public object DefaultValue { get; private set; }

        private object currentValue;
        public object Value
        {
            get => currentValue;
            set
            {
                // Type validation
                if (!IsValidType(value))
                {
                    throw new ArgumentException($"Invalid type for parameter '{Name}'. Expected {Type}");
                }
                currentValue = value;
            }
        }

        public Parameter(string name, ParameterType type, object defaultValue = null)
        {
            Name = name;
            Type = type;
            DefaultValue = defaultValue ?? GetDefaultForType(type);
            currentValue = DefaultValue;
        }

        public void Reset()
        {
            currentValue = DefaultValue;
        }

        private bool IsValidType(object value)
        {
            if (value == null) return false;

            return Type switch
            {
                ParameterType.Bool => value is bool,
                ParameterType.Int => value is int,
                ParameterType.Float => value is float,
                ParameterType.Trigger => value is bool,
                ParameterType.String => value is string,
                _ => false
            };
        }

        private object GetDefaultForType(ParameterType type)
        {
            return type switch
            {
                ParameterType.Bool => false,
                ParameterType.Int => 0,
                ParameterType.Float => 0f,
                ParameterType.Trigger => false,
                ParameterType.String => string.Empty,
                _ => null
            };
        }
    }

    /// <summary>
    /// Parameter store implementation (supports both Global and Per-FSM)
    /// </summary>
    public class ParameterStore : IParameterStore
    {
        private Dictionary<string, Parameter> parameters = new Dictionary<string, Parameter>();
        private HashSet<string> activeTriggers = new HashSet<string>();

        public event Action<string, object> OnParameterChanged;

        // Add/Remove parameters
        public void AddParameter(string name, ParameterType type, object defaultValue = null)
        {
            if (parameters.ContainsKey(name))
            {
                UnityEngine.Debug.LogWarning($"Parameter '{name}' already exists. Skipping.");
                return;
            }

            parameters[name] = new Parameter(name, type, defaultValue);
        }

        public void RemoveParameter(string name)
        {
            parameters.Remove(name);
            activeTriggers.Remove(name);
        }

        public bool HasParameter(string name) => parameters.ContainsKey(name);

        // Generic get/set
        public T GetValue<T>(string name)
        {
            if (!parameters.ContainsKey(name))
            {
                UnityEngine.Debug.LogError($"Parameter '{name}' not found");
                return default;
            }

            return (T)parameters[name].Value;
        }

        public void SetValue(string name, object value)
        {
            if (!parameters.ContainsKey(name))
            {
                UnityEngine.Debug.LogError($"Parameter '{name}' not found");
                return;
            }

            var oldValue = parameters[name].Value;
            parameters[name].Value = value;
            OnParameterChanged?.Invoke(name, value);

            // Log if logger available
            FSMManager.Instance?.Logger?.LogParameterChange(name, oldValue, value);
        }

        // Bool parameters
        public bool GetBool(string name) => GetValue<bool>(name);
        public void SetBool(string name, bool value) => SetValue(name, value);

        // Int parameters
        public int GetInt(string name) => GetValue<int>(name);
        public void SetInt(string name, int value) => SetValue(name, value);

        // Float parameters
        public float GetFloat(string name) => GetValue<float>(name);
        public void SetFloat(string name, float value) => SetValue(name, value);

        // String parameters
        public string GetString(string name) => GetValue<string>(name);
        public void SetString(string name, string value) => SetValue(name, value);

        // Trigger parameters
        public void SetTrigger(string name)
        {
            if (!parameters.ContainsKey(name))
            {
                UnityEngine.Debug.LogError($"Trigger '{name}' not found");
                return;
            }

            if (parameters[name].Type != ParameterType.Trigger)
            {
                UnityEngine.Debug.LogError($"Parameter '{name}' is not a trigger");
                return;
            }

            activeTriggers.Add(name);
            SetValue(name, true);
        }

        public void ResetTrigger(string name)
        {
            activeTriggers.Remove(name);
            if (parameters.ContainsKey(name))
            {
                SetValue(name, false);
            }
        }

        public bool IsTriggerSet(string name)
        {
            return activeTriggers.Contains(name);
        }

        // Internal: Reset triggers (called by FSM manager each frame)
        public void ResetTriggersAfterFrame()
        {
            foreach (var trigger in activeTriggers.ToList())
            {
                ResetTrigger(trigger);
            }
        }

        // Reset all
        public void ResetAll()
        {
            foreach (var param in parameters.Values)
            {
                param.Reset();
            }
            activeTriggers.Clear();
        }

        // Query methods
        public IReadOnlyList<string> GetAllParameterNames() => parameters.Keys.ToList();
        public IParameter GetParameter(string name) => parameters.ContainsKey(name) ? parameters[name] : null;
    }
}