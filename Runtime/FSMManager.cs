using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Central manager for all FSMs in the scene.
    /// Handles updates, priority ordering, and global coordination.
    /// </summary>
    public class FSMManager : MonoBehaviour
    {
        private static FSMManager instance;
        public static FSMManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[UFSM Manager]");
                    instance = go.AddComponent<FSMManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // Public properties
        public IFSMLogger Logger { get; private set; }
        public bool IsPaused { get; set; } = false;

        // FSM tracking
        private List<IStateMachine> registeredFSMs = new List<IStateMachine>();
        private List<IStateMachine> sortedFSMs = new List<IStateMachine>();
        private bool needsResorting = false;

        // Statistics
        public int ActiveFSMCount => registeredFSMs.Count(fsm => fsm.IsActive);
        public int TotalFSMCount => registeredFSMs.Count;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Logger = new FSMLogger();
            Logger.LogInfo("FSM Manager initialized");
        }

        // FSM Registration
        public void RegisterFSM(IStateMachine fsm)
        {
            if (!registeredFSMs.Contains(fsm))
            {
                registeredFSMs.Add(fsm);
                needsResorting = true;
                Logger?.LogInfo($"Registered FSM: {fsm.MachineName}");
            }
        }

        public void UnregisterFSM(IStateMachine fsm)
        {
            if (registeredFSMs.Remove(fsm))
            {
                needsResorting = true;
                Logger?.LogInfo($"Unregistered FSM: {fsm.MachineName}");
            }
        }

        // Unity Update Methods
        private void Update()
        {
            if (IsPaused) return;

            // Resort if needed (when priorities change or FSMs added/removed)
            if (needsResorting)
            {
                SortFSMs();
                needsResorting = false;
            }

            // Update all active FSMs in priority order
            float deltaTime = Time.deltaTime;
            foreach (var fsm in sortedFSMs)
            {
                if (fsm.IsActive)
                {
                    fsm.Update(deltaTime);
                }
            }

            // Reset all triggers after frame
            GlobalParameterStore.Instance.ResetTriggersAfterFrame();
            foreach (var fsm in sortedFSMs)
            {
                if (fsm.IsActive)
                {
                    (fsm.GetParameterStore() as ParameterStore)?.ResetTriggersAfterFrame();
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsPaused) return;

            float fixedDeltaTime = Time.fixedDeltaTime;
            foreach (var fsm in sortedFSMs)
            {
                if (fsm.IsActive)
                {
                    fsm.FixedUpdate(fixedDeltaTime);
                }
            }
        }

        private void LateUpdate()
        {
            if (IsPaused) return;

            float deltaTime = Time.deltaTime;
            foreach (var fsm in sortedFSMs)
            {
                if (fsm.IsActive)
                {
                    fsm.LateUpdate(deltaTime);
                }
            }
        }

        // Sort FSMs by priority (higher priority = updated first)
        private void SortFSMs()
        {
            sortedFSMs = registeredFSMs.OrderByDescending(fsm => fsm.Priority).ToList();
            Logger?.LogVerbose($"FSMs sorted by priority. Count: {sortedFSMs.Count}");
        }

        // Query methods
        public IStateMachine GetFSM(string machineName)
        {
            return registeredFSMs.FirstOrDefault(fsm => fsm.MachineName == machineName);
        }

        public List<IStateMachine> GetAllFSMs() => new List<IStateMachine>(registeredFSMs);

        public void SetPriority(IStateMachine fsm, int priority)
        {
            fsm.Priority = priority;
            needsResorting = true;
        }

        // Global controls
        public void PauseAll()
        {
            IsPaused = true;
            Logger?.LogInfo("All FSMs paused");
        }

        public void ResumeAll()
        {
            IsPaused = false;
            Logger?.LogInfo("All FSMs resumed");
        }

        public void StopAll()
        {
            foreach (var fsm in registeredFSMs)
            {
                fsm.Stop();
            }
            Logger?.LogInfo("All FSMs stopped");
        }

        public void CleanupAll()
        {
            foreach (var fsm in registeredFSMs.ToList())
            {
                fsm.Cleanup();
                UnregisterFSM(fsm);
            }
            Logger?.LogInfo("All FSMs cleaned up");
        }

        private void OnDestroy()
        {
            CleanupAll();
        }
    }
}