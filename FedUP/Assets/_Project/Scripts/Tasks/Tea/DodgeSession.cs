namespace FedUp.Tasks.Tea
{
    /// <summary>
    /// The tea dodge (GDD Make Tea): survive 20 s of junk thrown from the cabinets. The clock only runs while you're
    /// in the kitchen (running away pauses it). Three hits and the kitchen catches fire.
    /// </summary>
    public sealed class DodgeSession
    {
        readonly float duration;
        readonly int hitsForFire;

        public DodgeSession(float durationSeconds, int hitsForFire)
        {
            duration = durationSeconds;
            this.hitsForFire = hitsForFire < 1 ? 1 : hitsForFire;
        }

        public float Elapsed { get; private set; }
        public int Hits { get; private set; }
        public bool Fire { get; private set; }
        public bool Survived { get; private set; }
        public bool Done => Fire || Survived;

        public void Tick(float dt, bool inKitchen)
        {
            if (Done || !inKitchen || dt <= 0f) return;
            Elapsed += dt;
            if (Elapsed >= duration) Survived = true;
        }

        /// <summary>Returns true if this hit started the fire.</summary>
        public bool Hit()
        {
            if (Done) return false;
            Hits++;
            if (Hits < hitsForFire) return false;
            Fire = true;
            return true;
        }
    }
}
