namespace FedUp.Interaction
{
    /// <summary>
    /// Pure timing logic for "press" and "hold" interactions. No Unity objects, so it is unit-tested in Edit Mode.
    /// A press is a hold of zero seconds. Letting go resets progress.
    /// </summary>
    public readonly struct HoldProgress
    {
        public float Required { get; }
        public float Elapsed { get; }
        public bool Completed { get; }
        /// <summary>True only on the tick where the hold finished, so the interaction fires once.</summary>
        public bool JustCompleted { get; }

        public HoldProgress(float requiredSeconds) : this(requiredSeconds, 0f, false, false) { }

        HoldProgress(float required, float elapsed, bool completed, bool justCompleted)
        {
            Required = required < 0f ? 0f : required;
            Elapsed = elapsed;
            Completed = completed;
            JustCompleted = justCompleted;
        }

        public float Normalized
        {
            get
            {
                if (Completed) return 1f;
                if (Required <= 0f) return 0f;
                float n = Elapsed / Required;
                return n < 0f ? 0f : n > 1f ? 1f : n;
            }
        }

        public HoldProgress Tick(bool held, float deltaSeconds)
        {
            if (!held) return new HoldProgress(Required);
            if (Completed) return new HoldProgress(Required, Elapsed, true, false);
            float elapsed = Elapsed + (deltaSeconds > 0f ? deltaSeconds : 0f);
            if (elapsed >= Required) return new HoldProgress(Required, Required, true, true);
            return new HoldProgress(Required, elapsed, false, false);
        }
    }
}
