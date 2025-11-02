using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UFSM.Core;

namespace UFSM.Runtime.SaveLoad
{
    /// <summary>
    /// Handles saving and loading of FSM states
    /// </summary>
    public class FSMSaveSystem
    {
        private static FSMSaveSystem instance;
        public static FSMSaveSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FSMSaveSystem();
                }
                return instance;
            }
        }

        private const string SAVE_FOLDER = "FSMSaves";
        private const string AUTO_SAVE_NAME = "autosave";

        // Auto-save settings
        public bool AutoSaveEnabled { get; set; } = false;
        public float AutoSaveInterval { get; set; } = 60f; // seconds

        private float autoSaveTimer = 0f;
        private IFSMLogger logger => FSMManager.Instance?.Logger;

        // Track transition history (optional)
        private Dictionary<string, List<string>> transitionHistories = new Dictionary<string, List<string>>();
        private int maxHistoryLength = 50;

        private FSMSaveSystem() { }

        // Update auto-save timer (call from FSMManager)
        public void Update(float deltaTime)
        {
            if (!AutoSaveEnabled) return;

            autoSaveTimer += deltaTime;
            if (autoSaveTimer >= AutoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }

        /// <summary>
        /// Save a single FSM to JSON
        /// </summary>
        public FSMSaveData SaveFSM(IStateMachine fsm, string customDataJson = null)
        {
            var saveData = new FSMSaveData
            {
                MachineName = fsm.MachineName,
                CurrentState = fsm.CurrentState?.StateName ?? "",
                PreviousState = fsm.PreviousState?.StateName ?? "",
                SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsActive = fsm.IsActive,
                Priority = fsm.Priority,
                UpdateMode = fsm.TransitionUpdateMode.ToString(),
                TransitionCheckInterval = fsm.TransitionCheckInterval,
                CustomDataJson = customDataJson
            };

            // Save parameters
            var paramStore = fsm.GetParameterStore();
            if (paramStore != null)
            {
                foreach (var paramName in paramStore.GetAllParameterNames())
                {
                    var param = paramStore.GetParameter(paramName);
                    if (param != null)
                    {
                        saveData.Parameters.Add(new ParameterSaveData
                        {
                            Name = param.Name,
                            Type = param.Type.ToString(),
                            Value = param.Value?.ToString() ?? ""
                        });
                    }
                }
            }

            // Save transition history
            if (transitionHistories.ContainsKey(fsm.MachineName))
            {
                saveData.TransitionHistory = new List<string>(transitionHistories[fsm.MachineName]);
            }

            logger?.LogInfo($"Saved FSM: {fsm.MachineName}");
            return saveData;
        }

        /// <summary>
        /// Load FSM state from save data
        /// </summary>
        public void LoadFSM(IStateMachine fsm, FSMSaveData saveData)
        {
            if (saveData.MachineName != fsm.MachineName)
            {
                logger?.LogWarning($"FSM name mismatch: {fsm.MachineName} vs {saveData.MachineName}");
            }

            // Restore parameters
            var paramStore = fsm.GetParameterStore();
            if (paramStore != null)
            {
                foreach (var paramData in saveData.Parameters)
                {
                    try
                    {
                        var paramType = (ParameterType)Enum.Parse(typeof(ParameterType), paramData.Type);

                        switch (paramType)
                        {
                            case ParameterType.Bool:
                                paramStore.SetBool(paramData.Name, bool.Parse(paramData.Value));
                                break;
                            case ParameterType.Int:
                                paramStore.SetInt(paramData.Name, int.Parse(paramData.Value));
                                break;
                            case ParameterType.Float:
                                paramStore.SetFloat(paramData.Name, float.Parse(paramData.Value));
                                break;
                            case ParameterType.String:
                                paramStore.SetString(paramData.Name, paramData.Value);
                                break;
                            case ParameterType.Trigger:
                                if (bool.Parse(paramData.Value))
                                {
                                    paramStore.SetTrigger(paramData.Name);
                                }
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.LogError($"Error loading parameter {paramData.Name}: {e.Message}");
                    }
                }
            }

            // Restore state
            fsm.IsActive = saveData.IsActive;
            fsm.Priority = saveData.Priority;

            if (Enum.TryParse<UpdateMode>(saveData.UpdateMode, out var updateMode))
            {
                fsm.TransitionUpdateMode = updateMode;
            }
            fsm.TransitionCheckInterval = saveData.TransitionCheckInterval;

            // Transition to saved state
            if (!string.IsNullOrEmpty(saveData.CurrentState))
            {
                fsm.ForceTransition(saveData.CurrentState);
            }

            // Restore history
            if (saveData.TransitionHistory.Count > 0)
            {
                transitionHistories[fsm.MachineName] = new List<string>(saveData.TransitionHistory);
            }

            logger?.LogInfo($"Loaded FSM: {fsm.MachineName} (State: {saveData.CurrentState})");
        }

        /// <summary>
        /// Save all active FSMs to file
        /// </summary>
        public void SaveAllFSMs(string saveName)
        {
            var allFSMs = FSMManager.Instance.GetAllFSMs();
            var collectionData = new FSMCollectionSaveData
            {
                SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SceneName = SceneManager.GetActiveScene().name
            };

            foreach (var fsm in allFSMs)
            {
                collectionData.FSMs.Add(SaveFSM(fsm));
            }

            string json = JsonUtility.ToJson(collectionData, true);
            string path = GetSavePath(saveName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
                logger?.LogInfo($"Saved all FSMs to: {saveName}");
            }
            catch (Exception e)
            {
                logger?.LogError($"Failed to save FSMs: {e.Message}");
            }
        }

        /// <summary>
        /// Load all FSMs from file
        /// </summary>
        public void LoadAllFSMs(string saveName)
        {
            string path = GetSavePath(saveName);

            if (!File.Exists(path))
            {
                logger?.LogWarning($"Save file not found: {saveName}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var collectionData = JsonUtility.FromJson<FSMCollectionSaveData>(json);

                var allFSMs = FSMManager.Instance.GetAllFSMs();

                foreach (var saveData in collectionData.FSMs)
                {
                    var fsm = allFSMs.FirstOrDefault(f => f.MachineName == saveData.MachineName);
                    if (fsm != null)
                    {
                        LoadFSM(fsm, saveData);
                    }
                    else
                    {
                        logger?.LogWarning($"FSM not found for loading: {saveData.MachineName}");
                    }
                }

                logger?.LogInfo($"Loaded all FSMs from: {saveName}");
            }
            catch (Exception e)
            {
                logger?.LogError($"Failed to load FSMs: {e.Message}");
            }
        }

        /// <summary>
        /// Auto-save all FSMs
        /// </summary>
        public void AutoSave()
        {
            SaveAllFSMs(AUTO_SAVE_NAME);
            logger?.LogVerbose("Auto-saved FSMs");
        }

        /// <summary>
        /// Load auto-save
        /// </summary>
        public void LoadAutoSave()
        {
            LoadAllFSMs(AUTO_SAVE_NAME);
        }

        /// <summary>
        /// Delete a save file
        /// </summary>
        public void DeleteSave(string saveName)
        {
            string path = GetSavePath(saveName);

            if (File.Exists(path))
            {
                File.Delete(path);
                logger?.LogInfo($"Deleted save: {saveName}");
            }
        }

        /// <summary>
        /// Check if save exists
        /// </summary>
        public bool SaveExists(string saveName)
        {
            return File.Exists(GetSavePath(saveName));
        }

        /// <summary>
        /// Get all save names
        /// </summary>
        public List<string> GetAllSaveNames()
        {
            string saveDir = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

            if (!Directory.Exists(saveDir))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(saveDir, "*.json");
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        }

        /// <summary>
        /// Track transition (for history)
        /// </summary>
        public void TrackTransition(string machineName, string fromState, string toState)
        {
            if (!transitionHistories.ContainsKey(machineName))
            {
                transitionHistories[machineName] = new List<string>();
            }

            var history = transitionHistories[machineName];
            string transitionRecord = $"{fromState} -> {toState} ({DateTime.Now:HH:mm:ss})";
            history.Add(transitionRecord);

            // Limit history length
            if (history.Count > maxHistoryLength)
            {
                history.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get transition history for an FSM
        /// </summary>
        public List<string> GetTransitionHistory(string machineName)
        {
            return transitionHistories.ContainsKey(machineName)
                ? new List<string>(transitionHistories[machineName])
                : new List<string>();
        }

        /// <summary>
        /// Clear all histories
        /// </summary>
        public void ClearHistories()
        {
            transitionHistories.Clear();
        }

        private string GetSavePath(string saveName)
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, $"{saveName}.json");
        }
    }
}
