using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Phone
{
    /// <summary>Boss calls #2-4. Edit Data/Tasks/CallTuning.asset to tune them.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Call Tuning", fileName = "CallTuning")]
    public class CallTuningSO : ScriptableObject
    {
        [Tooltip("A ring every this many seconds. At 2 s, +2 per ring is the table's +1 per second.")]
        [Min(0.5f)] public float ringEverySeconds = 2f;
        [Tooltip("GDD: 'if left ringing >20 s'. After this it rings out.")]
        [Min(3f)] public float ringOutSeconds = 20f;
        [Tooltip("'Found and answered quickly': within this many seconds of the first ring.")]
        [Min(1f)] public float quickWithinSeconds = 10f;

        [Header("Frustration events (Appendix 1, Phone #2-4)")]
        public FrustrationEventSO ringAfterFirst; // +2
        public FrustrationEventSO distorted;      // +5
        public FrustrationEventSO answeredQuickly; // -5
    }
}
