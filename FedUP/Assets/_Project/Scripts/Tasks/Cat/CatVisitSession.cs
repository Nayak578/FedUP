namespace FedUp.Tasks.Cat
{
    public enum PetResult { Purred, Hissed, TooSoon, Gone }

    /// <summary>
    /// One cat visit (GDD Phase 1 Cat Visit #1 + Task 8). Hold to pet: it purrs (-20). Above 60% it may hiss and
    /// move instead (50%); you can try again after 30 s, and a pet after a hiss calms less (-10). Ignore it long
    /// enough and it leaves (no change now; the ending remembers).
    /// </summary>
    public sealed class CatVisitSession
    {
        readonly float hissAbove, hissChance, cooldown, patience, petAmount, retryPetAmount;
        float waited;

        public CatVisitSession(float hissAbove, float hissChance, float retryCooldownSeconds, float patienceSeconds,
            float petAmount, float retryPetAmount)
        {
            this.hissAbove = hissAbove;
            this.hissChance = hissChance;
            cooldown = retryCooldownSeconds;
            patience = patienceSeconds;
            this.petAmount = petAmount;
            this.retryPetAmount = retryPetAmount;
        }

        public bool Petted { get; private set; }
        public bool Left { get; private set; }
        public bool LeftThisTick { get; private set; }
        public bool HissedOnce { get; private set; }
        public bool Done => Petted || Left;
        public float CooldownLeft { get; private set; }
        /// <summary>The frustration change of the successful pet (-20, or -10 after a hiss).</summary>
        public float PetAmount { get; private set; }

        /// <param name="roll">0..1 from the caller; tests pass fixed values.</param>
        public PetResult Pet(float frustration, float roll)
        {
            if (Done) return PetResult.Gone;
            if (CooldownLeft > 0f) return PetResult.TooSoon;
            if (frustration > hissAbove && roll < hissChance)
            {
                HissedOnce = true;
                CooldownLeft = cooldown;
                waited = 0f; // its patience starts again once it lets you near
                return PetResult.Hissed;
            }
            Petted = true;
            PetAmount = HissedOnce ? retryPetAmount : petAmount;
            return PetResult.Purred;
        }

        public void Tick(float dt)
        {
            LeftThisTick = false;
            if (Done || dt <= 0f) return;
            if (CooldownLeft > 0f)
            {
                CooldownLeft -= dt;
                if (CooldownLeft > 0f) return;
                dt = -CooldownLeft; // the rest of this tick counts as waiting
                CooldownLeft = 0f;
            }
            waited += dt;
            if (waited >= patience) { Left = true; LeftThisTick = true; }
        }
    }
}
