using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Cleanup
{
    /// <summary>Every number in Clean Up Mess. Edit Data/Tasks/CleanupTuning.asset to tune the feel.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Cleanup Tuning", fileName = "CleanupTuning")]
    public class CleanupTuningSO : ScriptableObject
    {
        [Tooltip("GDD: above 40% frustration the ghost interferes.")]
        public float interferenceAbove = 40f;
        [Tooltip("GDD: 30-50% chance a drop is rejected.")]
        [Range(0f, 1f)] public float rejectChance = 0.4f;
        [Min(1)] public int streakForBreak = 3;
        [Tooltip("GDD: after 3 in a row the ghost stops interfering for 60 s.")]
        [Min(0f)] public float ghostBreakSeconds = 60f;
        [Tooltip("GDD: room clean for 30 s (timer starts after a 3-item streak) gives -10.")]
        [Min(0f)] public float cleanRoomSeconds = 30f;
        [Tooltip("The narrative page's joke: the first time you bin 3 in a row, the third flies back out.")]
        public bool comedyFlybackOnce = true;
        [Tooltip("Seconds between little ghost twitches of the clutter.")]
        [Min(1f)] public float twitchEverySeconds = 6f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO flyback;   // failed placement / flyback +6
        public FrustrationEventSO giggle;    // ghost giggle +2
        public FrustrationEventSO roomClean; // -10
    }
}
