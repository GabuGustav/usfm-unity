using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// MonoBehaviour component that runs an FSM from a ScriptableObject definition.
    /// Drag-drop your FSMDefinition asset onto this component.
    /// </summary>
    public class FSMController : MonoBehaviour
    {
        [Header("FSM Configuration")]
        public FSMDefinition FSMDefinition;

        [Header("Context")]
        [Tooltip("Optional: Custom context component on this GameObject")]
        public MonoBehaviour CustomContextComponent;

        [Header("Animator Integration")]
        public Animator Animator;
        public bool AutoFindAnimator = true;

        [Header("Audio")]
        public AudioSource AudioSource;

        [Header("Runtime Info")]
        [SerializeField] private string currentStateName;
        [SerializeField] private string previousStateName;

        private IStateMachine runtimeFSM;
        private IContext context;
        private FSMAnimatorSync animatorSync;

        // Public accessors
        public IStateMachine RuntimeFSM => runtimeFSM;
        public IContext Context => context;
        public string CurrentState => currentStateName;
        public string PreviousState => previousStateName;

        private void Awake()
        {
            if (FSMDefinition == null)
            {
                Debug.LogError($"[FSMController] No FSM Definition assigned on {gameObject.name}");
                enabled = false;
                return;
            }

            // Find Animator if needed
            if (AutoFindAnimator && Animator == null)
            {
                Animator = GetComponent<Animator>();
            }

            // Find AudioSource if needed
            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
            }

            InitializeFSM();
        }

        private void InitializeFSM()
        {
            // Create context
            context = CreateContext();

            // Build runtime FSM from definition
            runtimeFSM = FSMBuilder.BuildFromDefinition(FSMDefinition, context);

            // Setup animator sync
            if (FSMDefinition.SyncWithAnimator && Animator != null)
            {
                animatorSync = new FSMAnimatorSync(runtimeFSM, Animator, FSMDefinition);
                animatorSync.Initialize();
            }

            // Subscribe to state changes
            runtimeFSM.OnStateChanged += OnStateChanged;

            // Register with manager
            FSMManager.Instance.RegisterFSM(runtimeFSM);

            // Initialize and start
            runtimeFSM.Initialize();
            runtimeFSM.Start(FSMDefinition.InitialStateName);

            Debug.Log($"[FSMController] Initialized FSM '{FSMDefinition.MachineName}' on {gameObject.name}");
        }

        private IContext CreateContext()
        {
            // Try to use custom context component
            if (CustomContextComponent != null && CustomContextComponent is IContext customContext)
            {
                return customContext;
            }

            // Create default context
            return new GenericContext(gameObject);
        }

        private void OnStateChanged(IState from, IState to)
        {
            previousStateName = from?.StateName ?? "None";
            currentStateName = to?.StateName ?? "None";

            // Find state definition
            var stateDef = FSMDefinition.GetState(currentStateName);
            if (stateDef != null)
            {
                // Trigger animation
                if (stateDef.TriggerAnimationOnEnter && Animator != null && !string.IsNullOrEmpty(stateDef.AnimationTriggerParameter))
                {
                    Animator.SetTrigger(stateDef.AnimationTriggerParameter);
                }

                // Play enter sound
                if (stateDef.EnterSound != null && AudioSource != null)
                {
                    AudioSource.PlayOneShot(stateDef.EnterSound);
                }

                // Invoke enter event
                stateDef.OnEnterEvent?.Invoke();
            }

            // Find previous state for exit sound
            if (from != null)
            {
                var prevStateDef = FSMDefinition.GetState(previousStateName);
                if (prevStateDef != null)
                {
                    // Play exit sound
                    if (prevStateDef.ExitSound != null && AudioSource != null)
                    {
                        AudioSource.PlayOneShot(prevStateDef.ExitSound);
                    }

                    // Invoke exit event
                    prevStateDef.OnExitEvent?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            if (runtimeFSM != null)
            {
                FSMManager.Instance.UnregisterFSM(runtimeFSM);
                runtimeFSM.Cleanup();
            }

            animatorSync?.Cleanup();
        }

        // Public API
        public void ForceTransition(string stateName)
        {
            runtimeFSM?.ForceTransition(stateName);
        }

        public void SetParameter(string name, object value)
        {
            runtimeFSM?.GetParameterStore()?.SetValue(name, value);
        }

        public T GetParameter<T>(string name)
        {
            var store = runtimeFSM?.GetParameterStore();
            if (store == null) return default(T);
            return store.GetValue<T>(name);
        }

        public void PauseFSM()
        {
            if (runtimeFSM != null)
            {
                runtimeFSM.IsActive = false;
            }
        }

        public void ResumeFSM()
        {
            if (runtimeFSM != null)
            {
                runtimeFSM.IsActive = true;
            }
        }
    }

    /// <summary>
    /// Generic context for FSMController when no custom context is provided
    /// </summary>
    public class GenericContext : UFSM.Core.ContextBase
    {
        public GenericContext(GameObject owner) : base(owner) { }
    }
}