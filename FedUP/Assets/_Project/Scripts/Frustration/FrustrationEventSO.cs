using UnityEngine;

namespace FedUp.Frustration
{
    /// <summary>One line of the frustration table (MACRO_PLAN.md Appendix 1). Edit the asset, not the code.</summary>
    [CreateAssetMenu(menuName = "FedUp/Frustration/Event", fileName = "FE_New")]
    public class FrustrationEventSO : ScriptableObject
    {
        [TextArea] public string description;
        [Tooltip("Percentage points. Positive annoys, negative calms.")]
        public float amount;
        [Tooltip("If ticked, the amount is applied every second while the cause lasts (e.g. smoke alarm +3/s).")]
        public bool perSecond;
    }
}
