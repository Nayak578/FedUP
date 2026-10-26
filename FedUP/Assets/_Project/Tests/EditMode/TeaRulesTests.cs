using FedUp.Tasks.Tea;
using NUnit.Framework;

namespace FedUp.Tests
{
    /// <summary>Make Tea rules from the GDD (Phase 3) and its rebalance: the hunt, the power cut, the dodge.</summary>
    public class TeaRulesTests
    {
        static TeaSession NewTea() => new TeaSession(items: 3, carryLimit: 2, powerSeconds: 30f, maxRescatters: 1);

        static TeaSession Vanished()
        {
            var t = NewTea();
            for (int i = 0; i < 3; i++) t.Gather(i);
            return t;
        }

        static TeaSession PowerCut()
        {
            var t = Vanished();
            t.PickUp(0); t.PickUp(1);
            t.EnterKitchen();
            t.Place(0); t.Place(1);
            t.PickUp(2);
            Assert.IsTrue(t.EnterKitchen());
            return t;
        }

        // ---- the hunt ----

        [Test]
        public void GatheringAllThree_TheyVanishIntoTheHouse()
        {
            var t = NewTea();
            t.Gather(0); t.Gather(1);
            Assert.AreEqual(TeaStage.Gather, t.Stage);
            t.Gather(2);
            Assert.AreEqual(TeaStage.Search, t.Stage);
            for (int i = 0; i < 3; i++) Assert.AreEqual(TeaItemState.Hidden, t.StateOf(i));
        }

        [Test]
        public void YouCanCarryOnlyTwo()
        {
            var t = Vanished();
            Assert.IsTrue(t.PickUp(0));
            Assert.IsTrue(t.PickUp(1));
            Assert.IsFalse(t.PickUp(2), "hands full");
            Assert.AreEqual(2, t.Carrying);
            Assert.AreEqual(TeaItemState.Hidden, t.StateOf(2));
        }

        [Test]
        public void BringingTheFirstTwo_DoesNotCutThePower()
        {
            var t = Vanished();
            t.PickUp(0); t.PickUp(1);
            Assert.IsFalse(t.EnterKitchen());
            Assert.IsTrue(t.Place(0));
            Assert.IsTrue(t.Place(1));
            Assert.AreEqual(2, t.Placed);
        }

        [Test]
        public void ComingBackWithTheLastOne_CutsThePower()
        {
            var t = PowerCut();
            Assert.AreEqual(TeaStage.PowerCut, t.Stage);
            Assert.AreEqual(30f, t.PowerLeft, 1e-4f);
        }

        [Test]
        public void InTheDark_YouCantPlaceIt()
        {
            var t = PowerCut();
            Assert.IsFalse(t.Place(2));
        }

        [Test]
        public void RestoringThePowerInTime_ThenThePlaceStartsTheBrew()
        {
            var t = PowerCut();
            t.Tick(20f);
            t.RestorePower();
            Assert.AreEqual(TeaStage.Search, t.Stage);
            Assert.IsTrue(t.Place(2));
            Assert.AreEqual(TeaStage.Brew, t.Stage);
        }

        [Test]
        public void AfterTheFuse_WalkingBackIn_DoesntCutThePowerAgain()
        {
            var t = PowerCut();
            t.RestorePower();
            Assert.IsFalse(t.EnterKitchen(), "the power cuts once per hunt, on the way back with the last one");
            Assert.AreEqual(TeaStage.Search, t.Stage);
            Assert.IsTrue(t.Place(2));
        }

        [Test]
        public void TooSlow_EverythingRescatters()
        {
            var t = PowerCut();
            t.Tick(29.9f);
            Assert.IsFalse(t.RescatteredThisTick);
            t.Tick(0.2f);
            Assert.IsTrue(t.RescatteredThisTick, "+10, once");
            Assert.AreEqual(TeaStage.Search, t.Stage, "the lights come back and the hunt starts again");
            for (int i = 0; i < 3; i++) Assert.AreEqual(TeaItemState.Hidden, t.StateOf(i));
            Assert.AreEqual(0, t.Carrying);
            t.Tick(1f);
            Assert.IsFalse(t.RescatteredThisTick);
        }

        [Test]
        public void TheRebalance_OnlyOneRescatterANight()
        {
            var t = PowerCut();
            t.Tick(31f);                // first: rescatter
            t.PickUp(0); t.PickUp(1);
            t.EnterKitchen();
            t.Place(0); t.Place(1);
            t.PickUp(2);
            Assert.IsTrue(t.EnterKitchen(), "the power still cuts");
            t.Tick(60f);
            Assert.IsFalse(t.RescatteredThisTick, "but it doesn't rescatter twice");
            Assert.AreEqual(1, t.Rescatters);
            Assert.AreEqual(TeaStage.PowerCut, t.Stage, "you still have to fix the power");
            t.RestorePower();
            Assert.IsTrue(t.Place(2));
            Assert.AreEqual(TeaStage.Brew, t.Stage);
        }

        // ---- the dodge ----

        [Test]
        public void Dodge_Survive20SecondsInTheKitchen()
        {
            var d = new DodgeSession(20f, 3);
            d.Tick(19.9f, true);
            Assert.IsFalse(d.Done);
            d.Tick(0.2f, true);
            Assert.IsTrue(d.Survived);
        }

        [Test]
        public void Dodge_RunningAway_PausesIt()
        {
            var d = new DodgeSession(20f, 3);
            d.Tick(10f, true);
            d.Tick(30f, false);
            Assert.AreEqual(10f, d.Elapsed, 1e-4f);
            Assert.IsFalse(d.Done);
        }

        [Test]
        public void Dodge_TwoHits_YouStillMakeIt()
        {
            var d = new DodgeSession(20f, 3);
            Assert.IsFalse(d.Hit());
            Assert.IsFalse(d.Hit());
            d.Tick(21f, true);
            Assert.IsTrue(d.Survived);
            Assert.AreEqual(2, d.Hits);
        }

        [Test]
        public void Dodge_ThirdHit_TheKitchenCatchesFire()
        {
            var d = new DodgeSession(20f, 3);
            d.Hit(); d.Hit();
            Assert.IsTrue(d.Hit());
            Assert.IsTrue(d.Fire);
            Assert.IsTrue(d.Done);
            Assert.IsFalse(d.Hit(), "fire starts once");
            d.Tick(30f, true);
            Assert.IsFalse(d.Survived);
        }
    }
}
