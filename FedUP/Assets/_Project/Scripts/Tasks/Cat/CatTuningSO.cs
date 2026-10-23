using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Cat
{
    /// <summary>Every number in a cat visit. Edit Data/Tasks/CatTuning.asset to tune the feel.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Cat Tuning", fileName = "CatTuning")]
    public class CatTuningSO : ScriptableObject
    {
        [Tooltip("GDD Task 8: above 60% the cat may hiss.")]
        public float hissAbove = 60f;
        [Tooltip("GDD Task 8: 50% chance to hiss.")]
        [Range(0f, 1f)] public float hissChance = 0.5f;
        [Tooltip("GDD Task 8: retry allowed after 30 s.")]
        [Min(0f)] public float retryCooldownSeconds = 30f;
        [Tooltip("How long it sits waiting for you before walking off. Not in the GDD; long enough to notice it and walk over.")]
        [Min(1f)] public float patienceSeconds = 20f;
        [Tooltip("GDD: 'walks in while the player is sitting or standing still'. Seconds of stillness before it comes.")]
        [Min(0f)] public float arriveAfterStillSeconds = 1.5f;
        [Tooltip("GDD: it walks in slowly.")]
        [Min(0.1f)] public float walkSpeed = 1.1f;
        [Tooltip("Where it sits down, metres from you.")]
        [Min(0.5f)] public float sitDistance = 1.4f;
        [Tooltip("GDD: hold to pet.")]
        [Min(0f)] public float petHoldSeconds = 1.5f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO petted;           // -20
        public FrustrationEventSO pettedAfterHiss;  // -10
    }
}
