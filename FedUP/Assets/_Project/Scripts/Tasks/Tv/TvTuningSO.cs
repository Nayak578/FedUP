using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Tasks.Tv
{
    /// <summary>Every number in Watch TV. Edit Data/Tasks/TvTuning.asset to tune the feel.</summary>
    [CreateAssetMenu(menuName = "FedUp/Tasks/TV Tuning", fileName = "TvTuning")]
    public class TvTuningSO : ScriptableObject
    {
        [Tooltip("GDD Task 2 channel odds, in order Static, Ad, Horror, Calm (50/30/15/5).")]
        public float[] channelOdds = { 0.5f, 0.3f, 0.15f, 0.05f };
        [Tooltip("GDD: 10 clicks in 5 s ejects the batteries under the couch.")]
        [Min(2)] public int spamPresses = 10;
        [Min(0.5f)] public float spamWindowSeconds = 5f;
        [Tooltip("GDD: the ghost hand only tries above 50% frustration.")]
        public float stealAbove = 50f;
        [Tooltip("GDD says 'low chance' per press; 10% is our reading.")]
        [Range(0f, 1f)] public float stealChance = 0.1f;
        [Tooltip("Tug of war: presses needed, and seconds to do them in.")]
        [Min(1)] public int tugPresses = 12;
        [Min(0.5f)] public float tugSeconds = 3f;
        [Tooltip("GDD: success = stay on the calm channel 10 s.")]
        [Min(1f)] public float calmSecondsToWin = 10f;
        [Tooltip("GDD rebalance: the TV can add at most +10 in total.")]
        public float positiveCap = 10f;
        [Tooltip("GDD: hold E to sit.")]
        [Min(0f)] public float sitHoldSeconds = 1f;
        [Tooltip("Camera height while sitting, metres above the floor.")]
        public float sitEyeHeight = 1.15f;
        [Tooltip("Hold E this long to feel under the couch for the batteries.")]
        [Min(0f)] public float searchHoldSeconds = 1.2f;

        [Header("Frustration events (Appendix 1)")]
        public FrustrationEventSO staticOrHorror; // +7
        public FrustrationEventSO infomercial;    // +5
        public FrustrationEventSO lostRemote;     // +10 (batteries under the couch)
        public FrustrationEventSO remoteStolen;   // +5 (tug of war lost)
        public FrustrationEventSO calm;           // -10
    }
}
