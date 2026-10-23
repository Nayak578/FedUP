using System.Collections.Generic;

namespace FedUp.Tasks.Tv
{
    public enum TvChannel { Off, Static, Ad, Horror, Calm }
    public enum TvPressResult { Changed, BatteriesOut, Stolen, Ignored }

    /// <summary>
    /// Watch TV rules (GDD Phase 1 + Task 2). Each remote press lands on a random channel (odds from the GDD);
    /// ten presses inside five seconds eject the batteries under the couch; above 50% the ghost may grab the
    /// remote instead. Staying on the calm channel for ten seconds while sitting wins. Everything the TV adds to
    /// frustration is capped (GDD rebalance: +10 at most); calming is never capped.
    /// </summary>
    public sealed class TvSession
    {
        readonly float[] odds;
        readonly int spamPresses;
        readonly float spamWindow, stealAbove, stealChance, calmToWin, cap;
        readonly Queue<float> recent = new Queue<float>();

        public TvSession(float[] odds, int spamPresses, float spamWindowSeconds, float stealAbove, float stealChance,
            float calmSecondsToWin, float positiveCap)
        {
            this.odds = odds;
            this.spamPresses = spamPresses < 1 ? 1 : spamPresses;
            spamWindow = spamWindowSeconds;
            this.stealAbove = stealAbove;
            this.stealChance = stealChance;
            calmToWin = calmSecondsToWin;
            cap = positiveCap;
            HasRemote = true;
        }

        public TvChannel Channel { get; private set; }
        public bool HasRemote { get; private set; }
        public bool Won { get; private set; }
        public bool WonThisTick { get; private set; }
        public float CalmHeld { get; private set; }
        public float PositiveSpent { get; private set; }

        /// <param name="odds">Static, Ad, Horror, Calm; they should add up to 1.</param>
        public static TvChannel ChannelFor(float roll, float[] odds)
        {
            float edge = 0f;
            for (int i = 0; i < 4 && i < odds.Length; i++)
            {
                edge += odds[i];
                if (roll < edge - 1e-5f) return (TvChannel)(i + 1); // tolerance: 0.5f + 0.3f + 0.15f lands a hair above 0.95f
            }
            return TvChannel.Calm;
        }

        /// <param name="now">Seconds, any clock that only goes forward.</param>
        /// <param name="channelRoll">0..1, picks the channel.</param>
        /// <param name="stealRoll">0..1, decides a ghost grab when frustration is high.</param>
        public TvPressResult Press(float now, float channelRoll, float frustration, float stealRoll)
        {
            if (!HasRemote || Won) return TvPressResult.Ignored;

            recent.Enqueue(now);
            while (recent.Count > 0 && recent.Peek() < now - spamWindow) recent.Dequeue();
            if (recent.Count >= spamPresses)
            {
                recent.Clear();
                HasRemote = false;
                return TvPressResult.BatteriesOut;
            }

            if (frustration > stealAbove && stealRoll < stealChance) return TvPressResult.Stolen;

            Channel = ChannelFor(channelRoll, odds);
            CalmHeld = 0f;
            return TvPressResult.Changed;
        }

        /// <param name="watching">True while he is sitting on the couch.</param>
        public void Tick(float dt, bool watching)
        {
            WonThisTick = false;
            if (Won || !watching || Channel != TvChannel.Calm || dt <= 0f) return;
            CalmHeld += dt;
            if (CalmHeld >= calmToWin) { Won = true; WonThisTick = true; }
        }

        /// <summary>Returns how much of <paramref name="amount"/> may actually be applied under the TV cap.</summary>
        public float Charge(float amount)
        {
            if (amount <= 0f) return amount;
            float left = cap - PositiveSpent;
            float applied = amount < left ? amount : (left > 0f ? left : 0f);
            PositiveSpent += applied;
            return applied;
        }

        public void RemoteLost() => HasRemote = false;

        public void RemoteRecovered()
        {
            HasRemote = true;
            recent.Clear();
        }
    }
}
