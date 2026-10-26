namespace FedUp.Tasks.Tea
{
    /// <summary>
    /// The Storeroom blackout (STORY_BIBLE P1, DESIGN_LOGIC #79): taking the tea from under the axe, during the hunt,
    /// blacks you out once a night. You wake in the bathtub with everything you carried; the milk is beside you if it
    /// was still hidden there.
    /// </summary>
    public static class BlackoutRules
    {
        public static bool ShouldBlackOut(TeaStage stage, int itemTaken, int teaItem, bool alreadyHappened) =>
            !alreadyHappened && stage == TeaStage.Search && itemTaken == teaItem;

        public static bool MilkBesideYou(TeaItemState milk) => milk == TeaItemState.Hidden;
    }
}
