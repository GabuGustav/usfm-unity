using System.Collections.Generic;
using UnityEngine;
using UFSM.Core;

namespace UFSM.Runtime
{
    /// <summary>
    /// Synchronizes FSM parameters with Unity Animator parameters
    /// </summary>
    public class FSMAnimatorSync
    {
        private IStateMachine fsm;
        private Animator animator;
        private FSMDefinition definition;

        private Dictionary<string, AnimatorControllerParameterType> animatorParams;
        private bool isInitialized = false;

        public FSMAnimatorSync(IStateMachine fsm, Animator animator, FSMDefinition definition)
        {
            this.fsm = fsm;
            this.animator = animator;
            this.definition = definition;
        }

        public void Initialize()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[FSMAnimatorSync] Animator or AnimatorController is null. Sync disabled.");
                return;
            }

            // Cache animator parameters
            animatorParams = new Dictionary<string, AnimatorControllerParameterType>();
            foreach (var param in animator.parameters)
            {
                animatorParams[param.name] = param.type;
            }

            // Auto-create animator parameters if enabled
            if (definition.AutoCreateAnimatorParameters)
            {
                CreateMissingAnimatorParameters();
            }

            // Subscribe to parameter changes
            var paramStore = fsm.GetParameterStore();
            if (paramStore != null)
            {
                paramStore.OnParameterChanged += OnFSMParameterChanged;
            }

            isInitialized = true;
            Debug.Log($"[FSMAnimatorSync] Initialized for FSM '{fsm.MachineName}'");
        }

        private void CreateMissingAnimatorParameters()
        {
            // Note: Unity doesn't allow runtime creation of Animator parameters
            // This would need to be done in the Editor
            // For now, we just log warnings for missing parameters

            foreach (var paramDef in definition.Parameters)
            {
                if (!paramDef.SyncToAnimator) continue;

                string animParamName = string.IsNullOrEmpty(paramDef.AnimatorParameterName)
                    ? paramDef.Name
                    : paramDef.AnimatorParameterName;

                if (!animatorParams.ContainsKey(animParamName))
                {
                    Debug.LogWarning($"[FSMAnimatorSync] Animator parameter '{animParamName}' not found. " +
                        $"Please add it to the Animator Controller.");
                }
            }
        }

        private void OnFSMParameterChanged(string paramName, object value)
        {
            var paramDef = definition.GetParameter(paramName);
            if (paramDef == null || !paramDef.SyncToAnimator) return;

            string animParamName = string.IsNullOrEmpty(paramDef.AnimatorParameterName)
                ? paramDef.Name
                : paramDef.AnimatorParameterName;

            if (!animatorParams.ContainsKey(animParamName)) return;

            // Sync to animator based on type
            try
            {
                switch (paramDef.Type)
                {
                    case ParameterType.Bool:
                        animator.SetBool(animParamName, (bool)value);
                        break;

                    case ParameterType.Int:
                        animator.SetInteger(animParamName, (int)value);
                        break;

                    case ParameterType.Float:
                        animator.SetFloat(animParamName, (float)value);
                        break;

                    case ParameterType.Trigger:
                        if ((bool)value)
                        {
                            animator.SetTrigger(animParamName);
                        }
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FSMAnimatorSync] Error syncing parameter '{animParamName}': {e.Message}");
            }
        }

        /// <summary>
        /// Sync animator parameters back to FSM (two-way sync)
        /// Call this if you need Animator → FSM sync
        /// </summary>
        public void SyncFromAnimator()
        {
            if (!isInitialized) return;

            var paramStore = fsm.GetParameterStore();
            if (paramStore == null) return;

            foreach (var paramDef in definition.Parameters)
            {
                if (!paramDef.SyncToAnimator) continue;

                string animParamName = string.IsNullOrEmpty(paramDef.AnimatorParameterName)
                    ? paramDef.Name
                    : paramDef.AnimatorParameterName;

                if (!animatorParams.ContainsKey(animParamName)) continue;

                try
                {
                    switch (paramDef.Type)
                    {
                        case ParameterType.Bool:
                            paramStore.SetBool(paramDef.Name, animator.GetBool(animParamName));
                            break;

                        case ParameterType.Int:
                            paramStore.SetInt(paramDef.Name, animator.GetInteger(animParamName));
                            break;

                        case ParameterType.Float:
                            paramStore.SetFloat(paramDef.Name, animator.GetFloat(animParamName));
                            break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[FSMAnimatorSync] Error reading animator parameter '{animParamName}': {e.Message}");
                }
            }
        }

        public void Cleanup()
        {
            if (fsm != null)
            {
                var paramStore = fsm.GetParameterStore();
                if (paramStore != null)
                {
                    paramStore.OnParameterChanged -= OnFSMParameterChanged;
                }
            }
        }
    }
}