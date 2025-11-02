using System;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Global singleton parameter store accessible by all FSMs
    /// </summary>
    public class GlobalParameterStore
    {
        private static GlobalParameterStore instance;
        public static GlobalParameterStore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GlobalParameterStore();
                }
                return instance;
            }
        }

        private ParameterStore store;

        private GlobalParameterStore()
        {
            store = new ParameterStore();
        }

        // Expose all ParameterStore methods
        public void AddParameter(string name, ParameterType type, object defaultValue = null)
            => store.AddParameter(name, type, defaultValue);

        public void RemoveParameter(string name) => store.RemoveParameter(name);
        public bool HasParameter(string name) => store.HasParameter(name);

        public T GetValue<T>(string name) => store.GetValue<T>(name);
        public void SetValue(string name, object value) => store.SetValue(name, value);

        public bool GetBool(string name) => store.GetBool(name);
        public void SetBool(string name, bool value) => store.SetBool(name, value);

        public int GetInt(string name) => store.GetInt(name);
        public void SetInt(string name, int value) => store.SetInt(name, value);

        public float GetFloat(string name) => store.GetFloat(name);
        public void SetFloat(string name, float value) => store.SetFloat(name, value);

        public string GetString(string name) => store.GetString(name);
        public void SetString(string name, string value) => store.SetString(name, value);

        public void SetTrigger(string name) => store.SetTrigger(name);
        public void ResetTrigger(string name) => store.ResetTrigger(name);
        public bool IsTriggerSet(string name) => store.IsTriggerSet(name);

        public void ResetAll() => store.ResetAll();

        public event Action<string, object> OnParameterChanged
        {
            add => store.OnParameterChanged += value;
            remove => store.OnParameterChanged -= value;
        }

        // Internal
        internal void ResetTriggersAfterFrame() => store.ResetTriggersAfterFrame();
    }
}