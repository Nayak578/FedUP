using FedUp.Frustration;
using UnityEngine;

namespace FedUp.Ghost
{
    /// <summary>
    /// The one place every scare asks for permission (see <see cref="DirectorRules"/>). Scares never talk to each
    /// other; they ask here, so they can never pile up.
    /// </summary>
    public class GhostDirector : MonoBehaviour
    {
        [SerializeField] GhostDirectorTuningSO tuning;
        [SerializeField] FrustrationChannelSO frustrationState;

        public DirectorRules Rules { get; private set; }
        public string LastAllowed { get; private set; }
        public float LastAllowedAt { get; private set; } = -1f;

        void Awake()
        {
            Rules = new DirectorRules(tuning.graceSeconds, tuning.quietCalmSeconds, tuning.quietFedUpSeconds, tuning.bigMultiplier);
        }

        public void EnterPhase(int phase) => Rules.EnterPhase(phase, Time.time);

        /// <summary>Asks to fire a scare now. True means: do it (it's used up and the house goes quiet after).</summary>
        public bool TryScare(string id, int minPhase, ScareSize size)
        {
            float f = frustrationState != null ? frustrationState.Last.Hidden : 0f;
            var d = Rules.Request(id, minPhase, size, Time.time, f);
            if (d != ScareDecision.Allowed) return false;
            LastAllowed = id;
            LastAllowedAt = Time.time;
            Debug.Log($"DIRECTOR allowed {id} at {Time.time:F1}s; quiet until {Rules.QuietUntil:F1}s");
            return true;
        }
    }
}
