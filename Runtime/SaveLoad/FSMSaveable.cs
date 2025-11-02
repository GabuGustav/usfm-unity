using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime.SaveLoad
{
    /// <summary>
    /// Add this component to enable saving/loading for an FSM Controller
    /// </summary>
    [RequireComponent(typeof(FSMController))]
    public class FSMSaveable : MonoBehaviour
    {
        [Header("Save Settings")]
        public bool AutoSaveWithFSM = true;
        public bool SaveCustomData = false;

        [Header("Custom Data")]
        [TextArea(3, 6)]
        public string CustomSaveDataJson;

        private FSMController controller;
        private IStateMachine fsm;

        private void Awake()
        {
            controller = GetComponent<FSMController>();
        }

        private void Start()
        {
            fsm = controller?.RuntimeFSM;

            if (fsm != null && AutoSaveWithFSM)
            {
                // Subscribe to state changes for history tracking
                fsm.OnStateChanged += OnStateChanged;
            }
        }

        private void OnStateChanged(IState from, IState to)
        {
            // Track transition in save system
            FSMSaveSystem.Instance.TrackTransition(
                fsm.MachineName,
                from?.StateName ?? "None",
                to?.StateName ?? "None"
            );
        }

        /// <summary>
        /// Save this FSM
        /// </summary>
        public FSMSaveData Save()
        {
            if (fsm == null) return null;

            string customData = SaveCustomData ? CustomSaveDataJson : null;
            return FSMSaveSystem.Instance.SaveFSM(fsm, customData);
        }

        /// <summary>
        /// Load this FSM
        /// </summary>
        public void Load(FSMSaveData saveData)
        {
            if (fsm == null) return;

            FSMSaveSystem.Instance.LoadFSM(fsm, saveData);

            if (SaveCustomData && !string.IsNullOrEmpty(saveData.CustomDataJson))
            {
                CustomSaveDataJson = saveData.CustomDataJson;
            }
        }

        private void OnDestroy()
        {
            if (fsm != null)
            {
                fsm.OnStateChanged -= OnStateChanged;
            }
        }
    }
}