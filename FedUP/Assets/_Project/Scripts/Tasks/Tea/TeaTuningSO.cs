using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Tea
{
    /// <summary>Every number in Make Tea. Edit Data/Tasks/TeaTuning.asset.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Tea Tuning", fileName = "TeaTuning")]
    public class TeaTuningSO : ScriptableObject
    {
        [Tooltip("GDD: the player can only carry 2 items at once.")]
        [Min(1)] public int carryLimit = 2;
        [Tooltip("GDD: restore power within a time limit.")]
        [Min(5f)] public float powerSeconds = 30f;
        [Tooltip("GDD rebalance: the tea items reset only once.")]
        [Min(0)] public int maxRescatters = 1;
        [Tooltip("Seconds the kettle takes before the ghost strikes.")]
        [Min(1f)] public float boilSeconds = 6f;
        [Tooltip("GDD: dodge thrown objects for 20 seconds.")]
        [Min(3f)] public float dodgeSeconds = 20f;
        [Tooltip("GDD: hit 3 times and the kitchen catches fire.")]
        [Min(1)] public int hitsForFire = 3;
        [Tooltip("A throw every this many seconds.")]
        [Min(0.5f)] public float throwEvery = 1.5f;
        [Tooltip("A cabinet bangs open this long before it throws (so you can read it).")]
        [Min(0f)] public float throwWarning = 0.5f;
        [Tooltip("Seconds a thrown thing takes to reach you.")]
        [Min(0.2f)] public float throwFlight = 0.8f;
        [Tooltip("Within this many metres of your chest when it lands = a hit.")]
        [Min(0.1f)] public float hitRadius = 0.5f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO rescatter; // +10
        public FrustrationEventSO hit;       // +7
        public FrustrationEventSO fire;      // +15
        public FrustrationEventSO made;      // -10
    }
}
