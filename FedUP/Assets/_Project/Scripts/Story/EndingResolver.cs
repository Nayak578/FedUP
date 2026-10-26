namespace FedUp.Story
{
    public enum Ending
    {
        DieInSleep,
        Loop,
        UnemployedGhostAngry,
        ShowdownAnswered,
        ShowdownIgnored,
        BurialAnswered,
        BurialIgnored,
        SecretLoop,
        SecretGhostWins,
    }

    public enum FinalChoice { StayAsleep, WakeUp }

    /// <summary>
    /// The ending table (MACRO_PLAN.md Appendix 2, confirmed by the designer 26 Oct). Only the cat, the phone and how
    /// you woke decide it; job and home are results. The cat rule beats everything: never petted, you die in your sleep.
    /// </summary>
    public static class EndingResolver
    {
        /// <summary>Designer ruling, 26 Oct: "answered the phone" means at least 3 of the 4 calls.</summary>
        public const int CallsNeeded = 3;
        /// <summary>Fed Up is 100 (CLAUDE.md frustration bands).</summary>
        public const float FedUp = 100f;

        public static bool PhoneAnswered(int callsAnswered) => callsAnswered >= CallsNeeded;

        public static Ending Resolve(bool catPetted, int callsAnswered, FinalChoice choice, float frustrationAtWake)
        {
            if (!catPetted) return Ending.DieInSleep;
            bool answered = PhoneAnswered(callsAnswered);
            if (choice == FinalChoice.StayAsleep) return answered ? Ending.Loop : Ending.UnemployedGhostAngry;
            if (frustrationAtWake >= FedUp) return answered ? Ending.ShowdownAnswered : Ending.ShowdownIgnored;
            return answered ? Ending.BurialAnswered : Ending.BurialIgnored;
        }

        /// <summary>Bed before any chore: the cat rule doesn't apply; the result is random (GDD: loop or ghost wins).</summary>
        public static Ending ResolveSecret(float roll) => roll < 0.5f ? Ending.SecretLoop : Ending.SecretGhostWins;
    }
}
