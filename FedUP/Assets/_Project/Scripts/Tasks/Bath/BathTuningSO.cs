using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Bath
{
    /// <summary>Every number in Take a Bath. Edit Data/Tasks/BathTuning.asset to tune the feel.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/Bath Tuning", fileName = "BathTuning")]
    public class BathTuningSO : ScriptableObject
    {
        [Tooltip("Seconds of holding E to finish the bath.")]
        [Min(1f)] public float relaxSeconds = 12f;
        [Tooltip("Seconds into the bath before the first snatch.")]
        [Min(0.5f)] public float firstSnatchAt = 2.5f;
        [Tooltip("A new snatch every this many seconds, for as long as you're relaxing (designer: they keep being snatched).")]
        [Min(0.5f)] public float snatchEvery = 2.6f;
        [Tooltip("The key each snatch asks for, in turn.")]
        public BathDirection[] directions = { BathDirection.Left, BathDirection.Up, BathDirection.Right, BathDirection.Down };
        [Tooltip("Seconds you have to snatch a sliding item back.")]
        [Min(0.3f)] public float snatchWindow = 1.6f;
        [Tooltip("Camera height while lying in the bath (metres above the floor).")]
        [Min(0.3f)] public float bathEyeHeight = 0.95f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO itemLost; // +6 each
        public FrustrationEventSO success;  // -10, nothing lost
    }
}
