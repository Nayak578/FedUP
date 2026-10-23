using System.Collections.Generic;

namespace FedUp.Ghost
{
    public enum ScareSize { Small, Big }
    public enum ScareDecision { Allowed, AlreadyFired, WrongPhase, Grace, Quiet }

    /// <summary>
    /// Ghost director v1 (MACRO_PLAN "GhostEventSystem"): every scare asks before it fires. A scare fires once,
    /// only in its phase, never during the grace after a phase change, and never while the house is "quiet"
    /// after the last scare. The quiet is shorter the angrier you are, and longer after a big scare.
    /// </summary>
    public sealed class DirectorRules
    {
        readonly float grace, quietCalm, quietFedUp, bigMultiplier;
        readonly HashSet<string> fired = new HashSet<string>();
        float graceUntil;

        public DirectorRules(float graceSeconds, float quietCalmSeconds, float quietFedUpSeconds, float bigMultiplier)
        {
            grace = graceSeconds;
            quietCalm = quietCalmSeconds;
            quietFedUp = quietFedUpSeconds;
            this.bigMultiplier = bigMultiplier;
            Phase = 1;
        }

        public int Phase { get; private set; }
        public float QuietUntil { get; private set; }
        public int Fired => fired.Count;

        public void EnterPhase(int phase, float now)
        {
            Phase = phase;
            graceUntil = now + grace;
        }

        public bool HasFired(string id) => fired.Contains(id);

        /// <summary>How long the house stays quiet after a scare of this size at this frustration (0..200).</summary>
        public float QuietAfter(ScareSize size, float frustration)
        {
            float k = frustration <= 0f ? 0f : frustration >= 100f ? 1f : frustration / 100f;
            float quiet = quietCalm + (quietFedUp - quietCalm) * k;
            return size == ScareSize.Big ? quiet * bigMultiplier : quiet;
        }

        /// <summary>Asks to fire a scare now. If allowed, the scare is used up and the quiet starts.</summary>
        public ScareDecision Request(string id, int minPhase, ScareSize size, float now, float frustration)
        {
            if (fired.Contains(id)) return ScareDecision.AlreadyFired;
            if (Phase < minPhase) return ScareDecision.WrongPhase;
            if (now < graceUntil) return ScareDecision.Grace;
            if (now < QuietUntil) return ScareDecision.Quiet;
            fired.Add(id);
            QuietUntil = now + QuietAfter(size, frustration);
            return ScareDecision.Allowed;
        }
    }
}
