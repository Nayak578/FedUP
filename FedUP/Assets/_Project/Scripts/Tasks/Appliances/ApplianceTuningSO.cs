using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Appliances
{
    /// <summary>Every number in appliance chaos. Edit Data/Tasks/ApplianceTuning.asset.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Appliance Tuning", fileName = "ApplianceTuning")]
    public class ApplianceTuningSO : ScriptableObject
    {
        [Tooltip("GDD Task 6: 10 s to shut it off before it overloads.")]
        [Min(2f)] public float timerSeconds = 10f;
        [Tooltip("GDD: hold to deactivate.")]
        [Min(0.2f)] public float holdSeconds = 1f;
        [Tooltip("GDD: above 50% the ghost may cover the control.")]
        public float blockAbove = 50f;
        [Range(0f, 1f)] public float blockChance = 0.5f;
        [Tooltip("Torch scare: presses of E with the torch on it.")]
        [Min(1)] public int scarePresses = 5;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO notShutOff; // +10
        public FrustrationEventSO blocked;    // +7
        public FrustrationEventSO scareOff;   // -5
    }
}
