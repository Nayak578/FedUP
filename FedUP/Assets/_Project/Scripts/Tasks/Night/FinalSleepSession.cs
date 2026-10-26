using FedUp.Story;

namespace FedUp.Tasks.Night
{
    public enum FinalSleepStage { Awake, Breathing, Choice, Chosen }

    /// <summary>
    /// The final sleep (GDD "Final Sleep Task" + Task 7 + the rebalance). Lie down. Above 50% the ghost disrupts you
    /// and you must breathe to the heartbeat: three beats in a row. A miss is a scream (+15) and you start again;
    /// only the first retry costs +10. After three misses the nightmare just happens. A clean sleep is -20. Then the
    /// blanket lifts: press to wake up, or do nothing and stay asleep.
    /// </summary>
    public sealed class FinalSleepSession
    {
        readonly float qteAbove, failCost, retryCost, cleanSleep, choiceSeconds;
        readonly int beats, maxFails;
        bool retryCharged;

        public FinalSleepSession(float qteAbove, int beats, int maxFails, float failCost, float retryCost, float cleanSleep, float choiceSeconds)
        {
            this.qteAbove = qteAbove;
            this.beats = beats < 1 ? 1 : beats;
            this.maxFails = maxFails < 1 ? 1 : maxFails;
            this.failCost = failCost;
            this.retryCost = retryCost;
            this.cleanSleep = cleanSleep;
            this.choiceSeconds = choiceSeconds;
        }

        public FinalSleepStage Stage { get; private set; } = FinalSleepStage.Awake;
        public int BeatsHit { get; private set; }
        public int Fails { get; private set; }
        public bool Nightmare { get; private set; }
        public FinalChoice Chosen { get; private set; }
        public float ChoiceLeft { get; private set; }

        /// <summary>Returns the frustration change (a clean sleep at once when you're calm enough).</summary>
        public float LieDown(float frustration)
        {
            if (Stage != FinalSleepStage.Awake) return 0f;
            if (frustration > qteAbove) { Stage = FinalSleepStage.Breathing; return 0f; }
            ToChoice();
            return cleanSleep;
        }

        /// <summary>One breath on the heartbeat. Returns the frustration change.</summary>
        public float Beat(bool onTheBeat)
        {
            if (Stage != FinalSleepStage.Breathing) return 0f;
            if (onTheBeat)
            {
                BeatsHit++;
                if (BeatsHit < beats) return 0f;
                ToChoice();
                return cleanSleep;
            }
            // the scream; you start again from the first breath
            Fails++;
            BeatsHit = 0;
            float cost = failCost;
            if (Fails >= maxFails)
            {
                Nightmare = true;
                ToChoice();
                return cost;
            }
            if (!retryCharged) { retryCharged = true; cost += retryCost; } // rebalance: one +10, not stacking
            return cost;
        }

        /// <summary>Counts down the choice; doing nothing means staying asleep.</summary>
        public void Tick(float dt)
        {
            if (Stage != FinalSleepStage.Choice || dt <= 0f) return;
            ChoiceLeft -= dt;
            if (ChoiceLeft > 0f) return;
            ChoiceLeft = 0f;
            Chosen = FinalChoice.StayAsleep;
            Stage = FinalSleepStage.Chosen;
        }

        public void WakeUp()
        {
            if (Stage != FinalSleepStage.Choice) return;
            Chosen = FinalChoice.WakeUp;
            Stage = FinalSleepStage.Chosen;
        }

        void ToChoice()
        {
            Stage = FinalSleepStage.Choice;
            ChoiceLeft = choiceSeconds;
        }
    }
}
