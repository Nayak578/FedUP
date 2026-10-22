using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Toast
{
    /// <summary>Every number in Make Toast. Edit Data/Tasks/ToastTuning.asset to tune the feel.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Toast Tuning", fileName = "ToastTuning")]
    public class ToastTuningSO : ScriptableObject
    {
        [Header("Timing bar")]
        [Tooltip("Seconds for the marker to go from raw to burnt.")]
        [Min(0.5f)] public float sweepSeconds = 3.5f;
        [Range(0f, 1f)] public float goldenZoneStart = 0.60f;
        [Range(0f, 1f)] public float goldenZoneEnd = 0.75f;
        [Tooltip("After the bar ends, how long before the toast is declared burnt.")]
        [Min(0f)] public float graceAfterEnd = 0.3f;

        [Header("Jam (GDD: above 30% frustration)")]
        public float jamAboveFrustration = 30f;
        [Min(1)] public int mashPresses = 8;
        [Min(0.5f)] public float mashSeconds = 3f;

        [Header("Golden toast and the smoke alarm")]
        [Tooltip("GDD: toast not picked up in 15 s gets eaten.")]
        [Min(1f)] public float pickupSeconds = 15f;
        [Min(1)] public int alarmAfterFailuresInARow = 2;
        [Min(0f)] public float cooldownAfterFailure = 0.8f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO failed;      // burnt toast or failed catch +10
        public FrustrationEventSO success;     // golden toast taken -5
        public FrustrationEventSO eatenByGhost; // not picked up in time +10
    }
}
