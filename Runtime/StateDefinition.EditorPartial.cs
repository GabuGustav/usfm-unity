// Assets/UFSM/Runtime/StateDefinition.EditorPartial.cs
using UnityEngine;

namespace UFSM.Runtime
{
    // Editor-time persistent fields for StateDefinition (GUID + EditorPosition).
    public partial class StateDefinition
    {
        [HideInInspector]
        public string Guid = System.Guid.NewGuid().ToString();
    }
}
