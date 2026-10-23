namespace FedUp.Tasks.Cleanup
{
    public enum DropResult { Binned, Rejected, ComedyFlyback }

    /// <summary>
    /// Clean Up Mess rules (GDD Phase 1 + Task 5, applying the rebalance "minor glitches over repeated flybacks"):
    /// below the interference threshold drops always work; above it the ghost may reject a drop. The first time
    /// you bin three in a row, the third flies back out once (the joke). After that, three in a row sends the
    /// ghost on a break and starts the clean-room timer, which pays out once.
    /// </summary>
    public sealed class CleanupSession
    {
        readonly int total, streakForBreak;
        readonly float interferenceAbove, rejectChance, breakSeconds, cleanSeconds;
        bool comedyPending;
        float breakLeft, cleanTimer = -1f;

        public CleanupSession(int totalItems, float interferenceAbove, float rejectChance, int streakForBreak,
            float ghostBreakSeconds, float cleanRoomSeconds, bool comedyFlybackOnce)
        {
            total = totalItems;
            this.interferenceAbove = interferenceAbove;
            this.rejectChance = rejectChance;
            this.streakForBreak = streakForBreak < 1 ? 1 : streakForBreak;
            breakSeconds = ghostBreakSeconds;
            cleanSeconds = cleanRoomSeconds;
            comedyPending = comedyFlybackOnce;
        }

        public int Total => total;
        public int Binned { get; private set; }
        public int Streak { get; private set; }
        public bool Done => Binned >= total;
        public bool GhostOnBreak => breakLeft > 0f;
        public bool CleanBonusThisTick { get; private set; }
        public bool CleanBonusGiven { get; private set; }

        /// <param name="roll">A random number 0..1 from the caller (tests pass fixed values).</param>
        public DropResult Drop(float frustration, float roll)
        {
            if (!GhostOnBreak && frustration > interferenceAbove && roll < rejectChance)
            {
                Streak = 0;
                return DropResult.Rejected;
            }

            Streak++;
            if (Streak >= streakForBreak && comedyPending)
            {
                comedyPending = false;
                Streak = 0;
                return DropResult.ComedyFlyback; // the item is back on the floor
            }

            Binned++;
            if (Streak >= streakForBreak)
            {
                Streak = 0;
                breakLeft = breakSeconds;
                if (!CleanBonusGiven && cleanTimer < 0f) cleanTimer = 0f;
            }
            return DropResult.Binned;
        }

        public void Tick(float dt)
        {
            CleanBonusThisTick = false;
            if (dt <= 0f) return;
            if (breakLeft > 0f) breakLeft -= dt;
            if (cleanTimer >= 0f && !CleanBonusGiven)
            {
                cleanTimer += dt;
                if (cleanTimer >= cleanSeconds)
                {
                    CleanBonusGiven = true;
                    CleanBonusThisTick = true;
                    cleanTimer = -1f;
                }
            }
        }
    }
}
