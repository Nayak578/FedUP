namespace FedUp.Tasks.Bath
{
    public enum BathDirection { Up, Down, Left, Right }

    public readonly struct BathPrompt
    {
        public readonly int Item;
        public readonly BathDirection Direction;
        public readonly float TimeLeft, Window;
        public BathPrompt(int item, BathDirection direction, float timeLeft, float window) { Item = item; Direction = direction; TimeLeft = timeLeft; Window = window; }
    }

    /// <summary>
    /// Take a Bath rules (GDD Phase 1 + designer notes). Holding the button fills the relax bar. On the clock,
    /// clothes start sliding off the hooks one by one; each shows a direction prompt you must press within the
    /// window to snatch it back, or it's lost. The bath ends once you're relaxed and nothing is sliding.
    /// </summary>
    public sealed class BathSession
    {
        readonly float relaxSeconds, window;
        readonly float[] slideTimes;
        readonly BathDirection[] directions;
        float clock, relaxed;
        int next;
        int activeItem = -1;
        float activeLeft;

        public BathSession(float relaxSeconds, float[] slideTimes, BathDirection[] directions, float windowSeconds)
        {
            this.relaxSeconds = relaxSeconds <= 0f ? 0.01f : relaxSeconds;
            this.slideTimes = slideTimes ?? new float[0];
            this.directions = directions ?? new BathDirection[0];
            window = windowSeconds;
        }

        public int Items => slideTimes.Length;
        public float Relax => relaxed / relaxSeconds > 1f ? 1f : relaxed / relaxSeconds;
        public BathPrompt? Prompt => activeItem < 0 ? (BathPrompt?)null
            : new BathPrompt(activeItem, DirectionFor(activeItem), activeLeft, window);
        public int Saved { get; private set; }
        public int Lost { get; private set; }
        public int LostThisTick { get; private set; }
        public bool Finished => Relax >= 1f && activeItem < 0 && (next >= slideTimes.Length || Saved + Lost >= slideTimes.Length);
        public bool Perfect => Finished && Lost == 0;

        public void Tick(float dt, bool relaxHeld)
        {
            LostThisTick = 0;
            if (Finished || dt <= 0f) return;
            clock += dt;
            if (relaxHeld) relaxed += dt;

            if (activeItem >= 0)
            {
                activeLeft -= dt;
                if (activeLeft <= 0f)
                {
                    Lost++;
                    LostThisTick++;
                    activeItem = -1;
                }
            }
            // once relaxed, any clothes that haven't started sliding are safe
            if (activeItem < 0 && next < slideTimes.Length && Relax < 1f && clock >= slideTimes[next])
            {
                activeItem = next++;
                activeLeft = window;
            }
            if (Relax >= 1f && activeItem < 0) next = slideTimes.Length;
        }

        /// <summary>Returns true if this press snatched the sliding item back.</summary>
        public bool Press(BathDirection d)
        {
            if (activeItem < 0 || d != DirectionFor(activeItem)) return false;
            Saved++;
            activeItem = -1;
            return true;
        }

        BathDirection DirectionFor(int item) => directions.Length == 0 ? BathDirection.Left : directions[item % directions.Length];
    }
}
