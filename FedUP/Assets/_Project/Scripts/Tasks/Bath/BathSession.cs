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
    /// Take a Bath rules (GDD Phase 1 + designer notes: "keep an eye on all clothes"). Holding the button fills the
    /// relax bar. For as long as you're relaxing, the ghost keeps snatching at whatever still hangs on the hooks:
    /// each snatch shows a direction prompt; press it in time and the piece goes back on its hook (it can be
    /// snatched again), miss it and that piece is gone. The bath ends once you're relaxed and nothing is sliding.
    /// </summary>
    public sealed class BathSession
    {
        readonly float relaxSeconds, window, snatchEvery;
        readonly BathDirection[] directions;
        readonly bool[] gone;
        float clock, relaxed, nextSnatchAt, activeLeft;
        int activeItem = -1, cursor;
        BathDirection activeDirection;

        public BathSession(float relaxSeconds, int items, float firstSnatchAt, float snatchEvery, BathDirection[] directions, float windowSeconds)
        {
            this.relaxSeconds = relaxSeconds <= 0f ? 0.01f : relaxSeconds;
            Items = items < 0 ? 0 : items;
            OnHooks = Items;
            gone = new bool[Items];
            nextSnatchAt = firstSnatchAt;
            this.snatchEvery = snatchEvery <= 0.1f ? 0.1f : snatchEvery;
            this.directions = directions == null || directions.Length == 0 ? new[] { BathDirection.Left } : directions;
            window = windowSeconds;
        }

        public int Items { get; }
        public int OnHooks { get; private set; }
        public int Snatches { get; private set; }
        public float Relax => relaxed / relaxSeconds > 1f ? 1f : relaxed / relaxSeconds;
        public BathPrompt? Prompt => activeItem < 0 ? (BathPrompt?)null : new BathPrompt(activeItem, activeDirection, activeLeft, window);
        public int Saved { get; private set; }
        public int Lost { get; private set; }
        public int LostThisTick { get; private set; }
        public bool Finished => Relax >= 1f && activeItem < 0;
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
                    gone[activeItem] = true;
                    OnHooks--;
                    Lost++;
                    LostThisTick++;
                    activeItem = -1;
                }
            }

            // the ghost keeps snatching while you're relaxing, at whatever still hangs there, round the hooks
            if (activeItem < 0 && Relax < 1f && OnHooks > 0 && clock >= nextSnatchAt)
            {
                while (gone[cursor % Items]) cursor++;
                activeItem = cursor % Items;
                cursor++;
                activeDirection = directions[Snatches % directions.Length];
                activeLeft = window;
                Snatches++;
                nextSnatchAt += snatchEvery;
                if (nextSnatchAt < clock) nextSnatchAt = clock + snatchEvery; // a snatch that waited doesn't double up
            }
        }

        /// <summary>Returns true if this press snatched the sliding item back.</summary>
        public bool Press(BathDirection d)
        {
            if (activeItem < 0 || d != activeDirection) return false;
            Saved++;
            activeItem = -1;
            return true;
        }
    }
}
