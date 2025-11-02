using UnityEngine;

namespace UFSM.Runtime
{
    // Add editor position to StateDefinition (partial class extension)
    public partial class StateDefinition
    {
        [HideInInspector]
        public Vector2 EditorPosition;
    }
}