namespace FedUp.Story
{
    /// <summary>
    /// The Burial chase (STORY_BIBLE Burial step 2, DESIGN_LOGIC #84): the ghost flees along a line of stops and never
    /// lets you closer than a few metres. Come within that and it moves on; past the last stop it's gone into the wall.
    /// </summary>
    public sealed class BurialChase
    {
        readonly int stops;
        readonly float keepAway;

        public BurialChase(int stops, float keepAway)
        {
            this.stops = stops < 1 ? 1 : stops;
            this.keepAway = keepAway;
        }

        public int Stop { get; private set; }
        public bool Gone { get; private set; }
        /// <summary>True on the update it moved on (or vanished).</summary>
        public bool MovedThisTick { get; private set; }

        /// <param name="distance">How far you are from where it stands now.</param>
        public void Tick(float distance)
        {
            MovedThisTick = false;
            if (Gone || distance >= keepAway) return;
            Stop++;
            MovedThisTick = true;
            if (Stop >= stops) Gone = true;
        }
    }
}
