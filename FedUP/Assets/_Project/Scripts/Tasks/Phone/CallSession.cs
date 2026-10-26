namespace FedUp.Tasks.Phone
{
    /// <summary>
    /// Boss calls #2-4 (GDD "Phone Rings #2-4" + Appendix 1). The phone rings every couple of seconds; each ring after
    /// the first costs +2 (the same rate as the table's "+1 per second, long ring ignored"). Answer within the quick
    /// window for -5; the distorted call costs +5 when you hear it. Unanswered, it rings out.
    /// </summary>
    public sealed class CallSession
    {
        readonly float ringEvery, ringOut, quickWithin, perRing, quickRelief, distortedCost;
        readonly bool distorted;
        float nextRing;

        public CallSession(float ringEverySeconds, float ringOutSeconds, float quickWithinSeconds, float perRingCost,
            float quickRelief, float distortedCost, bool distorted)
        {
            ringEvery = ringEverySeconds > 0.1f ? ringEverySeconds : 0.1f;
            ringOut = ringOutSeconds;
            quickWithin = quickWithinSeconds;
            perRing = perRingCost;
            this.quickRelief = quickRelief;
            this.distortedCost = distortedCost;
            this.distorted = distorted;
        }

        public int Rings { get; private set; }
        public bool Ringing { get; private set; }
        public bool Answered { get; private set; }
        public bool RangOut { get; private set; }
        public float Elapsed { get; private set; }
        /// <summary>True on the tick a new ring started (for the sound).</summary>
        public bool RangThisTick { get; private set; }

        /// <summary>The first ring. Free.</summary>
        public void Start()
        {
            if (Ringing || Answered || RangOut) return;
            Ringing = true;
            Rings = 1;
            Elapsed = 0f;
            nextRing = ringEvery;
        }

        /// <summary>Returns the frustration the rings added during this tick.</summary>
        public float Tick(float dt)
        {
            RangThisTick = false;
            if (!Ringing || dt <= 0f) return 0f;
            float cost = 0f;
            Elapsed += dt;
            while (Ringing && Elapsed >= nextRing - 1e-4f)
            {
                if (nextRing >= ringOut - 1e-4f) { Ringing = false; RangOut = true; break; }
                Rings++;
                cost += perRing;
                RangThisTick = true;
                nextRing += ringEvery;
            }
            if (Ringing && Elapsed >= ringOut) { Ringing = false; RangOut = true; }
            return cost;
        }

        /// <summary>Returns the frustration change of answering now (quick relief, plus the distorted line).</summary>
        public float Answer()
        {
            if (!Ringing) return 0f;
            Ringing = false;
            Answered = true;
            return (Elapsed <= quickWithin ? quickRelief : 0f) + (distorted ? distortedCost : 0f);
        }
    }
}
