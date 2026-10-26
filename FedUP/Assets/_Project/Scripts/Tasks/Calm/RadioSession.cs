namespace FedUp.Tasks.Calm
{
    /// <summary>
    /// The radio (GDD rebalance, calm extra): let the lo-fi song play 15 s uninterrupted, within earshot, and it
    /// calms you (-5, once a night). Walking away or an interruption starts the count again.
    /// </summary>
    public sealed class RadioSession
    {
        readonly float needed;

        public RadioSession(float secondsNeeded) => needed = secondsNeeded;

        public float Heard { get; private set; }
        public bool Calmed { get; private set; }
        public bool CalmedThisTick { get; private set; }

        public void Tick(float dt, bool playing, bool inEarshot)
        {
            CalmedThisTick = false;
            if (!playing || !inEarshot) { Heard = 0f; return; }
            if (Calmed || dt <= 0f) return;
            Heard += dt;
            if (Heard >= needed) { Calmed = true; CalmedThisTick = true; }
        }

        public void Interrupt() => Heard = 0f;
    }
}
