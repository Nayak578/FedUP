using FedUp.Story;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>The ending table, row by row (MACRO_PLAN.md Appendix 2, confirmed by the designer 26 Oct).</summary>
    public class EndingRulesTests
    {
        const int Answered = 3, Ignored = 2;

        // ---- the cat rule beats everything ----

        [TestCase(Answered, FinalChoice.StayAsleep, 20f)]
        [TestCase(Answered, FinalChoice.WakeUp, 100f)]
        [TestCase(Ignored, FinalChoice.WakeUp, 40f)]
        [TestCase(0, FinalChoice.StayAsleep, 0f)]
        public void CatNeverPetted_YouDieInYourSleep_WhateverElse(int calls, FinalChoice choice, float frustration)
        {
            Assert.AreEqual(Ending.DieInSleep, EndingResolver.Resolve(false, calls, choice, frustration));
        }

        // ---- the six main rows ----

        [Test] public void Answered_StayAsleep_TheLoop() => Assert.AreEqual(Ending.Loop, EndingResolver.Resolve(true, Answered, FinalChoice.StayAsleep, 40f));
        [Test] public void Answered_WakeAt100_Showdown() => Assert.AreEqual(Ending.ShowdownAnswered, EndingResolver.Resolve(true, Answered, FinalChoice.WakeUp, 100f));
        [Test] public void Answered_WakeBelow100_Burial() => Assert.AreEqual(Ending.BurialAnswered, EndingResolver.Resolve(true, Answered, FinalChoice.WakeUp, 99.9f));
        [Test] public void Ignored_StayAsleep_UnemployedGhostAngry() => Assert.AreEqual(Ending.UnemployedGhostAngry, EndingResolver.Resolve(true, Ignored, FinalChoice.StayAsleep, 40f));
        [Test] public void Ignored_WakeAt100_Showdown() => Assert.AreEqual(Ending.ShowdownIgnored, EndingResolver.Resolve(true, Ignored, FinalChoice.WakeUp, 100f));
        [Test] public void Ignored_WakeBelow100_Burial() => Assert.AreEqual(Ending.BurialIgnored, EndingResolver.Resolve(true, Ignored, FinalChoice.WakeUp, 50f));

        [Test]
        public void TheHiddenValueAbove100_StillCountsAsFedUp()
        {
            Assert.AreEqual(Ending.ShowdownIgnored, EndingResolver.Resolve(true, Ignored, FinalChoice.WakeUp, 170f));
        }

        [Test]
        public void StayingAsleep_FrustrationDoesntMatter()
        {
            Assert.AreEqual(Ending.Loop, EndingResolver.Resolve(true, Answered, FinalChoice.StayAsleep, 100f));
            Assert.AreEqual(Ending.UnemployedGhostAngry, EndingResolver.Resolve(true, Ignored, FinalChoice.StayAsleep, 150f));
        }

        // ---- "answered the phone" = at least 3 of 4 (designer ruling, 26 Oct) ----

        [TestCase(0, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        public void PhoneAnswered_MeansAtLeastThreeOfFour(int calls, bool answered)
        {
            Assert.AreEqual(answered, EndingResolver.PhoneAnswered(calls));
        }

        // ---- the secret ending: random, and the cat rule doesn't apply ----

        [Test]
        public void Secret_LowRoll_TheLoop() => Assert.AreEqual(Ending.SecretLoop, EndingResolver.ResolveSecret(0.2f));

        [Test]
        public void Secret_HighRoll_TheGhostWins() => Assert.AreEqual(Ending.SecretGhostWins, EndingResolver.ResolveSecret(0.8f));
    }
}
