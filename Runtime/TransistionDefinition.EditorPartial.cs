// Assets/UFSM/Runtime/TransitionDefinition.EditorPartial.cs
using UnityEngine;

namespace UFSM.Runtime
{
    // Editor-time GUID references for transitions (backwards-compatible).
    public partial class TransitionDefinition
    {
        [HideInInspector]
        public string FromStateGuid;

        [HideInInspector]
        public string ToStateGuid;
    }
}
