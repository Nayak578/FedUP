using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Night
{
    /// <summary>Every number in the final sleep. Edit Data/Tasks/FinalSleepTuning.asset to tune the feel.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Final Sleep Tuning", fileName = "FinalSleepTuning")]
    public class FinalSleepTuningSO : ScriptableObject
    {
        [Tooltip("GDD Task 7: the ghost disrupts you above 50%.")]
        public float disruptAbove = 50f;
        [Tooltip("GDD Task 7: a '3-stage rhythm match'.")]
        [Min(1)] public int beats = 3;
        [Tooltip("GDD Task 7: 'after 3 failed attempts' the nightmare happens.")]
        [Min(1)] public int maxFails = 3;
        [Tooltip("One heartbeat: the marker crosses the bar in this long.")]
        [Min(0.3f)] public float beatSeconds = 1.4f;
        [Range(0f, 1f)] public float zoneStart = 0.45f;
        [Range(0f, 1f)] public float zoneEnd = 0.62f;
        [Tooltip("How long the blanket takes to lift: press E in this time to wake up, or stay asleep.")]
        [Min(2f)] public float choiceSeconds = 8f;
        [Tooltip("Hold E on the bed this long to lie down.")]
        [Min(0f)] public float lieDownHoldSeconds = 1.5f;

        [Header("Frustration events (Appendix 1, Final Sleep)")]
        public FrustrationEventSO scream; // +15
        public FrustrationEventSO clean;  // -20
        public FrustrationEventSO retry;  // +10 once
    }
}
