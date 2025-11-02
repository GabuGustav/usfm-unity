using System;
using System.Collections.Generic;
using UnityEngine;

namespace UFSM.Runtime.SaveLoad
{
    /// <summary>
    /// Serializable data structure for saving FSM state
    /// </summary>
    [Serializable]
    public class FSMSaveData
    {
        public string MachineName;
        public string CurrentState;
        public string PreviousState;
        public long SaveTimestamp;

        public List<ParameterSaveData> Parameters = new List<ParameterSaveData>();
        public List<string> TransitionHistory = new List<string>();

        // Custom data (user-defined)
        public string CustomDataJson;

        // Metadata
        public bool IsActive;
        public int Priority;
        public string UpdateMode;
        public float TransitionCheckInterval;
    }

    [Serializable]
    public class ParameterSaveData
    {
        public string Name;
        public string Type; // "Bool", "Int", "Float", "String", "Trigger"
        public string Value; // Stored as string, converted on load
    }

    /// <summary>
    /// Save data for multiple FSMs
    /// </summary>
    [Serializable]
    public class FSMCollectionSaveData
    {
        public List<FSMSaveData> FSMs = new List<FSMSaveData>();
        public long SaveTimestamp;
        public string SceneName;
        public int Version = 1;
    }
}