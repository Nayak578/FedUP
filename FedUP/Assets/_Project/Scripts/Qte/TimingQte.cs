namespace FedUp.Qte
{
    public enum TimingJudgement { Early, Perfect, Late }

    /// <summary>
    /// A marker sweeps once from 0 (start) to 1 (end) over <c>sweepSeconds</c> and stops at the end.
    /// Pressing inside the golden zone is Perfect; before it Early; after it (or never) Late.
    /// </summary>
    public static class TimingQte
    {
        public static float MarkerPosition(float elapsed, float sweepSeconds)
        {
            if (sweepSeconds <= 0f) return 1f;
            float p = elapsed / sweepSeconds;
            return p < 0f ? 0f : p > 1f ? 1f : p;
        }

        public static TimingJudgement Judge(float position, float zoneStart, float zoneEnd)
        {
            const float eps = 1e-5f;
            if (position < zoneStart - eps) return TimingJudgement.Early;
            if (position > zoneEnd + eps) return TimingJudgement.Late;
            return TimingJudgement.Perfect;
        }
    }

    /// <summary>"Mash to unjam": reach the required presses before time runs out.</summary>
    public sealed class MashChallenge
    {
        readonly int required;
        readonly float seconds;
        int presses;
        float elapsed;

        public MashChallenge(int requiredPresses, float seconds)
        {
            required = requiredPresses < 1 ? 1 : requiredPresses;
            this.seconds = seconds;
        }

        public bool Succeeded => presses >= required;
        public bool Failed => !Succeeded && elapsed >= seconds;
        public bool Finished => Succeeded || Failed;
        public float Progress => presses >= required ? 1f : (float)presses / required;
        public float TimeLeft => seconds - elapsed < 0f ? 0f : seconds - elapsed;

        public void Press()
        {
            if (!Finished) presses++;
        }

        public void Tick(float deltaSeconds)
        {
            if (!Finished && deltaSeconds > 0f) elapsed += deltaSeconds;
        }
    }
}
