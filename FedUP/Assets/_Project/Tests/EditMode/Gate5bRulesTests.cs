using FedUp.Story;
using FedUp.Tasks.Tea;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Gate 5b rules: the Storeroom blackout (STORY_BIBLE P1) and the Burial chase (STORY_BIBLE Burial step 2).</summary>
    public class Gate5bRulesTests
    {
        const int Milk = 0, Sugar = 1, Tea = 2;

        [Test]
        public void TakingTheTea_DuringTheHunt_BlacksYouOut()
        {
            Assert.IsTrue(BlackoutRules.ShouldBlackOut(TeaStage.Search, Tea, Tea, false));
        }

        [Test]
        public void OnlyTheTea_NotTheMilkOrSugar()
        {
            Assert.IsFalse(BlackoutRules.ShouldBlackOut(TeaStage.Search, Milk, Tea, false));
            Assert.IsFalse(BlackoutRules.ShouldBlackOut(TeaStage.Search, Sugar, Tea, false));
        }

        [Test]
        public void OnceANight()
        {
            Assert.IsFalse(BlackoutRules.ShouldBlackOut(TeaStage.Search, Tea, Tea, true));
        }

        [Test]
        public void NotWhileGathering_OrInThePowerCut()
        {
            Assert.IsFalse(BlackoutRules.ShouldBlackOut(TeaStage.Gather, Tea, Tea, false), "the first gather is in the kitchen");
            Assert.IsFalse(BlackoutRules.ShouldBlackOut(TeaStage.PowerCut, Tea, Tea, false), "one dark thing at a time");
        }

        [Test]
        public void TheMilkIsBesideYou_OnlyIfItWasStillHidden()
        {
            Assert.IsTrue(BlackoutRules.MilkBesideYou(TeaItemState.Hidden));
            Assert.IsFalse(BlackoutRules.MilkBesideYou(TeaItemState.Carried));
            Assert.IsFalse(BlackoutRules.MilkBesideYou(TeaItemState.Placed));
        }

        // ---- the Burial chase ----

        [Test]
        public void FarAway_ItWaits()
        {
            var c = new BurialChase(stops: 3, keepAway: 3f);
            c.Tick(8f);
            Assert.AreEqual(0, c.Stop);
            Assert.IsFalse(c.MovedThisTick);
        }

        [Test]
        public void TooClose_ItMovesOn()
        {
            var c = new BurialChase(3, 3f);
            c.Tick(2.5f);
            Assert.AreEqual(1, c.Stop);
            Assert.IsTrue(c.MovedThisTick);
            c.Tick(9f);
            Assert.IsFalse(c.MovedThisTick);
        }

        [Test]
        public void PastTheLastStop_ItsGoneIntoTheWall()
        {
            var c = new BurialChase(2, 3f);
            c.Tick(1f);
            Assert.IsFalse(c.Gone);
            c.Tick(1f);
            Assert.IsTrue(c.Gone);
            Assert.IsTrue(c.MovedThisTick);
            c.Tick(0.5f);
            Assert.IsFalse(c.MovedThisTick, "gone is gone");
            Assert.AreEqual(2, c.Stop);
        }
    }
}
