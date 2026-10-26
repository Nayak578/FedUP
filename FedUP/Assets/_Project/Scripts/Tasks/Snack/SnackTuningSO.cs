using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Snack
{
    /// <summary>Every number in Grab a Snack (and the juice box). Edit Data/Tasks/SnackTuning.asset.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Snack Tuning", fileName = "SnackTuning")]
    public class SnackTuningSO : ScriptableObject
    {
        [Tooltip("GDD Task 3: items shift if frustration > 30%.")]
        public float slideAbove = 30f;
        [Tooltip("Look at the snack longer than this (above the threshold) and it slides away.")]
        [Min(0.1f)] public float grabWindow = 0.6f;
        [Min(0.1f)] public float slideSeconds = 0.4f;
        [Tooltip("GDD: sarcastic line if hovered > 3 s without grabbing.")]
        [Min(0.5f)] public float sarcasmAfter = 3f;
        [Tooltip("GDD: ghost hand QTE if grabbed after 10 s open.")]
        [Min(1f)] public float handAfterOpen = 10f;
        [Tooltip("The door slams if left open this long (GDD: 'if the player takes too long').")]
        [Min(2f)] public float slamAfter = 15f;
        [Tooltip("Ghost-hand tug: presses needed and seconds to do them in.")]
        [Min(1)] public int handPresses = 10;
        [Min(0.5f)] public float handSeconds = 2.5f;
        [Tooltip("GDD rebalance: the fridge 'occasionally' has a juice box.")]
        [Range(0f, 1f)] public float juiceChance = 0.6f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO failedGrab; // +5
        public FrustrationEventSO qteFail;    // +10
        public FrustrationEventSO success;    // -5
        public FrustrationEventSO sarcasm;    // +3
        public FrustrationEventSO juiceBox;   // -5
    }
}
