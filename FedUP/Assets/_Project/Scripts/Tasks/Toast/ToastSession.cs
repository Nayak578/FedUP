using FedUp.Qte;

namespace FedUp.Tasks.Toast
{
    public enum ToastOutcome { Golden, Burnt, Launched }

    /// <summary>
    /// Make Toast rules (GDD Task 1): pressing in the golden zone makes golden toast; too early and the crooked
    /// toaster launches it; too late (or never) it burns. Two failures in a row set off the smoke alarm.
    /// </summary>
    public sealed class ToastSession
    {
        public int AlarmAfterFailures { get; }
        public int ConsecutiveFailures { get; private set; }
        public bool AlarmShouldRing => ConsecutiveFailures >= AlarmAfterFailures;

        public ToastSession(int alarmAfterFailures = 2) => AlarmAfterFailures = alarmAfterFailures;

        public ToastOutcome Resolve(TimingJudgement judgement)
        {
            var outcome = judgement == TimingJudgement.Perfect ? ToastOutcome.Golden
                        : judgement == TimingJudgement.Early ? ToastOutcome.Launched
                        : ToastOutcome.Burnt;
            ConsecutiveFailures = outcome == ToastOutcome.Golden ? 0 : ConsecutiveFailures + 1;
            return outcome;
        }

        public void AlarmSilenced() => ConsecutiveFailures = 0;

        /// <summary>GDD: "If Frustration &gt; 30%, trigger jam event".</summary>
        public static bool ShouldJam(float frustration, float jamThreshold) => frustration > jamThreshold;
    }
}
