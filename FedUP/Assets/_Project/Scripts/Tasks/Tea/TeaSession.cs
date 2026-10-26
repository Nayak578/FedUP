namespace FedUp.Tasks.Tea
{
    public enum TeaStage { Gather, Search, PowerCut, Brew }
    public enum TeaItemState { InKitchen, Hidden, Carried, Placed }

    /// <summary>
    /// Make Tea, the ingredient hunt (GDD Phase 3 + rebalance). Gather the three things in the kitchen: they vanish
    /// and hide around the house. Carry at most two at a time to the tea station. Coming back into the kitchen with
    /// the last one cuts the power: restore it at the fuse box in time, or everything re-scatters (+10). The
    /// rebalance allows that only once a night. Once all three are placed, the kettle goes on.
    /// </summary>
    public sealed class TeaSession
    {
        readonly TeaItemState[] items;
        readonly int carryLimit, maxRescatters;
        readonly float powerSeconds;
        bool cutThisHunt; // the power cuts once per hunt; a rescatter starts a new hunt

        public TeaSession(int items, int carryLimit, float powerSeconds, int maxRescatters)
        {
            this.items = new TeaItemState[items < 1 ? 1 : items];
            this.carryLimit = carryLimit < 1 ? 1 : carryLimit;
            this.powerSeconds = powerSeconds;
            this.maxRescatters = maxRescatters;
        }

        public TeaStage Stage { get; private set; } = TeaStage.Gather;
        public int Carrying => Count(TeaItemState.Carried);
        public int Placed => Count(TeaItemState.Placed);
        public float PowerLeft { get; private set; }
        public int Rescatters { get; private set; }
        public bool RescatteredThisTick { get; private set; }

        public TeaItemState StateOf(int item) => items[item];

        int Count(TeaItemState s) { int n = 0; foreach (var i in items) if (i == s) n++; return n; }

        /// <summary>Pick up an item where it first stands in the kitchen.</summary>
        public void Gather(int item)
        {
            if (Stage != TeaStage.Gather || items[item] != TeaItemState.InKitchen) return;
            items[item] = TeaItemState.Carried;
            if (Carrying < items.Length) return;
            // the lights flicker: everything vanishes into the house
            for (int i = 0; i < items.Length; i++) items[i] = TeaItemState.Hidden;
            Stage = TeaStage.Search;
        }

        /// <summary>Pick up a hidden item. False if your hands are full.</summary>
        public bool PickUp(int item)
        {
            if (Stage == TeaStage.Gather || Stage == TeaStage.Brew || items[item] != TeaItemState.Hidden) return false;
            if (Carrying >= carryLimit) return false;
            items[item] = TeaItemState.Carried;
            return true;
        }

        /// <summary>You walked into the kitchen. Returns true if the power just cut out.</summary>
        public bool EnterKitchen()
        {
            if (Stage != TeaStage.Search || cutThisHunt || Carrying == 0 || Placed + Carrying < items.Length) return false;
            cutThisHunt = true;
            Stage = TeaStage.PowerCut;
            PowerLeft = powerSeconds;
            return true;
        }

        /// <summary>Put a carried item on the tea station. False in the dark.</summary>
        public bool Place(int item)
        {
            if (Stage != TeaStage.Search || items[item] != TeaItemState.Carried) return false;
            items[item] = TeaItemState.Placed;
            if (Placed == items.Length) Stage = TeaStage.Brew;
            return true;
        }

        public void RestorePower()
        {
            if (Stage == TeaStage.PowerCut) Stage = TeaStage.Search;
        }

        public void Tick(float dt)
        {
            RescatteredThisTick = false;
            if (Stage != TeaStage.PowerCut || dt <= 0f || PowerLeft <= 0f) return;
            PowerLeft -= dt;
            if (PowerLeft > 0f || Rescatters >= maxRescatters) return;
            // too slow: the lights come back and everything is gone again (once a night, per the rebalance)
            Rescatters++;
            RescatteredThisTick = true;
            for (int i = 0; i < items.Length; i++) items[i] = TeaItemState.Hidden;
            Stage = TeaStage.Search;
            cutThisHunt = false;
        }
    }
}
